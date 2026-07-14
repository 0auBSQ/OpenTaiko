using System.Collections.Concurrent;
using System.Text.Json;
using Android.App;
using Activity = Android.App.Activity;

namespace OpenTaiko.Android;

/// <summary>
/// First-boot soundtrack installer + per-launch updater. The full OpenTaiko soundtrack (~4.4 GB)
/// cannot ride inside an APK (32-bit zip, ~2 GB install ceiling), so the app offers to download it
/// from the OpenTaiko-Soundtrack GitHub repository into files/Songs.
///
/// The repo's soundtrack_info.json (the same index the OpenTaiko Hub uses) drives everything:
/// each song lists its files, per-song size and the chart's MD5. A song needs downloading when
/// files are missing or its local .tja MD5 no longer matches (chart update → the whole song
/// folder re-downloads). Only files listed in the index are fetched — Hub-side extras like
/// preview/ stay out of the song list. The dialog shows the actual pending weight, computed
/// before asking. An interrupted download resumes on the next launch; after the initial install
/// the same check runs silently every launch (no dialog).
/// </summary>
internal static class SoundtrackDownloader {
	private const string Owner = "OpenTaiko";
	private const string Repo = "OpenTaiko-Soundtrack";
	private const string Branch = "main";
	private const int Workers = 4;

	public static void EnsureSoundtrack(Activity activity, string dataRoot, Action<string> status) {
		string doneMarker = Path.Combine(dataRoot, ".soundtrack_done");
		string neverMarker = Path.Combine(dataRoot, ".soundtrack_declined");
		if (File.Exists(neverMarker))
			return;
		bool silent = File.Exists(doneMarker);

		try {
			using var http = new HttpClient();
			http.DefaultRequestHeaders.UserAgent.ParseAdd("OpenTaiko-Android");
			http.Timeout = TimeSpan.FromMinutes(10);   // per request; big oggs on slow connections

			if (!silent) status("Checking the official soundtrack…");
			var songs = FetchInfo(http);
			string songsRoot = Path.Combine(dataRoot, "Songs");
			var pending = ComputePending(songsRoot, songs, out long estimatedBytes, out int pendingSongs);

			if (pending.Count == 0) {
				if (!silent) File.WriteAllText(doneMarker, "");
				return;
			}

			if (!silent) {
				switch (AskUser(activity, pendingSongs, estimatedBytes)) {
					case Choice.Never:
						File.WriteAllText(neverMarker, "");
						return;
					case Choice.Later:
						return;
				}
			}

			var stat = new global::Android.OS.StatFs(dataRoot);
			if (stat.AvailableBytes < estimatedBytes + 200_000_000) {
				if (!silent) {
					status($"Not enough space for the soundtrack: need {GB(estimatedBytes)}, free {GB(stat.AvailableBytes)}.");
					Thread.Sleep(4000);
				}
				return;
			}

			if (DownloadAll(http, songsRoot, pending, status)) {
				File.WriteAllText(doneMarker, "");
				if (!silent) status("Soundtrack downloaded!");
			} else if (!silent) {
				status("Soundtrack download incomplete — it resumes on the next launch.");
				Thread.Sleep(2500);
			}
		} catch (Exception ex) {
			// Offline / flaky network / GitHub hiccup: never block the game over it.
			global::Android.Util.Log.Info("OpenTaiko", $"soundtrack check skipped: {ex.Message}");
			if (!silent) {
				status("Soundtrack check failed (offline?) — it will be offered again next launch.");
				Thread.Sleep(2000);
			}
		}
	}

	private enum Choice { Download, Later, Never }

	private static Choice AskUser(Activity activity, int songCount, long bytes) {
		var picked = Choice.Later;
		using var done = new ManualResetEventSlim(false);
		activity.RunOnUiThread(() => {
			new AlertDialog.Builder(activity)
				.SetTitle("OpenTaiko Soundtrack")!
				.SetMessage($"Download the official soundtrack now?\n\n{songCount} songs, {GB(bytes)} — Wi-Fi recommended. An interrupted download resumes on the next launch. You can also add songs yourself over USB (files/Songs).")!
				.SetPositiveButton("Download", (_, _) => { picked = Choice.Download; done.Set(); })!
				.SetNeutralButton("Not now", (_, _) => { picked = Choice.Later; done.Set(); })!
				.SetNegativeButton("Never", (_, _) => { picked = Choice.Never; done.Set(); })!
				.SetCancelable(false)!
				.Show();
		});
		done.Wait();
		return picked;
	}

	private sealed record Song(string Folder, string[] Files, string? TjaPath, string? TjaMd5, double SizeMb);
	private sealed record Job(string RepoPath, bool Overwrite);

	/// <summary>Fetch and parse soundtrack_info.json (the Hub's index). Short-fused: this runs on
	/// every launch and must not stall boot on a flaky connection.</summary>
	private static List<Song> FetchInfo(HttpClient http) {
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
		string json = http.GetStringAsync(RawUrl("soundtrack_info.json"), cts.Token).GetAwaiter().GetResult();
		var list = new List<Song>();
		using var doc = JsonDocument.Parse(json);
		foreach (var el in doc.RootElement.EnumerateArray()) {
			string folder = Norm(el.GetProperty("tjaFolderPath").GetString()!);
			var files = el.GetProperty("tjaFilesPath").EnumerateArray()
				.Select(f => Norm(f.GetString()!)).ToArray();
			string? tja = files.FirstOrDefault(f => f.EndsWith(".tja", StringComparison.OrdinalIgnoreCase));
			string? md5 = el.TryGetProperty("tjaMD5", out var m) ? m.GetString() : null;
			double sizeMb = el.TryGetProperty("chartSize", out var sz) ? sz.GetDouble() : 0;
			if (files.Length > 0)
				list.Add(new Song(folder, files, tja, md5, sizeMb));
		}
		if (list.Count == 0) throw new Exception("soundtrack_info.json listed no songs");
		return list;

		static string Norm(string p) => p.Replace('\\', '/');
	}

	/// <summary>What needs downloading: every file of a song whose chart MD5 changed (overwrite),
	/// plus individually missing files of otherwise-intact songs (resume).</summary>
	private static List<Job> ComputePending(string songsRoot, List<Song> songs, out long estimatedBytes, out int pendingSongs) {
		var jobs = new List<Job>();
		double mb = 0;
		pendingSongs = 0;
		foreach (var song in songs) {
			var missing = song.Files.Where(f => !File.Exists(ToLocal(songsRoot, f))).ToList();
			bool chartChanged = false;
			if (missing.Count == 0 && song.TjaPath != null && song.TjaMd5 != null) {
				try { chartChanged = !Md5File(ToLocal(songsRoot, song.TjaPath)).Equals(song.TjaMd5, StringComparison.OrdinalIgnoreCase); } catch { }
			}
			if (chartChanged) {
				jobs.AddRange(song.Files.Select(f => new Job(f, Overwrite: true)));
				mb += song.SizeMb;
				pendingSongs++;
			} else if (missing.Count > 0) {
				jobs.AddRange(missing.Select(f => new Job(f, Overwrite: false)));
				// Apportion the song's size across its files for the estimate.
				mb += song.SizeMb * missing.Count / song.Files.Length;
				pendingSongs++;
			}
		}
		estimatedBytes = (long)(mb * 1024 * 1024);
		return jobs;
	}

	private static bool DownloadAll(HttpClient http, string songsRoot, List<Job> jobsList, Action<string> status) {
		var jobs = new ConcurrentQueue<Job>(jobsList);
		int totalFiles = jobsList.Count, doneFiles = 0, failed = 0;
		long doneBytes = 0;
		var lastUi = System.Diagnostics.Stopwatch.StartNew();

		void Report() {
			if (lastUi.ElapsedMilliseconds < 500) return;
			lastUi.Restart();
			status($"Downloading soundtrack… {Volatile.Read(ref doneFiles)}/{totalFiles} files ({GB(Interlocked.Read(ref doneBytes))})");
		}

		var threads = new List<Thread>();
		for (int w = 0; w < Workers; w++) {
			var t = new Thread(() => {
				while (jobs.TryDequeue(out var job)) {
					// A pile of failures means something systemic (no network, blocked host) —
					// stop burning retries; whatever is missing downloads on the next launch.
					if (Volatile.Read(ref failed) >= 20) return;
					long got = DownloadOne(http, songsRoot, job);
					if (got >= 0) {
						Interlocked.Add(ref doneBytes, got);
						Interlocked.Increment(ref doneFiles);
					} else {
						Interlocked.Increment(ref failed);
					}
					Report();
				}
			}) { IsBackground = true, Name = $"SoundtrackDL{w}" };
			t.Start();
			threads.Add(t);
		}
		foreach (var t in threads) t.Join();

		return Volatile.Read(ref failed) == 0;
	}

	/// <summary>Bytes downloaded, or -1 on failure. Skips existing files unless the job overwrites.</summary>
	private static long DownloadOne(HttpClient http, string songsRoot, Job job) {
		string dst = ToLocal(songsRoot, job.RepoPath);
		if (!job.Overwrite && File.Exists(dst))
			return 0;
		for (int attempt = 1; attempt <= 3; attempt++) {
			try {
				Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
				// Async I/O blocked-on per worker thread: Android's AndroidMessageHandler does not
				// implement the synchronous HttpClient.Send path (it throws NotSupportedException).
				// HttpClient enforces Content-Length, so a truncated body throws rather than
				// leaving a silently short file.
				using (var resp = http.SendAsync(new HttpRequestMessage(HttpMethod.Get, RawUrl(job.RepoPath)),
						HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult()) {
					resp.EnsureSuccessStatusCode();
					using var src = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
					using var fs = File.Create(dst);
					src.CopyToAsync(fs, 256 * 1024).GetAwaiter().GetResult();
				}
				return new FileInfo(dst).Length;
			} catch (Exception ex) {
				try { if (File.Exists(dst)) File.Delete(dst); } catch { }
				global::Android.Util.Log.Warn("OpenTaiko", $"soundtrack: {job.RepoPath} attempt {attempt}: {ex.Message}");
				Thread.Sleep(1000 * attempt);
			}
		}
		return -1;
	}

	private static string RawUrl(string repoPath) =>
		$"https://raw.githubusercontent.com/{Owner}/{Repo}/{Branch}/" +
		string.Join('/', repoPath.Split('/').Select(Uri.EscapeDataString));

	private static string ToLocal(string songsRoot, string repoPath) =>
		Path.Combine(songsRoot, repoPath.Replace('/', Path.DirectorySeparatorChar));

	private static string Md5File(string path) {
		using var md5 = System.Security.Cryptography.MD5.Create();
		using var fs = File.OpenRead(path);
		return Convert.ToHexString(md5.ComputeHash(fs)).ToLowerInvariant();
	}

	private static string GB(long bytes) => $"{bytes / 1e9:0.00} GB";
}
