using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hefty.Engine.State;

public sealed partial class GameStateManager
{
    private sealed class DataEnvelope
    {
        [JsonPropertyName("format")] public string Format { get; set; } = "hefty-data-1";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("version")] public int Version { get; set; }
        [JsonPropertyName("data")] public JsonElement Data { get; set; }
    }

    /// <summary>Atomically saves arbitrary data. Does not capture contributors or mutate the supplied value.</summary>
    public void SaveData<T>(string slot, string typeId, int version, T data, Action<T>? validate = null)
    {
        string path = GetSlotPath(slot);
        ValidateContract(typeId, version);
        try
        {
            if (data is null) throw new JsonException("Data cannot be null.");
            validate?.Invoke(data);
            var envelope = new DataEnvelope { Type = typeId, Version = version,
                Data = JsonSerializer.SerializeToElement(data, _contributorSerializerOptions) };
            WriteAtomic(path, JsonSerializer.Serialize(envelope, _serializerOptions));
        }
        catch (GameStateException) { throw; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { throw new GameStateException(GameStateFailure.InputOutput, $"Could not save slot '{slot}'.", exception); }
        catch (Exception exception)
        { throw new GameStateException(GameStateFailure.InvalidData, "Data could not be validated or serialized.", exception); }
    }

    /// <summary>Loads and validates before returning data, without applying it to any services.
    /// Migration transforms an older payload to currentVersion in memory; loading never rewrites the file.</summary>
    public T LoadData<T>(string slot, string typeId, int currentVersion,
        Func<JsonElement, int, JsonElement>? migrate = null, Action<T>? validate = null)
    {
        string path = GetSlotPath(slot);
        ValidateContract(typeId, currentVersion);
        if (!File.Exists(path)) throw new GameStateException(GameStateFailure.NotFound, $"Save slot '{slot}' does not exist.");
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            DataEnvelope envelope = document.RootElement.Deserialize<DataEnvelope>(_serializerOptions)
                ?? throw new JsonException("Missing envelope.");
            // Format must be explicitly present: a legacy SaveGame is never interpreted as generic data.
            if (!document.RootElement.TryGetProperty("format", out var format)
                || format.GetString() != "hefty-data-1" || envelope.Type != typeId || envelope.Version < 1
                || envelope.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                throw new JsonException("Wrong format, type, version, or missing data.");
            if (envelope.Version > currentVersion || (envelope.Version < currentVersion && migrate is null))
                throw new GameStateException(GameStateFailure.UnsupportedVersion,
                    $"Data version {envelope.Version} is unsupported; expected {currentVersion}.");
            JsonElement payload = envelope.Version == currentVersion ? envelope.Data : migrate!(envelope.Data.Clone(), envelope.Version);
            T data = payload.Deserialize<T>(_contributorSerializerOptions) ?? throw new JsonException("Missing payload.");
            validate?.Invoke(data);
            return data;
        }
        catch (GameStateException) { throw; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { throw new GameStateException(GameStateFailure.InputOutput, $"Could not read slot '{slot}'.", exception); }
        catch (Exception exception)
        { throw new GameStateException(GameStateFailure.InvalidData, "Data could not be parsed, migrated, or validated.", exception); }
    }

    public GameStateResult TrySaveData<T>(string slot, string typeId, int version, T data, Action<T>? validate = null)
    {
        try { SaveData(slot, typeId, version, data, validate); return GameStateResult.Success(); }
        catch (GameStateException exception) { return GameStateResult.Failed(exception); }
    }

    public GameStateResult TryLoadData<T>(string slot, string typeId, int currentVersion, out T? data,
        Func<JsonElement, int, JsonElement>? migrate = null, Action<T>? validate = null)
    {
        try { data = LoadData<T>(slot, typeId, currentVersion, migrate, validate); return GameStateResult.Success(); }
        catch (GameStateException exception) { data = default; return GameStateResult.Failed(exception); }
    }

    private static void ValidateContract(string typeId, int version)
    {
        if (!IsValidSliceName(typeId) || version < 1)
            throw new GameStateException(GameStateFailure.InvalidData, "Type IDs require 1-64 identifier characters and versions must be positive.");
    }
}
