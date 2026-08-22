using System;
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
