namespace LaunchFixes.Diagnostics;

// Throwaway instrumentation for the cyclical-mission propellant calc. Logs only; never changes
// game behavior. Flip Enabled to false (or delete this folder) to remove the whole set.
static class CycDiag {
    // Single master toggle for the whole diagnostic set. Flip to false to disable every log call.
    // static readonly (not const) so the always-true guards don't trip CS0162 unreachable-code.
    internal static readonly bool Enabled = false;

    const string Tag = "[CYCDIAG]";

    // All accessed only from Harmony postfixes on Unity's main thread, so the plain Dictionaries
    // need no locking.
    static readonly System.Collections.Generic.Dictionary<string, string> LastSig = new();
    static readonly System.Collections.Generic.Dictionary<string, double> LastEmitTime = new();
    static readonly System.Collections.Generic.HashSet<string> Hit = new();

    // Inline time-based throttle for the CONTINUOUS site (CalculateCostInFuel): emit only when at
    // least 5s elapsed since the last emit for this site AND the sig changed. No coroutine, no
    // background worker — fires synchronously inside the patch on the main thread.
    internal static void Throttled(string site, string sig, string line) {
        if (!Enabled) {
            return;
        }
        var now = UnityEngine.Time.realtimeSinceStartup;
        var stale = !LastEmitTime.TryGetValue(site, out var t) || now - t >= 5f;
        var changed = !LastSig.TryGetValue(site, out var prev) || prev != sig;
        if (!stale || !changed) {
            return;
        }
        LastSig[site] = sig;
        LastEmitTime[site] = now;
        Log($"{site} {line}");
    }

    // Unthrottled emit for EVENT sites (fire at most once per user/AI action). Deduped only by sig
    // to drop exact repeats; no time gate, so every distinct planned/committed leg is logged.
    internal static void Event(string site, string sig, string line) {
        if (!Enabled) {
            return;
        }
        if (LastSig.TryGetValue(site, out var prev) && prev == sig) {
            return;
        }
        LastSig[site] = sig;
        Log($"{site} {line}");
    }

    // One-time "FIRST-HIT <site>" marker, regardless of throttle, so we can tell attached-but-never
    // -called from never-attached.
    internal static void FirstHit(string site) {
        if (!Enabled || !Hit.Add(site)) {
            return;
        }
        Log($"FIRST-HIT {site}");
    }

    internal static void Log(string body) {
        if (!Enabled) {
            return;
        }
        Plugin.Log.LogInfo($"{Tag} {body}");
    }

    // Round to ~1 decimal for signatures/display so float jitter doesn't defeat the sig dedup.
    internal static string R(double v) => double.IsNaN(v) || double.IsInfinity(v)
        ? v.ToString()
        : (System.Math.Round(v, 1)).ToString(System.Globalization.CultureInfo.InvariantCulture);

    // Identifying context present on every line so messages from different methods correlate by eye.
    internal static string Context(Game.UI.Windows.Elements.PlanMissionElements.PMMissionParameter p) {
        string sc;
        try { sc = p.SC?.GetTypeSpaceCraft()?.ID ?? "noSC"; } catch { sc = "?SC"; }
        string lv;
        try { lv = p.LV?.GetLaunchVehicleType()?.ID ?? "none"; } catch { lv = "?LV"; }
        return $"SC={sc} LV={lv} cyc={p.ForCyclicalMission} orbit={p.OrbitCase} moon={p.MoonCase} "
            + $"o2p={p.OrbitToPlanetNoLowOrbitalContainer} loc={p.IsLowOrbitalContainer} rfm={p.ReduceFuelToMinimum}";
    }
}
