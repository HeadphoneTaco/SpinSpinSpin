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

This pause screen uses **Mina's painted art** for its look, with **invisible buttons placed over the
painted ones** to catch the clicks. So you build: the art image, three transparent buttons, the
countdown number, and the controller wiring.

### 7.1 The art background

1. Under the `Main` Canvas, `PausePanel` holds `PauseImage` (Mina's art) as its first child. Set the
   art's RectTransform pivot/anchors to **center** (a stretch pivot offset it — that bug is fixed).
   Make sure `PauseImage` is the **top** child so it draws *behind* the buttons.
2. Keep `PausePanel` **inactive** by default (the controller shows it on `Paused`).

### 7.2 Transparent buttons over the painted ones

Mina's art paints three buttons — **PLAY**, **SETTINGS**, **QUIT**. Put one invisible UI button over
each. For every button:

1. **Image → Color alpha = 0**, but leave **Raycast Target ON** (an alpha-0 image still takes clicks).
2. **Button → Transition = None.** Otherwise the default Color-Tint transition repaints the image
   visible on hover/press. (This is *the* gotcha for invisible buttons.)
3. Delete/disable the button's child **Text (TMP)** — the label is in the art.
4. Position/size its RectTransform over the painted button. Tip: bump alpha to ~80 while placing,
   then back to 0.

Map and wire each:

| Painted button | Object       | How it acts                                                              |
|----------------|--------------|--------------------------------------------------------------------------|
| PLAY           | `Resume`     | Assign to **PauseMenuController → Resume Button** (triggers the countdown). |
| SETTINGS       | `Settings`   | `GameStateButton`, `Action = Settings` (the `SettingsOverlay` instance in `Main` shows itself). |
| QUIT           | `QuitToMenu` | `GameStateButton`, `Action = MainMenu` (returns to `Start` — NOT a desktop quit). |

> **Delete the old `Restart` button GameObject** under `PausePanel` — that feature was removed and
> the art has no Restart graphic.
>
> Resume is the one button NOT driven by a `GameStateButton`: it must run the countdown, so it's
> wired through `PauseMenuController.Resume Button` instead.

### 7.3 The resume countdown

1. Under the Canvas (above `PausePanel` so it draws on top, or its own child), make `CountdownRoot`
   — an empty holding a large **TextMeshPro** number centered on screen. Set `CountdownRoot`
   **inactive** by default.
2. On **PauseMenuController**, assign **Countdown Root** = `CountdownRoot`, **Countdown Text** = the
   TMP number, and leave **Countdown Seconds = 3** (set 0 to resume instantly).

When you resume, the controller hides the menu, shows `CountdownRoot`, counts 3→2→1 (gameplay still
frozen), then un-freezes. Want a "GO!" flash? Add it in `ResumeCountdown()` after the loop.

### 7.4 Wire the controller

Add **Pause Menu Controller** to a UI object and assign:

- `Pause Panel` → `PausePanel`
- `Resume Button` → the transparent `Resume` button (over PLAY)
- `Countdown Root` → `CountdownRoot`; `Countdown Text` → the TMP number
- `State Entered` → `ScriptableObjects/Events/StateEntered.asset`
- `State Exited`  → `ScriptableObjects/Events/StateExited.asset`
- `Main Menu Button` — optional; only if you wire QUIT through the controller instead of its own
  `GameStateButton`. The SETTINGS button carries its own `GameStateButton` (`Action = Settings`), so
  there's no settings field on the controller anymore.

### 7.5 Settings (shared overlay)

Pause reuses the **one `SettingsOverlay` prefab from §3** — there's no separate sub-panel. Place a
`SettingsOverlay` instance in `Main` (above `PausePanel`), set inactive. The §7.2 SETTINGS button
opens it via `GameStateButton` (`Action = Settings`): entering the `Settings` state exits `Paused`,
so `PauseMenuController` hides the pause panel and the overlay shows itself. The overlay's exit button
(`Action = CloseSettings`) — or **Esc** — returns to `Paused`, re-showing the pause panel. The sliders
and toggle reach the persistent managers, so they work mid-run with no extra wiring.

> Because settings opening exits `Paused`, **Esc while settings is open closes settings** (handled by
> `SettingsOverlay`) rather than triggering the resume countdown. Esc only resumes from the pause
> screen itself.

### 7.6 Test

1. **Play** from `Start` into `Main`.
2. **Esc** → run freezes, art + invisible buttons appear.
3. Click **PLAY** (Resume) or press **Esc** → menu hides, **3-2-1** counts down, *then* the run
   continues. Gameplay stays frozen for the whole count.
4. **SETTINGS** → pause panel hides, overlay opens; sliders change audio live; the exit button **or
   Esc** returns to the pause screen. (Esc here closes settings, it does *not* resume.)
5. **QUIT** → bubbles sweep back to the `Start` menu (app stays open).

> Needs an **EventSystem** in the scene for clicks/sliders (Unity adds one with the first Canvas).

## 8. High-contrast mode (plumbing only)

The on/off setting, persistence, and broadcast signal exist; the **visual swap layer is not
built yet** (pending the approach decision). To enable the broadcast:

1. Add an **Accessibility Manager** component to the `[Managers]` object and assign the
   `HighContrastChanged` event (`ScriptableObjects/Events/HighContrastChanged.asset`).
2. The `HighContrastControl` on the Settings toggle already flips it. Once you pick a visual
   approach, the color-swapping components subscribe to `HighContrastChanged`.

Use the **Accessibility ▸ WCAG Contrast Checker** window to verify any high-contrast palette
passes AA/AAA before wiring it.

## Notes

- Buttons use **TextMeshPro**; the first time you add one Unity may prompt to import TMP
  Essentials — accept it.
- If clicks do nothing, confirm an **EventSystem** exists in the scene.
