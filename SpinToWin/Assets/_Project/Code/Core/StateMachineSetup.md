# State Machine, Scenes, Camera & Transitions — Setup

How the whole flow fits together and **exactly which scene each piece goes in**. The game state
runs on the **CoreUtils StateMachine** and broadcasts changes through **GameEvents**
(ScriptableObject assets). Three scenes, one per "screen":

- **`Start`** — the laundromat title / main menu.
- **`Settings`** — the detergent-bottle settings screen.
- **`Main`** — inside the washing machine: the sock-gremlin gameplay.

All three must be in **Build Settings** (`Start` = 0, then `Settings` and `Main`). **Always press
Play from the `Start` scene** — that's where the persistent managers live (see Persistence below).

Each screen is its own scene; you move between them by changing game state (the button does that),
and `GameTransitions` loads the matching scene behind the bubble transition. State → scene:
`MainMenu → Start`, `Settings → Settings`, `Playing → Main`. `Paused` / `GameOver` are in-scene
overlays (no scene load).

---

## TL;DR — what goes in each scene

**`Start` scene (laundromat / menus):**

- `[Managers]` (root) — `GameManager`, `StateMachine` (+ 5 state children), `GameStateEventsBridge`,
  `GameTransitions`, `StateAudio`, and optionally `DebugStateControls`.
- Camera — **one fixed camera** framing the laundromat. No movement between screens, no
  `CameraDirector`; screen changes are covered by the bubble transition instead (see below).
- `[UI]` — main-menu Canvas with `GameStateButton`s (Play → Playing, Settings → Settings,
  Quit), and the `ScreenTransition` object (the bubble overlay). See `Code/UI/MenuSetup.md`.
- Laundromat geometry.

**`Settings` scene (detergent-bottle settings):**

- Camera — one fixed camera framing the bottle.
- `[UI]` — the settings Canvas: the self-binding setting controls (Music/Sfx/HighContrast) on
  Mina's bottle UI, plus a Back button (`GameStateButton` → MainMenu). No `[Managers]` here — they
  persist from `Start`, so the controls still reach `GameManager.Instance`.

**`Main` scene (inside the machine / gameplay):**

- Camera — one fixed camera framing the drum interior.
- `[UI]` — pause Canvas (`PauseMenuController`).
- Gremlin gameplay (coming later).

**Do NOT put a second copy in `Main`:** `[Managers]`, `StateAudio`, `GameTransitions`, and
`ScreenTransition` all persist from `Start` automatically. Duplicates would fight the originals
(`ScreenTransition` and `GameManager` even self-destroy duplicates).

---

## 1. `[Managers]` object — `Start` scene only

```
[Managers]                         (root — persists into Main via DontDestroyOnLoad)
├── (component) GameManager
├── (component) StateMachine        Default State = the MainMenu child below
├── (component) GameStateEventsBridge
├── (component) GameTransitions
├── (component) StateAudio
├── (component) DebugStateControls  (optional, dev only)
├── MainMenu      (empty child GameObject)
├── Settings      (empty child GameObject)
├── Playing       (empty child GameObject)
├── Paused        (empty child GameObject)
└── GameOver      (empty child GameObject)
```

- The five state children are **empty logical markers**. Name them **exactly** `MainMenu`,
  `Settings`, `Playing`, `Paused`, `GameOver` (must match `GameStateNames`).
- `StateMachine` → **Default State** = the `MainMenu` child.
- `GameManager` auto-finds the `StateMachine`; `AudioManager` is added at runtime (don't place it).

## 2. Event references — drag the assets from `ScriptableObjects/Events/`

| Component             | Scene        | Field(s)                              | Asset(s)                       |
|-----------------------|--------------|---------------------------------------|--------------------------------|
| GameStateEventsBridge | Start        | State Entered / State Exited          | StateEntered / StateExited     |
| GameTransitions       | Start        | State Entered                         | StateEntered                   |
| StateAudio            | Start        | State Entered                         | StateEntered                   |
| DebugStateControls    | Start        | State Entered                         | StateEntered                   |
| Spinner (optional)    | —            | State Entered / State Exited          | StateEntered / StateExited     |

> The old `CameraDirector` and `PanelViewSignal` (which drove a moving camera via
> `SettingsViewActive`) are gone — the camera is fixed and screen changes use the bubble
> transition. The `SettingsViewActive.asset` is now unused and can be deleted.

The event assets are shared ScriptableObjects, so components in both `Start` and `Main` (e.g. each
scene's `GameTransitions`/`StateAudio` listeners) reference the *same* `StateEntered` asset.

## 3. Camera — one fixed shot per scene

No `CameraDirector`, no Cinemachine blending, no dolly. Each scene has **one camera** that never
moves; the screen changes (Start → Settings → Main) are hidden by the bubble transition, not by
camera movement.

- **`Start`** — point the camera at the laundromat title and leave it.
- **`Settings`** — point the camera at the detergent bottle and leave it.
- **`Main`** — point the camera at the drum interior and leave it.

A plain `Camera` is enough. (You can keep a single `CinemachineCamera` + Brain if you prefer that
workflow, but nothing switches between framings anymore.)

## 4. Transitions (bubble cover → swap → reveal)

The transition is a full-screen overlay that **covers** the screen, lets the swap happen while
hidden, then **reveals**. The look is Mina's bubble animation; the mechanism is two scripts.

- `GameTransitions` lives on `[Managers]` (`Start`). It listens to `StateEntered` and, on a
  scene change, calls `Cover → load → Reveal`.
- **`ScreenTransition`** is its own object (author it under `Start`'s `[UI]` for tidiness — it
  detaches to the root and persists at runtime; duplicates self-destruct). Build it as a
  **full-screen Canvas** (high Sort Order) + a **`TransitionEffect`** component:
  - **`CanvasGroupFadeEffect`** (+ a black Image + CanvasGroup) — the working placeholder.
  - **`AnimatorTransitionEffect`** (+ an Animator + CanvasGroup) — swap to this when Mina's
    bubble animation lands: drop her clips into the `Cover` / `Reveal` Animator states and set the
    clip durations on the component.
  Assign the effect to `ScreenTransition.Effect` (or just leave it as a child — it's auto-found).
- It's found automatically via `ScreenTransition.Instance`, so nothing wires to `GameTransitions`.
- **Every screen hop uses it.** A button changes state (`GameStateButton`); `GameTransitions` maps
  the new state to its scene and plays cover → load → reveal. Start → Settings → back, Start →
  Main, all the same path.
- **Play flow:** click Play → `Playing` → bubbles cover → load `Main` → bubbles reveal on the drum.
  Settings and Back reverse through `Start` the same way.

## 5. Audio — `[Managers]` (`Start`), persists

On `StateAudio` assign `Menu Music`, `Gameplay Music`, `Game Over Sfx`, and `StateEntered`.
Because `AudioManager` persists, tracks swap cleanly across the scene change — no `StateAudio`
needed in `Main`.

## 6. Test pass

1. Open **`Start`**, press **Play** → `MainMenu`: laundromat shot, menu music.
2. Click **Settings** → bubbles sweep, the `Settings` scene loads; click **Back** → bubbles back to
   `Start`.
3. Click **Play** (or press `1`) → bubbles cover, load `Main`, bubbles reveal on the drum; gameplay
   music starts.
4. `2` pause, `3` game over, `0` back to menu. Console logs `[State] entered <Name>` each change.

## Gotchas

- **Play from `Start`.** `Main` has no `[Managers]`, so playing it directly won't have a
  GameManager. (If you want to test `Main` alone later, we can add a tiny bootstrap.)
- The camera is fixed per scene — no Brain/CameraDirector needed. One `ScreenTransition` persists
  across both scenes and covers every screen change.
- `Spinner` is a leftover Spin-to-Win input/audio test — not the gremlin gameplay. Keep or remove.

## Adding a new state

1. Add a child GameObject under the StateMachine, named for the state.
2. Add its name to `GameStateNames`.
3. React to it by switching on the name in a `StateEntered` handler.
