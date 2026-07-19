namespace engine;

using Microsoft.Xna.Framework;
using System;

public class Collider
{
    public Transform Transform { get; }
    public Vector2 Size { get; }
    public Vector2 Offset { get; }
	public Action<Collider> OnCollisionEnter { get; set; }
	public Action<Collider> OnCollisionExit { get; set; }

	public Collider(Transform transform, Vector2 size, Vector2 offset)
	{
		Transform = transform;
		Size = size;
		Offset = offset;
		CollisionManager.RegisterCollider(this);
	}

    public bool Intersects(Collider other)
    {
		return GetBounds().Intersects(other.GetBounds());
    }
	
	public Rectangle GetBounds()
	{
		return new Rectangle(
			(int)(Transform.Position.X + Offset.X),
			(int)(Transform.Position.Y + Offset.Y),
			(int)Size.X,
			(int)Size.Y
		);
	}

}
