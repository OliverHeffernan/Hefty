# Sprite animation

`AnimationClip` stores validated texture source rectangles, a fixed frame rate, and whether playback loops. `SpriteAnimator` is a `Component`: attach it to the same object as the `SpriteRenderer` it controls.

```csharp
using Hefty.Engine.Animation;

var gameObject = new GameObject();
var sprite = gameObject.AddComponent(new SpriteRenderer(texture, new Vector2(32, 32)));
var animator = new SpriteAnimator(sprite);

animator.AddClip("walk", new AnimationClip(
    [
        new Rectangle(0, 0, 32, 32),
        new Rectangle(32, 0, 32, 32),
        new Rectangle(64, 0, 32, 32),
    ],
    framesPerSecond: 10,
    loop: true));

gameObject.AddComponent(animator);
animator.Play("walk");
world.Add(gameObject);
```

`Play` applies frame zero immediately. Calling it for the already-playing clip is a no-op unless `restart: true` is supplied. Non-looping clips stop on their final frame. `Stop` leaves the current frame visible.

The animator changes only `SpriteRenderer.SourceRectangle`; position and scale are unaffected.

`IsPaused = true` freezes progress, preserving both the visible frame and partial-frame time.
Unpause to continue. Do not disable the SpriteRenderer or hide its GameObject to freeze time.
`Play` and `Stop` retain their existing semantics and do not change IsPaused.

`AutoAdvance` defaults to true. Set it false and call `Advance(deltaSeconds)` to drive an
individual animation manually. Pause also blocks explicit Advance; unpause before stepping.
Delta must be finite, nonnegative, and no greater than int.MaxValue seconds. Manual advancement
does not run the rest of the object's components. Do not call Advance and enable auto update
for the same interval, or time advances twice.

`TimeSource = gameTime => ...` supplies custom delta seconds for automatic updates only.
For example, return scaled elapsed time or a module-owned clock delta. It is not invoked
while paused or AutoAdvance is false. Default time remains GameTime.ElapsedGameTime.
This API plays sprite-sheet rectangles, not skeletal animation or rigs.
