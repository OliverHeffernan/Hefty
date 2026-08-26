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
