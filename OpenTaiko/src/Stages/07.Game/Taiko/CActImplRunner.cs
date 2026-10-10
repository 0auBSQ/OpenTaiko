using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

internal class CActImplRunner : CActivity {
	/// <summary>
	/// ランナー
	/// </summary>
	public CActImplRunner() {
		base.IsDeActivated = true;
	}

	public void Start(int Player, bool IsMiss, CChip? pChip) {
		if (Runner != null && !OpenTaiko.ConfigIni.SimpleMode) {
			ref var runner = ref stRunners[RunnerTail];
			if (!(runner.bUse || NotesManager.IsGenericRoll(pChip) || NotesManager.IsRollEnd(pChip))) {
				RunnerTail = (RunnerTail + 1) % RUNNER_COUNT;
				runner.bUse = true;
				runner.nPlayer = Player;
				if (IsMiss == true) {
					runner.nType = 0;
				} else {
					runner.nType = random.Next(1, Type + 1);
				}
				runner.ctProgress = new CCounter(0, OpenTaiko.Skin.Resolution[0], Timer, OpenTaiko.Timer);
				runner.nOldValue = 0;
				runner.nNowPtn = 0;
				runner.fX = 0;
			}
		}
	}

	public override void Activate() {
		if (OpenTaiko.ConfigIni.SimpleMode) {
			base.Activate();
			return;
		}

		for (int i = 0; i < RUNNER_COUNT; i++) {
			ref var runner = ref stRunners[i];
			runner = new STRunner();
			runner.bUse = false;
			runner.ctProgress = new CCounter();
		}
		RunnerHead = RunnerTail = 0;

		var preset = OpenTaiko.stageGameScreen.ScenePreset;

		if (preset == null) return;

		Random random = new Random();

		var dancerOrigindir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.RUNNER}");
		if (Directory.Exists($@"{dancerOrigindir}")) {
			var dirs = Directory.GetDirectories($@"{dancerOrigindir}");
			if (preset.RunnerSet?.Length > 0) {
				var _presetPath = (preset.RunnerSet.Length > 0) ? $@"{dancerOrigindir}" + preset.RunnerSet[random.Next(0, preset.RunnerSet.Length)] : "";
				var path = (Directory.Exists(_presetPath))
					? _presetPath
					: (dirs.Length > 0 ? dirs[random.Next(0, dirs.Length)] : "");
				LoadRunnerConifg(path);

				Runner = OpenTaiko.tTextureCreate($@"{path}{Path.DirectorySeparatorChar}Runner.png");
			}
		}

		// フィールド上で代入してたためこちらへ移動。
		base.Activate();
	}

	public override void DeActivate() {
		if (OpenTaiko.ConfigIni.SimpleMode) {
			base.DeActivate();
			return;
		}

		for (int i = 0; i < RUNNER_COUNT; i++) {
			stRunners[i].ctProgress = null;
		}

		OpenTaiko.tDisposeSafely(ref Runner);

		base.DeActivate();
	}

	public override void CreateManagedResource() {
		base.CreateManagedResource();
	}

	public override void ReleaseManagedResource() {
		base.ReleaseManagedResource();
	}

	public override int Draw() {
		if (OpenTaiko.ConfigIni.SimpleMode) {
			return base.Draw();
		}

		var prevHead = RunnerHead;
		for (int i = 0; i < RUNNER_COUNT; i++) {
			var iRunner = (prevHead + i) % RUNNER_COUNT;
			ref var runner = ref stRunners[iRunner];
			if (!runner.bUse) {
				if (iRunner == RunnerTail)
					break;
			} else {
				runner.nOldValue = runner.ctProgress.CurrentValue;
				runner.ctProgress.Tick();
				if (runner.ctProgress.IsEnded || runner.fX > OpenTaiko.Skin.Resolution[0]) {
					runner.ctProgress.Stop();
					runner.bUse = false;
					if (iRunner == RunnerHead)
						RunnerHead = (RunnerHead + 1) % RUNNER_COUNT;
				}
				int progress = runner.ctProgress.CurrentValue - runner.nOldValue;
				if (progress > 0) {
					runner.fX += progress * (float)(OpenTaiko.Skin.ScaleX * Math.Abs(OpenTaiko.stageGameScreen.actPlayInfo.dbGameBPS(runner.nPlayer)) * 10 / 3);
					int Width = OpenTaiko.Skin.Resolution[0] / Ptn;
					runner.nNowPtn = (int)runner.fX / Width;
				}
				if (Runner != null) {
					if (runner.nPlayer == 0) {
						Runner.t2DDraw((int)(StartPoint_X[0] + runner.fX), StartPoint_Y[0], new Rectangle(runner.nNowPtn * Size[0], runner.nType * Size[1], Size[0], Size[1]));
					} else {
						Runner.t2DDraw((int)(StartPoint_X[1] + runner.fX), StartPoint_Y[1], new Rectangle(runner.nNowPtn * Size[0], runner.nType * Size[1], Size[0], Size[1]));
					}
				}
			}
		}
		return base.Draw();
	}

	#region[ private ]
	//-----------------
	[StructLayout(LayoutKind.Sequential)]
	private struct STRunner {
		public bool bUse;
		public int nPlayer;
		public int nType;
		public int nOldValue;
		public int nNowPtn;
		public float fX;
		public CCounter ctProgress;
	}
	private const int RUNNER_COUNT = 128; // circular queue
	private readonly STRunner[] stRunners = new STRunner[RUNNER_COUNT];
	Random random = new Random();
	int RunnerHead = 0;
	int RunnerTail = 0;

	private CTexture Runner;

	private void LoadRunnerConifg(string dancerPath) {
		var _str = "";
		OpenTaiko.Skin.LoadSkinConfigFromFile(dancerPath + @"\RunnerConfig.txt", ref _str);

		string[] delimiter = { "\n" };
		string[] strSingleLine = _str.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

		Size = new int[2] { 60, 125 };
		Ptn = 48;
		Type = 4;
		StartPoint_X = new int[2] { 175, 175 };
		StartPoint_Y = new int[2] { 40, 560 };
		Timer = 16;

		foreach (string s in strSingleLine) {
			string str = s.Replace('\t', ' ').TrimStart(new char[] { '\t', ' ' });
			if ((str.Length != 0) && (str[0] != ';')) {
				try {
					string strCommand;
					string strParam;
					string[] strArray = str.Split(new char[] { '=' });

					if (strArray.Length == 2) {
						strCommand = strArray[0].Trim();
						strParam = strArray[1].Trim();

						if (strCommand == "Game_Runner_Size") {
							string[] strSplit = strParam.Split(',');
							for (int i = 0; i < 2; i++) {
								Size[i] = int.Parse(strSplit[i]);
							}
						} else if (strCommand == "Game_Runner_Ptn") {
							Ptn = int.Parse(strParam);
						} else if (strCommand == "Game_Runner_Type") {
							Type = int.Parse(strParam);
						} else if (strCommand == "Game_Runner_Timer") {
							Timer = int.Parse(strParam);
						} else if (strCommand == "Game_Runner_StartPoint_X") {
							string[] strSplit = strParam.Split(',');
							for (int i = 0; i < 2; i++) {
								StartPoint_X[i] = int.Parse(strSplit[i]);
							}
						} else if (strCommand == "Game_Runner_StartPoint_Y") {
							string[] strSplit = strParam.Split(',');
							for (int i = 0; i < 2; i++) {
								StartPoint_Y[i] = int.Parse(strSplit[i]);
							}
						}

					}
					continue;
				} catch (Exception exception) {
					Trace.TraceError(exception.ToString());
					Trace.TraceError("例外が発生しましたが処理を継続します。 (6a32cc37-1527-412e-968a-512c1f0135cd)");
					continue;
				}
			}
		}

	}

	// ランナー画像のサイズ。 X, Y
	private int[] Size;
	// ランナーのコマ数
	private int Ptn;
	// ランナーのキャラクターのバリエーション(ミス時を含まない)。
	private int Type;
	private int Timer;
	// スタート地点のX座標 1P, 2P
	private int[] StartPoint_X;
	// スタート地点のY座標 1P, 2P
	private int[] StartPoint_Y;

	//-----------------
	#endregion
}
