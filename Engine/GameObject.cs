using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace Hefty.Engine;
public class GameObject(params IUpdater[] components) : IUpdater, IComparable, IDestroyable
{
	private readonly List<IUpdater> components = [..components];
	private readonly List<IDestroyable> cleanup = [];
	public bool ToDestroy { get; private set; } = false;

	public int Priority { get; set; } = 0;
	public int CompareTo(object obj) {
		if (obj is GameObject other) {
			return Priority.CompareTo(other.Priority);
		}
		throw new ArgumentException("Object is not a GameObject");
	}

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
		if (component is IDestroyable destroyable)
			cleanup.Add(destroyable);
	}
	public void AddCleanup(IDestroyable resource) => cleanup.Add(resource);
	public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime) { }

	public void Destroy()
	{
		CleanUp();
		ToDestroy = true;
	}

	public void CleanUp()
	{
		foreach (IDestroyable component in cleanup) {
			component.Destroy();
		}
		cleanup.Clear();
	}
}
