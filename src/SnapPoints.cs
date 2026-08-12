using UnityEngine;

namespace Dovetail
{
    /// <summary>
    /// Gives every container piece snap points at the eight corners of its own footprint,
    /// so chests line up flush beside each other and stack cleanly on top.
    ///
    /// How the game snaps: Player.UpdatePlacementGhost calls FindClosestSnapPoints, which
    /// picks the globally closest pair of snap points - one on the ghost, one on a nearby
    /// placed piece - within 0.5m, then moves the ghost so the two coincide. Corners are
    /// used rather than face centres precisely because that rule is "make these two points
    /// the same point": a ghost's left corner landing on a placed chest's right corner is
    /// flush adjacency, and a ghost's bottom corner landing on a top corner is a stack.
    /// Mixing corners with face centres would let a chest snap half a chest out of line.
    ///
    /// A snap point is nothing but a child transform tagged "snappoint" - see
    /// Piece.GetSnapPoints, which is the whole of the game's side of this.
    /// </summary>
    internal static class SnapPoints
    {
        private const string Tag = "snappoint";

        private static bool _applied;

        /// <summary>Idempotent, and safe to call every frame until it takes.</summary>
        public static bool Apply()
        {
            if (_applied) return true;
            if (ZNetScene.instance == null) return false;

            var touched = 0;
            var skipped = 0;

            foreach (var prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab == null) continue;
                if (!Eligible(prefab)) continue;

                // Something already gave it snap points - vanilla, or another mod. Adding
                // a second set on top would fight whatever placement it already has.
                if (HasSnapPoints(prefab.transform)) { skipped++; continue; }

                // One awkward prefab - an odd collider, a missing mesh - should cost that
                // chest its snap points, not every chest after it in the list.
                try
                {
                    if (AddCorners(prefab)) touched++;
                }
                catch (System.Exception e)
                {
                    DovetailPlugin.Log.LogWarning(
                        "Could not snap " + prefab.name + ": " + e.Message);
                }
            }

            _applied = true;
            DovetailPlugin.Log.LogInfo(
                "Added corner snap points to " + touched + " piece(s); "
                + skipped + " already had their own.");

            ReportMissingFences();
            return true;
        }

        /// <summary>
        /// A configured fence name that matches no prefab does nothing at all, and does it
        /// silently - which is exactly how you spend an evening wondering why one fence
        /// still will not line up. Say so instead.
        /// </summary>
        private static void ReportMissingFences()
        {
            if (!DovetailConfig.SnapFences.Value) return;

            var missing = new System.Collections.Generic.List<string>();
            foreach (var name in DovetailConfig.ConfiguredFences())
                if (ZNetScene.instance.GetPrefab(name) == null) missing.Add(name);

            if (missing.Count == 0) return;

            DovetailPlugin.Log.LogWarning(
                "FencePrefabs names that match no prefab: " + string.Join(", ", missing.ToArray()));
        }

        /// <summary>
        /// Three ways in, in order of how confident we are that snapping is wanted.
        ///
        /// Containers are matched on components, not names - a modded chest is a Piece with
        /// a Container just as much as a vanilla one, so that covers them for free and does
        /// not rot when a prefab is renamed.
        ///
        /// Fences are not so lucky. Nothing distinguishes a fence from any other wall by
        /// component, so they need a name list; it lives in config rather than in code, and
        /// names that do not resolve are reported rather than silently skipped.
        ///
        /// The third way is the general one: any buildable piece the developers never gave
        /// snap points to. That set is mostly chests, fences and loose decoration, because
        /// walls, floors and beams all ship with their own. It is off by default because it
        /// also catches chairs and banners, where snapping is more nuisance than help.
        /// </summary>
        private static bool Eligible(GameObject prefab)
        {
            if (prefab.GetComponent<Piece>() == null) return false;

            // Ships carry cargo and are technically pieces. Snapping a longship to a chest
            // is not what anyone means by chaining storage.
            if (prefab.GetComponent<Ship>() != null) return false;

            if (DovetailConfig.IsExcluded(prefab.name)) return false;

            if (DovetailConfig.SnapContainers.Value && prefab.GetComponent<Container>() != null)
                return true;

            if (DovetailConfig.SnapFences.Value && DovetailConfig.IsFence(prefab.name))
                return true;

            return DovetailConfig.SnapUnsnappedPieces.Value;
        }

        private static bool HasSnapPoints(Transform root)
        {
            for (var i = 0; i < root.childCount; i++)
                if (root.GetChild(i).CompareTag(Tag)) return true;

            return false;
        }

        private static bool AddCorners(GameObject prefab)
        {
            if (!Footprint(prefab, out var bounds)) return false;

            // Snapping makes the two points the same point, so the gap between two chained
            // chests is twice however far the corners sit outside the box: chest A's right
            // point ends up on chest B's left point, and each contributed half the space.
            // Hence Gap/2, and hence pushing the corners out rather than pulling them in -
            // insetting them would make the chests overlap by the same arithmetic.
            var out2 = Mathf.Max(0f, DovetailConfig.Gap.Value) * 0.5f;
            var extents = bounds.extents + new Vector3(out2, out2, out2);

            foreach (var x in new[] { -1, 1 })
            foreach (var y in new[] { -1, 1 })
            foreach (var z in new[] { -1, 1 })
            {
                var corner = bounds.center + new Vector3(extents.x * x, extents.y * y, extents.z * z);

                // Named rather than numbered because the game prints this in the HUD when
                // you Tab through snap points manually.
                var name = (y < 0 ? "bottom" : "top") + "-"
                           + (z < 0 ? "back" : "front") + "-"
                           + (x < 0 ? "left" : "right");

                var point = new GameObject("snap_" + name);
                point.tag = Tag;
                point.transform.SetParent(prefab.transform, false);
                point.transform.localPosition = corner;
            }

            if (DovetailConfig.Verbose.Value)
                DovetailPlugin.Log.LogInfo(
                    prefab.name + ": footprint " + bounds.size.ToString("F2")
                    + " centred " + bounds.center.ToString("F2"));

            return true;
        }

        // ------------------------------------------------------------------ footprint

        /// <summary>
        /// The piece's own box, in its local space.
        ///
        /// Colliders are read as data (BoxCollider.center/size, mesh bounds) rather than
        /// through Collider.bounds, because prefabs sit inactive in ZNetScene and the
        /// world-space bounds of an inactive collider are not reliable. Transforms work
        /// on inactive objects, so converting the corners by hand is safe where asking
        /// Unity for a world AABB is not.
        /// </summary>
        private static bool Footprint(GameObject prefab, out Bounds bounds)
        {
            bounds = default;
            var found = false;

            foreach (var collider in prefab.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger) continue;
                if (!LocalBounds(collider, out var local)) continue;

                var box = ToRoot(prefab.transform, collider.transform, local);
                if (!found) { bounds = box; found = true; }
                else bounds.Encapsulate(box);
            }

            if (found) return true;

            // No usable collider: fall back to the mesh, which is also pure data.
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;

                var box = ToRoot(prefab.transform, filter.transform, filter.sharedMesh.bounds);
                if (!found) { bounds = box; found = true; }
                else bounds.Encapsulate(box);
            }

            return found;
        }

        private static bool LocalBounds(Collider collider, out Bounds local)
        {
            switch (collider)
            {
                case BoxCollider box:
                    local = new Bounds(box.center, box.size);
                    return true;

                case MeshCollider mesh when mesh.sharedMesh != null:
                    local = mesh.sharedMesh.bounds;
                    return true;

                case CapsuleCollider capsule:
                    var d = capsule.radius * 2f;
                    local = new Bounds(capsule.center, new Vector3(d, Mathf.Max(capsule.height, d), d));
                    return true;

                case SphereCollider sphere:
                    local = new Bounds(sphere.center, Vector3.one * sphere.radius * 2f);
                    return true;

                default:
                    local = default;
                    return false;
            }
        }

        /// <summary>
        /// Rewrites a child's local box into the root's space by carrying all eight corners
        /// across, so a rotated or scaled child still produces a box that contains it.
        /// </summary>
        private static Bounds ToRoot(Transform root, Transform child, Bounds local)
        {
            var centre = local.center;
            var extents = local.extents;

            var result = new Bounds(root.InverseTransformPoint(child.TransformPoint(centre)), Vector3.zero);

            for (var i = 0; i < 8; i++)
            {
                var corner = centre + new Vector3(
                    (i & 1) == 0 ? -extents.x : extents.x,
                    (i & 2) == 0 ? -extents.y : extents.y,
                    (i & 4) == 0 ? -extents.z : extents.z);

                result.Encapsulate(root.InverseTransformPoint(child.TransformPoint(corner)));
            }

            return result;
        }
    }
}
