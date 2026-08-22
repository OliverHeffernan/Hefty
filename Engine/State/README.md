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
Unknown slices are preserved when loaded but are replaced on the next save by registered
contributors.

Writes use a temporary file and atomic replacement, so an interrupted or failed write does not
partially overwrite an existing slot. `Save`/`Load` throw `GameStateException`; callers that do
not want exceptions can use `TrySave`/`TryLoad` and inspect `GameStateResult.Failure`. Malformed,
missing-version, and unsupported-version files are rejected before contributors receive state.
Contributor restoration itself cannot be made transactional by the manager; contributors should
validate their complete slice before mutating runtime state.

Schema changes must increment `SaveGame.CurrentSchemaVersion`, preserve explicit DTO contracts,
and add a sequential transformation in `Migrate`. Version 1 intentionally has no older migration.
