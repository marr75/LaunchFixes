using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using LaunchFixes.Diagnostics;

namespace LaunchFixes.Patches;

// Event-driven postfix on the CODE/CYCLICAL/AI commit handler. GameManager.PlanFlyCode (the path
// the cyclical controller commits through, SpaceCraftCyclicalMissionController.cs:316) routes here
// via OnClickScheduleButtonForCode -> OnClickScheduleButton(false). The cyclical leg has NO fuel
// slider (auto-sized via ReduceFuelToMinimum) and only this OK/schedule action, so this fires once
// per committed leg with the leg's FINAL computed state — the real capture point for the cyclical
// path. Unthrottled (event), deduped by sig. The inner OnClickScheduleButton overload is also
// patched (DiagScheduleDeduction) for the raw stock deduction; this site adds the full final state.
[HarmonyPatch(typeof(PMTabSchedule), "OnClickScheduleButtonForCode")]
static class DiagCyclicalCommit {
    [HarmonyPostfix]
    static void Postfix(PMTabSchedule __instance) {
        if (!CycDiag.Enabled) { return; }
        try {
            CycDiag.FirstHit("DiagCyclicalCommit");
            var p = __instance.PlanMissionWindow.PMMissionParameter;
            var cargoPot = p.CargoAll.cargoFuel.cargoMassPotencjal;

            var line = $"CyclicalCommit | {CycDiag.Context(p)} "
                + $"| minFuelCost={CycDiag.R(p.MINFuelCost)} cargoMassPotencjal={CycDiag.R(cargoPot)} "
                + $"costLv={CycDiag.R(p.AllFuelNeedLV)} fuelNeed={CycDiag.R(p.AllFuelNeed)} "
                + $"flightCost={CycDiag.R(p.FlightCost)} "
                + $"AllFuelNeed={CycDiag.R(p.AllFuelNeed)} AllFuelNeedLV={CycDiag.R(p.AllFuelNeedLV)} "
                + $"totalDrain={CycDiag.R(p.AllFuelNeed + p.AllFuelNeedLV)}";

            var sig = $"{CycDiag.Context(p)}|{CycDiag.R(p.MINFuelCost)}|{CycDiag.R(cargoPot)}"
                + $"|{CycDiag.R(p.AllFuelNeedLV)}|{CycDiag.R(p.AllFuelNeed)}|{CycDiag.R(p.FlightCost)}";

            CycDiag.Event("DiagCyclicalCommit", sig, line);
        }
        catch (Exception ex) { CycDiag.Log($"DiagCyclicalCommit postfix failed: {ex.Message}"); }
    }
}
