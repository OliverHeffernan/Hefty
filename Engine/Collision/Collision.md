# Collision and kinematic response

`PhysicsBody` is a component and is registered only while its owner belongs to the active world. Construct it with `BodyType.Static` or `BodyType.Kinematic`, then add axis-aligned colliders that use the owner's transform:

```csharp
var body = gameObject.AddComponent(new PhysicsBody(BodyType.Kinematic));
var shape = body.AddCollider(new Collider(gameObject.Transform, new(32, 48), Vector2.Zero));
shape.CollisionEntered += other => Console.WriteLine("enter");
body.Move(direction * speed * deltaSeconds);
```

Removal, object destruction, and world unload unregister bodies and colliders reliably. `CollisionEntered`, `CollisionStayed`, and `CollisionExited` are events, so consumers cannot replace one another's handlers. Layers contain exactly one nonzero bit; both collision masks must permit a pair. Triggers report events without blocking.

Kinematic movement and `Velocity` are swept against non-trigger static bodies and slide along surfaces. Directly changing a kinematic transform during gameplay bypasses response. Collision stepping and registration are engine internals. Scope remains translation-only AABBs: no gravity, dynamic bodies, rotation, friction, or restitution.
