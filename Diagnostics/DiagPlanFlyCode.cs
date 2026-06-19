using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using LaunchFixes.Diagnostics;
using Manager;

namespace LaunchFixes.Patches;

// Event-driven postfix on GameManager.PlanFlyCode, the entry the cyclical controller commits a
// planned leg through (SpaceCraftCyclicalMissionController.cs:316, after CheckCanPlanMission==AllOk).
// Chosen over a TryPlanCycleMission postfix because the leg's propellant is computed in an async
// SetPMParameterForCodeJobSystem callback that has NOT run when TryPlanCycleMission returns; by the
// time PlanFlyCode is invoked the leg is fully computed on missionParameter. Logs the planned leg's
// final state. Unthrottled (event, fires once per leg), deduped by sig.
[HarmonyPatch(typeof(GameManager), nameof(GameManager.PlanFlyCode))]
static class DiagPlanFlyCode {
    [HarmonyPostfix]
    static void Postfix(PMMissionParameter missionParameter) {
        if (!CycDiag.Enabled) {
            return;
        }
        try {
            CycDiag.FirstHit("DiagPlanFlyCode");
            var p = missionParameter;
            if (p == null) {
                return;
            }
            var cargoPot = p.CargoAll?.cargoFuel?.cargoMassPotencjal ?? -1.0;

            var line = $"PlanFlyCode | {CycDiag.Context(p)} "
                + $"| minFuelCost={CycDiag.R(p.MINFuelCost)} cargoMassPotencjal={CycDiag.R(cargoPot)} "
                + $"costLv={CycDiag.R(p.AllFuelNeedLV)} fuelNeed={CycDiag.R(p.AllFuelNeed)} "
                + $"flightCost={CycDiag.R(p.FlightCost)} "
                + $"AllFuelNeed={CycDiag.R(p.AllFuelNeed)} AllFuelNeedLV={CycDiag.R(p.AllFuelNeedLV)} "
                + $"totalDrain={CycDiag.R(p.AllFuelNeed + p.AllFuelNeedLV)}";

            var sig = $"{CycDiag.Context(p)}|{CycDiag.R(p.MINFuelCost)}|{CycDiag.R(cargoPot)}"
                + $"|{CycDiag.R(p.AllFuelNeedLV)}|{CycDiag.R(p.AllFuelNeed)}|{CycDiag.R(p.FlightCost)}";

            CycDiag.Event("DiagPlanFlyCode", sig, line);
        } catch (Exception ex) {
            CycDiag.Log($"DiagPlanFlyCode postfix failed: {ex.Message}");
        }
    }
}
