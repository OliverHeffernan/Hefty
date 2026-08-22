# Event-only collision

Creating a `Collider` registers it with `CollisionManager`. Colliders expose `OnCollisionEnter`, `OnCollisionStay`, and `OnCollisionExit`; the manager reports events but never changes transforms or resolves penetration. `IsTrigger` records gameplay intent and, like every collider, never blocks movement.

```csharp
Collider hitbox = new(transform, new Vector2(32, 48), Vector2.Zero,
    layer: 1u << 0, collisionMask: (1u << 1), isTrigger: true);
hitbox.OnCollisionEnter = other => HandleEnter(other);
hitbox.OnCollisionStay = other => HandleStay(other);
hitbox.OnCollisionExit = other => HandleExit(other);
```

`Layer` must be exactly one non-zero bit. Two colliders interact only when each collider's `CollisionMask` includes the other's layer; a zero mask intentionally disables all interactions. Size must be finite and positive, and offset must be finite.

The broadphase places bounds in every occupied 100-pixel grid cell, including negative cells, and canonicalizes candidates so callbacks are not duplicated. Bounds conservatively floor their minimum and ceil their maximum, so fractional sizes remain collidable. Previous-to-current swept AABB checks catch straightforward fast crossings. This is not continuous physics: curved motion, rotation, and multiple impacts within one frame are not modeled.

Call `UnregisterCollider` when an object is removed. `ClearColliders` is suitable for world teardown. Both produce one exit transition for active pairs and are safe to call from collision callbacks; mutations are deferred until both participants receive the current callback. A nested `CheckCollisions` call from a callback is ignored until the next game frame.
