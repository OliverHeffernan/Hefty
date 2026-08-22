# Audio service

`AudioManager` is a singleton-style service backed by MonoGame's content pipeline. Define explicit game IDs and initialize it during your game's `LoadContent`:

```csharp
var audio = AudioManager.Instance;
audio.Diagnostic += message => Console.Error.WriteLine(message);
audio.Initialize(Content, new AudioCatalog(
    new("jump", "Audio/Sfx/jump", AudioKind.Sfx),
    new("theme", "Audio/Music/theme", AudioKind.Music)));

audio.PlaySfx("jump");
audio.PlayMusic("theme", loop: true);
```

Asset names are content-pipeline names (without file extensions), not filesystem paths. Add the corresponding sound effects and songs to `Content.mgcb`; this repository currently supplies none.

`MasterVolume`, `MusicVolume`, and `SfxVolume` accept finite values and clamp them to 0–1. Changes apply immediately to music and tracked SFX instances. `StopMusic` only stops music. Unknown IDs, wrong audio kinds, pre-initialization calls, and load/playback failures do not throw; inspect `LastDiagnostic` or subscribe to `Diagnostic`.

Call `Dispose` when the game shuts down. The manager disposes only the `SoundEffectInstance` objects it creates and never unloads or disposes the caller's `ContentManager` or its cached assets. A disposed singleton cannot be reinitialized.
