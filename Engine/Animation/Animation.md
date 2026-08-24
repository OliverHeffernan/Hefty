# Sprite animation

`AnimationClip` stores validated texture source rectangles, a fixed frame rate, and whether playback loops. `SpriteAnimator` is a `Component`: attach it to the same `Sprite` it controls so the existing `GameObject.Update` path advances it.

```csharp
using Hefty.Engine.Animation;

var sprite = new Sprite(texture, transform, new Vector2(32, 32));
var animator = new SpriteAnimator(sprite);

animator.AddClip("walk", new AnimationClip(
    [
        new Rectangle(0, 0, 32, 32),
        new Rectangle(32, 0, 32, 32),
        new Rectangle(64, 0, 32, 32),
    ],
    framesPerSecond: 10,
    loop: true));

sprite.AddComponent(animator);
animator.Play("walk");
game.Instantiate(sprite);
```

`Play` applies frame zero immediately. Calling it for the already-playing clip is a no-op unless `restart: true` is supplied. Non-looping clips stop on their final frame. `Stop` leaves the current frame visible.

The animator changes only `Sprite.SourceRectangle`; position, scale, and the sprite-owned `Transform` are unaffected. Leaving `SourceRectangle` as `null` preserves full-texture drawing for non-animated sprites.
