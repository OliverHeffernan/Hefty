using System;
using Microsoft.Xna.Framework.Audio;

namespace Hefty.Engine;

/// <summary>Injected or generated sound. Each playback creates a manager-owned voice.</summary>
public interface IAudioResource : IDisposable { IAudioVoice CreateVoice(); }

public interface IAudioVoice : IDisposable
{
    bool IsStopped { get; }
    float Volume { get; set; }
    void Play();
    void Stop();
}

/// <summary>Wraps a supplied SoundEffect (including generated PCM). Borrowed by default.</summary>
public sealed class SoundEffectResource(SoundEffect effect, bool ownsEffect = false) : IAudioResource
{
    private readonly SoundEffect effect = effect ?? throw new ArgumentNullException(nameof(effect));
    private bool disposed;
    public IAudioVoice CreateVoice()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new Voice(effect.CreateInstance());
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (ownsEffect) effect.Dispose();
    }
    private sealed class Voice(SoundEffectInstance instance) : IAudioVoice
    {
        public bool IsStopped => instance.State == SoundState.Stopped;
        public float Volume { get => instance.Volume; set => instance.Volume = value; }
        public void Play() => instance.Play();
        public void Stop() => instance.Stop();
        public void Dispose() => instance.Dispose();
    }
}
