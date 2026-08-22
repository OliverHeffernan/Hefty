using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;

namespace Hefty.Engine.Animation;

/// <summary>A reusable sequence of texture source rectangles played at a fixed rate.</summary>
public sealed class AnimationClip
{
    private readonly Rectangle[] frames;

    public AnimationClip(IEnumerable<Rectangle> frames, double framesPerSecond, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(frames);

        this.frames = frames is Rectangle[] array ? (Rectangle[])array.Clone() : [.. frames];
        if (this.frames.Length == 0)
            throw new ArgumentException("An animation clip must contain at least one frame.", nameof(frames));

        for (int i = 0; i < this.frames.Length; i++)
        {
            Rectangle frame = this.frames[i];
            if (frame.X < 0 || frame.Y < 0 || frame.Width <= 0 || frame.Height <= 0)
                throw new ArgumentException($"Frame {i} must have a non-negative origin and positive dimensions.", nameof(frames));
        }

        if (!double.IsFinite(framesPerSecond) || framesPerSecond <= 0)
            throw new ArgumentOutOfRangeException(nameof(framesPerSecond), "Frames per second must be finite and greater than zero.");

        FramesPerSecond = framesPerSecond;
        Loop = loop;
        Frames = Array.AsReadOnly(this.frames);
    }

    public ReadOnlyCollection<Rectangle> Frames { get; }
    public int FrameCount => frames.Length;
    public double FramesPerSecond { get; }
    public bool Loop { get; }
    public Rectangle this[int index] => frames[index];
}
