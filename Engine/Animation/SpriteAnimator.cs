using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Hefty.Engine.Animation;

/// <summary>Advances named animation clips and applies their frames to a sprite.</summary>
public sealed class SpriteAnimator : Component
{
    private readonly Sprite sprite;
    private readonly Dictionary<string, AnimationClip> clips = new(StringComparer.Ordinal);
    private AnimationClip currentClip;
    private double elapsedInFrame;

    public SpriteAnimator(Sprite sprite)
    {
        this.sprite = sprite ?? throw new ArgumentNullException(nameof(sprite));
    }

    public string CurrentClipName { get; private set; }
    public int FrameIndex { get; private set; }
    public bool IsPlaying { get; private set; }

    public void AddClip(string name, AnimationClip clip)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(clip);
        clips.Add(name, clip);
    }

    public void Play(string name, bool restart = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!clips.TryGetValue(name, out AnimationClip clip))
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

    public override void Update(GameTime gameTime)
    {
        if (!IsPlaying)
            return;

        double elapsedSeconds = gameTime.ElapsedGameTime.TotalSeconds;
        if (elapsedSeconds <= 0)
            return;

        double frameDuration = 1.0 / currentClip.FramesPerSecond;
        elapsedInFrame += elapsedSeconds;
        long framesElapsed = (long)(elapsedInFrame / frameDuration);
        if (framesElapsed == 0)
            return;

        elapsedInFrame -= framesElapsed * frameDuration;
        if (currentClip.Loop)
        {
            FrameIndex = (int)((FrameIndex + framesElapsed) % currentClip.FrameCount);
        }
        else
        {
            long nextFrame = FrameIndex + framesElapsed;
            if (nextFrame >= currentClip.FrameCount - 1)
            {
                FrameIndex = currentClip.FrameCount - 1;
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
        sprite.SourceRectangle = currentClip[FrameIndex];
    }
}
