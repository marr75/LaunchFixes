using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using UnityEngine;

namespace LaunchFixes.Patches;

// Optional, default-OFF. Raises the lower bound of the launch-vehicle ascent-cost lerp at
// PMTabSchedule.CalculateCostStart (the costLv line, decompiled :2603):
//
//     num8 = Mathd.Lerp(0.0, num4 * LVCount, num7 / num5) * <mults>
//
// where num4 = LV.costLaunch (0 when LV == null), so M = num4 * LVCount is the full LV charge,
// t = num7 / num5 is the payload fill fraction (Mathd.Lerp clamps it via Clamp01), and <mults>
// is the gravity/bonus chain that follows. Stock lower bound is 0, so a tiny payload in a big LV
// costs ~nothing. We optionally lerp from minFactor * M instead.
//
// Mechanism: a transpiler swaps the single `call Mathd.Lerp(double,double,double)` in this method
// for a call to LerpWithFloor, which receives the identical three stack args (from = 0, to = M, t)
// and returns Lerp(minFactor * to, to, t). Chosen over a postfix because CalculateCostStart returns
// the fully multiplied/floored/rounded num10, from which M and the gravity/bonus factor cannot be
// cleanly recovered (and M*G is unrecoverable at t == 0 — division by zero). The swap is the most
// surgical correct interception: everything after the Lerp (the <mults> chain, the 0.1 floor, the
// rounding, the LV bonuses at :2608) is preserved untouched. It targets the Mathd.Lerp *call* rather
// than the `0.0` ldc operand, so it does not depend on argument evaluation order.
//
// minFactor == 0 reduces to exactly the stock value:
//     LerpWithFloor(0, M, t) with minF == 0  ->  Lerp(0 * M, M, t) = Lerp(0, M, t)  (byte-identical).
// No-LV path is inherently untouched: num4 == 0 there, so M == 0 and Lerp(minF*0, 0, t) == 0 == stock.
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

    // Stack-compatible stand-in for Mathd.Lerp(from, to, t). `from` is the stock 0.0; `to` is M
    // (= costLaunch * LVCount). Raises the lerp floor to minFactor * to when configured. Fail-open:
    // any error (or minFactor <= 0) falls back to the exact stock Lerp(from, to, t).
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
