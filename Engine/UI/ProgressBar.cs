using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine.UI;

public class ProgressBar : UiElement
{
    private float minimum;
    private float maximum;
    private float value;

    public ProgressBar(Vector2 position, Vector2 size, Texture2D texture, float minimum, float maximum,
        float value, Color backgroundColor, Color fillColor, Anchor anchor = Anchor.TopLeft)
        : base(position, size, anchor)
    {
        if (maximum <= minimum) throw new ArgumentException("Maximum must be greater than minimum.");
        Texture = texture;
        this.minimum = minimum;
        this.maximum = maximum;
        this.value = Math.Clamp(value, minimum, maximum);
        BackgroundColor = backgroundColor;
        FillColor = fillColor;
    }

    public Texture2D Texture { get; set; }
    public float Minimum => minimum;
    public float Maximum => maximum;
    public float Value { get => value; set => this.value = Math.Clamp(value, minimum, maximum); }
    public Color BackgroundColor { get; set; }
    public Color FillColor { get; set; }

    public void SetRange(float minimum, float maximum)
    {
        if (maximum <= minimum) throw new ArgumentException("Maximum must be greater than minimum.");
        this.minimum = minimum;
        this.maximum = maximum;
        value = Math.Clamp(value, minimum, maximum);
    }

    protected override void OnDraw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        if (Texture is null) return;
        spriteBatch.Draw(Texture, Bounds, BackgroundColor);
        float ratio = (value - minimum) / (maximum - minimum);
        Rectangle fill = new(Bounds.X, Bounds.Y, (int)MathF.Round(Bounds.Width * ratio), Bounds.Height);
        if (fill.Width > 0) spriteBatch.Draw(Texture, fill, FillColor);
    }
}
