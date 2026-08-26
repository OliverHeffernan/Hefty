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
