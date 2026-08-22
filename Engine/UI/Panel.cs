using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine.UI;

public class Panel(Vector2 position, Vector2 size, Texture2D texture, Color color, Anchor anchor = Anchor.TopLeft)
    : UiElement(position, size, anchor)
{
    public Texture2D Texture { get; set; } = texture;
    public Color Color { get; set; } = color;

    protected override void OnDraw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        if (Texture is not null)
            spriteBatch.Draw(Texture, Bounds, Color);
    }
}
