# State Machine + GameEvents Setup

This project's game state runs on the **CoreUtils StateMachine** (GameObject-based states)
and broadcasts changes through **CoreUtils GameEvents** (ScriptableObject assets). This guide
supersedes the old state/scene guides.

## The big picture

- States are **child GameObjects** under a `StateMachine`: `MainMenu`, `Playing`, `Paused`,
  `GameOver`. The machine activates exactly one at a time.
- A **bridge** (`GameStateEventsBridge`) listens to the machine and raises two event assets:
  - `StateEntered` (GameEventString, payload = state name)
  - `StateExited` (GameEventString, payload = state name)
- Everything else (audio, scene loading, the spinner, debug keys) **reacts to those events** —
  nothing references the state machine directly except `GameManager`.
- `GameManager` is the front door: `GameManager.Instance.StartGame()`, `.Pause()`,
  `.TogglePause()`, `.EndGame()`, `.ReturnToMenu()` all call `StateMachine.ChangeState(...)`.

The event assets already exist: `Assets/_Project/ScriptableObjects/Events/StateEntered.asset`
and `StateExited.asset`.

## 1. Build the persistent managers object

In the **Start** scene, create one root object and add the manager components to it:

```
[Managers]                         (root — survives scene loads via DontDestroyOnLoad)
├── (component) GameManager
├── (component) StateMachine        Default State = the MainMenu child below
├── (component) GameStateEventsBridge   State Entered = StateEntered.asset
│                                        State Exited  = StateExited.asset
├── (component) SceneLoader          State Entered = StateEntered.asset
├── (component) DebugStateControls   State Entered = StateEntered.asset   (optional, dev only)
├── MainMenu      (empty child GameObject)
├── Playing       (empty child GameObject)
├── Paused        (empty child GameObject)
└── GameOver      (empty child GameObject)
```

Notes:
- The four state children are **empty logical markers** — they don't hold the menu/gameplay
  visuals (those live in their scenes). Name them **exactly** `MainMenu`, `Playing`, `Paused`,
  `GameOver` (they must match `GameStateNames`).
- On the `StateMachine`, set **Default State** to the `MainMenu` child.
- `GameManager` auto-finds the `StateMachine` (same object / children), and the `AudioManager`
  is added automatically at runtime — you don't place it.
- Put `[Managers]` in the **Start** scene only. It persists into `Main` on its own.

## 2. Wire the event references

Drag the assets into the serialized fields:

| Component               | Field(s)                          | Asset             |
|-------------------------|-----------------------------------|-------------------|
| GameStateEventsBridge   | State Entered / State Exited      | StateEntered / StateExited |
| SceneLoader             | State Entered                     | StateEntered      |
| DebugStateControls      | State Entered                     | StateEntered      |
| StateAudio (per scene)  | State Entered                     | StateEntered      |
| Spinner (Main scene)    | State Entered / State Exited      | StateEntered / StateExited |

## 3. Scenes

- Build Settings already lists `Start` (0) and `Main` (1).
- `SceneLoader` loads `Start` on `MainMenu` and `Main` on `Playing` (it guards against
  reloading the scene you're already in, so resuming from Paused doesn't reload `Main`).
- **Main menu UI** goes in `Start` (see `Code/UI/MenuSetup.md`); the Play button already calls
  `GameManager.Instance.StartGame()`.

## 4. Audio per scene

`StateAudio` is a normal scene object. Simplest setup: put one in each scene —

- `Start`: assign `Menu Music` + the `StateEntered` event.
- `Main`: assign `Gameplay Music` (+ `Game Over Sfx`) + the `StateEntered` event.

On load it reads the current state and plays the right track.

## 5. Spinner (Main scene)

Add the `Spinner` to a visible object in `Main`, assign its clips and the
`StateEntered` / `StateExited` events. Input is enabled only while `Playing` is active.

## 6. Test pass

1. Open `Start`, press **Play**. The machine enters `MainMenu`; menu music plays.
2. Click **Play** (or press `1`) → `Main` loads, gameplay music starts, the spinner takes input.
3. `Space` / gamepad A / click → spins + SFX; coasting to a stop logs `[Spinner] Landed`.
4. `2` toggles pause; `3` ends the game (stinger); `0` returns to the menu.
   The Console logs `[State] entered <Name>` on each change.

## Adding a new state

1. Add a child GameObject under the StateMachine, named for the state.
2. Add its name to `GameStateNames`.
3. React to it wherever needed by switching on the name in a `StateEntered` handler.

## Designer-friendly extras (optional)

- Add a `State` + `StateEvents` component to any state child to wire per-state `UnityEvents`
  in the inspector (e.g. show/hide a pause panel) with no code.
- Create more `GameEvent` assets via **Create ▸ CoreUtils ▸ GameEvent** for other signals.
