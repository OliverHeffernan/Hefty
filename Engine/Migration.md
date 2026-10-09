# Migrating Hefty.Engine 0.3.1 → 0.4.0

0.4.0 marks new lifecycle/ownership contracts in this pre-1.0 engine. Existing hosted
worlds, component Update/Draw hooks, Collider enter/stay/exit events, keyboard/mouse
bindings, sprite/UI/camera/texture APIs, content catalogs, and version-1 SaveGame files
continue to work. .NET 10 and MonoGame DesktopGL 3.8.5.1 remain the requirements.

## Physics and finalization

- The internal static CollisionManager becomes public instance-owned CollisionWorld;
  there is no public global fallback. Each hosted WorldContext.Physics is independent.
  Consumers that copied source or used reflection into internal globals must migrate.
- Hosted bodies still register automatically. For headless simulation, attach a PhysicsBody
  to a GameObject, add same-transform colliders, then CollisionWorld.Add and Step it.
  Remove/Clear discard pending displacement; ownership cannot cross worlds simultaneously.
- Do not manually step/check a hosted world or sample its input: these throw. Standalone
  Step is synchronous; Step(0) is still a step; recursive stepping throws. No background loop.
- Override Component.PostPhysics for resolved-state finalization instead of relying on
  collision callbacks to finish a frame. It runs even without contacts. World-level
  Physics.Stepped runs first. Movement queued there applies next update.
- Contacts report solid **impacts**, not persistent grounding. Use Query/GetFloatBounds
  for resting surfaces. ShouldResolve controls solid response only; geometric legacy events
  are preserved. No game-specific rules are introduced. Solver tolerances/limits remain.

## Input and UI

- Construction is now side-effect-free. First Update establishes an edge-free baseline,
  as do focus/reconnect transitions. Held inputs remain held, but must release/press before
  generating a new confirmation. No synthetic releases on disconnect or focus loss.
- Rebinding evaluates sampled snapshots; removing a held binding does not synthesize an
  action release. If your adapter needs cancellation, explicitly cancel module state.
- Existing IInputBinding.IsDown(keyboard,mouse) implementations remain compatible through
  a default snapshot overload. Use pad button/axis bindings and deadzones for controllers.
- Use HeftyGameOptions.InputSource for injection and InputManagerUiInputSource at the UI
  boundary. Native exit handling uses the same sample; no extra GamePad.GetState call.
- UiCanvas has an additive headless bounds-provider constructor and Update method. Existing
  hosted usage is unchanged. Keep RenderSpace.Screen; no widget knows WorldContext.

## Audio

- Prefer new AudioManager over Instance; the host does not initialize/update/dispose it.
  Call Update once per simulation update and Dispose when the adapter/world ends.
- Default SFX capacity is now 32 active tracked voices, stealing the oldest at capacity;
  pass maxVoices to choose another bound. No unbounded voice growth.
- Reset is reusable cleanup; Dispose remains terminal for the instance. Legacy Instance
  access after disposal returns a fresh manager; old references are not revived.
- Content assets remain borrowed. Supplied IAudioResource/IAudioVoice support injection.
  RegisterSound owns a resource only with ownsResource:true; SoundEffectResource owns its
  underlying effect only with ownsEffect:true. StopAll does not release registrations.
- MediaPlayer music is still global, not an isolated mixer. The last PlayMusic owns it;
  only that manager changes/stops its playback. Independently mixed streams need a supplied
  resource/backend. Arbitrary custom backend errors propagate; expected playback failures
  retain diagnostics.

## Saves and animation

- SaveData<T>/LoadData<T> are separate generic envelopes with explicit type ID/version,
  validation and payload migration. They never capture/apply contributors or other services.
  Keep legacy SaveGame files and APIs unchanged; map to a new slot explicitly if migrating.
  Never overwrite a legacy slot unintentionally: both APIs use the same slot filenames.
- SpriteAnimator still animates source rectangles. IsPaused freezes partial progress without
  hiding the sprite. AutoAdvance=false plus Advance supports manual clocks; TimeSource supplies
  automatic delta time. Pause also blocks Advance. Avoid manual + automatic double advancement.

## Checks and release

`dotnet build Hefty.sln -c Release` builds the sample and engine.
`dotnet test tests/Hefty.Tests/Hefty.Tests.csproj -c Release` runs headless contracts.
`bash scripts/test-package.sh` packs and runs the same contracts against the standalone
NuGet consumer with a fresh package cache (no source project reference).

For native integration, after packing build/restore the package consumer then run:
`dotnet run --project tests/Hefty.PackageSmoke -c Release -- --host [capture-directory]`.
On Linux a desktop or `xvfb-run -a` with Mesa is required. This checks host ordering,
double-step prevention, sampled controller UI, and paused sprite rendering. Tests inject
devices; they are not a claim of physical controller/hardware-audio coverage.

Publish only through the existing GitHub Release → release.yml → production → NuGet
trusted-publishing path after source and packed tests pass. Use tag v0.4.0 on the verified
commit. A release/workflow start is not delivery: wait for workflow success and fetch the
exact 0.4.0 package from NuGet.org before claiming publication. No NuGet secrets belong in
commands or documentation.
