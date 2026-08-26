using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;

namespace Hefty.Engine.Collision;

/// <summary>Supported body modes: immovable or controller-driven.</summary>
public enum BodyType
{
    /// <summary>An immovable body that blocks kinematic movement.</summary>
    Static,
    /// <summary>A controller-driven body whose requested movement is collision-resolved.</summary>
    Kinematic
}

/// <summary>A translation-only component registered with physics only while its owner is in a world.</summary>
public sealed class PhysicsBody : Component
{
    private static long nextId;
    private readonly List<Collider> colliders = [];
    private Vector2 pendingMovement;
    private Vector2 velocity;
    private bool registered;

    internal long Id { get; } = Interlocked.Increment(ref nextId);

    /// <summary>Creates a body. Add colliders before or after attaching it.</summary>
    public PhysicsBody(BodyType type) => Type = type;

    /// <summary>Gets the body mode.</summary>
    public BodyType Type { get; }

    /// <summary>Gets a read-only view of owned colliders.</summary>
    public IReadOnlyList<Collider> Colliders => colliders;

    /// <summary>Continuous units-per-second movement for kinematic bodies.</summary>
    public Vector2 Velocity
    {
        get => velocity;
        set
        {
            if (!Finite(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Velocity must be finite.");
            velocity = value;
        }
    }

    /// <summary>Adds an unowned collider and returns it. A collider cannot be shared between bodies.</summary>
    /// <exception cref="ArgumentException">The collider belongs to another body or does not use the owner's transform.</exception>
    public T AddCollider<T>(T collider) where T : Collider
    {
        ArgumentNullException.ThrowIfNull(collider);
        if (colliders.Contains(collider))
            return collider;
        if (collider.BodyInternal is not null)
            throw new ArgumentException("A collider can belong to only one physics body.", nameof(collider));
        if (OwnerInternal is not null && !ReferenceEquals(collider.Transform, Owner.Transform))
            throw new ArgumentException("A collider must use its body's owner transform.", nameof(collider));

        collider.BodyInternal = this;
        colliders.Add(collider);
        if (registered)
        {
            CollisionManager.RegisterCollider(collider);
            CollisionManager.AttachCollider(this, collider);
        }
        return collider;
    }

    /// <summary>Removes and unregisters a collider owned by this body.</summary>
    /// <returns><see langword="true"/> when the collider was owned and removed.</returns>
    public bool RemoveCollider(Collider collider)
    {
        ArgumentNullException.ThrowIfNull(collider);
        if (!colliders.Remove(collider))
            return false;
        if (registered)
            CollisionManager.UnregisterCollider(collider);
        collider.BodyInternal = null;
        return true;
    }

    /// <summary>Queues displacement resolved during this frame's physics step.</summary>
    public void Move(Vector2 displacement)
    {
        if (Type != BodyType.Kinematic)
            throw new InvalidOperationException("Only kinematic bodies move.");
        if (!Finite(displacement))
            throw new ArgumentOutOfRangeException(nameof(displacement), "Movement must be finite.");
        pendingMovement += displacement;
    }

    protected override void OnWorldAttached()
    {
        foreach (Collider collider in colliders)
            if (!ReferenceEquals(collider.Transform, Owner.Transform))
                throw new InvalidOperationException("A collider must use its body's owner transform.");

        registered = true;
        CollisionManager.RegisterBody(this);
        foreach (Collider collider in colliders)
            CollisionManager.RegisterCollider(collider);
    }

    protected override void OnWorldDetached()
    {
        if (!registered)
            return;

        foreach (Collider collider in colliders)
            CollisionManager.UnregisterCollider(collider);
        CollisionManager.UnregisterBody(this);
        registered = false;
    }

    internal Vector2 ConsumeMovement(float seconds)
    {
        Vector2 result = pendingMovement + Velocity * seconds;
        pendingMovement = Vector2.Zero;
        return result;
    }

    internal void RemoveVelocityIntoSurface(Vector2 normal)
    {
        float amount = Vector2.Dot(Velocity, normal);
        if (amount < 0)
            Velocity -= normal * amount;
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
