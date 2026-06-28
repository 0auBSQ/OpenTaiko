# FFmpeg.AutoGen 5.1.1 binding (iOS only)

The iOS build patches and compiles the FFmpeg.AutoGen 5.1.1 managed binding from source.

Desktop (Windows/Linux/macOS) keeps using the stock NuGet package.

## Why

FFmpeg.AutoGen 5.1.1 detects the OS via `RuntimeInformation.IsOSPlatform(Windows|Linux|OSX)` and
throws `PlatformNotSupportedException` for anything else. iOS matches none of those, so:

- `LibraryLoader`'s `GetPlatformId` throws in `ffmpeg`'s static constructor (it computes the
  library naming and `EAGAIN`), poisoning the whole `ffmpeg` type before any of our code runs.
  Because the throw happens during type initialization, no runtime override can prevent it; the
  fix has to be in the binding's own source.
- `FunctionLoader.GetFunctionPointer` throws on the per-function `dlsym` hot path.

iOS is Darwin, so the macOS code paths (dylib naming, `dlsym`, `EAGAIN=35`) are correct for it.
Upstream only added non-desktop platforms in the 6.0/8.1 rewrite, which replaced the static
bindings with runtime-generated delegates that throw `NotSupportedException` under iOS AOT, so
5.1.1 is the only iOS-viable line for the ffmpeg 5.1 ABI our native framework ships.

## Layout

- `ios-darwin-fallback.patch`: the patch to apply to ffmpeg source.
- `upstream/` directory to store patched ffmpeg source, not checked in.
- `FFmpeg.AutoGen.csproj` - minimal csproject compiling upstream/.

Do not edit the fetched sources manually or regenerate them against other ffmpeg headers.
