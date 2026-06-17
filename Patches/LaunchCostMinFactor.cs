using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using UnityEngine;

namespace LaunchFixes.Patches;

// Optional, default-OFF (minFactor 0 == vanilla, byte-identical). A transpiler swaps the single
// Mathd.Lerp(0, M, t) in CalculateCostStart (the costLv line) for LerpWithFloor, which lerps from
// minFactor * M instead of 0, so a near-empty LV no longer costs ~nothing. Everything after the
// Lerp (gravity/bonus mults, floor, rounding) is preserved; the no-LV path has M == 0, untouched.
[HarmonyPatch(typeof(PMTabSchedule), "CalculateCostStart")]
static class LaunchCostMinFactor {
    static readonly MethodInfo LerpMethod = AccessTools.Method(
        typeof(Mathd),
        nameof(Mathd.Lerp),
        new[] { typeof(double), typeof(double), typeof(double) }
    );

    static readonly MethodInfo FloorMethod = AccessTools.Method(
        typeof(LaunchCostMinFactor),
        nameof(LerpWithFloor)
    );

    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) {
        var swapped = 0;
        foreach (var ins in instructions) {
            if (ins.Calls(LerpMethod)) {
                // Mutate in place so any labels / exception blocks on this instruction are preserved.
                ins.opcode = OpCodes.Call;
                ins.operand = FloorMethod;
                swapped++;
            }
            yield return ins;
        }
        if (swapped != 1) {
            Plugin.Log.LogWarning(
                $"LaunchCostMinFactor: expected exactly 1 Mathd.Lerp call in CalculateCostStart, swapped {swapped}. "
                + "Launch-cost min-factor may be inactive; vanilla behavior is preserved."
            );
        }
    }

    // Stack-compatible stand-in for Mathd.Lerp(from, to, t): raises the floor to minFactor * to.
    // Fail-open — minFactor <= 0 or any error returns the exact stock Lerp(from, to, t).
    public static double LerpWithFloor(double from, double to, double t) {
        try {
            var minF = Plugin.LaunchCostMinFactor.Value;
            if (minF <= 0.0) {
                return Mathd.Lerp(from, to, t); // vanilla — from is 0.0, byte-identical
            }
            if (minF > 1.0) {
                minF = 1.0;
            }
            return Mathd.Lerp(minF * to, to, t);
        } catch (Exception ex) {
            Plugin.Log.LogError($"LaunchCostMinFactor.LerpWithFloor failed: {ex}");
            return Mathd.Lerp(from, to, t); // fall back to stock
        }
    }
}
