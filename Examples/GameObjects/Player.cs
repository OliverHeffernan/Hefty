using Hefty.Engine;
using Hefty.Engine.Collision;
using Hefty.Examples.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Examples.GameObjects;

public class Player : Sprite
{
    public Player(Texture2D texture)
        : base(texture, new Transform(), new Vector2(50f, 50f))
    {
        Collider col = new(Transform, new(50f, 50f), Vector2.Zero)
		{
			OnCollisionEnter = _ =>
			{
				System.Console.Write("enter ");
				Color = Color.Red;
			},
			OnCollisionExit = _ =>
			{
				System.Console.Write("exit ");
				Color = Color.White;
			}
		};
        PhysicsBody body = new(Transform, BodyType.Kinematic, col);
        AddComponent(body);
        AddComponent(new PlayerController(body));
    }
}
