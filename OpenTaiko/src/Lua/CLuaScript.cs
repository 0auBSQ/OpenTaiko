using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json.Nodes;
using FDK;
using Newtonsoft.Json;
using NLua;

namespace OpenTaiko;

public record struct NamedLuaFunction(string Name, LuaFunction? Func = null) : IDisposable {
	public void Load(Lua? lua) => Func = lua?[Name] as LuaFunction;
	public void LoadNoop(Lua? lua) => Func = lua?.DoString("return function(...) end")?[0] as LuaFunction;
	public void Dispose() {
		Func?.Dispose();
		Func = null;
	}
}

class CLuaScript : IDisposable {

	#region [For the new Lua module methods]

	public HashSet<LuaTexture> TextureList = [];
	public HashSet<LuaCanvas> CanvasList = [];
	public HashSet<Lua3DScene> Scene3DList = [];
	public HashSet<LuaSound> SoundList = [];
	public HashSet<LuaVideo> VideoList = [];
	public HashSet<LuaText> TextList = [];
	public HashSet<LuaGlyphText> GlyphTextList = [];
	public HashSet<LuaGradientMap> GradientList = [];
	//public Dictionary<string, LuaSharedResource<LuaTexture>> SharedTextures = new();
	//public Dictionary<string, LuaSharedResource<LuaSound>> SharedSounds = new();

	public LuaSaveFile? GetLuaSaveFile(int player) {
		if (player < 0 || player > OpenTaiko.MAX_PLAYERS) {
			LogNotification.PopError($"Invalid player index in lua module, expected [0,{OpenTaiko.MAX_PLAYERS}]");
			return null;
		}
		return new LuaSaveFile(OpenTaiko.SaveFileInstances[player], player);
	}

	public LuaSongList RequestSongList(LuaSongListSettings lsls) {
		return new LuaSongList(lsls);
	}

	#endregion

	public static List<CLuaScript> listScripts { get; private set; } = new List<CLuaScript>();
	public static void tReloadLanguage(string lang) {
		foreach (var item in listScripts) {
			item.ReloadLanguage(lang);
		}
	}



	public string strDir { get; private set; }
	public string strScriptPath { get; private set; }
	public string strScriptShort { get; private set; }
	public string strTexturesDir { get; private set; }
	public string strSounsdDir { get; private set; }

	public bool bLoadedAssets { get; private set; }
	public bool bDisposed { get; private set; }
	public bool bCrashed { get; protected set; }

	protected Lua LuaScript { get; private set; }

	private NamedLuaFunction lfLoadAssets = new("loadAssets");
	private NamedLuaFunction lfReloadLanguage = new("reloadLanguage");

	private CLuaInfo luaInfo;
	private CLuaFps luaFPS = new CLuaFps();

	public List<IDisposable> listDisposables { get; private set; } = new List<IDisposable>();

	protected bool Available {
		get {
			return bLoadedAssets && !bDisposed && !bCrashed;
		}
	}

	private double getNum(JsonValue x) {
		return (double)x;
	}

	private string getText(JsonValue x) {
		return (string)x;
	}

	private List<double> getNumArray(JsonArray x) {
		List<double> array = new List<double>();

		foreach (double value in x) {
			array.Add(value);
		}
		return array;
	}

	private List<string> getTextArray(JsonArray x) {
		List<string> array = new List<string>();

		foreach (string value in x) {
			array.Add(value);
		}
		return array;
	}

	protected object[]? RunLuaCode(NamedLuaFunction luaFunction, params object[] args) {
		try {
			if (luaFunction.Func == null) {
				// To prevent lag
				return null;

				Trace.TraceWarning($"{this.GetType().Name} Warning: [{this.strScriptShort}] Function [{luaFunction.Name}] is called but undefined");
				Trace.TraceWarning($"Full script path: {this.strScriptPath}");
				Trace.TraceWarning(new StackTrace(new StackFrame(1, true)).ToString());
				luaFunction.LoadNoop(LuaScript); // silence further warnings
				return null;
			}
			var ret = luaFunction.Func.Call(args);
			// Run ONE incremental GC step per call instead of a full collection. This was previously
			// LuaGC.Collect — a FULL Lua-heap collection on every single Lua call. Per-frame callbacks
			// (stage + background Draw/Update, every character's Update/Draw, nameplates, modals) all go
			// through here, so it meant dozens of full collections per frame, with cost scaling to the
			// Lua heap — a direct cause of irregular gameplay hitches (and audio/visual desync). An
			// incremental step spreads collection cheaply while still keeping the Lua heap bounded.
			LuaScript?.State?.GarbageCollector(KeraLua.LuaGC.Step, 0);
			return ret;
		} catch (Exception exception) {
			var argsAsReprStrings = args.Select(v => JsonConvert.ToString(v));
			Crash(exception, $"RunLuaCode - {luaFunction.Name}({string.Join(", ", argsAsReprStrings)})");
		}
		return null;
	}

	// Run a hook (onStart / activate) as an engine-owned Lua coroutine: a count hook on the thread fires often +
	// cheaply, but only YIELDS once the current resume has run past YieldBudgetMs of wall-clock. So a long load
	// spreads across frames by TIME — not by VM-instruction count, which barely advances during C#-bound scene/
	// texture building (each scene:Obj…/RegisterTexture is ~1 Lua instruction but expensive), the reason a build
	// loop used to run start-to-finish in one resume and freeze the frame. Lua is render-thread-only so it can't
	// move off-thread — but it can yield. Resuming the thread directly (not via NLua's coroutine.resume under a
	// lua_pcall) is what lets the hook's yield propagate back to us.
	public static int YieldCheckInstructions = 2000;    // VM instructions between wall-clock checks (fire often, cheap)
	public static double YieldBudgetMs = 8.0;           // a resume keeps running until this much wall-time elapses
	private static long _resumeStartTs;                 // Stopwatch ts when the current resume began (render-thread only)

	// The skinner LOADING API + the wrapper that runs onStart/activate so queued blocks load + yield around it.
	// LOADING:Add(label?, weight?, fn) records a block; LOADING:Tick(sub) yields a frame inside a heavy loop.
	// All yielding is Lua-side (coroutine.yield) — never from a C# frame (that would cross a C-call boundary).
	private const string LoadingApiLua = @"
LOADING = { _queue = {}, _base = 0, _w = 0, _total = 1 }
function LOADING:Add(a, b, c)
  local label, weight, fn
  if type(a) == 'function' then fn = a
  elseif type(b) == 'function' then label = a; fn = b
  else label = a; weight = b; fn = c end
  self._queue[#self._queue + 1] = { label = label, weight = weight or 1, fn = fn }
end
function LOADING:Tick(sub)
  sub = tonumber(sub) or 0
  if sub < 0 then sub = 0 elseif sub > 1 then sub = 1 end
  coroutine.yield((self._base + sub * self._w) / self._total)
end
function LOADING.__begin()
  LOADING._queue = {}; LOADING._base = 0; LOADING._w = 0; LOADING._total = 1
end
function LOADING.__run()
  local q = LOADING._queue
  local total = 0
  for i = 1, #q do total = total + (q[i].weight or 1) end
  if total <= 0 then total = 1 end
  LOADING._total = total
  local done = 0
  for i = 1, #q do
    local item = q[i]
    LOADING._base = done; LOADING._w = item.weight or 1
    if type(item.fn) == 'function' then item.fn() end
    done = done + (item.weight or 1)
    coroutine.yield(done / total)
  end
  LOADING._queue = {}
end
function __otk_bindload(name)
  return function()
    LOADING.__begin()
    local f = _G[name]
    if type(f) == 'function' then f() end
    LOADING.__run()
  end
end
";

	// Static so the native callback isn't GC-collected. The count-event yield defers to hook-return (no longjmp
	// through this managed frame); the IsYieldable guard avoids "yield across a C-call boundary"; exceptions are
	// swallowed so none crosses back into native Lua.
	private static readonly KeraLua.LuaHookFunction _yieldHook = YieldHook;

	[MonoPInvokeCallback(typeof(KeraLua.LuaHookFunction))]
	private static void YieldHook(IntPtr L, IntPtr ar) {
		try {
			if (Stopwatch.GetElapsedTime(_resumeStartTs).TotalMilliseconds < YieldBudgetMs) return;   // under budget — keep running
			var st = KeraLua.Lua.FromIntPtr(L);
			if (st != null && st.IsYieldable) st.Yield(0);
		} catch { /* never let a managed exception cross back into native Lua */ }
	}

	// The coroutine thread we own + drive for the current hook (onStart). One at a time per script instance.
	private KeraLua.Lua? _hookCo;
	private int _hookThreadRef = -1;

	/// <summary>Begin running a hook function (e.g. onStart) as a coroutine on a dedicated, engine-owned Lua
	/// thread. Call tStepYieldable() repeatedly until it returns false.</summary>
	protected void tBeginYieldable(NamedLuaFunction luaFunction) {
		tEndYieldable();
		var L = LuaScript?.State;
		if (L == null || luaFunction.Func == null) return;   // undefined hook ⇒ no-op (treated as done)
		try {
			var co = L.NewThread();                              // new thread pushed on L's stack + wrapper
			_hookThreadRef = L.Ref(KeraLua.LuaRegistry.Index);   // anchor it (pops it off L) so Lua won't GC it
			// Wrap the target in the LOADING runner: __otk_bindload(name) → fn that runs LOADING.__begin();
			// _G[name](); LOADING.__run(), so any LOADING:Add blocks load + yield around it.
			if (L.GetGlobal("__otk_bindload") == KeraLua.LuaType.Function) {
				L.PushString(luaFunction.Name);
				if (L.PCall(1, 1, 0) != KeraLua.LuaStatus.OK) {
					Trace.TraceError($"{strScriptShort} loader wrap failed: {L.ToString(-1)}");
					L.Pop(1);
					tEndYieldable();
					return;
				}
				L.XMove(co, 1);                                  // wrapped closure onto co's stack
			} else {
				L.Pop(1);                                        // helper missing — run the function directly
				if (L.GetGlobal(luaFunction.Name) != KeraLua.LuaType.Function) {
					L.Pop(1);
					tEndYieldable();
					return;
				}
				L.XMove(co, 1);
			}
			co.SetHook(_yieldHook, KeraLua.LuaHookMask.Count, Math.Max(1, YieldCheckInstructions));
			_hookCo = co;
		} catch (Exception exception) {
			Crash(exception, $"tBeginYieldable({luaFunction.Name})");
			tEndYieldable();
		}
	}

	/// <summary>Resume the hook coroutine one chunk (to the next auto-yield / explicit coroutine.yield, or
	/// completion). Returns true while still running; sets <paramref name="progress"/> (0..1) to the last value
	/// passed to coroutine.yield (0 for an auto-yield).</summary>
	protected bool tStepYieldable(out float progress) {
		progress = 0f;
		var co = _hookCo;
		if (co == null) return false;
		try {
			_resumeStartTs = Stopwatch.GetTimestamp();   // YieldHook yields once this resume runs past YieldBudgetMs
			var status = co.Resume(LuaScript.State, 0, out int nresults);
			LuaScript?.State?.GarbageCollector(KeraLua.LuaGC.Step, 0);
			if (status == KeraLua.LuaStatus.Yield) {
				if (nresults > 0) {
					try { progress = (float)co.ToNumber(-nresults); } catch { /* keep 0 */ }
					co.Pop(nresults);   // drop the yielded values so the next Resume passes nothing back
				}
				return true;
			}
			if (status != KeraLua.LuaStatus.OK) {   // runtime/other error in the hook
				bCrashed = true;
				Trace.TraceError($"{this.strScriptShort} hook error: {co.ToString(-1)}");
			}
			tEndYieldable();
			return false;
		} catch (Exception exception) {
			Crash(exception, $"tStepYieldable({progress})");
			tEndYieldable();
			return false;
		}
	}

	private void tEndYieldable() {
		if (_hookThreadRef != -1) {
			try { LuaScript?.State?.Unref(KeraLua.LuaRegistry.Index, _hookThreadRef); } catch { /* ignore */ }
			_hookThreadRef = -1;
		}
		_hookCo = null;
	}

	private JsonNode LoadConfig(string name) {
		using Stream stream = File.OpenRead($"{strDir}/{name}");
		JsonNode jsonNode = JsonNode.Parse(stream);
		return jsonNode;
	}

	private CTexture LoadTexture(string name) {
		CTexture texture = new CTexture($"{strTexturesDir}/{name}", false);

		listDisposables.Add(texture);
		return texture;
	}

	private void DebugLog(string message) {
		Trace.TraceInformation("<Lua Log>: " + message);
	}

	private CSound LoadSound(string name, string soundGroupName) {
		ESoundGroup soundGroup;
		switch (soundGroupName) {
			case "soundeffect":
				soundGroup = ESoundGroup.SoundEffect;
				break;
			case "voice":
				soundGroup = ESoundGroup.Voice;
				break;
			case "songpreview":
				soundGroup = ESoundGroup.SongPreview;
				break;
			case "songplayback":
				soundGroup = ESoundGroup.SongPlayback;
				break;
			default:
				soundGroup = ESoundGroup.Unknown;
				break;
		}
		CSound sound = OpenTaiko.SoundManager?.tCreateSound($"{strSounsdDir}/{name}", soundGroup);

		listDisposables.Add(sound);
		return sound;
	}

	private CCachedFontRenderer LoadFontRenderer(int size, string fontStyleName) {
		CFontRenderer.FontStyle fontStyle;
		switch (fontStyleName) {
			case "regular":
				fontStyle = CFontRenderer.FontStyle.Regular;
				break;
			case "bold":
				fontStyle = CFontRenderer.FontStyle.Bold;
				break;
			case "italic":
				fontStyle = CFontRenderer.FontStyle.Italic;
				break;
			case "underline":
				fontStyle = CFontRenderer.FontStyle.Underline;
				break;
			case "strikeout":
				fontStyle = CFontRenderer.FontStyle.Strikeout;
				break;
			default:
				fontStyle = CFontRenderer.FontStyle.Regular;
				break;
		}
		CCachedFontRenderer fontRenderer = HPrivateFastFont.tInstantiateMainFont(size, fontStyle);

		listDisposables.Add(fontRenderer);
		return fontRenderer;
	}

	private string GetLocalizedString(string key, params object?[] args) {
		return CLangManager.LangInstance.GetString(key, args);
	}

	private TitleTextureKey CreateTitleTextureKey(string title, CCachedFontRenderer fontRenderer, int maxSize, LuaColor? color = null, LuaColor? edgeColor = null) {
		return new TitleTextureKey(title, fontRenderer,
				Color.FromArgb(color?.A ?? 0xFF, color?.R ?? 0xFF, color?.G ?? 0xFF, color?.B ?? 0xFF),
				Color.FromArgb(edgeColor?.A ?? 0xFF, edgeColor?.R ?? 0x00, edgeColor?.G ?? 0x00, edgeColor?.B ?? 0x00), maxSize);
	}

	private CTexture GetTextTex(TitleTextureKey titleTextureKey, bool vertical, bool keepCenter) {
		return TitleTextureKey.ResolveTitleTexture(titleTextureKey, vertical, keepCenter);
	}

	public bool IsAvailable { get; private set; }

	public CLuaScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true, string fallbackScript = "") {
		strDir = dir;
		strScriptPath = Path.Join(strDir, "Script.lua");
		strScriptShort = Path.Join(Path.GetFileName(Path.GetDirectoryName(this.strScriptPath)), Path.GetFileName(this.strScriptPath));
		strTexturesDir = texturesDir ?? $"{dir}/Textures";
		strSounsdDir = soundsDir ?? $"{dir}/Sounds";

		IsAvailable = true;

		LuaScript = new Lua();
		LuaScript.LoadCLRPackage();
		LuaScript.State.Encoding = Encoding.UTF8;
		LuaSecurity.Secure(LuaScript, dir);

		try {
			LuaScript["info"] = luaInfo = new CLuaInfo(strDir);
			LuaScript["fps"] = luaFPS;

			LuaScript["loadConfig"] = LoadConfig;
			LuaScript["loadTexture"] = LoadTexture;
			LuaScript["loadSound"] = LoadSound;
			LuaScript["loadFontRenderer"] = LoadFontRenderer;
			LuaScript["createTitleTextureKey"] = CreateTitleTextureKey;
			LuaScript["getTextTex"] = GetTextTex;
			LuaScript["getNum"] = getNum;
			LuaScript["getText"] = getText;
			LuaScript["getNumArray"] = getNumArray;
			LuaScript["getTextArray"] = getTextArray;
			LuaScript["getLocalizedString"] = GetLocalizedString;
			LuaScript["debugLog"] = DebugLog;

			// New Lua Module API
			var ltf = new LuaTextureFunc(TextureList, dir);
			var lsf = new LuaSoundFunc(SoundList, dir);

			LuaScript["TEXTURE"] = ltf;
			LuaScript["CANVAS"] = new LuaCanvasFunc(CanvasList);
			LuaScript["GRAPHICS"] = new LuaGraphicsFunc();   // true scissor clip for scrolling UI panels
			LuaScript["SCENE3D"] = new Lua3DSceneFunc(Scene3DList);
			LuaScript["PHYSICS"] = new LuaPhysicsFunc();
			LuaScript["GLOBALCAMERA"] = new LuaGlobalCamera();
			LuaScript["NET"] = new LuaNetworking();   // P2P online core (OpenTaiko Online protocol)
			LuaScript["COLLIDERS"] = new LuaCollidersFunc();   // general collider shapes (raycast / overlap)
			LuaScript["PATHFIND"] = new LuaPathfindFunc();     // weighted nav graph + A*
			LuaScript["MODEL"] = new LuaModelFunc(dir);
			LuaScript["SOUND"] = lsf;
			LuaScript["VIDEO"] = new LuaVideoFunc(VideoList, dir);
			LuaScript["TEXT"] = new LuaTextFunc(TextList, GlyphTextList, dir);
			LuaScript["JSONLOADER"] = new LuaJsonLoaderFunc(dir);
			LuaScript["INILOADER"] = new LuaIniLoaderFunc(dir);
			LuaScript["INPUT"] = new LuaInputFunc();
			LuaScript["REPLAY"] = new LuaReplayFunc();   // list a chart's replays + start watching one
			LuaScript["SIZE"] = new LuaSizeFunc();
			LuaScript["VECTOR2"] = new LuaVector2Func();
			LuaScript["VECTOR3"] = new LuaVector3Func();
			LuaScript["VECTOR4"] = new LuaVector4Func();
			LuaScript["VECTOR"] = new LuaVectorFunc();
			LuaScript["MATRIX2"] = new LuaMatrix2Func();
			LuaScript["MATRIX3"] = new LuaMatrix3Func();
			LuaScript["MATRIX4"] = new LuaMatrix4Func();
			LuaScript["MATRIX"] = new LuaMatrixFunc();
			LuaScript["QUATERNION"] = new LuaQuaternionFunc();
			LuaScript["COLOR"] = new LuaColorFunc();
			LuaScript["COUNTER"] = new LuaCounterFunc();
			LuaScript["NAMEPLATE"] = new LuaNameplateFunc();
			LuaScript["NAMEPLATESLIST"] = new LuaNameplatesDatabase();
			LuaScript["CONFIG"] = new LuaConfigIniFunc();
			LuaScript["SONGMOUNT"] = new LuaSongMountFunc();   // read the song the host just confirmed (for online sync)
			LuaScript["THEME"] = new LuaThemeFunc();
			LuaScript["SHARED"] = new LuaSharedResourceFunc(OpenTaiko.GlobalStores.SharedTextures, OpenTaiko.GlobalStores.SharedSounds, OpenTaiko.GlobalStores.SharedStrings, ltf, lsf, dir);
			LuaScript["DATABASE"] = new LuaDataStorageFunc(dir);
			LuaScript["HEIGHTMAP"] = new LuaHeightmapFunc(dir);
			LuaScript["CHARACTER"] = new LuaCharacterFunc();
			LuaScript["PUCHICHARALIST"] = OpenTaiko.Tx?.LuaPuchicharaDb;
			LuaScript["CHARACTERLIST"] = OpenTaiko.Tx?.LuaCharacterDb;
			LuaScript["STORAGE"] = new LuaStorageFunc(dir);
			LuaScript["PLAYSTATE"] = new LuaPlayStateFunc();
			LuaScript["SQL"] = new LuaSQLFunc(dir);
			LuaScript["LANG"] = new LuaLangFunc();
			LuaScript["ACTIVITY"] = new LuaActivityFunc();
			LuaScript["ROACTIVITY"] = new LuaROActivityFunc();
			LuaScript["MODICONS"] = new LuaModIconsFunc();
			LuaScript["HITSOUNDSLIST"] = new LuaHitsoundsDatabase();
			LuaScript["VIRTUALSLOTS"] = new LuaVirtualSlotManager();
			LuaScript["DANBUILDER"] = new LuaDanBuildFunc();
			LuaScript["GRADIENT"] = new LuaGradientMapFunc(GradientList);

			LuaScript["GetSaveFile"] = GetLuaSaveFile;
			LuaScript["RequestSongList"] = RequestSongList;
			LuaScript["GenerateSongListSettings"] = LuaSongListSettings.Generate;
			LuaScript["IsSongsEnumerating"] = (Func<bool>)(() => OpenTaiko.EnumSongs?.IsEnumerating ?? false);
			// positive "enumeration fully done" flag (IsSongsEnumerating is also false BEFORE the scan starts)
			LuaScript["IsSongsEnumDone"] = (Func<bool>)(() => OpenTaiko.EnumSongs?.IsSongListEnumCompletelyDone ?? false);

			LuaScript.DoString(LoadingApiLua, "LOADING");   // skinner loading-bar API (LOADING:Add/Tick), see tBeginYieldable

			if (File.Exists(this.strScriptPath)) {
				LuaScript.DoString(File.ReadAllText(this.strScriptPath), this.strScriptShort);
			} else {
				strScriptPath = strScriptShort = nameof(fallbackScript);
				LuaScript.DoString(fallbackScript, strScriptShort);
			}

			lfLoadAssets.Load(LuaScript);
			lfReloadLanguage.Load(LuaScript);

			if (loadAssets) LoadAssets();

			listScripts.Add(this);
		} catch (Exception e) {
			Crash(e, $"initializing {nameof(CLuaScript)}");
		}
	}

	public void LoadAssets(params object[] args) {
		if (bLoadedAssets) return;

		RunLuaCode(lfLoadAssets, args);

		bLoadedAssets = true;
		bDisposed = false;
	}

	public void ReloadLanguage(params object[] args) {
		RunLuaCode(lfReloadLanguage, args);
	}

	public void Dispose() {
		if (bDisposed) return;

		void freeDisposableList<T>(ICollection<T> list) where T : IDisposable? {
			foreach (var disposable in list)
				disposable?.Dispose();
			list.Clear();
		}
		freeDisposableList(this.listDisposables);
		freeDisposableList(this.TextureList);
		freeDisposableList(this.CanvasList);
		freeDisposableList(this.Scene3DList);
		freeDisposableList(this.SoundList);
		freeDisposableList(this.VideoList);
		freeDisposableList(this.TextList);
		freeDisposableList(this.GlyphTextList);
		freeDisposableList(this.GradientList);

		LuaScript?.Dispose();

		bDisposed = true;
		bLoadedAssets = false;

		listScripts.Remove(this);
	}

	protected void Crash(Exception exception, string? at = null) {
		bCrashed = true;
		// iOS: surface Lua errors to the device console (os_log), which is easier to read than the on-screen overlay.

		LogNotification.PopError($"{this.GetType().Name} Error{(string.IsNullOrWhiteSpace(at) ? "" : $" at {at}")}: {exception.ToString()}");
		Trace.TraceError($"Full script path: {this.strScriptPath}");
		Trace.TraceError(exception.StackTrace);
	}
}
