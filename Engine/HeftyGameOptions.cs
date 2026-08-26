using System;
using Microsoft.Xna.Framework;

namespace Hefty.Engine;

/// <summary>Configures the MonoGame host before its graphics device is initialized.</summary>
public sealed class HeftyGameOptions
{
    /// <summary>Gets the preferred back-buffer width in pixels.</summary>
    public int BackBufferWidth { get; init; } = 800;

    /// <summary>Gets the preferred back-buffer height in pixels.</summary>
    public int BackBufferHeight { get; init; } = 480;

    /// <summary>Gets whether the game starts in full-screen mode.</summary>
    public bool IsFullScreen { get; init; }

    /// <summary>Gets whether MonoGame displays the system mouse pointer over the game window.</summary>
    public bool IsMouseVisible { get; init; } = true;

    /// <summary>Gets whether MonoGame uses its fixed-time-step update loop.</summary>
    public bool IsFixedTimeStep { get; init; } = true;

    /// <summary>Gets whether pressing Escape exits the game.</summary>
    public bool ExitOnEscape { get; init; } = true;

    /// <summary>Gets whether pressing the primary gamepad's Back button exits the game.</summary>
    public bool ExitOnGamePadBack { get; init; } = true;

    /// <summary>Gets the color used to clear the back buffer before drawing each frame.</summary>
    public Color ClearColor { get; init; } = Color.CornflowerBlue;

    /// <summary>Gets the root directory used by the MonoGame content manager.</summary>
    public string ContentRootDirectory { get; init; } = "Content";

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(BackBufferWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(BackBufferHeight);
        ArgumentException.ThrowIfNullOrWhiteSpace(ContentRootDirectory);
    }
}
