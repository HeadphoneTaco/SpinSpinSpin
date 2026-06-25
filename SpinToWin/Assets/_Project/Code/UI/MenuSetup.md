# Menu Setup Guide

How to wire up the Main Menu and the Settings **overlay** in the Unity editor. Settings is an
**in-scene overlay** now — not its own scene (`Settings.unity` is retired) and not a `PanelButton`
toggle. It's still a real game state (`Settings`), but that state no longer loads a scene; instead a
`SettingsOverlay` component shows/hides the panel when the state enters/exits. The menu's Settings
button is a normal `GameStateButton` (`Action = Settings`), exactly like Play. The scripts only
handle logic; you build the visuals and assign references here.

Why a state instead of a plain `PanelButton` show/hide: opening Settings *exits* the previous state
(MainMenu or Paused), so gameplay stays frozen via the existing `IsPlaying` gating **and Esc no
longer fires the resume countdown while Settings is open**. Closing returns to whichever state opened
it — MainMenu from the title screen, Paused from the pause menu — with no per-instance wiring.

The UI is **modular** — small single-purpose components, each doing one job (SOLID):

- **`GameStateButton`** (`Code/UI/`) — a Button that asks `GameManager` for one transition
  (Play, Settings, CloseSettings, MainMenu/Back, Pause, Resume, Quit). Every menu/nav button uses
  this. For states that map to a scene in `GameTransitions` the bubble transition + scene load happen
  automatically; `Settings` / `CloseSettings` don't load a scene, they just flip the overlay. One
  button = one component = one action.
- **`SettingsOverlay`** (`Code/UI/`) — goes on the settings overlay prefab root. Listens to the
  StateEntered / StateExited events and shows/hides its panel when the `Settings` state enters/exits.
  Also closes on Esc / gamepad B. This is the component that makes the overlay state-driven.
- **`ScreenTransition`** + a **`TransitionEffect`** (`Code/UI/Transitions/`) — the persistent
  full-screen bubble overlay that covers each screen change. One per game; see the Transitions
  section of `Code/Core/StateMachineSetup.md`. (The camera no longer moves.)
- **`MusicVolumeControl` / `SfxVolumeControl`** (`Code/UI/Settings/`) — go on the slider objects in
  the overlay; each binds its slider (and an optional % label) to a volume.
- **`HighContrastControl`** (`Code/UI/Settings/`) — goes on the toggle; binds it to high-contrast.
- The controls talk to the managers through an `ISetting<T>` abstraction (`Code/Settings/`), not
  to `AudioManager`/`AccessibilityManager` directly — so a control doesn't care who owns the value.
- **`PanelButton`** (`Code/UI/Panels/`) — still around for genuine in-scene sub-panels behind the
  transition. The Settings overlay no longer uses it (it's state-driven via `SettingsOverlay`).

To add a new slider-backed setting later, you write a ~3-line subclass of `SliderSettingControl`
and a matching `ISetting<float>` on the manager — no existing file changes.

## 1. Create the Canvas

1. In the menu scene, right-click in the Hierarchy → **UI → Canvas**.
   (This also creates an **EventSystem** — required for clicks/sliders. Don't delete it.)
2. Select the Canvas → set **Canvas Scaler → UI Scale Mode** to **Scale With Screen Size**
   (reference resolution e.g. `1920 x 1080`) so the menu scales across resolutions.

## 2. Main Menu (`Start` scene)

1. Right-click the Canvas → **Create Empty**, rename it `MainMenu`.
2. Add three buttons under it (**UI → Button - TextMeshPro**), label them `Play`,
   `Settings`, `Quit`. Arrange them with a **Vertical Layout Group** on `MainMenu` if you
   want automatic spacing.
3. Add a **`GameStateButton`** to each (select the button → **Add Component → Game State Button**;
   the `Button` reference auto-fills) and set its `Action`:
   - `Play` button → **Play**
   - `Settings` button → **Settings**
   - `Quit` button → **Quit**

That's the whole menu — no controller object, each button carries its own one-line intent.

## 3. Settings overlay — one shared prefab (used by main menu *and* pause)

There is **one** settings menu, built once as a **`SettingsOverlay` prefab** and dropped into both the
`Start` scene (opened from the main menu) and the `Main` scene (opened from pause). It's an in-scene
overlay driven by the `Settings` state — pause can't load a Settings *scene* or it would unload `Main`
and end the run. The old `Settings.unity` is retired (delete it / remove from Build Settings).

### 3a. Make the prefab by extracting the existing `Settings.unity` UI

You already built the whole settings UI (sliders, labels, toggle, art) in `Settings.unity` — reuse it
rather than rebuilding. Dragging an object to the Project window makes a prefab with **all its wired
references intact**, so this loses nothing:

1. Open **`Settings.unity`**. In the Hierarchy, find the root that holds the settings UI. From the
   scene it's the **`Canvas`** with `Background`, `MusicVolumeControl`, `SfxVolumeControl`,
   `AccessibilityToggle`, and `Settings_Menu` under it.
2. You want the *panel*, not the whole Canvas, to become the prefab (each target scene already has its
   own Canvas). Build a **two-level** structure — this matters, see the warning below:
   - Right-click the Canvas → **Create Empty Child**, name it **`SettingsOverlay`** (this is the
     always-active **root** that carries the component). Stretch/fill it (anchors min `0,0`
     max `1,1`, offsets `0`).
   - Right-click `SettingsOverlay` → **Create Empty Child**, name it **`Panel`**, also stretch/fill.
     This is the part that gets shown/hidden.
   - Drag `Background`, the two controls, `AccessibilityToggle`, and `Settings_Menu` **onto `Panel`**
     so they're *its* children. (Reparenting under the same Canvas keeps their layout.)

   > ⚠️ **Don't put the visible UI directly on `SettingsOverlay` and toggle that.** The component
   > below subscribes to events in `OnEnable`, which **never runs while its GameObject is inactive**.
   > If you deactivate the object the component lives on, it can never hear "Settings entered" to turn
   > itself back on — you'd open Settings and see nothing, with no way to Esc out. The root stays
   > **active**; only its child `Panel` toggles.
3. Add the **`SettingsOverlay`** component to the **`SettingsOverlay` root** (**Add Component →
   Settings Overlay**). Assign:
   - **Panel** → the child **`Panel`** object (NOT the root the component is on).
   - **State Entered** → `ScriptableObjects/Events/StateEntered.asset`
   - **State Exited**  → `ScriptableObjects/Events/StateExited.asset`
   - **Close On Cancel** → leave **on** (Esc / gamepad B closes the overlay).
4. The scene already has a back button (the **`MainMenu`** object, currently `GameStateButton`,
   `Action = MainMenu`). Re-point it to **`Action = CloseSettings`** so it returns to the caller
   (pause *or* menu) instead of always jumping to the main menu — see §3c. Make sure it's a child of
   **`Panel`** too (so it hides with the rest).
5. Drag **`SettingsOverlay`** from the Hierarchy into your **`Prefabs/UI`** folder → it becomes a
   prefab asset. Delete the leftover `Settings.unity` afterward.

> Prefer rebuilding from scratch? Same two-level result: under a Canvas make an active
> `SettingsOverlay` root with the component (step 3), and a child `Panel` holding `Background` art,
> two **Sliders** (Min 0, Max 1, Whole Numbers off) with `MusicVolumeControl` / `SfxVolumeControl`, a
> **Toggle** with `HighContrastControl`, and the exit button.

### 3b. Place an instance in each scene

Drag the `SettingsOverlay` prefab into the **`Start`** Canvas and the **`Main`** Canvas. Leave the
**`SettingsOverlay` root ACTIVE**, and set its child **`Panel` inactive** by default — the component
must stay enabled to listen for the `Settings` state, and it shows the `Panel` when that state enters.
(The component also forces the right starting visibility in `OnEnable`, so as long as the root is
active you're covered.) No other per-scene wiring: the event assets are shared, and the controls reach
`GameManager.Instance.Audio` / `.Accessibility` (persistent from `Start`).

### 3c. The exit button (closes the overlay)

Because closing returns to whichever state opened it, **both instances use the same component** — no
per-instance override needed:

- **Close** — `GameStateButton`, `Action = CloseSettings`. Returns to MainMenu (from `Start`) or
  Paused (from `Main`) automatically, because `GameManager` remembers the state it opened from.

(Want the pause instance to bail *all the way* to the main menu instead of back to the pause screen?
Swap just that instance's button to `GameStateButton`, `Action = MainMenu`.)

### 3d. Open it

- Main menu `Settings` button → `GameStateButton`, `Action = Settings`.
- Pause `Settings` button → `GameStateButton`, `Action = Settings`.

Both are the same one-line component as every other nav button.

## 4. How it connects at runtime

- Every nav button is a `GameStateButton` → it calls a `GameManager` transition (e.g.
  `OpenSettings()` / `CloseSettings()` / `StartGame()` / `ReturnToMenu()`), which changes state.
- `GameTransitions` (on the persistent `[Managers]`) maps a state to its scene and plays the bubble
  transition: cover → load scene → reveal. `MainMenu → Start`, `Playing → Main`. **Settings maps to
  no scene**, so it doesn't route through here — the scene stays put and the overlay covers it.
- **Opening settings:** `OpenSettings()` records the current state (`MainMenu` or `Paused`) and enters
  `Settings`. The active scene's `SettingsOverlay` sees the `Settings` StateEntered event and shows
  its panel. **Closing:** `CloseSettings()` (the exit button or Esc) re-enters the recorded state;
  `SettingsOverlay` sees StateExited and hides. Returning to `Paused` re-shows the pause panel via
  `PauseMenuController`; returning to `MainMenu` just reveals the title screen underneath.
- `GameManager` lives on `[Managers]` in `Start` and persists across scenes, so the overlay's
  controls reach `GameManager.Instance.Audio` / `.Accessibility` with no setup of their own.
- **Volume sliders** → `MusicVolumeControl` / `SfxVolumeControl` read & write the volume settings,
  initialised to the current values when the panel opens.
- **Quit** → exits the build (and stops Play mode in the editor).

## 5. Quick test

1. Press **Play** from the `Start` scene.
2. Click **Settings** → the overlay appears (no scene load); drag the sliders — volumes update live.
   Click the exit button **or press Esc** → the overlay closes back to the menu.
3. Click **Play** — the Console logs `[State] entered Playing` (from `DebugStateControls`, if
   present) and the `Main` scene loads.

## 6. The settings components, in detail

- **Music/Sfx Volume Control** — each goes on its slider object in the overlay. Optional
  `Value Label` (`TMP_Text`) shows the volume as a live percentage (e.g. `80%`); leave empty to skip.
- **High Contrast Control** — goes on a UI Toggle. Drives `GameManager.Instance.Accessibility`
  and remembers the choice (PlayerPrefs). The visual swap layer itself isn't built yet (see §8).
- **Settings Overlay** — goes on the overlay prefab root. Shows/hides its `Panel` when the `Settings`
  state enters/exits (via the StateEntered/StateExited events) and closes on Esc / gamepad B.
- **Game State Button** — one `Action` (Play/Settings/CloseSettings/MainMenu/Pause/Resume/Quit) per
  button. For scene-backed states the scene change + bubble transition follow automatically;
  `Settings`/`CloseSettings` just flip the overlay.
- **Screen transition** — the bubble overlay is a single persistent object set up once (see the
  Transitions section of `Code/Core/StateMachineSetup.md`); every screen hop drives it via
  `ScreenTransition.Instance`.

## 7. Pause menu — **`Main` (gameplay) scene**

(Sections 1–6 build the main menu + the shared settings overlay in the **`Start`** scene. The pause
menu lives in the **`Main`** scene. See `Code/Core/StateMachineSetup.md` for the per-scene map.)

**What already works in code (no scripting needed):** `GameManager.Pause/Resume/TogglePause`, the
`Paused` state, and `PauseMenuController` (auto show/hide + **Esc**/gamepad **Start** toggle).
Gameplay genuinely **freezes** while paused — `GremlinRunner`, `RunDirector`, `ScrollingItem`, and
`TrackSpawner` all gate `Update` on `IsPlaying`, which is false in `Paused`. Music keeps playing by
design. **Resuming plays a 3-2-1 countdown:** the game stays frozen while the count runs, then
`PauseMenuController` calls `Resume()` to enter `Playing`. Every resume path (the button and Esc)
goes through it. (No Restart button — that was removed.)

This pause screen uses **the painted art** for its look, with **invisible buttons placed over the
painted ones** to catch the clicks. So you build: the art image, three transparent buttons, the
countdown number, and the controller wiring.

### 7.1 Hierarchy — a `PauseOverlay` root (matches Settings & Game Over)

For consistency, pause uses the same shape as the settings and game-over overlays: a single
**always-active overlay root** that holds the controller and toggles its child panel.

```
Canvas
└─ PauseOverlay         ← always active; PauseMenuController lives here
   ├─ PausePanel        ← the painted menu + buttons (toggled on/off by the controller)
   │  ├─ PauseImage     (painted art; top child so it draws BEHIND the buttons)
   │  ├─ Resume         (Button → PauseMenuController.resumeButton)
   │  ├─ Settings       (GameStateButton, Action = Settings)
   │  └─ QuitToMainMenu (GameStateButton, Action = MainMenu)
   └─ CountdownRoot     ← the 3-2-1 number (shown only during resume)
```

1. Put `PauseMenuController` on the **`PauseOverlay`** root — **never** on `PausePanel`. If the script
   sat on the panel it toggles, hiding the panel would disable the script and it could never reopen.
   The component now logs an error if Pause Panel is set to its own object.
2. `PauseImage` is the **top** child of `PausePanel` so it draws *behind* the transparent buttons; set
   its RectTransform anchors/pivot to **center**.
3. Keep `PausePanel` and `CountdownRoot` **inactive** by default — the controller shows them.

### 7.2 Transparent buttons over the painted ones

The painted art shows three buttons — **RESUME**, **SETTINGS**, **QUIT**. Rather than draw new button
graphics, lay an invisible `Button` over each painted one to catch the click:

1. Add a UI `Button` per painted button, sized and positioned over the art.
2. On each Button's `Image`: set **alpha = 0** (invisible) but keep **Raycast Target ON** so it still
   catches clicks, set the Button's **Transition = None**, and delete the child Text.
3. Wire them: Resume → `PauseMenuController.resumeButton` (it runs the 3-2-1 countdown, so it is *not* a
   GameStateButton); Settings → `GameStateButton` Action **Settings**; Quit → `GameStateButton` Action
   **MainMenu**.

### 7.3 Wire the controller

On `PauseMenuController` (on the `PauseOverlay` root) assign: **Pause Panel** (`PausePanel`), **Resume
Button**, **Main Menu Button** (optional), **Countdown Root** + **Countdown Text** + **Countdown
Seconds**, and the shared **State Entered / State Exited** `GameEventString` assets.

## 8. Game Over screen — **`Main` (gameplay) scene**

`GameOverScreen` (`Code/UI/GameOverScreen.cs`) is the third overlay and uses the **same pattern** as
Settings and Pause: the script sits on an always-active **`GameOverOverlay`** root and toggles child
panels. The twist is there are **two** panels — a **`WinPanel`** and a **`LosePanel`** — and it shows
whichever matches `RunDirector.Outcome`, then fills that panel's run stats.

### 8.1 Hierarchy

```
Canvas
└─ GameOverOverlay      ← always active; GameOverScreen lives here
   ├─ WinPanel          ← shown on a WIN (toggled by the controller)
   │  ├─ WinImage       (painted "YOU WIN" art)
   │  ├─ SocksAmount / DistanceAmount / TimeAmount   (TMP stat labels)
   │  ├─ PlayAgain      (GameStateButton, Action = Play)
   │  └─ QuitToMainMenu (GameStateButton, Action = MainMenu)
   └─ LosePanel         ← shown on a LOSS (its own copy of the above)
      ├─ LoseImage      (painted "CRASHED" art)
      ├─ SocksAmount / DistanceAmount / TimeAmount
      ├─ PlayAgain      (GameStateButton, Action = Play)
      └─ QuitToMainMenu (GameStateButton, Action = MainMenu)
```

Put `GameOverScreen` on the **`GameOverOverlay`** root (not on a panel — same footgun guard as the
others). Keep `WinPanel` and `LosePanel` **inactive** by default. The painted Win/Lose images carry the
"YOU WIN" / "CRASHED" text, so there is no separate title field — the panel art *is* the title.

### 8.2 Buttons (no extra wiring)

Same as everywhere: Play Again → `GameStateButton` Action **Play** (re-enters Playing, which reloads
`Main` and resets the run — Restart was removed, Play does that job from here); Quit → `GameStateButton`
Action **MainMenu**. Use the transparent-button-over-art trick from §7.2 if they are painted in.

### 8.3 Wire it up

On `GameOverScreen` assign: **Win Panel** + its **Socks / Distance / Time** TMP labels, **Lose Panel** +
its **Socks / Distance / Time** labels, and the shared **State Entered / State Exited** `GameEventString`
assets (the same two the pause and settings overlays use). The run already routes both win and loss
through the GameOver state with `RunDirector.Outcome` set before the transition, so the screen just
reads it.
