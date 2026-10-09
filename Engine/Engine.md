# Hefty core API

Hefty uses four core types:

- `HeftyGame` is the MonoGame host and runs one world at a time.
- `IWorld` defines the lifetime of one scene or menu.
- `GameObject` supplies identity, a transform, ordering, and component ownership.
- `Component` is the extension point for behavior and rendering.

## Installing the package

Pin the engine version in the game project so upgrades remain deliberate and reproducible:

```bash
dotnet add package Hefty.Engine --version 0.4.0
```

Change the version in the resulting `PackageReference` when the game is ready to update or roll back. Games using the MonoGame content pipeline should also reference `MonoGame.Content.Builder.Task` version `3.8.5.1` and include their `.mgcb` file in the game project.

## Starting a game

Pass the first world to `HeftyGame` and run it like any other MonoGame game:

```csharp
using Hefty.Engine;

using var game = new HeftyGame(new LevelOne());
game.Run();
```

Pass `HeftyGameOptions` to configure the host before MonoGame initializes its graphics device:

```csharp
using Hefty.Engine;
using Microsoft.Xna.Framework;

HeftyGameOptions options = new()
{
    BackBufferWidth = 1280,
    BackBufferHeight = 720,
    IsFullScreen = false,
    IsMouseVisible = true,
    IsFixedTimeStep = true,
    ExitOnEscape = true,
    ExitOnGamePadBack = true,
    ClearColor = Color.Black,
    ContentRootDirectory = "Content"
};

using var game = new HeftyGame(new LevelOne(), options);
game.Run();
```

These values are fixed for the lifetime of the host. Width, height, and the content root are validated when `HeftyGame` is constructed.

## Loading a world

`IWorld.Load` receives a `WorldContext` with the services and commands valid for that world. Bind input, load content, create objects, and select a camera there. Each world receives a fresh input manager, so it establishes the bindings it uses without inheriting actions or held-state edges from the previous world.

```csharp
using Hefty.Engine;
using Hefty.Engine.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public sealed class LevelOne : IWorld
{
    public void Load(WorldContext world)
    {
        world.Input.Bind("MoveRight", new KeyboardBinding(Keys.D));
        world.Input.Bind("OpenMenu", new KeyboardBinding(Keys.M));

        Texture2D texture = world.Content.Load<Texture2D>("Sprites/player");
        Player player = new(texture) { Name = "Player" };
        player.Transform.Position = new Vector2(400, 300);
        world.Add(player);

        Camera2D camera = world.Add(new Camera2D());
        camera.Transform.Position = player.Transform.Position;
        world.ActiveCamera = camera;
    }
}
```

`WorldContext.Add` returns the same object for concise construction. Addition is deferred to a safe engine boundary, so it is also safe to call from component update or draw code. An object can belong to only one world, and a destroyed object cannot be added again.

A context becomes inactive before its world's `Unload` method runs. Content and graphics services remain available for cleanup, but calls to `Add`, `ChangeWorld`, or the active-camera setter through that stale context throw. Do not retain a context beyond its world lifetime.

## Creating game objects and components

Every object has one permanent `Transform` containing `Position` and `Scale`. Components attached to the object share that transform:

```csharp
public sealed class Player : GameObject
{
    public Player(Texture2D texture)
    {
        AddComponent(new SpriteRenderer(texture, new Vector2(50, 50)));
        AddComponent(new PlayerController());
    }
}

public sealed class PlayerController : Component
{
    public float Speed { get; set; } = 200f;

    protected override void Update(GameTime time)
    {
        if (!World.Input.IsHeld("MoveRight"))
            return;

        float seconds = (float)time.ElapsedGameTime.TotalSeconds;
        Transform.Position += Vector2.UnitX * Speed * seconds;
    }
}
```

`AddComponent` returns the attached component. `GetComponent`, `TryGetComponent`, and `RemoveComponent` provide type-based lookup and explicit removal. A component can have only one owner. Component additions and removals requested during an update or draw pass are applied after that pass.

Override the protected lifecycle, update, and draw hooks to implement a component. The engine invokes them in this order:

1. `OnAdded` runs when the component gains an owner. The object may not belong to a world yet.
2. `OnWorldAttached` runs whenever the owner enters a world; `World` is available here.
3. `Update` runs while both the object and component are enabled.
4. Physics resolves motion and dispatches contacts, then `PostPhysics` runs while object and component are enabled.
5. `Draw` runs while the object is visible and the component is enabled.
6. `OnWorldDetached` runs before the owner leaves its world, while `World` is still available.
7. `OnRemoved` runs once before the component loses its owner, including during object destruction.

`Owner`, `Transform`, and `World` are convenient component properties. `World` is unavailable until the owner enters a world and after it leaves.

## Rendering and ordering

`SpriteRenderer` draws a texture using its owner's transform. `RenderSpace.World` applies the active camera; `RenderSpace.Screen` uses viewport coordinates for menus and UI:

```csharp
GameObject panel = new()
{
    RenderSpace = RenderSpace.Screen,
    DrawOrder = 100
};
panel.AddComponent(new SpriteRenderer(texture, new Vector2(300, 100)));
world.Add(panel);
```

Set `PixelArt` to use sharp point sampling when a sprite is scaled:

```csharp
gameObject.AddComponent(new SpriteRenderer(texture, new Vector2(64, 64))
{
    PixelArt = true
});
```

Objects default to enabled, visible, world-space rendering, and order zero. `UpdateOrder` and `DrawOrder` sort objects independently. Component properties with the same names sort components within their owner. The order of addition breaks ties deterministically.

`SpriteRenderer` does not own or dispose its texture. `ContentManager` owns assets loaded through `world.Content`. A world must dispose textures or other resources it creates manually in `Unload`.

## Destruction and world changes

Call `Destroy` on an object rather than modifying engine collections:

```csharp
if (World.Input.IsPressed("Delete"))
    Owner.Destroy();
```

Destruction is idempotent. It immediately prevents any remaining update or draw callbacks for that object, then removes the object and its components at the next safe boundary. Destroying an object before it enters a world cleans it up immediately. Components unregister resources such as physics bodies as they detach.

Request a deferred world switch through the active context:

```csharp
if (World.Input.IsPressed("OpenMenu"))
    World.ChangeWorld(new MainMenu());
```

The current update and collision step finish before the old world unloads. The engine then deactivates its context, calls `Unload`, destroys all old objects, clears collision and input state, and loads the new world. If several changes are requested before that boundary, the last request wins.

## Integration boundaries in 0.4.0

The host's update order is: one input sample → object/component `Update` → one physics
step → collider events → `Physics.Stepped` → object/component `PostPhysics` → destruction
and deferred world switch. Finalization runs on contact-free frames too. Ordering within
both component passes uses `UpdateOrder` and stable insertion order. A component added
during Update can participate in PostPhysics that frame; destroyed/disabled objects do not.
Queue movement in Update and read `PhysicsBody.Motion`/`Contacts` in PostPhysics.
Movement queued during finalization belongs to the next update. `HeftyGame` remains sealed;
components and the world-owned `Stepped` event are supported extension points.

Each `WorldContext.Physics` owns its bodies and pairs, and rejects manual `Step` and
`CheckCollisions`. `new CollisionWorld()` and `new InputManager(source)` are synchronous,
headless services. Hosted input rejects manual Update; use `HeftyGameOptions.InputSource`
to supply device snapshots. These services are single-threaded; deterministic replay requires
the same registration order, snapshots, delta times, and mutations. This is not a promise of
bit-identical floating-point results across hardware. Do not retain old world services.

Audio and saves are explicitly caller-owned: no service locator, automatic audio Update,
or automatic state application. Inject adapters into game modules rather than passing the
whole WorldContext. A module can emit movement/audio/save intents; its adapter owns their
translation into engine calls. No character, damage, jump, stock, or hitstop policy is built in.

## Subsystem reference

The package includes these guides under `docs/`; the XML API file ships beside the assembly.

| Subsystem | Public API and ownership | Guide |
| --- | --- | --- |
| Collision | CollisionWorld, PhysicsBody, Collider, Aabb; swept kinematic/static AABBs, no gravity/dynamics | [Collision](https://github.com/OliverHeffernan/Hefty/blob/main/Engine/Collision/Collision.md) |
| Input | InputManager, IInputSource, InputSnapshot, bindings, deadzones; once per Update | [Input](https://github.com/OliverHeffernan/Hefty/blob/main/Engine/Input/Input.md) |
| UI | UiCanvas, Button, Label, Panel, ProgressBar; caller-supplied input/font/texture | [UI](https://github.com/OliverHeffernan/Hefty/blob/main/Engine/UI/UI.md) |
| Audio | AudioManager, AudioCatalog, IAudioResource/IAudioVoice; caller updates/disposes, bounded SFX | [Audio](https://github.com/OliverHeffernan/Hefty/blob/main/Engine/Audio/Audio.md) |
| Saves/settings | GameStateManager; arbitrary typed data or legacy SaveGame/contributors | [State](https://github.com/OliverHeffernan/Hefty/blob/main/Engine/State/State.md) |
| Animation | AnimationClip, SpriteAnimator; source rectangles, pause/manual/custom clock, no skeleton | [Animation](https://github.com/OliverHeffernan/Hefty/blob/main/Engine/Animation/Animation.md) |
| Runtime textures | TextureFactory blank/checkerboard; caller disposes | [Textures](https://github.com/OliverHeffernan/Hefty/blob/main/Engine/Textures/Textures.md) |

`SpriteRenderer` borrows its texture, supports tint/source rectangles, size/scale and pixel-art
sampling. `Camera2D` is a GameObject with zoom, rotation, optional Bounds, view matrices and
screen/world coordinate conversion. Assign an added camera to `world.ActiveCamera`; screen
objects bypass it. `CameraFollow` is sample code, not a packaged API. `world.Content.Load<T>`
loads MonoGame XNB assets owned by ContentManager; caller-created resources remain caller-owned.
No fonts, textures, sounds, or content builder are bundled. The host owns its SpriteBatch;
components must not change batch state themselves.

See the packaged `docs/Migration.md` or the repository's
[migration notes](https://github.com/OliverHeffernan/Hefty/blob/main/Engine/Migration.md)
before upgrading from 0.3.1.
