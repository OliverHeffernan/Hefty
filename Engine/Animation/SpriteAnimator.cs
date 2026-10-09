using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Hefty.Engine.Animation;

/// <summary>Advances named animation clips and applies their frames to a sprite.</summary>
public sealed class SpriteAnimator : Component
{
    private readonly SpriteRenderer sprite;
    private readonly Dictionary<string, AnimationClip> clips = new(StringComparer.Ordinal);
    private AnimationClip? currentClip;
    private double elapsedInFrame;

    public SpriteAnimator(SpriteRenderer sprite)
    {
        this.sprite = sprite ?? throw new ArgumentNullException(nameof(sprite));
    }

    public string? CurrentClipName { get; private set; }
    public int FrameIndex { get; private set; }
    public bool IsPlaying { get; private set; }
    /// <summary>Freezes time without disabling the sprite or resetting partial-frame progress.</summary>
    public bool IsPaused { get; set; }
    /// <summary>False leaves advancement entirely to Advance.</summary>
    public bool AutoAdvance { get; set; } = true;
    /// <summary>Optional per-animation clock returning delta seconds; defaults to GameTime.</summary>
    public Func<GameTime, double>? TimeSource { get; set; }

    public void AddClip(string name, AnimationClip clip)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(clip);
        clips.Add(name, clip);
    }

    public void Play(string name, bool restart = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!clips.TryGetValue(name, out AnimationClip? clip))
            throw new KeyNotFoundException($"No animation clip named '{name}' has been added.");

        if (!restart && IsPlaying && string.Equals(CurrentClipName, name, StringComparison.Ordinal))
            return;

        currentClip = clip;
        CurrentClipName = name;
        FrameIndex = 0;
        elapsedInFrame = 0;
        IsPlaying = true;
        ApplyCurrentFrame();
    }

    public void Stop()
    {
        IsPlaying = false;
        elapsedInFrame = 0;
    }

    protected override void Update(GameTime gameTime)
    {
        if (AutoAdvance && !IsPaused && IsPlaying)
            Advance(TimeSource?.Invoke(gameTime) ?? gameTime.ElapsedGameTime.TotalSeconds);
    }

    /// <summary>Advances explicitly, independent of AutoAdvance. Pause also blocks manual advancement.</summary>
    public void Advance(double elapsedSeconds)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0 || elapsedSeconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        if (!IsPlaying || IsPaused || elapsedSeconds == 0)
            return;

        // Accumulate progress in frames rather than dividing by a duration. The small
        // tolerance prevents values such as 0.3 * 10 being treated as 2.999999999...
        // at an exact frame boundary.
        AnimationClip clip = currentClip!;
        double frameProgress = (elapsedInFrame + elapsedSeconds) * clip.FramesPerSecond;
        if (!double.IsFinite(frameProgress) || frameProgress >= long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "Frame advancement exceeds the supported range.");
        long framesElapsed = (long)Math.Floor(frameProgress + 1e-9);
        if (framesElapsed == 0)
        {
            elapsedInFrame += elapsedSeconds;
            return;
        }

        elapsedInFrame = (frameProgress - framesElapsed) / clip.FramesPerSecond;
        if (clip.Loop)
        {
            FrameIndex = (int)((FrameIndex + framesElapsed) % clip.FrameCount);
        }
        else
        {
            long nextFrame = FrameIndex + framesElapsed;
            if (nextFrame >= clip.FrameCount - 1)
            {
                FrameIndex = clip.FrameCount - 1;
                elapsedInFrame = 0;
                IsPlaying = false;
            }
            else
            {
                FrameIndex = (int)nextFrame;
            }
        }

        ApplyCurrentFrame();
    }

    private void ApplyCurrentFrame()
    {
        sprite.SourceRectangle = currentClip![FrameIndex];
    }
}
