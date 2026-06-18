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
		Textures.Add(fileName, OpenTaiko.tテクスチャの生成($@"{DirPath}{Path.DirectorySeparatorChar}{trueFileName}"));
	}
	public void DrawGraph(double x, double y, string fileName) {
		Textures[fileName]?.t2D描画((int)x, (int)y);
	}
	public void DrawRectGraph(double x, double y, int rect_x, int rect_y, int rect_width, int rect_height, string fileName) {
		Textures[fileName]?.t2D描画((int)x, (int)y, new System.Drawing.RectangleF(rect_x, rect_y, rect_width, rect_height));
	}
	public void DrawGraphCenter(double x, double y, string fileName) {
		Textures[fileName]?.t2D拡大率考慮中央基準描画((int)x, (int)y);
	}
	public void DrawGraphRectCenter(double x, double y, int rect_x, int rect_y, int rect_width, int rect_height, string fileName) {
		Textures[fileName]?.t2D拡大率考慮中央基準描画((int)x, (int)y, new System.Drawing.RectangleF(rect_x, rect_y, rect_width, rect_height));
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
			Textures[fileName].fZ軸中心回転 = (float)(angle * Math.PI / 180);
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
					Textures[fileName].b加算合成 = false;
					Textures[fileName].b乗算合成 = false;
					Textures[fileName].b減算合成 = false;
					Textures[fileName].bスクリーン合成 = false;
					break;
				case "Add":
					Textures[fileName].b加算合成 = true;
					Textures[fileName].b乗算合成 = false;
					Textures[fileName].b減算合成 = false;
					Textures[fileName].bスクリーン合成 = false;
					break;
				case "Multi":
					Textures[fileName].b加算合成 = false;
					Textures[fileName].b乗算合成 = true;
					Textures[fileName].b減算合成 = false;
					Textures[fileName].bスクリーン合成 = false;
					break;
				case "Sub":
					Textures[fileName].b加算合成 = false;
					Textures[fileName].b乗算合成 = false;
					Textures[fileName].b減算合成 = true;
					Textures[fileName].bスクリーン合成 = false;
					break;
				case "Screen":
					Textures[fileName].b加算合成 = false;
					Textures[fileName].b乗算合成 = false;
					Textures[fileName].b減算合成 = false;
					Textures[fileName].bスクリーン合成 = true;
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
	public Dictionary<string, CTexture> Textures;

	protected Lua LuaScript;
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
		if (OpenTaiko.ConfigIni != null && !OpenTaiko.ConfigIni.bEnableLua) return;

		try {
			LuaScript = new Lua();
			LuaScript.State.Encoding = Encoding.UTF8;
			LuaSecurity.Secure(LuaScript);

			LuaScript["func"] = new ScriptBGFunc(Textures, Path.GetDirectoryName(filePath));
			using (var streamAPI = new StreamReader(OpenTaiko.ResolveAssetPath(Path.Combine(OpenTaiko.strEXEのあるフォルダ, "BGScriptAPI.lua")), Encoding.UTF8)) {
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
		List<CTexture> texs = new List<CTexture>();
		foreach (var tex in Textures.Values) {
			texs.Add(tex);
		}
		for (int i = 0; i < texs.Count; i++) {
			var tex = texs[i];
			OpenTaiko.tテクスチャの解放(ref tex);
		}

		Textures.Clear();

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
					raritiesP[i] = OpenTaiko.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(OpenTaiko.GetActualPlayer(i))].metadata.Rarity;
					raritiesC[i] = OpenTaiko.Tx.Characters[OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(i)].data.Character].metadata.Rarity;
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
				OpenTaiko.stageGameScreen.actGauge.db現在のゲージ値,
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
				int maxFloor = OpenTaiko.SongMount.rChoosenSong.score[5].譜面情報.nTotalFloor;
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
				OpenTaiko.stageGameScreen.actGauge.db現在のゲージ値,
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
