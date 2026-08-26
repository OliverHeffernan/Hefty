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
