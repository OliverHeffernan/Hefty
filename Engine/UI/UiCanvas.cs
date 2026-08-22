using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine.UI;

public sealed class UiCanvas : GameObject, IDrawable
{
    private sealed class CanvasUpdater(UiCanvas canvas) : IUpdater
    {
        public void Update(GameTime gameTime) => canvas.UpdateCanvas(gameTime);
        public int CompareTo(object obj) => 0;
    }

    private readonly GraphicsDevice graphicsDevice;
    private readonly IUiInputSource input;
    private readonly List<UiElement> children = [];
    private readonly List<UiElement> focusable = [];
    private UiElement hovered;
    private UiElement focused;
    private UiElement pressed;

    public UiCanvas(GraphicsDevice graphicsDevice, IUiInputSource input)
    {
        this.graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        this.input = input ?? throw new ArgumentNullException(nameof(input));
        AddComponent(new CanvasUpdater(this));
    }

    public IReadOnlyList<UiElement> Children => children;
    public UiElement FocusedElement => focused;
    public string ConfirmAction { get; set; } = "UiConfirm";
    public string NextAction { get; set; } = "UiNext";
    public string PreviousAction { get; set; } = "UiPrevious";

    public void Add(UiElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (element.Parent is not null)
            throw new InvalidOperationException("Remove the element from its parent before adding it to a canvas.");
        if (children.Contains(element))
            return;
        if (element.OwnerCanvas is not null)
            throw new InvalidOperationException("Remove the UI element from its canvas before adding it elsewhere.");
        element.AttachToCanvas(this);
        children.Add(element);
    }

    public bool Remove(UiElement element)
    {
        bool removed = children.Remove(element);
        if (removed && (IsInSubtree(element, focused)
            || IsInSubtree(element, hovered)
            || IsInSubtree(element, pressed)))
            ClearTransient(element);
        if (removed)
            element.DetachFromCanvas(this);
        return removed;
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        for (int i = 0; i < children.Count; i++)
            children[i].DrawTree(spriteBatch, gameTime);
    }

    public UiElement HitTest(Point screenPoint)
    {
        for (int i = children.Count - 1; i >= 0; i--)
        {
            UiElement hit = children[i].HitTest(screenPoint);
            if (hit is not null) return hit;
        }
        return null;
    }

    private void UpdateCanvas(GameTime gameTime)
    {
        Viewport viewport = graphicsDevice.Viewport;
        Rectangle screen = new(0, 0, viewport.Width, viewport.Height);
        focusable.Clear();
        for (int i = 0; i < children.Count; i++)
        {
            children[i].Layout(screen);
            children[i].CollectFocusable(focusable);
        }
        if (focused is not null && !focusable.Contains(focused))
            SetFocus(null);

        SetHovered(HitTest(input.MousePosition));
        if (input.IsMousePressed)
        {
            pressed = hovered;
            if (hovered?.IsFocusable == true) SetFocus(hovered);
        }
        if (input.IsMouseReleased)
        {
            if (pressed is not null && pressed == hovered)
                pressed.Activate();
            pressed = null;
        }

        if (input.IsPressed(NextAction)) MoveFocus(1);
        else if (input.IsPressed(PreviousAction)) MoveFocus(-1);
        if (input.IsPressed(ConfirmAction))
        {
            if (IsFocusableAndInteractive(focused))
                focused.Activate();
            else
                SetFocus(null);
        }

        for (int i = 0; i < children.Count; i++)
            children[i].UpdateTree(gameTime);
    }

    private void MoveFocus(int direction)
    {
        if (focusable.Count == 0) { SetFocus(null); return; }
        int index = focused is null ? (direction > 0 ? -1 : 0) : focusable.IndexOf(focused);
        index = (index + direction + focusable.Count) % focusable.Count;
        SetFocus(focusable[index]);
    }

    private void SetHovered(UiElement element)
    {
        if (hovered == element) return;
        if (hovered is not null) hovered.IsHovered = false;
        hovered = element;
        if (hovered is not null) hovered.IsHovered = true;
    }

    private void SetFocus(UiElement element)
    {
        if (focused == element) return;
        if (focused is not null) focused.IsFocused = false;
        focused = element;
        if (focused is not null) focused.IsFocused = true;
    }

    private void ClearTransient(UiElement element)
    {
        if (IsInSubtree(element, focused)) SetFocus(null);
        if (IsInSubtree(element, hovered)) SetHovered(null);
        if (IsInSubtree(element, pressed)) pressed = null;
    }

    private static bool IsInSubtree(UiElement root, UiElement element)
    {
        for (UiElement current = element; current is not null; current = current.Parent)
            if (ReferenceEquals(current, root))
                return true;
        return false;
    }

    private bool IsFocusableAndInteractive(UiElement element)
    {
        if (element is null || !focusable.Contains(element)
            || !ReferenceEquals(element.OwnerCanvas, this))
            return false;

        for (UiElement current = element; current is not null; current = current.Parent)
            if (!current.Visible || !current.Enabled)
                return false;
        return true;
    }
}
