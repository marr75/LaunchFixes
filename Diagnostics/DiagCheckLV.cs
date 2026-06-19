using System;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using LaunchFixes.Diagnostics;

namespace LaunchFixes.Patches;

// Diagnostic-only postfix on the LV capacity gate. The cyclical path gates on
// MaxPayloadOnThisObject*LVCount >= CargoCurrent (PMMissionParameter.cs:896) — cargo only, NOT
// propellant. Logging capacity vs cargo (required) vs dry+fuel shows why a small LV passes here
// but a one-shot capacity check (counting propellant) would refuse it.
[HarmonyPatch(typeof(PMMissionParameter), nameof(PMMissionParameter.CheckLV))]
static class DiagCheckLV {
    [HarmonyPostfix]
    static void Postfix(PMMissionParameter __instance, bool __result) {
        if (!CycDiag.Enabled) {
            return;
        }
        try {
            CycDiag.FirstHit("DiagCheckLV");
            var p = __instance;
            double capacity = -1.0, dry = -1.0, cargo = -1.0, loadedFuel = -1.0;
            try {
                var lv = p.LV;
                var sc = p.SC;
                if (lv != null && p.Start != null) {
                    capacity = lv.GetLaunchVehicleType().MaxPayloadOnThisObject(p.Start, p.FlyCompany)
                        * p.LVCount;
                }
                if (sc != null) {
                    dry = (double)sc.GetMass() * p.SCCount;
                }
                if (p.CargoAll != null) {
                    cargo = p.CargoAll.CargoCurrent;
                    loadedFuel = p.CargoAll.cargoFuel.cargoMassPotencjal;
                }
            } catch { /* leave sentinels; geometry may not expose Start/LV this tick */ }

            // required = cargo (the gate basis); the gate ignores dry+loadedFuel, which is the point.
            var line = $"CheckLV | {CycDiag.Context(p)} "
                + $"| verdict={(__result ? "ACCEPT" : "REFUSE")} "
                + $"capacity={CycDiag.R(capacity)} required(cargo)={CycDiag.R(cargo)} "
                + $"dry={CycDiag.R(dry)} loadedFuel={CycDiag.R(loadedFuel)} "
                + $"dry+cargo+fuel={CycDiag.R(dry + cargo + loadedFuel)}";

            var sig = $"{CycDiag.Context(p)}|{__result}|{CycDiag.R(capacity)}|{CycDiag.R(cargo)}"
                + $"|{CycDiag.R(dry)}|{CycDiag.R(loadedFuel)}";

            CycDiag.Throttled("DiagCheckLV", sig, line);
        } catch (Exception ex) {
            CycDiag.Log($"DiagCheckLV postfix failed: {ex.Message}");
        }
    }
}
