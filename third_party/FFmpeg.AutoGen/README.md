# FFmpeg.AutoGen 5.1.1 binding (iOS + Android)

The mobile builds patch and compile the FFmpeg.AutoGen 5.1.1 managed binding from source.

Desktop (Windows/Linux/macOS) keeps using the stock NuGet package.

## Why

FFmpeg.AutoGen 5.1.1 detects the OS via `RuntimeInformation.IsOSPlatform(Windows|Linux|OSX)` and
throws `PlatformNotSupportedException` for anything else. iOS and Android match none of those, so:

- `LibraryLoader`'s `GetPlatformId` throws in `ffmpeg`'s static constructor (it computes the
  library naming and `EAGAIN`), poisoning the whole `ffmpeg` type before any of our code runs.
  Because the throw happens during type initialization, no runtime override can prevent it; the
  fix has to be in the binding's own source.
- `FunctionLoader.GetFunctionPointer` throws on the per-function `dlsym` hot path.

iOS is Darwin, so the macOS code paths (dylib naming, `dlsym`, `EAGAIN=35`) are correct for it.
Android is Linux-like (`EAGAIN=11`) but bionic has no `libdl.so.2`, so it gets its own
`AndroidNativeMethods` (`libdl.so`); the Android host also overrides `ffmpeg.GetOrLoadLibrary`
to load the unversioned `lib*.so` names an APK requires (see `FDK CVideoDecoder`).
Upstream only added non-desktop platforms in the 6.0/8.1 rewrite, which replaced the static
bindings with runtime-generated delegates that throw `NotSupportedException` under iOS AOT, so
5.1.1 is the only mobile-viable line for the ffmpeg 5.1 ABI our native builds ship.

## Layout

- `ios-darwin-fallback.patch`: the iOS patch to apply to the binding source.
- `android-bionic-fallback.patch`: the Android patch, applied on top of the iOS one.
- `upstream/` directory to store patched ffmpeg source, not checked in.
- `FFmpeg.AutoGen.csproj` - minimal csproject compiling upstream/.

Fetch + patch with `OpenTaiko.iOS/scripts/fetch-ffmpeg-autogen.sh` (macOS/Linux) or
`OpenTaiko.Android/scripts/fetch-ffmpeg-autogen.ps1` (Windows); both produce the same tree.

Do not edit the fetched sources manually or regenerate them against other ffmpeg headers.
