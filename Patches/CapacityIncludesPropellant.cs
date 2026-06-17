using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFixes.Patches;

// CheckLV gate: refuse when dry hull + cargo exceeds the LV lift capacity
// (MaxPayloadOnThisObject x LVCount). Propellant is bounded by the slider clamp, not here;
// strict > keeps an exactly-full load (payload == capacity) launchable.
[HarmonyPatch(typeof(PMMissionParameter), nameof(PMMissionParameter.CheckLV))]
static class CapacityIncludesPropellant {
    [HarmonyPostfix]
    static void Postfix(PMMissionParameter __instance, ref bool __result) {
        try {
            if (!__result) {
                return; // stock already refuses — only ever tighten
            }
            var lv = __instance.LV;
            var sc = __instance.SC;
            var cargo = __instance.CargoAll;
            if (lv == null || sc == null || cargo == null) {
                return; // self-launch / incomplete plan — out of scope
            }

            // Skip the special branches that use a different mass basis.
            if (cargo.entireAsteroid
                || __instance.ForCyclicalMission
                || !ReferenceEquals(__instance.Start, __instance.StartHermesCase)
                || sc.GetTypeSpaceCraft().IsInterstellarShipOrAsteroidPullingShipFromFacility) {
                return;
            }

            var capacity = lv.GetLaunchVehicleType()
                    .MaxPayloadOnThisObject(__instance.Start, __instance.FlyCompany)
                * __instance.LVCount;
            var dryMass = (double)sc.GetMass() * __instance.SCCount;
            var maxFuel = cargo.cargoFuel.cargoMassPotencjal;
            var currentFuel = cargo.CargoCurrentFuel;
            var payload = dryMass + cargo.CargoCurrent;

            var refuse = payload > capacity;

            Plugin.LogCapacityCheck(
                dryMass,
                cargo.CargoCurrent,
                maxFuel,
                currentFuel,
                capacity,
                __instance.LVCount,
                __instance.SCCount,
                payload,
                refuse
            );

            if (refuse) {
                __result = false;
            }
        } catch (Exception ex) {
            Plugin.Log.LogError($"CapacityIncludesPropellant postfix failed: {ex}");
            // fail open — never block launch on a patch error
        }
    }
}
