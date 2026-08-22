#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Hefty.Engine.State;

/// <summary>The stable, serializable version 1 save-file contract.</summary>
public sealed class SaveGame
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public PlayerState Player { get; set; } = new();
    public InventoryState Inventory { get; set; } = new();
    public WorldState World { get; set; } = new();
    public TimeState Time { get; set; } = new();
    public DeterministicSeedState Determinism { get; set; } = new();

    /// <summary>State owned by explicitly registered engine or game systems.</summary>
    public Dictionary<string, JsonElement> Slices { get; set; } = new(StringComparer.Ordinal);
}

public sealed class PlayerState
{
    public PositionState Position { get; set; } = new();
    public PlayerStatsState Stats { get; set; } = new();
}

public sealed class PositionState
{
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class PlayerStatsState
{
    public int Health { get; set; }
    public int MaximumHealth { get; set; }
    public int Experience { get; set; }
}

public sealed class InventoryState
{
    public long Seed { get; set; }
}

public sealed class WorldState
{
    public long Seed { get; set; }
}

public sealed class TimeState
{
    public int Day { get; set; } = 1;
    public int Hour { get; set; }
    public int Minute { get; set; }
}

/// <summary>
/// Seeds needed to reproduce deterministic streams. Decimal strings avoid precision loss in
/// JSON consumers and make the signed/unsigned representation explicit.
/// </summary>
public sealed class DeterministicSeedState
{
    public string Algorithm { get; set; } = "system-random-v1";
    public string Seed { get; set; } = "0";
}
