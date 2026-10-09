using Microsoft.Xna.Framework;

namespace Hefty.Engine.Collision;

/// <summary>A solid sweep impact. Normal points from Other toward Shape; Time is normalized to the whole step.
/// Penetration is nonzero only for initial overlap. Movement is the remaining displacement at this impact.</summary>
public readonly record struct CollisionContact(Collider Shape, Collider Other, Vector2 Normal,
    float Time, float Penetration, bool InitiallyOverlapping, Vector2 Movement);

/// <summary>Requested displacement and actual translation (including depenetration) in the latest step.</summary>
public readonly record struct ResolvedMotion(Vector2 Requested, Vector2 Actual);
