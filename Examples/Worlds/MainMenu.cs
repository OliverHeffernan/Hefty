using Hefty.Engine;
using Hefty.Engine.Input;
using Hefty.Engine.Textures;
using Hefty.Engine.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Hefty.Examples.Worlds;

/// <summary>A minimal screen-space world. Press Enter to load the playable world.</summary>
public sealed class MainMenu : IWorld
{
    private Texture2D? panelTexture;

    public void Load(WorldContext world)
    {
        world.Input.Bind("Start", new KeyboardBinding(Keys.Enter));
        world.Input.Bind("UiNext", new GamePadButtonBinding(Buttons.DPadDown));
        world.Input.Bind("UiNext", new KeyboardBinding(Keys.Down));
        world.Input.Bind("UiPrevious", new GamePadButtonBinding(Buttons.DPadUp));
        world.Input.Bind("UiConfirm", new GamePadButtonBinding(Buttons.A));
        panelTexture = TextureFactory.CreateBlankTexture(world.GraphicsDevice);
        UiCanvas canvas = new(world.GraphicsDevice, new InputManagerUiInputSource(world.Input)) { RenderSpace = RenderSpace.Screen };
        // No font assets are bundled: this retained button preserves the original plain menu panel.
        Button start = new(new(250, 200), new(300, 100), panelTexture, null!, "",
            Color.White, Color.LightGray, Color.Gold, Color.DarkGray, Color.Black);
        start.Activated += _ => world.ChangeWorld(new LevelOne());
        canvas.Add(start);
        canvas.AddComponent(new StartLevel());
        world.Add(canvas);
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
