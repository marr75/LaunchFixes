using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx.Logging;
using Data.ScriptableObject;
using Manager;
using UnityEngine;

namespace LaunchFixes.Diagnostics;

static class VehicleStatsDumper {
    internal static void Dump(ManualLogSource log, string pluginDir) {
        try {
            var mgr = SerializedMonoBehaviourSingleton<AllScriptableObjectManager>.Instance;
            if (mgr == null) {
                log.LogWarning("[VehicleStatsDumper] AllScriptableObjectManager singleton is null — skipping dump.");
                return;
            }

            var all = mgr.AllLaunchVehicleType;
            if (all == null) {
                log.LogWarning("[VehicleStatsDumper] AllLaunchVehicleType is null — skipping dump.");
                return;
            }

            List<LaunchVehicleType> list = all.ListNotEmpty;
            if (list == null || list.Count == 0) {
                // Fallback: gather instances already loaded in memory
                var found = Resources.FindObjectsOfTypeAll<LaunchVehicleType>();
                if (found == null || found.Length == 0) {
                    log.LogWarning("[VehicleStatsDumper] No LaunchVehicleType instances found (singleton list empty, fallback empty).");
                    return;
                }
                list = new List<LaunchVehicleType>(found);
                log.LogInfo("[VehicleStatsDumper] Used Resources.FindObjectsOfTypeAll fallback.");
            }

            var sb = new StringBuilder();
            string header = $"{"ID",-40} {"Name",-28} {"Payload",10} {"FuelLoad",10} {"CostLaunch",12} {"ExhaustV",10} {"Reuse",7}";
            string rule   = new string('-', header.Length);
            sb.AppendLine(rule);
            sb.AppendLine(header);
            sb.AppendLine(rule);

            log.LogInfo($"[VehicleStatsDumper] {rule}");
            log.LogInfo($"[VehicleStatsDumper] {header}");
            log.LogInfo($"[VehicleStatsDumper] {rule}");

            foreach (var lv in list) {
                string name;
                try { name = lv.Name; } catch { name = lv.ID; }

                string row = $"{lv.ID,-40} {name,-28} {lv.maxPayload,10:F1} {lv.maxFuelLoad,10:F1} {lv.costLaunch,12:F1} {lv.exhaustV,10:F2} {lv.reusability,7:F3}";
                sb.AppendLine(row);
                log.LogInfo($"[VehicleStatsDumper] {row}");
            }

            sb.AppendLine(rule);
            log.LogInfo($"[VehicleStatsDumper] {rule}");
            log.LogInfo($"[VehicleStatsDumper] Dumped {list.Count} launch vehicle(s).");

            string outPath = Path.Combine(pluginDir, "lv-stats-dump.txt");
            File.WriteAllText(outPath, sb.ToString());
            log.LogInfo($"[VehicleStatsDumper] Written to {outPath}");
        }
        catch (Exception ex) {
            log.LogError($"[VehicleStatsDumper] Dump failed: {ex}");
        }
    }
}
