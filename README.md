# Hefty

A small 2D game-engine prototype built in C# with [MonoGame](https://monogame.net/). The project demonstrates a component-based update loop, world switching, sprites, camera tracking, keyboard input, and broad-phase collision detection and response.

All engine implementation files are contained in `Engine/` under the `Hefty.Engine` namespace. The files under `Examples/` are test and demonstration code; they are not part of the engine itself.

## Features

- Game objects composed from reusable update components
- Separate world-space and screen-space rendering
- 2D camera with zoom, rotation, bounds, and coordinate conversion
- Smooth camera-follow component
- Keyboard input with held and just-pressed key states
- Swept AABB kinematic collision response with stable wall sliding
- Layered collision enter/stay/exit callbacks and non-blocking triggers
- Switchable worlds with initialization and cleanup hooks
- MonoGame content pipeline support

## Engine systems

- **Core API:** [`Engine/Engine.md`](Engine/Engine.md) documents worlds, game objects, components, rendering, lifecycle, and cleanup.
- **Input:** [`Engine/Input/Input.md`](Engine/Input/Input.md) documents world-owned keyboard/mouse actions and `IsPressed`, `IsHeld`, and `IsReleased` frame semantics.
- **Collision:** [`Engine/Collision/Collision.md`](Engine/Collision/Collision.md) documents static/kinematic bodies, movement intent, layer/mask filtering, non-blocking triggers, and collision events.
- **Textures:** [`Engine/Textures/Textures.md`](Engine/Textures/Textures.md) documents caller-owned blank and checkerboard runtime textures.

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A platform supported by MonoGame DesktopGL (Windows, macOS, or Linux)
- Linux users can use [mise](https://mise.jdx.dev/) to install and select the required .NET SDK automatically.

## Using the engine package

Add an exact engine version to a .NET 9 game project:

```bash
dotnet add package Hefty.Engine --version 0.1.0
```

The resulting project reference is explicit, so restoring the game continues to use that version until it is deliberately changed:

```xml
<PackageReference Include="Hefty.Engine" Version="0.1.0" />
```

To update, run the same command with the desired newer version or edit `Version` in the project file. To roll back, select an earlier version in the same way. Avoid floating versions such as `0.*` when reproducible game builds matter.

Create a world, then pass it to the engine host from the game's entry point:

```csharp
using Hefty.Engine;

using var game = new HeftyGame(new MainWorld());
game.Run();
```

See the [core API guide](Engine/Engine.md) for host configuration and examples of adding worlds, game objects, components, cameras, input, and rendering. Games that use `.mgcb` content should also reference `MonoGame.Content.Builder.Task` at the same MonoGame version used by the engine (`3.8.5.1`).

> `Hefty.Engine` is configured as a NuGet package but has not yet been published. Until it is published, use the local-package flow below.

## Building this repository

Clone the repository, restore the local MonoGame tools and dependencies, then run the sample project:

```bash
git clone <repository-url>
cd Hefty
dotnet tool restore
dotnet restore Hefty.sln
dotnet run --project Hefty.Sample.csproj
```

To compile the engine and sample without launching the game:

```bash
dotnet build Hefty.sln
```

To create `Hefty.Engine.0.1.0.nupkg` locally:

```bash
dotnet pack Engine/Hefty.Engine.csproj --configuration Release --output artifacts/packages
```

The default local package version is maintained in `Engine/Hefty.Engine.csproj`. To pack and then compile a standalone consumer against only that local package, run:

```bash
bash scripts/test-package.sh
```

## Releasing to NuGet

The [`Publish NuGet` workflow](.github/workflows/release.yml) publishes `Hefty.Engine` automatically when a GitHub Release is published. The release tag supplies the package version and must use semantic-version syntax, such as `v0.2.0` or `v0.2.0-beta.1`.

Before the first release, add a repository Actions secret named `NUGET_API_KEY` containing a NuGet.org API key with permission to push `Hefty.Engine`. Then create and publish a GitHub Release from the commit to distribute. The workflow builds the solution, tests a consumer against the packed package, stores both package files as a workflow artifact, and pushes the package and symbols to NuGet.org. Re-running an existing release is safe because duplicate package versions are skipped.

### Linux

The Linux helper scripts use `mise` and select .NET SDK `9.0.317`. Install `mise` first if it is not already available, then run:

```bash
./scripts/run-linux.sh
```

`run-linux.sh` and `build-linux.sh` run setup automatically. To prepare the dependencies without launching the game, run `setup-linux.sh` directly:

```bash
./scripts/setup-linux.sh
```

Use `./scripts/build-linux.sh` to compile without launching the game.

The scripts enable invariant globalization for compatibility with minimal Linux installations that do not include ICU. If ICU is installed, it can be disabled with `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0`.

## Controls

| Key | Action |
| --- | --- |
| `W`, `A`, `S`, `D` | Move the player |
| `M` | Open the main menu |
| `Enter` | Start the level from the main menu |
| `Escape` | Exit the game |

## Project Structure

```text
Engine/                 The complete engine implementation (`Hefty.Engine`)
Examples/
├── Components/         Example behaviours used to test the engine
├── GameObjects/        Example game objects used to test the engine
└── Worlds/             Example scenes that demonstrate the engine
Content/                MonoGame content-pipeline configuration and assets
Program.cs              Demonstration application entry point
Hefty.Sample.csproj      Executable demonstration project
Hefty.sln                Engine and sample solution
tests/Hefty.PackageSmoke Standalone local-package compile check
```

## Engine and Example Code

The `Engine/` directory and `Hefty.Engine` namespace contain the entire engine. Everything under `Examples/` is disposable demonstration code for exercising engine features and showing how a game could use them. Example namespaces follow the directory structure, such as `Hefty.Examples.Components` and `Hefty.Examples.GameObjects`.

A world implements `IWorld` and creates its scene in `Load`. It uses the supplied `WorldContext` to bind input, load content, add game objects, select a camera, and request deferred world changes. Each context is valid only while its world is active.

Every `GameObject` has a `Transform` and owns reusable `Component` instances. Components provide update behavior and rendering; `SpriteRenderer` draws textures in world or screen space. Calling `Destroy` safely removes an object and cleans up its components at the next engine boundary.

The included `LevelOne` world creates a procedurally generated floor, a controllable player, an obstacle, and a bounded camera that follows the player.

## Working with Content

The MonoGame content project is located at `Content/Content.mgcb`. Open it with the local content editor using:

```bash
dotnet mgcb-editor Content/Content.mgcb
```

Generated content output under `Content/bin` and `Content/obj` is excluded from version control.

## Status

This project is an early prototype intended for experimentation and learning. APIs and project structure may change as engine features are developed. It is available under the [MIT License](LICENSE).
