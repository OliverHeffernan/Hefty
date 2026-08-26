using Hefty.Engine;
using Hefty.Engine.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Examples.GameObjects;

public sealed class Obstacle : GameObject
{
    public Obstacle(Texture2D texture)
    {
        AddComponent(new SpriteRenderer(texture, new(50)) { Color = Color.Red });
        PhysicsBody body = AddComponent(new PhysicsBody(BodyType.Static));
        body.AddCollider(new Collider(Transform, new(50), Vector2.Zero));
    }
}
