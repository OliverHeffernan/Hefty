using System;
using Microsoft.Xna.Framework;
namespace Hefty.Engine;
public interface IUpdater : IComparable, IDestroyable
{
	void Update(GameTime gameTime);
}
