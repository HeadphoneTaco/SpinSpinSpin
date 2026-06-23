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

## 3. Settings scene (`Settings.unity`)

Settings is its own scene (see the Build Settings step in `Code/Core/StateMachineSetup.md`). It
does **not** get an `[Managers]` object — those persist from `Start`, so the controls still reach
`GameManager.Instance`.

1. Create the scene (**File → New Scene**, save as `Settings` next to `Start`). Add a **Camera**
   pointed at Mina's detergent-bottle UI, and a **Canvas** (which also creates an **EventSystem** —
   keep it). Set the Canvas Scaler to **Scale With Screen Size**.
2. Build the controls on Mina's bottle art:
   - Two sliders (**UI → Slider**) — **Min 0, Max 1, Whole Numbers off**. Add **Music Volume
     Control** to one and **Sfx Volume Control** to the other (slider auto-fills); optionally
     assign each a `Value Label` (TMP) for the live %.
   - A **Toggle** for high contrast → add **High Contrast Control** (auto-fills).
   - A **Back** button (TextMeshPro) → add **`GameStateButton`**, `Action` = **MainMenu**. This
     returns to `Start` through the bubble transition. (Label it "Resume"/"Back" to taste.)
3. Add the scene to **Build Settings** (see the StateMachineSetup recipe).

> No `SettingsController` and no panel toggling — the scene *is* the settings screen, and each
> control binds itself.

## 4. How it connects at runtime

- Every nav button is a `GameStateButton` → it calls a `GameManager` transition (e.g.
  `OpenSettings()` / `StartGame()` / `ReturnToMenu()`), which changes state.
- `GameTransitions` (on the persistent `[Managers]`) maps the new state to its scene and plays the
  bubble transition: cover → load scene → reveal. `MainMenu → Start`, `Settings → Settings`,
  `Playing → Main`.
- `GameManager` lives on `[Managers]` in `Start` and persists across scenes, so the Settings
  scene's controls reach `GameManager.Instance.Audio` / `.Accessibility` with no setup of their own.
- **Volume sliders** → `MusicVolumeControl` / `SfxVolumeControl` read & write the volume settings,
  initialised to the current values when the Settings scene loads.
- **Quit** → exits the build (and stops Play mode in the editor).

## 5. Quick test

1. Press **Play** from the `Start` scene.
2. Click **Settings** → bubbles sweep, the `Settings` scene loads; drag the sliders — volumes update
   live on the AudioManager. Click **Back** → bubbles sweep back to `Start`.
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

(Sections 1–6 build the main menu + settings in the **`Start`** / **`Settings`** scenes. The pause
menu lives in the **`Main`** scene. See `Code/Core/StateMachineSetup.md` for the per-scene map.)

**What already works (no building needed):** `GameManager.Pause/Resume/TogglePause`, the `Paused`
state, and `PauseMenuController` (auto show/hide + **Esc**/gamepad **Start** toggle). And gameplay
genuinely **freezes** while paused: `GremlinRunner`, `RunDirector`, `ScrollingItem`, and
`TrackSpawner` each gate their `Update` on `GameManager.Instance.IsPlaying`, which is false in
`Paused` — so movement, scroll, spawning, and the timer all halt. (No `Time.timeScale` trick; the
state gate is the freeze.) Music keeps playing by design (`StateAudio` leaves it alone on `Paused`).

What you build in the editor is the **panel and its buttons**. Like the rest of the UI, each button
is one self-contained component (`GameStateButton` for state changes, `PanelButton` for show/hide),
so the controller just owns visibility + the Esc toggle.

### 7.1 The pause panel

1. Under the `Main` scene's **Canvas**, create an empty `PausePanel`.
   - Add a full-screen **Image** as its backdrop (a dim overlay). Keep **Raycast Target ON** so
     clicks can't fall through to the game behind it.
   - Add four **Button - TextMeshPro** children: `Resume`, `Restart`, `Settings`, `Quit to Menu`.
     A **Vertical Layout Group** on `PausePanel` spaces them automatically.
   - Set `PausePanel` **inactive** (untick the top-left checkbox) so it starts hidden.

2. Add the **Pause Menu Controller** to a UI object (the Canvas, or an empty `[PauseLogic]` under it):
   - `Pause Panel` → the `PausePanel` object.
   - `State Entered` → `ScriptableObjects/Events/StateEntered.asset`.
   - `State Exited`  → `ScriptableObjects/Events/StateExited.asset`.
   - Leave the optional `Resume/Settings/Main Menu Button` + `Settings Panel` fields **empty** —
     we drive each button with its own component below (one consistent pattern). The controller
     still handles auto show/hide on the `Paused` state and the **Esc**/**Start** toggle.

### 7.2 Wire the five buttons (one component each)

| Button       | Component        | Setting                                              |
|--------------|------------------|-----------------------------------------------------|
| Resume       | `GameStateButton`| `Action = Resume`                                   |
| Restart      | `GameStateButton`| `Action = Restart`  *(new action — reloads `Main`)* |
| Settings     | `PanelButton`    | `Action = Show`, `Target Panel = SettingsSubPanel`, `Use Transition = off` |
| Quit to Menu | `GameStateButton`| `Action = MainMenu`  *(returns to `Start` — NOT a desktop quit)* |

There is no desktop-`Quit` button here on purpose: a stray click mid-run shouldn't be able to close
the whole game. Full **Quit** lives on the Start-scene main menu (§2). `Quit to Menu` uses the
existing `MainMenu` action, so leaving a run always lands you safely back on the menu.

Select each button → **Add Component** → the listed component (the `Button` ref auto-fills) → set
the field shown. **Restart** calls the new `GameManager.RestartGame()`: it re-enters `Playing` and
reloads the `Main` scene behind the bubble transition, so `RunDirector` and the track rebuild from
scratch — a clean fresh run.

### 7.3 The in-panel Settings sub-panel

Reaching the full Settings *scene* from pause would abandon the run (its Back goes to the main menu),
so pause gets a small **in-place** settings panel instead — the sliders/toggle talk to the same
persistent managers, so no extra setup is needed.

1. Under `PausePanel`, create `SettingsSubPanel` (an Image background). Set it **inactive**.
2. Inside it add the same controls as the Settings scene (see §6):
   - Two **Sliders** (Min 0, Max 1, Whole Numbers off) → `Music Volume Control` / `Sfx Volume Control`.
   - A **Toggle** → `High Contrast Control`.
   - A **Back** button → `PanelButton`, `Action = Hide`, `Target Panel = SettingsSubPanel`,
     `Use Transition = off`. (Returns to the pause buttons.)

The §7.2 **Settings** button shows this panel; its **Back** button hides it. Because the controls
reach `GameManager.Instance.Audio` / `.Accessibility` (which persist from `Start`), they work mid-run
with no wiring of their own.

### 7.4 Test

1. **Play** from `Start`, then **Play** into `Main`.
2. Press **Esc** → the run freezes (gremlin, scroll, timer all stop) and `PausePanel` appears.
3. **Settings** → sub-panel opens; drag a slider → volume changes live; **Back** → pause buttons.
4. **Resume** (or **Esc**) → the run continues exactly where it left off.
5. **Restart** → bubbles sweep, `Main` reloads, a fresh run starts in `Playing`.
6. **Quit to Menu** → bubbles sweep back to the `Start` main menu (the run ends; the app stays open).

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
