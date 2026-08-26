using Hefty.Engine;
using Hefty.Engine.Input;
using Hefty.Examples.Components;
using Hefty.Examples.GameObjects;
using Hefty.Examples.Textures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Hefty.Examples.Worlds;

public sealed class LevelOne : IWorld
{
    private const int LevelWidth = 2000;
    private const int LevelHeight = 1200;
    private Texture2D floorTexture;
    private Texture2D objectTexture;

    public void Load(WorldContext world)
    {
        world.Input.Bind("Up", new KeyboardBinding(Keys.W));
        world.Input.Bind("Down", new KeyboardBinding(Keys.S));
        world.Input.Bind("Left", new KeyboardBinding(Keys.A));
        world.Input.Bind("Right", new KeyboardBinding(Keys.D));
        world.Input.Bind("Menu", new KeyboardBinding(Keys.M));

        floorTexture = TextureFactory.CreateCheckeredTexture(
            world.GraphicsDevice,
            LevelWidth,
            LevelHeight,
            100,
            new Color(72, 120, 72),
            new Color(56, 104, 56));
        GameObject floor = new();
        floor.AddComponent(new SpriteRenderer(floorTexture, new Vector2(LevelWidth, LevelHeight)));
        world.Add(floor);

        objectTexture = TextureFactory.CreateBlankTexture(world.GraphicsDevice);
        Player player = world.Add(new Player(objectTexture));
        player.Transform.Position = new Vector2(400, 300);
        Obstacle obstacle = world.Add(new Obstacle(objectTexture));
        obstacle.Transform.Position = new Vector2(600, 300);

        Camera2D camera = world.Add(new Camera2D
        {
            Bounds = new Rectangle(0, 0, LevelWidth, LevelHeight)
        });
        camera.Transform.Position = player.Transform.Position;
        camera.AddComponent(new CameraFollow(camera, player.Transform) { Smoothing = 8 });
        world.ActiveCamera = camera;

        GameObject switcher = new();
        switcher.AddComponent(new ChangeOnAction("Menu", () => world.ChangeWorld(new MainMenu())));
        world.Add(switcher);
    }

    public void Unload(WorldContext world)
    {
        floorTexture?.Dispose();
        objectTexture?.Dispose();
        floorTexture = null;
        objectTexture = null;
    }

    private sealed class ChangeOnAction(string action, System.Action change) : Component
    {
        protected override void Update(GameTime gameTime)
        {
            if (World.Input.IsPressed(action))
                change();
        }
    }
}
