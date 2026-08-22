#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Hefty.Engine.State;

public enum GameStateFailure
{
    InvalidSlot,
    NotFound,
    InvalidData,
    UnsupportedVersion,
    InputOutput,
    Contributor
}

public sealed class GameStateException : Exception
{
    public GameStateFailure Failure { get; }

    public GameStateException(GameStateFailure failure, string message, Exception? innerException = null)
        : base(message, innerException) => Failure = failure;
}

public readonly record struct GameStateResult(bool Succeeded, GameStateFailure? Failure, string? Error)
{
    public static GameStateResult Success() => new(true, null, null);
    public static GameStateResult Failed(GameStateException exception) =>
        new(false, exception.Failure, exception.Message);
}

/// <summary>Coordinates versioned, atomic save files and explicitly registered state owners.</summary>
public sealed class GameStateManager
{
    private const string SaveExtension = ".json";
    private readonly Dictionary<string, IGameStateContributor> _contributors =
        new(StringComparer.Ordinal);
    private readonly JsonSerializerOptions _serializerOptions;

    public string SaveDirectory { get; }

    public GameStateManager(
        string? saveDirectory = null,
        IEnumerable<IGameStateContributor>? contributors = null,
        JsonSerializerOptions? serializerOptions = null)
    {
        SaveDirectory = Path.GetFullPath(saveDirectory ?? GetDefaultSaveDirectory());
        _serializerOptions = serializerOptions is null
            ? new JsonSerializerOptions { WriteIndented = true }
            : new JsonSerializerOptions(serializerOptions);

        foreach (var contributor in contributors ?? [])
            Register(contributor);
    }

    public void Register(IGameStateContributor contributor)
    {
        ArgumentNullException.ThrowIfNull(contributor);
        if (!IsValidSliceName(contributor.SliceName))
            throw new ArgumentException("Slice names may contain 1-64 ASCII letters, digits, hyphens, underscores, or periods.", nameof(contributor));
        if (!_contributors.TryAdd(contributor.SliceName, contributor))
            throw new ArgumentException($"A contributor named '{contributor.SliceName}' is already registered.", nameof(contributor));
    }

    public void Save(string slot, SaveGame state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var path = GetSlotPath(slot);
        state.SchemaVersion = SaveGame.CurrentSchemaVersion;
        state.Slices = state.Slices is null
            ? new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            : new Dictionary<string, JsonElement>(state.Slices, StringComparer.Ordinal);

        try
        {
            foreach (var (name, contributor) in _contributors)
                state.Slices[name] = contributor.WriteState(_serializerOptions).Clone();
        }
        catch (Exception exception)
        {
            throw new GameStateException(GameStateFailure.Contributor, "A state contributor could not be captured.", exception);
        }

        ValidateState(state);

        string json;
        try
        {
            json = JsonSerializer.Serialize(state, _serializerOptions);
            Directory.CreateDirectory(SaveDirectory);
            var temporaryPath = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllText(temporaryPath, json);
                File.Move(temporaryPath, path, true);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }
        catch (GameStateException) { throw; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new GameStateException(GameStateFailure.InputOutput, $"Could not save slot '{slot}'.", exception);
        }
    }

    public SaveGame Load(string slot)
    {
        var path = GetSlotPath(slot);
        if (!File.Exists(path))
            throw new GameStateException(GameStateFailure.NotFound, $"Save slot '{slot}' does not exist.");

        SaveGame state;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var version = ReadVersion(document.RootElement, _serializerOptions);
            var migrated = Migrate(document.RootElement, version);
            state = migrated.Deserialize<SaveGame>(_serializerOptions)
                ?? throw new JsonException("The save file contained no state.");
            ValidateState(state);
            ValidateContributorSlices(state);
        }
        catch (GameStateException) { throw; }
        catch (JsonException exception)
        {
            throw new GameStateException(GameStateFailure.InvalidData, $"Save slot '{slot}' is malformed.", exception);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new GameStateException(GameStateFailure.InputOutput, $"Could not read slot '{slot}'.", exception);
        }

        // No contributor is touched until the entire envelope has parsed and validated.
        try
        {
            foreach (var (name, contributor) in _contributors)
                contributor.ReadState(state.Slices[name], _serializerOptions);
        }
        catch (Exception exception)
        {
            throw new GameStateException(GameStateFailure.Contributor, "A state contributor could not be restored.", exception);
        }

        return state;
    }

    public SaveGame NewGame()
    {
        try
        {
            foreach (var contributor in _contributors.Values)
                contributor.NewGame();
            return new SaveGame();
        }
        catch (Exception exception)
        {
            throw new GameStateException(GameStateFailure.Contributor,
                "A state contributor could not be reset for a new game.", exception);
        }
    }

    public GameStateResult TrySave(string slot, SaveGame state)
    {
        try { Save(slot, state); return GameStateResult.Success(); }
        catch (GameStateException exception) { return GameStateResult.Failed(exception); }
    }

    public GameStateResult TryLoad(string slot, out SaveGame? state)
    {
        try { state = Load(slot); return GameStateResult.Success(); }
        catch (GameStateException exception) { state = null; return GameStateResult.Failed(exception); }
    }

    private string GetSlotPath(string slot)
    {
        if (string.IsNullOrWhiteSpace(slot) || slot.Length > 64 ||
            slot is "." or ".." || slot.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
            throw new GameStateException(GameStateFailure.InvalidSlot, "Slots may contain 1-64 ASCII letters, digits, hyphens, or underscores.");
        return Path.Combine(SaveDirectory, slot + SaveExtension);
    }

    private static int ReadVersion(JsonElement root, JsonSerializerOptions serializerOptions)
    {
        string versionName = serializerOptions.PropertyNamingPolicy?.ConvertName(nameof(SaveGame.SchemaVersion))
            ?? nameof(SaveGame.SchemaVersion);
        if (root.ValueKind != JsonValueKind.Object ||
            (!root.TryGetProperty(versionName, out var property)
                && !root.TryGetProperty(nameof(SaveGame.SchemaVersion), out property)) ||
            !property.TryGetInt32(out var version))
            throw new GameStateException(GameStateFailure.InvalidData, "The save has no valid schema version.");
        return version;
    }

    /// <summary>Central migration entry point. Add sequential version transformations here.</summary>
    private static JsonElement Migrate(JsonElement root, int version)
    {
        if (version != SaveGame.CurrentSchemaVersion)
            throw new GameStateException(GameStateFailure.UnsupportedVersion,
                $"Save schema version {version} is unsupported; expected {SaveGame.CurrentSchemaVersion}.");
        return root.Clone();
    }

    private static void ValidateState(SaveGame state)
    {
        if (state.SchemaVersion != SaveGame.CurrentSchemaVersion || state.Player is null ||
            state.Player.Position is null || state.Player.Stats is null ||
            state.Inventory is null || state.World is null || state.Time is null ||
            state.Determinism is null || state.Slices is null || string.IsNullOrWhiteSpace(state.Determinism.Algorithm) ||
            !long.TryParse(state.Determinism.Seed, out _) || state.Time.Day < 1 ||
            state.Time.Hour is < 0 or > 23 || state.Time.Minute is < 0 or > 59)
            throw new GameStateException(GameStateFailure.InvalidData, "The save contains invalid or missing required state.");

        if (state.Slices.Keys.Any(name => !IsValidSliceName(name)))
            throw new GameStateException(GameStateFailure.InvalidData, "The save contains an invalid slice name.");
    }

    private void ValidateContributorSlices(SaveGame state)
    {
        foreach (string name in _contributors.Keys)
            if (!state.Slices.ContainsKey(name))
                throw new GameStateException(GameStateFailure.InvalidData,
                    $"The save is missing the state slice '{name}'.");
    }

    private static bool IsValidSliceName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name.Length <= 64 &&
        name.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');

    private static string GetDefaultSaveDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(appData))
            appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appData))
            throw new GameStateException(GameStateFailure.InputOutput, "No platform application-data directory is available.");
        return Path.Combine(appData, "Hefty", "Saves");
    }
}
