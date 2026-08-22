using System;
using Microsoft.Xna.Framework;

namespace Hefty.Engine.Collision;

/// <summary>
/// Floating-point axis-aligned bounds used by the physics solver.
/// </summary>
internal readonly record struct Aabb(float Left, float Top, float Right, float Bottom)
{
    public float Width => Right - Left;
    public float Height => Bottom - Top;
    public Vector2 Center => new((Left + Right) * 0.5f, (Top + Bottom) * 0.5f);

    public Aabb Translate(Vector2 amount) =>
        new(Left + amount.X, Top + amount.Y, Right + amount.X, Bottom + amount.Y);

    public Aabb Expand(float amount) =>
        new(Left - amount, Top - amount, Right + amount, Bottom + amount);

    public bool Overlaps(Aabb other, float tolerance = 0f) =>
        Right >= other.Left - tolerance &&
        Left <= other.Right + tolerance &&
        Bottom >= other.Top - tolerance &&
        Top <= other.Bottom + tolerance;

    public bool StrictlyOverlaps(Aabb other) =>
        Right > other.Left &&
        Left < other.Right &&
        Bottom > other.Top &&
        Top < other.Bottom;

    public static Aabb Union(Aabb first, Aabb second) => new(
        MathF.Min(first.Left, second.Left),
        MathF.Min(first.Top, second.Top),
        MathF.Max(first.Right, second.Right),
        MathF.Max(first.Bottom, second.Bottom));

    public Rectangle ToRectangle()
    {
        int left = (int)MathF.Floor(Left);
        int top = (int)MathF.Floor(Top);
        int right = (int)MathF.Ceiling(Right);
        int bottom = (int)MathF.Ceiling(Bottom);

        return new Rectangle(
            left,
            top,
            Math.Max(1, right - left),
            Math.Max(1, bottom - top));
    }
}
