namespace OpenTaiko.Android;

/// <summary>
/// Managed crash reporter — the Android counterpart of OpenTaiko.iOS/CrashLog.cs (managed part
/// only: native faults already produce logcat tombstones here). Reports are written to
/// files/CrashLogs/ inside the app's external files dir, so they can be pulled over USB/MTP
/// without adb, and retained reports are echoed to logcat on the next launch.
/// </summary>
internal static class CrashLog {
	private static string? _dir;
	private static FileStream? _nativeOut;   // keeps the redirected fd alive for the process lifetime

	public static void Install(string dataRoot) {
		_dir = Path.Combine(dataRoot, "CrashLogs");
		RedirectNativeStdout(dataRoot);
		// Managed exceptions escaping any .NET thread (loader threads, song enumeration, ...);
		// the runtime aborts the process right after this fires.
		AppDomain.CurrentDomain.UnhandledException += (_, e) =>
			Write(e.ExceptionObject as Exception, "UnhandledException");
		// Managed exceptions crossing back into Java (lifecycle/touch callbacks, Java-dispatched
		// threads) surface here instead of AppDomain.UnhandledException.
		global::Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
			Write(e.Exception, "JavaUnhandled");
	}

	public static void Write(Exception? ex, string source) {
		if (ex == null || _dir == null) return;
		try {
			Directory.CreateDirectory(_dir);
			string content = $"[{source}] {DateTime.UtcNow:O}\n{ex}\n";
			File.WriteAllText(Path.Combine(_dir, $"crash_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{source}.log"), content);
			global::Android.Util.Log.Error("OpenTaiko", $"[CRASH] {content}");
		} catch {
		}
	}

	/// <summary>
	/// Point the process's stdout/stderr (normally /dev/null on Android) at files/native_stdout.log,
	/// unbuffered. Mono's own diagnostics print there — e.g. the interpreter's fatal "NIY encountered
	/// in method X" names the offending method only on stdout, right before SIGABRT.
	/// </summary>
	private static void RedirectNativeStdout(string dataRoot) {
		try {
			var fs = new FileStream(Path.Combine(dataRoot, "native_stdout.log"),
				FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			int fd = fs.SafeFileHandle.DangerousGetHandle().ToInt32();
			dup2(fd, 1);
			dup2(fd, 2);
			_nativeOut = fs;
			// stdio would block-buffer a non-tty stream and an abort() would swallow the tail —
			// switch both C streams to unbuffered (bionic exports stdout/stderr as FILE* globals).
			nint libc = System.Runtime.InteropServices.NativeLibrary.Load("libc.so");
			foreach (string sym in new[] { "stdout", "stderr" }) {
				nint pp = System.Runtime.InteropServices.NativeLibrary.GetExport(libc, sym);
				setvbuf(System.Runtime.InteropServices.Marshal.ReadIntPtr(pp), IntPtr.Zero, 2 /* _IONBF */, 0);
			}
		} catch (Exception ex) {
			global::Android.Util.Log.Warn("OpenTaiko", $"stdout redirect failed: {ex.Message}");
		}
	}

	[System.Runtime.InteropServices.DllImport("libc")]
	private static extern int dup2(int oldfd, int newfd);

	[System.Runtime.InteropServices.DllImport("libc")]
	private static extern int setvbuf(IntPtr stream, IntPtr buf, int mode, IntPtr size);

	/// <summary>Echo retained reports to logcat on launch; the files stay for USB retrieval.</summary>
	public static void FlushPreviousCrashLogs() {
		try {
			if (_dir == null || !Directory.Exists(_dir)) return;
			foreach (string file in Directory.GetFiles(_dir, "crash_*.log"))
				global::Android.Util.Log.Warn("OpenTaiko",
					$"Previous crash log ({Path.GetFileName(file)}):\n{File.ReadAllText(file)}");
		} catch {
		}
	}
}
