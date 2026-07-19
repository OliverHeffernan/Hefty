using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace Hefty.Engine;
public class GameObject(params IUpdater[] components) : IUpdater
{
	private readonly List<IUpdater> components = [..components];

	/**
	 * Updates all components in the updater.
	 */
    public void Update(GameTime gameTime) {
		foreach (IUpdater component in components) {
			component.Update(gameTime);
		}
	}
	public void AddComponent(IUpdater component) {
		components.Add(component);
	}
	public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime) { }
}
