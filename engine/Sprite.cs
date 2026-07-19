using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace engine;

public class Sprite : GameObject, IDrawable
{
    // Store the image of the sprite.
    private readonly Texture2D image;
    public Transform Transform { get; }
    private readonly Vector2 size;
	public Color Color { get; set; } = Color.White;

    public Sprite(Texture2D image, Transform transform, Vector2 size)
    {
        this.image = image;
        Transform = transform;
        this.size = size;
    }

    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        spriteBatch.Draw(
            image,
            new Rectangle(
                (int)Transform.Position.X,
                (int)Transform.Position.Y,
                (int)(size.X * Transform.Scale.X),
                (int)(size.Y * Transform.Scale.Y)
            ),
			Color
        );
    }

}
