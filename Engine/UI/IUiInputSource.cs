using System;
using Hefty.Engine.Input;
using Microsoft.Xna.Framework;

namespace Hefty.Engine.UI;

public interface IUiInputSource
{
    Point MousePosition { get; }
    bool IsMousePressed { get; }
    bool IsMouseHeld { get; }
    bool IsMouseReleased { get; }
    bool IsPressed(string action);
    bool IsHeld(string action);
    bool IsReleased(string action);
}

/// <summary>Adapts sampled input without polling devices. Bind canvas actions to keyboard, pad buttons or axes.</summary>
public sealed class InputManagerUiInputSource(InputManager input) : IUiInputSource
{
    private readonly InputManager input = input ?? throw new ArgumentNullException(nameof(input));
    public Point MousePosition => input.MousePosition;
    public bool IsMousePressed => input.IsMouseButtonPressed(MouseButton.Left);
    public bool IsMouseHeld => input.IsMouseButtonDown(MouseButton.Left);
    public bool IsMouseReleased => input.IsMouseButtonReleased(MouseButton.Left);
    public bool IsPressed(string action) => input.IsPressed(action);
    public bool IsHeld(string action) => input.IsHeld(action);
    public bool IsReleased(string action) => input.IsReleased(action);
}

public sealed class DelegateUiInputSource(
    Func<Point> mousePosition,
    Func<bool> mousePressed,
    Func<bool> mouseHeld,
    Func<bool> mouseReleased,
    Func<string, bool> isPressed,
    Func<string, bool> isHeld,
    Func<string, bool> isReleased) : IUiInputSource
{
    public Point MousePosition => mousePosition();
    public bool IsMousePressed => mousePressed();
    public bool IsMouseHeld => mouseHeld();
    public bool IsMouseReleased => mouseReleased();
    public bool IsPressed(string action) => isPressed(action);
    public bool IsHeld(string action) => isHeld(action);
    public bool IsReleased(string action) => isReleased(action);
}
