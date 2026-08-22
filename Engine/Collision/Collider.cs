using System;
using System.Threading;
using Microsoft.Xna.Framework;

namespace Hefty.Engine.Collision;

public class Collider : IDestroyable
{
    private static long nextId;
    private uint layer;
    private uint collisionMask;

    internal long Id { get; } = Interlocked.Increment(ref nextId);
    public Transform Transform { get; }
    public Vector2 Size { get; }
    public Vector2 Offset { get; }
	public Action<Collider> OnCollisionEnter { get; set; }
	public Action<Collider> OnCollisionStay { get; set; }
	public Action<Collider> OnCollisionExit { get; set; }
	public bool IsTrigger { get; set; }
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
	public uint CollisionMask
	{
		get => collisionMask;
		set => collisionMask = value;
	}

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
		CollisionManager.RegisterCollider(this);
	}

    public bool Intersects(Collider other)
    {
		ArgumentNullException.ThrowIfNull(other);
		return GetFloatBounds().StrictlyOverlaps(other.GetFloatBounds());
    }
	
    public Rectangle GetBounds() => GetFloatBounds().ToRectangle();

    internal Aabb GetFloatBounds() => new(
        Transform.Position.X + Offset.X,
        Transform.Position.Y + Offset.Y,
        Transform.Position.X + Offset.X + Size.X,
        Transform.Position.Y + Offset.Y + Size.Y);

    public void CleanUp() => CollisionManager.UnregisterCollider(this);
    public void Destroy() => CleanUp();

	private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

}
