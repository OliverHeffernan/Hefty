using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine;

/// <summary>Base class for user behavior and rendering attached exclusively to one object.</summary>
public abstract class Component
{
    private GameObject? owner;

    /// <summary>Gets the owner after attachment.</summary>
    /// <exception cref="InvalidOperationException">The component is not attached to an object.</exception>
    public GameObject Owner => owner ?? throw new InvalidOperationException("The component has no owner.");
    internal GameObject? OwnerInternal => owner;
    /// <summary>Gets the owner's universal transform.</summary>
    public Transform Transform => Owner.Transform;
    /// <summary>Gets the active world context. Access while detached throws.</summary>
    public WorldContext World => Owner.World;
    /// <summary>Controls update and draw participation.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>Orders this component's update relative to siblings.</summary>
    public int UpdateOrder { get; set; }
    /// <summary>Orders this component's draw relative to siblings.</summary>
    public int DrawOrder { get; set; }
    internal void Attach(GameObject gameObject) { if (owner is not null) throw new InvalidOperationException("Components cannot be shared."); owner = gameObject; OnAdded(); }
    internal void Detach() { if (owner is null) return; OnRemoved(); owner = null; }
    internal void InvokeUpdate(GameTime time) => Update(time);
    internal void InvokeDraw(SpriteBatch batch, GameTime time) => Draw(batch, time);
    internal virtual SamplerState TextureSampler => SamplerState.LinearClamp;
    internal void InvokeWorldAttached() => OnWorldAttached();
    internal void InvokeWorldDetached() => OnWorldDetached();
    /// <summary>Called exactly once when attached to an object. World may be unavailable until that object is added.</summary>
    protected virtual void OnAdded() { }
    /// <summary>Called when the owner enters a world. <see cref="World"/> is available during this callback.</summary>
    protected virtual void OnWorldAttached() { }
    /// <summary>Called before the owner leaves its world, while <see cref="World"/> is still available.</summary>
    protected virtual void OnWorldDetached() { }
    /// <summary>Called exactly once before detachment, including during destruction.</summary>
    protected virtual void OnRemoved() { }
    /// <summary>Called by the engine while this component and its owner are enabled.</summary>
    protected virtual void Update(GameTime gameTime) { }
    /// <summary>Called by the engine while enabled and the owner is visible.</summary>
    protected virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime) { }
}
