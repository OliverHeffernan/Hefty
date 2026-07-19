using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace engine;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    private IWorld activeWorld;
    private IWorld pendingWorld;
    private readonly List<IUpdater> updaters = [];
    private readonly List<IDrawable> worldDrawables = [];
    private readonly List<IDrawable> screenDrawables = [];
    private Camera2D activeCamera;

    public Game1(IWorld startupWorld)
    {
        pendingWorld = startupWorld ?? throw new ArgumentNullException(nameof(startupWorld));
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    public void Instantiate(GameObject gameObject, RenderSpace renderSpace = RenderSpace.World)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        if (gameObject is IUpdater updater)
            updaters.Add(updater);

        if (gameObject is IDrawable drawable)
        {
            if (renderSpace == RenderSpace.World)
                worldDrawables.Add(drawable);
            else
                screenDrawables.Add(drawable);
        }
    }

    public void SetActiveCamera(Camera2D camera)
    {
        activeCamera = camera;
    }

    public void LoadWorld(IWorld world)
    {
        pendingWorld = world ?? throw new ArgumentNullException(nameof(world));
    }

    protected override void Initialize()
    {
        base.Initialize();
        ApplyPendingWorld();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardInputManager keyboard = KeyboardInputManager.Instance();
        keyboard.Update(gameTime);
		CollisionManager.CheckCollisions();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        foreach (IUpdater updater in updaters)
            updater.Update(gameTime);

        ApplyPendingWorld();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        activeCamera?.ClampToBounds(GraphicsDevice.Viewport);
        Matrix view = activeCamera?.GetViewMatrix(GraphicsDevice.Viewport) ?? Matrix.Identity;

        spriteBatch.Begin(transformMatrix: view);
        foreach (IDrawable drawable in worldDrawables)
            drawable.Draw(spriteBatch, gameTime);
        spriteBatch.End();

        spriteBatch.Begin(transformMatrix: Matrix.Identity);
        foreach (IDrawable drawable in screenDrawables)
            drawable.Draw(spriteBatch, gameTime);
        spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        activeWorld?.Unload(this);
        activeWorld = null;
        pendingWorld = null;
        ClearScene();
        spriteBatch?.Dispose();
        base.UnloadContent();
    }

    private void ApplyPendingWorld()
    {
        if (pendingWorld is null)
            return;

        IWorld incoming = pendingWorld;
        pendingWorld = null;

        activeWorld?.Unload(this);
        ClearScene();
        activeWorld = incoming;
        activeWorld.Initialize(this);
    }

    private void ClearScene()
    {
        updaters.Clear();
        worldDrawables.Clear();
        screenDrawables.Clear();
        activeCamera = null;
    }
}
