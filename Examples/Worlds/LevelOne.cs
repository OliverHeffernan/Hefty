using Hefty.Engine;
using Hefty.Examples.Components;
using Hefty.Examples.GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Hefty.Examples.Textures;

namespace Hefty.Examples.Worlds;

public class LevelOne : IWorld
{
    private const int LevelWidth = 2000;
    private const int LevelHeight = 1200;

    private Texture2D floorTexture;
    private Texture2D playerTexture;

    public void Initialize(Game1 game)
    {
        floorTexture = TextureFactory.CreateCheckeredTexture(
            game.GraphicsDevice,
            LevelWidth,
            LevelHeight,
            100,
            new Color(72, 120, 72),
            new Color(56, 104, 56)
        );
        Sprite floor = new(floorTexture, new Transform(), new Vector2(LevelWidth, LevelHeight));
        game.Instantiate(floor);

        playerTexture = TextureFactory.CreateBlankTexture(game.GraphicsDevice);
        Player player = new(playerTexture);
        player.Transform.Position = new Vector2(400f, 300f);
        game.Instantiate(player);

		var obstacle = new Obstacle(TextureFactory.CreateBlankTexture(game.GraphicsDevice));
		obstacle.Transform.Position = new Vector2(600f, 300f);
		game.Instantiate(obstacle);


        Camera2D camera = new()
        {
            Bounds = new Rectangle(0, 0, LevelWidth, LevelHeight)
        };
        camera.Transform.Position = player.Transform.Position;
        camera.AddComponent(new CameraFollow(camera, player.Transform) { Smoothing = 8f });
        game.Instantiate(camera);
        game.SetActiveCamera(camera);

        game.Instantiate(new GameObject(new OpenMenu(game)));
    }

    public void Unload(Game1 game)
    {
        floorTexture?.Dispose();
        floorTexture = null;
        playerTexture?.Dispose();
        playerTexture = null;
    }

    private sealed class OpenMenu(Game1 game) : IUpdater
    {
        public void Update(GameTime gameTime)
        {
            if (KeyboardInputManager.Instance().IsKeyPressed(Keys.M))
                game.LoadWorld(new MainMenu());
        }
    }
}
