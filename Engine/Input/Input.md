# Action input

Use `WorldContext.Input`; there is no singleton. `HeftyGame` polls it exactly once before gameplay each frame. Bind names during `IWorld.Load`, then query them from components:

```csharp
world.Input.Bind("Jump", new KeyboardBinding(Keys.Space));
world.Input.Bind("Jump", new MouseBinding(MouseButton.Right));
if (World.Input.IsPressed("Jump")) { }
if (World.Input.IsHeld("Jump")) { }
if (World.Input.IsReleased("Jump")) { }
```

Raw keyboard and mouse queries remain available. `Unbind`, `RemoveAction`, and `ClearActions` support explicit changes. The host gives each world a fresh input manager and clears the old manager during unload, preventing bindings or held-state edges from accumulating between worlds. A stale context retains only its inactive world's input manager. Therefore each world owns and establishes its bindings in `Load`.

## Snapshots and gamepads

`new InputManager(IInputSource)` never polls in its constructor. Call `Update` exactly once
per standalone simulation update. `IInputSource.Sample` returns one `InputSnapshot` with
KeyboardState, MouseState, four GamePadStates and IsFocused. Replay/synthetic sources need
no native window. `DeviceInputSource` polls raw (no MonoGame deadzone) hardware. Supply
`HeftyGameOptions.InputSource` for hosted injection; do not call hosted Input.Update yourself.
All bindings, raw queries, UI and host exit controls read the same snapshot.

```csharp
input.Bind("Confirm", new GamePadButtonBinding(Buttons.A, PlayerIndex.Two));
input.Bind("Accelerate", new GamePadAxisBinding(GamePadAxis.RightTrigger, 0.6f));
input.Bind("Left", new GamePadAxisBinding(GamePadAxis.LeftX, -0.5f));
Vector2 stick = input.GetStick(right: false, deadzone: 0.2f);
float trigger = input.GetAxis(GamePadAxis.LeftTrigger, deadzone: 0.05f);
```

Buttons use MonoGame's standard Buttons flags. Axes include both stick X/Y pairs and both
triggers; stick Y follows MonoGame (+up), not screen (+down). Axis bindings use signed **raw**
thresholds in [-1,1], excluding zero. GetAxis uses a rescaled axial deadzone; GetStick uses
a rescaled radial deadzone. `InputDeadzone.Apply`/`ApplyRadial` reuse these transforms without
a manager. Deadzones must be finite in [0,1); magnitudes clamp to 1 and retain direction.

First sampling, focus loss/regain and pad disconnect/reconnect do not emit synthetic press
or release edges. Held controls on focus/reconnect remain held but cannot confirm until a
real release/press. Unfocused input is neutral; the default host source uses Game.IsActive.
A custom source supplies its own focus flag. Reconnect baselining is per pad, so genuine
keyboard/other-pad edges in that update remain visible. Several bindings aggregate into one
action: switching between held bindings is not a new press. Binding edits are evaluated
against sampled snapshots and do not synthesize a release for a removed binding.

Existing IInputBinding implementations remain source/binary compatible: its snapshot-aware
overload defaults to the original keyboard/mouse method. New device bindings can override
that overload. `InputManagerUiInputSource` adapts these actions to IUiInputSource without
polling or coupling widgets to WorldContext; see the UI guide and MainMenu sample.
