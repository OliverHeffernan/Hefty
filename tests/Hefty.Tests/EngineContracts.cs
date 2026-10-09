using System.Text.Json;
using System.Runtime.CompilerServices;
using Hefty.Engine;
using Hefty.Engine.Animation;
using Hefty.Engine.Collision;
using Hefty.Engine.Input;
using Hefty.Engine.State;
using Hefty.Engine.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Xunit;

namespace Hefty.Tests;

public sealed class EngineContracts
{
    private static PhysicsBody Body(CollisionWorld world, BodyType type, Vector2 position, Vector2 size,
        bool trigger = false, uint layer = 1)
    {
        GameObject owner = new();
        owner.Transform.Position = position;
        PhysicsBody body = owner.AddComponent(new PhysicsBody(type));
        body.AddCollider(new Collider(owner.Transform, size, Vector2.Zero, layer, isTrigger: trigger));
        world.Add(body);
        return body;
    }

    [Fact]
    public void WorldsAreIsolatedAndMovementIsConsumedOnce()
    {
        CollisionWorld first = new(), second = new();
        PhysicsBody mover = Body(first, BodyType.Kinematic, Vector2.Zero, new(10, 10));
        Body(second, BodyType.Static, new(20, -50), new(10, 100));
        mover.Move(new(80, 7));
        Assert.Equal(Vector2.Zero, mover.Transform.Position);
        first.Step(0);
        Assert.Equal(new Vector2(80, 7), mover.Motion.Actual);
        first.Step(0);
        Assert.Equal(Vector2.Zero, mover.Motion.Actual);
        Assert.Throws<InvalidOperationException>(() => second.Add(mover));
        first.Clear();
        Assert.Single(second.Query(new(0, -100, 100, 100)));
        Assert.Empty(first.Query(new(0, -100, 100, 100)));
    }

    [Fact]
    public void SweepSlidesAndReportsShapesNormalAndWholeStepTime()
    {
        CollisionWorld world = new();
        PhysicsBody mover = Body(world, BodyType.Kinematic, Vector2.Zero, new(10, 10));
        PhysicsBody wall = Body(world, BodyType.Static, new(30, -100), new(5, 300));
        mover.Velocity = new(100, 40);
        world.Step(1);
        Assert.InRange(mover.Transform.Position.X, 19.998f, 20);
        Assert.InRange(mover.Transform.Position.Y, 39.998f, 40);
        CollisionContact contact = Assert.Single(mover.Contacts);
        Assert.Same(mover.Colliders[0], contact.Shape);
        Assert.Same(wall.Colliders[0], contact.Other);
        Assert.Equal(-Vector2.UnitX, contact.Normal);
        Assert.Equal(0.2f, contact.Time, 5);
        Assert.Equal(new Vector2(0, 40), mover.Velocity);
        Assert.Equal(new Vector2(100, 40), mover.Motion.Requested);
        world.Step(0);
        Assert.Empty(mover.Contacts);
    }

    [Fact]
    public void MultipleImpactTimesRemainRelativeToWholeStep()
    {
        CollisionWorld world = new();
        PhysicsBody mover = Body(world, BodyType.Kinematic, Vector2.Zero, new(10, 10));
        Body(world, BodyType.Static, new(30, -100), new(5, 300));
        Body(world, BodyType.Static, new(-100, 40), new(300, 5));
        mover.Move(new(100, 100));
        world.Step(0);
        Assert.Equal(2, mover.Contacts.Count);
        Assert.Equal(0.2f, mover.Contacts[0].Time, 4);
        Assert.InRange(mover.Contacts[1].Time, 0.2999f, 0.3001f);
        Assert.Equal(-Vector2.UnitY, mover.Contacts[1].Normal);
    }

    [Fact]
    public void FiltersCanImplementDirectionalSurfacesAndTemporaryExclusion()
    {
        CollisionWorld world = new();
        PhysicsBody mover = Body(world, BodyType.Kinematic, new(0, 30), new(10, 10));
        PhysicsBody surface = Body(world, BodyType.Static, new(-50, 20), new(100, 5), layer: 2);
        bool excluded = false;
        world.ShouldResolve = hit => !excluded && hit.Normal.Y < 0 && hit.Movement.Y > 0;
        mover.Move(new(0, -30));
        world.Step(0);
        Assert.Equal(0, mover.Transform.Position.Y);
        mover.Move(new(0, 60));
        world.Step(0);
        Assert.InRange(mover.Transform.Position.Y, 9.998f, 10);
        Assert.Single(mover.Contacts);
        Assert.Same(surface.Colliders[0], Assert.Single(world.Query(new(-1, 19, 11, 26), 2)));
        excluded = true;
        mover.Move(new(0, 30));
        world.Step(0);
        Assert.InRange(mover.Transform.Position.Y, 39.998f, 40);
        Assert.Empty(mover.Contacts);
    }

    [Fact]
    public void DepenetrationAndLegacyEnterStayExitRemainAvailable()
    {
        CollisionWorld world = new();
        PhysicsBody mover = Body(world, BodyType.Kinematic, Vector2.Zero, new(10, 10));
        Body(world, BodyType.Static, new(8, -20), new(10, 50));
        world.Step(0);
        Assert.InRange(mover.Transform.Position.X, -2.002f, -2);
        Assert.True(Assert.Single(mover.Contacts).InitiallyOverlapping);
        Assert.Equal(2, mover.Contacts[0].Penetration);
        world.Clear();
        mover = Body(world, BodyType.Kinematic, Vector2.Zero, new(10, 10));
        Body(world, BodyType.Static, new(5, 0), new(10, 10), trigger: true);
        List<string> events = [];
        mover.Colliders[0].CollisionEntered += _ => events.Add("enter");
        mover.Colliders[0].CollisionStayed += _ => events.Add("stay");
        mover.Colliders[0].CollisionExited += _ => events.Add("exit");
        world.Step(0); world.Step(0);
        mover.Move(new(100, 0)); world.Step(0);
        Assert.Equal(new[] { "enter", "stay", "exit" }, events);
        Assert.Empty(mover.Contacts);
    }

    [Fact]
    public void FinalizationAlwaysRunsAfterEventsAndCannotReenter()
    {
        CollisionWorld world = new();
        PhysicsBody mover = Body(world, BodyType.Kinematic, Vector2.Zero, new(10, 10));
        Body(world, BodyType.Static, new(20, 0), new(5, 20));
        List<string> events = [];
        mover.Colliders[0].CollisionEntered += _ => events.Add("enter");
        world.Stepped += _ =>
        {
            events.Add("final");
            Assert.Throws<InvalidOperationException>(() => world.Step(0));
            Assert.InRange(mover.Motion.Actual.X, 9.998f, 10);
        };
        mover.Move(new(50, 0)); world.Step(0);
        Assert.Equal(new[] { "enter", "final" }, events);
        CollisionWorld empty = new();
        int finalized = 0;
        empty.Stepped += _ => finalized++;
        empty.Step(0); empty.Step(1);
        Assert.Equal(2, finalized);
    }

    [Fact]
    public void MasksAndRemovalDuringLegacyEventsPreserveOwnership()
    {
        CollisionWorld world = new(), other = new();
        PhysicsBody mover = Body(world, BodyType.Kinematic, Vector2.Zero, new(10, 10));
        PhysicsBody obstacle = Body(world, BodyType.Static, new(20, 0), new(5, 20), layer: 2);
        mover.Colliders[0].CollisionMask = 1;
        mover.Move(new(100, 0)); world.Step(0);
        Assert.Equal(100, mover.Transform.Position.X); Assert.Empty(mover.Contacts);
        mover.Transform.Position = Vector2.Zero;
        mover.Colliders[0].CollisionMask = uint.MaxValue;
        int exits = 0;
        mover.Colliders[0].CollisionEntered += _ => world.Remove(obstacle);
        mover.Colliders[0].CollisionExited += _ => exits++;
        mover.Move(new(100, 0)); world.Step(0);
        Assert.Equal(1, exits); Assert.Null(obstacle.CollisionWorld);
        Assert.DoesNotContain(obstacle.Colliders[0], world.Query(new(-100, -100, 200, 200)));
        other.Add(obstacle);
        mover.Owner.Destroy();
        Assert.Empty(world.Query(new(-100, -100, 200, 200)));
        Assert.Single(other.Query(new(-100, -100, 200, 200)));
    }

    private sealed class Source : IInputSource
    {
        public InputSnapshot Next = new(default, default);
        public int Samples;
        public InputSnapshot Sample() { Samples++; return Next; }
    }
    private static GamePadState Pad(Buttons buttons = 0, float trigger = 0, Vector2 stick = default) =>
        new(stick, Vector2.Zero, trigger, 0, buttons);

    [Fact]
    public void InputSamplesOnceAndAggregatesBindingsWithoutRepeatEdges()
    {
        Source source = new();
        InputManager input = new(source);
        input.Bind("Fire", new KeyboardBinding(Keys.Space));
        input.Bind("Fire", new GamePadButtonBinding(Buttons.A));
        Assert.Equal(0, source.Samples);
        source.Next = new(default, default, Pad()); input.Update();
        source.Next = new(new KeyboardState(Keys.Space), default, Pad(Buttons.A)); input.Update();
        Assert.True(input.IsPressed("Fire"));
        Assert.True(input.IsGamePadPressed(Buttons.A));
        for (int i = 0; i < 10; i++) Assert.True(input.IsHeld("Fire"));
        Assert.Equal(2, source.Samples);
        source.Next = new(default, default, Pad(Buttons.A)); input.Update();
        Assert.True(input.IsHeld("Fire")); Assert.False(input.IsReleased("Fire"));
        source.Next = new(default, default, Pad()); input.Update();
        Assert.True(input.IsReleased("Fire"));
    }

    [Fact]
    public void FocusAndReconnectDoNotInventEdgesButGenuinePressStillWorks()
    {
        Source source = new(); InputManager input = new(source);
        input.Bind("Confirm", new GamePadButtonBinding(Buttons.A));
        input.Update();
        source.Next = new(default, default, Pad(Buttons.A)); input.Update();
        Assert.True(input.IsHeld("Confirm")); Assert.False(input.IsPressed("Confirm"));
        source.Next = new(default, default); input.Update();
        Assert.False(input.IsReleased("Confirm"));
        source.Next = new(default, default, Pad(Buttons.A), IsFocused: false); input.Update();
        Assert.False(input.IsHeld("Confirm"));
        source.Next = new(default, default, Pad(Buttons.A)); input.Update();
        Assert.False(input.IsPressed("Confirm"));
        source.Next = new(default, default, Pad()); input.Update();
        source.Next = new(default, default, Pad(Buttons.A)); input.Update();
        Assert.True(input.IsPressed("Confirm"));
    }

    [Fact]
    public void AxesTriggersAndDeadzoneBoundariesAreReusable()
    {
        Assert.Equal(0, InputDeadzone.Apply(-0.2f));
        Assert.Equal(-0.5f, InputDeadzone.Apply(-0.6f), 5);
        Vector2 radial = InputDeadzone.ApplyRadial(new(0.36f, 0.48f));
        Assert.Equal(0.3f, radial.X, 5); Assert.Equal(0.4f, radial.Y, 5);
        Source source = new(); InputManager input = new(source);
        input.Bind("Trigger", new GamePadAxisBinding(GamePadAxis.LeftTrigger, 0.7f));
        source.Next = new(default, default, Pad(trigger: 0.69f)); input.Update();
        Assert.False(input.IsHeld("Trigger"));
        source.Next = new(default, default, Pad(trigger: 0.7f, stick: new(-0.6f, 0))); input.Update();
        Assert.True(input.IsPressed("Trigger"));
        Assert.Equal(-0.5f, input.GetAxis(GamePadAxis.LeftX), 5);
        Assert.Throws<ArgumentOutOfRangeException>(() => InputDeadzone.Apply(1, 1));
    }

    [Fact]
    public void SampledControllerNavigatesCanvasAndActivatesOnlyInteractiveFocus()
    {
        Source source = new(); InputManager input = new(source);
        input.Bind("UiNext", new GamePadButtonBinding(Buttons.DPadDown));
        input.Bind("UiConfirm", new GamePadButtonBinding(Buttons.A));
        UiCanvas canvas = new(() => new Rectangle(0, 0, 640, 480), new InputManagerUiInputSource(input));
        Button first = ButtonAt(20), second = ButtonAt(80);
        canvas.Add(first); canvas.Add(second);
        int activated = 0; second.Activated += _ => activated++;
        void Frame(Buttons buttons) { source.Next = new(default, default, Pad(buttons)); input.Update(); canvas.Update(new GameTime()); }
        Frame(0); Frame(Buttons.DPadDown);
        Assert.Same(first, canvas.FocusedElement);
        Frame(0); Frame(Buttons.DPadDown);
        Assert.Same(second, canvas.FocusedElement);
        Frame(Buttons.A); Frame(Buttons.A);
        Assert.Equal(1, activated);
        second.Enabled = false; Frame(0); Frame(Buttons.A);
        Assert.Equal(1, activated); Assert.Null(canvas.FocusedElement);
        Assert.Equal(new Rectangle(20, 80, 200, 40), second.Bounds);
    }
    private static Button ButtonAt(int y) => new(new(20, y), new(200, 40), null!, null!, "",
        Color.White, Color.Gray, Color.Blue, Color.Black, Color.White);

    private sealed class Resource : IAudioResource
    {
        public readonly List<Voice> Voices = [];
        public int Disposals;
        public bool FailOnPlay;
        public IAudioVoice CreateVoice() { Voice voice = new() { FailOnPlay = FailOnPlay }; Voices.Add(voice); return voice; }
        public void Dispose() => Disposals++;
    }
    private sealed class Voice : IAudioVoice
    {
        public bool IsStopped { get; set; } = true;
        public float Volume { get; set; }
        public bool Disposed;
        public bool FailOnPlay;
        public void Play() { if (FailOnPlay) throw new InvalidOperationException("Device unavailable."); IsStopped = false; }
        public void Stop() => IsStopped = true;
        public void Dispose() => Disposed = true;
    }

    [Fact]
    public void AudioOwnershipLimitsResetAndIsolationNeedNoAudioHardware()
    {
        using AudioManager first = new(2), second = new();
        Resource owned = new(), borrowed = new();
        first.RegisterSound("owned", owned, true); second.RegisterSound("borrowed", borrowed);
        first.MasterVolume = 0.5f; first.SfxVolume = 0.4f;
        first.PlaySfx("owned"); first.PlaySfx("owned"); first.PlaySfx("owned");
        second.PlaySfx("borrowed");
        Assert.Equal(2, first.ActiveVoiceCount);
        Assert.True(owned.Voices[0].IsStopped && owned.Voices[0].Disposed);
        Assert.Equal(0.2f, owned.Voices[2].Volume, 5);
        owned.Voices[1].IsStopped = true; first.Update();
        Assert.Equal(1, first.ActiveVoiceCount);
        first.Reset();
        Assert.Equal(1, owned.Disposals); Assert.Equal(1, second.ActiveVoiceCount);
        first.RegisterSound("again", borrowed); first.PlaySfx("again");
        first.StopAll(); Assert.Equal(0, borrowed.Disposals);
        second.Dispose(); Assert.Equal(0, borrowed.Disposals);
        AudioManager legacy = AudioManager.Instance; legacy.Dispose();
        Assert.NotSame(legacy, AudioManager.Instance);
    }

    [Fact]
    public void AudioFailuresReportDiagnosticsAndReleaseFailedVoices()
    {
        using AudioManager audio = new();
        audio.PlaySfx("missing"); Assert.NotNull(audio.LastDiagnostic);
        Resource failed = new() { FailOnPlay = true };
        audio.PlaySfx(failed);
        Assert.Contains("Device unavailable", audio.LastDiagnostic);
        Assert.Equal(0, audio.ActiveVoiceCount); Assert.True(failed.Voices[0].Disposed);
        Resource borrowed = new(); audio.PlaySfx(borrowed);
        audio.Dispose(); audio.Dispose();
        Assert.Equal(0, borrowed.Disposals); Assert.True(borrowed.Voices[0].Disposed);
        Assert.Throws<ObjectDisposedException>(() => audio.PlaySfx(borrowed));
    }

    private sealed record Settings(int Volume, string Theme);
    private sealed class Contributor : IGameStateContributor
    {
        public string SliceName => "test";
        public int Calls;
        public JsonElement WriteState(JsonSerializerOptions options) { Calls++; return JsonSerializer.SerializeToElement(Calls); }
        public void ReadState(JsonElement state, JsonSerializerOptions options) => Calls++;
        public void NewGame() => Calls++;
    }

    [Fact]
    public void GenericDataIsAtomicMigratableValidatedAndNeverTouchesContributors()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            Contributor contributor = new();
            GameStateManager saves = new(directory, [contributor], new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            saves.SaveData("settings", "settings", 1, new Settings(30, "dark"));
            Assert.Equal(new Settings(30, "dark"), saves.LoadData<Settings>("settings", "settings", 1));
            string original = File.ReadAllText(Path.Combine(directory, "settings.json"));
            var failure = saves.TrySaveData("settings", "settings", 1, new Settings(-1, "bad"),
                value => { if (value.Volume < 0) throw new ArgumentException(); });
            Assert.Equal(GameStateFailure.InvalidData, failure.Failure);
            Assert.Equal(original, File.ReadAllText(Path.Combine(directory, "settings.json")));
            Settings migrated = saves.LoadData<Settings>("settings", "settings", 2,
                (payload, version) => JsonSerializer.SerializeToElement(new { volume = payload.GetProperty("volume").GetInt32() + 5, theme = "light" }));
            Assert.Equal(new Settings(35, "light"), migrated);
            Assert.Equal(original, File.ReadAllText(Path.Combine(directory, "settings.json")));
            Assert.Equal(GameStateFailure.UnsupportedVersion, saves.TryLoadData<Settings>("settings", "settings", 2, out _).Failure);
            Assert.Equal(GameStateFailure.InvalidData, saves.TryLoadData<Settings>("settings", "other", 1, out _).Failure);
            Assert.Equal(GameStateFailure.InvalidSlot, saves.TrySaveData("../bad", "settings", 1, migrated).Failure);
            Assert.Equal(0, contributor.Calls);
            saves.Save("legacy", new SaveGame());
            Assert.NotNull(saves.Load("legacy"));
            Assert.Equal(2, contributor.Calls);
            Assert.Equal(GameStateFailure.InvalidData, saves.TryLoadData<Settings>("legacy", "settings", 1, out _).Failure);
            Assert.Empty(Directory.GetFiles(directory, "*.tmp-*"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void GenericDataRejectsFutureCorruptAndInvalidPayloads()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            GameStateManager saves = new(directory);
            saves.SaveData("future", "settings", 3, new Settings(30, "dark"));
            bool migrated = false;
            Assert.Equal(GameStateFailure.UnsupportedVersion, saves.TryLoadData<Settings>("future", "settings", 2, out _,
                (payload, _) => { migrated = true; return payload; }).Failure);
            Assert.False(migrated);
            File.WriteAllText(Path.Combine(directory, "bad.json"), "{ broken");
            Assert.Equal(GameStateFailure.InvalidData, saves.TryLoadData<Settings>("bad", "settings", 1, out _).Failure);
            Assert.Equal(GameStateFailure.NotFound, saves.TryLoadData<Settings>("missing", "settings", 1, out _).Failure);
            Assert.Equal(GameStateFailure.InvalidData, saves.TryLoadData<Settings>("future", "settings", 3, out _,
                validate: _ => throw new ArgumentException()).Failure);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void AnimationPauseKeepsSpriteAndPartialFrameThenManualAdvanceResumes()
    {
        // The animator never accesses GPU texture data. This placeholder is not drawn or disposed.
        Texture2D placeholder = (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        GC.SuppressFinalize(placeholder);
        SpriteRenderer sprite = new(placeholder, Vector2.One);
        SpriteAnimator animator = new(sprite) { AutoAdvance = false };
        animator.AddClip("loop", new AnimationClip([new(0, 0, 8, 8), new(8, 0, 8, 8), new(16, 0, 8, 8)], 10));
        animator.Play("loop"); animator.Advance(0.06);
        animator.IsPaused = true; animator.Advance(1);
        Assert.Equal(0, animator.FrameIndex); Assert.True(sprite.Enabled);
        animator.IsPaused = false; animator.Advance(0.04);
        Assert.Equal(1, animator.FrameIndex); Assert.Equal(new Rectangle(8, 0, 8, 8), sprite.SourceRectangle);
        animator.Advance(0.2); Assert.Equal(0, animator.FrameIndex);
        Assert.Throws<ArgumentOutOfRangeException>(() => animator.Advance(double.NaN));
    }
}
