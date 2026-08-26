using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine;

/// <summary>Draws a texture using its owner's position and scale.</summary>
public sealed class SpriteRenderer : Component
{
    /// <summary>Creates a renderer with an unowned texture and unscaled destination size.</summary>
    public SpriteRenderer(Texture2D texture, Vector2 size) { Texture = texture ?? throw new ArgumentNullException(nameof(texture)); Size = size; }
    /// <summary>The texture to draw; its lifetime remains caller/content-manager owned.</summary>
    public Texture2D Texture { get; set; }
    /// <summary>Destination size before transform scale.</summary>
    public Vector2 Size { get; set; }
    /// <summary>Draw tint.</summary>
    public Color Color { get; set; } = Color.White;
    /// <summary>Optional texture region.</summary>
    public Rectangle? SourceRectangle { get; set; }
    protected override void Draw(SpriteBatch batch, GameTime time) => batch.Draw(Texture,
        new Rectangle((int)Transform.Position.X, (int)Transform.Position.Y, (int)(Size.X * Transform.Scale.X), (int)(Size.Y * Transform.Scale.Y)), SourceRectangle, Color);
}
