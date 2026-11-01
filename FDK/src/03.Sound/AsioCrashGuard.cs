using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FDK;

/// <summary>
/// Some ASIO drivers crash on a thread of their own: the Realtek one writes to a null pointer when headphones are
/// unplugged. That access violation would end the game. On Windows x64, the last-chance exception filter ends only
/// the faulting thread when the fault lies in a watched driver DLL; the sound manager then moves the sound off that
/// driver and never calls it again in this run. Every other crash goes on to the filter that was there before.
/// </summary>
internal static unsafe class AsioCrashGuard {
	[DllImport("kernel32.dll")] private static extern nint SetUnhandledExceptionFilter(nint filter);
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandleW(string name);
	[DllImport("kernel32.dll", CharSet = CharSet.Ansi, BestFitMapping = false)] private static extern nint GetProcAddress(nint module, string name);

	private static nint previousFilter, exitThread;
	private static bool installed;
	// the watched DLLs' address ranges: the filter reads them without a lock, so a range is written before `count` covers it
	private static readonly long[] starts = new long[8], ends = new long[8];
	private static int count;
	private static int crashPending;

	/// <summary>A driver crash was caught and waits for the sound manager.</summary>
	public static bool CrashPending => Volatile.Read(ref crashPending) != 0;
	public static bool TakeCrash() => Interlocked.Exchange(ref crashPending, 0) != 0;

	/// <summary>Watches the driver DLL BASSASIO names for an open device (the file name or its full path).</summary>
	public static void Watch(string? driverFile) {
		if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64 || string.IsNullOrEmpty(driverFile)) return;
		nint module = GetModuleHandleW(driverFile);
		if (module == 0) module = GetModuleHandleW(Path.GetFileName(driverFile));
		if (module == 0) {
			Trace.TraceWarning($"The ASIO driver's crashes are not caught: {driverFile} is not loaded.");
			return;
		}
		// SizeOfImage, from the PE header of the loaded DLL
		long size = *(uint*)(module + *(int*)(module + 0x3C) + 0x50);
		WatchRange(module, module + size);
	}

	internal static void WatchRange(long start, long end) {
		lock (starts) {
			if (!installed) {
				exitThread = GetProcAddress(GetModuleHandleW("kernel32.dll"), "ExitThread");
				if (exitThread == 0) return;
				previousFilter = SetUnhandledExceptionFilter((nint)(delegate* unmanaged<nint, int>)&Filter);
				installed = true;
			}
			for (int i = 0; i < count; i++) {
				if (starts[i] == start) return;
			}
			if (count == starts.Length) return;
			starts[count] = start;
			ends[count] = end;
			Volatile.Write(ref count, count + 1);
		}
	}

	// x64 layouts: EXCEPTION_POINTERS { ExceptionRecord*, ContextRecord* }; EXCEPTION_RECORD { ExceptionCode @0x00,
	// ExceptionAddress @0x10 }; CONTEXT { Rcx @0x80, Rsp @0x98, Rip @0xF8 }
	[UnmanagedCallersOnly]
	private static int Filter(nint pointers) {
		byte* record = *(byte**)pointers;
		byte* context = *(byte**)(pointers + 8);
		if (*(uint*)record == 0xC0000005) {   // an access violation
			long address = *(long*)(record + 0x10);
			int n = Volatile.Read(ref count);
			for (int i = 0; i < n; i++) {
				if (address < starts[i] || address >= ends[i]) continue;
				// the thread goes on into ExitThread(0), as if called, below the faulting frame
				ulong rsp = ((*(ulong*)(context + 0x98) - 0x100) & ~0xFUL) - 8;
				*(ulong*)rsp = 0;   // the return address ExitThread never uses
				*(ulong*)(context + 0x98) = rsp;
				*(ulong*)(context + 0x80) = 0;
				*(ulong*)(context + 0xF8) = (ulong)exitThread;
				Interlocked.Exchange(ref crashPending, 1);
				return -1;   // EXCEPTION_CONTINUE_EXECUTION
			}
		}
		return previousFilter != 0 ? ((delegate* unmanaged<nint, int>)previousFilter)(pointers) : 0;   // 0: EXCEPTION_CONTINUE_SEARCH
	}
}
