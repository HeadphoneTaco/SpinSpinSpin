# Gremlin Gameplay — Setup

How to wire the sock-gremlin runner in the **`Main`** scene and get a playable run. Companion to
`Code/Core/StateMachineSetup.md` (state/scenes/transitions) and `Code/UI/MenuSetup.md` (menus).

**The shape in one breath:** it's a treadmill. The gremlin never actually moves forward — it holds a
fixed Z and only steers left/right and hops. The *world* (socks + obstacles) scrolls toward it. That's
why your fixed drum camera can stay put. `RunDirector` sets the scroll speed; `TrackSpawner` spawns the
items; each item rides the belt and, when the gremlin touches it, either scores a sock or ends the run.

---

## The six scripts (all in `Code/Gameplay/`)

| Script            | Lives on                  | Job                                                                 |
|-------------------|---------------------------|--------------------------------------------------------------------|
| `RunDirector`     | one empty object in `Main`| Owns scroll **Speed** (ramps), **SockCount**, distance; `Crash()`.  |
| `TrackSpawner`    | one empty object in `Main`| Spawns socks/obstacles ahead at a fixed spacing; pools them.       |
| `GremlinRunner`   | the gremlin               | Analog steer + jump; input live only while **Playing**.            |
| `ScrollingItem`   | (base class — don't add)  | Moves an item toward the gremlin, despawns, detects the hit.       |
| `Collectible`     | the sock prefab           | On hit → `RunDirector.CollectSock` + sfx, then recycles.           |
| `Obstacle`        | each obstacle prefab      | On hit → `RunDirector.Crash()` → game over.                        |

`RunDirector`, `TrackSpawner`, and the items all check `GameManager.IsPlaying`, so the whole run
freezes the instant you pause and resumes cleanly — no time-scale tricks needed.

---

## 1. The drum floor (do this first or the gremlin falls forever)

`GremlinRunner` uses a `CharacterController`, which needs solid ground under it to register as
"grounded" (and therefore to jump). In `Main`, add a floor the gremlin can stand on:

- A plane / box at **Y = 0** spanning the drum width, with a **Collider** (Mesh or Box). No Rigidbody.
- Width should match `TrackSpawner.Half Width` × 2 (default drum half-width is **2.5**, so ~5 units wide).
- It can be invisible (disable the Renderer) — only the collider matters for the run.

> **Code / Colliders / Mesh split.** The scripts support the studio convention of separating logic,
> collision, and art onto child objects under a prefab root. Two house rules make it click:
> the gameplay script can live on a **`Code`** child, but the **`CharacterController` (gremlin)** and
> the **trigger collider + Rigidbody (items)** must sit where physics needs them (see below). The
> **`Mesh`** child is always free to stay separate — it just rides along under the moving root.

## 2. The gremlin

1. Create the gremlin as a **root** GameObject named **`Character_Gremlin`** at **(0, 0, 0)** facing
   **+Z** (into the drum), with `Code` / `Colliders` / `Mesh` children as you like.
2. Put the **`CharacterController`** on the **root**, not a child. It's both the collider *and* the
   mover, and Unity won't let those be split — whatever it's on is what physically moves, so it has to
   be the root for the `Mesh` child to follow. Size its capsule to wrap the gremlin; bottom near Y = 0.
3. Put **`GremlinRunner`** on the **`Code`** child (or the root — your call). It finds the
   `CharacterController` on itself, its parent, or a child automatically. Set:
   - *Steer Speed* `8`, *Half Width* `2.5` (match the floor + spawner), *Steer Smoothing* `12`.
   - *Jump Height* `2`, *Gravity* `-25`.
   - *State Entered* / *State Exited* → drag the **`StateEntered`** / **`StateExited`** assets from
     `ScriptableObjects/Events/` (same ones the rest of the game uses).
4. Controls are built in (no Input Action asset): **A/D or ←/→** steer, **Space / ↑ / gamepad A** jump.

> The CharacterController is the gremlin's collider, so you don't need a separate `Colliders` child or
> a Rigidbody on the gremlin — the item's kinematic Rigidbody is what makes the trigger fire.

## 3. Sock + obstacle prefabs (the trigger trick)

Each item is a prefab **root** with `Code` / `Colliders` / `Mesh` children. Trigger overlaps need a
**Rigidbody on the moving side**, so every item carries one:

**Sock prefab** (`Prefabs/Items/`):
- `Code` child → **`Collectible`** (set *Value* `1`, optional *Collect Sfx*). *Body* can stay empty —
  it auto-targets the prefab root.
- `Colliders` child → a **Collider with `Is Trigger` ✓** and a **Rigidbody** with **`Is Kinematic` ✓**.
  Nothing else — no script lives here.
- `Mesh` child → the sock art.

**Obstacle prefab(s)** (`Prefabs/Props/`): identical structure — **`Obstacle`** on `Code` (optional
*Crash Sfx*), and **trigger Collider + kinematic Rigidbody** on `Colliders`.

> **No trigger script on the item.** The gremlin scans its own capsule each frame and finds overlapping
> items itself, so the hit is detected without any script on the `Colliders` child — `Code` stays the
> only place a script lives. For tighter performance you can put items on an **Items layer** and set the
> gremlin's *Item Mask* to it; by default it scans Everything and filters to objects with a
> `ScrollingItem`.

## 4. RunDirector + TrackSpawner

1. Empty object **`RunDirector`** in `Main` → add **`RunDirector`**:
   - *Base Speed* `10`, *Max Speed* `30`, *Acceleration* `0.4`.
   - Leave *Sock Count Changed* / *Distance Changed* empty for now (those feed a HUD later — §6).
2. Make two **PrefabBuckets** (right-click → Create → *CoreUtils/Bucket/Prefab Bucket*): one for socks,
   one for obstacles. Point each at its prefab folder (or add prefabs by hand); every prefab in the
   bucket is spawned at random, which is how multiple sock types show up. **If a bucket is empty, that
   category never spawns.**
3. Empty object **`TrackSpawner`** in `Main` → add **`TrackSpawner`**:
   - *Sock Bucket* → your sock bucket; *Obstacle Bucket* → your obstacle bucket.
   - *Lane Count* `6` — items spawn in this many fixed lanes across the drum.
   - *Player Z* `0` (the gremlin's Z) and *Ground Y* → match the gremlin so items arrive right on it.
   - *Despawn Z* `-10`, *Half Width* `2.5` (**must match** the gremlin's *Half Width*).
   - *Radius* `10` and *Sweep Degrees* `180` define the **C** items ride down. Bigger radius = wider
     sweep and more reaction time; `180` = a full C, less = a gentler hook.
   - *Squish* `1` deforms the C's height to match the drum: below `1` = flatter, above `1` = taller /
     more exaggerated (use this to sit the C against Pedro's curved interior).
   - *Spawn Spacing* `8` (smaller = denser), *Obstacle Chance* `0.55`, *Sock Chance* `0.7`.

   The **Scene view draws each lane's C as a gizmo** (yellow = the C the item rides, cyan = the arrival
   line at the gremlin) so you can tune *Radius* / *Sweep Degrees* / *Lane Count* without pressing Play.
   Per row, one obstacle and one sock drop into two different lanes, so there's always a clear path.
   Reaction time ≈ arc length (*Radius* × sweep) ÷ speed, so grow *Radius* (or lower `RunDirector`'s
   *Max Speed* / *Acceleration*) if it feels rushed.

The spawner instantiates its own socks/obstacles at runtime, so **don't leave loose copies sitting in
the `Main` scene** — delete any, or you'll have a frozen sock parked at the origin. Nothing else
cross-references in the Inspector; `TrackSpawner` and the items find `RunDirector`/`GameManager` at runtime.

## 5. Test pass

1. Open **`Start`**, press **Play**, click **Play** → bubbles cover, `Main` loads, gremlin on the floor.
2. Socks and obstacles slide toward you and speed up the longer you survive.
3. **A/D / ←→** weave between them; **Space** to hop. Grab a sock → it pops and the count climbs
   (watch `RunDirector.SockCount` in the Inspector). Hit an obstacle → state flips to **GameOver**.
4. **Esc** pauses → the whole belt freezes; resume → it picks up exactly where it was.

## 6. On-screen HUD (`ScoreHud`)

1. On the gameplay Canvas in `Main`, add three **TMP Text** objects: a sock counter, a timer, and a
   centred outcome banner.
2. Add **`ScoreHud`** to the Canvas and drag those three texts into *Socks Text* / *Time Text* /
   *Outcome Text*. It polls `RunDirector` each frame - no event wiring.
3. It auto-adapts to the mode: the timer hides itself in CollectSocks, the sock counter shows
   `X / target` in CollectSocks (just `X` in SurviveTime), and the banner shows **YOU WIN!** /
   **CRASHED!** when the run ends.

## 7. Two playstyles + curved floor

**Switch playstyle on `RunDirector` → *Win Mode*:**
- **SurviveTime** - visible countdown; survive *Target Time* (default 60s) to win. Socks are score only.
- **CollectSocks** - timer hidden; collect *Target Socks* (default 25) to win. The hidden timer tightens
  spawn spacing from full toward *Min Spawn Spacing Scale* over *Spawn Ramp Time* (items arrive faster).

In both, hitting an obstacle loses the run. Win and loss both drop into the existing **GameOver** state;
the HUD banner says which. So tomorrow's two prototypes are one flip of the *Win Mode* dropdown.

**The C-curve (items only):** socks and obstacles trace a **C** — they enter high near the top, sweep
out and around the drum wall, and come down to the gremlin at the bottom (`ApproachCurve`, a semicircle
in the approach Z / height Y plane). Items advance along it by *angle*, so the path doubles back in Z
the way a C does. Tune `TrackSpawner` *Radius* (size of the C) and *Sweep Degrees* (`180` = full C).
Lane (X) is separate so the player steers across the C's bottom. The **gremlin moves flat** and items
arrive at *Ground Y*. Lower *Sweep Degrees* toward `90` for a gentler quarter-hook, or use *Squish* to
flatten / exaggerate the C's height to match the drum interior.

## Gotchas

- **Everything's frozen / nothing moves** → the run only runs in the **Playing** state. Press Play from
  **`Start`** and click your Play button; hitting Unity-Play with `Main` open sits in `MainMenu` and
  nothing moves. (No `[Managers]`/`GameManager` exists if you start in `Main` either.)
- **The gremlin's logic runs but the model doesn't move** → in a Code/Colliders/Mesh split, the
  **`CharacterController` must be on the moving root**, not a `Code`/`Colliders` child. A script only
  moves the object it's on; the `Mesh` follows only if it's under that moving root (§2).
- **Socks scroll but never get picked up (or obstacles never crash)** → the item has no collider on its
  `Colliders` child, the collider isn't **Is Trigger**, or the gremlin's *Item Mask* excludes the item's
  layer. The gremlin scans for items; it needs a trigger collider to find (§3).
- **Gremlin sinks through the floor / can't jump** → no floor collider at Y = 0 (§1), or the
  CharacterController capsule starts below the floor.
- **Socks pass through the gremlin** → the item is missing its kinematic **Rigidbody**, or its collider
  isn't **Is Trigger** (§3).
- **Items spawn off to the side / past the wall** → `Half Width` differs between gremlin, floor, and
  spawner. Keep all three equal.
- **Items sit below the floor (but still ding when collected)** → *Ground Y* on `TrackSpawner` is too
  low. They're inside the gremlin's scan capsule (so they register) but their pivots are under the
  floor. Raise *Ground Y* to the gremlin's body height (§4).
- **No socks or hazards appear** → the matching **PrefabBucket** is empty or unassigned on `TrackSpawner` (§4).
- **`Spinner.cs`** is the old Spin-to-Win test — delete it once this is in; it's unrelated.
