# Game state

`GameStateManager` stores schema-versioned JSON in the platform's local application-data
directory (`Hefty/Saves`) by default. Pass a directory to its constructor for tests, portable
installs, or another policy. Slot names are restricted to ASCII letters, digits, `_`, and `-`.

```csharp
var stateManager = new GameStateManager(contributors: [inventoryContributor]);
var state = stateManager.NewGame();
state.Player.Position.X = 120;
state.World.Seed = 42;
stateManager.Save("slot-1", state);

GameStateResult result = stateManager.TryLoad("slot-1", out SaveGame? loaded);
```

Contributors implement `IGameStateContributor` and are registered explicitly; there is no
reflection-based discovery. Their stable, unique `SliceName` keys JSON owned by that system.
Unknown slices are preserved when loaded and on subsequent saves. A registered contributor
must have a slice in the file; otherwise loading fails explicitly instead of leaving stale
runtime state in place.

Writes use a temporary file and atomic replacement, so an interrupted or failed write does not
partially overwrite an existing slot. `Save`/`Load` throw `GameStateException`; callers that do
not want exceptions can use `TrySave`/`TryLoad` and inspect `GameStateResult.Failure`. Malformed,
missing-version, and unsupported-version files are rejected before contributors receive state.
Contributor restoration itself cannot be made transactional by the manager; contributors should
validate their complete slice before mutating runtime state.

Schema changes must increment `SaveGame.CurrentSchemaVersion`, preserve explicit DTO contracts,
and add a sequential transformation in `Migrate`. Version 1 intentionally has no older migration.

## Arbitrary game/settings data

The additive typed API does not require RPG DTOs or invoke contributors:

```csharp
record Settings(float Volume, string Language);
var saves = new GameStateManager(mySaveDirectory);
saves.SaveData("settings", "my-game.settings", 2, new Settings(0.7f, "en"),
    validate: value => {
        if (!float.IsFinite(value.Volume) || value.Volume is < 0 or > 1)
            throw new ArgumentException("Volume out of range.");
    });
Settings loaded = saves.LoadData<Settings>("settings", "my-game.settings", 2);
// The caller decides whether/when to apply it to a module.
```

The JSON envelope has fixed names `format: "hefty-data-1"`, `type`, `version`, and `data`.
Type IDs are stable 1–64 ASCII letters/digits/`-`/`_`/`.`; use a distinct ID for each schema.
Version is an application-owned positive integer, independent of the envelope format and
SaveGame.CurrentSchemaVersion. Payload serialization uses caller JsonSerializerOptions.
Type mismatch, absent/wrong format, null/malformed data and validation failures are InvalidData.
Newer versions are always UnsupportedVersion. Older versions require a migration callback:

```csharp
Settings loaded = saves.LoadData<Settings>("settings", "my-game.settings", 2,
    migrate: (oldPayload, oldVersion) => oldVersion == 1
        ? JsonSerializer.SerializeToElement(new Settings(
            oldPayload.GetProperty("Volume").GetSingle(), "en"))
        : throw new GameStateException(GameStateFailure.UnsupportedVersion, "Unknown settings version."),
    validate: ValidateSettings);
```

Migration returns a payload in the requested current schema (chain sequential transformations
inside the callback when needed). Validate runs after migration/deserialization and before data
returns. Loading never rewrites the file. Explicitly SaveData after acceptance to persist the
upgraded schema. Failed saves leave the prior slot intact; validators should be side-effect-free.
Callbacks' ordinary exceptions map to InvalidData; a callback may throw GameStateException to
choose a precise failure. I/O/permissions map to InputOutput; missing files to NotFound; unsafe
slot names to InvalidSlot. TrySaveData/TryLoadData return GameStateResult and default output on
failure. There is no silent fallback to defaults or automatic mutation of services.

Legacy Save/Load/NewGame and SaveGame version-1 files/contributors are unchanged. Generic and
legacy formats are deliberately separate; never reinterpret one as the other. To migrate an
RPG save, Load it explicitly, map its fields to your DTO, then SaveData to a new slot. Both APIs
share slot filenames/atomic replacement, so use different slot names to keep legacy files.
Writes are single-writer, per-file atomic replacements, not transactions across multiple slots.
