using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine.UI;

public enum Anchor
{
    TopLeft, Top, TopRight,
    Left, Center, Right,
    BottomLeft, Bottom, BottomRight
}

public abstract class UiElement
{
    private readonly List<UiElement> children = [];
    private Vector2 size;

    protected UiElement(Vector2 position, Vector2 size, Anchor anchor = Anchor.TopLeft)
    {
        Position = position;
        Size = size;
        Anchor = anchor;
    }

    public Vector2 Position { get; set; }
    public Vector2 Size
    {
        get => size;
        set
        {
            if (value.X < 0 || value.Y < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "UI dimensions cannot be negative.");
            size = value;
        }
    }
    public Anchor Anchor { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public bool IsHovered { get; internal set; }
    public bool IsFocused { get; internal set; }
    public virtual bool IsFocusable => false;
    public UiElement Parent { get; private set; }
    internal UiCanvas OwnerCanvas { get; private set; }
    public IReadOnlyList<UiElement> Children => children;
    public Rectangle Bounds { get; private set; }

    public void AddChild(UiElement child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (child == this || IsAncestorOf(child))
            throw new InvalidOperationException("A UI element cannot contain itself or an ancestor.");
        if (child.OwnerCanvas is not null)
            throw new InvalidOperationException("Remove a UI element from its canvas before reparenting it.");
        child.Parent?.RemoveChild(child);
        child.Parent = this;
        children.Add(child);
        if (OwnerCanvas is not null)
            child.AttachToCanvas(OwnerCanvas);
    }

    public bool RemoveChild(UiElement child)
    {
        if (!children.Remove(child))
            return false;
        if (OwnerCanvas is not null)
        {
            OwnerCanvas.ClearTransient(child);
            child.DetachFromCanvas(OwnerCanvas);
        }
        child.Parent = null;
        return true;
    }

    internal void AttachToCanvas(UiCanvas canvas)
    {
        if (OwnerCanvas is not null && !ReferenceEquals(OwnerCanvas, canvas))
            throw new InvalidOperationException("A UI element cannot belong to multiple canvases.");
        OwnerCanvas = canvas;
        for (int i = 0; i < children.Count; i++)
            children[i].AttachToCanvas(canvas);
    }

    internal void DetachFromCanvas(UiCanvas canvas)
    {
        if (!ReferenceEquals(OwnerCanvas, canvas))
            return;
        OwnerCanvas = null;
        for (int i = 0; i < children.Count; i++)
            children[i].DetachFromCanvas(canvas);
    }

    internal void Layout(Rectangle parentBounds)
    {
        Vector2 origin = AnchorPoint(parentBounds, Anchor);
        Vector2 pivot = AnchorPivot(Size, Anchor);
        Bounds = new Rectangle(
            (int)MathF.Round(origin.X + Position.X - pivot.X),
            (int)MathF.Round(origin.Y + Position.Y - pivot.Y),
            (int)MathF.Round(Size.X), (int)MathF.Round(Size.Y));
        OnLayout();
        for (int i = 0; i < children.Count; i++)
            children[i].Layout(Bounds);
    }

    internal void UpdateTree(GameTime gameTime)
    {
        if (!Visible || !Enabled)
            return;
        OnUpdate(gameTime);
        for (int i = 0; i < children.Count; i++)
            children[i].UpdateTree(gameTime);
    }

    internal void DrawTree(SpriteBatch spriteBatch, GameTime gameTime)
    {
        if (!Visible)
            return;
        OnDraw(spriteBatch, gameTime);
        for (int i = 0; i < children.Count; i++)
            children[i].DrawTree(spriteBatch, gameTime);
    }

    internal UiElement HitTest(Point point)
    {
        if (!Visible || !Enabled || !Bounds.Contains(point))
            return null;
        for (int i = children.Count - 1; i >= 0; i--)
        {
            UiElement hit = children[i].HitTest(point);
            if (hit is not null)
                return hit;
        }
        return this;
    }

    internal void CollectFocusable(List<UiElement> destination)
    {
        if (!Visible || !Enabled)
            return;
        if (IsFocusable)
            destination.Add(this);
        for (int i = 0; i < children.Count; i++)
            children[i].CollectFocusable(destination);
    }

    internal virtual void Activate() { }
    protected virtual void OnLayout() { }
    protected virtual void OnUpdate(GameTime gameTime) { }
    protected virtual void OnDraw(SpriteBatch spriteBatch, GameTime gameTime) { }

    private bool IsAncestorOf(UiElement candidate)
    {
        for (UiElement current = Parent; current is not null; current = current.Parent)
            if (current == candidate) return true;
        return false;
    }

    private static Vector2 AnchorPoint(Rectangle bounds, Anchor anchor) => anchor switch
    {
        Anchor.TopLeft or Anchor.Left or Anchor.BottomLeft => new(bounds.Left, Vertical(bounds, anchor)),
        Anchor.TopRight or Anchor.Right or Anchor.BottomRight => new(bounds.Right, Vertical(bounds, anchor)),
        _ => new(bounds.Center.X, Vertical(bounds, anchor))
    };

    private static float Vertical(Rectangle bounds, Anchor anchor) => anchor switch
    {
        Anchor.TopLeft or Anchor.Top or Anchor.TopRight => bounds.Top,
        Anchor.BottomLeft or Anchor.Bottom or Anchor.BottomRight => bounds.Bottom,
        _ => bounds.Center.Y
    };

    private static Vector2 AnchorPivot(Vector2 size, Anchor anchor) => anchor switch
    {
        Anchor.TopLeft => Vector2.Zero,
        Anchor.Top => new(size.X / 2, 0),
        Anchor.TopRight => new(size.X, 0),
        Anchor.Left => new(0, size.Y / 2),
        Anchor.Center => size / 2,
        Anchor.Right => new(size.X, size.Y / 2),
        Anchor.BottomLeft => new(0, size.Y),
        Anchor.Bottom => new(size.X / 2, size.Y),
        _ => size
    };
}
