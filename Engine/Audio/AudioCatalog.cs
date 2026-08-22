using System;
using System.Collections.Generic;

namespace Hefty.Engine;

public enum AudioKind
{
    Sfx,
    Music
}

public readonly record struct AudioCatalogEntry(string Id, string AssetName, AudioKind Kind);

/// <summary>Maps game-facing audio IDs to MonoGame content-pipeline asset names.</summary>
public sealed class AudioCatalog
{
    private readonly Dictionary<string, AudioCatalogEntry> entries;

    public static AudioCatalog Empty { get; } = new();

    public AudioCatalog(params AudioCatalogEntry[] entries)
        : this((IEnumerable<AudioCatalogEntry>)entries)
    {
    }

    public AudioCatalog(IEnumerable<AudioCatalogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        this.entries = new Dictionary<string, AudioCatalogEntry>(StringComparer.Ordinal);

        foreach (AudioCatalogEntry entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id))
                throw new ArgumentException("Audio IDs cannot be empty.", nameof(entries));
            if (string.IsNullOrWhiteSpace(entry.AssetName))
                throw new ArgumentException($"The asset name for audio ID '{entry.Id}' cannot be empty.", nameof(entries));
            if (!this.entries.TryAdd(entry.Id, entry))
                throw new ArgumentException($"Audio ID '{entry.Id}' appears more than once.", nameof(entries));
        }
    }

    public bool TryGet(string id, out AudioCatalogEntry entry)
    {
        if (id is null)
        {
            entry = default;
            return false;
        }

        return entries.TryGetValue(id, out entry);
    }
}
