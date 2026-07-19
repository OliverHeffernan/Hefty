using Hefty.Engine;
using Hefty.Examples.Components;
using Hefty.Engine.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Examples.GameObjects;

public class Player : Sprite
{
    public Player(Texture2D texture)
        : base(texture, new Transform(), new Vector2(50f, 50f))
    {
        AddComponent(new PlayerController(Transform));
		Collider col = new(Transform, new Vector2(50f, 50f), new Vector2(0f, 0f));
		col.OnCollisionEnter = other => {
			System.Console.Write("enter ");
			Color = Color.Red;
		};
		col.OnCollisionExit = other => {
			System.Console.Write("exit ");
			Color = Color.White;
		};

    }
}
