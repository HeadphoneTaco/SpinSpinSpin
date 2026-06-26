# Authoring Spawn Waves

A **wave** is a small, hand-painted grid of what flows down the drum toward the gremlin. Waves replace
random spawning with deliberate routing, let us teach mechanics in order, and give exact control over how
often rare socks show up. Anyone can make one — it's just text, no code.

## The big picture

- The track has **7 lanes**. A wave is a block of **rows**; each row is one **spawn line** that streams
  down the drum. The **top line of your text arrives first** (farthest away), the bottom line last.
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
p             paddle, auto     (shows each variant once in order, then random)
0-3           a SPECIFIC paddle variant (see Paddles & routing below)
```

Letters are case-insensitive. Any character that isn't in the legend is treated as empty, so you can use
`.` for gaps to keep the grid readable. A **blank line** is a full row of empty — a nice breather between
clusters.

## Paddles & routing

A paddle is a wall the gremlin has to **jump**. There are four variants, picked by the digit you paint —
they match the prefabs in `Prefabs/Paddles`:

- `0` **Basic** — low all the way across; jump in any lane.
- `1` **Center** — raised block in the middle; hop the side lanes (the centre is walled off).
- `2` **Left** — raised block on the left; hop the right lanes.
- `3` **Right** — raised block on the right; hop the left lanes.

`p` (lowercase) is the **auto** paddle — it shows each variant once in order, then goes random; handy in
an intro wave. Put the paddle character in the **middle column (lane 3, columns are numbered 0–6)** so the
full-width mesh centres correctly. The variant number — not where you type it — decides which lanes are
walls and which are jumpable.

In the preview a paddle row is the orange bar, marked per lane: **cyan lines = lanes you can hop**, **dark
shading = the wall** you have to route around. That's the route the paddle forces, so you can place socks
to reward the safe lane or bait a risky one. (Exact jumpable lanes per variant live in
`Code/Editor/SpawnWaveInspector.cs` → `JumpableLanesByPaddle`, a dev can tune them there.)

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

Teach the Left paddle (`2`) — its left side is walled, so the player routes right and hops; the sock on
the right rewards the safe lane:

```
.....o.
...2...
.....o.
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
- Only the **Basic** paddle walls every lane. **Center / Left / Right** leave open (jumpable) lanes — use
  them to craft the route, and watch the preview's cyan/dark to see which is which.
- Outside of a paddle, always leave a **clear path** — don't block every lane with obstacles.
- Lead hard clusters with an empty row or two so the player can read them coming.
- Test in isolation: temporarily point **Pool Waves** at a bucket with just your wave to watch it loop.

## Previewing a wave

With a Spawn Wave asset selected, the Inspector draws a live colour grid under the text, oriented like the
game: a blue **play-area** bar sits along the bottom (where the gremlin runs) and rows scroll down toward
it, so the bottom row is the one that arrives first. (You still paint top-down — the first line arrives
first — the preview just flips it to match the screen.) Sock tiers, obstacles and empties show as coloured
cells; a paddle shows as an orange bar with **cyan jump lines** on the lanes you can hop and **dark
shading** on the walled lanes. It uses the same parser the game does, so what you see is what spawns.
