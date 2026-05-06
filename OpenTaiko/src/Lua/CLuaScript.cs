using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json.Nodes;
using FDK;
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
	public HashSet<LuaSound> SoundList = [];
	public HashSet<LuaVideo> VideoList = [];
	public HashSet<LuaText> TextList = [];
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
			LuaScript?.State?.GarbageCollector(KeraLua.LuaGC.Collect, 0);
			return ret;
		} catch (Exception exception) {
			Crash(exception);
		}
		return null;
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

	public CLuaScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true, string fallbackScript = "") {
		strDir = dir;
		strScriptPath = Path.Join(strDir, "Script.lua");
		strScriptShort = Path.Join(Path.GetFileName(Path.GetDirectoryName(this.strScriptPath)), Path.GetFileName(this.strScriptPath));
		strTexturesDir = texturesDir ?? $"{dir}/Textures";
		strSounsdDir = soundsDir ?? $"{dir}/Sounds";


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
			LuaScript["SOUND"] = lsf;
			LuaScript["VIDEO"] = new LuaVideoFunc(VideoList, dir);
			LuaScript["TEXT"] = new LuaTextFunc(TextList, dir);
			LuaScript["JSONLOADER"] = new LuaJsonLoaderFunc(dir);
			LuaScript["INILOADER"] = new LuaIniLoaderFunc(dir);
			LuaScript["INPUT"] = new LuaInputFunc();
			LuaScript["SIZE"] = new LuaSizeFunc();
			LuaScript["VECTOR2"] = new LuaVector2Func();
			LuaScript["COLOR"] = new LuaColorFunc();
			LuaScript["COUNTER"] = new LuaCounterFunc();
			LuaScript["NAMEPLATE"] = new LuaNameplateFunc();
			LuaScript["NAMEPLATESLIST"] = new LuaNameplatesDatabase();
			LuaScript["CONFIG"] = new LuaConfigIniFunc();
			LuaScript["THEME"] = new LuaThemeFunc();
			LuaScript["SHARED"] = new LuaSharedResourceFunc(OpenTaiko.GlobalStores.SharedTextures, OpenTaiko.GlobalStores.SharedSounds, OpenTaiko.GlobalStores.SharedStrings, ltf, lsf, dir);
			LuaScript["DATABASE"] = new LuaDataStorageFunc(dir);
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
			Crash(e);
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
		freeDisposableList(this.SoundList);
		freeDisposableList(this.VideoList);
		freeDisposableList(this.TextList);
		freeDisposableList(this.GradientList);

		LuaScript.Dispose();

		bDisposed = true;
		bLoadedAssets = false;

		listScripts.Remove(this);
	}

	protected void Crash(Exception exception) {
		bCrashed = true;

		LogNotification.PopError($"{this.GetType().Name} Error: {exception.ToString()}");
		Trace.TraceError($"Full script path: {this.strScriptPath}");
		Trace.TraceError(exception.StackTrace);
	}
}
