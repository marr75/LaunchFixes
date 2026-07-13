# Launch Fixes

> Launch vehicles finally pay their fair share — capacity, fuel, and cost all add up the way you'd expect.

<!-- SCREENSHOT: hero shot — the launch-vehicle picker/schedule tab showing a cost breakdown. File: docs/images/launchfixes-hero.png -->

## What it does

- **Launch vehicles can't be overloaded without you knowing.** Vanilla's "can this launch vehicle lift it?" check ignores the propellant you're loading — you could plan a launch that was actually too heavy and only find out later. This mod counts propellant mass toward the launch vehicle's lift capacity.
- **The fuel slider won't let you over-fuel in the first place.** It's now clamped to whatever the launch vehicle can actually lift, so you don't need to hunt-and-peck for a value that passes the capacity check.
- **The "leftover fuel" tooltip shows the right number.** Vanilla recalculates the schedule tab in a way that overwrites the figure right before the tooltip reads it, so the tooltip could show a stale amount. Now it shows what you actually loaded.
- **(On by default) The launch vehicle bears the cost of getting your craft off the ground**, instead of your spacecraft burning its own fuel for the ascent. Your craft launches with a full tank; the launch vehicle gets billed for the climb.
- **(Off by default) Close the "nearly-empty launch vehicle is nearly free" loophole.** In vanilla, launch cost scales down toward zero as the payload gets lighter, so an almost-empty launch vehicle costs almost nothing to send up. This adds a tunable floor so a near-empty launch still costs something.

## Before / after

Vanilla: an almost-empty launch vehicle launches for next to nothing, and the capacity check that's supposed to stop you loading more than a launch vehicle can lift doesn't count the propellant you're carrying. Launch Fixes: the capacity check accounts for propellant, and (if you turn it on) launch cost has a real floor instead of trending to zero.

## Configuration

Settings live in `BepInEx/config/marr75.solarexpanse.launchfixes.cfg` and are also editable in-game if you have Configuration Manager installed. The two worth knowing about:

- **`LaunchVehicleBearsLaunch`** (default: on) — the launch vehicle pays for your craft's launch burn instead of the craft itself. Turn it off to go back to vanilla's behavior.
- **`LaunchCostMinFactor`** (default: `0`, i.e. off) — how expensive a _nearly empty_ launch vehicle launch is, as a fraction of a _full_ launch's cost. `0` is vanilla (can trend toward free); `1` means a near-empty launch costs the same as a full one. Try something like `0.2`–`0.4` if you want near-empty launches to cost _something_ without being as expensive as a full load.

Everything under the `Diagnostics` section (`DumpVehicleStats`, `DumpHotkey`) is a developer tool for dumping launch-vehicle stats to a file — safe to ignore as a player.

<!-- SCREENSHOT: config panel showing LaunchCostMinFactor and LaunchVehicleBearsLaunch. File: docs/images/launchfixes-config.png -->

## Requirements

- Solar Expanse + BepInEx 5 (Mono/x64).

## Install

1. Install BepInEx 5.
2. Drop the `LaunchFixes` folder into `BepInEx/plugins/`.

## Building (developers)

`dotnet build` deploys the DLL to the game's plugins folder via the post-build target. See `AGENTS.md`.
