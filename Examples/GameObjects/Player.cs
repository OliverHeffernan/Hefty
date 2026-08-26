using Hefty.Engine;
using Hefty.Engine.Collision;
using Hefty.Examples.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hefty.Examples.GameObjects;

public sealed class Player : GameObject
{
    public Player(Texture2D texture)
    {
        SpriteRenderer renderer = AddComponent(new SpriteRenderer(texture, new(50)));
        PhysicsBody body = AddComponent(new PhysicsBody(BodyType.Kinematic));
        Collider collider = body.AddCollider(new Collider(Transform, new(50), Vector2.Zero));
        collider.CollisionEntered += _ => renderer.Color = Color.Red;
        collider.CollisionExited += _ => renderer.Color = Color.White;
        AddComponent(new PlayerController(body));
    }
}
