using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Hefty.Engine;

public class KeyboardInputManager : IUpdater
{
	private KeyboardState previous;
	private KeyboardState current;
	private static KeyboardInputManager instance;
	public static KeyboardInputManager Instance()
	{
		instance ??= new KeyboardInputManager();
		return instance;
	}

	private KeyboardInputManager()
	{
		previous = Keyboard.GetState();
		current = Keyboard.GetState();
	}
	public void Update(GameTime gameTime)
	{
		previous = current;
		current = Keyboard.GetState();
	}

	public bool IsKeyPressed(Keys key)
	{
		return current.IsKeyDown(key) && !previous.IsKeyDown(key);
	}

	public bool IsKeyReleased(Keys key)
	{
		return !current.IsKeyDown(key) && previous.IsKeyDown(key);
	}

	public bool IsKeyDown(Keys key)
	{
		return current.IsKeyDown(key);
	}

	public bool IsKeyUp(Keys key)
	{
		return !current.IsKeyDown(key);
	}
}
