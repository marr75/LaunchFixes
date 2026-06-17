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

    ConfigEntry<bool> _dumpVehicleStats = null!;
    ConfigEntry<KeyboardShortcut> _dumpHotkey = null!;

    bool _autoDumped;

    void Awake() {
        Log = Logger;

        LaunchCostMinFactor = Config.Bind(
            "Balance", "LaunchCostMinFactor",
            0.0,
            "Raises the lower bound of the launch-vehicle ascent-cost lerp. The LV charge is "
            + "lerped from minFactor x full cost (empty payload) to full cost (payload = capacity) "
            + "across the payload fill fraction. 0 = vanilla (no change, a near-empty LV costs ~nothing); "
            + "1 = flat full launch cost regardless of payload. Clamped to [0,1]. Read live, so retuning "
            + "in-game takes effect without a rebuild.");

        _dumpVehicleStats = Config.Bind(
            "Diagnostics", "DumpVehicleStats",
            false,
            "When true, auto-dumps all launch vehicle stats once the AllScriptableObjectManager singleton is ready.");

        _dumpHotkey = Config.Bind(
            "Diagnostics", "DumpHotkey",
            new KeyboardShortcut(KeyCode.L, KeyCode.LeftControl, KeyCode.LeftShift),
            "Ctrl+Shift+L: dump launch vehicle stats on demand (works regardless of DumpVehicleStats). "
            + "Avoids F8, which the game reserves for its built-in bug-report button.");

        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} loaded.");
        new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();
    }

    void Update() {
        bool hotkeyPressed = _dumpHotkey.Value.IsDown();

        if (!_dumpVehicleStats.Value && !hotkeyPressed) {
            return;
        }

        if (hotkeyPressed) {
            string pluginDir = System.IO.Path.GetDirectoryName(Info.Location)!;
            VehicleStatsDumper.Dump(Log, pluginDir);
            return;
        }

        // Auto one-shot: wait until singleton is available, then dump once
        if (_autoDumped) {
            return;
        }

        if (SerializedMonoBehaviourSingleton<AllScriptableObjectManager>.InstanceIsNull) {
            return;
        }

        _autoDumped = true;
        string dir = System.IO.Path.GetDirectoryName(Info.Location)!;
        VehicleStatsDumper.Dump(Log, dir);
    }
}
