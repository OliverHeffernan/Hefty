using System;
using Hefty.Engine.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine;

/// <summary>Provides services and mutation commands valid only for one active world.</summary>
public sealed class WorldContext
{
    private readonly HeftyGame game;
    private Camera2D activeCamera;
    internal WorldContext(HeftyGame game, ContentManager content, GraphicsDevice graphics, InputManager input)
        => (this.game, Content, GraphicsDevice, Input) = (game, content, graphics, input);
    /// <summary>Gets the host content manager. Assets loaded through it are manager-owned.</summary>
    public ContentManager Content { get; }
    /// <summary>Gets the host graphics device.</summary>
    public GraphicsDevice GraphicsDevice { get; }
    /// <summary>Gets the input service. Bindings are cleared when this world unloads.</summary>
    public InputManager Input { get; }
    /// <summary>
    /// Gets or sets the camera used for world-space rendering. Setting this property through a context
    /// whose world has unloaded throws <see cref="InvalidOperationException"/>. A non-null camera must
    /// have been added to this world and must not be destroyed.
    /// </summary>
    public Camera2D ActiveCamera
    {
        get => activeCamera;
        set => game.SetActiveCamera(this, value);
    }
    internal bool IsActive { get; private set; } = true;
    /// <summary>Queues an unattached, never-destroyed object for attachment at the next safe boundary.</summary>
    /// <returns>The supplied object, allowing creation and addition in one expression.</returns>
    /// <exception cref="InvalidOperationException">The context is inactive, or the object is destroyed or already owned.</exception>
    public T Add<T>(T gameObject) where T : GameObject
    {
        game.QueueAdd(this, gameObject);
        return gameObject;
    }
    /// <summary>Requests a deferred world switch. The current update and collision step complete safely.</summary>
    /// <remarks>If several changes are requested before the frame boundary, the last request wins.</remarks>
    public void ChangeWorld(IWorld world) => game.QueueWorld(this, world);
    internal void SetActiveCamera(Camera2D camera) => activeCamera = camera;
    internal void Deactivate() { activeCamera = null; IsActive = false; }
}
