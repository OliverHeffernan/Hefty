using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine;

/// <summary>A world object that maps world coordinates into the current viewport.</summary>
public class Camera2D : GameObject
{
    private float zoom = 1f;

    /// <summary>Gets or sets the magnification. Values must be greater than zero.</summary>
    public float Zoom
    {
        get => zoom;
        set
        {
            if (value <= 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Camera zoom must be greater than zero."
                );

            zoom = value;
        }
    }

    /// <summary>Gets or sets clockwise camera rotation in radians.</summary>
    public float Rotation { get; set; }

    /// <summary>The camera's optional world-space limits. Rotation is not considered when clamping.</summary>
    public Rectangle? Bounds { get; set; }

    /// <summary>Creates the world-to-screen matrix for a viewport.</summary>
    public Matrix GetViewMatrix(Viewport viewport)
    {
        return Matrix.CreateTranslation(-Transform.Position.X, -Transform.Position.Y, 0f)
            * Matrix.CreateRotationZ(-Rotation)
            * Matrix.CreateScale(Zoom, Zoom, 1f)
            * Matrix.CreateTranslation(viewport.Width / 2f, viewport.Height / 2f, 0f);
    }

    /// <summary>Converts a point from world coordinates to viewport coordinates.</summary>
    public Vector2 WorldToScreen(Vector2 world, Viewport viewport)
    {
        return Vector2.Transform(world, GetViewMatrix(viewport));
    }

    /// <summary>Converts a point from viewport coordinates to world coordinates.</summary>
    public Vector2 ScreenToWorld(Vector2 screen, Viewport viewport)
    {
        return Vector2.Transform(screen, Matrix.Invert(GetViewMatrix(viewport)));
    }

    /// <summary>Moves the camera inside <see cref="Bounds"/>, accounting for viewport size and zoom.</summary>
    public void ClampToBounds(Viewport viewport)
    {
        if (Bounds is not Rectangle bounds)
            return;

        float halfWidth = viewport.Width / (2f * Zoom);
        float halfHeight = viewport.Height / (2f * Zoom);
        float x = ClampAxis(Transform.Position.X, bounds.Left, bounds.Right, halfWidth);
        float y = ClampAxis(Transform.Position.Y, bounds.Top, bounds.Bottom, halfHeight);
        Transform.Position = new Vector2(x, y);
    }

    private static float ClampAxis(float value, float minimum, float maximum, float halfVisible)
    {
        if (maximum - minimum <= halfVisible * 2f)
            return (minimum + maximum) / 2f;

        return MathHelper.Clamp(value, minimum + halfVisible, maximum - halfVisible);
    }
}
