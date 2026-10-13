using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using KeraLua;

namespace OpenTaiko.AviUtl2 {
	// The script files of a project (*.anm2, *.obj2, *.anm, *.obj) and the Lua state that runs them against
	// AviUtl's `obj` interface. The scripts are not trusted: the state has no file, process or library access.
	internal sealed class Aup2ScriptLibrary : IAup2Scripts, IDisposable {
		public const string ScriptControl = "スクリプト制御";
		private const string ScriptKey = "スクリプト";
		// how long one run of a script may take, and how much memory the state may hold
		internal double BudgetMs { get; set; } = 3000;
		private const int MemoryLimitKb = 512 * 1024;
		private const int ListLimit = 200;

		private readonly List<Aup2ScriptFile> _files = new();
		private readonly Dictionary<string, Aup2ScriptSection> _byName = new(StringComparer.Ordinal);
		private readonly Dictionary<string, Aup2ScriptSection> _inline = new(StringComparer.Ordinal);
		private readonly Dictionary<Aup2ScriptSection, Compiled> _compiled = new();
		private readonly Aup2ScriptFile _inlineFile = new() { Stem = ScriptControl };
		private readonly List<string> _errors = new(), _messages = new();
		private readonly HashSet<Aup2ScriptSection> _reported = new();

		private sealed class Compiled {
			public int Ref;
			public bool Disabled;
		}

		// the first error of each script
		public IReadOnlyList<string> Errors => _errors;
		// what the scripts printed (print, debug_print, obj.mes) and what they asked for that is not here
		public IReadOnlyList<string> Messages => _messages;
		public IReadOnlyList<Aup2ScriptFile> Files => _files;
		// every effect name that designates a script
		public IReadOnlyCollection<string> Names => _byName.Keys;
		// faces sent to the host since the library was made
		public long PolygonCount { get; private set; }

		// folders: each is searched with its sub-folders, one level down
		public Aup2ScriptLibrary(IEnumerable<string> folders) {
			var paths = new List<string>();
			foreach (string folder in folders) {
				if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) continue;
				try {
					paths.AddRange(Directory.GetFiles(folder));
					foreach (string sub in Directory.GetDirectories(folder)) paths.AddRange(Directory.GetFiles(sub));
				} catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
			}
			paths.Sort(StringComparer.Ordinal);
			foreach (string path in paths) {
				string ext = Path.GetExtension(path).ToLowerInvariant();
				if (ext != ".anm" && ext != ".anm2" && ext != ".obj" && ext != ".obj2") continue;
				try {
					if (new FileInfo(path).Length > 2 * 1024 * 1024) continue;   // a 3d model can be named .obj too
					_files.Add(Aup2ScriptFile.Load(path));
				} catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
			}
			Index();
		}

		// "section@file" always; the file's name for a file of one script; the bare section when one file has it
		private void Index() {
			var seen = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (var f in _files) {
				foreach (var s in f.Sections) {
					if (s.Name.Length == 0) continue;
					_byName.TryAdd(s.Name + "@" + f.Stem, s);
					seen[s.Name] = seen.GetValueOrDefault(s.Name) + 1;
				}
			}
			foreach (var f in _files) {
				if (f.Multi || f.Sections.Count != 1) continue;
				_byName.TryAdd(f.Stem, f.Sections[0]);
			}
			foreach (var f in _files)
				foreach (var s in f.Sections)
					if (s.Name.Length > 0 && seen[s.Name] == 1) _byName.TryAdd(s.Name, s);
		}

		public Aup2ScriptSection? Find(string effectName) => _byName.GetValueOrDefault(effectName);

		public bool Knows(string effectName) => effectName == ScriptControl || _byName.ContainsKey(effectName);

		#region The Lua state
		private NLua.Lua? _lua;
		private Lua? _main;
		private bool _broken;
		private int _runRef;
		private Lua? _thread;
		private int _threadRef;
		private readonly Dictionary<IntPtr, Lua> _wrappers = new();
		private long _started;
		private bool _over;
		private readonly long _born = Stopwatch.GetTimestamp();

		[ThreadStatic] private static Aup2ScriptLibrary? _running;
		// kept so the callbacks handed to Lua stay alive
		private static readonly LuaFunction _dispatch = Dispatch;
		private static readonly LuaHookFunction _hook = Hook;

		private static readonly string[] HostNames = {
			"sync", "size", "draw", "poly", "load", "setfont", "getfont", "rand", "rand1", "setoption", "getoption", "getvalue",
			"getpixel", "putpixel", "copypixel", "pixeloption", "copybuffer", "clearbuffer", "effect", "getinfo", "log", "portable",
		};

		private bool EnsureState() {
			if (_main != null) return true;
			if (_broken) return false;
			try {
				_lua = new NLua.Lua();
				var L = _lua.State;
				L.Encoding = Encoding.UTF8;
				L.NewTable();
				for (int i = 0; i < HostNames.Length; i++) {
					L.PushInteger(i);
					L.PushCClosure(_dispatch, 1);
					L.SetField(-2, HostNames[i]);
				}
				if (L.LoadString(Prelude, "=aup2") != LuaStatus.OK) throw new InvalidOperationException(L.ToString(-1, false));
				L.Insert(-2);
				if (L.PCall(1, 1, 0) != LuaStatus.OK) throw new InvalidOperationException(L.ToString(-1, false));
				_runRef = L.Ref(LuaRegistry.Index);
				L.GarbageCollector(LuaGC.Collect, 0);
				_main = L;
				return true;
			} catch (Exception e) {
				_broken = true;
				Add(_errors, "script host: " + e.Message);
				try { _lua?.Dispose(); } catch { }
				_lua = null;
				return false;
			}
		}

		private Lua Thread() {
			if (_thread != null) return _thread;
			_thread = _main!.NewThread();
			_threadRef = _main.Ref(LuaRegistry.Index);
			_thread.SetHook(_hook, LuaHookMask.Count, 10000);
			return _thread;
		}

		private void DropThread() {
			if (_thread == null) return;
			_main!.Unref(LuaRegistry.Index, _threadRef);
			_thread = null;
			_wrappers.Clear();
		}

		private Lua Wrap(IntPtr p) {
			if (_thread != null && p == _thread.Handle) return _thread;
			if (p == _main!.Handle) return _main;
			if (!_wrappers.TryGetValue(p, out var L)) {
				if (_wrappers.Count > 256) _wrappers.Clear();
				L = Lua.FromIntPtr(p);
				_wrappers[p] = L;
			}
			return L;
		}

		[MonoPInvokeCallback(typeof(LuaFunction))]
		private static int Dispatch(IntPtr p) {
			var lib = _running;
			if (lib == null) return 0;
			try {
				var L = lib.Wrap(p);
				return lib.Call((int)L.ToInteger(Lua.UpValueIndex(1)), L);
			} catch (Exception e) {
				// nothing managed may cross back into Lua
				lib.Report(lib._section, e.GetType().Name + ": " + e.Message);
				return 0;
			}
		}

		// Stops a script that runs too long or holds too much: it is made to yield out to the host, which
		// never resumes it.
		[MonoPInvokeCallback(typeof(LuaHookFunction))]
		private static void Hook(IntPtr p, IntPtr ar) {
			try {
				var lib = _running;
				if (lib == null) return;
				if (!lib._over) {
					if (Stopwatch.GetElapsedTime(lib._started).TotalMilliseconds < lib.BudgetMs
						&& lib._main!.GarbageCollector(LuaGC.Count, 0) < MemoryLimitKb) return;
					lib._over = true;
				}
				var L = lib.Wrap(p);
				if (L.IsYieldable) L.Yield(0);
			} catch { }
		}

		public void Dispose() {
			if (_lua == null) return;
			_thread = null;
			_main = null;
			_wrappers.Clear();
			_compiled.Clear();
			try { _lua.Dispose(); } catch { }
			_lua = null;
			_broken = true;
		}
		#endregion

		#region Running a script
		private Aup2Context? _c;
		private Aup2ScriptSection? _section;
		// the object the values below belong to
		private Aup2Context? _owner;
		// what the script set: obj.ox .. obj.sy
		private double _ox, _oy, _oz, _rx, _ry, _rz, _cx, _cy, _cz, _zoom = 1, _alpha = 1, _aspect, _sx = 1, _sy = 1;
		// the object without it
		private double _bx, _by, _bz, _brx, _bry, _brz, _bcx, _bcy, _bcz, _bsx = 1, _bsy = 1, _bop = 1;

		private static void Add(List<string> list, string text) {
			if (list.Count < ListLimit) list.Add(text);
		}

		private static string Label(Aup2ScriptSection? s) => s == null ? "script" : s.File.Multi ? s.Name + "@" + s.File.Stem : s.File.Stem;

		// a chunk's name goes through the system's own encoding on its way into Lua: it is a number there,
		// and the script's name takes its place in what is reported
		private const string ChunkPrefix = "script#";
		private readonly List<Aup2ScriptSection> _chunks = new();

		private string Named(string message) {
			int at = 0;
			while ((at = message.IndexOf(ChunkPrefix, at, StringComparison.Ordinal)) >= 0) {
				int from = at + ChunkPrefix.Length, to = from;
				while (to < message.Length && char.IsAsciiDigit(message[to])) to++;
				if (to == from || !int.TryParse(message.AsSpan(from, to - from), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) || n >= _chunks.Count) { at = to; continue; }
				string label = Label(_chunks[n]);
				message = message[..at] + label + message[to..];
				at += label.Length;
			}
			return message;
		}

		private void Report(Aup2ScriptSection? section, string message) {
			if (section != null && !_reported.Add(section)) return;
			Add(_errors, Label(section) + ": " + Named(message));
		}

		private static string Unescape(string s) {
			if (s.IndexOf('\\') < 0) return s;
			var sb = new StringBuilder(s.Length);
			for (int i = 0; i < s.Length; i++) {
				if (s[i] == '\\' && i + 1 < s.Length && (s[i + 1] == 'n' || s[i + 1] == '\\')) {
					sb.Append(s[i + 1] == 'n' ? '\n' : '\\');
					i++;
				} else sb.Append(s[i]);
			}
			return sb.ToString();
		}

		private Aup2ScriptSection? Resolve(Aup2Context c) {
			string name = c.Effect.Name;
			if (name != ScriptControl) return _byName.GetValueOrDefault(name);
			string source = Unescape(c.S(ScriptKey));
			if (source.Trim().Length == 0) return null;
			if (!_inline.TryGetValue(source, out var s)) {
				if (_inline.Count > 256) {
					foreach (var old in _inline.Values) Forget(old);
					_inline.Clear();
				}
				s = new Aup2ScriptSection { Name = ScriptControl, File = _inlineFile, Code = source };
				_inline[source] = s;
			}
			return s;
		}

		private void Forget(Aup2ScriptSection s) {
			if (_compiled.Remove(s, out var rt) && rt.Ref != 0 && _main != null) _main.Unref(LuaRegistry.Index, rt.Ref);
			_reported.Remove(s);
		}

		private static readonly HashSet<string> Reserved = new() {
			"and", "break", "do", "else", "elseif", "end", "false", "for", "function", "goto", "if", "in", "local", "nil", "not", "or",
			"repeat", "return", "then", "true", "until", "while",
		};

		private static bool IsLuaName(string name) {
			if (name.Length == 0 || char.IsAsciiDigit(name[0]) || Reserved.Contains(name)) return false;
			foreach (char ch in name)
				if (!(char.IsAsciiLetterOrDigit(ch) || ch == '_')) return false;
			return true;
		}

		private static bool Numbered(Aup2ScriptSetting s) => s.Slot >= 0 && s.Legacy;

		// The section as one function: its settings arrive in a table and become the variables the script
		// reads. All of that stands on the first line, so the lines after it keep the file's numbers.
		private bool Compile(Aup2ScriptSection section, Compiled rt) {
			var sb = new StringBuilder("local __s = ... ");
			foreach (var s in section.Settings) {
				if (Numbered(s)) continue;
				string name = Aup2ScriptFile.Mangle(s.Variable);
				if (!IsLuaName(name)) continue;
				if (!(s.Kind == Aup2SettingKind.Dialog && !s.Local)) sb.Append("local ");
				sb.Append(name).Append("=__s.").Append(name).Append(' ');
			}
			foreach (string code in section.Preamble) sb.Append(Aup2ScriptFile.Portable(code).Replace('\n', ' ')).Append(' ');
			sb.Append('\n', Math.Max(0, section.FirstLine - 1));
			sb.Append(Aup2ScriptFile.Portable(section.Code));
			var L = _main!;
			_chunks.Add(section);
			string chunk = "=" + ChunkPrefix + (_chunks.Count - 1).ToString(CultureInfo.InvariantCulture);
			if (L.LoadBuffer(Encoding.UTF8.GetBytes(sb.ToString()), chunk, "t") != LuaStatus.OK) {
				Report(section, L.ToString(-1, false) ?? "does not compile");
				L.Pop(1);
				rt.Disabled = true;
				return false;
			}
			rt.Ref = L.Ref(LuaRegistry.Index);
			return true;
		}

		public void Run(Aup2Context c) {
			var section = Resolve(c);
			if (section == null) return;
			if (!_compiled.TryGetValue(section, out var rt)) _compiled[section] = rt = new Compiled();
			if (rt.Disabled || !EnsureState()) return;
			if (rt.Ref == 0 && !Compile(section, rt)) return;

			var item = c.Item;
			var keep = (item.X, item.Y, item.Z, item.RotX, item.RotY, item.RotZ, item.PivotX, item.PivotY, item.PivotZ,
				item.ScaleX, item.ScaleY, item.Opacity, item.Blend, item.Billboard);
			var previous = _running;
			var co = Thread();
			_running = this;
			_c = c;
			_section = section;
			Begin(c);
			_started = Stopwatch.GetTimestamp();
			_over = false;
			bool ok = false;
			try {
				co.SetTop(0);
				co.RawGetInteger(LuaRegistry.Index, _runRef);
				co.RawGetInteger(LuaRegistry.Index, rt.Ref);
				PushSettings(co, c, section);
				PushVariables(co, c, section);
				var status = co.Resume(_main, 4, out _);
				if (status == LuaStatus.OK) {
					ok = true;
					co.SetTop(0);
					Flush();
				} else if (status == LuaStatus.Yield) {
					if (_over) rt.Disabled = true;
					Report(section, _over ? "stopped: it ran too long or used too much memory" : "attempt to yield from outside a coroutine");
					DropThread();
				} else {
					Report(section, co.ToString(-1, false) ?? "error");
					DropThread();
				}
			} catch (Exception e) {
				Report(section, e.GetType().Name + ": " + e.Message);
				DropThread();
			} finally {
				if (c.Flat) {
					if (c.Host != null) c.Host.SetDrawTarget(c, false, 0, 0);
					else c.Flat = false;
				}
				if (!ok) {
					// the object stays as it was
					(item.X, item.Y, item.Z, item.RotX, item.RotY, item.RotZ, item.PivotX, item.PivotY, item.PivotZ,
						item.ScaleX, item.ScaleY, item.Opacity, item.Blend, item.Billboard) = keep;
					_owner = null;
					_pix = null;
					_dirty = false;
				}
				_c = null;
				_section = null;
				_running = previous;
			}
		}

		private void Begin(Aup2Context c) {
			if (c.Scene != _frameScene || c.Now != _frameNow) {
				_frameScene = c.Scene; _frameNow = c.Now;
				_tagPix.Clear(); _versions.Clear(); _sizes.Clear();
			}
			bool fresh = !ReferenceEquals(c, _owner);
			if (fresh) {
				_owner = c;
				_ox = _oy = _oz = _rx = _ry = _rz = _cx = _cy = _cz = _aspect = 0;
				_zoom = _alpha = _sx = _sy = 1;
				_pix = null; _dirty = false; _objTag = null;
			}
			Rebase(fresh);
			// these last one call
			c.CullBack = false;
			_font = new Aup2TextStyle { Size = 34 };
			_fontKind = 0;
			_pixType = "col";
			_pixBlend = -1;
		}

		private double FactorX => _zoom * _sx * (_aspect > 0 ? 1 - Math.Min(_aspect, 1) : 1);
		private double FactorY => _zoom * _sy * (_aspect < 0 ? 1 + Math.Max(_aspect, -1) : 1);

		// what the object is without the script's own values
		private void Rebase(bool fresh) {
			var it = _c!.Item;
			_bx = it.X - _ox; _by = it.Y - _oy; _bz = it.Z - _oz;
			_brx = it.RotX - _rx; _bry = it.RotY - _ry; _brz = it.RotZ - _rz;
			_bcx = it.PivotX - _cx; _bcy = it.PivotY - _cy; _bcz = it.PivotZ - _cz;
			double fx = FactorX, fy = FactorY;
			if (fx != 0) _bsx = it.ScaleX / fx; else if (fresh) _bsx = it.ScaleX;
			if (fy != 0) _bsy = it.ScaleY / fy; else if (fresh) _bsy = it.ScaleY;
			if (_alpha > 0) _bop = it.Opacity / Math.Min(_alpha, 1); else if (fresh) _bop = it.Opacity;
		}

		private void Apply(bool position) {
			var it = _c!.Item;
			if (position) { it.X = _bx + _ox; it.Y = _by + _oy; it.Z = _bz + _oz; }
			it.RotX = _brx + _rx; it.RotY = _bry + _ry; it.RotZ = _brz + _rz;
			it.PivotX = _bcx + _cx; it.PivotY = _bcy + _cy; it.PivotZ = _bcz + _cz;
			it.ScaleX = _bsx * FactorX; it.ScaleY = _bsy * FactorY;
			it.Opacity = _bop * Math.Clamp(_alpha, 0, 1);
		}

		private static string? FindKey(Aup2Effect e, Aup2ScriptSetting s) {
			foreach (string key in s.Keys())
				if (e.Has(key)) return key;
			return null;
		}

		private static bool Truth(string text) {
			text = text.Trim();
			if (text == "true") return true;
			return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && v != 0;
		}

		// a colour as a project writes it ("rrggbb"), or as a script does (0xrrggbb, a number); null: none
		private static long? Colour(string text, bool fromProject) {
			text = text.Trim();
			if (text.Length == 0 || text == "nil") return null;
			if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				return long.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hex) ? hex : null;
			if (fromProject && text.Length == 6 && long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long rgb)) return rgb;
			return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? (long)v : null;
		}

		private static void PushNum(Lua L, double v) {
			if (v == Math.Floor(v) && Math.Abs(v) < 9e15) L.PushInteger((long)v);
			else L.PushNumber(double.IsNaN(v) ? 0 : v);
		}

		private static void SetNum(Lua L, int table, string key, double v) { PushNum(L, v); L.SetField(table, key); }

		// two tables: the settings ready to use, and those written as Lua text, worked out on the other side
		private void PushSettings(Lua L, Aup2Context c, Aup2ScriptSection section) {
			L.NewTable();
			int values = L.GetTop();
			L.NewTable();
			int texts = values + 1;
			var e = c.Effect;
			foreach (var s in section.Settings) {
				if (Numbered(s)) continue;
				string name = Aup2ScriptFile.Mangle(s.Variable);
				string? key = FindKey(e, s);
				string? saved = key == null ? null : e.Get(key);
				switch (s.Kind) {
					case Aup2SettingKind.Track:
						SetNum(L, values, name, key != null ? c.V(key, s.Number) : s.Number);
						break;
					case Aup2SettingKind.Check: {
						bool on = saved != null ? Truth(saved) : s.Number != 0;
						if (s.Boolean) L.PushBoolean(on); else L.PushInteger(on ? 1 : 0);
						L.SetField(values, name);
						break;
					}
					case Aup2SettingKind.Color: {
						long? col = Colour(saved ?? s.Default, saved != null);
						if (col == null) break;
						L.PushInteger(col.Value);
						L.SetField(values, name);
						break;
					}
					case Aup2SettingKind.Select: {
						string text = saved ?? s.Default;
						foreach (var choice in s.Choices)
							if (choice.Key == text) { text = choice.Value; break; }
						if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) PushNum(L, v);
						else L.PushString(text);
						L.SetField(values, name);
						break;
					}
					case Aup2SettingKind.Text:
						L.PushString(saved != null ? Unescape(saved) : s.Default);
						L.SetField(values, name);
						break;
					case Aup2SettingKind.Value:
						L.PushString(saved ?? s.Default);
						L.SetField(texts, name);
						break;
					case Aup2SettingKind.Dialog: {
						string text = saved ?? s.Default;
						if (saved != null && s.Hint == "col" && Colour(saved, true) is long col) text = col.ToString(CultureInfo.InvariantCulture);
						L.PushString(text);
						L.SetField(texts, name);
						break;
					}
					default:
						L.PushString(saved ?? s.Default);
						L.SetField(values, name);
						break;
				}
			}
		}

		private void PushVariables(Lua L, Aup2Context c, Aup2ScriptSection section) {
			L.NewTable();
			int t = L.GetTop();
			var o = c.Object;
			SetNum(L, t, "ox", _ox); SetNum(L, t, "oy", _oy); SetNum(L, t, "oz", _oz);
			SetNum(L, t, "rx", _rx); SetNum(L, t, "ry", _ry); SetNum(L, t, "rz", _rz);
			SetNum(L, t, "cx", _cx); SetNum(L, t, "cy", _cy); SetNum(L, t, "cz", _cz);
			SetNum(L, t, "sx", _sx); SetNum(L, t, "sy", _sy); SetNum(L, t, "sz", 1);
			SetNum(L, t, "zoom", _zoom); SetNum(L, t, "aspect", _aspect); SetNum(L, t, "alpha", _alpha);
			SetNum(L, t, "x", _bx); SetNum(L, t, "y", _by); SetNum(L, t, "z", _bz);
			SetNum(L, t, "screen_w", c.FrameW); SetNum(L, t, "screen_h", c.FrameH);
			SetNum(L, t, "framerate", c.Scene.Rate);
			SetNum(L, t, "frame", Math.Floor(c.Frame)); SetNum(L, t, "time", c.Time);
			SetNum(L, t, "totalframe", o.End + 1 - o.Start); SetNum(L, t, "totaltime", c.Duration);
			SetNum(L, t, "layer", o.Layer + 1); SetNum(L, t, "effect_layer", o.Layer + 1);
			SetNum(L, t, "index", 0); SetNum(L, t, "num", 1);
			SetNum(L, t, "id", o.Id); SetNum(L, t, "effect_id", o.Id * 100L + c.EffectIndex);
			SetNum(L, t, "frame_s", o.Start); SetNum(L, t, "frame_e", o.End);
			SetNum(L, t, "originframe", Math.Floor(c.Now)); SetNum(L, t, "scene_id", c.Scene.Index);
			if (!section.File.Legacy) return;
			// the older scripts read their numbered settings off the object
			for (int i = 0; i < 4; i++) {
				var s = section.Track(i);
				string? key = s == null ? null : FindKey(c.Effect, s);
				SetNum(L, t, "track" + i.ToString(CultureInfo.InvariantCulture), s == null ? 0 : key != null ? c.V(key, s.Number) : s.Number);
			}
			bool check = false;
			foreach (var s in section.Settings) {
				if (s.Kind != Aup2SettingKind.Check || s.Slot != 0) continue;
				string? key = FindKey(c.Effect, s);
				check = key != null ? Truth(c.Effect.Get(key) ?? "") : s.Number != 0;
			}
			L.PushBoolean(check);
			L.SetField(t, "check0");
		}
		#endregion

		#region Arguments
		private static double N(Lua L, int i, double fallback = 0) {
			switch (L.Type(i)) {
				case LuaType.Number: {
					double v = L.ToNumber(i);
					return double.IsNaN(v) ? fallback : v;
				}
				case LuaType.String: return L.ToNumberX(i) ?? fallback;
				case LuaType.Boolean: return L.ToBoolean(i) ? 1 : 0;
				default: return fallback;
			}
		}

		private static int I(Lua L, int i, int fallback = 0) => (int)Math.Clamp(Math.Floor(N(L, i, fallback)), int.MinValue, int.MaxValue);

		private static string? Str(Lua L, int i) => L.Type(i) is LuaType.String or LuaType.Number ? L.ToString(i, false) : null;

		private static bool Flag(Lua L, int i) => L.Type(i) switch {
			LuaType.Boolean => L.ToBoolean(i),
			LuaType.Number => L.ToNumber(i) != 0,
			LuaType.String => (L.ToNumberX(i) ?? 1) != 0,
			_ => false,
		};

		private static bool IsNumber(Lua L, int i) => L.Type(i) == LuaType.Number;

		private static double Field(Lua L, int table, long k, double fallback = 0) {
			L.RawGetInteger(table, k);
			double v = N(L, -1, fallback);
			L.Pop(1);
			return v;
		}
		#endregion

		#region The object's picture
		private byte[]? _pix;                   // the picture read back, straight rgba
		private int _pixW, _pixH;
		private bool _dirty;                    // _pix holds changes the picture does not have yet
		private string _pixType = "col";
		private int _pixBlend = -1;
		// when the picture is a copy of a named buffer: that buffer and its version
		private string? _objTag;
		private readonly Dictionary<string, int> _versions = new();
		private readonly Dictionary<string, (byte[] pix, int w, int h)> _tagPix = new();
		private readonly Dictionary<string, (int w, int h)> _sizes = new();
		private Aup2Scene? _frameScene;
		private double _frameNow = double.NaN;
		private Aup2TextStyle _font = new();
		private int _fontKind;

		private static bool Picture(Aup2Context c) {
			if (c.Surface != null) return true;
			if (c.Host == null || c.Skip) return false;
			if (c.Pixels()) return true;
			return c.Host.Gpu == null && c.Host.Materialize(c) && c.Surface != null;
		}

		private void PictureChanged() {
			_pix = null;
			_objTag = null;
			_dirty = false;
		}

		private void Flush() {
			if (!_dirty || _pix == null) { _dirty = false; return; }
			_dirty = false;
			_objTag = null;
			_c!.Host?.WritePixels(_c, _pix, _pixW, _pixH);
		}

		private bool EnsurePix() {
			if (_pix != null) return true;
			var c = _c!;
			if (c.Host == null) return false;
			if (c.Surface == null && !Picture(c)) return false;
			_pix = c.Host.ReadPixels(c, out _pixW, out _pixH);
			if (_pix == null || _pix.Length < _pixW * _pixH * 4) { _pix = null; return false; }
			if (_objTag != null) _tagPix[_objTag] = (_pix, _pixW, _pixH);
			return true;
		}

		private string Bump(string name) {
			int v = _versions.GetValueOrDefault(name) + 1;
			_versions[name] = v;
			return name + "\u0001" + v.ToString(CultureInfo.InvariantCulture);
		}

		private string Tag(string name) => name + "\u0001" + _versions.GetValueOrDefault(name).ToString(CultureInfo.InvariantCulture);

		private static string Buffer(string name) => name switch { "obj" => "object", "tmp" => "tempbuffer", "frm" => "framebuffer", _ => name };

		// a file a script names: as it is when it is a full path, else beside the script
		private string ResolvePath(string path) {
			if (Path.IsPathRooted(path)) return path;
			string? dir = _section == null || _section.File.Path.Length == 0 ? null : Path.GetDirectoryName(_section.File.Path);
			if (dir == null) return path;
			string beside = Path.Combine(dir, path);
			return File.Exists(beside) ? beside : path;
		}
		#endregion

		#region What the scripts call
		private int Call(int fn, Lua L) {
			var c = _c;
			if (c == null) return 0;
			switch (fn) {
				case 0: return Sync(L);
				case 1: return Size(L);
				case 2: return Draw(L, c);
				case 3: return Poly(L, c);
				case 4: return Load(L, c);
				case 5: return SetFont(L);
				case 6: return GetFont(L);
				case 7: return Rand(L, c);
				case 8: L.PushNumber(Rand01(L, c, 1, 2)); return 1;
				case 9: return SetOption(L, c);
				case 10: return GetOption(L, c);
				case 11: return GetValue(L, c);
				case 12: return GetPixel(L);
				case 13: return PutPixel(L);
				case 14: return CopyPixel(L);
				case 15: return PixelOption(L);
				case 16: return CopyBuffer(L, c);
				case 17: return ClearBuffer(L, c);
				case 18: return Effect(L, c);
				case 19: return GetInfo(L, c);
				case 20: Add(_messages, Str(L, 1) ?? ""); return 0;
				case 21: L.PushString(Aup2ScriptFile.Portable(Str(L, 1) ?? "")); return 1;
				default: return 0;
			}
		}

		private int Sync(Lua L) {
			_ox = N(L, 1); _oy = N(L, 2); _oz = N(L, 3);
			_rx = N(L, 4); _ry = N(L, 5); _rz = N(L, 6);
			_cx = N(L, 7); _cy = N(L, 8); _cz = N(L, 9);
			_zoom = N(L, 10, 1); _alpha = N(L, 11, 1); _aspect = Math.Clamp(N(L, 12), -1, 1);
			_sx = N(L, 13, 1); _sy = N(L, 14, 1);
			Apply(true);
			return 0;
		}

		private int Size(Lua L) {
			var c = _c!;
			bool has = Picture(c);
			L.PushInteger(has ? c.Surface!.W : 0);
			L.PushInteger(has ? c.Surface!.H : 0);
			return 2;
		}

		private int Draw(Lua L, Aup2Context c) {
			Flush();
			if (c.Host == null) return 0;
			c.Host.ScriptDraw(c, N(L, 1), N(L, 2), N(L, 3), N(L, 4, 1), N(L, 5, 1), N(L, 6), N(L, 7), N(L, 8));
			if (c.Flat) Bump("tempbuffer");
			return 0;
		}

		// one face; a corner written twice makes it a triangle
		private void Face(Aup2Context c, ReadOnlySpan<Vector3> corners, ReadOnlySpan<Vector2> uv, double alpha) {
			Span<Vector3> p = stackalloc Vector3[4];
			Span<Vector2> t = stackalloc Vector2[4];
			int n = 0;
			for (int i = 0; i < corners.Length && n < 4; i++) {
				if (n > 0 && corners[i] == p[n - 1]) continue;
				p[n] = corners[i]; t[n] = uv[i]; n++;
			}
			if (n > 3 && p[n - 1] == p[0]) n--;
			if (n < 3) return;
			c.Host!.ScriptPoly(c, p[..n], t[..n], alpha);
			PolygonCount++;
		}

		private int Poly(Lua L, Aup2Context c) {
			Flush();
			if (c.Host == null || !Picture(c)) return 0;
			float w = c.Surface!.W, h = c.Surface.H;
			bool legacy = _section!.File.Legacy;
			// the older scripts add obj.ox to the corners themselves
			if (legacy) { c.Item.X = _bx; c.Item.Y = _by; c.Item.Z = _bz; }
			Span<Vector3> p = stackalloc Vector3[4];
			Span<Vector2> t = stackalloc Vector2[4];
			try {
				if (L.Type(1) != LuaType.Table) {
					if (L.GetTop() < 12) return 0;
					for (int i = 0; i < 4; i++) p[i] = new Vector3((float)N(L, 1 + 3 * i), (float)N(L, 2 + 3 * i), (float)N(L, 3 + 3 * i));
					t[0] = new Vector2(0, 0); t[1] = new Vector2(w, 0); t[2] = new Vector2(w, h); t[3] = new Vector2(0, h);
					if (L.GetTop() >= 20)
						for (int i = 0; i < 4; i++) {
							// places outside the picture are brought back to its edge
							t[i] = new Vector2((float)Math.Clamp(N(L, 13 + 2 * i, t[i].X), 0, w), (float)Math.Clamp(N(L, 14 + 2 * i, t[i].Y), 0, h));
						}
					Face(c, p, t, N(L, 21, 1));
					return 0;
				}
				int count = L.RawLen(1);
				if (count == 0) return 0;
				L.RawGetInteger(1, 1);
				int width = L.Type(-1) == LuaType.Table ? L.RawLen(-1) : 0;
				L.Pop(1);
				if (width == 20 || width == 32) {
					// a list of faces, places in pixels
					double alpha = N(L, 2, 1);
					for (int k = 1; k <= count; k++) {
						L.RawGetInteger(1, k);
						int e = L.GetTop();
						if (L.Type(e) == LuaType.Table) {
							for (int i = 0; i < 4; i++) {
								p[i] = new Vector3((float)Field(L, e, 1 + 3 * i), (float)Field(L, e, 2 + 3 * i), (float)Field(L, e, 3 + 3 * i));
								t[i] = new Vector2((float)Field(L, e, 13 + 2 * i), (float)Field(L, e, 14 + 2 * i));
							}
							Face(c, p, t, alpha);
						}
						L.Pop(1);
					}
				} else if (width == 5 || width == 8) {
					// a list of corners, places from 0 to 1
					int per = I(L, 2, 4) == 3 ? 3 : 4;
					double alpha = N(L, 3, 1);
					for (int k = 1; k + per - 1 <= count; k += per) {
						for (int i = 0; i < per; i++) {
							L.RawGetInteger(1, k + i);
							int e = L.GetTop();
							if (L.Type(e) == LuaType.Table) {
								p[i] = new Vector3((float)Field(L, e, 1), (float)Field(L, e, 2), (float)Field(L, e, 3));
								t[i] = new Vector2((float)Field(L, e, 4) * w, (float)Field(L, e, 5) * h);
							}
							L.Pop(1);
						}
						Face(c, p[..per], t[..per], alpha);
					}
				} else if (width == 7 || width == 10 || width == 28 || width == 40) {
					Note("obj.drawpoly: faces with their own colours are not drawn");
				}
				return 0;
			} finally {
				if (legacy) Apply(true);
				if (c.Flat) Bump("tempbuffer");
			}
		}

		private void Note(string text) {
			if (!_messages.Contains(text)) Add(_messages, text);
		}

		private static readonly HashSet<string> LoadKinds = new() {
			"figure", "text", "image", "framebuffer", "tempbuffer", "movie", "movie.frame", "movie.info", "layer", "before", "text.layout", "textlayout",
		};
		private static readonly string[] Decorations = { "標準文字", "影付き文字", "影付き文字(薄)", "縁取り文字", "縁取り文字(細)", "縁取り文字(太)", "縁取り文字(角)" };

		private static string AlignName(int align) {
			if (align < 0 || align > 17) return "左寄せ[上]";
			if (align < 9) return new[] { "左寄せ", "中央揃え", "右寄せ" }[align % 3] + new[] { "[上]", "[中]", "[下]" }[align / 3];
			align -= 9;
			return "縦書 " + new[] { "上寄", "中央", "下寄" }[align % 3] + new[] { "[右]", "[中]", "[左]" }[align / 3];
		}

		// cuts the picture down to a part of itself
		private void Crop(Lua L, Aup2Context c, int at) {
			if (!IsNumber(L, at) || !IsNumber(L, at + 3) || !Picture(c)) return;
			double x = N(L, at), y = N(L, at + 1), w = N(L, at + 2), h = N(L, at + 3);
			var inv = CultureInfo.InvariantCulture;
			c.RunEffect("クリッピング", new KeyValuePair<string, string>[] {
				new("上", Math.Max(0, y).ToString(inv)), new("下", Math.Max(0, c.Surface!.H - y - h).ToString(inv)),
				new("左", Math.Max(0, x).ToString(inv)), new("右", Math.Max(0, c.Surface.W - x - w).ToString(inv)),
				new("中心の位置を変更", "1"),
			});
		}

		private int Load(Lua L, Aup2Context c) {
			string? kind = Str(L, 1);
			int a = 2;
			if (kind == null) { L.PushBoolean(false); return 1; }
			if (!LoadKinds.Contains(kind)) {
				// no kind given: told from what the first argument looks like
				a = 1;
				string ext = kind.Length < 260 && kind.IndexOf('\n') < 0 ? Path.GetExtension(kind).ToLowerInvariant() : "";
				kind = ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp" ? "image"
					: ext is ".avi" or ".mp4" or ".mov" or ".mkv" or ".webm" ? "movie"
					: Aup2Shapes.Knows(kind) ? "figure" : "text";
			}
			Flush();
			bool ok = false;
			switch (kind) {
				case "figure": {
					string shape = Str(L, a) ?? "";
					int rgb = (int)((long)N(L, a + 1, 0xffffff) & 0xffffff);
					double size = Math.Max(0, N(L, a + 2, 100)), line = Math.Max(0, N(L, a + 3));
					if (c.Host == null || !c.Host.LoadFigure(c, shape, rgb, size, line)) break;
					// the same figure has the same pixels: read back once a frame at most
					PictureChanged();
					_objTag = string.Join("\u0001", "figure", shape, rgb.ToString(CultureInfo.InvariantCulture), size.ToString("R", CultureInfo.InvariantCulture), line.ToString("R", CultureInfo.InvariantCulture));
					if (_tagPix.TryGetValue(_objTag, out var known)) (_pix, _pixW, _pixH) = known;
					L.PushBoolean(true);
					return 1;
				}
				case "text": {
					var f = _font;
					int align = IsNumber(L, a + 3) ? I(L, a + 3) : -1;
					var style = new Aup2TextStyle {
						Text = Str(L, a) ?? "", Font = f.Font, Size = f.Size, Spacing = f.Spacing, Leading = f.Leading,
						R = f.R, G = f.G, B = f.B, ER = f.ER, EG = f.EG, EB = f.EB, Bold = f.Bold, Italic = f.Italic,
						Decoration = Decorations[Math.Clamp(_fontKind, 0, Decorations.Length - 1)], Align = AlignName(align),
						Speed = N(L, a + 1), Time = IsNumber(L, a + 2) ? N(L, a + 2) : c.Time,
					};
					ok = c.Host != null && c.Host.LoadText(c, style);
					break;
				}
				case "image": {
					string? path = Str(L, a);
					ok = c.Host != null && !string.IsNullOrEmpty(path) && c.Host.LoadImage(c, ResolvePath(path));
					break;
				}
				case "framebuffer":
				case "tempbuffer":
					ok = c.Host != null && c.Host.CopyBuffer(c, "object", kind);
					if (ok) Crop(L, c, a);
					break;
				case "movie":
				case "movie.frame":
					Note("obj.load: \"" + kind + "\" is not available");
					L.PushInteger(0);
					return 1;
				case "movie.info":
					L.PushInteger(0); L.PushInteger(0); L.PushInteger(0);
					return 3;
				case "text.layout":
				case "textlayout":
					L.PushInteger(0); L.PushInteger(0);
					return 2;
				default:
					Note("obj.load: \"" + kind + "\" is not available");
					break;
			}
			if (ok) PictureChanged();
			L.PushBoolean(ok);
			return 1;
		}

		private static void Rgb(double colour, out int r, out int g, out int b) {
			long v = (long)colour & 0xffffff;
			r = (int)(v >> 16) & 255; g = (int)(v >> 8) & 255; b = (int)v & 255;
		}

		private int SetFont(Lua L) {
			var f = _font = new Aup2TextStyle { Font = Str(L, 1) ?? "", Size = Math.Clamp(N(L, 2, 34), 1, 4096) };
			_fontKind = I(L, 3);
			Rgb(N(L, 4, 0xffffff), out f.R, out f.G, out f.B);
			Rgb(N(L, 5, 0), out f.ER, out f.EG, out f.EB);
			f.Bold = Flag(L, 6); f.Italic = Flag(L, 7);
			f.Spacing = N(L, 8); f.Leading = N(L, 9);
			return 0;
		}

		private int GetFont(Lua L) {
			var f = _font;
			L.PushString(f.Font); PushNum(L, f.Size); L.PushInteger(_fontKind);
			L.PushInteger((f.R << 16) | (f.G << 8) | f.B); L.PushInteger((f.ER << 16) | (f.EG << 8) | f.EB);
			L.PushBoolean(f.Bold); L.PushBoolean(f.Italic); PushNum(L, f.Spacing); PushNum(L, f.Leading);
			return 9;
		}

		// the same number for the same object, seed and frame; a seed under zero is shared by every object
		private double Rand01(Lua L, Aup2Context c, int seedAt, int frameAt) {
			bool seeded = IsNumber(L, seedAt);
			long seed = seeded ? (long)N(L, seedAt) : 0;
			long frame = IsNumber(L, frameAt) ? (long)N(L, frameAt) : (long)Math.Floor(c.Frame);
			long own = seeded && seed < 0 ? 0 : c.Object.Id * 7919L + 1;
			unchecked {
				ulong x = (ulong)frame * 0x9E3779B97F4A7C15UL + (ulong)seed * 0xC2B2AE3D27D4EB4FUL + (ulong)own * 0x165667B19E3779F9UL + 0x27D4EB2F165667C5UL;
				x ^= x >> 31; x *= 0xD6E8FEB86659FD93UL; x ^= x >> 32; x *= 0xD6E8FEB86659FD93UL; x ^= x >> 29;
				return (x >> 11) / (double)(1UL << 53);
			}
		}

		private int Rand(Lua L, Aup2Context c) {
			double a = Math.Floor(N(L, 1)), b = Math.Floor(N(L, 2));
			if (b < a) (a, b) = (b, a);
			double r = Rand01(L, c, 3, 4);
			PushNum(L, Math.Min(b, a + Math.Floor(r * (b - a + 1))));
			return 1;
		}

		private static readonly string[] BlendNames = { "none", "add", "sub", "mul", "screen", "overlay", "light", "dark", "brightness", "chroma", "shadow", "light_dark", "diff" };
		private static readonly Aup2Blend[] Blends = {
			Aup2Blend.Normal, Aup2Blend.Add, Aup2Blend.Subtract, Aup2Blend.Multiply, Aup2Blend.Screen, Aup2Blend.Overlay, Aup2Blend.Lighten,
			Aup2Blend.Darken, Aup2Blend.Luminance, Aup2Blend.ColorDifference, Aup2Blend.Shade, Aup2Blend.LightDark, Aup2Blend.Difference,
		};

		private int SetOption(Lua L, Aup2Context c) {
			switch (Str(L, 1)) {
				case "culling": c.CullBack = Flag(L, 2); break;
				case "billboard": c.Item.Billboard = N(L, 2) != 0; break;
				case "blend": {
					int at = IsNumber(L, 2) ? I(L, 2) : Array.IndexOf(BlendNames, Str(L, 2) ?? "none");
					// the modes made for the temporary buffer alone draw as the plain one
					c.Item.Blend = at >= 0 && at < Blends.Length ? Blends[at] : Aup2Blend.Normal;
					break;
				}
				case "drawtarget": {
					bool temp = Buffer(Str(L, 2) ?? "") == "tempbuffer";
					int w = temp ? Math.Clamp(I(L, 3), 0, Aup2Gpu.MaxSide) : 0, h = temp ? Math.Clamp(I(L, 4), 0, Aup2Gpu.MaxSide) : 0;
					if (c.Host != null) c.Host.SetDrawTarget(c, temp, w, h);
					else c.Flat = temp;
					if (w > 0 && h > 0) { Bump("tempbuffer"); _sizes["tempbuffer"] = (w, h); }
					break;
				}
				case "draw_state": c.Drawn = Flag(L, 2); break;
			}
			return 0;
		}

		private Aup2ScriptSetting? SettingOf(Lua L, int at) {
			if (IsNumber(L, at)) return _section!.Track(I(L, at));
			string? name = Str(L, at);
			return name == null ? null : _section!.Find(name);
		}

		private int GetOption(Lua L, Aup2Context c) {
			switch (Str(L, 1)) {
				case "track_mode": {
					var s = SettingOf(L, 2);
					string? key = s == null ? null : FindKey(c.Effect, s);
					var track = key == null ? null : c.Effect.Track(key);
					if (track == null || !track.IsAnimated) L.PushInteger(0); else L.PushString(track.MotionName);
					return 1;
				}
				case "section_num": L.PushInteger(c.Object.Frames.Length - 1); return 1;
				case "script_name": {
					int at = c.EffectIndex + I(L, 2);
					string name = at >= 0 && at < c.Object.Effects.Count ? c.Object.Effects[at].Name : "";
					L.PushString(_byName.ContainsKey(name) ? name : "");
					return 1;
				}
				case "gui": L.PushBoolean(false); return 1;
				case "group_info": L.PushInteger(0); return 1;
				case "camera_mode": L.PushInteger(c.Item.UnderCamera ? 1 : 0); return 1;
				case "camera_param": {
					var cam = c.Host?.CurrentCamera;
					var eye = cam?.Eye ?? new Vector3(0, 0, -1024);
					var target = cam?.Target ?? Vector3.Zero;
					double d = cam?.ScreenDistance(c.FrameH) ?? 1024;
					PushNum(L, eye.X); PushNum(L, eye.Y); PushNum(L, eye.Z);
					PushNum(L, target.X); PushNum(L, target.Y); PushNum(L, target.Z);
					PushNum(L, cam?.Roll ?? 0);
					L.PushInteger(0); L.PushInteger(-1); L.PushInteger(0);
					PushNum(L, d);
					return 11;
				}
				case "multi_object": L.PushBoolean(c.Item.Parts != null); return 1;
				case "blend": {
					int at = Array.IndexOf(Blends, c.Item.Blend);
					L.PushString(BlendNames[Math.Max(0, at)]);
					return 1;
				}
				case "culling": L.PushInteger(c.CullBack ? 1 : 0); return 1;
				case "billboard": L.PushInteger(c.Item.Billboard ? 3 : 0); return 1;
				case "drawtarget": L.PushString(c.Flat ? "tempbuffer" : "framebuffer"); return 1;
				case "draw_state": L.PushBoolean(c.Drawn); return 1;
				case "enable_group":
				case "enable_camera": L.PushBoolean(true); return 1;
				case "clipping_object":
				case "clipping_upper_object": L.PushBoolean(false); return 1;
				default: return 0;
			}
		}

		// a setting of an effect at a frame of its object, as the evaluator would work it out
		private static double At(Aup2Object o, Aup2Effect e, string key, double fallback, double frame)
			=> e.Track(key, fallback).Evaluate(frame, o.Frames, o.Id * 131 + key.GetHashCode(StringComparison.Ordinal));

		// the frame (from the object's first) that `time` seconds after one of its points is
		private double FrameOf(Lua L, Aup2Context c, Aup2Object o, int timeAt) {
			if (!IsNumber(L, timeAt)) return c.Now - o.Start;
			int point = I(L, timeAt + 1);
			if (point < 0) point = o.Frames.Length - 1;
			point = Math.Clamp(point, 0, o.Frames.Length - 1);
			return N(L, timeAt) * c.Scene.Rate + o.Frames[point] - o.Frames[0];
		}

		private static Aup2Object? OnLayer(Aup2Context c, int layer) {
			foreach (var o in c.Scene.Objects)
				if (o.Layer == layer - 1 && c.Now >= o.Start && c.Now < o.End + 1) return o;
			return null;
		}

		private static readonly Dictionary<string, (string key, double fallback)> DrawKeys = new() {
			["x"] = ("X", 0), ["y"] = ("Y", 0), ["z"] = ("Z", 0), ["rx"] = ("X軸回転", 0), ["ry"] = ("Y軸回転", 0), ["rz"] = ("Z軸回転", 0),
			["cx"] = ("中心X", 0), ["cy"] = ("中心Y", 0), ["cz"] = ("中心Z", 0), ["zoom"] = ("拡大率", 100), ["aspect"] = ("縦横比", 0), ["alpha"] = ("透明度", 0),
		};

		// one of the object's own values; null when there is no such name
		private double? Own(Lua L, Aup2Context c, Aup2Object o, string name, int timeAt) {
			bool self = ReferenceEquals(o, c.Object) && !IsNumber(L, timeAt);
			switch (name) {
				case "time": return self ? c.Time : FrameOf(L, c, o, timeAt) / c.Scene.Rate;
				case "frame_s": return o.Start;
				case "frame_e": return o.End;
				case "scenechange": return c.Item.ChangeProgress;
				case "sz": return 1;
			}
			if (self) {
				double big = Math.Max(Math.Abs(_bsx), Math.Abs(_bsy));
				switch (name) {
					case "x": return _bx;
					case "y": return _by;
					case "z": return _bz;
					case "rx": return _brx;
					case "ry": return _bry;
					case "rz": return _brz;
					case "cx": return _bcx;
					case "cy": return _bcy;
					case "cz": return _bcz;
					case "sx": return _bsx;
					case "sy": return _bsy;
					case "zoom": return big * 100;
					case "aspect": return big == 0 ? 0 : Math.Abs(_bsx) < Math.Abs(_bsy) ? 1 - Math.Abs(_bsx) / big : -(1 - Math.Abs(_bsy) / big);
					case "alpha": return _bop;
				}
				return null;
			}
			var draw = o.Find(Aup2Evaluator.StandardDraw);
			double frame = FrameOf(L, c, o, timeAt);
			double V(string n) => draw == null ? DrawKeys[n].fallback : At(o, draw, DrawKeys[n].key, DrawKeys[n].fallback, frame);
			switch (name) {
				case "sx": return V("zoom") / 100 * (V("aspect") > 0 ? 1 - V("aspect") / 100 : 1);
				case "sy": return V("zoom") / 100 * (V("aspect") < 0 ? 1 + V("aspect") / 100 : 1);
				case "aspect": return V("aspect") / 100;
				case "alpha": return 1 - Math.Clamp(V("alpha") / 100, 0, 1);
			}
			return DrawKeys.ContainsKey(name) ? V(name) : null;
		}

		private int PushOwn(Lua L, Aup2Context c, Aup2Object o, string name, int timeAt) {
			string[]? three = name switch {
				"pos" => new[] { "x", "y", "z" }, "angle" => new[] { "rx", "ry", "rz" },
				"center" => new[] { "cx", "cy", "cz" }, "scale" => new[] { "sx", "sy", "sz" }, _ => null,
			};
			if (three != null) {
				foreach (string n in three) PushNum(L, Own(L, c, o, n, timeAt) ?? 0);
				return 3;
			}
			double? v = Own(L, c, o, name, timeAt);
			if (v == null) return 0;
			PushNum(L, v.Value);
			return 1;
		}

		// a setting of one of an object's effects ("name" or "name:n"), by the name it has in a project
		private int PushEffectValue(Lua L, Aup2Context c, Aup2Object? o, string effect, string item, int timeAt) {
			if (o == null) return 0;
			int nth = 0, colon = effect.LastIndexOf(':');
			if (colon > 0 && int.TryParse(effect[(colon + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)) { nth = n; effect = effect[..colon]; }
			Aup2Effect? found = null;
			foreach (var e in o.Effects) {
				if (e.Name != effect) continue;
				if (nth-- == 0) { found = e; break; }
			}
			string? text = found?.Get(item);
			if (found == null || text == null) return 0;
			if (found.Track(item).Text != null) L.PushString(text);
			else PushNum(L, At(o, found, item, 0, FrameOf(L, c, o, timeAt)));
			return 1;
		}

		private int PushTrack(Lua L, Aup2Context c, Aup2ScriptSetting? s, int timeAt) {
			if (s == null) { L.PushInteger(0); return 1; }
			string? key = FindKey(c.Effect, s);
			PushNum(L, key == null ? s.Number : IsNumber(L, timeAt) ? At(c.Object, c.Effect, key, s.Number, FrameOf(L, c, c.Object, timeAt)) : c.V(key, s.Number));
			return 1;
		}

		private int GetValue(Lua L, Aup2Context c) {
			bool second = L.Type(2) == LuaType.String;
			if (IsNumber(L, 1)) {
				if (second && L.Type(3) == LuaType.String) return PushEffectValue(L, c, OnLayer(c, I(L, 1)), Str(L, 2)!, Str(L, 3)!, 4);
				return PushTrack(L, c, _section!.Track(I(L, 1)), 2);
			}
			string? target = Str(L, 1);
			if (target == null) return 0;
			if (second) return PushEffectValue(L, c, c.Object, target, Str(L, 2)!, 3);
			if (target.StartsWith("track.", StringComparison.Ordinal)) return PushTrack(L, c, _section!.Find(target[6..]), 2);
			if (target.StartsWith("layer", StringComparison.Ordinal)) {
				// "layer7" tells whether there is an object, "layer7.x" gives one of its values
				int i = 5;
				while (i < target.Length && char.IsAsciiDigit(target[i])) i++;
				if (!int.TryParse(target.AsSpan(5, i - 5), NumberStyles.Integer, CultureInfo.InvariantCulture, out int layer)) return 0;
				// a layer number that went through a division reads "7.0"
				if (i + 1 < target.Length && target[i] == '.' && target[i + 1] == '0' && (i + 2 == target.Length || target[i + 2] == '.')) i += 2;
				var other = OnLayer(c, layer);
				if (i >= target.Length) { L.PushBoolean(other != null); return 1; }
				return other == null ? 0 : PushOwn(L, c, other, target[(i + 1)..], 2);
			}
			return PushOwn(L, c, c.Object, target, 2);
		}

		private int GetPixel(Lua L) {
			if (L.GetTop() == 0) return Size(L);
			string type = Str(L, 3) ?? _pixType;
			int x = I(L, 1), y = I(L, 2);
			int r = 0, g = 0, b = 0, a = 0;
			if (EnsurePix() && x >= 0 && y >= 0 && x < _pixW && y < _pixH) {
				int at = (y * _pixW + x) * 4;
				r = _pix![at]; g = _pix[at + 1]; b = _pix[at + 2]; a = _pix[at + 3];
			}
			if (type == "rgb") {
				L.PushInteger(r); L.PushInteger(g); L.PushInteger(b); L.PushInteger(a);
				return 4;
			}
			if (type == "yc") {
				PushNum(L, Math.Round(16 * (0.299 * r + 0.587 * g + 0.114 * b)));
				PushNum(L, Math.Round(16 * (-0.169 * r - 0.331 * g + 0.5 * b)));
				PushNum(L, Math.Round(16 * (0.5 * r - 0.419 * g - 0.081 * b)));
				PushNum(L, Math.Round(a * 4096 / 255.0));
				return 4;
			}
			L.PushInteger((r << 16) | (g << 8) | b);
			L.PushNumber(a / 255.0);
			return 2;
		}

		private static byte Byte(double v) => (byte)Math.Clamp(Math.Round(v), 0, 255);

		private int PutPixel(Lua L) {
			if (L.GetTop() == 0) return Size(L);
			int x = I(L, 1), y = I(L, 2);
			if (!EnsurePix() || x < 0 || y < 0 || x >= _pixW || y >= _pixH) return 0;
			double r, g, b, a;
			if (_pixType == "rgb") {
				r = N(L, 3); g = N(L, 4); b = N(L, 5); a = N(L, 6, 255) / 255;
			} else if (_pixType == "yc") {
				double yy = N(L, 3) / 16, cb = N(L, 4) / 16, cr = N(L, 5) / 16;
				r = yy + 1.402 * cr; g = yy - 0.344 * cb - 0.714 * cr; b = yy + 1.772 * cb; a = N(L, 6, 4096) / 4096;
			} else {
				Rgb(N(L, 3), out int ri, out int gi, out int bi);
				r = ri; g = gi; b = bi; a = N(L, 4, 1);
			}
			a = Math.Clamp(a, 0, 1);
			int at = (y * _pixW + x) * 4;
			var p = _pix!;
			if (_pixBlend >= 0) {
				// over what is there: plain, added, taken away, multiplied
				double da = p[at + 3] / 255.0, dr = p[at], dg = p[at + 1], db = p[at + 2];
				switch (_pixBlend) {
					case 1: r = dr + r * a; g = dg + g * a; b = db + b * a; break;
					case 2: r = dr - r * a; g = dg - g * a; b = db - b * a; break;
					case 3: r = dr * (1 - a + a * r / 255); g = dg * (1 - a + a * g / 255); b = db * (1 - a + a * b / 255); break;
					default: {
						double oa = a + da * (1 - a);
						if (oa > 0) { r = (r * a + dr * da * (1 - a)) / oa; g = (g * a + dg * da * (1 - a)) / oa; b = (b * a + db * da * (1 - a)) / oa; }
						da = oa;
						break;
					}
				}
				a = Math.Max(a, da);
			}
			p[at] = Byte(r); p[at + 1] = Byte(g); p[at + 2] = Byte(b); p[at + 3] = Byte(a * 255);
			_dirty = true;
			return 0;
		}

		private int CopyPixel(Lua L) {
			int dx = I(L, 1), dy = I(L, 2), sx = I(L, 3), sy = I(L, 4);
			if (!EnsurePix() || dx < 0 || dy < 0 || sx < 0 || sy < 0 || dx >= _pixW || sx >= _pixW || dy >= _pixH || sy >= _pixH) return 0;
			System.Buffer.BlockCopy(_pix!, (sy * _pixW + sx) * 4, _pix!, (dy * _pixW + dx) * 4, 4);
			_dirty = true;
			return 0;
		}

		private int PixelOption(Lua L) {
			switch (Str(L, 1)) {
				case "type": {
					string? t = Str(L, 2);
					if (t is "col" or "rgb" or "yc") _pixType = t;
					break;
				}
				case "blend": _pixBlend = IsNumber(L, 2) ? Math.Clamp(I(L, 2), 0, 3) : -1; break;
				case "get":
					// asking again reads the picture again
					Flush();
					_pix = null;
					if (Buffer(Str(L, 2) ?? "") == "framebuffer") Note("obj.pixeloption: only the object's own picture is read and written");
					break;
				case "put":
					if (Buffer(Str(L, 2) ?? "") == "framebuffer") Note("obj.pixeloption: only the object's own picture is read and written");
					break;
			}
			return 0;
		}

		private int CopyBuffer(Lua L, Aup2Context c) {
			string dst = Buffer(Str(L, 1) ?? ""), src = Buffer(Str(L, 2) ?? "");
			Flush();
			bool ok = false;
			if (c.Host != null && dst.Length > 0 && src.Length > 0) {
				string from = src.StartsWith("image:", StringComparison.Ordinal) ? "image:" + ResolvePath(src[6..]) : src;
				ok = c.Host.CopyBuffer(c, dst, from);
			}
			if (ok) {
				bool named = src == "tempbuffer" || src.StartsWith("cache:", StringComparison.Ordinal);
				if (dst == "object") {
					// the picture is now what the buffer holds: pixels read from that buffer before still stand
					PictureChanged();
					if (named) {
						_objTag = Tag(src);
						if (_tagPix.TryGetValue(_objTag, out var known)) (_pix, _pixW, _pixH) = known;
					}
				} else if (dst != "framebuffer") {
					string tag = Bump(dst);
					if (src == "object") {
						_objTag = tag;
						if (_pix != null) _tagPix[tag] = (_pix, _pixW, _pixH);
						if (c.Surface != null) _sizes[dst] = (c.Surface.W, c.Surface.H);
					} else if (_sizes.TryGetValue(src, out var size)) _sizes[dst] = size;
					else _sizes.Remove(dst);
				}
			}
			L.PushBoolean(ok);
			return 1;
		}

		private const string KeepBuffer = "cache:\u0001keep";

		// clearbuffer(target[,color]) and clearbuffer(target,w,h[,color])
		private int ClearBuffer(Lua L, Aup2Context c) {
			string target = Buffer(Str(L, 1) ?? "");
			int given = 0;
			while (given < 3 && IsNumber(L, 2 + given)) given++;
			bool sized = given >= 2, coloured = given == 1 || given == 3;
			int w = sized ? I(L, 2) : 0, h = sized ? I(L, 3) : 0;
			bool ok = false;
			if (c.Host != null && (target == "object" || target == "tempbuffer" || target.StartsWith("cache:", StringComparison.Ordinal))) {
				if (!sized) {
					if (target == "object") { if (Picture(c)) { w = c.Surface!.W; h = c.Surface.H; } }
					else if (_sizes.TryGetValue(target, out var size)) (w, h) = size;
				}
				if (w > 0 && h > 0) {
					w = Math.Min(w, Aup2Gpu.MaxSide); h = Math.Min(h, Aup2Gpu.MaxSide);
					var bytes = new byte[w * h * 4];
					if (coloured) {
						Rgb(N(L, sized ? 4 : 2), out int r, out int g, out int b);
						for (int i = 0; i < bytes.Length; i += 4) { bytes[i] = (byte)r; bytes[i + 1] = (byte)g; bytes[i + 2] = (byte)b; bytes[i + 3] = 255; }
					}
					Flush();
					if (target == "object") {
						c.Host.WritePixels(c, bytes, w, h);
						PictureChanged();
						(_pix, _pixW, _pixH) = (bytes, w, h);
						ok = true;
					} else if (target == "tempbuffer" && !coloured) {
						bool flat = c.Flat;
						c.Host.SetDrawTarget(c, true, w, h);
						c.Flat = flat;
						ok = true;
					} else {
						// through the object's picture, which is put back after
						bool had = Picture(c) && c.Host.CopyBuffer(c, KeepBuffer, "object");
						c.Host.WritePixels(c, bytes, w, h);
						ok = c.Host.CopyBuffer(c, target, "object");
						if (had) c.Host.CopyBuffer(c, "object", KeepBuffer);
					}
					if (ok && target != "object") { Bump(target); _sizes[target] = (w, h); }
				}
			}
			L.PushBoolean(ok);
			return 1;
		}

		private int Effect(Lua L, Aup2Context c) {
			string? name = Str(L, 1);
			if (string.IsNullOrEmpty(name)) return 0;
			var settings = new List<KeyValuePair<string, string>>();
			for (int i = 2; i + 1 <= L.GetTop(); i += 2) {
				string? key = Str(L, i);
				if (key == null) continue;
				string value = L.Type(i + 1) switch {
					LuaType.Boolean => L.ToBoolean(i + 1) ? "1" : "0",
					LuaType.Number => L.ToNumber(i + 1).ToString("0.######", CultureInfo.InvariantCulture),
					_ => Str(L, i + 1) ?? "",
				};
				settings.Add(new(key, value));
			}
			Flush();
			if (!c.RunEffect(name, settings)) { Note("obj.effect: no effect named \"" + name + "\""); return 0; }
			PictureChanged();
			Rebase(false);
			return 0;
		}

		private int GetInfo(Lua L, Aup2Context c) {
			switch (Str(L, 1)) {
				case "script_path": {
					string? dir = _section!.File.Path.Length == 0 ? null : Path.GetDirectoryName(_section.File.Path);
					L.PushString(dir == null ? "" : dir + Path.DirectorySeparatorChar);
					return 1;
				}
				case "filter": L.PushBoolean(c.Item.Kind == Aup2ItemKind.Filter); return 1;
				case "origin_scene_id": L.PushInteger(c.Scene.Index); return 1;
				case "saving": L.PushBoolean(false); return 1;
				case "image_max": L.PushInteger(Aup2Gpu.MaxSide); L.PushInteger(Aup2Gpu.MaxSide); return 2;
				case "frame_max": L.PushInteger(Math.Max(0, c.Scene.FrameCount - 1)); return 1;
				case "layer_max": {
					int max = 0;
					foreach (var o in c.Scene.Objects) max = Math.Max(max, o.Layer + 1);
					L.PushInteger(max);
					return 1;
				}
				case "bpm":
					PushNum(L, c.Scene.Section.GetDouble("display.tempo.bpm", 120));
					PushNum(L, c.Scene.Section.GetDouble("display.tempo.beat", 4));
					PushNum(L, c.Scene.Section.GetDouble("display.tempo.offset", 0));
					return 3;
				case "clock": L.PushNumber(Stopwatch.GetElapsedTime(_born).TotalSeconds); return 1;
				case "script_time": L.PushNumber(Stopwatch.GetElapsedTime(_started).TotalMilliseconds); return 1;
				case "version": L.PushInteger(2011100); return 1;
				default: return 0;
			}
		}
		#endregion

		// What the state is made of before any script runs. It receives the host's functions and gives back
		// the function that runs one script.
		private const string Prelude = """
local H = ...
local type, tonumber, pairs, ipairs, select, rawget, rawset, pcall, error, setmetatable = type, tonumber, pairs, ipairs, select, rawget, rawset, pcall, error, setmetatable
local floor, mtype, concat, unpack = math.floor, math.type, table.concat, table.unpack
local rawload, rawtostring = load, tostring
local os_time, os_clock, os_date, os_difftime = os.time, os.clock, os.date, os.difftime

-- only what a script may use stays
do
	local keep = { _G = 1, _VERSION = 1, assert = 1, error = 1, getmetatable = 1, ipairs = 1, next = 1, pairs = 1, pcall = 1, rawequal = 1,
		rawget = 1, rawlen = 1, rawset = 1, select = 1, setmetatable = 1, tonumber = 1, tostring = 1, type = 1, xpcall = 1,
		coroutine = 1, math = 1, string = 1, table = 1, utf8 = 1 }
	local drop = {}
	for k in pairs(_G) do if not keep[k] then drop[#drop + 1] = k end end
	for _, k in ipairs(drop) do _G[k] = nil end
end
string.dump = nil

local function refuse() return nil, "not available" end
os = { time = os_time, clock = os_clock, date = os_date, difftime = os_difftime, getenv = function() return nil end }

-- a whole number out of anything
local function I(x)
	x = tonumber(x) or 0
	if mtype(x) == "float" then
		if x ~= x or x >= 9.2e18 or x <= -9.2e18 then return 0 end
		x = floor(x)
	end
	return x
end

function tostring(v)
	if mtype(v) == "float" and v == floor(v) and v > -1e15 and v < 1e15 then return ("%d"):format(v) end
	return rawtostring(v)
end

function print(...)
	local t = {}
	for i = 1, select("#", ...) do
		local v = (select(i, ...))
		local k = type(v)
		t[i] = (k == "function" or k == "userdata") and "" or tostring(v)
	end
	H.log(concat(t))
end
debug_print = print

function load(chunk, name, mode, ...)
	if type(chunk) == "string" then chunk = H.portable(chunk) end
	if select("#", ...) > 0 then return rawload(chunk, name, "t", (...)) end
	return rawload(chunk, name, "t")
end
function loadstring(chunk, name) return load(chunk, name) end
function loadfile() return nil, "loadfile is not available" end
function dofile() error("dofile is not available", 2) end
function setfenv(f) return f end
function getfenv() return _G end
function collectgarbage(what) return 0 end
_G.unpack = unpack

local function stub()
	return setmetatable({}, { __index = function() return function() end end, __call = function() end })
end

io = setmetatable({}, { __index = function() return refuse end })
package = { path = ".\\?.lua;.\\script\\?.lua", cpath = ".\\?.dll;.\\script\\?.dll", loaded = {}, preload = {} }

-- arithmetic on 32 bits, as LuaJIT's bit library does it
local function b32(x)
	x = I(x) & 0xffffffff
	if x >= 0x80000000 then x = x - 0x100000000 end
	return x
end
bit = {
	tobit = b32,
	bnot = function(a) return b32(~I(a)) end,
	band = function(a, ...) a = I(a) for i = 1, select("#", ...) do a = a & I((select(i, ...))) end return b32(a) end,
	bor = function(a, ...) a = I(a) for i = 1, select("#", ...) do a = a | I((select(i, ...))) end return b32(a) end,
	bxor = function(a, ...) a = I(a) for i = 1, select("#", ...) do a = a ~ I((select(i, ...))) end return b32(a) end,
	lshift = function(a, n) return b32(I(a) << (I(n) & 31)) end,
	rshift = function(a, n) return b32((I(a) & 0xffffffff) >> (I(n) & 31)) end,
	arshift = function(a, n) return b32(b32(a) // (1 << (I(n) & 31))) end,
	tohex = function(a) return ("%08x"):format(I(a) & 0xffffffff) end,
}

local modules = { bit = bit, string = string, table = table, math = math, coroutine = coroutine, rikky_module = stub() }
function require(name)
	local m = modules[name]
	if m ~= nil then return m end
	error("module '" .. tostring(name) .. "' not found", 2)
end

-- what Lua 5.1 had and the scripts count on
do
	local m = math
	local fmod, random, seed = m.fmod, m.random, m.randomseed
	m.pow = function(a, b) return a ^ b end
	m.atan2 = m.atan
	m.fmod = function(a, b) return fmod(a + 0.0, b + 0.0) end
	m.mod = m.fmod
	m.log10 = function(x) return m.log(x, 10) end
	m.ldexp = function(x, e) return x * 2.0 ^ e end
	m.cosh = function(x) return (m.exp(x) + m.exp(-x)) / 2 end
	m.sinh = function(x) return (m.exp(x) - m.exp(-x)) / 2 end
	m.tanh = function(x) local a, b = m.exp(x), m.exp(-x) return (a - b) / (a + b) end
	m.random = function(a, b)
		if a == nil then return random() end
		if b == nil then a = I(a) if a < 1 then return 1 end return random(a) end
		a, b = I(a), I(b)
		if b < a then return a end
		return random(a, b)
	end
	m.randomseed = function(x) seed(I(x)) end

	local t = table
	local insert, remove = t.insert, t.remove
	t.getn = function(x) return #x end
	t.maxn = function(x) local n = 0 for k in pairs(x) do if type(k) == "number" and k > n then n = k end end return n end
	t.foreach = function(x, f) for k, v in pairs(x) do local r = f(k, v) if r ~= nil then return r end end end
	t.foreachi = function(x, f) for i, v in ipairs(x) do local r = f(i, v) if r ~= nil then return r end end end
	t.insert = function(x, a, ...)
		if select("#", ...) == 0 then return insert(x, a) end
		local at = I(a)
		if at < 1 or at > #x + 1 then x[at] = (...) return end
		return insert(x, at, (...))
	end
	t.remove = function(x, at)
		if at == nil then return remove(x) end
		at = I(at)
		local n = #x
		if n == 0 or at < 1 or at > n then local v = x[at] x[at] = nil return v end
		return remove(x, at)
	end

	local s = string
	local sub, rep, byte, char, format = s.sub, s.rep, s.byte, s.char, s.format
	local function opt(x) if x == nil then return nil end return I(x) end
	s.gfind = s.gmatch
	s.sub = function(str, i, j) return sub(str, opt(i) or 1, opt(j) or -1) end
	s.byte = function(str, i, j) i = opt(i) or 1 return byte(str, i, opt(j) or i) end
	s.rep = function(str, n, sep)
		n = I(n)
		if n > 0 and (#tostring(str) + #(sep or "")) * n > 67108864 then error("resulting string too large", 2) end
		if sep ~= nil then return rep(str, n, sep) end
		return rep(str, n)
	end
	s.char = function(...)
		local a = { ... }
		for i = 1, #a do a[i] = I(a[i]) & 255 end
		return char(unpack(a))
	end
	-- %d of a number with a fraction was fine before
	s.format = function(f, ...)
		local a, n, k = { ... }, select("#", ...), 0
		for spec in tostring(f):gmatch("%%[%-+ #0]*%d*%.?%d*([%a%%])") do
			if spec ~= "%" then
				k = k + 1
				if k <= n and mtype(a[k]) == "float" and (spec == "d" or spec == "i" or spec == "u" or spec == "c" or spec == "x" or spec == "X" or spec == "o") then
					a[k] = I(a[k])
				end
			end
		end
		return format(f, unpack(a, 1, n))
	end
end

function OR(a, b) return I(a) | I(b) end
function AND(a, b) return I(a) & I(b) end
function XOR(a, b) return I(a) ~ I(b) end
function SHIFT(a, n)
	a, n = I(a), I(n)
	if n >= 0 then return a << n end
	return a // (1 << -n)
end

local obj = {}
_G.obj = obj
global = {}

local function c8(x) x = I(x) if x < 0 then return 0 elseif x > 255 then return 255 end return x end
local function progress()
	local total = tonumber(rawget(obj, "totaltime")) or 0
	if total <= 0 then return 0 end
	local t = (tonumber(rawget(obj, "time")) or 0) / total
	if t < 0 then return 0 elseif t > 1 then return 1 end
	return t
end

function RGB(r, g, b, r2, g2, b2)
	if g == nil then
		local c = I(r)
		return (c >> 16) & 255, (c >> 8) & 255, c & 255
	end
	if b2 ~= nil then
		local t = progress()
		r, g, b = r + (r2 - r) * t, g + (g2 - g) * t, b + (b2 - b) * t
	end
	return (c8(r) << 16) | (c8(g) << 8) | c8(b)
end

local function hsv2rgb(h, s, v)
	h, s, v = (tonumber(h) or 0) % 360 / 60, (tonumber(s) or 0) / 100, (tonumber(v) or 0) / 100
	if s < 0 then s = 0 elseif s > 1 then s = 1 end
	if v < 0 then v = 0 elseif v > 1 then v = 1 end
	local i = floor(h)
	local f = h - i
	local p, q, t = v * (1 - s), v * (1 - s * f), v * (1 - s * (1 - f))
	local r, g, b
	if i == 0 then r, g, b = v, t, p elseif i == 1 then r, g, b = q, v, p elseif i == 2 then r, g, b = p, v, t
	elseif i == 3 then r, g, b = p, q, v elseif i == 4 then r, g, b = t, p, v else r, g, b = v, p, q end
	return RGB(r * 255 + 0.5, g * 255 + 0.5, b * 255 + 0.5)
end

function HSV(h, s, v, h2, s2, v2)
	if s == nil then
		local r, g, b = RGB(h)
		local max, min = math.max(r, g, b), math.min(r, g, b)
		local d, hue = max - min, 0
		if d > 0 then
			if max == r then hue = (g - b) / d % 6 elseif max == g then hue = (b - r) / d + 2 else hue = (r - g) / d + 4 end
		end
		return floor(hue * 60 + 0.5) % 360, max == 0 and 0 or floor(d / max * 100 + 0.5), floor(max / 255 * 100 + 0.5)
	end
	if v2 ~= nil then
		local t = progress()
		h, s, v = h + (h2 - h) * t, s + (s2 - s) * t, v + (v2 - v) * t
	end
	return hsv2rgb(h, s, v)
end

function rotation(x0, y0, x1, y1, x2, y2, x3, y3, zoom, r)
	zoom, r = tonumber(zoom) or 1, math.rad(tonumber(r) or 0)
	local c, s = math.cos(r) * zoom, math.sin(r) * zoom
	return x0 * c - y0 * s, x0 * s + y0 * c, x1 * c - y1 * s, x1 * s + y1 * c,
		x2 * c - y2 * s, x2 * s + y2 * c, x3 * c - y3 * s, x3 * s + y3 * c
end

-- the picture's size is asked for only when a script reads it
setmetatable(obj, { __index = function(t, k)
	if k == "w" or k == "h" then
		local w, h = H.size()
		rawset(t, "w", w)
		rawset(t, "h", h)
		if k == "w" then return w end
		return h
	end
end })

local function sync()
	H.sync(obj.ox, obj.oy, obj.oz, obj.rx, obj.ry, obj.rz, obj.cx, obj.cy, obj.cz, obj.zoom, obj.alpha, obj.aspect, obj.sx, obj.sy)
end
local function resized()
	rawset(obj, "w", nil)
	rawset(obj, "h", nil)
end

local api = {}
function api.draw(...) sync() H.draw(...) end
function api.drawpoly(...) sync() H.poly(...) end
function api.load(...) sync() resized() return H.load(...) end
function api.effect(name, ...)
	if name == nil then return end
	sync() resized()
	H.effect(name, ...)
end
function api.copybuffer(dst, src) sync() resized() return H.copybuffer(dst, src) end
function api.clearbuffer(...) resized() return H.clearbuffer(...) end
api.setfont, api.getfont = H.setfont, H.getfont
api.rand, api.rand1 = H.rand, H.rand1
api.setoption = H.setoption
api.getvalue = H.getvalue
api.getpixel, api.putpixel, api.copypixel, api.pixeloption = H.getpixel, H.putpixel, H.copypixel, H.pixeloption
function api.getoption(name, ...)
	if name == "camera_param" then
		local x, y, z, tx, ty, tz, rz, ux, uy, uz, d = H.getoption(name)
		return { x = x, y = y, z = z, tx = tx, ty = ty, tz = tz, rz = rz, ux = ux, uy = uy, uz = uz, d = d }
	elseif name == "camera_focus" then
		return { x = 0, y = 0, z = 0, bokeh = 0 }
	end
	return H.getoption(name, ...)
end
function api.getinfo(name, ...)
	if name == "bpm_list" then
		local tempo, beat, offset = H.getinfo("bpm")
		return { { tempo = tempo, beat = beat, start = 0, offset = offset } }
	end
	return H.getinfo(name, ...)
end
function api.mes(text) H.log(tostring(text)) end
function api.interpolation(t, ...)
	local a, n = { ... }, select("#", ...)
	local dims = n >= 12 and 3 or n >= 8 and 2 or 1
	local out = {}
	local t2, t3 = t * t, t * t * t
	for d = 1, dims do
		local p0, p1, p2, p3 = a[d], a[dims + d], a[dims * 2 + d], a[dims * 3 + d]
		out[d] = 0.5 * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (3 * p1 - p0 - 3 * p2 + p3) * t3)
	end
	return unpack(out, 1, dims)
end
-- each call of the function sets the object up, and it is drawn once that way
function api.multiobject(num, func)
	num = I(num)
	if num < 1 or type(func) ~= "function" then return end
	local index, count = rawget(obj, "index"), rawget(obj, "num")
	for i = 0, num - 1 do
		obj.index, obj.num = i, num
		func()
		sync()
		H.draw()
	end
	obj.index, obj.num = index, count
end
-- what cannot be done here answers as if nothing were there
function api.getpixeldata() local w, h = H.size() return nil, w, h end
function api.putpixeldata() return false end
function api.pixelshader() return false end
function api.computeshader() return false end
function api.getaudio(buf) if buf == nil then return 0, 0, {} end return 0, 0 end
function api.module() return stub() end
function api.data() return nil, 0 end
function api.setanchor(name, num) return I(num) end
function api.getpoint() return 0 end

mes, rand, rand1 = api.mes, H.rand, H.rand1

-- settings written as Lua text (a number, a quoted string, a table): compiled once, worked out each run
local compiled, count = {}, 0
local function eval(text)
	if text == "" or text == "nil" then return nil end
	local f = compiled[text]
	if f == nil then
		if count > 2000 then compiled, count = {}, 0 end
		f = rawload("return " .. H.portable(text), "=setting", "t") or false
		compiled[text] = f
		count = count + 1
	end
	if f then
		local ok, v = pcall(f)
		if ok and v ~= nil then return v end
	end
	return text
end

return function(chunk, settings, texts, vars)
	for k, f in pairs(api) do rawset(obj, k, f) end
	for k, v in pairs(vars) do rawset(obj, k, v) end
	resized()
	for k, text in pairs(texts) do settings[k] = eval(text) end
	chunk(settings)
	sync()
end
""";
	}
}
