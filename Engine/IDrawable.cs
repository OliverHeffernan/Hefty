using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine;

public interface IDrawable
{
    void Draw(SpriteBatch spriteBatch, GameTime gameTime);
}
