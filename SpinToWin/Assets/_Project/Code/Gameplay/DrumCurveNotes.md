# Drum Curve & Item Rotation

How socks/obstacles ride the drum and rotate so they look pinned to the wall by centripetal force.
Scripts: `DrumCurve.cs` (the `ApproachCurve` struct), `ScrollingItem.cs`, `TrackSpawner.cs`.

## The path (the "C")

Items don't fall straight — they trace a **C** inside the drum: enter high near the top, sweep around
the curved wall, and arrive at the gremlin at the bottom. The shape is an `ApproachCurve` (a struct on
the `TrackSpawner`), a circle of `radius` whose **bottom touches the player** at `(playerZ, groundY)`,
with `squish` flattening/exaggerating its height.

Items advance along it by **angle `phi`**, not by Z, because a C doubles back on itself in Z. `phi`
sweeps from `StartAngle` (= `sweepDegrees`, the entry, high up) down to `0` (arrival, at the player).
`PointAt(phi)` gives the world `(z, y)`; lane (X) is handled separately by the spawner so the player
can still steer.

## The rotation (pinned to the drum)

An item stuck to the inside of a spinning drum shares the drum's **angular position** — so "rotate
with the drum" literally means "rotate by `phi`." At each step the item pitches about world **X** by
`-phi`, which points its local **up** along the inward radial `(0, cos phi, -sin phi)` — toward the
drum axis. At `phi = 0` (the player) that's flat/upright, i.e. its normal authored pose; partway up
it leans back against the wall; at a full 180° sweep it's upside-down at the top.

The math lives in **one place** so the preview and the real thing can't drift:

- `ApproachCurve.RotationAt(phi)` → the orientation (used by items at runtime).
- `ApproachCurve.UpAt(phi)` → the up direction (used by the gizmo preview).

`ScrollingItem` applies `RotationAt(phi) * _baseRotation` each frame, where `_baseRotation` is the
prefab's **authored** rotation captured in `Awake` — so any facing the artist set is preserved; the
curve tilt just layers on top.

## Gotcha: it's runtime-only

The tilt runs in `ScrollingItem.Update()`, so **with the game paused/stopped the items sit flat** in
their authored pose. That's expected — press Play to see them hug the wall. (This looked like a bug at
first; it wasn't.)

To tune the look **without** playing, select the `TrackSpawner`: the Scene gizmo draws the yellow C
for each lane plus **green "up" ticks** fanning around it. Those ticks are the exact item orientation
(`UpAt`), so if they rotate smoothly from leaning-back at the top to straight-up at the bottom, it's
correct.

## Tuning dials

- **Sweep Degrees** (`TrackSpawner`) — how far around the drum items travel. Lower = items enter less
  inverted / gentler hook.
- **Squish** — vertical flatten of the C (1 = round, <1 = flatter, >1 = taller).
- **Radius** — bigger = wider sweep and more reaction time.
- **Align To Curve** (per item, on `Collectible` / `Obstacle`) — on by default; untick for an item
  that should stay flat (e.g. a coin-like sock).
- **Pivot matters:** items rotate around `Body`'s pivot. If a mesh's pivot is at its center it spins in
  place instead of looking pinned by its back to the wall — nudge the mesh on the prefab so the pivot
  sits at the face that should touch the drum.
