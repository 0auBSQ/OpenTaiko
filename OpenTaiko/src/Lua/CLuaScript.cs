using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json.Nodes;
using FDK;
using NLua;

namespace TJAPlayer3 {
	class CLuaScript : IDisposable {
		private static List<CLuaScript> listScripts = new List<CLuaScript>();
		public static void tReloadLanguage(string lang) {
			foreach (var item in listScripts) {
				item.ReloadLanguage(lang);
			}
		}



		public string strDir { get; private set; }
		public string strTexturesDir { get; private set; }
		public string strSounsdDir { get; private set; }

		public bool bLoadedAssets { get; private set; }
		public bool bDisposed { get; private set; }
		public bool bCrashed { get; protected set; }

		protected Lua LuaScript { get; private set; }

		private LuaFunction lfLoadAssets;
		private LuaFunction lfReloadLanguage;

		private CLuaInfo luaInfo;
		private CLuaFps luaFPS = new CLuaFps();

		private List<IDisposable> listDisposables = new List<IDisposable>();

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

		protected object[] RunLuaCode(LuaFunction luaFunction, params object[] args) {
			try {
				var ret = luaFunction.Call(args);
				LuaScript.State.GarbageCollector(KeraLua.LuaGC.Collect, 0);
				return ret;
			} catch (Exception exception) {
				Crash(exception);
			}
			return new object[0];
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
			CSound sound = TJAPlayer3.SoundManager?.tCreateSound($"{strSounsdDir}/{name}", soundGroup);

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

		private TitleTextureKey CreateTitleTextureKey(string title, CCachedFontRenderer fontRenderer, int maxSize, Color? color = null, Color? edgeColor = null) {
			return new TitleTextureKey(title, fontRenderer, color ?? Color.White, edgeColor ?? Color.Black, maxSize);
		}

		private CTexture GetTextTex(TitleTextureKey titleTextureKey, bool vertical, bool keepCenter) {
			return TitleTextureKey.ResolveTitleTexture(titleTextureKey, vertical, keepCenter);
		}

		public CLuaScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) {
			strDir = dir;
			strTexturesDir = texturesDir ?? $"{dir}/Textures";
			strSounsdDir = soundsDir ?? $"{dir}/Sounds";


			LuaScript = new Lua();
			LuaScript.LoadCLRPackage();
			LuaScript.State.Encoding = Encoding.UTF8;
			LuaScript.DoFile($"{strDir}/Script.lua");

			LuaScript["info"] = luaInfo = new CLuaInfo(strDir);
			LuaScript["fps"] = luaFPS;


			lfLoadAssets = (LuaFunction)LuaScript["loadAssets"];
			lfReloadLanguage = (LuaFunction)LuaScript["reloadLanguage"];

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
			LuaScript["displayDanPlate"] = CActSelect段位リスト.tDisplayDanPlate;
			LuaScript["debugLog"] = DebugLog;


			if (loadAssets) LoadAssets();

			listScripts.Add(this);
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

			foreach (IDisposable disposable in listDisposables) {
				disposable.Dispose();
			}
			listDisposables.Clear();

			LuaScript.Dispose();

			bDisposed = true;
			bLoadedAssets = false;

			listScripts.Remove(this);
		}

		private void Crash(Exception exception) {
			bCrashed = true;

			LogNotification.PopError($"Lua Script Error: {exception.ToString()}");
		}
	}
}
