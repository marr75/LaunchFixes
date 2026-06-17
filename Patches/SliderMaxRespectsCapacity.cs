using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using UnityEngine;

namespace LaunchFixes.Patches;

// Clamps the fuel slider's max (and its SetDate auto-fill, FuelSpaceCraftUI:164) to the LV's
// spare lift: headroom = capacity - dry - cargo, same basis as the CheckLV gate.
// Clamped into [0, stock max] — never raises the stock max, never negative.
[HarmonyPatch(typeof(PMMissionParameter), nameof(PMMissionParameter.MaxValueSliderFuel))]
static class SliderMaxRespectsCapacity {
    [HarmonyPostfix]
    static void Postfix(PMMissionParameter __instance, ref double __result) {
        try {
            var lv = __instance.LV;
            var sc = __instance.SC;
            var cargo = __instance.CargoAll;
            if (lv == null || sc == null || cargo == null) {
                return; // self-launch / incomplete plan — leave stock max
            }

            // Skip the special branches that use a different mass basis (same set as the gate).
            if (cargo.entireAsteroid
                || __instance.ForCyclicalMission
                || !ReferenceEquals(__instance.Start, __instance.StartHermesCase)
                || sc.GetTypeSpaceCraft().IsInterstellarShipOrAsteroidPullingShipFromFacility) {
                return;
            }

            // Headroom = lift capacity minus dry hull and cargo.
            var capacity = lv.GetLaunchVehicleType().MaxPayloadOnThisObject(__instance.Start, __instance.FlyCompany)
                * __instance.LVCount;
            var dryMass = (double)sc.GetMass() * __instance.SCCount;
            var headroom = capacity - dryMass - cargo.CargoCurrent;

            __result = Mathd.Clamp(headroom, 0.0, __result); // never raise the max, never negative
        } catch (Exception ex) {
            Plugin.Log.LogError($"SliderMaxRespectsCapacity postfix failed: {ex}");
            // fail open — leave the stock max so the loadout UI never breaks
        }
    }
}
