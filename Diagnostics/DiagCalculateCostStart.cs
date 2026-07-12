using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using LaunchFixes.Diagnostics;

namespace LaunchFixes.Patches;

// Diagnostic-only postfix on the launch-cost calc. __result is costLv. The folded LV mass basis
// num6 (:2568 = GetMassToCalculateFuel() + cargoMassPotencjal) is a pure local not stored on any
// object, so we log its accessible components (massBase + cargoMassPotencjal) instead — see gap note.
[HarmonyPatch(typeof(PMTabSchedule), "CalculateCostStart")]
static class DiagCalculateCostStart {
    [HarmonyPostfix]
    static void Postfix(
        PMTabSchedule __instance,
        double fuelNoOnOrbit,
        bool launchCostZero,
        double __result
    ) {
        if (!CycDiag.Enabled) { return; }
        try {
            CycDiag.FirstHit("DiagCalculateCostStart");
            var p = __instance.PlanMissionWindow.PMMissionParameter;
            var massBase = p.GetMassToCalculateFuel();
            var cargoPot = p.CargoAll.cargoFuel.cargoMassPotencjal;
            var foldedMass = massBase + cargoPot; // mirrors :2568 num6 (the :2570 re-base is not visible here)

            var line = $"CalculateCostStart | {CycDiag.Context(p)} "
                + $"| costLv(result)={CycDiag.R(__result)} launchCostZero={launchCostZero} fuelNoOnOrbit={CycDiag.R(fuelNoOnOrbit)} "
                + $"massBase={CycDiag.R(massBase)} cargoMassPotencjal={CycDiag.R(cargoPot)} foldedMass~={CycDiag.R(foldedMass)}";

            var sig = $"{CycDiag.Context(p)}|{CycDiag.R(__result)}|{launchCostZero}"
                + $"|{CycDiag.R(massBase)}|{CycDiag.R(cargoPot)}";

            CycDiag.Throttled("DiagCalculateCostStart", sig, line);
        }
        catch (Exception ex) { CycDiag.Log($"DiagCalculateCostStart postfix failed: {ex.Message}"); }
    }
}
