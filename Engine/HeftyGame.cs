using System;
using System.Collections.Generic;
using System.Linq;
using Hefty.Engine.Collision;
using Hefty.Engine.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Hefty.Engine;

/// <summary>The MonoGame host that owns input and runs one <see cref="IWorld"/> at a time.</summary>
public sealed class HeftyGame : Game
{
    private readonly GraphicsDeviceManager graphics;
    private readonly HeftyGameOptions options;
    private readonly List<GameObject> objects = [];
    private readonly List<GameObject> additions = [];
    private long nextSequence;
    private SpriteBatch? spriteBatch;
    private IWorld? activeWorld;
    private IWorld? pendingWorld;
    private WorldContext? context;

    /// <summary>Creates a host. The startup world is loaded after MonoGame initializes.</summary>
    /// <param name="startupWorld">The first world to load.</param>
    /// <param name="options">Optional host and graphics configuration.</param>
    /// <exception cref="ArgumentNullException"><paramref name="startupWorld"/> is <see langword="null"/>.</exception>
    public HeftyGame(IWorld startupWorld, HeftyGameOptions? options = null)
    {
        pendingWorld = startupWorld ?? throw new ArgumentNullException(nameof(startupWorld));
        this.options = options ?? new HeftyGameOptions();
        this.options.Validate();

        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = this.options.BackBufferWidth;
        graphics.PreferredBackBufferHeight = this.options.BackBufferHeight;
        graphics.IsFullScreen = this.options.IsFullScreen;
        Content.RootDirectory = this.options.ContentRootDirectory;
        IsMouseVisible = this.options.IsMouseVisible;
        IsFixedTimeStep = this.options.IsFixedTimeStep;
        Input = new InputManager();
    }

    internal InputManager Input { get; private set; }

    internal void QueueAdd(WorldContext source, GameObject gameObject)
    {
        ValidateContext(source);
        ArgumentNullException.ThrowIfNull(gameObject);
        if (gameObject.IsDestroyed) throw new InvalidOperationException("Destroyed objects cannot be added again.");
        if (gameObject.WorldInternal is not null || additions.Contains(gameObject))
            throw new InvalidOperationException("A game object can belong to only one world.");
        gameObject.Sequence = ++nextSequence;
        additions.Add(gameObject);
    }

    internal void QueueWorld(WorldContext source, IWorld world)
    {
        ValidateContext(source);
        pendingWorld = world ?? throw new ArgumentNullException(nameof(world));
    }

    internal void SetActiveCamera(WorldContext source, Camera2D? camera)
    {
        ValidateContext(source);
        if (camera is not null
            && (!ReferenceEquals(camera.WorldInternal, source) && !additions.Contains(camera)))
        {
            throw new InvalidOperationException("The active camera must be added to this world.");
        }
        if (camera?.IsDestroyed == true)
            throw new InvalidOperationException("A destroyed camera cannot be made active.");

        source.SetActiveCamera(camera);
    }

    private void ValidateContext(WorldContext source)
    {
        if (!ReferenceEquals(context, source) || !source.IsActive)
            throw new InvalidOperationException("This world context is no longer active.");
    }

    protected override void Initialize() { base.Initialize(); ApplyPendingWorld(); }
    protected override void LoadContent() => spriteBatch = new SpriteBatch(GraphicsDevice);

    protected override void Update(GameTime gameTime)
    {
        Input.Update();
        bool exitRequested = (options.ExitOnGamePadBack
                && GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            || (options.ExitOnEscape && Input.IsKeyDown(Keys.Escape));
        if (exitRequested)
            Exit();
        ApplyAdditions();
        foreach (GameObject item in objects.OrderBy(x => x.UpdateOrder).ThenBy(x => x.Sequence).ToArray()) item.UpdateInternal(gameTime);
        CollisionManager.Step((float)gameTime.ElapsedGameTime.TotalSeconds);
        RemoveDestroyed();
        ApplyPendingWorld();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(options.ClearColor);
        ApplyAdditions();
        context?.ActiveCamera?.ClampToBounds(GraphicsDevice.Viewport);
        DrawSpace(RenderSpace.World, context?.ActiveCamera?.GetViewMatrix(GraphicsDevice.Viewport) ?? Matrix.Identity, gameTime);
        DrawSpace(RenderSpace.Screen, Matrix.Identity, gameTime);
        RemoveDestroyed();
        base.Draw(gameTime);
    }

    private void DrawSpace(RenderSpace space, Matrix matrix, GameTime time)
    {
        SamplerState samplerState = SamplerState.LinearClamp;
        spriteBatch!.Begin(samplerState: samplerState, transformMatrix: matrix);
        foreach (GameObject item in objects.Where(x => x.RenderSpace == space).OrderBy(x => x.DrawOrder).ThenBy(x => x.Sequence).ToArray())
        {
            item.DrawInternal(spriteBatch, time, component =>
            {
                if (ReferenceEquals(component.TextureSampler, samplerState))
                    return;

                spriteBatch.End();
                samplerState = component.TextureSampler;
                spriteBatch.Begin(samplerState: samplerState, transformMatrix: matrix);
            });
        }
        spriteBatch.End();
    }

    protected override void UnloadContent()
    {
        UnloadWorld();
        pendingWorld = null;
        spriteBatch?.Dispose();
        base.UnloadContent();
    }

    private void ApplyPendingWorld()
    {
        if (pendingWorld is null) return;
        IWorld incoming = pendingWorld;
        pendingWorld = null;
        UnloadWorld();
        Input = new InputManager();
        activeWorld = incoming;
        context = new WorldContext(this, Content, GraphicsDevice, Input);
        activeWorld.Load(context);
    }

    private void UnloadWorld()
    {
        if (activeWorld is null) return;
        context!.Deactivate();
        activeWorld.Unload(context);
        foreach (GameObject item in objects.Concat(additions).Distinct()) item.DestroyNow();
        objects.Clear(); additions.Clear();
        CollisionManager.ClearColliders();
        Input.ClearActions();
        activeWorld = null; context = null;
    }

    private void ApplyAdditions()
    {
        if (context is null || additions.Count == 0) return;
        foreach (GameObject item in additions)
        {
            if (item.IsDestroyed)
            {
                item.DestroyNow();
                continue;
            }

            item.Attach(context);
            objects.Add(item);
        }
        additions.Clear();
    }

    private void RemoveDestroyed()
    {
        foreach (GameObject item in objects.Where(x => x.IsDestroyed).ToArray())
        {
            if (ReferenceEquals(context?.ActiveCamera, item))
                context.ActiveCamera = null;

            item.DestroyNow();
            objects.Remove(item);
        }
    }
}
