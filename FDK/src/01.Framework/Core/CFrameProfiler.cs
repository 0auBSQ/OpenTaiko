using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FDK;

// Debug: while a play is measured, each game-loop frame is cut into named sections (a Section call closes the open
// one), and extras (Lua calls, texture uploads, sound work) are timed on top of them. For every frame the game
// thread's own CPU time, its allocations, the collections and the page faults are read, so a slow frame tells
// whether the thread was working or waiting. Slow frames are kept and written with the totals when the play stops.
public static class CFrameProfiler {
	public const double SlowFrameMs = 20;
	private const int MaxSlots = 1024, MaxRecords = 400, TopSections = 5, TopExtras = 5;

	public static bool Active { get; private set; }
	public static long ChartMs;   // the play screen's chart time, for the slow-frame lines

	private static readonly Dictionary<string, int> slotByLiteral = new(ReferenceEqualityComparer.Instance);
	private static readonly Dictionary<string, int> slotByName = new();
	private static readonly string[] names = new string[MaxSlots];
	private static readonly bool[] isExtra = new bool[MaxSlots];
	private static int nSlots;

	// this frame, per slot
	private static readonly double[] fMs = new double[MaxSlots];
	private static readonly long[] fAlloc = new long[MaxSlots];
	private static readonly int[] fCalls = new int[MaxSlots];
	private static readonly int[] touched = new int[MaxSlots];
	private static readonly bool[] isTouched = new bool[MaxSlots];
	private static int nTouched;
	// the whole play, per slot
	private static readonly double[] tMs = new double[MaxSlots], tMax = new double[MaxSlots];
	private static readonly long[] tAlloc = new long[MaxSlots], tCalls = new long[MaxSlots];
	private static readonly int[] tTop = new int[MaxSlots];   // slow frames whose largest section this was

	private static int openSlot = -1;
	private static long openTs, openAlloc;
	private static long frameTs, frameAlloc;
	private static ulong frameCycles;
	private static double cyclesPerMs = 1;
	private static readonly int[] frameGc = new int[3];
	private static TimeSpan framePause;
	private static uint frameFaults;
	private static int depth;   // nested extras: only the outermost counts toward "lua"

	private static long frames;
	private static readonly long[] histogram = new long[7];   // <8, <16, <25, <50, <100, <200, more
	private static readonly double[] histogramEdges = { 8, 16, 25, 50, 100, 200 };

	private struct Record {
		public long chartMs;
		public double ms, cpuMs, gcPauseMs;
		public long allocBytes, workingSetMB;
		public int gc0, gc1, gc2;
		public uint faults;
		public int firstDraws, prevFirstDraws;
		public long firstDrawBytes, prevFirstDrawBytes;
		public string? firstDrawNames;
	}

	// textures drawn for the first time this frame and the one before: the GL may only then make their GPU copy
	private const int FirstDrawNames = 4;
	private static int fFirstDraws, pFirstDraws;
	private static long fFirstDrawBytes, pFirstDrawBytes, tFirstDraws, tFirstDrawBytes;
	private static readonly string?[] fFirstNames = new string?[FirstDrawNames], pFirstNames = new string?[FirstDrawNames];

	[Conditional("DEBUG")]
	public static void FirstDraw(string? path, long bytes) {
		if (!Active || !OnGameThread) return;
		if (fFirstDraws < FirstDrawNames) fFirstNames[fFirstDraws] = path;
		fFirstDraws++;
		fFirstDrawBytes += bytes;
		tFirstDraws++;
		tFirstDrawBytes += bytes;
	}
	private static readonly Record[] records = new Record[MaxRecords];
	private static readonly int[] recSlot = new int[MaxRecords * (TopSections + TopExtras)];
	private static readonly double[] recMs = new double[MaxRecords * (TopSections + TopExtras)];
	private static readonly int[] recCalls = new int[MaxRecords * (TopSections + TopExtras)];
	private static int nRecords, nSlowFrames;

	private static int luaSlot = -1;

	// ── control ──

	// each frame of play: the first one starts the measure
	[Conditional("DEBUG")]
	public static void PlayFrame(long chartMs) {
		if (!Active) Start();
		ChartMs = chartMs;
	}

	[Conditional("DEBUG")]
	public static void Start() {
		if (Active) Stop();
		Array.Clear(tMs, 0, nSlots); Array.Clear(tMax, 0, nSlots); Array.Clear(tAlloc, 0, nSlots);
		Array.Clear(tCalls, 0, nSlots); Array.Clear(tTop, 0, nSlots);
		Array.Clear(histogram);
		frames = 0; nRecords = 0; nSlowFrames = 0; depth = 0;
		fFirstDraws = pFirstDraws = 0; fFirstDrawBytes = pFirstDrawBytes = tFirstDraws = tFirstDrawBytes = 0;
		ClearFrame();
		Calibrate();
		luaSlot = SlotOf("lua (all calls)", extra: true);
		frameTs = 0;
		openSlot = -1;
		Active = true;
	}

	[Conditional("DEBUG")]
	public static void Stop() {
		if (!Active) return;
		Active = false;
		Trace.TraceInformation(Report());
	}

	// top of each game-loop iteration
	[Conditional("DEBUG")]
	public static void FrameBegin() {
		if (!Active || !OnGameThread) return;
		long now = Stopwatch.GetTimestamp();
		long alloc = GC.GetAllocatedBytesForCurrentThread();
		Close(now, alloc);
		if (frameTs != 0) EndFrame(now, alloc);
		else ClearFrame();   // sections drawn before the first frame began
		frameTs = now;
		frameAlloc = alloc;
		frameCycles = ThreadCycles();
		for (int g = 0; g < 3; g++) frameGc[g] = GC.CollectionCount(g);
		framePause = GC.GetTotalPauseDuration();
		frameFaults = PageFaults(out _);
		Open("loop", now, alloc);
	}

	// closes the open section and opens this one; the name is a literal
	[Conditional("DEBUG")]
	public static void Section(string name) {
		if (!Active || !OnGameThread) return;
		long now = Stopwatch.GetTimestamp();
		long alloc = GC.GetAllocatedBytesForCurrentThread();
		Close(now, alloc);
		Open(name, now, alloc);
	}

	// ── extras: timed on top of the sections ──

	// a slot for a composed name (a Lua function of a script); register once and keep the number
	public static int Register(string name) {
		lock (slotByName) {
			if (slotByName.TryGetValue(name, out int slot)) return slot;
			if (nSlots >= MaxSlots) return -1;
			slot = nSlots++;
			names[slot] = name;
			isExtra[slot] = true;
			slotByName[name] = slot;
			return slot;
		}
	}

	public static int Extra(string literal) => SlotOf(literal, extra: true);

	public static long ExtraBegin() {
		if (!Active || !OnGameThread) return 0;
		depth++;
		return Stopwatch.GetTimestamp();
	}

	public static void ExtraEnd(int slot, long start, long allocAtStart = 0) {
		if (start == 0) return;
		depth--;
		if (!Active || slot < 0) return;
		double ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
		long alloc = allocAtStart != 0 ? GC.GetAllocatedBytesForCurrentThread() - allocAtStart : 0;
		Add(slot, ms, alloc);
		if (depth == 0 && luaSlot >= 0 && names[slot].StartsWith("lua:", StringComparison.Ordinal)) Add(luaSlot, ms, alloc);
	}

	public static long AllocNow() => Active ? GC.GetAllocatedBytesForCurrentThread() : 0;

	// ── internals ──

	private static bool OnGameThread => Environment.CurrentManagedThreadId == Game.MainThreadID;

	private static int SlotOf(string literal, bool extra) {
		lock (slotByName) {
			if (slotByLiteral.TryGetValue(literal, out int slot)) return slot;
			if (!slotByName.TryGetValue(literal, out slot)) {
				if (nSlots >= MaxSlots) return -1;
				slot = nSlots++;
				names[slot] = literal;
				isExtra[slot] = extra;
				slotByName[literal] = slot;
			}
			slotByLiteral[literal] = slot;
			return slot;
		}
	}

	private static void Open(string name, long now, long alloc) {
		openSlot = SlotOf(name, extra: false);
		openTs = now;
		openAlloc = alloc;
	}

	private static void Close(long now, long alloc) {
		if (openSlot < 0) return;
		Add(openSlot, Stopwatch.GetElapsedTime(openTs, now).TotalMilliseconds, alloc - openAlloc);
		openSlot = -1;
	}

	private static void Add(int slot, double ms, long alloc) {
		if (!isTouched[slot]) {
			isTouched[slot] = true;
			touched[nTouched++] = slot;
		}
		fMs[slot] += ms;
		fAlloc[slot] += alloc;
		fCalls[slot]++;
	}

	private static void EndFrame(long now, long alloc) {
		double ms = Stopwatch.GetElapsedTime(frameTs, now).TotalMilliseconds;
		frames++;
		int b = 0;
		while (b < histogramEdges.Length && ms >= histogramEdges[b]) b++;
		histogram[b]++;

		int top = -1;
		for (int i = 0; i < nTouched; i++) {
			int s = touched[i];
			if (!isExtra[s] && (top < 0 || fMs[s] > fMs[top])) top = s;
		}

		if (ms >= SlowFrameMs) {
			nSlowFrames++;
			if (top >= 0) tTop[top]++;
			if (nRecords < MaxRecords) {
				ref Record r = ref records[nRecords];
				r.chartMs = ChartMs;
				r.ms = ms;
				r.cpuMs = (ThreadCycles() - frameCycles) / cyclesPerMs;
				r.allocBytes = alloc - frameAlloc;
				r.gc0 = GC.CollectionCount(0) - frameGc[0];
				r.gc1 = GC.CollectionCount(1) - frameGc[1];
				r.gc2 = GC.CollectionCount(2) - frameGc[2];
				r.gcPauseMs = (GC.GetTotalPauseDuration() - framePause).TotalMilliseconds;
				r.faults = PageFaults(out long ws) - frameFaults;
				r.workingSetMB = ws / (1024 * 1024);
				r.firstDraws = fFirstDraws;
				r.firstDrawBytes = fFirstDrawBytes;
				r.prevFirstDraws = pFirstDraws;
				r.prevFirstDrawBytes = pFirstDrawBytes;
				r.firstDrawNames = fFirstDraws + pFirstDraws == 0 ? null
					: string.Join(", ", pFirstNames.Take(Math.Min(pFirstDraws, FirstDrawNames)).Concat(fFirstNames.Take(Math.Min(fFirstDraws, FirstDrawNames)))
						.Select(p => p == null ? "(no file)" : Path.GetFileName(Path.GetDirectoryName(p)) + "/" + Path.GetFileName(p)));
				int baseIdx = nRecords * (TopSections + TopExtras);
				Pick(baseIdx, TopSections, extra: false);
				Pick(baseIdx + TopSections, TopExtras, extra: true);
				nRecords++;
			}
		}
		pFirstDraws = fFirstDraws;
		pFirstDrawBytes = fFirstDrawBytes;
		Array.Copy(fFirstNames, pFirstNames, FirstDrawNames);
		fFirstDraws = 0;
		fFirstDrawBytes = 0;

		for (int i = 0; i < nTouched; i++) {
			int s = touched[i];
			tMs[s] += fMs[s];
			if (fMs[s] > tMax[s]) tMax[s] = fMs[s];
			tAlloc[s] += fAlloc[s];
			tCalls[s] += fCalls[s];
		}
		ClearFrame();
	}

	// the n largest slots of a kind this frame, into the record
	private static void Pick(int at, int n, bool extra) {
		for (int k = 0; k < n; k++) {
			int best = -1;
			for (int i = 0; i < nTouched; i++) {
				int s = touched[i];
				if (isExtra[s] != extra || fMs[s] < 0) continue;
				bool taken = false;
				for (int j = 0; j < k; j++) if (recSlot[at + j] == s) { taken = true; break; }
				if (taken) continue;
				if (best < 0 || fMs[s] > fMs[best]) best = s;
			}
			recSlot[at + k] = best;
			recMs[at + k] = best >= 0 ? fMs[best] : 0;
			recCalls[at + k] = best >= 0 ? fCalls[best] : 0;
		}
	}

	private static void ClearFrame() {
		for (int i = 0; i < nTouched; i++) {
			int s = touched[i];
			fMs[s] = 0; fAlloc[s] = 0; fCalls[s] = 0; isTouched[s] = false;
		}
		nTouched = 0;
	}

	private static string Report() {
		var sb = new StringBuilder();
		sb.Append($"[PROFILE] {frames} frames, {nSlowFrames} of {SlowFrameMs:0} ms or more. Frame times:");
		string[] labels = { "<8", "8-16", "16-25", "25-50", "50-100", "100-200", ">200" };
		for (int i = 0; i < histogram.Length; i++) sb.Append($" {labels[i]} ms {histogram[i]},");
		sb.Length--;
		sb.AppendLine($". Textures drawn for the first time during play: {tFirstDraws} ({tFirstDrawBytes / (1024 * 1024)} MB)");
		sb.AppendLine("[PROFILE] sections and extras: total ms, slowest frame ms, per frame ms, calls, KB allocated, slow frames it led");
		var order = Enumerable.Range(0, nSlots).Where(s => tCalls[s] > 0).OrderByDescending(s => tMs[s]).ToList();
		foreach (int s in order) {
			sb.AppendLine($"  {(isExtra[s] ? "+" : " ")} {names[s],-44} {tMs[s],10:0} {tMax[s],9:0.0} {tMs[s] / Math.Max(1, frames),8:0.000} {tCalls[s],9} {tAlloc[s] / 1024,10}{(tTop[s] > 0 ? $" {tTop[s]}" : "")}");
		}
		for (int r = 0; r < nRecords; r++) {
			ref Record rec = ref records[r];
			sb.Append($"[PROFILE] at {rec.chartMs} ms: {rec.ms:0.0} ms (game thread busy {rec.cpuMs:0.0} ms, {rec.allocBytes / 1024} KB, GC {rec.gc0}/{rec.gc1}/{rec.gc2} {rec.gcPauseMs:0.0} ms, {rec.faults} page faults, working set {rec.workingSetMB} MB):");
			int baseIdx = r * (TopSections + TopExtras);
			for (int k = 0; k < TopSections; k++) {
				int s = recSlot[baseIdx + k];
				if (s >= 0 && recMs[baseIdx + k] >= 0.5) sb.Append($" {names[s]} {recMs[baseIdx + k]:0.0}");
			}
			if (rec.firstDrawNames != null)
				sb.Append($" | first drawn: {rec.prevFirstDraws} ({rec.prevFirstDrawBytes / (1024 * 1024)} MB) the frame before, {rec.firstDraws} ({rec.firstDrawBytes / (1024 * 1024)} MB) this one: {rec.firstDrawNames}");
			bool first = true;
			for (int k = 0; k < TopExtras; k++) {
				int s = recSlot[baseIdx + TopSections + k];
				if (s < 0 || recMs[baseIdx + TopSections + k] < 0.5) continue;
				sb.Append(first ? " | " : ", ");
				first = false;
				sb.Append($"{names[s]} {recMs[baseIdx + TopSections + k]:0.0}");
				if (recCalls[baseIdx + TopSections + k] > 1) sb.Append($" x{recCalls[baseIdx + TopSections + k]}");
			}
			sb.AppendLine();
		}
		return sb.ToString().TrimEnd();
	}

	// cycles per millisecond of this thread, from a short busy wait
	private static void Calibrate() {
		ulong c0 = ThreadCycles();
		long t0 = Stopwatch.GetTimestamp();
		while (Stopwatch.GetElapsedTime(t0).TotalMilliseconds < 20) { }
		double ms = Stopwatch.GetElapsedTime(t0).TotalMilliseconds;
		ulong c1 = ThreadCycles();
		cyclesPerMs = c1 > c0 ? (c1 - c0) / ms : 1;
	}

	private static ulong ThreadCycles() {
		if (!OperatingSystem.IsWindows()) return 0;
		return QueryThreadCycleTime(GetCurrentThread(), out ulong cycles) ? cycles : 0;
	}

	private static uint PageFaults(out long workingSet) {
		workingSet = 0;
		if (!OperatingSystem.IsWindows()) return 0;
		var c = new ProcessMemoryCounters { cb = (uint)Marshal.SizeOf<ProcessMemoryCounters>() };
		if (!GetProcessMemoryInfo(GetCurrentProcess(), ref c, c.cb)) return 0;
		workingSet = (long)c.WorkingSetSize;
		return c.PageFaultCount;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct ProcessMemoryCounters {
		public uint cb, PageFaultCount;
		public nuint PeakWorkingSetSize, WorkingSetSize, QuotaPeakPagedPoolUsage, QuotaPagedPoolUsage,
			QuotaPeakNonPagedPoolUsage, QuotaNonPagedPoolUsage, PagefileUsage, PeakPagefileUsage;
	}

	[DllImport("kernel32.dll")] private static extern nint GetCurrentThread();
	[DllImport("kernel32.dll")] private static extern nint GetCurrentProcess();
	[DllImport("kernel32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool QueryThreadCycleTime(nint thread, out ulong cycles);
	[DllImport("kernel32.dll", EntryPoint = "K32GetProcessMemoryInfo")][return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetProcessMemoryInfo(nint process, ref ProcessMemoryCounters counters, uint size);
}
