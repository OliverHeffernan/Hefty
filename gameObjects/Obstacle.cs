using engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Obstacle : Sprite
{
	public Obstacle(Texture2D texture)
		: base(texture, new Transform(), new Vector2(50f, 50f))
	{
		Color = Color.Red;
		new Collider(Transform, new Vector2(50f, 50f), new Vector2(0f, 0f));
	}
}
