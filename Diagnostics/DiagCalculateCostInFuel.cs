using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using LaunchFixes.Diagnostics;

namespace LaunchFixes.Patches;

// Diagnostic-only postfix on the core fuel-cost math. Logs the values that discriminate the
// branches: minFuelCost + cargoMassPotencjal together let us infer flag2 (flag2=false zeroes both).
// costStart out-param is the fresh costLv the caller will store; __result is fuelNeed.
[HarmonyPatch(typeof(PMTabSchedule), "CalculateCostInFuel")]
static class DiagCalculateCostInFuel {
    [HarmonyPostfix]
    static void Postfix(
        PMTabSchedule __instance,
        ref double __result,
        ref double costStart,
        ref double minFuelCost,
        ref double _flightCost
    ) {
        if (!CycDiag.Enabled) { return; }
        try {
            CycDiag.FirstHit("DiagCalculateCostInFuel");
            var p = __instance.PlanMissionWindow.PMMissionParameter;
            var cargoPot = p.CargoAll.cargoFuel.cargoMassPotencjal;

            // AllFuelNeed/AllFuelNeedLV fields are written by SetFuelNeed AFTER this returns, so
            // they are stale here; the fresh values are the out-params (costStart=costLv, __result).
            var line = $"CalculateCostInFuel | {CycDiag.Context(p)} "
                + $"| minFuelCost={CycDiag.R(minFuelCost)} cargoMassPotencjal={CycDiag.R(cargoPot)} "
                + $"costLv(out)={CycDiag.R(costStart)} flightCost(out)={CycDiag.R(_flightCost)} "
                + $"fuelNeed(result)={CycDiag.R(__result)} "
                + $"AllFuelNeed(field)={CycDiag.R(p.AllFuelNeed)} AllFuelNeedLV(field)={CycDiag.R(p.AllFuelNeedLV)}";

            var sig = $"{CycDiag.Context(p)}|{CycDiag.R(minFuelCost)}|{CycDiag.R(cargoPot)}"
                + $"|{CycDiag.R(costStart)}|{CycDiag.R(_flightCost)}|{CycDiag.R(__result)}";

            CycDiag.Throttled("DiagCalculateCostInFuel", sig, line);
        }
        catch (Exception ex) { CycDiag.Log($"DiagCalculateCostInFuel postfix failed: {ex.Message}"); }
    }
}
