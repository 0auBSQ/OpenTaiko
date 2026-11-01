using System;
using System.IO;
#if !DEBUG
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Foundation;
using UIKit;
#endif

namespace OpenTaiko.iOS;

/// <summary>
/// In-app crash reporter. Crash reports are written to Documents/CrashLogs/ so they survive
/// termination and can be retrieved via the Files app / file sharing (UIFileSharingEnabled),
/// then echoed to the device console on the next launch. Reports are retained, never deleted.
///
/// Managed .NET exceptions are captured via <see cref="Write"/> (from GameViewController's
/// game-loop try/catch); their stack traces already contain method names, so the .log files
/// need no symbolication.
///
/// Native crashes (POSIX signals from BASS/OpenGL/Skia/Lua/Mono) are captured by signal
/// handlers that write an Apple-style ".crash" report (thread backtrace + Binary Images table),
/// symbolicated against the build's dSYM using atos (the exact command is in the report itself).
///
/// The native signal-handler path is Release-only (#if !DEBUG): on the Mono runtime used by
/// Debug/simulator builds, marshalling a managed signal handler aborts the process at startup.
/// Release/device (TestFlight) uses AOT, where these constructs are supported.
/// </summary>
internal static class CrashLog {
	private static string GetCrashDir() =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CrashLogs");

#if !DEBUG
	// The signal handler writes here while crashing. Kept in Library/Caches — NOT the file-shared
	// Documents/CrashLogs — so the always-present empty pending stub is never visible to testers.
	// PrepareNativeReport promotes it into CrashLogs on the next launch iff it is non-empty.
	private static string GetPendingNativePath() {
		string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		string caches = Path.Combine(Path.GetDirectoryName(docs) ?? docs, "Library", "Caches");
		return Path.Combine(caches, "crash_native_pending.crash");
	}
#endif

	/// <summary>Record a managed exception. Called from GameViewController's try/catch sites.</summary>
	public static void Write(Exception? ex, string source) {
		if (ex == null) return;
		try {
			string crashDir = GetCrashDir();
			Directory.CreateDirectory(crashDir);
			string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
			string filename = $"crash_{timestamp}_{source}.log";
			string content = $"[{source}] {DateTime.UtcNow:O}\n{ex}\n";
			File.WriteAllText(Path.Combine(crashDir, filename), content);
			Console.Error.WriteLine($"[OpenTaiko CRASH] {content}");
		} catch {
		}
	}

	/// <summary>
	/// On launch: echo retained crash reports (.log managed + .crash native) to the console
	/// (visible in Console.app / Xcode device logs). Reports are NOT deleted — they must stay
	/// retrievable via the Files app / file sharing.
	/// </summary>
	public static void FlushPreviousCrashLogs() {
		try {
			string crashDir = GetCrashDir();
			if (!Directory.Exists(crashDir)) return;
			Echo("crash_*.log", "crash log");
			Echo("crash_*.crash", "crash report");

			void Echo(string pattern, string label) {
				foreach (string file in Directory.GetFiles(crashDir, pattern)) {
					string content = File.ReadAllText(file);
					if (content.Length == 0) continue; // empty pending native file (no crash this session)
					Console.WriteLine($"[OpenTaiko] Previous {label} ({Path.GetFileName(file)}):\n{content}");
				}
			}
		} catch {
		}
	}

#if !DEBUG
	// ======================================================================================
	//  Native crash capture (Release builds only)
	// ======================================================================================

	// ---- Darwin signal / ABI constants -------------------------------------------------
	private const int SIGILL = 4, SIGTRAP = 5, SIGABRT = 6, SIGFPE = 8, SIGBUS = 10, SIGSEGV = 11, SIGSYS = 12;
	private const int SA_SIGINFO = 0x0040, SA_ONSTACK = 0x0001;
	private static readonly int[] HandledSignals = { SIGILL, SIGTRAP, SIGABRT, SIGFPE, SIGBUS, SIGSEGV, SIGSYS };

	// Faults below this address are treated as managed null-derefs that Mono converts to
	// NullReferenceExceptions; we forward them rather than reporting a crash.
	private const ulong NullDerefGuardLimit = 0x1000;

	private static readonly sigaction_t[] _oldHandlers = new sigaction_t[64];
	private static int _handling; // reentrancy guard for the signal handler
	private static bool _installed;
	private static bool _handlersArmed; // set once sigaction succeeds for all handled signals

	// Pre-pinned, pre-formatted report data + a dedicated signal stack, so the crash handler
	// itself does zero managed allocation / File I/O (which would deadlock after a real fault).
	private static GCHandle _prefixHandle, _pathHandle;
	private static IntPtr _prefixPtr, _pathPtr;
	private static int _prefixLen;
	private static IntPtr _altStack;
	private const int AltStackSize = 64 * 1024;
	private const int O_WRONLY = 0x0001, O_TRUNC = 0x0400;

	// Environment captured once at install time (UIKit/Foundation access is unsafe from
	// within a signal handler, so we snapshot everything up front).
	private static string _procName = "OpenTaiko";
	private static string _bundleId = "";
	private static string _version = "";
	private static string _build = "";
	private static string _os = "";
	private static string _model = "";

	public static void Install() {
		if (_installed) return;
		_installed = true;
		CaptureEnvironment();
		PrepareNativeReport();
		InstallSignalHandlers();
		// Capture unhandled managed exceptions that escape GameViewController.OnFrame's try/catch —
		// e.g. on background loader threads during stage transitions. Gives a full C# stack trace
		// (no symbolication needed). Native faults are handled separately by the signal handlers.
		AppDomain.CurrentDomain.UnhandledException += (_, e) =>
			Write(e.ExceptionObject as Exception, "UnhandledException");
		// Capture managed exceptions thrown inside a native→managed callback (CADisplayLink/OnFrame,
		// touch handlers, UIAlertAction, NSTimer, ...) as they cross back into native code. These go
		// through Xamarin's managed→ObjC marshaling (xamarin_unhandled_exception_handler → abort),
		// which does NOT fire AppDomain.UnhandledException — so without this hook they produce no
		// managed .log, only an uninformative SIGABRT. This gives the full C# stack.
		ObjCRuntime.Runtime.MarshalManagedException += (_, e) =>
			Write(e.Exception, "MarshalManagedException");
		WriteStatusBreadcrumb();
	}

	// Records that the reporter armed successfully and what the native path will be able to emit, so
	// that an empty CrashLogs dir after a crash positively means "uncatchable OS kill" (OOM/jetsam,
	// watchdog, SIGKILL) rather than "the handler silently failed to install". Plain .txt so it is
	// ignored by FlushPreviousCrashLogs (which only reads .log/.crash).
	private static void WriteStatusBreadcrumb() {
		try {
			string dir = GetCrashDir();
			Directory.CreateDirectory(dir);
			string s = $"crash reporter armed {DateTime.UtcNow:O}\n" +
				$"version={_version} build={_build} os={_os} model={_model}\n" +
				$"signal_handlers={(_handlersArmed ? "armed" : "FAILED")} alt_stack={(_altStack != IntPtr.Zero)}\n" +
				$"native_prefix_built={(_prefixPtr != IntPtr.Zero)} prefix_len={_prefixLen}\n";
			File.WriteAllText(Path.Combine(dir, "crash_native_status.txt"), s);
		} catch {
		}
	}

	private static unsafe void InstallSignalHandlers() {
		try {
			// Run handlers on a dedicated stack (SA_ONSTACK below) so a stack-overflow crash can
			// still be caught — the faulting thread's own stack is exhausted.
			_altStack = Marshal.AllocHGlobal(AltStackSize);
			var ss = new stack_t { ss_sp = _altStack, ss_size = AltStackSize, ss_flags = 0 };
			sigaltstack(ref ss, IntPtr.Zero);

			// [UnmanagedCallersOnly] + &method gives a native function pointer whose
			// native→managed wrapper is generated at AOT time. (Marshal.GetFunctionPointerForDelegate
			// fails here: its wrapper would need JIT, unavailable in aot-only mode.)
			IntPtr fp = (IntPtr)(delegate* unmanaged<int, IntPtr, IntPtr, void>)&NativeSignalHandler;
			foreach (int sig in HandledSignals) {
				var act = new sigaction_t {
					sa_handler = fp,
					sa_mask = 0,
					sa_flags = SA_SIGINFO | SA_ONSTACK,
				};
				sigaction(sig, ref act, out _oldHandlers[sig]);
			}
			_handlersArmed = true;
		} catch (Exception ex) {
			Console.Error.WriteLine($"[OpenTaiko CRASH] Failed to install signal handlers: {ex}");
		}
	}

	[UnmanagedCallersOnly]
	private static unsafe void NativeSignalHandler(int sig, IntPtr info, IntPtr ucontext) {
		bool reentrant = Interlocked.Exchange(ref _handling, 1) != 0;
		IntPtr faultAddr = info != IntPtr.Zero ? Marshal.ReadIntPtr(info, 24 /* si_addr offset */) : IntPtr.Zero;

		// Skip reporting for (a) reentrant faults (a crash while handling) and (b) likely
		// managed null-derefs Mono converts to NullReferenceExceptions. In both cases we just
		// forward to the previously installed (Mono/default) handler.
		bool likelyManagedNull = (sig == SIGSEGV || sig == SIGBUS) && (ulong)faultAddr < NullDerefGuardLimit;
		if (!reentrant && !likelyManagedNull && _prefixPtr != IntPtr.Zero && _pathPtr != IntPtr.Zero) {
			// Async-signal-safe report: open the (pre-created) file, write the pre-formatted prefix
			// + Binary Images, then a signal line and a raw backtrace. No managed alloc/GC/locks.
			int fd = open(_pathPtr, O_WRONLY | O_TRUNC);
			if (fd >= 0) {
				write(fd, _prefixPtr, _prefixLen);
				byte* line = stackalloc byte[192];
				int p = 0;
				p = Ascii(line, p, "\nException Type:  ");
				p = Ascii(line, p, SignalName(sig));
				p = Ascii(line, p, " (signal ");
				p = Dec(line, p, sig);
				p = Ascii(line, p, ")\nException Codes: fault address ");
				p = Hex(line, p, (ulong)faultAddr);
				p = Ascii(line, p, "\n\nThread 0 Crashed (symbolicate each frame via the Binary Images above):\n");
				write(fd, (IntPtr)line, p);
				void** frames = stackalloc void*[128];
				int fn = backtrace(frames, 128);
				backtrace_symbols_fd(frames, fn, fd);
				close(fd);
			}
		}

		// Hand off to the previously installed handler so Mono's own dump + termination (and
		// any OS/TestFlight report, or Mono's null-deref → NRE conversion) still happen.
		// Restore the previous disposition, then let the faulting instruction re-execute (for
		// hardware faults) or re-raise (for the rest). We forward this way rather than calling
		// the old handler via a function pointer because `calli` through an unmanaged function
		// pointer is rejected at load time by the Mono interpreter (non-AOT) builds.
		try {
			sigaction(sig, ref _oldHandlers[sig], out _);
		} catch {
		}
		_handling = 0;

		bool reExecutes = sig == SIGSEGV || sig == SIGBUS || sig == SIGILL || sig == SIGFPE;
		if (!reExecutes)
			raise(sig);
		// else: return — the faulting instruction re-runs and hits the restored handler.
	}

	// Builds the report path + prefix (header + Binary Images table) once at startup, pinned, so
	// the signal handler can emit them without allocating. Also retains a native crash left from
	// the previous session and (re)creates an empty pending file the handler opens + truncates.
	private static unsafe void PrepareNativeReport() {
		try {
			string dir = GetCrashDir();
			Directory.CreateDirectory(dir);
			string path = GetPendingNativePath();
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			// Promote a report the handler wrote last session into the user-visible dir. Only a
			// non-empty file is a real native crash; drop an empty leftover (handler faulted before
			// writing, or this is just a clean relaunch). Because the pending file lives in Caches
			// — not Documents/CrashLogs — testers never see the empty stub: the user-visible dir
			// only ever gains a crash_<ts>_native.crash when a fault was actually captured.
			if (File.Exists(path)) {
				if (new FileInfo(path).Length > 0)
					try { File.Move(path, Path.Combine(dir, $"crash_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_native.crash")); } catch { }
				else
					try { File.Delete(path); } catch { }
			}
			// (Re)create the empty pending file so the handler's open(O_WRONLY|O_TRUNC) — which does
			// not create — finds it. It lives in Caches and is never surfaced to the user.
			File.WriteAllText(path, "");

			byte[] pathBytes = Encoding.UTF8.GetBytes(path + "\0");
			_pathHandle = GCHandle.Alloc(pathBytes, GCHandleType.Pinned);
			_pathPtr = _pathHandle.AddrOfPinnedObject();

			byte[] prefix = Encoding.UTF8.GetBytes(BuildReportPrefix());
			_prefixHandle = GCHandle.Alloc(prefix, GCHandleType.Pinned);
			_prefixPtr = _prefixHandle.AddrOfPinnedObject();
			_prefixLen = prefix.Length;
		} catch {
		}
	}

	private static unsafe string BuildReportPrefix() {
		var sb = new StringBuilder();
		sb.Append("Incident Identifier: ").Append(Guid.NewGuid().ToString().ToUpperInvariant()).Append('\n');
		sb.Append("Process:             ").Append(_procName).Append('\n');
		sb.Append("Identifier:          ").Append(_bundleId).Append('\n');
		sb.Append("Version:             ").Append(_version).Append(" (").Append(_build).Append(")\n");
		sb.Append("OS Version:          ").Append(_os).Append("  Device: ").Append(_model).Append('\n');
		sb.Append("Session Start:       ").Append(DateTime.UtcNow.ToString("O")).Append('\n');
		sb.Append("\nBinary Images:\n");
		uint count = _dyld_image_count();
		for (uint i = 0; i < count; i++) {
			IntPtr header = _dyld_get_image_header(i);
			string fullPath = Marshal.PtrToStringAnsi(_dyld_get_image_name(i)) ?? "???";
			TryGetImageInfo(header, out string uuid, out ulong textSize, out int cpuType);
			ulong start = (ulong)header;
			ulong end = textSize > 0 ? start + textSize - 1 : start;
			sb.Append("0x").Append(start.ToString("x")).Append(" - 0x").Append(end.ToString("x"))
			  .Append(' ').Append(Path.GetFileName(fullPath)).Append(' ').Append(CpuName(cpuType))
			  .Append("  <").Append(uuid).Append("> ").Append(fullPath).Append('\n');
		}
		sb.Append("\nSymbolicate a frame: atos -o <App>.app.dSYM/Contents/Resources/DWARF/<App> -l <image load addr above> <frame addr below>\n");
		return sb.ToString();
	}

	// ---- async-signal-safe formatting (writes into a caller-provided stack buffer; no alloc) --
	private static unsafe int Ascii(byte* buf, int pos, string s) {
		for (int i = 0; i < s.Length; i++) buf[pos++] = (byte)s[i];
		return pos;
	}
	private static unsafe int Hex(byte* buf, int pos, ulong v) {
		buf[pos++] = (byte)'0'; buf[pos++] = (byte)'x';
		for (int shift = 60; shift >= 0; shift -= 4) {
			int d = (int)((v >> shift) & 0xF);
			buf[pos++] = (byte)(d < 10 ? '0' + d : 'a' + d - 10);
		}
		return pos;
	}
	private static unsafe int Dec(byte* buf, int pos, int v) {
		if (v == 0) { buf[pos++] = (byte)'0'; return pos; }
		byte* tmp = stackalloc byte[12];
		int t = 0; uint u = (uint)v;
		while (u > 0) { tmp[t++] = (byte)('0' + (int)(u % 10)); u /= 10; }
		while (t > 0) buf[pos++] = tmp[--t];
		return pos;
	}

	// ---- Mach-O header parsing (read-only; used only while building a report) -----------

	private static unsafe bool TryGetImageInfo(IntPtr header, out string uuid, out ulong textSize, out int cpuType) {
		uuid = "00000000-0000-0000-0000-000000000000";
		textSize = 0;
		cpuType = 0;
		try {
			if (header == IntPtr.Zero) return false;
			byte* p = (byte*)header;
			uint magic = *(uint*)p;
			if (magic != 0xFEEDFACF) return false; // MH_MAGIC_64
			cpuType = *(int*)(p + 4);
			uint ncmds = *(uint*)(p + 16);
			byte* cmd = p + 32; // sizeof(mach_header_64)
			for (uint i = 0; i < ncmds; i++) {
				uint c = *(uint*)cmd;
				uint csize = *(uint*)(cmd + 4);
				if (csize < 8) break;
				if (c == 0x1B) { // LC_UUID
					uuid = FormatUuid(cmd + 8);
				} else if (c == 0x19) { // LC_SEGMENT_64
					if (SegNameIs(cmd + 8, "__TEXT"))
						textSize = *(ulong*)(cmd + 8 + 16 + 8); // segname[16], vmaddr(8), then vmsize(8)
				}
				cmd += csize;
			}
			return true;
		} catch {
			return false;
		}
	}

	private static unsafe string FormatUuid(byte* u) {
		var sb = new StringBuilder(36);
		for (int i = 0; i < 16; i++) {
			sb.Append(u[i].ToString("X2"));
			if (i == 3 || i == 5 || i == 7 || i == 9) sb.Append('-');
		}
		return sb.ToString();
	}

	private static unsafe bool SegNameIs(byte* seg, string name) {
		for (int i = 0; i < name.Length; i++)
			if (seg[i] != (byte)name[i]) return false;
		return seg[name.Length] == 0;
	}

	private static string CpuName(int cpuType) => cpuType switch {
		0x0100000C => "arm64",
		0x01000007 => "x86_64",
		_ => $"cpu_0x{cpuType:x}",
	};

	private static string SignalName(int sig) => sig switch {
		SIGILL => "SIGILL",
		SIGTRAP => "SIGTRAP",
		SIGABRT => "SIGABRT",
		SIGFPE => "SIGFPE",
		SIGBUS => "SIGBUS",
		SIGSEGV => "SIGSEGV",
		SIGSYS => "SIGSYS",
		_ => "SIGNAL", // all arms return interned literals — safe to call from the signal handler
	};

	private static void CaptureEnvironment() {
		try {
			_bundleId = NSBundle.MainBundle.BundleIdentifier ?? "";
			var info = NSBundle.MainBundle.InfoDictionary;
			_procName = info?["CFBundleName"]?.ToString() ?? "OpenTaiko";
			_version = info?["CFBundleShortVersionString"]?.ToString() ?? "";
			_build = info?["CFBundleVersion"]?.ToString() ?? "";
			var dev = UIDevice.CurrentDevice;
			_os = $"{dev.SystemName} {dev.SystemVersion}";
			_model = HwMachine(dev);
		} catch {
		}
	}

	private static string HwMachine(UIDevice dev) {
		try {
			nuint len = 0;
			if (sysctlbyname("hw.machine", null, ref len, IntPtr.Zero, 0) != 0 || len == 0)
				return dev.Model;
			var buf = new byte[(int)len];
			if (sysctlbyname("hw.machine", buf, ref len, IntPtr.Zero, 0) != 0)
				return dev.Model;
			int n = (int)len;
			if (n > 0 && buf[n - 1] == 0) n--;
			return Encoding.ASCII.GetString(buf, 0, n);
		} catch {
			return dev.Model;
		}
	}

	// ---- P/Invoke ----------------------------------------------------------------------

	[StructLayout(LayoutKind.Sequential)]
	private struct sigaction_t {
		public IntPtr sa_handler; // union of sa_handler / sa_sigaction (function pointer)
		public uint sa_mask;      // sigset_t is uint32 on Darwin
		public int sa_flags;
	}

	[DllImport("libc", SetLastError = true)]
	private static extern int sigaction(int sig, ref sigaction_t act, out sigaction_t oldact);

	[DllImport("libc")]
	private static extern int raise(int sig);

	[DllImport("libc")]
	private static extern unsafe int backtrace(void** array, int size);

	[DllImport("libc")]
	private static extern int sysctlbyname(string name, byte[]? oldp, ref nuint oldlenp, IntPtr newp, nuint newlen);

	// Async-signal-safe primitives used inside the crash handler, plus dyld image enumeration
	// and sigaltstack used at install time.
	[DllImport("libc", SetLastError = true)]
	private static extern int open(IntPtr path, int oflag);
	[DllImport("libc")]
	private static extern nint write(int fd, IntPtr buf, nint count);
	[DllImport("libc")]
	private static extern int close(int fd);
	[DllImport("libc")]
	private static extern unsafe void backtrace_symbols_fd(void** array, int size, int fd);
	[DllImport("libc")]
	private static extern uint _dyld_image_count();
	[DllImport("libc")]
	private static extern IntPtr _dyld_get_image_name(uint index);
	[DllImport("libc")]
	private static extern IntPtr _dyld_get_image_header(uint index);
	[DllImport("libc")]
	private static extern int sigaltstack(ref stack_t ss, IntPtr oss);

	[StructLayout(LayoutKind.Sequential)]
	private struct stack_t {
		public IntPtr ss_sp;
		public nint ss_size;
		public int ss_flags;
	}
#else
	/// <summary>No-op on Debug builds (see class summary for why native handlers are Release-only).</summary>
	public static void Install() { }
#endif
}
