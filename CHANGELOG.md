# Changelog

Notable changes to Dovetail. Format follows [Keep a Changelog](https://keepachangelog.com),
and the mod uses [semantic versioning](https://semver.org).

## [0.9.0] — 2026-08-16

Not released yet. Everything below is written, deployed and confirmed loading, but the
snapping has only been loaded and not yet played. The version stays under 1.0 until it has,
and 1.0.0 will be the release.

### Snapping

- **Chests and fences line up.** Place one and the next snaps flush beside it or squarely on
  top, with no nudging and no gaps you find after the wall is built.
- Each piece's own footprint is measured at load and given a snap point on **all eight
  corners** of it.
- **Corners rather than face centres**, and the set is deliberately uniform. The game snaps
  by making the closest pair of points *coincide*, so a corner meeting a corner is flush
  adjacency and a corner meeting a face centre would put a piece half its own length out of
  line.
- `Gap` pushes the corners **outward** by half its value, because each of the two pieces
  contributes half the space between them. Insetting them, which is the intuitive reading,
  makes pieces overlap by exactly the same arithmetic.

### What gets snapped

- **Containers**, matched on components rather than names — anything with both a `Piece` and
  a `Container`. Modded chests are covered without a list to maintain. Ships are excluded.
- **Fences**, matched by name, because nothing about a fence's components distinguishes it
  from any other wall. The list is config rather than code, and any configured name matching
  no prefab is **reported in the log at startup** rather than silently doing nothing.
- **Everything the developers never gave snap points to**, off by default. That set is mostly
  chests, fences and loose decoration, but it also catches chairs, banners and item stands,
  where snapping fights you rather than helps.
- Pieces that already have snap points of their own are always left alone.

### Correctness

- Footprints are read from collider **data** rather than from `Collider.bounds`. Prefabs sit
  inactive in `ZNetScene`, where world-space bounds have never been computed and read as
  zero — exactly when they are wanted.
- Loads on dedicated servers.
- **Core is optional.** Installed, it is used: the mod joins Core's version gate, which
  compares mod versions and build ids on connect and refuses a client that disagrees. That
  matters here because this adds child transforms to shared prefabs. Absent, nothing is
  degraded and the mod runs standalone, so installing Dovetail no longer pulls Core in with
  it. A hard dependency would have been worse than no gate at all, since a missing hard
  dependency means the plugin never loads.

### Naming

Named for chests because that is where it started. It now covers fences and stake walls too,
and the name stays so existing configs keep working.
