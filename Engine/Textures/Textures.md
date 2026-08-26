# Runtime textures

`TextureFactory` creates simple textures directly on a `GraphicsDevice`, without adding assets to the MonoGame content pipeline.

```csharp
using Hefty.Engine.Textures;

Texture2D pixel = TextureFactory.CreateBlankTexture(world.GraphicsDevice);
Texture2D checkerboard = TextureFactory.CreateCheckerboard(
    world.GraphicsDevice,
    width: 800,
    height: 600,
    cellSize: 40,
    firstColor: Color.LightGray,
    secondColor: Color.DarkGray);
```

`CreateBlankTexture` returns a one-pixel opaque white texture. Scale it through a `SpriteRenderer` and use the renderer's `Color` property to draw solid-color rectangles without creating another texture for each color.

`CreateCheckerboard` builds and uploads a complete pixel array. Create checkerboards during world loading, not during update or draw callbacks.

Textures returned by this factory are runtime resources and are not owned by `ContentManager`. The caller must dispose them, normally from the `IWorld.Unload` method that owns them:

```csharp
public void Unload(WorldContext world)
{
    pixel?.Dispose();
    checkerboard?.Dispose();
}
```
