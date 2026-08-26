using Hefty.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Hefty.Examples.Components;

public class CameraFollow(Camera2D camera, Transform target) : Component
{
    private readonly Camera2D camera = camera ?? throw new ArgumentNullException(nameof(camera));
    private readonly Transform target = target ?? throw new ArgumentNullException(nameof(target));
    private float smoothing;

    public Vector2 Offset { get; set; }

    public float Smoothing
    {
        get => smoothing;
        set
        {
            if (value < 0f)
                throw new ArgumentOutOfRangeException(nameof(value), "Smoothing cannot be negative.");

            smoothing = value;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        Vector2 destination = target.Position + Offset;
        if (Smoothing == 0f)
        {
            camera.Transform.Position = destination;
            return;
        }

        float seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float amount = 1f - MathF.Exp(-Smoothing * seconds);
        camera.Transform.Position = Vector2.Lerp(camera.Transform.Position, destination, amount);
    }
}
