# Collision and kinematic response

`Collider` remains a floating-point axis-aligned shape, layer/mask filter, and enter/stay/exit event source. Raw colliders are event-only. A trigger (`IsTrigger = true`) reports events but never blocks motion.

Solid response is opt-in through `PhysicsBody`, which is also an engine `Component`. A body owns a shared `Transform` and one or more colliders and is either `Static` or `Kinematic`. Static bodies are immovable obstacles. Controllers call `kinematic.Move(displacement)` (or set `Velocity`); they must not directly move its transform during gameplay. Add bodies with `GameObject.AddComponent` for automatic cleanup, or call `Destroy` explicitly when managing them separately.

```csharp
Collider shape = new(transform, new Vector2(32, 48), Vector2.Zero);
PhysicsBody body = new(transform, BodyType.Kinematic, shape);
body.Move(inputDirection * speed * deltaSeconds);
```

Each frame, `Game1` updates input and gameplay components first. It then calls `CollisionManager.Step`: pending kinematic motion is swept against filtered, non-trigger static bodies using floating-point AABBs, up to four deterministic impacts are resolved, and remaining tangential motion slides along surfaces. A small contact skin stabilizes resting contact. Events are dispatched afterward from final/touching state, with swept detection retaining fast trigger/event crossings.

`Layer` is one non-zero bit and both masks must permit a pair. `GetBounds()` provides a conservative integer rectangle for legacy/query uses; response uses floating-point bounds internally. `ClearColliders` clears bodies, colliders, and contact state during world changes. No gravity, dynamic rigid bodies, rotation, friction, or restitution is implemented.
