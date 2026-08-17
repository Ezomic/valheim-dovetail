using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace Dovetail
{
    internal static class DovetailConfig
    {
        /// <summary>
        /// Vanilla fences and stake walls, which ship with no snap points of their own.
        ///
        /// A list is unavoidable here - unlike a chest, nothing about a fence's components
        /// tells you it is a fence rather than any other wall. Keeping it in config at
        /// least means a wrong or outdated entry is something you can fix without a build,
        /// and unresolved names are logged rather than quietly doing nothing.
        /// </summary>
        private const string DefaultFences =
            "wood_fence, piece_sharpstakes, piece_stakewall_blackwood, "
            + "piece_dvergr_sharpstakes, piece_dvergr_stake_wall";

        public static ConfigEntry<bool> SnapContainers;
        public static ConfigEntry<bool> SnapFences;
        public static ConfigEntry<bool> SnapUnsnappedPieces;
        public static ConfigEntry<string> FencePrefabs;
        public static ConfigEntry<string> ExcludePrefabs;
        public static ConfigEntry<float> Gap;
        public static ConfigEntry<float> FenceLadderStep;
        public static ConfigEntry<float> FenceLadderBelow;
        public static ConfigEntry<bool> Verbose;

        public static void Bind(ConfigFile config)
        {
            SnapContainers = config.Bind("Snapping", "SnapContainers", true,
                "Snap anything buildable that holds items. Matched on components, so "
                + "modded chests are covered without naming them.");

            SnapFences = config.Bind("Snapping", "SnapFences", true,
                "Snap the fences and stake walls listed in FencePrefabs.");

            SnapUnsnappedPieces = config.Bind("Snapping", "SnapUnsnappedPieces", false,
                "Snap every buildable piece the game never gave snap points to. Catches "
                + "modded pieces for free, but also chairs, banners and item stands, where "
                + "snapping tends to fight you rather than help.");

            FencePrefabs = config.Bind("Snapping", "FencePrefabs", DefaultFences,
                "Comma-separated prefab names treated as fences. Names that do not exist "
                + "are reported in the log at startup.");

            ExcludePrefabs = config.Bind("Snapping", "ExcludePrefabs", "",
                "Comma-separated prefab names to leave alone, whatever else matches.");

            Gap = config.Bind("Snapping", "Gap", 0f,
                "Metres of space left between two chained pieces. 0 places them flush. "
                + "Negative values are ignored - overlapping pieces just clip.");

            // A fence follows the ground; a chest does not. Corners give a fence two
            // heights to attach at, its base and a full panel up, and neither is any use
            // for running a line up a hill - so a fence gets a ladder of points up each
            // end instead. The idea is MSchmoecker's FenceSnap, which hand-places seven
            // rungs 0.2m apart on wood_fence; this derives them from the footprint so it
            // works on a piece nobody has measured.
            FenceLadderStep = config.Bind("Snapping", "FenceLadderStep", 0.2f,
                "Vertical spacing of the snap points up each end of a fence, in metres. "
                + "Smaller follows sloping ground more closely and costs more points per "
                + "piece. 0 turns the ladder off and gives fences plain corners like a "
                + "chest.");

            FenceLadderBelow = config.Bind("Snapping", "FenceLadderBelow", 0.2f,
                "How far below its own base a fence's lowest rung sits, in metres. This "
                + "is what lets the next panel step down rather than only up.");

            Verbose = config.Bind("Diagnostics", "Verbose", false,
                "Log the measured footprint of every piece that gets snap points, and the "
                + "colliders it was measured from.");
        }

        // ------------------------------------------------------------------ lookups

        private static HashSet<string> _fences;
        private static HashSet<string> _excluded;

        public static bool IsFence(string prefabName)
        {
            if (_fences == null) _fences = Split(FencePrefabs.Value);
            return _fences.Contains(prefabName);
        }

        public static bool IsExcluded(string prefabName)
        {
            if (_excluded == null) _excluded = Split(ExcludePrefabs.Value);
            return _excluded.Count > 0 && _excluded.Contains(prefabName);
        }

        /// <summary>Configured fence names, so startup can report the ones that miss.</summary>
        public static IEnumerable<string> ConfiguredFences()
        {
            if (_fences == null) _fences = Split(FencePrefabs.Value);
            return _fences;
        }

        private static HashSet<string> Split(string value)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(value)) return set;

            foreach (var entry in value.Split(','))
            {
                var name = entry.Trim();
                if (name.Length > 0) set.Add(name);
            }

            return set;
        }
    }
}
