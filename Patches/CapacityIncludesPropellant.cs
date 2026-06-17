using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFixes.Patches;

// Launch-vehicle capacity gate: refuse loadouts whose full mass exceeds what the
// launch vehicle can lift to orbit.
//
// Vanilla CheckLV (PMMissionParameter.cs:896/:919) compares capacity vs CARGO ONLY:
//   MaxPayloadOnThisObject(Start, FlyCompany) * LVCount  >=  CargoCurrent
// — it ignores the craft's dry hull mass and the loaded propellant.
//
// We replace that with the craft's real ascent mass:
//   payload   = dry*SCCount + CargoCurrent + loaded fuel
//   capacity  = MaxPayloadOnThisObject(Start, FlyCompany) * LVCount
// Capacity is vanilla's term verbatim: it scales with launch-site gravity (more
// capacity on low-g bodies) and differs per LV, both correct. We only tighten
// (flip __result true -> false); we never loosen, and refuse on STRICT payload >
// capacity so an exactly-full load (payload == capacity) stays allowed.
//
// loaded fuel = cargoFuel.cargoMassPotencjal — the slider's live target, the same
// term the game's own total-mass uses (GetAcceleration, :1441). The slider clamp
// in SliderMaxRespectsCapacity caps that to capacity - dry - cargo, so the two
// patches agree: the gate refuses exactly the loadouts the slider cannot reach.
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

            // capacity = vanilla's lift term, scaled by LV count.
            var capacity = lv.GetLaunchVehicleType()
                    .MaxPayloadOnThisObject(__instance.Start, __instance.FlyCompany)
                * __instance.LVCount;
            // payload = dry hull + cargo + loaded fuel (the craft's real ascent mass).
            var dryMass = (double)sc.GetMass() * __instance.SCCount;
            var loadedFuel = cargo.cargoFuel.cargoMassPotencjal;
            var payload = dryMass + cargo.CargoCurrent;

            var refuse = payload > capacity;

            Plugin.LogCapacityCheck(
                dryMass,
                cargo.CargoCurrent,
                loadedFuel,
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
