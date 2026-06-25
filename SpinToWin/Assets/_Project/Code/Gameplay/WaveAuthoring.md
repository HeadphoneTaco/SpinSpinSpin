# Authoring Spawn Waves

A **wave** is a small, hand-painted grid of what flows down the drum toward the gremlin. Waves replace
random spawning with deliberate routing, let us teach mechanics in order, and give exact control over how
often rare socks show up. Anyone can make one — it's just text, no code.

## The big picture

- The track has **7 lanes**. A wave is a block of **rows**; each row is one **spawn line** that streams
  down the drum. The **top line arrives first** (farthest away), the bottom line last.
- Each row is a string of characters, **one character per lane, left → right**.
- The `TrackSpawner` plays one wave row per spawn tick. When a wave finishes, it starts the next one.
- **Two pools** feed it: **Intro Waves** play once, in order, at the start of a run (use these to teach —
  first paddle, first rare sock, etc.), then **Pool Waves** are pulled at random forever after.
- If no waves are assigned at all, the spawner quietly falls back to the old random mode — so the game is
  never empty while we're still building the wave set.

## The legend (what each character means)

```
.  or space   empty lane (nothing here)
o             common sock      (Sock Bucket)
u             uncommon sock    (Uncommon Sock Bucket)
r             rare sock        (Rare Sock Bucket)
x  or #       obstacle         (Obstacle Bucket)
p             paddle           (auto: shows each paddle variant in order, then random)
0-9           a SPECIFIC paddle by bucket index (0 = first paddle in the Paddle Bucket)
```

Letters are case-insensitive. Any character that isn't in the legend is treated as empty, so you can use
`.` for gaps to keep the grid readable. A **blank line** is a full row of empty — a nice breather between
clusters.

Paddles are full-width and must be **jumped**. Put a full-width paddle in the **middle column** (lane 4
of 7). If we add part-width paddles later, put their character in the lane(s) they should block.

## How to make a wave

1. In the Project window: **Create ▸ SpinToWin ▸ Spawn Wave**. Name it something readable
   (e.g. `Intro_01_FirstPaddle`, `Pool_ZigZag`).
2. In the Inspector, type your grid into the **Grid** box. Optionally jot a **Notes** line for the team.
3. Drop the asset into the folder our wave buckets watch (see below). That's it — no scene or code edits.

## How waves get into the game (buckets)

Waves are collected with the same **CoreUtils buckets** we use for socks/obstacles/paddles:

- **`SpawnWaveBucket`** — a bucket of wave assets. Create via **Create ▸ SpinToWin ▸ Spawn Wave Bucket**,
  then point its **Sources** at a folder of waves; it auto-collects every wave in that folder.
- We keep two on the `TrackSpawner`:
  - **Intro Waves** — played once, **in bucket order**. Sort this bucket to set the teaching order.
  - **Pool Waves** — pulled at random after the intro.

So the workflow for a teammate is: make a `Spawn Wave` asset, save it in the right folder, done. To add a
brand-new wave to the random rotation, just save it in the Pool folder.

## Rare socks — how often they appear

Rarity is **placement**, not a dice roll. A sock's tier comes from which character you paint:

- `o` pulls from the **Sock Bucket** (the everyday socks),
- `u` from the **Uncommon Sock Bucket**,
- `r` from the **Rare Sock Bucket** (keep this bucket small and special).

So "how often does a gold sock appear?" = "how many `r` cells we author across all waves." Put one `r` in
one-in-five pool waves and rares stay a treat. Within a tier, the bucket still picks a random sock of that
tier, so you get variety without losing control of rarity.

## Examples

A gentle intro wave — a lane of commons, then your first obstacle to dodge:

```
...o...
...o...
.......
..x.o..
```

Teach the paddle (full-width, middle column) with a reward sock for jumping it:

```
...o...
...p...
...o...
```

A routing pool wave — weave between obstacles, with one rare worth crossing for:

```
x.....x
.x...x.
..x.x..
...r...
..x.x..
```

## Tips

- Keep rows **7 characters wide** (one per lane). Shorter rows just leave the missing lanes empty; longer
  rows ignore the extras.
- Always leave a **clear path** unless you mean to force a jump (a full-width paddle row is the one time
  every lane is blocked).
- Lead hard clusters with an empty row or two so the player can read them coming.
- Test in isolation: temporarily point **Pool Waves** at a bucket with just your wave to watch it loop.
