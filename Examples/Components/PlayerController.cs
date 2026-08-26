using Hefty.Engine;
using Hefty.Engine.Collision;
using Microsoft.Xna.Framework;

namespace Hefty.Examples.Components;

public sealed class PlayerController(PhysicsBody body) : Component
{
    protected override void Update(GameTime gameTime)
    {
        Vector2 movement = Vector2.Zero;
        if (World.Input.IsHeld("Up"))
            movement.Y--;
        if (World.Input.IsHeld("Down"))
            movement.Y++;
        if (World.Input.IsHeld("Left"))
            movement.X--;
        if (World.Input.IsHeld("Right"))
            movement.X++;

        if (movement != Vector2.Zero)
        {
            movement.Normalize();
            body.Move(movement * 200f * (float)gameTime.ElapsedGameTime.TotalSeconds);
        }
    }
}
