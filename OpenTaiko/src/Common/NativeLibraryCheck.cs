using System.Diagnostics;
using FDK;
using FFmpeg.AutoGen;
using LightningDB.Native;

namespace OpenTaiko;

// startup check of the optional native libraries (lmdb, ffmpeg)
internal static class NativeLibraryCheck {
	internal sealed record Failure(string FileName, string[] SideEffects);

	public static bool LmdbAvailable { get; private set; } = true;

	// ffmpeg libraries used for playback, dependencies first so the first failure names the root
	private static readonly string[] FFmpegLibraries = ["avutil", "swresample", "swscale", "avcodec", "avformat"];

	public static List<Failure> Run() {
		var failures = new List<Failure>();

		try {
			Lmdb.mdb_version(out _, out _, out _);
		} catch (Exception e) {
			LmdbAvailable = false;
			Trace.TraceWarning($"LMDB failed to load: {e.Message}");
			failures.Add(new Failure(LmdbFileName(), [
				"Some Lua Stages (Custom or not) might behave wrongly or no longer properly save data",
			]));
		}

		// ios and android set their ffmpeg loader in this static constructor
		System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(CVideoDecoder).TypeHandle);
		string? ffmpegFile = null;
		foreach (string lib in FFmpegLibraries) {
			try {
				LoadFFmpeg(lib);
			} catch (Exception e) {
				ffmpegFile = FFmpegFileName(lib);
				Trace.TraceWarning($"FFmpeg ({ffmpegFile}) failed to load: {e.Message}");
				break;
			}
		}
		if (ffmpegFile != null) {
			CVideoDecoder.LibraryDisabled = true;
			failures.Add(new Failure(ffmpegFile, [
				"Videos will fail to play (whatever they are chart videos or cutscenes)",
			]));
		}

		return failures;
	}

	// first call into a library loads it
	private static void LoadFFmpeg(string lib) {
		switch (lib) {
			case "avutil": ffmpeg.avutil_version(); break;
			case "swresample": ffmpeg.swresample_version(); break;
			case "swscale": ffmpeg.swscale_version(); break;
			case "avcodec": ffmpeg.avcodec_version(); break;
			case "avformat": ffmpeg.avformat_version(); break;
		}
	}

	private static string LmdbFileName()
		=> OperatingSystem.IsWindows() ? "lmdb.dll"
		: OperatingSystem.IsMacOS() ? "lmdb.dylib"
		: OperatingSystem.IsIOS() ? "liblmdb.framework"
		: "liblmdb.so";

	private static string FFmpegFileName(string lib) {
		string version = ffmpeg.LibraryVersionMap.TryGetValue(lib, out int v) ? v.ToString() : "";
		if (OperatingSystem.IsWindows()) return $"{lib}-{version}.dll";
		if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS()) return $"lib{lib}.{version}.dylib";
		return $"lib{lib}.so.{version}";
	}
}
