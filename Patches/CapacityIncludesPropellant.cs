using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFixes.Patches;

// CheckLV gate: refuse when the fueled stack exceeds the LV lift capacity
// (MaxPayloadOnThisObject x LVCount). One-shot legs stay slider-bounded (propellant clamped by
// the fuel slider, not counted here); cyclical legs have no slider, so the auto-loaded
// arrive-empty propellant (cargoMassPotencjal) is counted against capacity. CheckLVB swaps the
// backing fields to the B endpoint before delegating to CheckLV, so this postfix covers both legs.
// Strict > keeps an exactly-full load (payload == capacity) launchable.
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

            // Different mass basis / out of scope in both paths.
            if (cargo.entireAsteroid
                || sc.GetTypeSpaceCraft().IsInterstellarShipOrAsteroidPullingShipFromFacility) {
                return;
            }

            var capacity = lv.GetLaunchVehicleType()
                    .MaxPayloadOnThisObject(__instance.Start, __instance.FlyCompany)
                * __instance.LVCount;
            var dryMass = (double)sc.GetMass() * __instance.SCCount;

            double payload;
            if (__instance.ForCyclicalMission) {
                // No fuel slider on a cyclical leg: the arrive-empty load (minFuelCost, already in
                // cargoMassPotencjal) is auto-loaded and must count against lift capacity.
                // cargoMassPotencjal is 0 for LowOrbitContainer payloads, so this no-ops there.
                var fuelMass = cargo.cargoFuel.cargoMassPotencjal;
                if (!ReferenceEquals(__instance.Start, __instance.StartHermesCase)) {
                    // Hermes leg starts from an intermediate orbit: fuel already staged there
                    // isn't lifted by this LV, so it must not count against capacity.
                    var staged = __instance.StartHermesCaseDataCheckResources
                        .CheckResourcesInterface(__instance.FuelNeedToStart);
                    fuelMass = Math.Max(0.0, fuelMass - staged);
                }
                payload = dryMass + cargo.CargoCurrent + fuelMass;
            } else {
                if (!ReferenceEquals(__instance.Start, __instance.StartHermesCase)) {
                    return; // one-shot non-Hermes basis handled elsewhere — unchanged
                }
                payload = dryMass + cargo.CargoCurrent; // one-shot: slider clamps fuel
            }

            if (payload > capacity) {
                __result = false;
            }
        } catch (Exception ex) {
            Plugin.Log.LogError($"CapacityIncludesPropellant postfix failed: {ex}");
            // fail open — never block launch on a patch error
        }
    }
}
