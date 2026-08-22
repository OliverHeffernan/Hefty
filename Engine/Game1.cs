using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Hefty.Engine.Collision;

namespace Hefty.Engine;


public class Game1 : Game
{
    private readonly GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    private IWorld activeWorld;
    private IWorld pendingWorld;
    private readonly SortedList<IUpdater> updaters = new();
    private readonly SortedList<IDrawable> worldDrawables = new();
    private readonly SortedList<IDrawable> screenDrawables = new();
    private Camera2D activeCamera;

    public Game1(IWorld startupWorld)
    {
        pendingWorld = startupWorld ?? throw new ArgumentNullException(nameof(startupWorld));
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
    }

    public void Instantiate(GameObject gameObject, RenderSpace renderSpace = RenderSpace.World)
    {
        if (gameObject is IUpdater updater)
            updaters.QueueAdd(updater);

        if (gameObject is IDrawable drawable)
        {
            if (renderSpace == RenderSpace.World)
                worldDrawables.QueueAdd(drawable);
            else
                screenDrawables.QueueAdd(drawable);
        }
    }

    public void SetActiveCamera(Camera2D camera)
    {
        activeCamera = camera;
    }

    public void LoadWorld(IWorld world)
    {
		CollisionManager.ClearColliders();
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
		updaters.ApplyQueues();
		worldDrawables.ApplyQueues();
		screenDrawables.ApplyQueues();

        KeyboardInputManager keyboard = KeyboardInputManager.Instance();
        keyboard.Update(gameTime);
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        foreach (IUpdater updater in updaters)
            updater.Update(gameTime);

        CollisionManager.Step((float)gameTime.ElapsedGameTime.TotalSeconds);

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
        CollisionManager.ClearColliders();
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
