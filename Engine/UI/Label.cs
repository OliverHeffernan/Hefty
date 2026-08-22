using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine.UI;

public class Label(Vector2 position, Vector2 size, SpriteFont font, string text, Color color, Anchor anchor = Anchor.TopLeft)
    : UiElement(position, size, anchor)
{
    public SpriteFont Font { get; set; } = font;
    public string Text { get; set; } = text ?? string.Empty;
    public Color Color { get; set; } = color;
    public Vector2 TextOffset { get; set; }

    protected override void OnDraw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        if (Font is not null && Text.Length != 0)
            spriteBatch.DrawString(Font, Text, new Vector2(Bounds.X, Bounds.Y) + TextOffset, Color);
    }
}
