using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;

namespace LaunchFixes.Patches;

// CalculateCostInFuel clobbers cargoMassPotencjal before SetTextTooltip runs, so the tooltip
// shows a stale "leftover fuel" figure. We stash the player's selection at the outermost entry
// and inject it in the tooltip prefix.
static class TooltipLeftoverFuel {
    static PMTabSchedule? _owner;
    static double _loadedFuel;

    [HarmonyPatch(typeof(PMTabSchedule), "CalculateCostInFuel")]
    static class CaptureLoadedFuel {
        [HarmonyPrefix]
        static void Prefix(PMTabSchedule __instance) {
            try {
                if (PMTabSchedule.calculateCostInFuelRecurentOn) { // skip recursive re-entry
                    return;
                }
                var p = __instance.PlanMissionWindow.PMMissionParameter;
                if (p.LV == null) {
                    _owner = null;
                    return; // self-launch — stock behavior
                }
                _owner = __instance;
                _loadedFuel = p.CargoAll.cargoFuel.cargoMassPotencjal;
            } catch (Exception ex) {
                Plugin.Log.LogError($"TooltipLeftoverFuel capture prefix failed: {ex}");
                _owner = null;
            }
        }
    }

    [HarmonyPatch(typeof(PMTabSchedule), "SetTextTooltip")]
    static class OverrideLeftover {
        [HarmonyPrefix]
        static void Prefix(PMTabSchedule __instance, ref double leftOverFuel) {
            try {
                if (!ReferenceEquals(_owner, __instance)) {
                    return; // no captured load for this instance — leave stock value
                }
                var p = __instance.PlanMissionWindow.PMMissionParameter;
                if (p.LV == null) {
                    return; // self-launch — stock behavior
                }
                leftOverFuel = _loadedFuel;
            } catch (Exception ex) {
                Plugin.Log.LogError($"TooltipLeftoverFuel override prefix failed: {ex}");
                // fail open — leave leftOverFuel untouched
            }
        }
    }
}
