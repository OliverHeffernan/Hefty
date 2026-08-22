# Hefty retained UI

`UiCanvas` is a retained, viewport-aware screen-space `GameObject`. Add widgets once, then register the canvas with the existing game loop:

```csharp
var canvas = new UiCanvas(game.GraphicsDevice, input);
canvas.Add(new Label(new(20, 20), new(300, 40), font, "Hello", Color.White));
game.Instantiate(canvas, RenderSpace.Screen);
```

Children draw in insertion order (later children are on top) and hit testing uses the reverse order. `UiElement.AddChild` creates nested layouts. `Position` is an offset from `Anchor`; the element's matching point is attached to that anchor. For example, `Anchor.BottomRight` with position `(-20, -20)` stays 20 pixels from the viewport or parent bottom-right when resized.

Buttons support hover, focus, disabled visuals, and `Activated`. Focus order is deterministic depth-first insertion order. Mouse edges activate the topmost hit button. The configurable canvas actions default to `UiConfirm`, `UiNext`, and `UiPrevious`.

## Input adapter

The UI deliberately does not depend on the separate action-input change. `IUiInputSource` is the boundary. After that API is merged, wire its `InputManager.IsPressed`, `IsHeld`, and `IsReleased` methods without reflection:

```csharp
IUiInputSource input = new DelegateUiInputSource(
    () => inputManager.MousePosition,
    () => inputManager.IsMousePressed,
    () => inputManager.IsMouseHeld,
    () => inputManager.IsMouseReleased,
    inputManager.IsPressed,
    inputManager.IsHeld,
    inputManager.IsReleased);
```

Adjust the four mouse delegates to the final A4 mouse member names if they differ; the named-action delegates already match that API. The held/released queries are exposed for custom widgets even though built-in navigation uses press edges.

No fonts or textures are bundled. Supply a `SpriteFont` and a caller-owned texture (normally a 1x1 white texture) to widgets. The UI never creates assets or changes graphics state.
