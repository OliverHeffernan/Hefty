#nullable enable

using System.Text.Json;

namespace Hefty.Engine.State;

/// <summary>A deliberately registered owner of one named save-state slice.</summary>
public interface IGameStateContributor
{
    /// <summary>A stable, unique identifier stored in the save file.</summary>
    string SliceName { get; }

    /// <summary>Captures this contributor's current state as JSON.</summary>
    JsonElement WriteState(JsonSerializerOptions serializerOptions);

    /// <summary>Restores state previously returned by <see cref="WriteState"/>.</summary>
    void ReadState(JsonElement state, JsonSerializerOptions serializerOptions);

    /// <summary>Resets this contributor for a new game.</summary>
    void NewGame();
}
