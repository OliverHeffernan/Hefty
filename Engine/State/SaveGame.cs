#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hefty.Engine.State;

/// <summary>The stable, serializable version 1 save-file contract.</summary>
public sealed class SaveGame
{
    public const int CurrentSchemaVersion = 1;

    [JsonRequired]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    [JsonRequired]
    public PlayerState Player { get; set; } = new();
    [JsonRequired]
    public InventoryState Inventory { get; set; } = new();
    [JsonRequired]
    public WorldState World { get; set; } = new();
    [JsonRequired]
    public TimeState Time { get; set; } = new();
    [JsonRequired]
    public DeterministicSeedState Determinism { get; set; } = new();

    /// <summary>State owned by explicitly registered engine or game systems.</summary>
    [JsonRequired]
    public Dictionary<string, JsonElement> Slices { get; set; } = new(StringComparer.Ordinal);
}

public sealed class PlayerState
{
    [JsonRequired]
    public PositionState Position { get; set; } = new();
    [JsonRequired]
    public PlayerStatsState Stats { get; set; } = new();
}

public sealed class PositionState
{
    [JsonRequired]
    public float X { get; set; }
    [JsonRequired]
    public float Y { get; set; }
}

public sealed class PlayerStatsState
{
    [JsonRequired]
    public int Health { get; set; }
    [JsonRequired]
    public int MaximumHealth { get; set; }
    [JsonRequired]
    public int Experience { get; set; }
}

public sealed class InventoryState
{
    [JsonRequired]
    public long Seed { get; set; }
}

public sealed class WorldState
{
    [JsonRequired]
    public long Seed { get; set; }
}

public sealed class TimeState
{
    [JsonRequired]
    public int Day { get; set; } = 1;
    [JsonRequired]
    public int Hour { get; set; }
    [JsonRequired]
    public int Minute { get; set; }
}

/// <summary>
/// Seeds needed to reproduce deterministic streams. Decimal strings avoid precision loss in
/// JSON consumers and make the signed/unsigned representation explicit.
/// </summary>
public sealed class DeterministicSeedState
{
    [JsonRequired]
    public string Algorithm { get; set; } = "system-random-v1";
    [JsonRequired]
    public string Seed { get; set; } = "0";
}
