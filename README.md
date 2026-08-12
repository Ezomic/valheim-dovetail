# Dovetail

Chests and fences that line up. Place one, and the next snaps flush beside it or squarely
on top — no nudging, no eyeballing, no gaps you only notice after you have built the wall.

Built against the installed game (0.221.12, Unity 6000.0.61, BepInEx 5.4.23.3, Harmony 2.9).
Single DLL, no asset bundle.

Named for chests because that is where it started; it now covers fences and stake walls
too. The name stays so existing configs keep working.

## How it works

A snap point in Valheim is nothing but a child transform tagged `snappoint` — that is the
entirety of the game's side of it, in `Piece.GetSnapPoints`. Chests and fences have none,
which is exactly why they are fiddly to line up, while walls and floors are not.

This mod measures each piece's own footprint at load and puts a snap point on all **eight
corners** of it.

Corners rather than face centres, because of how the game actually snaps.
`Player.UpdatePlacementGhost` calls `FindClosestSnapPoints`, which picks the globally
closest pair of points — one on the ghost, one on a nearby placed piece — within 0.5m, and
then moves the ghost so **the two points become the same point**. Under that rule:

- a ghost's left corner landing on a placed piece's right corner is flush adjacency
- a ghost's bottom corner landing on a top corner is a clean stack
- for a long thin fence, the same rule chains panels end to end

Mixing in face centres would let a corner snap to a centre and put a piece half its own
length out of line, so the set is deliberately uniform.

Because snapping makes the two points coincide, the `Gap` setting pushes the corners
*outward* by half its value — each of the two pieces contributes half the space between
them. Insetting the points, which is the intuitive reading, would make them overlap by
exactly the same arithmetic.

## What gets snapped

Three ways in, in descending order of confidence that snapping is wanted:

**Containers**, matched on components rather than names — anything with both a `Piece` and
a `Container`. Modded chests are covered without a list to maintain, and nothing rots when
a prefab is renamed. Ships are excluded; they hold cargo and are technically pieces, but
snapping a longship to a chest is not what anyone means by chaining storage.

**Fences**, matched by name, because nothing about a fence's components distinguishes it
from any other wall. The list is config rather than code, so a wrong or outdated entry is
something you fix without a build — and any configured name that matches no prefab is
**reported in the log at startup** rather than silently doing nothing.

**Everything else the developers never gave snap points to** (`SnapUnsnappedPieces`, off by
default). That set is mostly chests, fences and loose decoration, since walls, floors and
beams all ship with their own. It is off because it also catches chairs, banners and item
stands, where snapping tends to fight you rather than help.

Pieces that already have snap points of their own are always left alone.

Footprints are read from collider *data* (`BoxCollider.center`/`size`, mesh bounds) rather
than from `Collider.bounds`. Prefabs sit inactive in `ZNetScene`, and the world-space bounds
of an inactive collider are not reliable; transforms work regardless of active state, so the
corners are carried across by hand instead.

## Note on the existing mods

You already have **Frogger-ChestSnap** and **MSchmoecker-FenceSnap** in other profiles.
This replaces both. ChestSnap in particular dates from **September 2022** and works from a
hardcoded prefab list, so it predates several game and Unity versions.

Do not run this alongside either — it skips pieces that already have snap points, so
whichever registers first wins, which is a coin toss rather than a decision.

## Building

```bash
dotnet build
```

Deploys to the repo-local `testprofile\`. Override with `-p:ProfileDir=...`, or build it
into the shared play profile with `valheim-own-profile\build-all.ps1`.

## Config

`BepInEx\config\robbin.valheim.dovetail.cfg`

| Key | Default | What it does |
| --- | --- | --- |
| `SnapContainers` | `true` | Snap anything buildable that holds items |
| `SnapFences` | `true` | Snap the pieces named in `FencePrefabs` |
| `SnapUnsnappedPieces` | `false` | Snap every buildable piece with no snap points of its own |
| `FencePrefabs` | see below | Comma-separated prefab names treated as fences |
| `ExcludePrefabs` | | Comma-separated names to leave alone whatever else matches |
| `Gap` | `0` | Metres left between chained pieces; `0` is flush |
| `Verbose` | `false` | Log the measured footprint of every piece that gets points |

`FencePrefabs` defaults to `wood_fence, piece_sharpstakes, piece_stakewall_blackwood,
piece_dvergr_sharpstakes, piece_dvergr_stake_wall`.

A value already written to the `.cfg` beats a new default in code — change the `.cfg`, not
the source.

## What to check

1. Place a chest, then bring up a second — it should snap flush alongside, and stack when
   aimed above.
2. Place a wood fence and chain a second onto its end.
3. Same for sharp stakes.
4. **Read the startup log** for a `FencePrefabs names that match no prefab` warning — the
   default list is inferred from the asset manifest, so an entry may need correcting.
5. **Tab** cycles snap points manually; the HUD names them (`snap_top-front-left`), which
   is why they are named by position rather than numbered.
6. Set `Verbose = true` once and read the measured footprints if a piece snaps at the wrong
   distance.

## Author

Dovetail is an original mod by **Robbin Thijssen** (Thijssen Software).
Copyright (c) 2026 Robbin Thijssen. See `LICENSE`.
