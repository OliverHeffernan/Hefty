using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace engine;

public class Camera2D : GameObject
{
    private float zoom = 1f;

    public Transform Transform { get; } = new();

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

    public float Rotation { get; set; }

    /// <summary>The camera's optional world-space limits. Rotation is not considered when clamping.</summary>
    public Rectangle? Bounds { get; set; }

    public Matrix GetViewMatrix(Viewport viewport)
    {
        return Matrix.CreateTranslation(-Transform.Position.X, -Transform.Position.Y, 0f)
            * Matrix.CreateRotationZ(-Rotation)
            * Matrix.CreateScale(Zoom, Zoom, 1f)
            * Matrix.CreateTranslation(viewport.Width / 2f, viewport.Height / 2f, 0f);
    }

    public Vector2 WorldToScreen(Vector2 world, Viewport viewport)
    {
        return Vector2.Transform(world, GetViewMatrix(viewport));
    }

    public Vector2 ScreenToWorld(Vector2 screen, Viewport viewport)
    {
        return Vector2.Transform(screen, Matrix.Invert(GetViewMatrix(viewport)));
    }

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
