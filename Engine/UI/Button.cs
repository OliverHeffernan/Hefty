using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine.UI;

public class Button : UiElement
{
    public Button(Vector2 position, Vector2 size, Texture2D texture, SpriteFont font, string text,
        Color normalColor, Color hoverColor, Color focusedColor, Color disabledColor,
        Color textColor, Anchor anchor = Anchor.TopLeft) : base(position, size, anchor)
    {
        Texture = texture;
        Font = font;
        Text = text ?? string.Empty;
        NormalColor = normalColor;
        HoverColor = hoverColor;
        FocusedColor = focusedColor;
        DisabledColor = disabledColor;
        TextColor = textColor;
    }

    public override bool IsFocusable => true;
    public Texture2D Texture { get; set; }
    public SpriteFont Font { get; set; }
    public string Text { get; set; }
    public Color NormalColor { get; set; }
    public Color HoverColor { get; set; }
    public Color FocusedColor { get; set; }
    public Color DisabledColor { get; set; }
    public Color TextColor { get; set; }
    public event Action<Button> Activated;

    internal override void Activate()
    {
        if (Enabled) Activated?.Invoke(this);
    }

    protected override void OnDraw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        Color background = !Enabled ? DisabledColor : IsHovered ? HoverColor : IsFocused ? FocusedColor : NormalColor;
        if (Texture is not null) spriteBatch.Draw(Texture, Bounds, background);
        if (Font is null || Text.Length == 0) return;
        Vector2 measured = Font.MeasureString(Text);
        Vector2 location = new(Bounds.Center.X - measured.X / 2, Bounds.Center.Y - measured.Y / 2);
        spriteBatch.DrawString(Font, Text, location, TextColor);
    }
}
