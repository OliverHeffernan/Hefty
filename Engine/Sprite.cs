using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine;

public class Sprite(Texture2D image, Transform transform, Vector2 size) : GameObject, IDrawable
{
    private readonly Texture2D image = image;
    public Transform Transform { get; } = transform;
    private readonly Vector2 size = size;
    public Color Color { get; set; } = Color.White;

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
