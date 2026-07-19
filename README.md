# Hefty

A small 2D game-engine prototype built in C# with [MonoGame](https://monogame.net/). The project demonstrates a component-based update loop, world switching, sprites, camera tracking, keyboard input, and broad-phase collision detection.

All engine implementation files are contained in `Engine/` under the `Hefty.Engine` namespace. The files under `Examples/` are test and demonstration code; they are not part of the engine itself.

## Features

- Game objects composed from reusable update components
- Separate world-space and screen-space rendering
- 2D camera with zoom, rotation, bounds, and coordinate conversion
- Smooth camera-follow component
- Keyboard input with held and just-pressed key states
- Grid-based broad-phase collision detection
- Collision enter and exit callbacks
- Switchable worlds with initialization and cleanup hooks
- MonoGame content pipeline support

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A platform supported by MonoGame DesktopGL (Windows, macOS, or Linux)

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

A world implements `IWorld` and creates its scene in `Initialize`. Objects are registered with `Game1.Instantiate`, which adds them to the update and rendering loops according to the interfaces they implement. Calling `Game1.LoadWorld` queues a world change and safely clears the previous scene.

`GameObject` holds `IUpdater` components, while drawable objects implement the engine's `IDrawable` interface. Sprites can be rendered in either world space, through the active camera, or directly in screen space for UI.

The included `LevelOne` world creates a procedurally generated floor, a controllable player, an obstacle, and a bounded camera that follows the player.

## Working with Content

The MonoGame content project is located at `Content/Content.mgcb`. Open it with the local content editor using:

```bash
dotnet mgcb-editor Content/Content.mgcb
```

Generated content output under `Content/bin` and `Content/obj` is excluded from version control.

## Status

This project is an early prototype intended for experimentation and learning. APIs and project structure may change as engine features are developed.
