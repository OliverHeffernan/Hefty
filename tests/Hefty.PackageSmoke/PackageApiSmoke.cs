using Hefty.Engine;
using Hefty.Engine.Textures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.PackageSmoke;

/// <summary>Compiles representative consumer code against the packed engine rather than its source project.</summary>
public sealed class PackageWorld : IWorld
{
    /// <summary>Exercises construction of the package's configurable game host.</summary>
    public static HeftyGame CreateHost()
    {
        return new HeftyGame(
            new PackageWorld(),
            new HeftyGameOptions
            {
                BackBufferWidth = 1280,
                BackBufferHeight = 720,
                ClearColor = Color.Black
            });
    }

    /// <inheritdoc />
    public void Load(WorldContext world)
    {
        GameObject player = world.Add(new GameObject { Name = "Player" });
        player.Transform.Position = new Vector2(100, 100);
        player.AddComponent(new MovementComponent());

        Camera2D camera = world.Add(new Camera2D());
        camera.Transform.Position = player.Transform.Position;
        world.ActiveCamera = camera;
    }

    /// <summary>Exercises the public runtime-texture API without requiring a graphics device during this compile-only check.</summary>
    public static Texture2D CreatePixel(WorldContext world)
    {
        return TextureFactory.CreateBlankTexture(world.GraphicsDevice);
    }
}

/// <summary>Compiles a consumer-defined engine component.</summary>
public sealed class MovementComponent : Component
{
    /// <inheritdoc />
    protected override void Update(GameTime gameTime)
    {
        Transform.Position += Vector2.UnitX * (float)gameTime.ElapsedGameTime.TotalSeconds;
    }
}
