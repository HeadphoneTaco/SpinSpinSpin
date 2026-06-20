# Menu Setup Guide

How to wire up the Main Menu and Settings panel in the Unity editor. The scripts
(`MainMenuController`, `SettingsController`) only handle logic — you build the visuals
and assign references here.

## 1. Create the Canvas

1. In the menu scene, right-click in the Hierarchy → **UI → Canvas**.
   (This also creates an **EventSystem** — required for clicks/sliders. Don't delete it.)
2. Select the Canvas → set **Canvas Scaler → UI Scale Mode** to **Scale With Screen Size**
   (reference resolution e.g. `1920 x 1080`) so the menu scales across resolutions.

## 2. Main Menu

1. Right-click the Canvas → **Create Empty**, rename it `MainMenu`.
2. Add three buttons under it (**UI → Button - TextMeshPro**), label them `Play`,
   `Settings`, `Quit`. Arrange them with a **Vertical Layout Group** on `MainMenu` if you
   want automatic spacing.
3. Select `MainMenu` → **Add Component → Main Menu Controller**.
4. Drag the buttons into the matching fields:
   - `Play Button`   ← the Play button
   - `Settings Button` ← the Settings button
   - `Quit Button`   ← the Quit button
   - `Settings Panel` ← the `Settings` panel from step 3 below (assign after creating it)

## 3. Settings Panel

1. Right-click the Canvas → **Create Empty**, rename it `Settings`. This is the panel that
   opens from the Settings button. Add an `Image` background if you like.
2. Add two sliders under it (**UI → Slider**), name them `MusicSlider` and `SfxSlider`.
   - For each slider: set **Min Value = 0**, **Max Value = 1**, and leave **Whole Numbers**
     unchecked.
3. (Optional) Add a `Close` button (**UI → Button - TextMeshPro**).
4. Select `Settings` → **Add Component → Settings Controller**.
5. Drag references into the fields:
   - `Music Slider` ← `MusicSlider`
   - `Sfx Slider`   ← `SfxSlider`
   - `Close Button` ← the Close button (optional)
6. **Set `Settings` inactive** by default: select it and uncheck the box next to its name at
   the top of the Inspector. The Settings button opens it; the Close button hides it.

## 4. How it connects at runtime

- The controllers reach the game through `GameManager.Instance` (transitions) and
  `GameManager.Instance.Audio` (volume).
- `GameManager` lives on the **[Managers]** object in the Start scene (see
  `Code/Core/StateMachineSetup.md`). It persists across scenes, so it must exist in the boot
  scene — it is no longer auto-created.
- **Play** → `GameManager.Instance.StartGame()` (drives the StateMachine MainMenu → Playing,
  which loads the Main scene).
- **Settings sliders** → `Audio.SetMusicVolume` / `Audio.SetSfxVolume`, initialized to the
  current volumes when the panel opens.
- **Quit** → exits the build (and stops Play mode in the editor).

## 5. Quick test

1. Press Play.
2. Click **Settings**, drag the sliders — volumes update live on the AudioManager.
3. Click **Play** — the Console logs `[State] entered Playing` (from `DebugStateControls`, if
   present) and the Main scene loads.

## 6. Settings extras

The `SettingsController` now has optional fields:

- **Music/Sfx Value Label** (`TMP_Text`) — shows the volume as a live percentage (e.g. `80%`).
  Put a TMP label next to each slider and assign it. Leave empty to skip.
- **High Contrast Toggle** (`Toggle`) — add a UI Toggle, label it "High Contrast", and assign
  it. It drives `GameManager.Instance.Accessibility` and remembers the choice (PlayerPrefs).

## 7. Pause menu (Main scene)

1. Under the Main scene Canvas, make a `PausePanel` (an Image background + Resume / Settings /
   Main Menu buttons). Set it **inactive** by default.
2. Add the **Pause Menu Controller** to a UI object and assign: `Pause Panel`, the three
   buttons, the optional `Settings Panel`, and the `StateEntered` / `StateExited` events.
3. The controller shows the panel on the `Paused` state and hides it otherwise. **Esc** (or
   gamepad **Start**) toggles pause from anywhere in play — it's a code-defined input, no asset
   wiring needed.
4. Resume → `Resume()`, Main Menu → `ReturnToMenu()` (loads `Start`).

> Pause doesn't freeze gameplay yet — that's the separate time-control work. For now it blocks
> spin input and shows the menu.

## 8. High-contrast mode (plumbing only)

The on/off setting, persistence, and broadcast signal exist; the **visual swap layer is not
built yet** (pending the approach decision). To enable the broadcast:

1. Add an **Accessibility Manager** component to the `[Managers]` object and assign the
   `HighContrastChanged` event (`ScriptableObjects/Events/HighContrastChanged.asset`).
2. The Settings toggle already flips it. Once you pick a visual approach, the color-swapping
   components subscribe to `HighContrastChanged`.

Use the **Accessibility ▸ WCAG Contrast Checker** window to verify any high-contrast palette
passes AA/AAA before wiring it.

## Notes

- Buttons use **TextMeshPro**; the first time you add one Unity may prompt to import TMP
  Essentials — accept it.
- If clicks do nothing, confirm an **EventSystem** exists in the scene.
