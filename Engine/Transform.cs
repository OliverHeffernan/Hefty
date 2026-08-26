using Microsoft.Xna.Framework;

namespace Hefty.Engine;

/// <summary>Stores the world or screen position and scale shared by all components on a game object.</summary>
public class Transform
{
    /// <summary>Creates a transform at the origin with unit scale.</summary>
    public Transform()
    {
        Position = Vector2.Zero;
        Scale = Vector2.One;
    }

    /// <summary>Gets or sets the position in world or screen units, according to the object's render space.</summary>
    public Vector2 Position { get; set; }
    /// <summary>Gets or sets the scale applied by components that support scaling.</summary>
    public Vector2 Scale { get; set; }
}
