using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LaunchFixes.Config;
using LaunchFixes.Core;
using LaunchFixes.Diagnostics;
using Manager;
using UnityEngine;

namespace LaunchFixes;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    internal static ManualLogSource Log = null!;
    bool _autoDumped;
    bool _firstUpdateLogged;

    void Awake() {
        Log = Logger;
        Services.Init(new Configuration(Config)); // must precede patching: patches read Services.Config

        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} loaded.");
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        // Throwaway [CYCDIAG] instrumentation: one-time self-diagnosis banner (attached-or-not signal).
        if (CycDiag.Enabled) {
            var patched = 0;
            try { patched = new List<MethodBase>(harmony.GetPatchedMethods()).Count; }
            catch {
                /* count is best-effort */
            }
            CycDiag.Log(
                $"loaded, Enabled={CycDiag.Enabled}, patchedMethods={patched}, "
                + $"NearEmptyLaunchCostFloor={Services.Config.NearEmptyLaunchCostFloor.Value}, "
                + $"LaunchVehiclePaysForAscent={Services.Config.LaunchVehiclePaysForAscent.Value}"
            );
        }
    }

    void Update() {
        // Breadcrumb: prove Update() is actually ticking (once).
        if (!_firstUpdateLogged) {
            _firstUpdateLogged = true;
            Log.LogInfo("Plugin.Update() is running.");
        }

        bool hotkeyPressed;
        try { hotkeyPressed = Services.Config.DumpVehicleStatsHotkey.Value.IsDown(); }
        catch (Exception ex) {
            Log.LogError($"DumpVehicleStatsHotkey poll failed: {ex.Message}");
            hotkeyPressed = false;
        }

        if (!Services.Config.AutoDumpVehicleStatsOnLoad.Value && !hotkeyPressed) { return; }

        if (hotkeyPressed) {
            Log.LogInfo("Dump hotkey pressed — attempting on-demand dump.");
            var pluginDir = Path.GetDirectoryName(Info.Location)!;
            VehicleStatsDumper.Dump(Log, pluginDir);
            return;
        }

        // Auto one-shot: wait until the singleton resolves, then dump once.
        if (_autoDumped) { return; }

        // Readiness via an existence count (not a nullable compare, which trips an
        // always-false inspection). The dump body resolves the singleton via .Instance.
        if (Resources.FindObjectsOfTypeAll<AllScriptableObjectManager>().Length == 0) {
            return; // manager not loaded yet — retry next frame
        }

        _autoDumped = true;
        Log.LogInfo("AutoDumpVehicleStatsOnLoad=true and singleton ready — attempting auto dump.");
        var dir = Path.GetDirectoryName(Info.Location)!;
        VehicleStatsDumper.Dump(Log, dir);
    }
}
