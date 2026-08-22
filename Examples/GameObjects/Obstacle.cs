using Hefty.Engine;
using Hefty.Engine.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Examples.GameObjects;

public class Obstacle : Sprite
{
	public Obstacle(Texture2D texture)
		: base(texture, new Transform(), new Vector2(50f, 50f))
	{
		Color = Color.Red;
		Collider collider = new(Transform, new Vector2(50f, 50f), Vector2.Zero);
		AddComponent(new PhysicsBody(Transform, BodyType.Static, collider));
	}
}
