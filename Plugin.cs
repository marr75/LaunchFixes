using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace LaunchFixes;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    internal static ManualLogSource Log = null!;

    void Awake() {
        Log = Logger;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} loaded.");
        new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();
    }
}
