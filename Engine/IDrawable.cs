using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine;

public interface IDrawable : IComparable, IDestroyable
{
    void Draw(SpriteBatch spriteBatch, GameTime gameTime);
}
