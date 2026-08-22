# Action input

`InputManager` polls keyboard and mouse state once per `Game1.Update`. Bind one or more device inputs to a named action, then query its frame state without polling or allocating per query:

```csharp
using Hefty.Engine.Input;
using Microsoft.Xna.Framework.Input;

InputManager input = InputManager.Instance();
input.Bind("Jump", new KeyboardBinding(Keys.Space));
input.Bind("Jump", new MouseBinding(MouseButton.Right));

if (input.IsPressed("Jump")) { /* first frame down */ }
if (input.IsHeld("Jump")) { /* any binding is down */ }
if (input.IsReleased("Jump")) { /* first frame all bindings are up */ }
```

Bindings can be changed with `Unbind`, `RemoveAction`, and `ClearActions`. Action names must not be null, empty, or whitespace. Binding an input that is already held initializes the action as held rather than generating a false press.

`KeyboardInputManager` remains available as a compatibility shim. Its existing `IsKeyPressed`, `IsKeyReleased`, `IsKeyDown`, and `IsKeyUp` methods use the same per-frame snapshot.

Input is updated before collision checks and game-object updaters. Call `Update` exactly once per frame if using `InputManager` outside `Game1`.
