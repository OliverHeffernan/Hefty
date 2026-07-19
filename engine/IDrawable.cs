using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace engine;

public interface IDrawable
{
    void Draw(SpriteBatch spriteBatch, GameTime gameTime);
}
