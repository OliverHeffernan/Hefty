using Hefty.Engine;
using Hefty.Engine.Input;
using Hefty.Examples.Textures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Hefty.Examples.Worlds;

/// <summary>A minimal screen-space world. Press Enter to load the playable world.</summary>
public sealed class MainMenu : IWorld
{
    private Texture2D panelTexture;

    public void Load(WorldContext world)
    {
        world.Input.Bind("Start", new KeyboardBinding(Keys.Enter));
        panelTexture = TextureFactory.CreateBlankTexture(world.GraphicsDevice);
        GameObject panel = new() { RenderSpace = RenderSpace.Screen };
        panel.Transform.Position = new Vector2(250, 200);
        panel.AddComponent(new SpriteRenderer(panelTexture, new Vector2(300, 100)));
        panel.AddComponent(new StartLevel());
        world.Add(panel);
    }

    public void Unload(WorldContext world)
    {
        panelTexture?.Dispose();
        panelTexture = null;
    }

    private sealed class StartLevel : Component
    {
        protected override void Update(GameTime gameTime)
        {
            if (World.Input.IsPressed("Start"))
                World.ChangeWorld(new LevelOne());
        }
    }
}
