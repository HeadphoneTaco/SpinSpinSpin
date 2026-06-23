# Menu Setup Guide

How to wire up the Main Menu and the Settings **screen** in the Unity editor. Settings is its own
scene now (`Settings.unity`) — not a panel — so the menu's Settings button *navigates* to it, just
like Play navigates to the game. The scripts only handle logic; you build the visuals and assign
references here.

The UI is **modular** — small single-purpose components, each doing one job (SOLID):

- **`GameStateButton`** (`Code/UI/`) — a Button that asks `GameManager` for one transition
  (Play, Settings, MainMenu/Back, Pause, Resume, Quit). Every menu/nav button uses this; the scene
  change + bubble transition happen automatically because the new state maps to a scene in
  `GameTransitions`. One button = one component = one action.
- **`ScreenTransition`** + a **`TransitionEffect`** (`Code/UI/Transitions/`) — the persistent
  full-screen bubble overlay that covers each screen change. One per game; see the Transitions
  section of `Code/Core/StateMachineSetup.md`. (The camera no longer moves.)
- **`MusicVolumeControl` / `SfxVolumeControl`** (`Code/UI/Settings/`) — go on the slider objects in
  the Settings scene; each binds its slider (and an optional % label) to a volume.
- **`HighContrastControl`** (`Code/UI/Settings/`) — goes on the toggle; binds it to high-contrast.
- The controls talk to the managers through an `ISetting<T>` abstraction (`Code/Settings/`), not
  to `AudioManager`/`AccessibilityManager` directly — so a control doesn't care who owns the value.
- **`PanelButton`** (`Code/UI/Panels/`) — still around for genuine in-scene panels (e.g. a pause
  overlay): Show/Hide/Toggle a panel behind the transition. Not used for Settings anymore.

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

## 3. Settings panel — one shared overlay (used by main menu *and* pause)

There is **one** settings menu, built once as a **`SettingsPanel` prefab** and dropped into both the
`Start` scene (opened from the main menu) and the `Main` scene (opened from pause). It is an
**in-scene overlay**, not a separate scene — pause can't load a Settings *scene* or it would unload
`Main` and end the run. The old `Settings.unity` is retired (delete it / remove from Build Settings;
`GameManager.OpenSettings()` and the `Settings` state are just left unused — no harm).

**Build the prefab once:**

1. Under a Canvas, make `SettingsPanel` with Mina's settings art as the background image. Anchor the
   art **center** at the correct size — a *stretch* pivot offsets it (the same bug you hit on the
   pause art). Set `SettingsPanel` **inactive** by default.
2. Add the controls, positioned over the painted art (same transparent-over-art trick as the pause
   buttons wherever the art already draws the control):
   - Two **Sliders** (Min 0, Max 1, Whole Numbers off) → `Music Volume Control` / `Sfx Volume
     Control`. Optional `Value Label` (TMP) for the live %.
   - A **Toggle** → `High Contrast Control`.
   - A **bottom/exit button** (see below).
3. Drag `SettingsPanel` into your Prefabs folder, then place an instance in **both** `Start` and `Main`.

**The swappable bottom button.** Because the panel is an overlay, *closing* it reveals whatever
opened it. Choose per instance:

- **Back to caller** — `PanelButton`, `Action = Hide`, `Target Panel = SettingsPanel`. Hiding reveals
  the main menu (in `Start`) or the pause menu (in `Main`) underneath, no extra logic.
- **Straight to main menu** — if the pause instance's exit should bail all the way out, swap that one
  button to a `GameStateButton`, `Action = MainMenu`.

So "main menu vs back to pause" is simply *which component sits on that instance's bottom button* —
override it on the `Main` prefab instance if it should differ from `Start`.

**Open it:**

- Main menu `Settings` button → `PanelButton`, `Action = Show`, `Target Panel = SettingsPanel`
  (replaces the old `GameStateButton = Settings`).
- Pause `Settings` button → `PanelButton`, `Action = Show`, `Target Panel = SettingsPanel`.

The controls reach `GameManager.Instance.Audio` / `.Accessibility` (persistent from `Start`), so the
same prefab works in both scenes with no per-scene wiring.

## 4. How it connects at runtime

- Every nav button is a `GameStateButton` → it calls a `GameManager` transition (e.g.
  `OpenSettings()` / `StartGame()` / `ReturnToMenu()`), which changes state.
- `GameTransitions` (on the persistent `[Managers]`) maps a state to its scene and plays the bubble
  transition: cover → load scene → reveal. `MainMenu → Start`, `Playing → Main`. (Settings is now an
  in-scene overlay panel, not a scene, so it doesn't route through here.)
- `GameManager` lives on `[Managers]` in `Start` and persists across scenes, so the settings
  panel's controls reach `GameManager.Instance.Audio` / `.Accessibility` with no setup of their own.
- **Volume sliders** → `MusicVolumeControl` / `SfxVolumeControl` read & write the volume settings,
  initialised to the current values when the panel opens.
- **Quit** → exits the build (and stops Play mode in the editor).

## 5. Quick test

1. Press **Play** from the `Start` scene.
2. Click **Settings** → the settings overlay opens; drag the sliders — volumes update live. Click
   the bottom button → the overlay closes back to the menu.
3. Click **Play** — the Console logs `[State] entered Playing` (from `DebugStateControls`, if
   present) and the `Main` scene loads.

## 6. The settings components, in detail

- **Music/Sfx Volume Control** — each goes on its slider object in the Settings scene. Optional
  `Value Label` (`TMP_Text`) shows the volume as a live percentage (e.g. `80%`); leave empty to skip.
- **High Contrast Control** — goes on a UI Toggle. Drives `GameManager.Instance.Accessibility`
  and remembers the choice (PlayerPrefs). The visual swap layer itself isn't built yet (see §8).
- **Game State Button** — one `Action` (Play/Settings/MainMenu/Pause/Resume/Quit) per button. The
  scene change and bubble transition follow automatically from the state it requests.
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
| SETTINGS       | `Settings`   | `PanelButton`, `Action = Show`, `Target Panel = SettingsPanel`, `Use Transition = off`. |
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
- Leave `Settings Button` / `Main Menu Button` / `Settings Panel` empty — `Settings` and
  `QuitToMenu` carry their own `PanelButton` / `GameStateButton`.

### 7.5 Settings (shared overlay)

Pause reuses the **one `SettingsPanel` prefab from §3** — there's no separate sub-panel. Place a
`SettingsPanel` instance in `Main` (an overlay, above `PausePanel`), set inactive. The §7.2 SETTINGS
button opens it (`PanelButton` → `Show`, target = that instance). Its bottom button hides it (back to
the pause menu) or, if you override that instance, jumps to the main menu — see §3. The sliders and
toggle reach the persistent managers, so they work mid-run with no extra wiring.

### 7.6 Test

1. **Play** from `Start` into `Main`.
2. **Esc** → run freezes, art + invisible buttons appear.
3. Click **PLAY** (Resume) or press **Esc** → menu hides, **3-2-1** counts down, *then* the run
   continues. Gameplay stays frozen for the whole count.
4. **SETTINGS** → sub-panel opens; sliders change audio live; **Back** returns.
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
