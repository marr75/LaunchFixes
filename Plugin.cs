using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LaunchFixes.Diagnostics;
using Manager;
using UnityEngine;

namespace LaunchFixes;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    internal static ManualLogSource Log = null!;

    // Live-read by the LaunchCostMinFactor transpiler helper. Clamped to [0,1] on read.
    internal static ConfigEntry<double> LaunchCostMinFactor = null!;
    static ConfigEntry<bool> _logCapacityCheck = null!;

    // Rate-limit guard for LogCapacityCheck: CheckLV runs every frame, so only emit
    // when the rounded value tuple changes (and never more than ~once/sec).
    static string _lastCapacityLog = "";
    static float _lastCapacityLogTime;

    bool _autoDumped;
    bool _firstUpdateLogged;
    ConfigEntry<KeyboardShortcut> _dumpHotkey = null!;

    ConfigEntry<bool> _dumpVehicleStats = null!;

    void Awake() {
        Log = Logger;

        LaunchCostMinFactor = Config.Bind(
            "Balance",
            "LaunchCostMinFactor",
            0.0,
            "Raises the lower bound of the launch-vehicle ascent-cost lerp. The LV charge is "
            + "lerped from minFactor x full cost (empty payload) to full cost (payload = capacity) "
            + "across the payload fill fraction. 0 = vanilla (no change, a near-empty LV costs ~nothing); "
            + "1 = flat full launch cost regardless of payload. Clamped to [0,1]. Read live, so retuning "
            + "in-game takes effect without a rebuild."
        );

        _dumpVehicleStats = Config.Bind(
            "Diagnostics",
            "DumpVehicleStats",
            false,
            "When true, auto-dumps all launch vehicle stats once the AllScriptableObjectManager singleton is ready."
        );

        _dumpHotkey = Config.Bind(
            "Diagnostics",
            "DumpHotkey",
            new KeyboardShortcut(KeyCode.L, KeyCode.LeftControl, KeyCode.LeftShift),
            "Ctrl+Shift+L: dump launch vehicle stats on demand (works regardless of DumpVehicleStats). "
            + "Avoids F8, which the game reserves for its built-in bug-report button."
        );

        _logCapacityCheck = Config.Bind(
            "Diagnostics",
            "LogCapacityCheck",
            false,
            "When true, logs the launch-vehicle capacity-gate terms (dry, cargo, loaded fuel, capacity, "
            + "LVCount, SCCount, payload, verdict) from the CheckLV postfix. Rate-limited: logs only when "
            + "the rounded values change, at most about once per second — never per-frame spam."
        );

        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} loaded.");
        new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();
    }

    void Update() {
        // Breadcrumb: prove Update() is actually ticking (once).
        if (!_firstUpdateLogged) {
            _firstUpdateLogged = true;
            Log.LogInfo("Plugin.Update() is running.");
        }

        bool hotkeyPressed;
        try {
            hotkeyPressed = _dumpHotkey.Value.IsDown();
        } catch (System.Exception ex) {
            Log.LogError($"DumpHotkey poll failed: {ex.Message}");
            hotkeyPressed = false;
        }

        if (!_dumpVehicleStats.Value && !hotkeyPressed) {
            return;
        }

        if (hotkeyPressed) {
            Log.LogInfo("Dump hotkey pressed — attempting on-demand dump.");
            var pluginDir = Path.GetDirectoryName(Info.Location)!;
            VehicleStatsDumper.Dump(Log, pluginDir);
            return;
        }

        // Auto one-shot: wait until the singleton resolves, then dump once.
        if (_autoDumped) {
            return;
        }

        // Readiness via an existence count, not a nullable-reference compare: an int test
        // can't trip the "always-false null check" inspection, finds the manager even if
        // inactive, and matches the dumper's FindObjectsOfTypeAll fallback idiom. The dump
        // body still resolves the singleton via .Instance.
        if (Resources.FindObjectsOfTypeAll<AllScriptableObjectManager>().Length == 0) {
            return; // manager not loaded yet — retry next frame
        }

        _autoDumped = true;
        Log.LogInfo("DumpVehicleStats=true and singleton ready — attempting auto dump.");
        var dir = Path.GetDirectoryName(Info.Location)!;
        VehicleStatsDumper.Dump(Log, dir);
    }

    // Behind the [Diagnostics] LogCapacityCheck flag. Cheap and rate-limited: builds no
    // string and returns immediately when the flag is off; otherwise logs only when the
    // rounded value tuple changes or ~1s has elapsed, so CheckLV's per-frame cadence
    // cannot spam the log.
    internal static void LogCapacityCheck(
        double dry,
        double cargo,
        double loadedFuel,
        double capacity,
        int lvCount,
        int scCount,
        double payload,
        bool refuse
    ) {
        if (!_logCapacityCheck.Value) {
            return;
        }
        var line = $"[CapacityCheck] dry={dry:F1} cargo={cargo:F1} loadedFuel={loadedFuel:F1} "
            + $"capacity={capacity:F1} LVCount={lvCount} SCCount={scCount} "
            + $"payload={payload:F1} verdict={(refuse ? "REFUSE" : "allow")}";
        var now = Time.realtimeSinceStartup;
        if (line == _lastCapacityLog && now - _lastCapacityLogTime < 1f) {
            return;
        }
        _lastCapacityLog = line;
        _lastCapacityLogTime = now;
        Log.LogInfo(line);
    }
}
