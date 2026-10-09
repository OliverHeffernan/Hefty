# Collision and kinematic response

`PhysicsBody` is a component and is registered only while its owner belongs to the active world. Construct it with `BodyType.Static` or `BodyType.Kinematic`, then add axis-aligned colliders that use the owner's transform:

```csharp
var body = gameObject.AddComponent(new PhysicsBody(BodyType.Kinematic));
var shape = body.AddCollider(new Collider(gameObject.Transform, new(32, 48), Vector2.Zero));
shape.CollisionEntered += other => Console.WriteLine("enter");
body.Move(direction * speed * deltaSeconds);
```

Removal, object destruction, and world unload unregister bodies and colliders reliably. `CollisionEntered`, `CollisionStayed`, and `CollisionExited` are events, so consumers cannot replace one another's handlers. Layers contain exactly one nonzero bit; both collision masks must permit a pair. Triggers report events without blocking.

Kinematic movement and `Velocity` are swept against non-trigger static bodies and slide along surfaces. Directly changing a kinematic transform during gameplay bypasses response. Hosted stepping and registration are automatic. Scope remains translation-only AABBs: no gravity, dynamic bodies, rotation, friction, or restitution. Collider size/offset do not inherit transform scale.

## Standalone world (no window or GraphicsDevice)

```csharp
var physics = new CollisionWorld();
var entity = new GameObject();
var body = entity.AddComponent(new PhysicsBody(BodyType.Kinematic));
body.AddCollider(new Collider(entity.Transform, new(16, 24), Vector2.Zero));
physics.Add(body); // collider transforms must match the owner
body.Move(new(100, 0));
physics.Step(1f / 60f); // synchronous; consumes Move once, also integrates Velocity
Console.WriteLine(body.Motion.Actual);
physics.Remove(body); // emits exits, discards queued displacement
physics.Clear();      // releases registrations, not GameObjects
```

Register static bodies the same way. A body can belong to only one CollisionWorld.
Destroying its GameObject/removing its component also unregisters it. Separate instances
never collide or clear one another. Duplicate Add is idempotent. `Step(0)` consumes queued
displacement and depenetrates; it is a real step. Reentrant stepping throws. There is no
automatic clock for standalone worlds: call Step once per simulation update. In the host,
use `World.Physics`; calling its Step/CheckCollisions manually throws to prevent double work.
`CheckCollisions` is event-only standalone detection, not a second solver.

## Resolved facts and filtering

Read `body.Motion.Requested` and `Actual` after Step or in `Component.PostPhysics`.
Actual includes depenetration. `body.Contacts` contains this step's solid sweep impacts in
solver order, including `Shape`, `Other`, outward `Normal`, `Time` (0–1 relative to the
whole step), `Penetration`, `InitiallyOverlapping`, and remaining `Movement`. Normals point
from the static shape toward the moving shape. Contacts clear each step; copy them if
retaining history. They are impacts, not a persistent grounding flag, and exclude triggers.
Use geometry queries for resting contact when there is no motion.

`physics.ShouldResolve = hit => ...` filters a candidate **after the swept AABB test and
before response**. Return false to pass through that shape. A one-way adapter can examine
normal/direction and previous game-module state; temporary exclusions can compare shape
identity. Keep filters pure: do not change registrations/transforms or step from a filter.
Filtering affects solid response only; legacy enter/stay/exit events still describe geometric
overlap/swept intersections. Use masks or filter events in the adapter to suppress those too.

`physics.Query(new Aabb(left, top, right, bottom), mask, predicate)` returns current touching/
overlapping shapes, ordered by creation ID, without events or stepping. It includes triggers
and shapes from the caller's own body unless the predicate excludes them. `GetFloatBounds`
avoids integer rounding; expand a small query below a body to inspect resting surfaces.
Query bounds must be finite and ordered. Solid motion retains the existing 100-unit grid,
0.001-unit contact skin, four-impact limit and static-only response; kinematic pairs still
report events but do not block each other.

`physics.Stepped` runs after all motion and collider events, even when no contact exists.
Hosted component PostPhysics runs afterward in normal update order. Callback exceptions
propagate; stepping is not transactional. Enter/stay/exit handlers may remove shapes or clear
the world safely; registrations made during events become motion participants next step.
