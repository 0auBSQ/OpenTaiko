using Android.Content;
using Android.Content.Res;

namespace OpenTaiko.Android;

/// <summary>
/// First-run extraction of the game data bundled as APK assets. Unlike the iOS bundle, APK assets
/// are not a filesystem — the game expects ordinary files (writable song folders, LMDB databases,
/// skins enumerable via Directory APIs), so everything is streamed out to the app's external files
/// dir once. Existing files are NEVER overwritten: user state (saves, added songs, edited configs)
/// survives app updates; genuinely new files from an update are added on the next launch.
/// The build writes assets_index.txt (one relative path per line) so no recursive AssetManager
/// listing — notoriously slow — is needed.
/// </summary>
public static class AssetExtractor {
	/// <summary>Runs the extraction if this APK version hasn't completed one yet.
	/// Reports progress as (copied, total) through <paramref name="progress"/>.</summary>
	public static void EnsureExtracted(Context context, string targetRoot, Action<int, int>? progress = null) {
		var assets = context.Assets!;
		long versionCode = global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.P
			? context.PackageManager!.GetPackageInfo(context.PackageName!, 0)!.LongVersionCode
			: 0;

		// The stamp includes whether this APK carries a bundled song library, so installing a
		// songs-bundled build over a plain one (same version code) still re-runs extraction.
		bool hasBundledSongs = AssetExists(assets, "songs.zip");
		string stampPath = Path.Combine(targetRoot, ".assets_version");
		string stamp = $"v{versionCode}{(hasBundledSongs ? "+songs" : "")}";
		if (File.Exists(stampPath) && File.ReadAllText(stampPath).Trim() == stamp) {
			SweepStalePartFiles(targetRoot);   // clear litter from builds that used the .part scheme
			return;
		}

		string[] index;
		using (var reader = new StreamReader(assets.Open("assets_index.txt")))
			index = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		int done = 0;
		byte[] buffer = new byte[256 * 1024];
		foreach (string rel in index) {
			// __init__/ is the dot-dodging travel name for .init/ (aapt2 drops dot-prefixed asset paths).
			string diskRel = rel.StartsWith("__init__/") ? ".init/" + rel.Substring("__init__/".Length) : rel;
			string dst = Path.Combine(targetRoot, diskRel.Replace('/', Path.DirectorySeparatorChar));
			if (!File.Exists(dst)) {
				try {
					Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
					// Write straight to the destination. A temp-name+rename is unreliable on the
					// FUSE-backed external storage (the rename can spuriously report the source
					// missing) and would leave *.part litter that the game's texture loader then
					// tries to open. A partial file from a process kill is skipped next launch (it
					// exists) — acceptable, and the same behaviour the port shipped with originally.
					using (var src = assets.Open(rel))
					using (var outStream = File.Create(dst)) {
						int n;
						while ((n = src.Read(buffer, 0, buffer.Length)) > 0)
							outStream.Write(buffer, 0, n);
					}
				} catch (Exception ex) {
					// Never let one bad file (aapt2-dropped asset, transient FS error) crash the
					// whole boot — skip it, drop any partial, and keep extracting.
					try { if (File.Exists(dst)) File.Delete(dst); } catch { }
					global::Android.Util.Log.Warn("OpenTaiko", $"asset skipped ({rel}): {ex.Message}");
				}
			}
			done++;
			if ((done & 63) == 0) progress?.Invoke(done, index.Length);
		}
		progress?.Invoke(index.Length, index.Length);

		if (hasBundledSongs)
			ExtractBundledSongs(assets, targetRoot, progress);

		SweepStalePartFiles(targetRoot);
		File.WriteAllText(stampPath, stamp);
	}

	/// <summary>Delete leftover *.part files an earlier build's temp-file extraction may have
	/// stranded — the game's texture/chart loaders enumerate directories and would try to open them.</summary>
	private static void SweepStalePartFiles(string targetRoot) {
		try {
			foreach (var sub in new[] { "System", "Songs", ".init" }) {
				string dir = Path.Combine(targetRoot, sub);
				if (!Directory.Exists(dir)) continue;
				foreach (var f in Directory.EnumerateFiles(dir, "*.part", SearchOption.AllDirectories)) {
					try { File.Delete(f); } catch { }
				}
			}
		} catch { }
	}

	private static bool AssetExists(AssetManager assets, string name) {
		try { using var s = assets.Open(name); return true; } catch (Java.IO.FileNotFoundException) { return false; }
	}

	/// <summary>
	/// Unpack the optional songs.zip asset (BundleSongs=true builds) into Songs/, same
	/// never-overwrite rule as the file extraction. Songs travel as ONE zip because aapt2 rejects
	/// the unicode filenames real charts have; the zip is copied to disk first since compressed
	/// asset streams aren't seekable.
	/// </summary>
	private static void ExtractBundledSongs(AssetManager assets, string targetRoot, Action<int, int>? progress) {
		string tmp = Path.Combine(targetRoot, "songs.zip.part");
		try {
			using (var src = assets.Open("songs.zip"))
			using (var dst = File.Create(tmp))
				src.CopyTo(dst);

			string songsRoot = Path.Combine(targetRoot, "Songs");
			using var zip = System.IO.Compression.ZipFile.OpenRead(tmp);
			int done = 0, total = zip.Entries.Count;
			foreach (var entry in zip.Entries) {
				done++;
				if (string.IsNullOrEmpty(entry.Name) || entry.FullName.Contains(".."))
					continue;   // directory entry / malformed path
				string dst = Path.Combine(songsRoot, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
				if (!File.Exists(dst)) {
					Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
					using (var es = entry.Open())
					using (var fs = File.Create(dst))
						es.CopyTo(fs);
				}
				if ((done & 63) == 0) progress?.Invoke(done, total);
			}
			progress?.Invoke(total, total);
		} catch (Exception ex) {
			// A failed song unpack shouldn't brick the game (the stamp stays unwritten, so the
			// next launch retries and fills in whatever is missing).
			global::Android.Util.Log.Warn("OpenTaiko", $"bundled songs unpack failed: {ex.Message}");
		} finally {
			try { File.Delete(tmp); } catch { }
		}
	}
}