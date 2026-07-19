using Microsoft.Xna.Framework;

namespace Hefty.Engine;

public class Transform
{
    public Transform()
    {
        Position = Vector2.Zero;
        Scale = Vector2.One;
    }

    public Vector2 Position { get; set; }
    public Vector2 Scale { get; set; }
}
