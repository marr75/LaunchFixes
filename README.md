# LaunchFixes

A BepInEx 5 (Mono) Harmony mod for the Unity game **Solar Expanse**.

Fixes launch-vehicle propellant accounting: vanilla charges propellant deduction to both the
deployed craft and the launch vehicle, and the launch vehicle's capacity calculation ignores its
own propellant mass. LaunchFixes patches the ascent-cost and capacity logic so the launch vehicle
correctly bears the launch burn while the deployed craft keeps its full fuel load, and includes
tunable config for the ascent-cost curve plus optional diagnostics (vehicle-stat dumps, cyclical
mission scheduling traces).

Config is exposed through BepInEx's standard config file (`Balance` and `Diagnostics` sections),
including a live-read `LaunchCostMinFactor` and `LaunchVehicleBearsLaunch` toggle, and a
Ctrl+Shift+L hotkey to dump launch-vehicle stats on demand.

## Build

Requires the .NET SDK targeting `net48` and a local Solar Expanse install.

```powershell
$env:SOLAR_EXPANSE_DIR = 'C:\path\to\Solar Expanse'
dotnet build
```

`SOLAR_EXPANSE_DIR` must point at the game's install directory (the folder containing
`Solar Expanse_Data`). The build resolves game references from
`$SOLAR_EXPANSE_DIR\Solar Expanse_Data\Managed` and, after a successful build, copies the plugin
DLL (and PDB) into `$SOLAR_EXPANSE_DIR\BepInEx\plugins\LaunchFixes\`.

License: MIT (see LICENSE)
