using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Engine;

public abstract class Component : IUpdater, IDrawable
{
	public int Priority { get; set; } = 0;

	public virtual int CompareTo(object obj)
	{
		if (obj is Component other)
			return Priority.CompareTo(other.Priority);
		throw new ArgumentException($"Object is not a Component");
	}

	public virtual void Update(GameTime gameTime) { }

	public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime) { }
}
