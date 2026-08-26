using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace Hefty.Engine;

/// <summary>Loads and plays catalogued audio through MonoGame's content pipeline.</summary>
public sealed class AudioManager : IDisposable
{
    private static readonly Lazy<AudioManager> lazyInstance = new(() => new AudioManager());
    private readonly List<SoundEffectInstance> activeSfx = new();
    private ContentManager? content;
    private AudioCatalog? catalog;
    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private bool disposed;

    public static AudioManager Instance => lazyInstance.Value;

    /// <summary>Raised for non-fatal misuse or content/playback failures.</summary>
    public event Action<string>? Diagnostic;

    /// <summary>The most recent diagnostic, or null when none has been reported.</summary>
    public string? LastDiagnostic { get; private set; }

    public bool IsInitialized => content is not null && !disposed;

    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = ClampVolume(value, nameof(MasterVolume));
            ApplyVolumes();
        }
    }

    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = ClampVolume(value, nameof(MusicVolume));
            ApplyVolumes();
        }
    }

    public float SfxVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = ClampVolume(value, nameof(SfxVolume));
            ApplyVolumes();
        }
    }

    private AudioManager()
    {
    }

    public void Initialize(ContentManager contentManager, AudioCatalog audioCatalog)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        content = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
        catalog = audioCatalog ?? throw new ArgumentNullException(nameof(audioCatalog));
        LastDiagnostic = null;
        ApplyVolumes();
    }

    public void PlaySfx(string id)
    {
        if (!TryResolve(id, AudioKind.Sfx, out AudioCatalogEntry entry))
            return;

        PruneFinishedSfx();
        SoundEffectInstance? instance = null;
        try
        {
            SoundEffect effect = content!.Load<SoundEffect>(entry.AssetName);
            instance = effect.CreateInstance();
            instance.Volume = masterVolume * sfxVolume;
            instance.Play();
            activeSfx.Add(instance);
        }
        catch (Exception exception) when (IsPlaybackException(exception))
        {
            instance?.Dispose();
            Report($"Unable to play SFX '{id}' from '{entry.AssetName}': {exception.Message}");
        }
    }

    /// <summary>Prunes completed sound-effect instances. Call once per game frame.</summary>
    public void Update()
    {
        if (!disposed)
            PruneFinishedSfx();
    }

    public void PlayMusic(string id, bool loop)
    {
        if (!TryResolve(id, AudioKind.Music, out AudioCatalogEntry entry))
            return;

        try
        {
            Song song = content!.Load<Song>(entry.AssetName);
            MediaPlayer.Stop();
            MediaPlayer.IsRepeating = loop;
            MediaPlayer.Volume = masterVolume * musicVolume;
            MediaPlayer.Play(song);
        }
        catch (Exception exception) when (IsPlaybackException(exception))
        {
            Report($"Unable to play music '{id}' from '{entry.AssetName}': {exception.Message}");
        }
    }

    public void StopMusic()
    {
        if (!EnsureInitialized())
            return;

        try
        {
            MediaPlayer.Stop();
        }
        catch (Exception exception) when (IsPlaybackException(exception))
        {
            Report($"Unable to stop music: {exception.Message}");
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;

        if (content is not null)
        {
            try
            {
                MediaPlayer.Stop();
            }
            catch (Exception exception) when (IsPlaybackException(exception))
            {
                Report($"Unable to stop music while disposing audio: {exception.Message}");
            }
        }

        foreach (SoundEffectInstance instance in activeSfx)
            instance.Dispose();
        activeSfx.Clear();
        content = null;
        catalog = null;
        disposed = true;
    }

    private bool TryResolve(string id, AudioKind expectedKind, out AudioCatalogEntry entry)
    {
        entry = default;
        if (!EnsureInitialized())
            return false;
        if (!catalog!.TryGet(id, out entry))
        {
            Report($"Unknown audio ID '{id ?? "<null>"}'.");
            return false;
        }
        if (entry.Kind != expectedKind)
        {
            Report($"Audio ID '{id}' is {entry.Kind}, not {expectedKind}.");
            return false;
        }
        return true;
    }

    private bool EnsureInitialized()
    {
        if (disposed)
        {
            Report("AudioManager has been disposed.");
            return false;
        }
        if (content is null)
        {
            Report("AudioManager is not initialized. Call Initialize with the game's ContentManager and an AudioCatalog.");
            return false;
        }
        return true;
    }

    private void ApplyVolumes()
    {
        if (disposed)
            return;

        if (content is not null)
            MediaPlayer.Volume = masterVolume * musicVolume;

        PruneFinishedSfx();
        foreach (SoundEffectInstance instance in activeSfx)
            instance.Volume = masterVolume * sfxVolume;
    }

    private void PruneFinishedSfx()
    {
        for (int index = activeSfx.Count - 1; index >= 0; index--)
        {
            if (activeSfx[index].State != SoundState.Stopped)
                continue;
            activeSfx[index].Dispose();
            activeSfx.RemoveAt(index);
        }
    }

    private static float ClampVolume(float value, string propertyName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            throw new ArgumentOutOfRangeException(propertyName, "Volume must be a finite number.");
        return Math.Clamp(value, 0f, 1f);
    }

    private static bool IsPlaybackException(Exception exception) =>
        exception is ContentLoadException
            or InvalidOperationException
            or ArgumentException
            or NoAudioHardwareException;

    private void Report(string message)
    {
        LastDiagnostic = message;
        Diagnostic?.Invoke(message);
    }
}
