using System;
using System.Threading;
using Microsoft.Xna.Framework;

namespace Hefty.Engine.Collision;

/// <summary>An axis-aligned shape owned by one <see cref="PhysicsBody"/>.</summary>
public class Collider
{
    private static long nextId;
    private uint layer;
    private uint collisionMask;

    internal long Id { get; } = Interlocked.Increment(ref nextId);
    internal PhysicsBody? BodyInternal { get; set; }
    /// <summary>Gets the transform used to position this collider.</summary>
    public Transform Transform { get; }
    /// <summary>Gets the collider dimensions in world units.</summary>
    public Vector2 Size { get; }
    /// <summary>Gets the offset from the transform position in world units.</summary>
    public Vector2 Offset { get; }

    /// <summary>Raised on the first frame of contact.</summary>
    public event Action<Collider>? CollisionEntered;
    /// <summary>Raised while contact continues.</summary>
    public event Action<Collider>? CollisionStayed;
    /// <summary>Raised when contact ends.</summary>
    public event Action<Collider>? CollisionExited;
    /// <summary>Gets or sets whether contacts report events without blocking movement.</summary>
    public bool IsTrigger { get; set; }
    /// <summary>Gets or sets the single collision layer bit occupied by this collider.</summary>
    public uint Layer
    {
        get => layer;
        set
        {
            if (value == 0 || (value & (value - 1)) != 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Layer must contain exactly one bit.");
            layer = value;
        }
    }
    /// <summary>Gets or sets the layer bits this collider accepts contact with.</summary>
    public uint CollisionMask
    {
        get => collisionMask;
        set => collisionMask = value;
    }

    /// <summary>Creates an axis-aligned collider. Add it to a <see cref="PhysicsBody"/> to activate it.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Size is non-positive/non-finite, offset is non-finite, or layer is not one bit.</exception>
    public Collider(
        Transform transform,
        Vector2 size,
        Vector2 offset,
        uint layer = 1,
        uint collisionMask = uint.MaxValue,
        bool isTrigger = false)
    {
        ArgumentNullException.ThrowIfNull(transform);
        if (!IsFinite(size.X) || !IsFinite(size.Y) || size.X <= 0 || size.Y <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Collider size must be finite and positive.");
        if (!IsFinite(offset.X) || !IsFinite(offset.Y))
            throw new ArgumentOutOfRangeException(nameof(offset), "Collider offset must be finite.");

        Transform = transform;
        Size = size;
        Offset = offset;
        Layer = layer;
        CollisionMask = collisionMask;
        IsTrigger = isTrigger;
    }

    /// <summary>Returns whether this collider strictly overlaps another collider at their current transforms.</summary>
    public bool Intersects(Collider other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return GetFloatBounds().StrictlyOverlaps(other.GetFloatBounds());
    }

    /// <summary>Gets a conservative integer rectangle containing the collider's current bounds.</summary>
    public Rectangle GetBounds() => GetFloatBounds().ToRectangle();

    internal Aabb GetFloatBounds() => new(
        Transform.Position.X + Offset.X,
        Transform.Position.Y + Offset.Y,
        Transform.Position.X + Offset.X + Size.X,
        Transform.Position.Y + Offset.Y + Size.Y);

    internal void RaiseEntered(Collider other) => CollisionEntered?.Invoke(other);
    internal void RaiseStayed(Collider other) => CollisionStayed?.Invoke(other);
    internal void RaiseExited(Collider other) => CollisionExited?.Invoke(other);

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

}
