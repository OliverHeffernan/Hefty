using System;
using System.IO;
using Hefty.Engine;
using Hefty.Engine.Animation;
using Hefty.Engine.Collision;
using Hefty.Engine.Input;
using Hefty.Engine.Textures;
using Hefty.Engine.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

// Optional native integration check. Ordinary `dotnet test` runs the shared headless contracts.
if (args.Length == 0 || args[0] != "--host")
{
    Console.WriteLine("Use dotnet test for headless contracts; --host [capture-directory] needs DesktopGL.");
    return;
}
Replay source = new();
HostWorld world = new();
using HeftyGame game = new(world, new HeftyGameOptions
{
    BackBufferWidth = 400, BackBufferHeight = 300, IsFixedTimeStep = false,
    ExitOnEscape = false, ExitOnGamePadBack = false, InputSource = source, ClearColor = Color.Black
});
void Frame(Buttons buttons = 0)
{
    source.Buttons = buttons;
    game.RunOneFrame();
    Check(world.Probe!.Finalizations == source.Samples, "Host must sample and finalize exactly once per update.");
    Check(world.Context!.Physics.StepIndex == source.Samples, "Host must step exactly once.");
}
void Capture(string name)
{
    if (args.Length < 2) return;
    Directory.CreateDirectory(args[1]);
    Color[] pixels = new Color[400 * 300];
    game.GraphicsDevice.GetBackBufferData(pixels);
    using Texture2D image = new(game.GraphicsDevice, 400, 300);
    image.SetData(pixels);
    using FileStream file = File.Create(Path.Combine(args[1], name + ".png"));
    image.SaveAsPng(file, 400, 300);
}
Frame(); Frame(); Capture("default");
Frame(Buttons.DPadDown); Capture("focused");
Check(world.Canvas!.FocusedElement == world.First, "D-pad must focus first button.");
Frame(); Frame(Buttons.DPadDown);
Check(world.Canvas.FocusedElement == world.Second, "Second navigation must move focus.");
Frame(Buttons.A); Frame(Buttons.A);
Check(world.Activations == 1, "Held confirm must activate only once.");
world.Second!.Enabled = false;
Frame(); Capture("disabled");
Check(world.Canvas.FocusedElement is null, "Disabled focus must clear.");
Check(world.Animator!.IsPaused && world.Sprite!.SourceRectangle == new Rectangle(0, 0, 1, 1), "Paused animation keeps its frame.");
Check(world.Probe!.ContactFreeFinalizations > 0, "Finalization must also run without contacts.");
int clockCalls = 0;
world.Animator.TimeSource = _ => { clockCalls++; return 0.1; };
world.Animator.IsPaused = false;
Frame();
Check(clockCalls == 1 && world.Animator.FrameIndex == 1, "Custom automatic clock must advance once.");
world.Animator.AutoAdvance = false;
Frame();
Check(clockCalls == 1 && world.Animator.FrameIndex == 1, "Manual mode must not also auto-advance.");
world.Animator.Advance(0.1);
Check(world.Animator.FrameIndex == 0, "Manual advancement must wrap the clip.");
using (AudioManager audio = new(2))
{
    SoundEffect generated = new(new byte[4410], 22050, AudioChannels.Mono);
    audio.RegisterSound("generated", new SoundEffectResource(generated, ownsEffect: true), ownsResource: true);
    audio.PlaySfx("generated");
    Check(audio.LastDiagnostic is null && audio.ActiveVoiceCount == 1, "Generated PCM must play through supplied-resource API.");
    audio.Reset();
    Check(generated.IsDisposed && audio.ActiveVoiceCount == 0, "Reset must release owned generated resources and voices.");
}
Console.WriteLine("PASS: packaged native host, once-per-update input/physics, post-physics order, controller UI, paused sprite, runtime textures.");

static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

sealed class Replay : IInputSource
{
    public Buttons Buttons;
    public int Samples;
    public InputSnapshot Sample()
    {
        Samples++;
        return new(default, new MouseState(399, 299, 0, 0, 0, 0, 0, 0),
            new GamePadState(Vector2.Zero, Vector2.Zero, 0, 0, Buttons));
    }
}

sealed class HostWorld : IWorld
{
    public WorldContext? Context;
    public Probe? Probe;
    public UiCanvas? Canvas;
    public Button? First, Second;
    public SpriteRenderer? Sprite;
    public SpriteAnimator? Animator;
    public int Activations;
    private Texture2D? texture;
    public void Load(WorldContext world)
    {
        Context = world;
        try { world.Physics.Step(0); throw new Exception("Manual hosted step accepted."); }
        catch (InvalidOperationException) { }
        try { world.Input.Update(); throw new Exception("Manual hosted sampling accepted."); }
        catch (InvalidOperationException) { }
        texture = TextureFactory.CreateBlankTexture(world.GraphicsDevice);
        GameObject mover = world.Add(new GameObject());
        PhysicsBody body = mover.AddComponent(new PhysicsBody(BodyType.Kinematic));
        body.AddCollider(new Collider(mover.Transform, new(10, 10), Vector2.Zero));
        Probe = mover.AddComponent(new Probe(body));
        body.Colliders[0].CollisionEntered += _ => Probe.ContactDelivered = true;
        GameObject wall = world.Add(new GameObject()); wall.Transform.Position = new(20, -50);
        wall.AddComponent(new PhysicsBody(BodyType.Static)).AddCollider(new Collider(wall.Transform, new(5, 100), Vector2.Zero));
        world.Input.Bind("UiNext", new GamePadButtonBinding(Buttons.DPadDown));
        world.Input.Bind("UiConfirm", new GamePadButtonBinding(Buttons.A));
        Canvas = world.Add(new UiCanvas(world.GraphicsDevice, new InputManagerUiInputSource(world.Input)) { RenderSpace = RenderSpace.Screen });
        First = MakeButton(60); Second = MakeButton(120);
        Canvas.Add(First); Canvas.Add(Second);
        Second.Activated += _ => Activations++;
        GameObject picture = world.Add(new GameObject { RenderSpace = RenderSpace.Screen });
        picture.Transform.Position = new(40, 220);
        Sprite = picture.AddComponent(new SpriteRenderer(texture, new(40, 40)) { Color = Color.Lime, PixelArt = true });
        Animator = picture.AddComponent(new SpriteAnimator(Sprite) { IsPaused = true });
        Animator.AddClip("hold", new AnimationClip([new(0, 0, 1, 1), new(0, 0, 1, 1)], 10)); Animator.Play("hold");
    }
    private Button MakeButton(int y) => new(new(40, y), new(320, 40), texture!, null!, "",
        Color.White, Color.Gray, Color.Gold, Color.DarkGray, Color.White);
    public void Unload(WorldContext world) => texture?.Dispose();
}

sealed class Probe(PhysicsBody body) : Component
{
    public int Finalizations, ContactFreeFinalizations;
    public bool ContactDelivered;
    protected override void Update(GameTime gameTime) => body.Move(Finalizations == 0 ? new(100, 0) : new(-1, 0));
    protected override void PostPhysics(GameTime gameTime)
    {
        if (Finalizations == 0 && (!ContactDelivered || body.Motion.Actual.X < 9.998f || body.Motion.Actual.X > 10))
            throw new InvalidOperationException("PostPhysics ran before motion/contact delivery.");
        if (body.Contacts.Count == 0) ContactFreeFinalizations++;
        Finalizations++;
    }
}
