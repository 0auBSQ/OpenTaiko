# OpenTaiko.Android

The Android host for OpenTaiko — the counterpart of `OpenTaiko.iOS`, built on the same FDK
hosted-mode contract (`InitWithExternalContext` / `RenderHostedFrame`): the shared `OpenTaiko` and
`FDK` projects compile unchanged as net8.0 libraries, and this thin app owns the window, EGL
context, game loop, input, storage, and audio session.

## Building

The one-shot way (checks the workload, finds the Android SDK + JDK, fetches missing
natives/binding source, builds with the right flags):

```
powershell -ExecutionPolicy Bypass -File OpenTaiko.Android/scripts/build.ps1              # Debug APK
powershell -ExecutionPolicy Bypass -File OpenTaiko.Android/scripts/build.ps1 -Release    # Release APK
powershell -ExecutionPolicy Bypass -File OpenTaiko.Android/scripts/build.ps1 -Install    # + deploy to a connected device
powershell -ExecutionPolicy Bypass -File OpenTaiko.Android/scripts/build.ps1 -Run        # + launch it
```

**Songs — how players get them:** the APK ships without songs. On first launch (right after
asset extraction) the app offers to download the official soundtrack from the OpenTaiko-Soundtrack
GitHub repo straight into `files/Songs`. The repo's `soundtrack_info.json` (the Hub's index)
drives it: the dialog shows the actual pending weight, downloads are resumable, and once installed
a silent per-launch check re-fetches any song whose chart MD5 changed (Hub-style updates). Free-
space checked; declinable ("Not now" asks again next launch, "Never" doesn't). See
`SoundtrackDownloader.cs`. Manual copying over USB into
`Android/data/com.opentaiko.OpenTaiko/files/Songs` always works too.

Developer song options for the build itself:

```
build.ps1 -Release -BundleSongs -SongsPath "..\OpenTaiko-Soundtrack\01 OpenTaiko Chapter I"
build.ps1 -Release -Install -PushSongs                # full library over adb after install
```

- `-BundleSongs` packs the folder into the APK as one `songs.zip` asset (aapt2 rejects the
  unicode filenames real charts have, so individual files can't ship as assets), unpacked into
  `Songs/` on first run without overwriting user files. Dot-folders (`.git`!) and Windows junk
  files are filtered out. Capped at ~1.8 GB: APKs are 32-bit zips and bigger ones fail to
  install — the full ~4.8 GB soundtrack does not fit; bundle per-chapter subsets instead.
  The repo's own `Songs` folder is never packed.
- `-PushSongs` adb-pushes the folder into the app's `files/Songs` after the build — no size limit,
  needs the device connected (on Android 11+ a few devices block shell writes to `Android/data`).
- `-SongsPath` defaults to the `OpenTaiko-Soundtrack` checkout next to the repo.
- The songs zip is rebuilt only when missing from `obj/` — use `-Clean` after changing songs.

Other options: `-Clean` (wipe obj/bin first), `-AndroidSdk PATH` / `-JavaSdk PATH` when
auto-detection fails. Only hard prerequisites: .NET 8 SDK with the Android workload
(`dotnet workload install android`), an Android SDK, and a JDK 17 (Android Studio's default
install provides both).

Two hard-won build rules the script enforces (guard targets error if violated):

- **Release must compile with `-p:Optimize=false`** (globally, so FDK/OpenTaiko get it too):
  csc-optimized IL crashes the Mono 8.0.10 Android interpreter at boot with
  `Assertion: should not be reached at interp.c:3847` + SIGABRT a few seconds into skin loading.
  Bisected on the emulator (Debug+`Optimize=true` crashes identically). Revisit on runtime upgrade.
- Assemblies are embedded in every APK (`EmbedAssembliesIntoApk=true`): Debug otherwise relies on
  ".NET fast deployment" and a hand-installed Debug APK aborts with "No assemblies found".

### Manual steps (what build.ps1 automates)

1. BASS natives: `powershell -ExecutionPolicy Bypass -File scripts/download-bass.ps1`
   (fetches bass / bassmix / bass_fx for arm64-v8a + x86_64 from un4seen.com into `jniLibs/`).
2. FFmpeg natives (video playback): `powershell -ExecutionPolicy Bypass -File scripts/download-ffmpeg.ps1`
   (fetches the FFmpeg 5.1.2 LGPL shared libraries for both ABIs from Maven Central into `jniLibs/`).
3. FFmpeg.AutoGen binding source: `powershell -ExecutionPolicy Bypass -File scripts/fetch-ffmpeg-autogen.ps1`
   (fetches + patches the vendored binding under `third_party/FFmpeg.AutoGen/upstream/`;
   the stock NuGet binding throws `PlatformNotSupportedException` on Android — see the README there).

The launcher icons under `Resources/mipmap-*` are generated from `OpenTaiko/OpenTaiko.ico` by
`py -3 scripts/make-appicons.py` (Pillow required) — rerun it only when the logo changes.

Then (`-p:UseVendoredFFmpeg=true` is required; the build errors without it):

```
dotnet build OpenTaiko.Android/OpenTaiko.Android.csproj -f net8.0-android -p:UseVendoredFFmpeg=true
dotnet build OpenTaiko.Android/OpenTaiko.Android.csproj -f net8.0-android -p:UseVendoredFFmpeg=true -t:Install   # deploy to a connected device
```

If the build can't find the Android SDK or complains about the JDK version (it wants JDK 17 —
Android Studio's bundled JBR works), pass the paths explicitly:

```
dotnet build OpenTaiko.Android/OpenTaiko.Android.csproj -f net8.0-android -p:UseVendoredFFmpeg=true ^
  -p:AndroidSdkDirectory="%LOCALAPPDATA%\Android\Sdk" ^
  -p:JavaSdkDirectory="C:\Program Files\Android\Android Studio\jbr"
```

## Architecture

| Concern | Implementation |
|---|---|
| Window / GL | `SurfaceView` + `AndroidGLContext` (EGL14, ES 3.0, context parked on a pbuffer while backgrounded so GL resources survive) |
| Game loop | Dedicated render thread; NLua stays thread-affine to it. Renders the hosted frame off-screen at the skin's logical resolution, then aspect-fit `glBlitFramebuffer` + `eglSwapBuffers` (vsync-paced) |
| Input | Touch zones → single-frame HID key pulses (`CInputKeyboard_Android`, identical mapping to iOS: Don circle / Ka halves / ESC corner / Config D-pad); hardware keyboards via Keycode→HID |
| Storage | APK assets extracted once to `getExternalFilesDir()` (`AssetExtractor`, never overwrites user files); the game roots all paths there via the current directory |
| Audio | BASS with the standard mixer pipeline; before `Bass.Init` the host feeds the device's native sample rate + burst size (AudioManager `PROPERTY_OUTPUT_SAMPLE_RATE` / `PROPERTY_OUTPUT_FRAMES_PER_BUFFER`) into `CSoundDeviceBASS.Android.cs`, which burst-aligns `BASS_CONFIG_DEV_PERIOD` and shrinks the device buffer so AAudio (8.1+) opens its LOW_LATENCY fast-mixer stream |
| Video | FFmpeg 5.1.2 shared libraries (Bytedeco LGPL builds) in `jniLibs/` + the vendored FFmpeg.AutoGen 5.1.1 binding compiled from patched source (`UseVendoredFFmpeg=true`); `CVideoDecoder` loads the unversioned `lib*.so` by soname |
| Lua | Unchanged from desktop: NLua/KeraLua with the `liblua54.so` KeraLua's NuGet package ships for Android; `UseInterpreter=true` keeps NLua's dynamic dispatch working |
| Text input | `AlertDialog` + `EditText` behind the shared `CTextInput` host hook |
| Lifecycle | `onPause` pulses ESC during gameplay (in-game pause), idles the loop, `Bass.Pause()`; `onResume` restores; `onTrimMemory` evicts least-recently-drawn textures |

## Known gaps

- Songs/skins are user-managed under `Android/data/com.opentaiko.OpenTaiko/files/` on external
  storage (visible over USB/MTP).
- x86_64 jniLibs are included for emulator use; add `armeabi-v7a` to the script + csproj if 32-bit
  devices matter.
