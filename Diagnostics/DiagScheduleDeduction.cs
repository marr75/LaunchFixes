using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using LaunchFixes.Diagnostics;

namespace LaunchFixes.Patches;

// Diagnostic-only postfix on the commit path. The dual RemoveResource at PMTabSchedule.cs:1073
// deducts AllFuelNeedLV (=costLv) and AllFuelNeed (=fuelNeed) from the start planet's stock.
// This is the ground-truth charge. justCheckCanCreatedFly=true is the gate-check pass, not a commit.
[HarmonyPatch(typeof(PMTabSchedule), "OnClickScheduleButton",
    new[] { typeof(bool), typeof(bool) }, new[] { ArgumentType.Normal, ArgumentType.Out })]
static class DiagScheduleDeduction {
    [HarmonyPostfix]
    static void Postfix(PMTabSchedule __instance, bool justCheckCanCreatedFly, bool result) {
        if (!CycDiag.Enabled) {
            return;
        }
        try {
            CycDiag.FirstHit("DiagScheduleDeduction");
            var p = __instance.PlanMissionWindow.PMMissionParameter;
            var lvCharge = p.AllFuelNeedLV;
            var scCharge = p.AllFuelNeed;
            var phase = justCheckCanCreatedFly ? "CHECK" : (result ? "COMMIT" : "abort");

            var line = $"ScheduleDeduction | {CycDiag.Context(p)} "
                + $"| phase={phase} result={result} "
                + $"AllFuelNeedLV={CycDiag.R(lvCharge)} AllFuelNeed={CycDiag.R(scCharge)} "
                + $"totalDrain={CycDiag.R(lvCharge + scCharge)}";

            var sig = $"{CycDiag.Context(p)}|{phase}|{result}|{CycDiag.R(lvCharge)}|{CycDiag.R(scCharge)}";

            CycDiag.Event("DiagScheduleDeduction", sig, line);
        } catch (Exception ex) {
            CycDiag.Log($"DiagScheduleDeduction postfix failed: {ex.Message}");
        }
    }
}
