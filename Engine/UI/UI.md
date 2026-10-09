# Hefty retained UI

`UiCanvas` is a retained, viewport-aware screen-space `GameObject`. Add widgets once, then register the canvas with the existing game loop:

```csharp
var canvas = new UiCanvas(world.GraphicsDevice, input);
canvas.Add(new Label(new(20, 20), new(300, 40), font, "Hello", Color.White));
canvas.RenderSpace = RenderSpace.Screen;
world.Add(canvas);
```

Children draw in insertion order (later children are on top) and hit testing uses the reverse order. `UiElement.AddChild` creates nested layouts. `Position` is an offset from `Anchor`; the element's matching point is attached to that anchor. For example, `Anchor.BottomRight` with position `(-20, -20)` stays 20 pixels from the viewport or parent bottom-right when resized.

An element tree can belong to only one canvas. Remove a root with `UiCanvas.Remove` (or a child
with `RemoveChild`) before adding it to another canvas or reparenting it.

Buttons support hover, focus, disabled visuals, and `Activated`. Focus order is deterministic depth-first insertion order. Mouse edges activate the topmost hit button. The configurable canvas actions default to `UiConfirm`, `UiNext`, and `UiPrevious`.

## Input adapter

`IUiInputSource` is the UI boundary. Adapt the current world's input service:

```csharp
IUiInputSource input = new DelegateUiInputSource(
    () => inputManager.MousePosition,
    () => inputManager.IsMouseButtonPressed(MouseButton.Left),
    () => inputManager.IsMouseButtonDown(MouseButton.Left),
    () => inputManager.IsMouseButtonReleased(MouseButton.Left),
    inputManager.IsPressed,
    inputManager.IsHeld,
    inputManager.IsReleased);
```

The mouse delegates use A4's `InputManager.MousePosition` and left-button edge/state queries; choose another `MouseButton` when needed. The held/released action queries are exposed for custom widgets even though built-in navigation uses press edges.

No fonts or textures are bundled. Supply a `SpriteFont` and a caller-owned texture (normally a 1x1 white texture) to widgets. The UI never creates assets or changes graphics state.

## Sampled controller navigation and headless layout

The built-in adapter is `new InputManagerUiInputSource(inputManager)`; DelegateUiInputSource
remains supported. Bind UiNext/UiPrevious to D-pad buttons or signed stick-axis thresholds,
and UiConfirm to A/Enter. Widgets consume only IUiInputSource, never game services:

```csharp
world.Input.Bind("UiNext", new GamePadButtonBinding(Buttons.DPadDown));
world.Input.Bind("UiPrevious", new GamePadButtonBinding(Buttons.DPadUp));
world.Input.Bind("UiConfirm", new GamePadButtonBinding(Buttons.A));
var canvas = new UiCanvas(world.GraphicsDevice, new InputManagerUiInputSource(world.Input))
    { RenderSpace = RenderSpace.Screen };
world.Add(canvas);
```

A press moves deterministic focus; held controls do not autorepeat. Confirm activates the
current interactive focus only. Disabled/hidden/removed focus clears. First press of Next
focuses the first element; focus is initially null. Focus loss/reconnect baselines avoid
accidental confirmation (see Input guide). MainMenu demonstrates retained controller input.

For standalone tests use `new UiCanvas(() => new Rectangle(0,0,width,height), input)` and
call `canvas.Update(gameTime)` **after** input sampling. Layout/hit testing/focus/activation
need no native graphics. Hosted canvases update through their component and reject manual
Update. Drawing still needs SpriteBatch and caller resources; use RenderSpace.Screen as before.
Panel groups/layouts children, Label draws text, and ProgressBar renders a normalized fraction.
The host owns draw state; UI does not own fonts/textures or poll native devices.
