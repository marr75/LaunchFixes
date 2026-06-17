using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFixes.Patches;

[HarmonyPatch(typeof(PMTabSchedule), "CalculateCostInFuel")]
static class LaunchVehicleBearsLaunch {
    [HarmonyPostfix]
    static void Postfix(PMTabSchedule __instance,
                        ref double __result,
                        ref double leftOverFuel,
                        ref double _flightCost) {
        try {
            var p = __instance.PlanMissionWindow.PMMissionParameter;
            if (p.LV == null) {
                return; // self-launch — craft burns its own fuel (stock behavior)
            }

            double loaded = p.CargoAll.cargoFuel.cargoMassPotencjal; // L

            // LV bears the launch: zero the SC launch burn so the craft keeps its full load.
            _flightCost = 0.0;       // no SC launch burn
            leftOverFuel = loaded;   // craft arrives with its full load → :1251 fills the tank
            __result = loaded;       // fuelNeed stays L: planet fills the tank, total billed = costLv + L
        } catch (Exception ex) {
            Plugin.Log.LogError($"LaunchVehicleBearsLaunch postfix failed: {ex}");
            // fall back to stock behavior
        }
    }
}
