using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFixes.Patches;

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

            double payload = (double)sc.GetMass() * __instance.SCCount
                           + cargo.CargoCurrent
                           + cargo.cargoFuel.cargoMassPotencjal;
            double capacity = lv.GetLaunchVehicleType()
                                .MaxPayloadOnThisObject(__instance.Start, __instance.FlyCompany)
                            * __instance.LVCount;

            if (payload > capacity) {
                __result = false;
            }
        } catch (Exception ex) {
            Plugin.Log.LogError($"CapacityIncludesPropellant postfix failed: {ex}");
            // fail open — never block launch on a patch error
        }
    }
}
