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
corners** of it. Fences are the exception and get a **ladder** of points up each end, for
reasons that come down to sloping ground; see below.

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

## Fences get a ladder instead

Eight corners give a fence exactly two heights to attach at: its base, and a full panel
up. Neither is any use for running a fence up a hill, which is most of what you do with a
fence. So a fence gets points up **both ends, at mid-depth, every `FenceLadderStep`
metres**, starting `FenceLadderBelow` metres under its own base so the next panel can step
down as well as up.

This idea is **MSchmoecker's**, from FenceSnap. It hand-places seven rungs 0.2m apart on
`wood_fence` plus one below the base. The difference here is that the rungs are derived
from the measured footprint rather than typed in, so a modded fence that nobody has
measured gets the same treatment from one config entry.

It does mean two different point layouts exist, which the uniform corner set was meant to
avoid: a fence rung and a chest corner can pair up and put the two half a piece out of
line. That is contained by fences being opt-in by name, and it is the same trade FenceSnap
made. Sloping ground is worth more than that edge. Set `FenceLadderStep = 0` to go back to
plain corners.

## What gets snapped

First, a precondition: **the piece has to be something you can actually build**
(`BuildablePiecesOnly`, on). Having a `Piece` component is not the same as being buildable,
and the gap is not small. Matching on components alone gave snap points to 35 prefabs out
of 46 that you can never place: 24 `TreasureChest_*` variants, nine pots, two loose loot
chests. The cost was not the wasted transforms. Snap points work both ways, so a chest
carried into a crypt snapped itself to the loot chests already standing there, and a
barrow's pottery became a snap target for a wall.

The set is read off the game's own piece tables rather than guessed at. Every buildable
piece sits in the `PieceTable` of some tool, via `ItemDrop.m_itemData.m_shared.m_buildPieces`,
so the Hammer, Hoe, Cultivator and any modded tool with its own table all contribute and
nothing needs naming.

Then, three ways in, in descending order of confidence that snapping is wanted:

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

The footprint is also measured from **only the geometry that will be standing there**. A
built piece carries its damage states and its destruction chunks inside the same prefab.
`WearNTear` holds `m_new`, `m_worn` and `m_broken` as separate subtrees, plus
`m_fragmentRoots`, and asking for every collider in the prefab returns all of them at
once. Measured that way `wood_fence` came out **2.72 × 2.30 × 0.85**, which is 0.7m wider
and 0.8m taller than the panel you actually place, and two chained fences would have stood
**0.72m apart**. So the search starts at `WearNTear.m_new` when there is one, skips
subtrees switched off via `activeSelf`, and skips colliders hanging off their own rigidbody
the way `WearNTear.SetupColliders` does. `Verbose` now names every collider it measured
from, which is how you find the culprit if a piece still snaps at the wrong distance.

## When derivation gets it wrong

One axis-aligned box cannot describe an L-shape or a piece whose geometry sits off centre,
and no amount of care will change that. `piece_dvergr_sharpstakes` measures
2.40 × 1.70 × 3.94 centred 0.35 off in x, so the corners of its box are nowhere near the
actual stakes. Both mods that came before this one ended up needing per-prefab data for the
same reason: FenceSnap hand-places its gate points, and ChestSnap moved to a YAML file of
them.

So `PointOverrides` takes exact points for named prefabs:

```
PointOverrides = piece_dvergr_sharpstakes: -0.5,0,2 | -0.5,0,-2 ; wooden_fence_1_gate: -2.4,0,0 | -2.4,1.17,0
```

Semicolons separate prefabs, a colon follows the name, pipes separate points, commas
separate one point's three coordinates. **Decimals must use a dot**, since a comma already
means something here. Naming a prefab is enough to get it snapped, so it does not also have
to be a container or a listed fence, and it skips the buildable filter as well: an explicit
name is more specific than any heuristic. `ExcludePrefabs` still wins, or it would not be an
escape hatch you could get back out of. `Gap` and the ladder do not apply, because a point
given by hand is used exactly as written. Names matching no prefab are reported at startup
like the fence list.

## Pieces the game can never find

`Piece.GetSnapPoints` finds neighbours with
`Physics.OverlapSphereNonAlloc(..., s_pieceRayMask)`, and that mask is
`LayerMask.GetMask("piece", "piece_nonsolid")`. A modded piece whose colliders were left on
another layer is invisible to that search, so snap points on it can never be found by
anything, however correct they are.

Dovetail says so in the log and leaves it there. Both ChestSnap and FenceSnap carry a
`FixPiece` that rewrites every collider onto the piece layer, and that is a real fix, but it
changes what those colliders collide with. That is too large a side effect to apply silently
to somebody else's content, and the piece belongs to whoever shipped it.

## Credit where it is due

This mod is written from scratch and shares no code with anything else, but it does not
pretend to have invented the idea. Two mods came first and both are worth naming.

**FenceSnap**, by **MSchmoecker**. The ladder of points up each end of a fence is its idea,
and its hand-tuned numbers are also what caught a bug here: FenceSnap puts `wood_fence`
points at x = ±1.0, this mod's measured box said ±1.36, and one of those had to be wrong.
It was this one. Without a second opinion to compare against, that 0.72m gap would have
been found by building a fence.

**ChestSnap**, by **Frogger**. The original chest snapping mod, and the reason anyone knows
chests are worth snapping at all. Now at 0.1.1 and driven by a YAML file of snap point
data, so custom and modded containers are added by editing config rather than by waiting
for an update.

**Extra Snap Points Made Easy**, by **Searica**, is the broadest mod in this space at 2.0.5
and does far more than this one: manual snapping with keybinds to cycle points, grid
snapping, and points added by piece shape across beams, triangles, rectangles and roofs. If
you want the whole toolbox rather than chests and fences that line up, use it instead.

Do not run this alongside any of them. It skips pieces that already have snap points, so
whichever registers first wins, which is a coin toss rather than a decision.

## Building

```bash
dotnet build
```

Deploys to the repo-local `testprofile\`. Override with `-p:ProfileDir=...`, or build it
into the shared play profile with `valheim-own-profile\build-all.ps1`.


## Core is optional

Dovetail installs and runs on its own. [Core](https://github.com/Ezomic/valheim-core) is a
**soft** dependency: present, it is used; absent, nothing here is degraded. Installing
Dovetail from Thunderstore no longer installs Core with it.

What Core adds is the **version gate** — a handshake that compares mod versions and build
ids on connect and refuses a client that does not match. Without it nothing reports two ends running different builds, and this adds child transforms to shared prefabs — so a disagreement about a prefab passes unnoticed.

Solo, none of that applies and Core is not needed at all.

## Config

`BepInEx\config\ezomic.valheim.dovetail.cfg`

| Key | Default | What it does |
| --- | --- | --- |
| `SnapContainers` | `true` | Snap anything buildable that holds items |
| `SnapFences` | `true` | Snap the pieces named in `FencePrefabs` |
| `SnapUnsnappedPieces` | `false` | Snap every buildable piece with no snap points of its own |
| `BuildablePiecesOnly` | `true` | Only snap pieces that appear in a build menu |
| `PointOverrides` | | Exact points for named prefabs, replacing anything derived |
| `FencePrefabs` | see below | Comma-separated prefab names treated as fences |
| `ExcludePrefabs` | | Comma-separated names to leave alone whatever else matches |
| `Gap` | `0` | Metres left between chained pieces; `0` is flush |
| `FenceLadderStep` | `0.2` | Vertical spacing of a fence's rungs; `0` gives fences plain corners |
| `FenceLadderBelow` | `0.2` | How far under its own base a fence's lowest rung sits |
| `Verbose` | `false` | Log the measured footprint of every piece that gets points, and the colliders behind it |

`FencePrefabs` defaults to `wood_fence, piece_sharpstakes, piece_stakewall_blackwood,
piece_dvergr_sharpstakes, piece_dvergr_stake_wall`.

A value already written to the `.cfg` beats a new default in code — change the `.cfg`, not
the source.

## What to check

1. Place a chest, then bring up a second — it should snap flush alongside, and stack when
   aimed above.
2. Place a wood fence and chain a second onto its end. Then chain one **up a slope**, which
   is what the ladder is for.
3. Same for sharp stakes.
4. **Check the measured fence in the log.** With `Verbose = true`, `wood_fence` should now
   report a footprint close to 2.0m wide rather than the 2.72m it reported before, which
   would have left a 0.72m gap between panels. If it still says 2.72, the inflation is not
   coming from the damage states and the collider lines underneath it will say what it is.
5. **Read the startup log** for a `FencePrefabs names that match no prefab` warning — the
   default list is inferred from the asset manifest, so an entry may need correcting.
6. **Tab** cycles snap points manually; the HUD names them, which is why they are named by
   position (`snap_top-front-left`) and a fence's rungs by height (`snap_left-y0.60`).
7. Set `Verbose = true` once and read the measured footprints if a piece snaps at the wrong
   distance. Each footprint is now followed by the colliders it was measured from.

## Author

Dovetail is an original mod by **Robbin Thijssen** (Thijssen Software).
Copyright (c) 2026 Robbin Thijssen. MIT licensed — see `LICENSE`.
