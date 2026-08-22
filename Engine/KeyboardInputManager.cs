using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Hefty.Engine.Input;

namespace Hefty.Engine;

public class KeyboardInputManager : Component
{
	private static KeyboardInputManager instance;
	public static KeyboardInputManager Instance()
	{
		instance ??= new KeyboardInputManager();
		return instance;
	}

	private KeyboardInputManager()
	{
	}
	
	public override void Update(GameTime gameTime)
	{
		InputManager.Instance().Update(gameTime);
	}

	public bool IsKeyPressed(Keys key)
	{
		return InputManager.Instance().IsKeyPressed(key);
	}

	public bool IsKeyReleased(Keys key)
	{
		return InputManager.Instance().IsKeyReleased(key);
	}

	public bool IsKeyDown(Keys key)
	{
		return InputManager.Instance().IsKeyDown(key);
	}

	public bool IsKeyUp(Keys key)
	{
		return InputManager.Instance().IsKeyUp(key);
	}
}
