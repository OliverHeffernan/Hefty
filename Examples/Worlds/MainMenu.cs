using Hefty.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Hefty.Examples.Textures;

namespace Hefty.Examples.Worlds;

/// <summary>A minimal screen-space world. Press Enter to load the playable world.</summary>
public class MainMenu : IWorld
{
    private Texture2D panelTexture;

    public void Initialize(Game1 game)
    {
        panelTexture = TextureFactory.CreateBlankTexture(game.GraphicsDevice);
        Sprite panel = new(panelTexture, new Transform { Position = new Vector2(250f, 200f) }, new Vector2(300f, 100f));
        game.Instantiate(panel, RenderSpace.Screen);
        game.Instantiate(new GameObject(new StartLevel(game)));
    }

    public void Unload(Game1 game)
    {
        panelTexture?.Dispose();
        panelTexture = null;
    }

    private sealed class StartLevel(Game1 game) : IUpdater
    {
        public void Update(GameTime gameTime)
        {
            if (KeyboardInputManager.Instance().IsKeyPressed(Keys.Enter))
                game.LoadWorld(new LevelOne());
        }
    }
}
