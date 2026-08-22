using Hefty.Engine;
using Hefty.Engine.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace Hefty.Examples.Components;

public class PlayerController(PhysicsBody body) : Component
{
	private PhysicsBody Body { get; } = body;
	private readonly KeyboardInputManager keyboardInputManager = KeyboardInputManager.Instance();

	public override void Update(GameTime gameTime)
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
			Body.Move(movement * speed * (float)gameTime.ElapsedGameTime.TotalSeconds);
		}
	}
}
