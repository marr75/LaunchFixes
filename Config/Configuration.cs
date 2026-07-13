using BepInEx.Configuration;
using UnityEngine;

namespace LaunchFixes.Config;

sealed class Configuration {
    public readonly ConfigEntry<bool> AutoDumpVehicleStatsOnLoad;
    public readonly ConfigEntry<KeyboardShortcut> DumpVehicleStatsHotkey;
    public readonly ConfigEntry<bool> LaunchVehiclePaysForAscent;
    public readonly ConfigEntry<double> NearEmptyLaunchCostFloor;

    public Configuration(ConfigFile c) {
        const string nearEmptyLaunchCostFloorDescription = "Minimum launch cost for an almost-empty launch vehicle, as a fraction of a full "
            + "launch's cost. Vanilla lets this trend toward free as payload shrinks (0); set "
            + "higher to close that loophole, up to 1 (a near-empty launch costs the same as a "
            + "full one). Changes apply immediately, no restart needed.";
        NearEmptyLaunchCostFloor = c.Bind(
            "Balance",
            "NearEmptyLaunchCostFloor",
            0.0,
            new ConfigDescription(
                nearEmptyLaunchCostFloorDescription,
                new AcceptableValueRange<double>(0.0, 1.0)
            )
        );
        const string launchVehiclePaysForAscentDescription = "When on, the launch vehicle is billed for your craft's launch burn and your craft keeps "
            + "a full tank on arrival. When off, your craft burns its own fuel to launch (vanilla). "
            + "Changes apply immediately, no restart needed.";
        LaunchVehiclePaysForAscent = c.Bind("Balance", "LaunchVehiclePaysForAscent", true, launchVehiclePaysForAscentDescription);
        const string autoDumpVehicleStatsOnLoadDescription = "Writes every launch vehicle's stats to a file as soon as the game finishes loading them. "
            + "Only useful for reporting bugs or digging into the numbers yourself.";
        AutoDumpVehicleStatsOnLoad = c.Bind("Debug", "AutoDumpVehicleStatsOnLoad", false, autoDumpVehicleStatsOnLoadDescription);
        const string dumpVehicleStatsHotkeyDescription = "Keyboard shortcut that dumps launch vehicle stats to a file on demand, regardless of the "
            + "auto-dump setting above. Defaults to Ctrl+Shift+L (F8 is reserved by the game's "
            + "bug-report button).";
        DumpVehicleStatsHotkey = c.Bind(
            "Debug",
            "DumpVehicleStatsHotkey",
            new KeyboardShortcut(KeyCode.L, KeyCode.LeftControl, KeyCode.LeftShift),
            dumpVehicleStatsHotkeyDescription
        );
    }
}
