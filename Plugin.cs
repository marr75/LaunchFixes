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

    // Live-read by the LaunchVehicleBearsLaunch postfix. false = byte-identical vanilla.
    internal static ConfigEntry<bool> LaunchVehicleBearsLaunch = null!;
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

        LaunchVehicleBearsLaunch = Config.Bind(
            "Balance",
            "LaunchVehicleBearsLaunch",
            true,
            "When true, a launch vehicle bears the craft's launch burn: the SC keeps its full fuel "
            + "load and the LV is billed for the ascent. false = vanilla (the postfix early-returns, "
            + "no field writes), so the cyclical path is uncontaminated. Read live, so toggling takes "
            + "effect without a rebuild."
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

        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} loaded.");
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        // Throwaway [CYCDIAG] instrumentation: one-time self-diagnosis banner (attached-or-not signal).
        if (CycDiag.Enabled) {
            var patched = 0;
            try { patched = new System.Collections.Generic.List<System.Reflection.MethodBase>(harmony.GetPatchedMethods()).Count; } catch { /* count is best-effort */ }
            CycDiag.Log($"loaded, Enabled={CycDiag.Enabled}, patchedMethods={patched}, "
                + $"LaunchCostMinFactor={LaunchCostMinFactor.Value}, "
                + $"LaunchVehicleBearsLaunch={LaunchVehicleBearsLaunch.Value}");
        }
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

        // Readiness via an existence count (not a nullable compare, which trips an
        // always-false inspection). The dump body resolves the singleton via .Instance.
        if (Resources.FindObjectsOfTypeAll<AllScriptableObjectManager>().Length == 0) {
            return; // manager not loaded yet — retry next frame
        }

        _autoDumped = true;
        Log.LogInfo("DumpVehicleStats=true and singleton ready — attempting auto dump.");
        var dir = Path.GetDirectoryName(Info.Location)!;
        VehicleStatsDumper.Dump(Log, dir);
    }

}
