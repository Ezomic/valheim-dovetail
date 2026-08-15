# Changelog

Notable changes to Dovetail. Format follows [Keep a Changelog](https://keepachangelog.com),
and the mod uses [semantic versioning](https://semver.org).

## [1.0.0] — 2026-08-16

First release.

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
- Loads on dedicated servers, and declares to Core's version gate.

### Naming

Named for chests because that is where it started. It now covers fences and stake walls too,
and the name stays so existing configs keep working.
