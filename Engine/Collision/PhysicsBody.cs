using System;
using System.Collections.Generic;
using System.Threading;
using Hefty.Engine;
using Microsoft.Xna.Framework;

namespace Hefty.Engine.Collision;

public enum BodyType { Static, Kinematic }

/// <summary>A translation-only body. Controllers submit motion; CollisionManager applies it.</summary>
public sealed class PhysicsBody : Component
{
    private static long nextId;
    private readonly List<Collider> colliders = [];
    private Vector2 pendingMovement;
    private bool destroyed;

    internal long Id { get; } = Interlocked.Increment(ref nextId);
    public Transform Transform { get; }
    public BodyType Type { get; }
    public IReadOnlyList<Collider> Colliders => colliders;
    public Vector2 Velocity
    {
        get => velocity;
        set
        {
            if (!IsFinite(value.X) || !IsFinite(value.Y))
                throw new ArgumentOutOfRangeException(nameof(value), "Velocity must be finite.");
            velocity = value;
        }
    }
    public bool ToDestroy => destroyed;
    private Vector2 velocity;

    public PhysicsBody(Transform transform, BodyType type, params Collider[] colliders)
    {
        Transform = transform ?? throw new ArgumentNullException(nameof(transform));
        Type = type;
        CollisionManager.RegisterBody(this);
        foreach (Collider collider in colliders)
            AddCollider(collider);
    }

    public void AddCollider(Collider collider)
    {
        ArgumentNullException.ThrowIfNull(collider);
        if (destroyed)
            throw new ObjectDisposedException(nameof(PhysicsBody));
        if (!ReferenceEquals(collider.Transform, Transform))
            throw new ArgumentException("A body's colliders must share its Transform.", nameof(collider));
        if (!colliders.Contains(collider))
        {
            CollisionManager.AttachCollider(this, collider);
            colliders.Add(collider);
        }
    }

    public void Move(Vector2 displacement)
    {
        if (Type != BodyType.Kinematic)
            throw new InvalidOperationException("Only kinematic bodies accept movement.");
        if (!IsFinite(displacement.X) || !IsFinite(displacement.Y))
            throw new ArgumentOutOfRangeException(nameof(displacement), "Movement must be finite.");
        pendingMovement += displacement;
    }

    internal Vector2 ConsumeMovement(float seconds)
    {
        Vector2 result = pendingMovement + Velocity * seconds;
        pendingMovement = Vector2.Zero;
        return result;
    }

    internal void RemoveVelocityIntoSurface(Vector2 normal)
    {
        float intoSurface = Vector2.Dot(Velocity, normal);
        if (intoSurface < 0f)
            Velocity -= normal * intoSurface;
    }

    public void CleanUp() => Destroy();
    public void Destroy()
    {
        if (destroyed) return;
        destroyed = true;
        foreach (Collider collider in colliders.ToArray())
            CollisionManager.UnregisterCollider(collider);
        CollisionManager.UnregisterBody(this);
        colliders.Clear();
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
