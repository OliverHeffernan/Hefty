# Audio service

`AudioManager` is an independently owned service. Define explicit game IDs and initialize it during your world's Load or game's LoadContent:

```csharp
var audio = new AudioManager(maxVoices: 32);
audio.Diagnostic += message => Console.Error.WriteLine(message);
audio.Initialize(Content, new AudioCatalog(
    new("jump", "Audio/Sfx/jump", AudioKind.Sfx),
    new("theme", "Audio/Music/theme", AudioKind.Music)));

audio.PlaySfx("jump");
audio.PlayMusic("theme", loop: true);
```

Asset names are content-pipeline names (without file extensions), not filesystem paths. Add the corresponding sound effects and songs to `Content.mgcb`; this repository currently supplies none.

`MasterVolume`, `MusicVolume`, and `SfxVolume` accept finite values and clamp them to 0–1. Changes apply immediately to music and tracked SFX instances. `StopMusic` only stops music. Unknown IDs, wrong audio kinds, pre-initialization calls, and load/playback failures do not throw; inspect `LastDiagnostic` or subscribe to `Diagnostic`.

Call `Update` once per game update to release completed SFX instances. **HeftyGame does not
update audio automatically.** Your adapter/component owns Update and cleanup. It is safe
to call before initialization or after disposal. Pruning also occurs when playing SFX.

Call `Dispose` when that owner shuts down. ContentManager and content-loaded assets remain
borrowed. Existing `AudioManager.Instance` calls still work; accessing Instance after it is
disposed creates a fresh manager (existing references stay disposed). Prefer explicit instances
for tests, multiple adapters and predictable lifetimes. The service is single-threaded.

## Supplied/generated sound and injected playback

```csharp
// PCM: signed 16-bit interleaved mono/stereo samples, following MonoGame SoundEffect's contract.
var generated = new SoundEffect(pcmBytes, sampleRate, AudioChannels.Mono);
var resource = new SoundEffectResource(generated, ownsEffect: true);
audio.RegisterSound("tone", resource, ownsResource: true);
audio.PlaySfx("tone");
```

`SoundEffectResource` borrows its SoundEffect by default. If ownsEffect is true, disposing
the wrapper disposes the effect. RegisterSound transfers wrapper disposal only when
ownsResource is true; otherwise the caller must outlive all active voices and dispose it.
IDs must be nonempty and unique. Registered sound IDs take precedence over content SFX IDs.
`PlaySfx(IAudioResource)` borrows a resource for direct playback without catalog initialization.
Implement IAudioResource/IAudioVoice for a custom backend or a headless fake; the manager
owns each returned voice and applies volume/play/stop/dispose. Neither construction nor
injected SFX playback requires native audio. Native SoundEffect/Song playback still does.

At MaxVoices capacity, a new voice stops/disposes the oldest active voice. Completed voices
are pruned on Update; ActiveVoiceCount reports tracked voices, not a hardware playing count.
`StopAll` stops/disposes voices but retains registered resources/content and volumes.
`Reset` additionally disposes owned registrations, clears borrowed registrations and content
references/diagnostics, and allows new registration/Initialize. Volume settings persist.
`Dispose` performs Reset then makes that instance terminal, and is idempotent. Explicit
resource calls after disposal throw; legacy ID playback reports diagnostics as before.
Expected content/playback failures report diagnostics; arbitrary custom backend exceptions
outside the documented MonoGame failure classes propagate. Resource/voice Dispose must be
idempotent and nonthrowing. Do not transfer ownership of one resource to several managers.

Music/content/diagnostics remain available. **MonoGame MediaPlayer is process-global**:
PlayMusic replaces the current song and that manager becomes the music owner. Other
managers' volume changes, StopMusic, Reset and Dispose do not stop/change its music.
This is not independently mixed music; use injected SFX resources/backends if independently
owned streams are required. The host does not automatically initialize or dispose audio.
