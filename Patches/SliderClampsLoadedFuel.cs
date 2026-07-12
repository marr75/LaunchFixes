using System;
using Game.UI.Windows.Elements.PlanMissionElements.PMScheduleElements;
using HarmonyLib;

namespace LaunchFixes.Patches;

// Prefix on OnValueSliderChange: clamps the slider to LV spare lift (capacity - dry - cargo),
// same basis as the CheckLV gate. Downward only — never raises arg0; leaves the stock min-snap.
[HarmonyPatch(typeof(FuelSpaceCraftUI), "OnValueSliderChange")]
static class SliderClampsLoadedFuel {
    [HarmonyPrefix]
    static void Prefix(FuelSpaceCraftUI __instance, ref float arg0) {
        try {
            var p = __instance.tabSchedule.PlanMissionWindow.PMMissionParameter;
            var lv = p.LV;
            var sc = p.SC;
            var cargo = p.CargoAll;
            if (lv == null || sc == null || cargo == null) {
                return; // self-launch / incomplete plan — leave stock value
            }

            if (cargo.entireAsteroid
                || p.ForCyclicalMission
                || !ReferenceEquals(p.Start, p.StartHermesCase)
                || sc.GetTypeSpaceCraft().IsInterstellarShipOrAsteroidPullingShipFromFacility) { return; }

            var capacity = lv.GetLaunchVehicleType().MaxPayloadOnThisObject(p.Start, p.FlyCompany)
                * p.LVCount;
            var dryMass = (double)sc.GetMass() * p.SCCount;
            var headroom = capacity - dryMass - cargo.CargoCurrent;

            // Clamp downward only — never raise the slider, never negative.
            if (headroom >= 0.0 && arg0 > Math.Floor(headroom)) { arg0 = (float)Math.Floor(headroom); }
        }
        catch (Exception ex) {
            Plugin.Log.LogError($"SliderClampsLoadedFuel prefix failed: {ex}");
            // fail open — leave arg0 untouched so the loadout UI never breaks
        }
    }
}
