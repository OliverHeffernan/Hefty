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

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A platform supported by MonoGame DesktopGL (Windows, macOS, or Linux)
- Linux users can use [mise](https://mise.jdx.dev/) to install and select the required .NET SDK automatically.

## Getting Started

Clone the repository, restore the local MonoGame tools and dependencies, then run the project:

```bash
git clone <repository-url>
cd Hefty
dotnet tool restore
dotnet restore
dotnet run
```

To compile without launching the game:

```bash
dotnet build
```

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
├── Textures/           Example procedural texture helpers
└── Worlds/             Example scenes that demonstrate the engine
Content/                MonoGame content-pipeline configuration and assets
Program.cs              Demonstration application entry point
Hefty.csproj
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

This project is an early prototype intended for experimentation and learning. APIs and project structure may change as engine features are developed.
