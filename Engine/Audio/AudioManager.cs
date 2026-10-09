using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace Hefty.Engine;

/// <summary>Loads and plays catalogued audio through MonoGame's content pipeline.</summary>
public sealed class AudioManager : IDisposable
{
    private static AudioManager? instance;
    private static AudioManager? musicOwner;
    private readonly List<IAudioVoice> activeSfx = new();
    private readonly Dictionary<string, (IAudioResource Resource, bool Owned)> resources = new(StringComparer.Ordinal);
    private ContentManager? content;
    private AudioCatalog? catalog;
    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private bool disposed;

    /// <summary>Legacy convenience instance. Access after disposal creates a fresh manager.</summary>
    public static AudioManager Instance => instance is null || instance.disposed ? instance = new AudioManager() : instance;
    public int MaxVoices { get; }
    public int ActiveVoiceCount => activeSfx.Count;

    /// <summary>Raised for non-fatal misuse or content/playback failures.</summary>
    public event Action<string>? Diagnostic;

    /// <summary>The most recent diagnostic, or null when none has been reported.</summary>
    public string? LastDiagnostic { get; private set; }

    public bool IsInitialized => (content is not null || resources.Count > 0) && !disposed;

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

    /// <summary>Creates an independently owned SFX service. At capacity the oldest voice is stopped.</summary>
    public AudioManager(int maxVoices = 32)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxVoices);
        MaxVoices = maxVoices;
    }

    /// <summary>Registers a unique supplied resource. Owned resources are disposed by Reset/Dispose, never by StopAll.</summary>
    public void RegisterSound(string id, IAudioResource resource, bool ownsResource = false)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(resource);
        if (!resources.TryAdd(id, (resource, ownsResource))) throw new ArgumentException("Duplicate audio ID.", nameof(id));
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
        if (id is null) { Report("Unknown audio ID '<null>'."); return; }
        if (!disposed && resources.TryGetValue(id, out var registered))
        {
            PlaySfx(registered.Resource);
            return;
        }
        if (!TryResolve(id, AudioKind.Sfx, out AudioCatalogEntry entry))
            return;

        try
        {
            SoundEffect effect = content!.Load<SoundEffect>(entry.AssetName);
            PlaySfx(new SoundEffectResource(effect));
        }
        catch (Exception exception) when (IsPlaybackException(exception))
        {
            Report($"Unable to play SFX '{id}' from '{entry.AssetName}': {exception.Message}");
        }
    }

    /// <summary>Plays a borrowed resource without content initialization. The caller retains its lifetime.</summary>
    public void PlaySfx(IAudioResource resource)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(resource);
        PruneFinishedSfx();
        IAudioVoice? voice = null;
        try
        {
            voice = resource.CreateVoice();
            voice.Volume = masterVolume * sfxVolume;
            if (activeSfx.Count == MaxVoices)
            {
                activeSfx[0].Stop();
                activeSfx[0].Dispose();
                activeSfx.RemoveAt(0);
            }
            voice.Play();
            activeSfx.Add(voice);
            voice = null; // ownership transferred to the active list
        }
        catch (Exception exception) when (IsPlaybackException(exception))
        {
            Report($"Unable to play supplied SFX: {exception.Message}");
        }
        finally { voice?.Dispose(); }
    }

    public void StopAll()
    {
        foreach (IAudioVoice voice in activeSfx) { voice.Stop(); voice.Dispose(); }
        activeSfx.Clear();
        StopMusic();
    }

    /// <summary>Stops voices, releases owned resources, detaches borrowed content, and permits reinitialization.</summary>
    public void Reset()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        StopAll();
        foreach (var resource in resources.Values) if (resource.Owned) resource.Resource.Dispose();
        resources.Clear();
        content = null;
        catalog = null;
        LastDiagnostic = null;
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
            musicOwner = this;
        }
        catch (Exception exception) when (IsPlaybackException(exception))
        {
            Report($"Unable to play music '{id}' from '{entry.AssetName}': {exception.Message}");
        }
    }

    public void StopMusic()
    {
        if (!ReferenceEquals(musicOwner, this))
            return;

        try
        {
            MediaPlayer.Stop();
            musicOwner = null;
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

        Reset();
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

        if (ReferenceEquals(musicOwner, this))
            MediaPlayer.Volume = masterVolume * musicVolume;

        PruneFinishedSfx();
        foreach (IAudioVoice instance in activeSfx)
            instance.Volume = masterVolume * sfxVolume;
    }

    private void PruneFinishedSfx()
    {
        for (int index = activeSfx.Count - 1; index >= 0; index--)
        {
            if (!activeSfx[index].IsStopped)
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
