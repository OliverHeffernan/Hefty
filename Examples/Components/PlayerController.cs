using Hefty.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace Hefty.Examples.Components;

public class PlayerController(Transform transform) : IUpdater
{
	private Transform Transform { get; } = transform;
	private readonly KeyboardInputManager keyboardInputManager = KeyboardInputManager.Instance();

	public void Update(GameTime gameTime)
	{
		float speed = 200f;
		Vector2 movement = Vector2.Zero;

		if (keyboardInputManager.IsKeyDown(Keys.W))
		{
			movement.Y -= 1;
		}
		if (keyboardInputManager.IsKeyDown(Keys.S))
		{
			movement.Y += 1;
		}
		if (keyboardInputManager.IsKeyDown(Keys.A))
		{
			movement.X -= 1;
		}
		if (keyboardInputManager.IsKeyDown(Keys.D))
		{
			movement.X += 1;
		}

		if (movement != Vector2.Zero)
		{
			movement.Normalize();
			Transform.Position += movement * speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
		}
	}
}
