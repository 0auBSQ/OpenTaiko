using System.ComponentModel;
using System.Diagnostics;
using System.Xml.XPath;

namespace FDK {
	/// <summary>
	/// The LoudnessMetadataScanner plays two roles:
	/// 1. Scanning of song audio files using BS1770GAIN (http://bs1770gain.sourceforge.net/)
	///    to determine their perceived loudness. Running on a background thread while not
	///    in song gameplay, songs without existing loudness metadata files (e.g. *.bs1770gain.xml)
	///    have their perceived loudness determined and saved into an associated metadata file
	///    without modifying the original audio file. This scanning process begins running
	///    with scanning jobs ordered based on the order in which songs are enumerated when
	///    the application starts, but shifts to prioritize songs which are browsed and previewed
	///    while on the song select screen.
	/// 2. Loading of loudness metadata from the BS1770GAIN metadata file alongside each audio file.
	///    This occurs when parsing .tja files, when song preview begins, and when song playback
	///    begins. When no file is available on disk, a scanning job is passed to the background
	///    scanning thread for processing. The loaded metadata is then passed into the
	///    SongGainController for combination with a configured target loudness, resulting in a
	///    gain value assigned to the sound object just before playback begins.
	/// </summary>
	public static class LoudnessMetadataScanner {
		private const string Bs1770GainExeFileName = "bs1770gain.exe";

		private static readonly Stack<string> Jobs = new Stack<string>();
		private static readonly object LockObject = new object();
		private static readonly Queue<double> RecentFileScanDurations = new Queue<double>();

		private static Thread ScanningThread;
		private static Semaphore Semaphore;

		public static void StartBackgroundScanning() {
			var tracePrefix = $"{nameof(LoudnessMetadataScanner)}.{nameof(StartBackgroundScanning)}";

			if (!IsBs1770GainAvailable()) {
				Trace.TraceInformation($"{tracePrefix}: BS1770GAIN is not available. A background scanning thread will not be started.");
				return;
			}

			Trace.TraceInformation($"{tracePrefix}: BS1770GAIN is available. Starting background scanning thread...");

			lock (LockObject) {
				Semaphore = new Semaphore(Jobs.Count, int.MaxValue);
				ScanningThread = new Thread(Scan) {
					IsBackground = true,
					Name = "LoudnessMetadataScanner background scanning thread.",
					Priority = ThreadPriority.Lowest
				};
				ScanningThread.Start();
			}

			Trace.TraceInformation($"{tracePrefix}: Background scanning thread started.");
		}

		public static void StopBackgroundScanning(bool joinImmediately) {
			var scanningThread = ScanningThread;

			if (scanningThread == null) {
				return;
			}

			var tracePrefix = $"{nameof(LoudnessMetadataScanner)}.{nameof(StopBackgroundScanning)}";

			Trace.TraceInformation($"{tracePrefix}: Stopping background scanning thread...");

			lock (LockObject) {
				ScanningThread = null;
				Semaphore.Release();
				Semaphore = null;
			}

			if (joinImmediately) {
				scanningThread.Join();
			}

			Trace.TraceInformation($"{tracePrefix}: Background scanning thread stopped.");
		}

		public static LoudnessMetadata? LoadForAudioPath(string absoluteBgmPath) {
			try {
				var loudnessMetadataPath = GetLoudnessMetadataPath(absoluteBgmPath);

				if (File.Exists(loudnessMetadataPath)) {
					return LoadFromMetadataPath(loudnessMetadataPath);
				}

				SubmitForBackgroundScanning(absoluteBgmPath);
			} catch (Exception e) {
				var tracePrefix = $"{nameof(LoudnessMetadataScanner)}.{nameof(LoadForAudioPath)}";
				Trace.TraceError($"{tracePrefix}: Encountered an exception while attempting to load {absoluteBgmPath}");
				Trace.TraceError(e.ToString());
			}

			return null;
		}

		private static string GetLoudnessMetadataPath(string absoluteBgmPath) {
			return Path.Combine(
				Path.GetDirectoryName(absoluteBgmPath),
				Path.GetFileNameWithoutExtension(absoluteBgmPath) + ".bs1770gain.xml");
		}

		private static LoudnessMetadata? LoadFromMetadataPath(string loudnessMetadataPath) {
			XPathDocument xPathDocument;
			try {
				xPathDocument = new XPathDocument(loudnessMetadataPath);
			} catch (IOException) {
				var tracePrefix = $"{nameof(LoudnessMetadataScanner)}.{nameof(LoadFromMetadataPath)}";
				Trace.TraceWarning($"{tracePrefix}: Encountered IOException while attempting to read {loudnessMetadataPath}. This can occur when attempting to load while scanning the same file. Returning null...");
				return null;
			}

			var trackNavigator = xPathDocument.CreateNavigator()
				.SelectSingleNode(@"//bs1770gain/track[@ToTal=""1"" and @Number=""1""]");

			var integratedLufsNode = trackNavigator?.SelectSingleNode(@"integrated/@lufs");
			var truePeakTpfsNode = trackNavigator?.SelectSingleNode(@"true-peak/@tpfs");

			if (trackNavigator == null || integratedLufsNode == null || truePeakTpfsNode == null) {
				var tracePrefix = $"{nameof(LoudnessMetadataScanner)}.{nameof(LoadFromMetadataPath)}";
				Trace.TraceWarning($"{tracePrefix}: Encountered incorrect xml element structure while parsing {loudnessMetadataPath}. Returning null...");
				return null;
			}

			var integrated = integratedLufsNode.ValueAsDouble;
			var truePeak = truePeakTpfsNode.ValueAsDouble;

			if (integrated <= -70.0 || truePeak >= 12.04) {
				var tracePrefix = $"{nameof(LoudnessMetadataScanner)}.{nameof(LoadFromMetadataPath)}";
				Trace.TraceWarning($"{tracePrefix}: Encountered evidence of extreme clipping while parsing {loudnessMetadataPath}. Returning null...");
				return null;
			}

			return new LoudnessMetadata(new Lufs(integrated), new Lufs(truePeak));
		}

		private static void SubmitForBackgroundScanning(string absoluteBgmPath) {
			lock (LockObject) {
				// Quite often, the loading process will cause the same job to be submitted many times.
				// As such, we'll do a quick check as when this happens an equivalent job will often
				// already be at the top of the stack and we need not add it again.
				//
				// Note that we will not scan the whole stack as that is an O(n) operation on the main
				// thread, whereas redundant file existence checks on the background thread are not harmful.
				//
				// We also do not want to scan the whole stack, for example to skip pushing a new item onto it,
				// because we want to re-submit jobs as the user interacts with their data, usually by
				// scrolling through songs and previewing them. Their current interests should drive
				// scanning priorities, and it is for this reason that a stack is used instead of a queue.
				var semaphore = Semaphore;
				if (semaphore != null && (Jobs.Count == 0 || Jobs.Peek() != absoluteBgmPath)) {
					Jobs.Push(absoluteBgmPath);
					semaphore.Release();
				}
			}
		}

		private static void Scan() {
			try {
				while (true) {
					RaiseScanningStateChanged(false);

					Semaphore?.WaitOne();

					if (ScanningThread == null) {
						return;
					}

					RaiseScanningStateChanged(true);

					int jobCount;
					string absoluteBgmPath;
					lock (LockObject) {
						jobCount = Jobs.Count;
						absoluteBgmPath = Jobs.Pop();
					}

					var tracePrefix = $"{nameof(LoudnessMetadataScanner)}.{nameof(Scan)}";

					try {
						if (!File.Exists(absoluteBgmPath)) {
							Trace.TraceWarning($"{tracePrefix}: Scanning jobs outstanding: {jobCount - 1}. Missing audio file. Skipping {absoluteBgmPath}...");
							continue;
						}

						var loudnessMetadataPath = GetLoudnessMetadataPath(absoluteBgmPath);

						if (File.Exists(loudnessMetadataPath)) {
							Trace.TraceWarning($"{tracePrefix}: Scanning jobs outstanding: {jobCount - 1}. Pre-existing metadata. Skipping {absoluteBgmPath}...");
							continue;
						}

						Trace.TraceInformation($"{tracePrefix}: Scanning jobs outstanding: {jobCount}. Scanning {absoluteBgmPath}...");
						var stopwatch = Stopwatch.StartNew();

						File.Delete(loudnessMetadataPath);
						var arguments = $"-it --xml -f \"{Path.GetFileName(loudnessMetadataPath)}\" \"{Path.GetFileName(absoluteBgmPath)}\"";
						Execute(Path.GetDirectoryName(absoluteBgmPath), Bs1770GainExeFileName, arguments, true);

						var seconds = stopwatch.Elapsed.TotalSeconds;
						RecentFileScanDurations.Enqueue(seconds);
						while (RecentFileScanDurations.Count > 20) {
							RecentFileScanDurations.Dequeue();
						}
						var averageSeconds = RecentFileScanDurations.Average();
						Trace.TraceInformation($"{tracePrefix}: Scanned in {seconds}s. Estimated remaining: {(int)(averageSeconds * (jobCount - 1))}s.");
					} catch (Exception e) {
						Trace.TraceError($"{tracePrefix}: Encountered an exception while attempting to scan {absoluteBgmPath}");
						Trace.TraceError(e.ToString());
					}
				}
			} catch (Exception e) {
				var tracePrefix = $"{nameof(LoudnessMetadataScanner)}.{nameof(Scan)}";
				Trace.TraceError($"{tracePrefix}: caught an exception at the level of the thread method. The background scanning thread will now terminate.");
				Trace.TraceError(e.ToString());
			}
		}

		private static bool IsBs1770GainAvailable() {
			try {
				Execute(null, Bs1770GainExeFileName, "-h");
				return true;
			} catch (Win32Exception) {
				return false;
			} catch (Exception e) {
				var tracePrefix = $"{nameof(LoudnessMetadataScanner)}.{nameof(IsBs1770GainAvailable)}";
				Trace.TraceError($"{tracePrefix}: Encountered an exception. Returning false...");
				Trace.TraceError(e.ToString());

				return false;
			}
		}

		private static string Execute(
			string workingDirectory, string fileName, string arguments, bool shouldFailOnStdErrDataReceived = false) {
			var processStartInfo = new ProcessStartInfo(fileName, arguments) {
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				WorkingDirectory = workingDirectory ?? ""
			};

			var stdoutWriter = new StringWriter();
			var stderrWriter = new StringWriter();
			using (var process = Process.Start(processStartInfo)) {
				process.OutputDataReceived += (s, e) => {
					if (e.Data != null) {
						stdoutWriter.Write(e.Data);
						stdoutWriter.Write(Environment.NewLine);
					}
				};

				var errorDataReceived = false;
				process.ErrorDataReceived += (s, e) => {
					if (e.Data != null) {
						errorDataReceived = true;
						stderrWriter.Write(e.Data);
						stderrWriter.Write(Environment.NewLine);
					}
				};

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
				process.WaitForExit();

				if ((shouldFailOnStdErrDataReceived && errorDataReceived) || process.ExitCode != 0) {
					var stderr = stderrWriter.ToString();
					if (string.IsNullOrEmpty(stderr)) {
						stderr = stdoutWriter.ToString();
					}

					throw new Exception(
						$"Execution of {processStartInfo.FileName} with arguments {processStartInfo.Arguments} failed with exit code {process.ExitCode}: {stderr}");
				}

				return stdoutWriter.ToString();
			}
		}

		private static void RaiseScanningStateChanged(bool isActivelyScanning) {
			ScanningStateChanged?.Invoke(null, new ScanningStateChangedEventArgs(isActivelyScanning));
		}

		public class ScanningStateChangedEventArgs : EventArgs {
			public ScanningStateChangedEventArgs(bool isActivelyScanning) {
				IsActivelyScanning = isActivelyScanning;
			}

			public bool IsActivelyScanning { get; private set; }
		}

		public static event EventHandler<ScanningStateChangedEventArgs> ScanningStateChanged;
	}
}
