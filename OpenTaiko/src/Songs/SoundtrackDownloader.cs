using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace OpenTaiko;

internal enum SoundtrackDownloadChoice {
	Download,
	Later,
	Never,
}

internal interface ISoundtrackDownloadHost {
	string UserAgent { get; }
	SoundtrackDownloadChoice AskUser(int songCount, long bytes);
	long GetFreeBytes(string dataRoot);
	void LogInformation(string message);
	void LogWarning(string message);
}

/// <summary>
/// Installs and updates the official soundtrack. Platform hosts provide the user prompt,
/// free-space query, and logging while this component owns the shared download behavior.
/// </summary>
internal static class SoundtrackDownloader {
	private const string Owner = "OpenTaiko";
	private const string Repo = "OpenTaiko-Soundtrack";
	private const string Branch = "main";
	private const int Workers = 4;

	internal static void EnsureSoundtrack(ISoundtrackDownloadHost host, string dataRoot, Action<string> status) {
		string doneMarker = Path.Combine(dataRoot, ".soundtrack_done");
		string neverMarker = Path.Combine(dataRoot, ".soundtrack_declined");
		if (File.Exists(neverMarker))
			return;
		bool silent = File.Exists(doneMarker);

		try {
			using var http = new HttpClient();
			http.DefaultRequestHeaders.UserAgent.ParseAdd(host.UserAgent);
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
				switch (host.AskUser(pendingSongs, estimatedBytes)) {
					case SoundtrackDownloadChoice.Never:
						File.WriteAllText(neverMarker, "");
						return;
					case SoundtrackDownloadChoice.Later:
						return;
				}
			}

			long freeBytes = host.GetFreeBytes(dataRoot);
			if (freeBytes < estimatedBytes + 200_000_000) {
				if (!silent) {
					status($"Not enough space for the soundtrack: need {GB(estimatedBytes)}, free {GB(freeBytes)}.");
					Thread.Sleep(4000);
				}
				return;
			}

			if (DownloadAll(host, http, songsRoot, pending, status)) {
				File.WriteAllText(doneMarker, "");
				if (!silent) status("Soundtrack downloaded!");
			} else if (!silent) {
				status("Soundtrack download incomplete — it resumes on the next launch.");
				Thread.Sleep(2500);
			}
		} catch (Exception ex) {
			// Offline / flaky network / GitHub hiccup: never block the game over it.
			host.LogInformation($"soundtrack check skipped: {ex.Message}");
			if (!silent) {
				status("Soundtrack check failed (offline?) — it will be offered again next launch.");
				Thread.Sleep(2000);
			}
		}
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

	private static bool DownloadAll(ISoundtrackDownloadHost host, HttpClient http, string songsRoot,
		List<Job> jobsList, Action<string> status) {
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
					long got = DownloadOne(host, http, songsRoot, job);
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
	private static long DownloadOne(ISoundtrackDownloadHost host, HttpClient http, string songsRoot, Job job) {
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
				host.LogWarning($"soundtrack: {job.RepoPath} attempt {attempt}: {ex.Message}");
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
