using System.Diagnostics;
using System.Text;
using FDK;
using NLua;

namespace OpenTaiko;

class ScriptBGFunc {
	private Dictionary<string, CTexture> Textures;
	private string DirPath;

	public ScriptBGFunc(Dictionary<string, CTexture> texs, string dirPath) {
		Textures = texs;
		DirPath = dirPath;
	}

	public (int x, int y) DrawText(double x, double y, string text) {
		return OpenTaiko.actTextConsole.Print((int)x, (int)y, CTextConsole.EFontType.White, text);
	}
	public (int x, int y) DrawNum(double x, double y, double text) {
		return OpenTaiko.actTextConsole.Print((int)x, (int)y, CTextConsole.EFontType.White, text.ToString());
	}
	public void AddGraph(string fileName) {
		string trueFileName = fileName.Replace('/', Path.DirectorySeparatorChar);
		trueFileName = trueFileName.Replace('\\', Path.DirectorySeparatorChar);
		if (this.Textures.ContainsKey(fileName)) // already loaded
			return;
		// Load SYNC: BG scripts read func:GetTextureWidth/Height right after AddGraph (in init) to size their
		// scroll/tile loops — a streamed/async texture has size 0 until its upload finishes, which corrupts the
		// layout (broken-looking background). The BG loads behind the loading bar, so an inline load here is fine.
		bool prev = CTexture.SyncForce;
		CTexture.SyncForce = true;
		try { Textures.Add(fileName, OpenTaiko.tTextureCreate($@"{DirPath}{Path.DirectorySeparatorChar}{trueFileName}")); }
		finally { CTexture.SyncForce = prev; }
		Textures[fileName]?.SetTextureWrapMode(Silk.NET.OpenGLES.TextureWrapMode.Repeat);
	}
	public void DrawGraph(double x, double y, string fileName) {
		Textures[fileName]?.t2DDraw((int)x, (int)y);
	}
	public void DrawRectGraph(double x, double y, int rect_x, int rect_y, int rect_width, int rect_height, string fileName) {
		Textures[fileName]?.t2DDraw((int)x, (int)y, new System.Drawing.RectangleF(rect_x, rect_y, rect_width, rect_height));
	}
	public void DrawGraphCenter(double x, double y, string fileName) {
		Textures[fileName]?.t2DScaledCenterBasedDraw((int)x, (int)y);
	}
	public void DrawGraphRectCenter(double x, double y, int rect_x, int rect_y, int rect_width, int rect_height, string fileName) {
		Textures[fileName]?.t2DScaledCenterBasedDraw((int)x, (int)y, new System.Drawing.RectangleF(rect_x, rect_y, rect_width, rect_height));
	}
	public void SetOpacity(double opacity, string fileName) {
		if (Textures[fileName] != null)
			Textures[fileName].Opacity = (int)opacity;
	}
	public void SetScale(double xscale, double yscale, string fileName) {
		if (Textures[fileName] != null) {
			Textures[fileName].vcScaleRatio.X = (float)xscale;
			Textures[fileName].vcScaleRatio.Y = (float)yscale;
		}
	}
	public void SetRotation(double angle, string fileName) {
		if (Textures[fileName] != null) {
			Textures[fileName].fZAxisCenterRotate = (float)(angle * Math.PI / 180);
		}
	}
	public void SetColor(double r, double g, double b, string fileName) {
		if (Textures[fileName] != null) {
			Textures[fileName].color4 = new Color4((float)r, (float)g, (float)b, 1f);
		}
	}
	public void SetBlendMode(string type, string fileName) {
		if (Textures[fileName] != null) {
			switch (type) {
				case "Normal":
				default:
					Textures[fileName].bAddBlend = false;
					Textures[fileName].bMultiplyBlend = false;
					Textures[fileName].bSubtractBlend = false;
					Textures[fileName].bScreenBlend = false;
					break;
				case "Add":
					Textures[fileName].bAddBlend = true;
					Textures[fileName].bMultiplyBlend = false;
					Textures[fileName].bSubtractBlend = false;
					Textures[fileName].bScreenBlend = false;
					break;
				case "Multi":
					Textures[fileName].bAddBlend = false;
					Textures[fileName].bMultiplyBlend = true;
					Textures[fileName].bSubtractBlend = false;
					Textures[fileName].bScreenBlend = false;
					break;
				case "Sub":
					Textures[fileName].bAddBlend = false;
					Textures[fileName].bMultiplyBlend = false;
					Textures[fileName].bSubtractBlend = true;
					Textures[fileName].bScreenBlend = false;
					break;
				case "Screen":
					Textures[fileName].bAddBlend = false;
					Textures[fileName].bMultiplyBlend = false;
					Textures[fileName].bSubtractBlend = false;
					Textures[fileName].bScreenBlend = true;
					break;
			}
		}
	}

	public double GetTextureWidth(string fileName) {
		if (Textures[fileName] != null) {
			return Textures[fileName].szTextureSize.Width;
		}
		return -1;
	}

	public double GetTextureHeight(string fileName) {
		if (Textures[fileName] != null) {
			return Textures[fileName].szTextureSize.Height;
		}
		return -1;
	}
}
class ScriptBG : IDisposable {
	public Dictionary<string, CTexture> Textures = [];
	public HashSet<LuaTexture> TextureList = [];
	public HashSet<LuaSound> SoundList = [];
	public HashSet<LuaText> TextList = [];

	protected Lua? LuaScript;
	protected string FilePath;
	protected string FilePathShort;

	protected NamedLuaFunction LuaSetConstValues = new("setConstValues");
	protected NamedLuaFunction LuaUpdateValues = new("updateValues");
	protected NamedLuaFunction LuaClearIn = new("clearIn");
	protected NamedLuaFunction LuaClearOut = new("clearOut");
	protected NamedLuaFunction LuaInit = new("init");
	protected NamedLuaFunction LuaUpdate = new("update");
	protected NamedLuaFunction LuaDraw = new("draw");

	public ScriptBG(string filePath) {
		this.Init(filePath);
	}

	// script fallback list
	public ScriptBG(params string[] filePaths) {
		foreach (var filePath in filePaths) {
			this.Init(filePath);
			if (this.Exists())
				return;
		}
	}

	private void Init(string filePath) {
		this.FilePath = filePath;
		this.FilePathShort = Path.Join(Path.GetFileName(Path.GetDirectoryName(filePath)), Path.GetFileName(filePath));
		Textures = new Dictionary<string, CTexture>();


		if (!File.Exists(filePath)) return;

		LuaScript = new Lua();
		LuaScript.State.Encoding = Encoding.UTF8;
		LuaSecurity.Secure(LuaScript, filePath);

		string path = Path.GetDirectoryName(filePath) ?? "";
		LuaScript["func"] = new ScriptBGFunc(Textures, path);
		LuaScript["TEXTURE"] = new LuaTextureFunc(TextureList, path);
		LuaScript["SOUND"] = new LuaSoundFunc(SoundList, path);
		LuaScript["TEXT"] = new LuaTextFunc(TextList, path);
		LuaScript["JSONLOADER"] = new LuaJsonLoaderFunc(path);
		LuaScript["INPUT"] = new LuaInputFunc();
		LuaScript["COLOR"] = new LuaColorFunc();
		LuaScript["COUNTER"] = new LuaCounterFunc();
		LuaScript["CONFIG"] = new LuaConfigIniFunc();


		try {
			using (var streamAPI = new StreamReader("BGScriptAPI.lua", Encoding.UTF8)) {
				using (var stream = new StreamReader(filePath, Encoding.UTF8)) {
					var text = $"{streamAPI.ReadToEnd()}\n{stream.ReadToEnd()}";
					LuaScript.DoString(text, this.FilePathShort);
				}
			}

			LuaSetConstValues.Load(LuaScript);
			LuaUpdateValues.Load(LuaScript);
			LuaClearIn.Load(LuaScript);
			LuaClearOut.Load(LuaScript);
			LuaInit.Load(LuaScript);
			LuaUpdate.Load(LuaScript);
			LuaDraw.Load(LuaScript);
		} catch (Exception ex) {
			Crash(ex);
		}
	}
	public bool Exists() {
		return LuaScript != null;
	}

	protected object[]? RunLuaCode(NamedLuaFunction luaFunction, params object[] args) {
		if (LuaScript == null)
			return null;
		try {
			if (luaFunction.Func == null) {
				LogNotification.PopWarning($"{this.GetType().Name} Warning: [{this.FilePathShort}] Function [{luaFunction.Name}] is called but undefined");
				Trace.TraceWarning($"Full script path: {this.FilePath}");
				Trace.TraceWarning(new StackTrace(new StackFrame(1, true)).ToString());
				luaFunction.LoadNoop(LuaScript); // silence further warnings
				return null;
			}
			return luaFunction.Func.Call(args);
		} catch (Exception exception) {
			Crash(exception);
		}
		return null;
	}

	protected void Crash(Exception exception) {
		LogNotification.PopError($"{this.GetType().Name} Error: {exception.ToString()}");
		Trace.TraceError($"Full script path: {this.FilePath}");
		Trace.TraceError(exception.StackTrace);
		LuaScript?.Dispose();
		LuaScript = null;
	}
	public void Dispose() {
		foreach (var (key, tex) in this.Textures)
			tex?.Dispose();
		Textures.Clear();

		void freeDisposableList<T>(ICollection<T> list) where T : IDisposable? {
			foreach (var disposable in list)
				disposable?.Dispose();
			list.Clear();
		}
		freeDisposableList(this.TextureList);
		freeDisposableList(this.SoundList);
		freeDisposableList(this.TextList);

		LuaScript?.Dispose();

		LuaSetConstValues.Dispose();
		LuaUpdateValues.Dispose();
		LuaClearIn.Dispose();
		LuaClearOut.Dispose();
		LuaInit.Dispose();
		LuaUpdate.Dispose();
		LuaDraw.Dispose();
	}

	public void ClearIn(int player) => RunLuaCode(LuaClearIn, player);
	public void ClearOut(int player) => RunLuaCode(LuaClearOut, player);
	public void Init() {
		if (LuaScript == null) return;
		try {
			// Preprocessing
			string[] raritiesP = { "Common", "Common", "Common", "Common", "Common" };
			string[] raritiesC = { "Common", "Common", "Common", "Common", "Common" };

			if (OpenTaiko.Tx.Puchichara != null && OpenTaiko.Tx.Characters != null) {
				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
					raritiesP[i] = OpenTaiko.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(i)].metadata.Rarity;
					raritiesC[i] = OpenTaiko.Tx.Characters[OpenTaiko.SaveFileInstances[i].data.Character].metadata.Rarity;
				}
			}

			// Initialisation
			RunLuaCode(LuaSetConstValues, OpenTaiko.ConfigIni.nPlayerCount,
				OpenTaiko.P1IsBlue(),
				OpenTaiko.ConfigIni.sLang,
				OpenTaiko.ConfigIni.SimpleMode,
				raritiesP,
				raritiesC
			);

			RunLuaCode(LuaUpdateValues, OpenTaiko.FPS.DeltaTime,
				OpenTaiko.FPS.NowFPS,
				OpenTaiko.stageGameScreen.bIsAlreadyCleared,
				0,
				OpenTaiko.stageGameScreen.AIBattleState,
				OpenTaiko.stageGameScreen.bIsAIBattleWin,
				OpenTaiko.stageGameScreen.actGauge.dbCurrentGaugeValue,
				OpenTaiko.stageGameScreen.actPlayInfo.dbBPM,
				new bool[] { false, false, false, false, false },
				-1
			);

			RunLuaCode(LuaInit);
		} catch (Exception ex) {
			Crash(ex);
		}
	}

	public void Update() {
		if (LuaScript == null) return;
		try {
			float currentFloorPositionMax140 = 0;

			if (OpenTaiko.SongMount.rChoosenSong != null && OpenTaiko.SongMount.rChoosenSong.score[5] != null) {
				int maxFloor = OpenTaiko.SongMount.rChoosenSong.score[5].ChartInfo.nTotalFloor;
				int nightTime = Math.Max(140, maxFloor / 2);

				currentFloorPositionMax140 = Math.Min(OpenTaiko.stageGameScreen.actPlayInfo.NowMeasure[0] / (float)nightTime, 1f);
			}
			double timestamp = -1.0;

			if (OpenTaiko.TJA != null) {
				double msTimeOffset = OpenTaiko.SongMount.nChoosenSongDifficulty[0] != (int)Difficulty.Dan ? 0 : -CTja.msDanNextSongDelay;
				// Due to the fact that all Dans use DELAY to offset instead of OFFSET, Dan offset can't be properly synced. ¯\_(ツ)_/¯

				timestamp = (OpenTaiko.TJA.RawTjaTimeToDefTime(
					OpenTaiko.TJA.TjaTimeToRawTjaTimeNote(
						OpenTaiko.TJA.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs))
				) + msTimeOffset) / 1000.0;
			}

			RunLuaCode(LuaUpdateValues, OpenTaiko.FPS.DeltaTime,
				OpenTaiko.FPS.NowFPS,
				OpenTaiko.stageGameScreen.bIsAlreadyCleared,
				(double)currentFloorPositionMax140,
				OpenTaiko.stageGameScreen.AIBattleState,
				OpenTaiko.stageGameScreen.bIsAIBattleWin,
				OpenTaiko.stageGameScreen.actGauge.dbCurrentGaugeValue,
				OpenTaiko.stageGameScreen.actPlayInfo.dbBPM,
				OpenTaiko.stageGameScreen.bIsGOGOTIME,
				timestamp);
			/*LuaScript.SetObjectToPath("fps", TJAPlayer3.FPS.n現在のFPS);
            LuaScript.SetObjectToPath("deltaTime", TJAPlayer3.FPS.DeltaTime);
            LuaScript.SetObjectToPath("isClear", TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared);
            LuaScript.SetObjectToPath("towerNightOpacity", (double)(255 * currentFloorPositionMax140));*/
			RunLuaCode(LuaUpdate);
		} catch (Exception ex) {
			Crash(ex);
		}
	}
	public void Draw() => RunLuaCode(LuaDraw);
}
