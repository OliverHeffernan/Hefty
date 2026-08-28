using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine;

/// <summary>An owned world entity with a universal transform and safely mutable component set.</summary>
public class GameObject
{
    private readonly List<Component> components = [];
    private readonly List<Component> pendingAdd = [];
    private readonly List<Component> pendingRemove = [];
    private bool iterating;
    internal long Sequence { get; set; }
    internal WorldContext? WorldInternal { get; private set; }
    /// <summary>Optional diagnostic name.</summary>
    public string? Name { get; set; }
    /// <summary>The object's permanent transform.</summary>
    public Transform Transform { get; } = new();
    /// <summary>Controls component updates.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>Controls component drawing.</summary>
    public bool Visible { get; set; } = true;
    /// <summary>Reports whether destruction was requested or completed.</summary>
    public bool IsDestroyed { get; private set; }
    /// <summary>Selects camera-transformed or screen rendering.</summary>
    public RenderSpace RenderSpace { get; set; } = RenderSpace.World;
    /// <summary>Orders this object during update.</summary>
    public int UpdateOrder { get; set; }
    /// <summary>Orders this object during drawing.</summary>
    public int DrawOrder { get; set; }
    /// <summary>Gets the active context after attachment.</summary>
    public WorldContext World => WorldInternal ?? throw new InvalidOperationException("The object is not attached to a world.");
    /// <summary>Adds an unowned component, deferring attachment when iteration is in progress.</summary>
    public T AddComponent<T>(T component) where T : Component
    {
        ArgumentNullException.ThrowIfNull(component);
        if (IsDestroyed) throw new InvalidOperationException("Cannot modify a destroyed object.");
        if (component.OwnerInternal is not null || pendingAdd.Contains(component)) throw new InvalidOperationException("Components cannot be shared.");
        if (iterating) pendingAdd.Add(component); else AttachComponent(component);
        return component;
    }
    /// <summary>Gets the first assignable component, or null.</summary>
    public T? GetComponent<T>() where T : Component => components.OfType<T>().FirstOrDefault();
    /// <summary>Attempts to get the first assignable component.</summary>
    public bool TryGetComponent<T>([NotNullWhen(true)] out T? component) where T : Component { component = GetComponent<T>(); return component is not null; }
    /// <summary>Removes a component. Removal during update/draw occurs after that pass.</summary>
    public bool RemoveComponent(Component component)
    {
        ArgumentNullException.ThrowIfNull(component);
        if (!components.Contains(component)) return pendingAdd.Remove(component);
        if (iterating) { if (!pendingRemove.Contains(component)) pendingRemove.Add(component); }
        else DetachComponent(component);
        return true;
    }
    /// <summary>
    /// Idempotently requests destruction at the next engine boundary. An object that has not entered a
    /// world is cleaned up immediately; an attached object receives no further updates or drawing.
    /// </summary>
    public void Destroy()
    {
        if (IsDestroyed)
            return;

        IsDestroyed = true;
        if (WorldInternal is null)
            DestroyNow();
    }
    internal void Attach(WorldContext world) { WorldInternal = world; Flush(); foreach (Component c in components.ToArray()) c.InvokeWorldAttached(); }
    internal void UpdateInternal(GameTime time) { if (!Enabled || IsDestroyed) return; Iterate(components.OrderBy(c => c.UpdateOrder), c => { if (c.Enabled) c.InvokeUpdate(time); }); }
    internal void DrawInternal(SpriteBatch batch, GameTime time, Action<Component> prepareDraw) { if (!Visible || IsDestroyed) return; Iterate(components.OrderBy(c => c.DrawOrder), c => { if (c.Enabled) { prepareDraw(c); c.InvokeDraw(batch, time); } }); }
    internal void DestroyNow()
    {
        IsDestroyed = true;
        foreach (Component c in components.ToArray()) DetachComponent(c);
        pendingAdd.Clear(); pendingRemove.Clear(); WorldInternal = null;
    }
    private void Iterate(IEnumerable<Component> source, Action<Component> action)
    {
        iterating = true;
        foreach (Component component in source.ToArray())
        {
            if (IsDestroyed)
                break;
            if (!pendingRemove.Contains(component))
                action(component);
        }
        iterating = false;
        Flush();
    }
    private void Flush()
    {
        foreach (Component component in pendingRemove.ToArray())
            DetachComponent(component);
        pendingRemove.Clear();

        if (IsDestroyed)
        {
            pendingAdd.Clear();
            return;
        }

        foreach (Component component in pendingAdd.ToArray())
            AttachComponent(component);
        pendingAdd.Clear();
    }
    private void AttachComponent(Component c) { components.Add(c); c.Attach(this); if (WorldInternal is not null) c.InvokeWorldAttached(); }
    private void DetachComponent(Component c) { if (!components.Remove(c)) return; if (WorldInternal is not null) c.InvokeWorldDetached(); c.Detach(); }
}
