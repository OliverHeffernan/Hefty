using System;
using engine;
using Microsoft.Xna.Framework;

namespace components;

public class CameraFollow : IUpdater
{
    private readonly Camera2D camera;
    private readonly Transform target;
    private float smoothing;

    public CameraFollow(Camera2D camera, Transform target)
    {
        this.camera = camera ?? throw new ArgumentNullException(nameof(camera));
        this.target = target ?? throw new ArgumentNullException(nameof(target));
    }

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

    public bool Enabled { get; set; } = true;

    public void Update(GameTime gameTime)
    {
        if (!Enabled)
            return;

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
