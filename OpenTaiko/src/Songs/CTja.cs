using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using FDK;
using FDK.ExtensionMethods;
using SkiaSharp;
using Color = System.Drawing.Color;

namespace OpenTaiko;

internal class CTja : CActivity {
	// 定数

	public enum E種別 { DTX, GDA, G2D, BMS, BME, SMF }

	public List<string> listErrors = new List<string>();
	private int nNowReadLine;
	// Class

	public class CBPM {
		public double dbBPM値;
		public double bpm_change_time;
		public double bpm_change_bmscroll_time;
		public ECourse bpm_change_course = ECourse.eNormal;
		public int n内部番号;
		public int n表記上の番号;

		public override string ToString() {
			StringBuilder builder = new StringBuilder(0x80);
			if (this.n内部番号 != this.n表記上の番号) {
				builder.Append(string.Format("CBPM{0}(内部{1})", CTja.tZZ(this.n表記上の番号), this.n内部番号));
			} else {
				builder.Append(string.Format("CBPM{0}", CTja.tZZ(this.n表記上の番号)));
			}
			builder.Append(string.Format(", BPM:{0}", this.dbBPM値));
			return builder.ToString();
		}
	}
	/// <summary>
	/// 判定ライン移動命令
	/// </summary>
	public class CJPOSSCROLL {
		public double msMoveDt;
		public double pxOrigX;
		public double pxOrigY;
		public double pxMoveDx;
		public double pxMoveDy;
		public int n内部番号;
		public int n表記上の番号;
		public CChip? chip;

		public override string ToString() {
			StringBuilder builder = new StringBuilder(0x80);
			if (this.n内部番号 != this.n表記上の番号) {
				builder.Append(string.Format("CJPOSSCROLL{0}(内部{1})", CTja.tZZ(this.n表記上の番号), this.n内部番号));
			} else {
				builder.Append(string.Format("CJPOSSCROLL{0}", CTja.tZZ(this.n表記上の番号)));
			}
			builder.Append(string.Format(", JPOSSCROLL:{0}", this.msMoveDt / 1000));
			return builder.ToString();
		}
	}

	public enum EBranchConditionType {
		Accuracy,
		Drumroll,
		Score,
		Accuracy_BigNotesOnly
	}

	public class CWAV : IDisposable {
		public bool bBGMとして使う;
		public List<int> listこのWAVを使用するチャンネル番号の集合 = new List<int>(16);
		public int nチップサイズ = 100;
		public int n位置;
		public long[] n一時停止時刻 = new long[OpenTaiko.ConfigIni.nPoliphonicSounds];    // 4
		public int SongVol = CSound.DefaultSongVol;
		public LoudnessMetadata? SongLoudnessMetadata = null;
		public int n現在再生中のサウンド番号;
		public long[] n再生開始時刻 = new long[OpenTaiko.ConfigIni.nPoliphonicSounds];    // 4
		public int n内部番号;
		public int n表記上の番号;
		public CSound[] rSound = new CSound[OpenTaiko.ConfigIni.nPoliphonicSounds];     // 4
		public string strコメント文 = "";
		public string strファイル名 = "";
		public bool bBGMとして使わない {
			get {
				return !this.bBGMとして使う;
			}
			set {
				this.bBGMとして使う = !value;
			}
		}
		public bool bIsBassSound = false;
		public bool bIsGuitarSound = false;
		public bool bIsDrumsSound = false;
		public bool bIsSESound = false;
		public bool bIsBGMSound = false;

		public override string ToString() {
			var sb = new StringBuilder(128);

			if (this.n表記上の番号 == this.n内部番号) {
				sb.Append(string.Format("CWAV{0}: ", CTja.tZZ(this.n表記上の番号)));
			} else {
				sb.Append(string.Format("CWAV{0}(内部{1}): ", CTja.tZZ(this.n表記上の番号), this.n内部番号));
			}
			sb.Append(
				$"{nameof(SongVol)}:{this.SongVol}, {nameof(LoudnessMetadata.Integrated)}:{this.SongLoudnessMetadata?.Integrated}, {nameof(LoudnessMetadata.TruePeak)}:{this.SongLoudnessMetadata?.TruePeak}, 位置:{this.n位置}, サイズ:{this.nチップサイズ}, BGM:{(this.bBGMとして使う ? 'Y' : 'N')}, File:{this.strファイル名}, Comment:{this.strコメント文}");

			return sb.ToString();
		}

		#region [ Dispose-Finalize パターン実装 ]
		//-----------------
		public void Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		public void Dispose(bool bManagedリソースの解放も行う) {
			if (this.bDisposed済み)
				return;

			if (bManagedリソースの解放も行う) {
				for (int i = 0; i < OpenTaiko.ConfigIni.nPoliphonicSounds; i++) // 4
				{
					if (this.rSound[i] != null)
						OpenTaiko.SoundManager.tDisposeSound(this.rSound[i]);
					this.rSound[i] = null;

					if ((i == 0) && OpenTaiko.ConfigIni.bOutputCreationReleaseLog)
						Trace.TraceInformation("サウンドを解放しました。({0})({1})", this.strコメント文, this.strファイル名);
				}
			}

			this.bDisposed済み = true;
		}
		~CWAV() {
			this.Dispose(false);
		}
		//-----------------
		#endregion

		#region [ private ]
		//-----------------
		private bool bDisposed済み;
		//-----------------
		#endregion
	}

	[Serializable]
	public class DanSongs {
		[NonSerialized]
		public CTexture TitleTex;
		[NonSerialized]
		public CTexture SubTitleTex;
		public string Title;
		public string SubTitle;
		public string FileName;
		public string Genre;
		public int ScoreInit;
		public int ScoreDiff;
		public int Level;
		public int Difficulty;
		public static int Number = 0;
		public bool bTitleShow;
		public Dan_C[] Dan_C = new Dan_C[CExamInfo.cMaxExam];

		[NonSerialized]
		public CWAV Wave;

		public DanSongs() {
			Number++;
		}
	}

	public struct STLYRIC {
		public long Time;
		public SKBitmap TextTex;
		public string Text;
		public int index;
	}


	// 構造体

	public struct STチップがある {
		public bool Drums;
		public bool Branch;
	}
	public enum ECourse {
		eNormal,
		eExpert,
		eMaster
	}

	public enum ELevelIcon {
		eMinus,
		eNone,
		ePlus
	}

	public enum ESide {
		eNormal,
		eEx
	}
	public class CLine {
		public int n小節番号;
		public int n文字数;
		public ECourse nコース = ECourse.eNormal;
	}

	// Properties


	public class CBranchStartInfo {
		public int nMeasureCount;
		public double dbTime;
		public double dbBPM;
		public double dbSCROLL;
		public double dbSCROLLY;
		public double dbBMScollTime;
		public double db移動待機時刻;
		public double db出現時刻;
		public float fMeasure_s;
		public float fMeasure_m;
	}

	/// <summary>
	/// 分岐開始時の情報を記録するためのあれ 2020.04.21
	/// </summary>
	public CBranchStartInfo cBranchStart = new CBranchStartInfo();

	public int nBGMAdjust {
		get;
		private set;
	}
	public bool b分岐を一回でも開始した = false; //2020.04.22 akasoko26 分岐譜面のみ値を代入するように。

	public int nPlayerSide; //2017.08.14 kairera0467 引数で指定する
	public bool bSession譜面を読み込む;
	public string ARTIST;
	public string BACKGROUND;
	public double BASEBPM;
	public double BPM;
	public double MinBPM;
	public double MaxBPM;
	public STチップがある bチップがある;
	public string COMMENT;
	public string GENRE;
	public string MAKER;
	public string[] NOTESDESIGNER = new string[(int)Difficulty.Total] { "", "", "", "", "", "", "" };
	public bool EXPLICIT;
	public string SELECTBG;
	public bool HIDDENLEVEL;
	public STDGBVALUE<int> LEVEL;
	public bool bLyrics;
	public int[] LEVELtaiko = new int[(int)Difficulty.Total] { -1, -1, -1, -1, -1, -1, -1 };
	public ELevelIcon[] LEVELtaikoIcon = new ELevelIcon[(int)Difficulty.Total] { ELevelIcon.eNone, ELevelIcon.eNone, ELevelIcon.eNone, ELevelIcon.eNone, ELevelIcon.eNone, ELevelIcon.eNone, ELevelIcon.eNone };
	public ESide SIDE;
	public CSongUniqueID uniqueID;

	// Tower lifes
	public int LIFE;

	public string TOWERTYPE;

	public int DANTICK = 0;
	public Color DANTICKCOLOR = Color.White;

	public Dictionary<int, CVideoDecoder> listVD;
	public Dictionary<int, CBPM> listBPM;
	public List<CChip> listChip; // increasing time > chip priority > definition order
	public List<CChip> listBarLineChip; // increasing definition order
	public List<CChip> listNoteChip; // increasing definition order
	public List<CChip>[] listChip_Branch;
	public Dictionary<int, CWAV> listWAV;
	public List<CJPOSSCROLL> listJPOSSCROLL;
	public List<DanSongs> List_DanSongs;
	private EScrollMode eScrollMode;

	public const int n最大音数 = 4;
	public const int n小節の解像度 = 384;
	public const double msDanNextSongDelay = 6200.0;
	public string PANEL;
	public string PATH_WAV;
	public string PREIMAGE;
	public string PREVIEW;
	public string strハッシュofDTXファイル;
	public string strファイル名;
	public string strファイル名の絶対パス;
	public string strフォルダ名;
	public CLocalizationData SUBTITLE = new CLocalizationData();
	public CLocalizationData TITLE = new CLocalizationData();
	public double dbDTXVPlaySpeed;
	public double dbScrollSpeed;
	public int nデモBGMオフセット;

	private int n現在の小節数 = 1;

	private int[] nNowRollCountBranch = new int[3] { -1, -1, -1 };

	private int[] n連打チップ_temp = new int[3];
	private int msOFFSET_Abs = 0; // from initial measure to music begin
	private bool isOFFSET_Negative = false;
	private int msMOVIEOFFSET_Abs = 0; // from music begin to video begin
	private bool isMOVIEOFFSET_Negative = false;
	private double dbNowBPM = 120.0;
	private int nDELAY = 0;

	public bool[] bHasBranch = new bool[(int)Difficulty.Total] { false, false, false, false, false, false, false };

	public bool[] bHasBranchDan = new bool[1] { false };

	//分岐関連
	private ECourse n現在のコース = ECourse.eNormal;

	private bool b最初の分岐である;
	public int[] nノーツ数 = new int[4]; //3:共通

	public int[] nDan_NotesCount = new int[1];
	public int[] nDan_BalloonCount = new int[1];
	// public int[] nDan_BallonCount = new int[1];

	public int[] nノーツ数_Branch = new int[4]; //
	public CChip[] pDan_LastChip;
	public int[] n風船数 = new int[4]; //0～2:各コース 3:共通

	private List<CLine> listLine;
	private int nLineCountTemp; //分岐開始時の小節数を記録。
	private ECourse nLineCountCourseTemp = ECourse.eNormal; //現在カウント中のコースを記録。

	public int n参照中の難易度 = 3;
	public int nScoreModeTmp = 99; //2017.01.28 DD
	public int[,] nScoreInit = new int[2, (int)Difficulty.Total]; //[ x, y ] x=通常or真打 y=コース
	public int[] nScoreDiff = new int[(int)Difficulty.Total]; //[y]
	public bool[,] b配点が指定されている = new bool[3, (int)Difficulty.Total]; //2017.06.04 kairera0467 [ x, y ] x=通常(Init)or真打orDiff y=コース

	public float fNow_Measure_s = 4.0f;
	public float fNow_Measure_m = 4.0f;
	public double dbNowTime = 0.0;
	public double dbNowBMScollTime = 0.0;
	public double dbNowScroll = 1.0;
	public double dbNowScrollY = 0.0; //2016.08.13 kairera0467 複素数スクロール
	public double dbLastTime = 0.0; //直前の小節の開始時間
	public double dbLastBMScrollTime = 0.0;

	public int[] bBARLINECUE = new int[2]; //命令を入れた次の小節の操作を実現するためのフラグ。0 = mainflag, 1 = cuetype
	public bool b小節線を挿入している = false;

	//Normal Regular Masterにしたいけどここは我慢。
	private List<int>[] listBalloon_Branch;
	private List<int> listBalloon; //旧構文用

	public List<SKBitmap> listLyric; //歌詞を格納していくリスト。スペル忘れた(ぉい
	public List<STLYRIC> listLyric2;

	//public Dictionary<double, CChip> kusudaMAP = new Dictionary<double, CChip>();

	public bool usingLyricsFile; //If lyric file is used (VTT/LRC), ignore #LYRIC tags & do not parse other lyric file tags

	private int[] listBalloon_Branch_数値管理;

	public string scenePreset;

	public bool[] b譜面が存在する = new bool[(int)Difficulty.Total];

	private string[] dlmtSpace = { " " };
	private string[] dlmtEnter = { "\n" };
	private string[] dlmtCOURSE = { "COURSE:" };

	private int nスクロール方向 = 0;
	//2015.09.18 kairera0467
	//バタフライスライドみたいなアレをやりたいがために実装。
	//次郎2みたいな複素数とかは意味不明なので、方向を指定してスクロールさせることにした。
	//0:通常
	//1:上
	//2:下
	//3:右上
	//4:右下
	//5:左
	//6:左上
	//7:左下

	public string strBGIMAGE_PATH;
	public string strBGVIDEO_PATH;

	public double db出現時刻;
	public double db移動待機時刻;

	public string strBGM_PATH;
	public int SongVol;
	public LoudnessMetadata? SongLoudnessMetadata;

	public bool bHIDDENBRANCH; //2016.04.01 kairera0467 選曲画面上、譜面分岐開始前まで譜面分岐の表示を隠す
	public bool bGOGOTIME; //2018.03.11 kairera0467

	public bool[] IsBranchBarDraw = new bool[4]; // 仕様変更により、黄色lineの表示法を変更.2020.04.21.akasoko26
	public bool IsEndedBranching = true; // BRANCHENDが呼び出されたかどうか
	public Dan_C[] Dan_C;

	public bool IsEnabledFixSENote;
	public int FixSENote;
	public GaugeIncreaseMode GaugeIncreaseMode;

	#region [ EXTENDED VARiABLES ]
	public Dictionary<string, CSongObject> listObj;
	public Dictionary<string, CTexture> listTextures;
	public Dictionary<string, CTexture> listOriginalTextures;
	#endregion



#if TEST_NOTEOFFMODE
		public STLANEVALUE<bool> b演奏で直前の音を消音する;
//		public bool bHH演奏で直前のHHを消音する;
//		public bool bGUITAR演奏で直前のGUITARを消音する;
//		public bool bBASS演奏で直前のBASSを消音する;
#endif
	// Constructor

	public CTja() {
		this.nPlayerSide = 0;
		this.TITLE.SetString("default", "");
		this.SUBTITLE.SetString("default", "");
		this.ARTIST = "";
		this.COMMENT = "";
		this.SIDE = ESide.eEx;
		this.PANEL = "";
		this.GENRE = "";
		this.MAKER = "";
		this.EXPLICIT = false;
		this.SELECTBG = "";
		this.bLyrics = false;
		this.usingLyricsFile = false;
		this.PREVIEW = "";
		this.PREIMAGE = "";
		this.BACKGROUND = "";
		this.PATH_WAV = "";
		this.BPM = 120.0;
		this.msOFFSET_Abs = 0;
		this.isOFFSET_Negative = false;
		STDGBVALUE<int> stdgbvalue = new STDGBVALUE<int>();
		stdgbvalue.Drums = 0;
		this.LEVEL = stdgbvalue;
		this.bHIDDENBRANCH = false;
		this.bチップがある = new STチップがある();
		this.strファイル名 = "";
		this.strフォルダ名 = "";
		this.strファイル名の絶対パス = "";
		this.n無限管理WAV = new int[36 * 36];
		this.n無限管理BPM = new int[36 * 36];
		this.n無限管理PAN = new int[36 * 36];
		this.n無限管理SIZE = new int[36 * 36];
		this.listBalloon_Branch_数値管理 = new int[3];
		this.nRESULTIMAGE用優先順位 = new int[7];
		this.nRESULTMOVIE用優先順位 = new int[7];
		this.nRESULTSOUND用優先順位 = new int[7];

		this.nBGMAdjust = 0;
		this.nPolyphonicSounds = OpenTaiko.ConfigIni.nPoliphonicSounds;
		this.dbDTXVPlaySpeed = 1.0f;

		//this.nScoreModeTmp = 1;
		for (int y = 0; y < (int)Difficulty.Total; y++) {
			this.nScoreInit[0, y] = 300;
			this.nScoreInit[1, y] = 1000;
			this.nScoreDiff[y] = 120;
			this.b配点が指定されている[0, y] = false;
			this.b配点が指定されている[1, y] = false;
			this.b配点が指定されている[2, y] = false;
		}

		this.b最初の分岐である = true;

		this.SongVol = CSound.DefaultSongVol;
		this.SongLoudnessMetadata = null;

		GaugeIncreaseMode = GaugeIncreaseMode.Normal;

#if TEST_NOTEOFFMODE
			this.bHH演奏で直前のHHを消音する = true;
			this.bGUITAR演奏で直前のGUITARを消音する = true;
			this.bBASS演奏で直前のBASSを消音する = true;
#endif

		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; // Change default culture to invariant, fixes (Purota)
		Dan_C = new Dan_C[CExamInfo.cMaxExam];
		pDan_LastChip = new CChip[1];
		DanSongs.Number = 0;

	}
	public CTja(string strファイル名, bool bヘッダのみ, int nBGMAdjust, int difficulty)
		: this() {
		this.Activate();
		this.t入力(strファイル名, bヘッダのみ, nBGMAdjust, 0, false, difficulty);
	}
	public CTja(string strファイル名, bool bヘッダのみ, int nBGMAdjust, int nPlayerSide, bool bSession, int difficulty)
		: this() {
		this.Activate();
		this.t入力(strファイル名, bヘッダのみ, nBGMAdjust, nPlayerSide, bSession, difficulty);
	}


	// メソッド

	public void tAVIの読み込み() {
		if (!this.bヘッダのみ) {
			if (this.listVD != null) {
				foreach (CVideoDecoder cvd in this.listVD.Values) {
					cvd.InitRead();
					cvd.dbPlaySpeed = OpenTaiko.ConfigIni.SongPlaybackSpeed;
				}
			}
		}
	}

	public void tWave再生位置自動補正() {
		foreach (CWAV cwav in this.listWAV.Values) {
			this.tWave再生位置自動補正(cwav);
		}
	}
	public void tWave再生位置自動補正(CWAV wc) {
		if (wc.rSound[0] != null && wc.rSound[0].TotalPlayTime >= 5000) {
			for (int i = 0; i < nPolyphonicSounds; i++) {
				if ((wc.rSound[i] != null) && (wc.rSound[i].IsPlaying)) {
					long nCurrentTime = SoundManager.PlayTimer.SystemTimeMs;
					if (nCurrentTime > wc.n再生開始時刻[i]) {
						long nAbsTimeFromStartPlaying = nCurrentTime - wc.n再生開始時刻[i];
						// WASAPI/ASIO用↓
						if (!OpenTaiko.stageGameScreen.bPAUSE) {
							if (wc.rSound[i].IsPaused) wc.rSound[i].Resume(nAbsTimeFromStartPlaying);
							else wc.rSound[i].tSetPositonToBegin(nAbsTimeFromStartPlaying);
						} else {
							wc.rSound[i].Pause();
						}
					}
				}
			}
		}
	}
	public void tWavの再生停止(int nWaveの内部番号) {
		tWavの再生停止(nWaveの内部番号, false);
	}
	public void tWavの再生停止(int nWaveの内部番号, bool bミキサーからも削除する) {
		if (this.listWAV.TryGetValue(nWaveの内部番号, out CWAV cwav)) {
			for (int i = 0; i < nPolyphonicSounds; i++) {
				if (cwav.rSound[i] != null && cwav.rSound[i].IsPlaying) {
					if (bミキサーからも削除する) {
						cwav.rSound[i].tStopSoundAndRemoveSoundFromMixer();
					} else {
						cwav.rSound[i].Stop();
					}
				}
			}
		}
	}
	public void tWAVの読み込み(CWAV cwav) {
		string str = string.IsNullOrEmpty(this.PATH_WAV) ? this.strフォルダ名 : this.PATH_WAV;
		str = str + cwav.strファイル名;

		try {
			#region [ 同時発音数を、チャンネルによって変える ]

			int nPoly = nPolyphonicSounds;
			if (OpenTaiko.SoundManager.GetCurrentSoundDeviceType() != "DirectSound") // DShowでの再生の場合はミキシング負荷が高くないため、
			{
				// チップのライフタイム管理を行わない
				if (cwav.bIsBassSound) nPoly = (nPolyphonicSounds >= 2) ? 2 : 1;
				else if (cwav.bIsGuitarSound) nPoly = (nPolyphonicSounds >= 2) ? 2 : 1;
				else if (cwav.bIsSESound) nPoly = 1;
				else if (cwav.bIsBGMSound) nPoly = 1;
			}

			if (cwav.bIsBGMSound) nPoly = 1;

			#endregion

			for (int i = 0; i < nPoly; i++) {
				try {
					cwav.rSound[i] = OpenTaiko.SoundManager.tCreateSound(str, ESoundGroup.SongPlayback);

					if (!OpenTaiko.ConfigIni.bDynamicBassMixerManagement) {
						cwav.rSound[i].AddBassSoundFromMixer();
					}

					if (OpenTaiko.ConfigIni.bOutputCreationReleaseLog) {
						Trace.TraceInformation("サウンドを作成しました。({3})({0})({1})({2}bytes)", cwav.strコメント文, str,
							cwav.rSound[0].SoundBufferSize, cwav.rSound[0].IsStreamPlay ? "Stream" : "OnMemory");
					}
				} catch (Exception e) {
					cwav.rSound[i] = null;
					Trace.TraceError("サウンドの作成に失敗しました。({0})({1})", cwav.strコメント文, str);
					Trace.TraceError(e.ToString());
				}
			}
		} catch (Exception exception) {
			Trace.TraceError("サウンドの生成に失敗しました。({0})({1})", cwav.strコメント文, str);
			Trace.TraceError(exception.ToString());

			for (int j = 0; j < nPolyphonicSounds; j++) {
				cwav.rSound[j] = null;
			}

			//continue;
		}
	}

	public static string tZZ(int n) {
		if (n < 0 || n >= 36 * 36)
			return "!!";    // オーバー／アンダーフロー。

		// n を36進数2桁の文字列にして返す。

		string str = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		return new string(new char[] { str[n / 36], str[n % 36] });
	}

	public void tApplyFunMods(int player = 0) {
		Random rnd = new System.Random();

		var eFun = OpenTaiko.ConfigIni.nFunMods[OpenTaiko.GetActualPlayer(player)];
		var chara = OpenTaiko.Tx.Characters[OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(player)].data.Character];

		var bombFactor = Math.Max(1, Math.Min(100, chara.effect.BombFactor));
		var fuseRollFactor = Math.Max(0, Math.Min(100, chara.effect.FuseRollFactor));

		switch (eFun) {
			case EFunMods.Minesweeper:
				foreach (var chip in this.listChip) {
					if (NotesManager.IsMissableNote(chip)) {
						int n = rnd.Next(100);

						if (n < bombFactor) chip.nChannelNo = 0x1C;
					}

					if (NotesManager.IsBalloon(chip)) {
						int n = rnd.Next(100);

						if (n < fuseRollFactor) chip.nChannelNo = 0x1D;
					}

				}
				break;
			case EFunMods.Avalanche:
				foreach (var chip in this.listChip) {
					int n = rnd.Next(100);


					chip.dbSCROLL *= (n + 50) / (double)100;
				}
				break;
			case EFunMods.None:
			default:
				break;
		}
	}

	public void tRandomizeTaikoChips(int player = 0) {
		//2016.02.11 kairera0467
		Random rnd = new System.Random();

		var eRandom = OpenTaiko.ConfigIni.eRandom[OpenTaiko.GetActualPlayer(player)];

		switch (eRandom) {
			case ERandomMode.Mirror:
				foreach (var chip in this.listChip) {
					switch (chip.nChannelNo) {
						case 0x11:
							chip.nChannelNo = 0x12;
							break;
						case 0x12:
							chip.nChannelNo = 0x11;
							break;
						case 0x13:
							chip.nChannelNo = 0x14;
							chip.nSenote = 6;
							break;
						case 0x14:
							chip.nChannelNo = 0x13;
							chip.nSenote = 5;
							break;
					}
				}
				break;
			case ERandomMode.Random:
				foreach (var chip in this.listChip) {
					int n = rnd.Next(100);

					if (n >= 0 && n <= 20) {
						switch (chip.nChannelNo) {
							case 0x11:
								chip.nChannelNo = 0x12;
								break;
							case 0x12:
								chip.nChannelNo = 0x11;
								break;
							case 0x13:
								chip.nChannelNo = 0x14;
								chip.nSenote = 6;
								break;
							case 0x14:
								chip.nChannelNo = 0x13;
								chip.nSenote = 5;
								break;
						}
					}
				}
				break;
			case ERandomMode.SuperRandom:
				foreach (var chip in this.listChip) {
					int n = rnd.Next(100);

					if (n >= 0 && n <= 50) {
						switch (chip.nChannelNo) {
							case 0x11:
								chip.nChannelNo = 0x12;
								break;
							case 0x12:
								chip.nChannelNo = 0x11;
								break;
							case 0x13:
								chip.nChannelNo = 0x14;
								chip.nSenote = 6;
								break;
							case 0x14:
								chip.nChannelNo = 0x13;
								chip.nSenote = 5;
								break;
						}
					}
				}
				break;
			case ERandomMode.MirrorRandom:
				foreach (var chip in this.listChip) {
					int n = rnd.Next(100);

					if (n >= 0 && n <= 80) {
						switch (chip.nChannelNo) {
							case 0x11:
								chip.nChannelNo = 0x12;
								break;
							case 0x12:
								chip.nChannelNo = 0x11;
								break;
							case 0x13:
								chip.nChannelNo = 0x14;
								chip.nSenote = 6;
								break;
							case 0x14:
								chip.nChannelNo = 0x13;
								chip.nSenote = 5;
								break;
						}
					}
				}
				break;
			case ERandomMode.Off:
			default:
				break;
		}

		if (OpenTaiko.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(OpenTaiko.GetActualPlayer(nPlayerSide))].effect.AllPurple) {
			foreach (var chip in this.listChip) {
				if (chip.nChannelNo is 0x13 or 0x1A or 0x14 or 0x1B) {
					chip.nChannelNo = 0x101;
				}
			}
		}

		if (eRandom != ERandomMode.Off) {
			#region[ list作成 ]
			//ひとまずチップだけのリストを作成しておく。
			List<CChip> list音符のみのリスト;
			list音符のみのリスト = new List<CChip>();
			int nCount = 0;
			int dkdkCount = 0;

			foreach (CChip chip in this.listChip) {
				if (chip.nChannelNo >= 0x11 && chip.nChannelNo < 0x18) {
					list音符のみのリスト.Add(chip);
				}
			}
			#endregion

			this.tSenotes_Core_V2(list音符のみのリスト);
		}
	}

	#region [ チップの再生と停止 ]
	public void tチップの再生(CChip pChip, long n再生開始システム時刻ms) {
		if (OpenTaiko.ConfigIni.bNoAudioIfNot1xSpeed && OpenTaiko.ConfigIni.nSongSpeed != 20)
			return;

		if (pChip.n整数値_内部番号 >= 0) {
			if (this.listWAV.TryGetValue(pChip.n整数値_内部番号, out CWAV wc)) {
				int index = wc.n現在再生中のサウンド番号 = (wc.n現在再生中のサウンド番号 + 1) % nPolyphonicSounds;
				if ((wc.rSound[0] != null) &&
					(wc.rSound[0].IsStreamPlay || wc.rSound[index] == null)) {
					index = wc.n現在再生中のサウンド番号 = 0;
				}
				CSound sound = wc.rSound[index];
				if (sound != null) {
					sound.PlaySpeed = OpenTaiko.ConfigIni.SongPlaybackSpeed;
					// 再生速度によって、WASAPI/ASIOで使う使用mixerが決まるため、付随情報の設定(音量/PAN)は、再生速度の設定後に行う

					// 2018-08-27 twopointzero - DON'T attempt to load (or queue scanning) loudness metadata here.
					//                           This code is called right after loading the .tja, and that code
					//                           will have just made such an attempt.
					OpenTaiko.SongGainController.Set(wc.SongVol, wc.SongLoudnessMetadata, sound);

					sound.SoundPosition = wc.n位置;
					sound.PlayStart();
				}
				wc.n再生開始時刻[wc.n現在再生中のサウンド番号] = n再生開始システム時刻ms;
				this.tWave再生位置自動補正(wc);
			}
		}
	}
	public void t各自動再生音チップの再生時刻を変更する(int nBGMAdjustの増減値) {
		this.nBGMAdjust += nBGMAdjustの増減値;

		for (int i = 0; i < this.listChip.Count; i++) {
			int nChannelNumber = this.listChip[i].nChannelNo;
			if (((
					 (nChannelNumber == 1) ||
					 ((0x61 <= nChannelNumber) && (nChannelNumber <= 0x69))
				 ) ||
				 ((0x70 <= nChannelNumber) && (nChannelNumber <= 0x79))
				) ||
				(((0x80 <= nChannelNumber) && (nChannelNumber <= 0x89)) || ((0x90 <= nChannelNumber) && (nChannelNumber <= 0x92)))
			   ) {
				this.listChip[i].n発声時刻ms += nBGMAdjustの増減値;
			}
		}
		foreach (CWAV cwav in this.listWAV.Values) {
			for (int j = 0; j < nPolyphonicSounds; j++) {
				if ((cwav.rSound[j] != null) && cwav.rSound[j].IsPlaying) {
					cwav.n再生開始時刻[j] += nBGMAdjustの増減値;
				}
			}
		}
	}
	public void t全チップの再生一時停止() {
		foreach (CWAV cwav in this.listWAV.Values) {
			for (int i = 0; i < nPolyphonicSounds; i++) {
				if ((cwav.rSound[i] != null) && cwav.rSound[i].IsPlaying) {
					cwav.rSound[i].Pause();
					cwav.n一時停止時刻[i] = SoundManager.PlayTimer.SystemTimeMs;
				}
			}
		}
	}
	public void t全チップの再生再開() {
		foreach (CWAV cwav in this.listWAV.Values) {
			for (int i = 0; i < nPolyphonicSounds; i++) {
				if ((cwav.rSound[i] != null) && cwav.rSound[i].IsPaused) {
					cwav.rSound[i].Resume(cwav.n一時停止時刻[i] - cwav.n再生開始時刻[i]);
					cwav.n再生開始時刻[i] += SoundManager.PlayTimer.SystemTimeMs - cwav.n一時停止時刻[i];
				}
			}
		}
	}
	public void tStopAllChips() {
		foreach (CWAV cwav in this.listWAV.Values) {
			this.tWavの再生停止(cwav.n内部番号);
		}
	}
	public void t全チップの再生停止とミキサーからの削除() {
		foreach (CWAV cwav in this.listWAV.Values) {
			this.tWavの再生停止(cwav.n内部番号, true);
		}
	}
	#endregion

	public void t入力(string strファイル名, bool bヘッダのみ, int nBGMAdjust, int nPlayerSide, bool bSession, int difficulty) {
		this.bヘッダのみ = bヘッダのみ;
		this.strファイル名の絶対パス = Path.GetFullPath(strファイル名);
		this.strファイル名 = Path.GetFileName(this.strファイル名の絶対パス);
		this.strフォルダ名 = Path.GetDirectoryName(this.strファイル名の絶対パス) + Path.DirectorySeparatorChar;

		// Unique ID parsing/generation
		this.uniqueID = new CSongUniqueID(this.strフォルダ名 + @$"{Path.DirectorySeparatorChar}uniqueID.json");

		try {
			this.nPlayerSide = nPlayerSide;
			this.bSession譜面を読み込む = bSession;
			//次郎方式
			StreamReader reader = new StreamReader(strファイル名, Encoding.GetEncoding(OpenTaiko.sEncType));
			string str2 = reader.ReadToEnd();
			reader.Close();
			this.t入力_全入力文字列から(str2, nBGMAdjust, difficulty);
		} catch (Exception ex) {
			Trace.TraceError("おや?エラーが出たようです。お兄様。");
			Trace.TraceError(ex.ToString());
			Trace.TraceError("例外が発生しましたが処理を継続します。 (79ff8639-9b3c-477f-bc4a-f2eea9784860)");
		}
	}
	public void t入力_全入力文字列から(string str全入力文字列, int nBGMAdjust, int Difficulty) {
		if (!string.IsNullOrEmpty(str全入力文字列)) {
			#region [ 初期化 ]
			for (int j = 0; j < 36 * 36; j++) {
				this.n無限管理WAV[j] = -j;
				this.n無限管理BPM[j] = -j;
				this.n無限管理PAN[j] = -10000 - j;
				this.n無限管理SIZE[j] = -j;
			}
			this.n内部番号WAV1to = 1;
			this.n内部番号BPM1to = 1;
			this.bstackIFからENDIFをスキップする = new Stack<bool>();
			this.bstackIFからENDIFをスキップする.Push(false);
			this.n現在の乱数 = 0;
			for (int k = 0; k < 7; k++) {
				this.nRESULTIMAGE用優先順位[k] = 0;
				this.nRESULTMOVIE用優先順位[k] = 0;
				this.nRESULTSOUND用優先順位[k] = 0;
			}
			#endregion
			#region [ 入力/行解析 ]
			#region[初期化]
			this.dbNowScroll = 1.0;
			this.n現在のコース = ECourse.eNormal;
			#endregion
			this.t入力_V4(str全入力文字列, Difficulty);

			#endregion
			this.n無限管理WAV = null;
			this.n無限管理BPM = null;
			this.n無限管理PAN = null;
			this.n無限管理SIZE = null;
			if (!this.bヘッダのみ) {
				#region [ CWAV初期化 ]
				foreach (CWAV cwav in this.listWAV.Values) {
					if (cwav.nチップサイズ < 0) {
						cwav.nチップサイズ = 100;
					}
					if (cwav.n位置 <= -10000) {
						cwav.n位置 = 0;
					}
				}
				#endregion
				#region [ チップ倍率設定 ]						// #28145 2012.4.22 yyagi 二重ループを1重ループに変更して高速化)
				foreach (CChip chip in this.listChip) {
					if (this.listWAV.TryGetValue(chip.n整数値_内部番号, out CWAV cwav)) {
						chip.dbChipSizeRatio = ((double)cwav.nチップサイズ) / 100.0;
					}
				}
				#endregion
				if (this.listChip.Count > 0) {
					this.listChip = this.listChip.OrderBy(x => x).ToList();
												// 高速化のためにはこれを削りたいが、listChipの最後がn発声位置の終端である必要があるので、
												// 保守性確保を優先してここでのソートは残しておく
												// なお、093時点では、このソートを削除しても動作するようにはしてある。
												// (ここまでの一部チップ登録を、listChip.Add(c)から同Insert(0,c)に変更してある)
												// これにより、数ms程度ながらここでのソートも高速化されている。
				}
				#region [ 発声時刻の計算 ]
				double bpm = this.BASEBPM;

				List<STLYRIC> tmplistlyric = new List<STLYRIC>();
				int BGM番号 = 0;


				// Chip post-process:
				// * Offset chips from RawTjaTime To TjaTime; see RawTjaTimeToTjaTimeMusic()
				// * TaikoJiro 1 behavior: Notes' scrolling BPM and HBScroll beat (but not time) are re-adjusted to the active timing
				//   (also affect notes' time in TaikoJiro 2 (?))
				// * Truncate movement of unfinished JPosScrolls and calculate resulting source coordination for deterministic behavior
				CJPOSSCROLL? lastJPosScroll = null;
				foreach (CChip chip in this.listChip) {
					int ch = chip.nChannelNo;

					switch (ch) {
						case 0x01: {
								if (this.isOFFSET_Negative == false)
									chip.n発声時刻ms += this.msOFFSET_Abs;

								#region[listlyric2の時間合わせ]
								for (int ind = 0; ind < listLyric2.Count; ind++) {
									if (listLyric2[ind].index == BGM番号) {
										STLYRIC lyrictmp = this.listLyric2[ind];

										lyrictmp.Time += chip.n発声時刻ms;

										tmplistlyric.Add(lyrictmp);
									}
								}


								BGM番号++;
								#endregion
								continue;
							}
						case 0x02:  // BarLength
						{
								if (this.isOFFSET_Negative)
									chip.n発声時刻ms += this.msOFFSET_Abs;
								continue;
							}
						case 0x03:  // Initial BPM
						{
								if (this.isOFFSET_Negative)
									chip.n発声時刻ms += this.msOFFSET_Abs;
								// this.dbNowBPM has already been initialized
								continue;
							}
						case 0x04:  // BGA (レイヤBGA1)
						case 0x07:  // レイヤBGA2
							break;

						case 0x15:
						case 0x16:
						case 0x17:
						case 0x19:
						case 0x1D:
						case 0x20:
						case 0x21: {
								if (this.isOFFSET_Negative) {
									chip.n発声時刻ms += this.msOFFSET_Abs;
									chip.nNoteEndTimems += this.msOFFSET_Abs;
								}
								continue;
							}
						case 0x18: {
								if (this.isOFFSET_Negative) {
									chip.n発声時刻ms += this.msOFFSET_Abs;
								}
								continue;
							}

						case 0x55:
						case 0x56:
						case 0x57:
						case 0x58:
						case 0x59:
						case 0x60:
							break;

						case 0x50: {
								if (this.isOFFSET_Negative)
									chip.n発声時刻ms += this.msOFFSET_Abs;
								continue;
							}

						case 0x05:  // Extended Object (非対応)
						case 0x06:  // Missアニメ (非対応)
						case 0x5A:  // 未定義
						case 0x5b:  // 未定義
						case 0x5c:  // 未定義
						case 0x5d:  // 未定義
						case 0x5e:  // 未定義
						case 0x5f:  // 未定義
						{
								continue;
							}
						case 0x08:  // 拡張BPM
						{
								if (this.isOFFSET_Negative)
									chip.n発声時刻ms += this.msOFFSET_Abs;
								if (this.listBPM.TryGetValue(chip.n整数値_内部番号, out CBPM cBPM)) {
									bpm = (cBPM.n表記上の番号 == 0 ? 0.0 : this.BASEBPM) + cBPM.dbBPM値;
									this.dbNowBPM = bpm;
								}
								continue;
							}
						case 0x54:  // 動画再生
						{
								if (this.isOFFSET_Negative == false)
									chip.n発声時刻ms += this.msOFFSET_Abs;
								continue;
							}
						case 0x97:
						case 0x98:
						case 0x99: {
								if (this.isOFFSET_Negative) {
									chip.n発声時刻ms += this.msOFFSET_Abs;
									chip.nNoteEndTimems += this.msOFFSET_Abs;
								}
								continue;
							}
						case 0x9A: {

								if (this.isOFFSET_Negative) {
									chip.n発声時刻ms += this.msOFFSET_Abs;
								}
								continue;
							}
						case 0x9D: {
								continue;
							}
						case 0xDC: {
								if (this.isOFFSET_Negative)
									chip.n発声時刻ms += this.msOFFSET_Abs;
								continue;
							}
						case 0xDE: {
								if (this.isOFFSET_Negative) {
									chip.n発声時刻ms += this.msOFFSET_Abs;
									chip.n分岐時刻ms += this.msOFFSET_Abs;
								}
								this.n現在のコース = chip.nBranch;
								continue;
							}
						case 0x52: {
								if (this.isOFFSET_Negative) {
									chip.n発声時刻ms += this.msOFFSET_Abs;
									chip.n分岐時刻ms += this.msOFFSET_Abs;
								}
								this.n現在のコース = chip.nBranch;
								continue;
							}
						case 0xDF: {
								if (this.isOFFSET_Negative)
									chip.n発声時刻ms += this.msOFFSET_Abs;
								continue;
							}
						case 0xE0: {
								continue;
							}
						case 0xE2: { // #JPOSSCROLL
								if (this.isOFFSET_Negative)
									chip.n発声時刻ms += this.msOFFSET_Abs;

								// calculate accumulated movement by time order (not definition order)
								CJPOSSCROLL jposs = this.listJPOSSCROLL[chip.n整数値_内部番号];
								if (lastJPosScroll == null) {
									jposs.pxOrigX = 0;
									jposs.pxOrigY = 0;
								} else {
									if (lastJPosScroll.msMoveDt > 0) {
										double msLastMoveDt = lastJPosScroll.msMoveDt;
										double msCanMove = double.Max(0, chip.n発声時刻ms - lastJPosScroll.chip!.n発声時刻ms);
										// truncate movement of last JPosScroll if unfinished
										if (msCanMove < msLastMoveDt) {
											double lastMoveRate = msCanMove / msLastMoveDt;
											lastJPosScroll.msMoveDt = msCanMove;
											lastJPosScroll.pxMoveDx *= lastMoveRate;
											lastJPosScroll.pxMoveDy *= lastMoveRate;
										}
									}
									jposs.pxOrigX = lastJPosScroll.pxOrigX + lastJPosScroll.pxMoveDx;
									jposs.pxOrigY = lastJPosScroll.pxOrigY + lastJPosScroll.pxMoveDy;
								}
								lastJPosScroll = jposs;
								continue;
							}
						default: {
								if (this.isOFFSET_Negative)
									chip.n発声時刻ms += this.msOFFSET_Abs;
								chip.dbBPM = this.dbNowBPM;
								continue;
							}
					}
				}
				#endregion

				#region[listlyricを時間順に並び替え。]
				this.listLyric2 = tmplistlyric;
				this.listLyric2 = this.listLyric2.OrderBy(x => x.Time).ToList();
				#endregion

				this.nBGMAdjust = 0;
				this.t各自動再生音チップの再生時刻を変更する(nBGMAdjust);

				#region [ チップの種類を分類し、対応するフラグを立てる ]
				foreach (CChip chip in this.listChip) {
					if ((chip.nChannelNo == 0x01 && this.listWAV.TryGetValue(chip.n整数値_内部番号, out CWAV cwav)) && !cwav.listこのWAVを使用するチャンネル番号の集合.Contains(chip.nChannelNo)) {
						cwav.listこのWAVを使用するチャンネル番号の集合.Add(chip.nChannelNo);

						int c = chip.nChannelNo >> 4;
						switch (c) {
							case 0x01:
								cwav.bIsDrumsSound = true; break;
							case 0x02:
								cwav.bIsGuitarSound = true; break;
							case 0x0A:
								cwav.bIsBassSound = true; break;
							case 0x06:
							case 0x07:
							case 0x08:
							case 0x09:
								cwav.bIsSESound = true; break;
							case 0x00:
								if (chip.nChannelNo == 0x01) {
									cwav.bIsBGMSound = true; break;
								}
								break;
						}
					}
				}
				#endregion
				#region[ seNotes計算 ]
				if (this.bチップがある.Branch)
					this.tSetSenotes_branch();
				else
					this.tSetSenotes();

				#endregion
				#region [ bLogDTX詳細ログ出力 ]
				if (OpenTaiko.ConfigIni.bOutputDetailedDTXLog) {
					foreach (CWAV cwav in this.listWAV.Values) {
						Trace.TraceInformation(cwav.ToString());
					}
					foreach (CBPM cbpm3 in this.listBPM.Values) {
						Trace.TraceInformation(cbpm3.ToString());
					}
					foreach (CChip chip in this.listChip) {
						Trace.TraceInformation(chip.ToString());
					}
				}
				#endregion
				int n整数値管理 = 0;
				foreach (CChip chip in this.listChip) {
					if (chip.nChannelNo != 0x54)
						chip.n整数値 = n整数値管理;
					n整数値管理++;
				}

			}
		}
	}

	private string tコメントを削除する(string input) {
		string strOutput = Regex.Replace(input, @" *//.*", ""); //2017.01.28 DD コメント前のスペースも削除するように修正

		return strOutput;
	}

	private string[] tコマンド行を削除したTJAを返す(string[] input) {
		return this.tコマンド行を削除したTJAを返す(input, 0);
	}

	private string[] tコマンド行を削除したTJAを返す(string[] input, int nMode) {
		var sb = new StringBuilder();

		// 18/11/11 AioiLight 譜面にSpace、スペース、Tab等が入っているとおかしくなるので修正。
		// 多分コマンドもスペースが抜かれちゃっているが、コマンド行を除く譜面を返すので大丈夫(たぶん)。
		for (int i = 0; i < input.Length; i++) {
			input[i] = input[i].Trim();
		}

		for (int n = 0; n < input.Length; n++) {
			if (nMode == 0) {
				if (!string.IsNullOrEmpty(input[n]) && NotesManager.FastFlankedParsing(input[n]))//this.CharConvertNote(input[n].Substring(0, 1)) != -1)
				{
					sb.Append(input[n] + "\n");
				}
			} else if (nMode == 1) {
				if (!string.IsNullOrEmpty(input[n]) &&
					(input[n].Substring(0, 1) == "#"
					 || input[n].StartsWith("EXAM")
					 || NotesManager.FastFlankedParsing(input[n]))) {
					if (input[n].StartsWith("BALLOON") || input[n].StartsWith("BPM")) {
						//A～Fで始まる命令が削除されない不具合の対策
					} else {
						sb.Append(input[n] + "\n");
					}
				}
			} else if (nMode == 2) {
				if (!string.IsNullOrEmpty(input[n]) && NotesManager.FastFlankedParsing(input[n])) {
					if (input[n].StartsWith("BALLOON") || input[n].StartsWith("BPM")) {
						//A～Fで始まる命令が削除されない不具合の対策
					} else {
						sb.Append(input[n] + "\n");
					}
				} else {
					if (input[n].StartsWith("#BRANCHSTART") || input[n] == "#N" || input[n] == "#E" || input[n] == "#M") {
						sb.Append(input[n] + "\n");
					}

				}
			}
		}

		string[] strOutput = sb.ToString().Split(this.dlmtEnter, StringSplitOptions.None);

		return strOutput;
	}

	private string[] t空のstring配列を詰めたstring配列を返す(string[] input) {
		var sb = new StringBuilder();

		for (int n = 0; n < input.Length; n++) {
			if (!string.IsNullOrEmpty(input[n])) {
				sb.Append(input[n] + "\n");
			}
		}

		string[] strOutput = sb.ToString().Split(this.dlmtEnter, StringSplitOptions.None);

		return strOutput;
	}

	private string StringArrayToString(string[] input) {
		return this.StringArrayToString(input, "");
	}
	private string StringArrayToString(string[] input, string strデリミタ文字) {
		var sb = new StringBuilder();

		for (int n = 0; n < input.Length; n++) {
			sb.Append(input[n] + strデリミタ文字);
		}

		return sb.ToString();
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="InputText"></param>
	/// <returns>1小節内の文字数</returns>
	private int t1小節の文字数をカウントする(string InputText) {
		return InputText.Length - 1;
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="InputText"></param>
	/// <returns>1小節内の文字数</returns>
	private void t1小節の文字数をカウントしてリストに追加する(string InputText) {
		int nCount = 0;

		if (InputText.StartsWith("#BRANCHSTART")) {
			this.nLineCountTemp = this.n現在の小節数;
			return;
		} else if (InputText.StartsWith("#N")) {
			this.nLineCountCourseTemp = ECourse.eNormal;
			this.n現在の小節数 = this.nLineCountTemp;
			return;
		} else if (InputText.StartsWith("#E")) {
			this.nLineCountCourseTemp = ECourse.eExpert;
			this.n現在の小節数 = this.nLineCountTemp;
			return;
		} else if (InputText.StartsWith("#M")) {
			this.nLineCountCourseTemp = ECourse.eMaster;
			this.n現在の小節数 = this.nLineCountTemp;
			return;
		}

		var line = new CLine();
		line.nコース = this.nLineCountCourseTemp;
		line.n文字数 = InputText.Length - 1;
		line.n小節番号 = this.n現在の小節数;
		this.listLine.Add(line);
		this.n現在の小節数++;
	}

	/// <summary>
	/// 0:改行文字を削除して、デリミタとしてスペースを入れる。(返り値:string)
	/// 1:改行文字を削除、さらにSplitして返す(返り値:string[n])
	/// </summary>
	/// <param name="strInput"></param>
	/// <param name="nMode"></param>
	/// <returns></returns>
	private object str改行文字を削除する(string strInput, int nMode) {
		string str = "";
		str = strInput;

		unsafe {
			fixed (char* s = str) {
				for (int i = 0; i < str.Length; i++) {
					if (s[i] == '\t')
						s[i] = ' ';
					else if (s[i] == '\r')
						s[i] = '\n';
				}
			}
		}

		if (nMode == 0) {
			str = str.Replace("\n", " ");
		} else if (nMode == 1) {
			str = str + "\n";

			string[] strArray;
			strArray = str.Split(this.dlmtEnter, StringSplitOptions.RemoveEmptyEntries);

			return strArray;
		}

		return str;
	}

	/// <summary>
	/// コースごとに譜面を分割する。
	/// </summary>
	/// <param name="strTJA"></param>
	/// <returns>各コースの譜面(string[5])</returns>
	private string[] tコースで譜面を分割する(string strTJA) {
		string[] strCourseTJA = new string[(int)Difficulty.Total];

		if (strTJA.IndexOf("COURSE", 0) != -1) {
			//tja内に「COURSE」があればここを使う。
			string[] strTemp = strTJA.Split(this.dlmtCOURSE, StringSplitOptions.RemoveEmptyEntries);

			for (int n = 1; n < strTemp.Length; n++) {
				int nCourse = 0;
				string nNC = "";
				while (strTemp[n].Substring(0, 1) != "\n") //2017.01.29 DD COURSE単語表記に対応
				{
					nNC += strTemp[n].Substring(0, 1);
					strTemp[n] = strTemp[n].Remove(0, 1);
				}

				if (this.strConvertCourse(nNC) != -1) {
					nCourse = this.strConvertCourse(nNC);
					strCourseTJA[nCourse] = strTemp[n];
				}
			}
		} else {
			strCourseTJA[3] = strTJA;
		}

		return strCourseTJA;
	}

	// Regexes
	private static readonly Regex regexForStrippingHeadingLines = new Regex(
		@"^(?!(TITLE|LEVEL|BPM|WAVE|OFFSET|BALLOON|EXAM1|EXAM2|EXAM3|EXAM4|EXAM5|EXAM6|EXAM7|DANTICK|DANTICKCOLOR|RENREN22|RENREN23|RENREN32|RENREN33|RENREN42|RENREN43|BALLOONNOR|BALLOONEXP|BALLOONMAS|SONGVOL|SEVOL|SCOREINIT|SCOREDIFF|COURSE|STYLE|TOWERTYPE|GAME|LIFE|DEMOSTART|SIDE|SUBTITLE|SCOREMODE|GENRE|MAKER|SELECTBG|MOVIEOFFSET|BGIMAGE|BGMOVIE|HIDDENBRANCH|GAUGEINCR|LYRICFILE|#HBSCROLL|#BMSCROLL)).+\n",
		RegexOptions.Multiline | RegexOptions.Compiled);

	private int nDifficulty;

	/// <summary>
	/// 新型。
	/// ○未実装
	/// _「COURSE」定義が無い譜面は未対応
	/// 　→ver2015082200で対応完了。
	///
	/// </summary>
	/// <param name="strInput">譜面のデータ</param>
	private void t入力_V4(string strInput, int difficulty) {
		nDifficulty = difficulty;
		if (!String.IsNullOrEmpty(strInput)) //空なら通さない
		{
			//2017.02.03 DD ヘッダ内にある命令以外の文字列を削除
			var startIndex = strInput.IndexOf("#START");
			if (startIndex < 0) {
				Trace.TraceWarning($"#START命令が少なくとも1つは必要です。 ({strファイル名の絶対パス})");
			}
			string strInputHeader = strInput.Remove(startIndex);
			strInput = strInput.Remove(0, startIndex);
			strInput = strInputHeader + "\n" + strInput;

			//どうせ使わないので先にSplitしてコメントを削除。
			var strSplitした譜面 = (string[])this.str改行文字を削除する(strInput, 1);

			for (int i = 0; strSplitした譜面.Length > i; i++) {
				int idx = strSplitした譜面[i].IndexOf("//");
				if (idx >= 0)
					strSplitした譜面[i] = strSplitした譜面[i].Substring(0, idx);
			}
			//空のstring配列を詰める
			strSplitした譜面 = this.t空のstring配列を詰めたstring配列を返す(strSplitした譜面);

			#region[ヘッダ]

			//2015.05.21 kairera0467
			//ヘッダの読み込みは譜面全体から該当する命令を探す。
			//少し処理が遅くなる可能性はあるが、ここは正確性を重視する。
			//点数などの指定は後から各コースで行うので問題は無いだろう。

			//SplitしたヘッダのLengthの回数だけ、forで回して各種情報を読み取っていく。
			for (int i = 0; strSplitした譜面.Length > i; i++) {
				this.t入力_行解析ヘッダ(strSplitした譜面[i]);
			}
			#endregion

			#region[譜面]

			int n読み込むコース = 3;
			int n譜面数 = 0; //2017.07.22 kairera0467 tjaに含まれる譜面の数
			bool b新処理 = false;

			//まずはコースごとに譜面を分割。
			strSplitした譜面 = this.tコースで譜面を分割する(this.StringArrayToString(strSplitした譜面, "\n"));
			string strTest = "";
			//存在するかのフラグ作成。
			for (int i = 0; i < strSplitした譜面.Length; i++) {
				if (!String.IsNullOrEmpty(strSplitした譜面[i])) {
					this.b譜面が存在する[i] = true;
					n譜面数++;
				} else
					this.b譜面が存在する[i] = false;
			}

			#region[ 読み込ませるコースを決定 ]
			if (this.b譜面が存在する[difficulty] == false) {
				n読み込むコース = difficulty;
				n読み込むコース++;
				for (int n = 1; n < (int)Difficulty.Total; n++) {
					if (this.b譜面が存在する[n読み込むコース] == false) {
						n読み込むコース++;
						if (n読み込むコース > (int)Difficulty.Total - 1)
							n読み込むコース = 0;
					} else
						break;
				}
			} else
				n読み込むコース = difficulty;
			#endregion

			//指定したコースの譜面の命令を消去する。
			strSplitした譜面[n読み込むコース] = CDTXStyleExtractor.tセッション譜面がある(
				strSplitした譜面[n読み込むコース],
				OpenTaiko.ConfigIni.nPlayerCount > 1 ? (this.nPlayerSide + 1) : 0,
				this.strファイル名の絶対パス);

			//命令をすべて消去した譜面
			var str命令消去譜面 = strSplitした譜面[n読み込むコース].Split(this.dlmtEnter, StringSplitOptions.RemoveEmptyEntries);


			str命令消去譜面 = this.tコマンド行を削除したTJAを返す(str命令消去譜面, 2);

			//ここで1行の文字数をカウント。配列にして返す。
			var strSplit読み込むコース = strSplitした譜面[n読み込むコース].Split(this.dlmtEnter, StringSplitOptions.RemoveEmptyEntries);
			string str = "";
			try {
				if (n譜面数 > 0) {
					//2017.07.22 kairera0467 譜面が2つ以上ある場合はCOURSE以下のBALLOON命令を使う
					this.listBalloon.Clear();
					foreach (var listBalloon in this.listBalloon_Branch)
						listBalloon.Clear();
					for (int i = 0; i < listBalloon_Branch_数値管理.Length; ++i)
						this.listBalloon_Branch_数値管理[i] = 0;
				}

				for (int i = 0; i < strSplit読み込むコース.Length; i++) {
					if (!String.IsNullOrEmpty(strSplit読み込むコース[i])) {
						this.t難易度別ヘッダ(strSplit読み込むコース[i]);
					}
				}
				for (int i = 0; i < str命令消去譜面.Length; i++) {
					if (str命令消去譜面[i].IndexOf(',', 0) == -1 && !String.IsNullOrEmpty(str命令消去譜面[i])) {
						if (str命令消去譜面[i].Substring(0, 1) == "#") {
							this.t1小節の文字数をカウントしてリストに追加する(str + str命令消去譜面[i]);
						}

						if (NotesManager.FastFlankedParsing(str命令消去譜面[i]))//this.CharConvertNote(str命令消去譜面[i].Substring(0, 1)) != -1)
							str += str命令消去譜面[i];
					} else {
						this.t1小節の文字数をカウントしてリストに追加する(str + str命令消去譜面[i]);
						str = "";
					}
				}
			} catch (Exception ex) {
				Trace.TraceError(ex.ToString());
				Trace.TraceError("例外が発生しましたが処理を継続します。 (9e401212-0b78-4073-88d0-f7e791f36a91)");
			}

			//読み込み部分本体に渡す譜面を作成。
			//0:ヘッダー情報 1:#START以降 となる。個数の定義は後からされるため、ここでは省略。
			var strSplitした後の譜面 = strSplit読み込むコース; //strSplitした譜面[ n読み込むコース ].Split( this.dlmtEnter, StringSplitOptions.RemoveEmptyEntries );
			strSplitした後の譜面 = this.tコマンド行を削除したTJAを返す(strSplitした後の譜面, 1);
			this.n現在の小節数 = 1;
			try {
				for (int i = 0; strSplitした後の譜面.Length > i; i++) {
					nNowReadLine++;
					str = strSplitした後の譜面[i];
					this.t入力_行解析譜面_V4(str);
				}

				// Retrieve all the global exams (non individual) at the end
				if (DanSongs.Number > 0) {
					for (int i = 0; i < CExamInfo.cMaxExam; i++) {
						if (Dan_C[i] != null && List_DanSongs[0].Dan_C[i] == null) {
							List_DanSongs[0].Dan_C[i] = Dan_C[i];
						}
					}
				}

			} catch (Exception ex) {
				Trace.TraceError(ex.ToString());
				Trace.TraceError("例外が発生しましたが処理を継続します。 (2da1e880-6b63-4e82-b018-bf18c3568335)");
			}
			#endregion
		}
	}

	private CChip t発声位置から過去方向で一番近くにある指定チャンネルのチップを返す(int n発声時刻, int nチャンネル番号) {
		//過去方向への検索
		for (int i = this.listChip.Count - 1; i >= 0; i--) {
			if (this.listChip[i].nChannelNo == nチャンネル番号) {
				return this.listChip[i];
			}
		}

		return null;
	}

	//現在、以下のような行には対応できていません。
	//_パラメータを持つ命令がある
	//_行の途中に命令がある
	private int t文字数解析(string InputText) {
		int n文字数 = 0;

		for (int i = 0; i < InputText.Length; i++) {
			if (this.CharConvertNote(InputText.Substring(i, 1)) != -1) {
				n文字数++;
			}
		}


		return n文字数;
	}

	private static readonly Regex CommandAndArgumentRegex =
		new Regex(@"^(#[A-Z]+)(?:\s?)(.+?)?$", RegexOptions.Compiled);

	private static readonly Regex BranchStartArgumentRegex =
		new Regex(@"^([^,\s]+)\s*,\s*([^,\s]+)\s*,\s*([^,\s]+)$", RegexOptions.Compiled);

	private void AddError(string command, string argument) {
		listErrors.Add($"コメントアウトを除く{(Difficulty)nDifficulty}の{nNowReadLine}行目の{command}が正しくありません。値が{argument}になっています");
	}
	private void AddError_Single(string str) {
		listErrors.Add($"コメントアウトを除く{(Difficulty)nDifficulty}の{nNowReadLine}行目の{str}");
	}
	private void AddError(string str) {
		listErrors.Add(str);
	}

	private string[] SplitComma(string input) {
		var result = new List<string>();
		var workingIndex = 0;
		for (int i = 0; i < input.Length; i++) {
			if (input[i].Equals(',')) // カンマにぶち当たった
			{
				if (i - 1 >= 0 && input[i - 1].Equals('\\')) {
					input = input.Remove(i - 1, 1);
					--i;
				} else {
					// workingIndexから今の位置までをリストにブチ込む
					result.Add(input.Substring(workingIndex, i - workingIndex));
					// workingIndexに今の位置+1を代入
					workingIndex = i + 1;
				}
			}
			if (i + 1 == input.Length) // 最後に
			{
				result.Add(input.Substring(workingIndex, input.Length - workingIndex));
			}
		}
		return result.ToArray();
	}

	/// <summary>
	/// 譜面読み込みメソッドV4で使用。
	/// </summary>
	/// <param name="InputText"></param>
	private void t命令を挿入する(string InputText) {
		#region [Split comma and arguments values]
		var match = CommandAndArgumentRegex.Match(InputText);
		if (!match.Success) {
			return;
		}

		var command = match.Groups[1].Value;
		var argumentMatchGroup = match.Groups[2];
		var argument = argumentMatchGroup.Success ? argumentMatchGroup.Value : null;

		while (true) {//命令の最後に,が残ってしまっているときの対応
			if (argument != null && argument[argument.Length - 1] == ',')
				argument = argument.Substring(0, argument.Length - 1);
			else
				break;
		}

		char[] chDelimiter = new char[] { ' ' };
		string[] strArray = null;

		#endregion

		if (command == "#START") {
			InitializeChartDefinitionBody();
		} else if (command == "#END") {
			// TaikoJiro compatibility: #END ends unended rolls
			for (int i = 0; i < 3; i++) {
				if (this.nNowRollCountBranch[i] >= 0) {
					ECourse branch = (ECourse)i;
					if (branch == ECourse.eNormal || this.bHasBranch[this.n参照中の難易度]) {
						Trace.TraceWarning(this.bHasBranch[this.n参照中の難易度] ?
							$"{nameof(CTja)}: An unended roll in branch {branch} is ended by #END. In {this.strファイル名の絶対パス}"
							: $"{nameof(CTja)}: An unended roll is ended by #END. In {this.strファイル名の絶対パス}"
						);
					}
					InsertNoteAtDefCursor(8, 0, 1, branch);
				}
			}

			//ためしに割り込む。
			var chip = this.NewEventChipAtDefCursor(0xFF, 1, argInt: 0xFF);
			chip.n発声位置 = ((this.n現在の小節数 + 2) * 384);
			chip.n発声時刻ms = (int)(this.dbNowTime + 1000); //2016.07.16 kairera0467 終了時から1秒後に設置するよう変更。
			// チップを配置。

			if (n参照中の難易度 == (int)Difficulty.Dan) {
				for (int i = listChip.Count - 1; i >= 0; i--) {
					if (NotesManager.IsHittableNote(listChip[i])) {
						if (DanSongs.Number != 0) {
							Array.Resize(ref this.pDan_LastChip, this.pDan_LastChip.Length + 1);
							this.pDan_LastChip[DanSongs.Number - 1] = listChip[i];
							break;
						}
					}
				}
			}

			this.listChip.Add(chip);
		} else if (command == "#BPMCHANGE") {
			double dbBPM;
			if (!double.TryParse(argument, out dbBPM)) {
				AddError(command, argument);
				dbBPM = 150;
			}
			this.dbNowBPM = dbBPM;

			if (dbBPM > MaxBPM) {
				MaxBPM = dbBPM;
			} else if (dbBPM < MinBPM) {
				MinBPM = dbBPM;
			}

			this.listBPM.Add(this.n内部番号BPM1to - 1, new CBPM() { n内部番号 = this.n内部番号BPM1to - 1, n表記上の番号 = 0, dbBPM値 = dbBPM, bpm_change_time = this.dbNowTime, bpm_change_bmscroll_time = this.dbNowBMScollTime, bpm_change_course = this.n現在のコース });

			this.listChip.Add(this.NewEventChipAtDefCursor(0x08, this.n内部番号BPM1to - 1));
			this.listChip.Add(this.NewEventChipAtDefCursor(0x9C, this.n内部番号BPM1to - 1));

			this.n内部番号BPM1to++;
		} else if (command == "#SCROLL") {
			double[] dbComplexNum = new double[2];
			//2016.08.13 kairera0467 複素数スクロールもどきのテスト
			try {
				//iが入っていた場合、複素数スクロールとみなす。
				if (argument.IndexOf('i') != -1)
					this.tParsedComplexNumber(argument, ref dbComplexNum);
				else
					dbComplexNum[0] = double.Parse(argument);
			} catch (Exception ex) {
				AddError(command, argument);
				dbComplexNum[0] = 1.0;
				dbComplexNum[1] = 0.0;
			}

			this.dbNowScroll = dbComplexNum[0];
			this.dbNowScrollY = dbComplexNum[1];

			//チップ追加して割り込んでみる。
			var chip = this.NewEventChipAtDefCursor(0x9D);
			chip.n発声位置 -= 1;
			chip.dbSCROLL = dbComplexNum[0];
			chip.dbSCROLL_Y = dbComplexNum[1];
			chip.nBranch = this.n現在のコース;

			// チップを配置。

			this.listChip.Add(chip);
		} else if (command == "#MEASURE") {
			strArray = argument.Split(new char[] { '/' });
			WarnSplitLength("#MEASURE subsplit", strArray, 2);

			double[] dbLength = new double[2];
			try {
				dbLength[0] = Convert.ToDouble(strArray[0]);
				dbLength[1] = Convert.ToDouble(strArray[1]);
			} catch (Exception ex) {
				AddError(command, argument);
			}

			double db小節長倍率 = dbLength[0] / dbLength[1];
			this.fNow_Measure_m = (float)dbLength[1];
			this.fNow_Measure_s = (float)dbLength[0];

			this.listChip.Add(this.NewEventChipAtDefCursor(0x02, 1, argDb: db小節長倍率));
		} else if (command == "#DELAY") {
			double nDELAY = 0;
			if (!double.TryParse(argument, out nDELAY)) {
				AddError(command, argument);
				nDELAY = 0;
			}
			nDELAY *= 1000;

			//チップ追加して割り込んでみる。
			var chip = this.NewEventChipAtDefCursor(0xDC);
			chip.nBranch = this.n現在のコース;
			// チップを配置。

			this.dbNowTime += nDELAY;
			this.dbNowBMScollTime += nDELAY * this.dbNowBPM / 15000;

			this.listChip.Add(chip);
		} else if (command == "#GOGOSTART") {
			this.bGOGOTIME = true;
			this.listChip.Add(this.NewEventChipAtDefCursor(0x9E, 1));
		} else if (command == "#GOGOEND") {
			this.bGOGOTIME = false;
			this.listChip.Add(this.NewEventChipAtDefCursor(0x9F, 1));
		} else if (command == "#BGAON") {
			try {
				var commandData = argument.Split(' ');
				string listvdIndex = commandData[0];
				var bgaStartTime = commandData[1];
				int index = (10 * int.Parse(listvdIndex[0].ToString())) + int.Parse(listvdIndex[1].ToString()) + 2;

				var chip = this.NewEventChipAtDefCursor(0x54, index, index);
				chip.VideoStartTimeMs = (int)(float.Parse(bgaStartTime) * 1000);
				this.listChip.Add(chip);
			} catch (Exception ex) {
				AddError(command, argument);
			}
		} else if (command == "#BGAOFF") {
			int index = (10 * int.Parse(argument[0].ToString())) + int.Parse(argument[1].ToString()) + 2;
			this.listChip.Add(this.NewEventChipAtDefCursor(0x55, index, index));
		} else if (command == "#CAMVMOVESTART") {
			//starts vertical camera moving: <start x> to <end y>
			this.ParseArgCamStartCommand(command, argument, 0xA0, ref this.currentCamVMoveChip,
				(chip, start) => chip.fCamScrollStartY = start, (chip, end) => chip.fCamScrollEndY = end,
				"#CAMVMOVEEND");
		} else if (command == "#CAMVMOVEEND") {
			//ends vertical camera moving
			this.ParseArgCamEndCommand(command, argument, 0xA1, ref this.currentCamVMoveChip, "#CAMVMOVESTART");
		} else if (command == "#CAMHMOVESTART") {
			//starts horizontal camera moving: <start x> to <end x>
			this.ParseArgCamStartCommand(command, argument, 0xA2, ref this.currentCamHMoveChip,
				(chip, start) => chip.fCamScrollStartX = start, (chip, end) => chip.fCamScrollEndX = end,
				"#CAMHMOVEEND");
		} else if (command == "#CAMHMOVEEND") {
			//ends horizontal camera moving
			this.ParseArgCamEndCommand(command, argument, 0xA3, ref this.currentCamHMoveChip, "#CAMHMOVESTART");
		} else if (command == "#CAMZOOMSTART") {
			//starts zooming in/out the screen: <start value> to <end value>
			this.ParseArgCamStartCommand(command, argument, 0xA4, ref this.currentCamZoomChip,
				(chip, start) => chip.fCamZoomStart = start, (chip, end) => chip.fCamZoomEnd = end,
				"#CAMZOOMEND");
		} else if (command == "#CAMZOOMEND") {
			//stops zooming
			this.ParseArgCamEndCommand(command, argument, 0xA5, ref this.currentCamZoomChip, "#CAMZOOMSTART");
		} else if (command == "#CAMROTATIONSTART") {
			//starts rotating the screen: <start degrees> to <end degrees>
			this.ParseArgCamStartCommand(command, argument, 0xA6, ref this.currentCamRotateChip,
				(chip, start) => chip.fCamRotationStart = start, (chip, end) => chip.fCamRotationEnd = end,
				"#CAMROTATIONEND");
		} else if (command == "#CAMROTATIONEND") {
			//stops screen rotation
			this.ParseArgCamEndCommand(command, argument, 0xA7, ref this.currentCamRotateChip, "#CAMROTATIONSTART");
		} else if (command == "#CAMVSCALESTART") {
			//starts rotating the screen: <start degrees> to <end degrees>
			this.ParseArgCamStartCommand(command, argument, 0xA8, ref this.currentCamVScaleChip,
				(chip, start) => chip.fCamScaleStartY = start, (chip, end) => chip.fCamScaleEndY = end,
				"#CAMVSCALEEND");
		} else if (command == "#CAMVSCALEEND") {
			//ends vertical camera scaling
			this.ParseArgCamEndCommand(command, argument, 0xA9, ref this.currentCamVScaleChip, "#CAMVSCALESTART");
		} else if (command == "#CAMHSCALESTART") {
			//starts horizontal camera scale changing: <start scale> to <end scale>
			this.ParseArgCamStartCommand(command, argument, 0xB0, ref this.currentCamHScaleChip,
				(chip, start) => chip.fCamScaleStartX = start, (chip, end) => chip.fCamScaleEndX = end,
				"#CAMHSCALEEND");
		} else if (command == "#CAMHSCALEEND") {
			//ends horizontal camera scaling
			this.ParseArgCamEndCommand(command, argument, 0xB1, ref this.currentCamHScaleChip, "#CAMHSCALESTART");
		} else if (command == "#BORDERCOLOR") {
			//sets border color
			//arguments: <r>,<g>,<b>
			var chip = this.NewEventChipAtDefCursor(0xB2, 1);

			string[] args = argument.Split(',');
			chip.borderColor = new Color4(1f, float.Parse(args[0]) / 255, float.Parse(args[1]) / 255, float.Parse(args[2]) / 255);

			// チップを配置。
			this.listChip.Add(chip);
		} else if (command == "#CAMHOFFSET") {
			//sets camera x offset: <offset>
			this.ParseArgCamSetCommand(command, argument, 0xB3, this.currentCamHMoveChip,
				(chip, value) => chip.fCamScrollStartX = chip.fCamScrollEndX = value,
				"#CAMHMOVEEND");
		} else if (command == "#CAMVOFFSET") {
			//sets camera y offset: <offset>
			this.ParseArgCamSetCommand(command, argument, 0xB4, this.currentCamVMoveChip,
				(chip, value) => chip.fCamScrollStartY = chip.fCamScrollEndY = value,
				"#CAMVMOVEEND");
		} else if (command == "#CAMZOOM") {
			//sets camera zoom factor: <zoom factor>
			this.ParseArgCamSetCommand(command, argument, 0xB5, this.currentCamZoomChip,
				(chip, value) => chip.fCamZoomStart = chip.fCamZoomEnd = value,
				"#CAMZOOMEND");
		} else if (command == "#CAMROTATION") {
			//sets camera rotation: <degrees>
			this.ParseArgCamSetCommand(command, argument, 0xB6, this.currentCamRotateChip,
				(chip, value) => chip.fCamRotationStart = chip.fCamRotationEnd = value,
				"#CAMROTATIONEND");
		} else if (command == "#CAMHSCALE") {
			//sets camera x scale: <scale>
			this.ParseArgCamSetCommand(command, argument, 0xB7, this.currentCamHScaleChip,
				(chip, value) => chip.fCamScaleStartX = chip.fCamScaleEndX = value,
				"#CAMHSCALEEND");
		} else if (command == "#CAMVSCALE") {
			//sets camera y scale: <scale>
			this.ParseArgCamSetCommand(command, argument, 0xB8, this.currentCamVScaleChip,
				(chip, value) => chip.fCamScaleStartY = chip.fCamScaleEndY = value,
				"#CAMVSCALEEND");
		} else if (command == "#CAMRESET") {
			//resets camera properties
			var chip = this.NewEventChipAtDefCursor(0xB9, 1);

			chip.fCamScrollStartX = 0.0f;
			chip.fCamScrollEndX = 0.0f;
			chip.fCamScrollStartY = 0.0f;
			chip.fCamScrollEndY = 0.0f;

			chip.fCamZoomStart = 1.0f;
			chip.fCamZoomEnd = 1.0f;
			chip.fCamRotationStart = 0.0f;
			chip.fCamRotationEnd = 0.0f;

			chip.fCamScaleStartX = 1.0f;
			chip.fCamScaleEndX = 1.0f;
			chip.fCamScaleStartY = 1.0f;
			chip.fCamScaleEndY = 1.0f;

			chip.strCamEaseType = "IN_OUT";

			// チップを配置。
			this.listChip.Add(chip);
		} else if (command == "#ENABLEDORON") {
			this.listChip.Add(this.NewEventChipAtDefCursor(0xBA, 1));
		} else if (command == "#DISABLEDORON") {
			this.listChip.Add(this.NewEventChipAtDefCursor(0xBB, 1));
		} else if (command == "#ADDOBJECT") {
			//adds object
			var chip = this.NewEventChipAtDefCursor(0xBC, 1);

			try {
				string[] args = argument.Split(',');

				chip.strObjName = args[0];
				chip.fObjX = float.Parse(args[1]);
				chip.fObjY = float.Parse(args[2]);
				var txPath = this.strフォルダ名 + args[3];
				Trace.TraceInformation("" + this.bSession譜面を読み込む);
				if (this.bSession譜面を読み込む) {
					var obj = new CSongObject(chip.strObjName, chip.fObjX, chip.fObjY, txPath);
					this.listObj.Add(args[0], obj);
				}

				// チップを配置。
				this.listChip.Add(chip);
			} catch (Exception ex) {
				AddError(command, argument);
			}
		} else if (command == "#REMOVEOBJECT") {
			//removes object
			var chip = this.NewEventChipAtDefCursor(0xBD, 1);
			chip.strObjName = argument;
			this.listChip.Add(chip);
		} else if (command == "#OBJVMOVESTART") {
			//starts vertical object movement: <start y> to <end y>
			this.ParseArgObjStartCommand(command, argument, 0xBE, "vmove", "#OBJVMOVEEND");
		} else if (command == "#OBJVMOVEEND") {
			//ends vertical camera moving
			this.ParseArgObjEndCommand(command, argument, 0xBF, "vmove", "#OBJVMOVESTART");
		} else if (command == "#OBJHMOVESTART") {
			//starts horizontal object movement: <start x> to <end x>
			this.ParseArgObjStartCommand(command, argument, 0xC0, "hmove", "#OBJHMOVEEND");
		} else if (command == "#OBJHMOVEEND") {
			//ends horizontal camera moving
			this.ParseArgObjEndCommand(command, argument, 0xC1, "hmove", "#OBJHMOVESTART");
		} else if (command == "#OBJVSCALESTART") {
			this.ParseArgObjStartCommand(command, argument, 0xC2, "vscale", "#OBJVSCALEEND");
		} else if (command == "#OBJVSCALEEND") {
			this.ParseArgObjEndCommand(command, argument, 0xC3, "vscale", "#OBJVSCALESTART");
		} else if (command == "#OBJHSCALESTART") {
			this.ParseArgObjStartCommand(command, argument, 0xC4, "hscale", "#OBJHSCALEEND");
		} else if (command == "#OBJHSCALEEND") {
			this.ParseArgObjEndCommand(command, argument, 0xC5, "hscale", "#OBJHSCALESTART");
		} else if (command == "#OBJROTATIONSTART") {
			this.ParseArgObjStartCommand(command, argument, 0xC6, "rotation", "#OBJROTATIONEND");
		} else if (command == "#OBJROTATIONEND") {
			this.ParseArgObjEndCommand(command, argument, 0xC7, "rotation", "#OBJROTATIONSTART");
		} else if (command == "#OBJOPACITYSTART") {
			this.ParseArgObjStartCommand(command, argument, 0xC8, "opacity", "#OBJOPACITYEND");
		} else if (command == "#OBJOPACITYEND") {
			this.ParseArgObjEndCommand(command, argument, 0xC9, "opacity", "#OBJOPACITYSTART");
		} else if (command == "#OBJCOLOR") {
			var chip = this.NewEventChipAtDefCursor(0xCA, 1);

			try {
				string[] args = argument.Split(',');
				chip.strObjName = args[0];
				chip.borderColor = new Color4(1f, float.Parse(args[1]) / 255, float.Parse(args[2]) / 255, float.Parse(args[3]) / 255);

				// チップを配置。
				this.listChip.Add(chip);
			} catch (Exception ex) {
				AddError(command, argument);
			}
		} else if (command == "#OBJY") {
			this.ParseArgObjSetCommand(command, argument, 0xCB, "vmove", "#OBJVMOVEEND");
		} else if (command == "#OBJX") {
			this.ParseArgObjSetCommand(command, argument, 0xCC, "hmove", "#OBJHMOVEEND");
		} else if (command == "#OBJVSCALE") {
			this.ParseArgObjSetCommand(command, argument, 0xCD, "vscale", "#OBJVSCALEEND");
		} else if (command == "#OBJHSCALE") {
			this.ParseArgObjSetCommand(command, argument, 0xCE, "hscale", "#OBJHSCALEEND");
		} else if (command == "#OBJROTATION") {
			this.ParseArgObjSetCommand(command, argument, 0xCF, "rotation", "#OBJROTATIONEND");
		} else if (command == "#OBJOPACITY") {
			this.ParseArgObjSetCommand(command, argument, 0xD0, "opacity", "#OBJOPACITYEND");
		} else if (command == "#CHANGETEXTURE") {
			var chip = this.NewEventChipAtDefCursor(0xD1, 1);

			string[] args = argument.Split(',');
			try {
				chip.strTargetTxName = args[0]
					.Replace('/', Path.DirectorySeparatorChar)
					.Replace('\\', Path.DirectorySeparatorChar);
				chip.strNewPath = this.strフォルダ名 + args[1];

				if (this.bSession譜面を読み込む) {
					if (!this.listOriginalTextures.ContainsKey(chip.strTargetTxName)) {
						OpenTaiko.Tx.trackedTextures.TryGetValue(chip.strTargetTxName, out CTexture oldTx);
						this.listOriginalTextures.Add(chip.strTargetTxName, new CTexture(oldTx));
					}
					if (!this.listTextures.ContainsKey(chip.strNewPath)) {
						CTexture tx = OpenTaiko.Tx.TxCSong(chip.strNewPath);
						this.listTextures.Add(chip.strNewPath, tx);
					}
				}

				// チップを配置。
				this.listChip.Add(chip);
			} catch (Exception ex) {
				AddError(command, argument);
			}
		} else if (command == "#RESETTEXTURE") {
			var chip = this.NewEventChipAtDefCursor(0xD2, 1);
			chip.strTargetTxName = argument
				.Replace('/', Path.DirectorySeparatorChar)
				.Replace('\\', Path.DirectorySeparatorChar);
			this.listChip.Add(chip);
		} else if (command == "#SETCONFIG") {
			var chip = this.NewEventChipAtDefCursor(0xD3, 1);
			chip.strConfigValue = argument;
			this.listChip.Add(chip);
		} else if (command == "#OBJANIMSTART") {
			var chip = this.NewEventChipAtDefCursor(0xD4, 1);

			string[] args = argument.Split(',');
			try {
				chip.strObjName = args[0];
				chip.dbAnimInterval = double.Parse(args[1]);

				// チップを配置。
				this.listChip.Add(chip);
			} catch (Exception ex) {
				AddError(command, argument);
			}
		} else if (command == "#OBJANIMSTARTLOOP") {
			var chip = this.NewEventChipAtDefCursor(0xD5, 1);

			string[] args = argument.Split(',');
			try {
				chip.strObjName = args[0];
				chip.dbAnimInterval = double.Parse(args[1]);

				// チップを配置。
				this.listChip.Add(chip);
			} catch (Exception ex) {
				AddError(command, argument);
			}
		} else if (command == "#OBJANIMEND") {
			var chip = this.NewEventChipAtDefCursor(0xD6, 1);
			chip.strObjName = argument;
			this.listChip.Add(chip);
		} else if (command == "#OBJFRAME") {
			var chip = this.NewEventChipAtDefCursor(0xD7, 1);

			string[] args = argument.Split(',');
			try {
				chip.strObjName = args[0];
				chip.intFrame = int.Parse(args[1]);

				// チップを配置。
				this.listChip.Add(chip);
			} catch (Exception ex) {
				AddError(command, argument);
			}
		} else if (command == "#GAMETYPE") {
			CChip chip = this.NewEventChipAtDefCursor(0xD8, 1);
			chip.eGameType = argument switch {
				"Taiko" => EGameType.Taiko,
				"Bongo" or "Konga" => EGameType.Konga,
				_ => chip.eGameType,
			};

			// チップを配置。
			this.listChip.Add(chip);
		} else if (command == "#SPLITLANE") {
			this.listChip.Add(this.NewEventChipAtDefCursor(0xD9, 1));
		} else if (command == "#MERGELANE") {
			this.listChip.Add(this.NewEventChipAtDefCursor(0xE3, 1));
		} else if (command == "#BARLINE") {
			var chip = this.NewEventChipAtDefCursor(0xE4, 1);
			chip.dbSCROLL_Y = this.dbNowScrollY;
			chip.bHideBarLine = false;
			this.listChip.Add(chip);
			this.listBarLineChip.Add(chip);
		} else if (command == "#SECTION") {
			this.listChip.Add(this.NewEventChipAtDefCursor(0xDD, 1)); //分岐:条件リセット
		} else if (command == "#BRANCHSTART") {
			//分岐:分岐スタート
			#region [ 譜面分岐のパース方法を作り直し ]
			this.bチップがある.Branch = true;
			this.b最初の分岐である = false;
			this.b分岐を一回でも開始した = true;

			//条件数値。
			double[] nNum = new double[2];

			//名前と条件Aの間に,が無いと正常に動作しなくなる.2020.04.23.akasoko26
			#region [ 名前と条件Aの間に,が無いと正常に動作しなくなる ]
			//空白を削除する。
			argument = Regex.Replace(argument, @"\s", "");
			//2文字目が,か数値かをチェック
			var IsNumber = bIsNumber(argument[1]);
			//IsNumber == true であったら,が無いということなので,を2文字目にぶち込む・・・
			if (IsNumber)
				argument = argument.Insert(1, ",");
			#endregion

			var branchStartArgumentMatch = BranchStartArgumentRegex.Match(argument);
			nNum[0] = Convert.ToDouble(branchStartArgumentMatch.Groups[2].Value);
			nNum[1] = Convert.ToDouble(branchStartArgumentMatch.Groups[3].Value);

			var e条件 = branchStartArgumentMatch.Groups[1].Value switch {
				"r" => EBranchConditionType.Drumroll,
				"s" => EBranchConditionType.Score,
				"d" => EBranchConditionType.Accuracy_BigNotesOnly,
				"p" or _ => EBranchConditionType.Accuracy,
			};

			#region [ 分岐開始時のチップ情報を記録 ]
			//現在のチップ情報を記録する必要がある。
			this.t現在のチップ情報を記録する(true);
			#endregion

			#region [ 一小節前の分岐開始Chip ]
			//16分前に戻す計算なんか当てにしちゃだめよ。。(by Akasoko)
			var c小節前の小節線情報 = c一小節前の小節線情報を返す(listChip, e条件);
			CChip c小節前の連打開始位置 = null;

			var chip = new CChip();

			if (e条件 == EBranchConditionType.Drumroll) {
				/*
				c小節前の連打開始位置 = c一小節前の小節線情報を返す(listChip, e条件, true);
				//連打分岐の位置を再現
				//この計算式はあてにならないと思うが、まあどうしようもないんでこれで
				//なるべく連打のケツの部分に
				var f連打の長さの半分 = (c小節前の小節線情報.n発声時刻ms - c小節前の連打開始位置.n発声時刻ms) / 2.0f;
				*/

				chip.n発声時刻ms = c小節前の小節線情報.n発声時刻ms;
			} else chip.n発声時刻ms = c小節前の小節線情報.n発声時刻ms;

			chip.nChannelNo = 0xDE;
			chip.fNow_Measure_m = c小節前の小節線情報.fNow_Measure_m;
			chip.fNow_Measure_s = c小節前の小節線情報.fNow_Measure_s;

			//ノーツ * 0.5分後ろにして、ノーツが残らないようにする
			chip.n分岐時刻ms = this.dbNowTime - ((15000.0 / this.dbNowBPM * (this.fNow_Measure_s / this.fNow_Measure_m)) * 0.5);
			chip.eBranchCondition = e条件;
			chip.nBranchCondition1_Professional = nNum[0];// listに追加していたが仕様を変更。
			chip.nBranchCondition2_Master = nNum[1];// ""
			chip.dbSCROLL = c小節前の小節線情報.dbSCROLL;
			chip.dbBPM = c小節前の小節線情報.dbBPM;
			this.listChip.Add(chip);
			#endregion

			for (int i = 0; i < 3; i++)
				IsBranchBarDraw[i] = true;//3コース分の黄色小説線表示㋫ラブ

			IsEndedBranching = false;
			#endregion
		} else if (command == "#N" || command == "#E" || command == "#M")//これCourseを全部集めてあとから分岐させればいい件
		{
			//開始時の情報にセット
			t現在のチップ情報を記録する(false);

			if (command == "#N")
				this.n現在のコース = ECourse.eNormal;//分岐:普通譜面
			else if (command == "#E")
				this.n現在のコース = ECourse.eExpert;//分岐:玄人譜面
			else if (command == "#M")
				this.n現在のコース = ECourse.eMaster;//分岐:達人譜面
		} else if (command == "#LEVELHOLD") {
			var chip = this.NewEventChipAtDefCursor(0xE1, 1);
			chip.n発声位置 -= 1;
			this.listChip.Add(chip);
		} else if (command == "#BRANCHEND") {
			//End用チャンネルをEmptyから引っ張ってきた。
			var GoBranch = this.NewEventChipAtDefCursor(0x52, 1);
			GoBranch.n発声位置 -= 1;
			this.listChip.Add(GoBranch);

			//End時にも黄色い小節線あったべ？
			for (int i = 0; i < 3; i++)
				IsBranchBarDraw[i] = true;//3コース分の黄色小説線表示㋫ラブ

			IsEndedBranching = true;
		} else if (command == "#BARLINEOFF") {
			var chip = this.NewEventChipAtDefCursor(0xE0, 1);
			chip.n発声位置 -= 1;
			chip.n発声時刻ms += 1;
			chip.nBranch = this.n現在のコース;
			this.bBARLINECUE[0] = 1;

			this.listChip.Add(chip);
		} else if (command == "#BARLINEON") {
			var chip = this.NewEventChipAtDefCursor(0xE0, 2);
			chip.n発声位置 -= 1;
			chip.n発声時刻ms += 1;
			chip.nBranch = this.n現在のコース;
			this.bBARLINECUE[0] = 0;

			this.listChip.Add(chip);
		} else if (command == "#LYRIC" && !usingLyricsFile && OpenTaiko.ConfigIni.nPlayerCount < 4) // Do not parse LYRIC tags if a lyric file is already loaded
		{
			if (OpenTaiko.rCurrentStage.eStageID == CStage.EStage.SongLoading)//起動時に重たくなってしまう問題の修正用
				this.listLyric.Add(this.pf歌詞フォント.DrawText(argument, OpenTaiko.Skin.Game_Lyric_ForeColor, OpenTaiko.Skin.Game_Lyric_BackColor, null, 30));

			var chip = this.NewEventChipAtDefCursor(0xF1, this.listLyric.Count - 1);
			chip.nBranch = this.n現在のコース;
			this.listChip.Add(chip);
			this.bLyrics = true;
		} else if (command == "#DIRECTION") {
			double dbSCROLL = Convert.ToDouble(argument);
			this.nスクロール方向 = (int)dbSCROLL;

			//チップ追加して割り込んでみる。
			var chip = this.NewEventChipAtDefCursor(0xF2, 0);
			chip.n発声位置 -= 1;
			chip.nScrollDirection = (int)dbSCROLL;
			chip.nBranch = this.n現在のコース;

			// チップを配置。

			this.listChip.Add(chip);
		} else if (command == "#SUDDEN") {
			strArray = argument.Split(chDelimiter);
			WarnSplitLength("#SUDDEN", strArray, 2);
			double db出現時刻 = Convert.ToDouble(strArray[0]);
			double db移動待機時刻 = Convert.ToDouble(strArray[1]);
			this.db出現時刻 = db出現時刻;
			this.db移動待機時刻 = db移動待機時刻;

			//チップ追加して割り込んでみる。
			var chip = this.NewEventChipAtDefCursor(0xF3, 0);
			chip.n発声位置 -= 1;
			chip.nノーツ出現時刻ms = (int)this.db出現時刻;
			chip.nノーツ移動開始時刻ms = (int)this.db移動待機時刻;
			chip.nBranch = this.n現在のコース;

			// チップを配置。

			this.listChip.Add(chip);
		} else if (command == "#JPOSSCROLL") {
			strArray = argument.Split(chDelimiter);
			WarnSplitLength("#JPOSSCROLL", strArray, 2);
			double msMoveDt = double.Max(0, 1000 * Convert.ToDouble(strArray[0]));
			double pxMoveDx = 0;
			double pxMoveDy = 0;
			if (strArray[1].IndexOf('i') != -1) {
				double[] dbComplexNum = new double[2];
				this.tParsedComplexNumber(strArray[1], ref dbComplexNum);
				pxMoveDx = dbComplexNum[0];
				pxMoveDy = dbComplexNum[1];
			} else
				pxMoveDx = Convert.ToDouble(strArray[1]);


			int n移動方向 = (strArray.Length >= 3) ? Convert.ToInt32(strArray[2]) : 0;
			if (n移動方向 == 0) { // same move direction as notes in `#SCROLL x+yi`
				pxMoveDx = -pxMoveDx;
				pxMoveDy = -pxMoveDy;
			}

			//チップ追加して割り込んでみる。
			var chip = this.NewEventChipAtDefCursor(0xE2, this.listJPOSSCROLL.Count);
			chip.n発声位置 -= 1;
			chip.nBranch = this.n現在のコース;

			// チップを配置。
			this.listJPOSSCROLL.Add(new CJPOSSCROLL() {
				n内部番号 = this.listJPOSSCROLL.Count,
				n表記上の番号 = 0,
				msMoveDt = msMoveDt,
				pxMoveDx = pxMoveDx,
				pxMoveDy = pxMoveDy,
				chip = chip,
			});
			this.listChip.Add(chip);
		} else if (command == "#SENOTECHANGE") {
			FixSENote = int.Parse(argument);
			IsEnabledFixSENote = true;
		} else if (command == "#NEXTSONG") {
			var chip = this.NewEventChipAtDefCursor(0x9B, List_DanSongs.Count);
			chip.n発声位置 -= ((this.n現在の小節数) * 384) - 1;
			chip.nBranch = this.n現在のコース;
			this.listChip.Add(chip);

			// 6.2秒ディレイ
			this.dbNowTime += msDanNextSongDelay;
			this.dbNowBMScollTime += msDanNextSongDelay * this.dbNowBPM / 15000;

			AddPreBakedMusicPreTimeMs(); // 段位の幕が開いてからの遅延。

			strArray = SplitComma(argument); // \,をエスケープ処理するメソッドだぞっ

			for (int i = listChip.Count - 1; i >= 0; i--) {
				//if (listChip[i].nチャンネル番号 >= 0x11 && listChip[i].nチャンネル番号 <= 0x18)
				if (NotesManager.IsHittableNote(listChip[i])) {
					if (DanSongs.Number != 0) {
						Array.Resize(ref this.pDan_LastChip, this.pDan_LastChip.Length + 1);
						this.pDan_LastChip[DanSongs.Number - 1] = listChip[i];
						break;
					}
				}
			}

			WarnSplitLength("#NEXTSONG", strArray, 8);
			var dansongs = new DanSongs();
			dansongs.Title = strArray[0];
			dansongs.SubTitle = strArray[1];
			dansongs.Genre = strArray[2];
			dansongs.FileName = strArray[3];
			dansongs.ScoreInit = int.Parse(strArray[4]);
			dansongs.ScoreDiff = int.Parse(strArray[5]);

			if (strArray.Length >= 7 && strArray[6] != "" && strArray[6] != null)
				dansongs.Level = int.Parse(strArray[6]);
			else if (strArray.Length < 7)
				dansongs.Level = 10;

			if (strArray.Length >= 8 && strArray[7] != "" && strArray[7] != null)
				dansongs.Difficulty = strConvertCourse(strArray[7]);
			else if (strArray.Length < 8)
				dansongs.Difficulty = 3;

			if (strArray.Length == 9 && strArray[8] != "" && strArray[8] != null)
				dansongs.bTitleShow = bool.Parse(strArray[8]);
			else if (strArray.Length < 9)
				dansongs.bTitleShow = false;

			dansongs.Wave = new CWAV {
				n内部番号 = this.n内部番号WAV1to,
				n表記上の番号 = this.n内部番号WAV1to,
				nチップサイズ = this.n無限管理SIZE[this.n内部番号WAV1to],
				n位置 = this.n無限管理PAN[this.n内部番号WAV1to],
				SongVol = this.SongVol,
				SongLoudnessMetadata = this.SongLoudnessMetadata,
				strファイル名 = CDTXCompanionFileFinder.FindFileName(this.strフォルダ名, strファイル名, dansongs.FileName),
				strコメント文 = "TJA BGM"
			};
			dansongs.Wave.SongLoudnessMetadata = LoudnessMetadataScanner.LoadForAudioPath(dansongs.Wave.strファイル名);
			List_DanSongs.Add(dansongs);
			this.listWAV.Add(this.n内部番号WAV1to, dansongs.Wave);
			this.n内部番号WAV1to++;

			this.listWAV[1].strファイル名 = "";

			Array.Resize(ref bHasBranchDan, List_DanSongs.Count);
			bHasBranchDan[bHasBranchDan.Length - 1] = false;

			// チップを配置。
			this.listChip.Add(this.NewEventChipAtDefCursor(0x01, 1 + List_DanSongs.Count, 0x01));
		} else if (command == "#NMSCROLL") {
			eScrollMode = EScrollMode.Normal;

			var chip = this.NewEventChipAtDefCursor(0x09);
			chip.n発声位置 -= 1;
			chip.nBranch = this.n現在のコース;
			this.listChip.Add(chip);
		} else if (command == "#BMSCROLL") {
			eScrollMode = EScrollMode.BMScroll;

			var chip = this.NewEventChipAtDefCursor(0x0A);
			chip.n発声位置 -= 1;
			chip.nBranch = this.n現在のコース;
			this.listChip.Add(chip);
		} else if (command == "#HBSCROLL") {
			eScrollMode = EScrollMode.HBScroll;

			var chip = this.NewEventChipAtDefCursor(0x0B);
			chip.n発声位置 -= 1;
			chip.nBranch = this.n現在のコース;
			this.listChip.Add(chip);
		}
	}

	private void ParseArgCamSetCommand(string command, string argument, int channelNo, CChip? camChip, Action<CChip, float> setValue, string commandEnd) {
		if (camChip == null) {
			var chip = this.NewEventChipAtDefCursor(channelNo, 1);

			if (float.TryParse(argument, out float value)) {
				setValue(chip, value);
			} else {
				AddError(command, argument);
			}
			chip.strCamEaseType = "IN_OUT";

			// チップを配置。
			this.listChip.Add(chip);
		} else {
			AddError_Single($"Missing {commandEnd}");
			Trace.TraceInformation($"TJA ERROR: Missing {commandEnd}");
		}
	}

	private void ParseArgCamStartCommand(string command, string argument, int channelNo, ref CChip? camChip,
		Action<CChip, float> setStart, Action<CChip, float> setEnd,
		string commandEnd
	) {
		if (camChip == null) {
			//starts camera attribute changing
			//arguments: <start value>,<end value>,<easing type>,<calc type>
			var chip = this.NewEventChipAtDefCursor(channelNo, 0);

			try {
				string[] args = argument.Split(',');
				setStart(chip, float.Parse(args[0]));
				setEnd(chip, float.Parse(args[1]));
				chip.strCamEaseType = args[2];
				chip.fCamMoveType = TjaArgToEasingCalcType(args[3]);

				camChip = chip;

				// チップを配置。
				this.listChip.Add(chip);
			} catch (Exception ex) {
				AddError(command, argument);
			}
		} else {
			AddError_Single($"Missing {commandEnd}");
			Trace.TraceInformation($"TJA ERROR: Missing {commandEnd}");
		}
	}

	private void ParseArgCamEndCommand(string command, string argument, int channelNo, ref CChip? camChip, string commandStart) {
		if (camChip != null) {
			//ends camera attribute changing
			var chip = this.NewEventChipAtDefCursor(channelNo, 1);

			var index = this.listChip.IndexOf(camChip);
			var msDiff = chip.n発声時刻ms - camChip.n発声時刻ms;

			camChip.fCamTimeMs = msDiff;
			this.listChip[index] = camChip;

			camChip = null;

			// チップを配置。
			this.listChip.Add(chip);
		} else {
			AddError_Single($"Missing {commandStart}");
			Trace.TraceInformation($"TJA ERROR: Missing {commandStart}");
		}
	}

	private void ParseArgObjSetCommand(string command, string argument, int channelNo, string animationKey, string commandEnd) {
		string[] args = argument.Split(',');
		try {
			string name = args[0];

			if (!currentObjAnimations.ContainsKey($"{animationKey}_{name}")) {
				var chip = this.NewEventChipAtDefCursor(channelNo, 0);
				chip.strObjName = args[0];
				chip.fObjStart = float.Parse(args[1]);
				chip.fObjEnd = float.Parse(args[1]);
				chip.strObjEaseType = "IN_OUT";

				// チップを配置。
				this.listChip.Add(chip);
			} else {
				AddError_Single($"Missing {commandEnd}");
				Trace.TraceInformation($"TJA ERROR: Missing {commandEnd}");
			}
		} catch (Exception ex) {
			AddError(command, argument);
		}
	}

	private void ParseArgObjStartCommand(string command, string argument, int channelNo, string animationKey, string commandEnd) {
		string[] args = argument.Split(',');

		try {
			string name = args[0];

			if (!currentObjAnimations.ContainsKey($"{animationKey}_{name}")) {
				//starts attribute changing
				//arguments: <start value>,<end value>,<easing type>,<calc type>
				var chip = this.NewEventChipAtDefCursor(channelNo, 0);
				chip.strObjName = args[0];
				chip.fObjStart = float.Parse(args[1]);
				chip.fObjEnd = float.Parse(args[2]);
				chip.strObjEaseType = args[3];
				chip.objCalcType = TjaArgToEasingCalcType(args[4]);

				currentObjAnimations.Add($"{animationKey}_{name}", chip);

				// チップを配置。
				this.listChip.Add(chip);
			} else {
				AddError_Single($"Missing {commandEnd}");
				Trace.TraceInformation($"TJA ERROR: Missing {commandEnd}");
			}
		} catch (Exception ex) {
			AddError(command, argument);
		}
	}

	private void ParseArgObjEndCommand(string command, string argument, int channelNo, string animationKey, string commandStart) {
		string name = argument;

		if (currentObjAnimations.ContainsKey($"{animationKey}_{name}")) {
			//ends attribute changing
			var chip = this.NewEventChipAtDefCursor(channelNo, 1);
			chip.strObjName = argument;

			currentObjAnimations.TryGetValue($"{animationKey}_{name}", out CChip startChip);

			var index = this.listChip.IndexOf(startChip);
			var msDiff = chip.n発声時刻ms - startChip.n発声時刻ms;

			startChip.fObjTimeMs = msDiff;
			this.listChip[index] = startChip;

			currentObjAnimations.Remove($"{animationKey}_{name}");

			// チップを配置。
			this.listChip.Add(chip);
		} else {
			AddError_Single($"Missing {commandStart}");
			Trace.TraceInformation($"TJA ERROR: Missing {commandStart}");
		}
	}

	private static Easing.CalcType TjaArgToEasingCalcType(string type)
		=> type switch {
			"CUBIC" => Easing.CalcType.Cubic,
			"QUARTIC" => Easing.CalcType.Quartic,
			"QUINTIC" => Easing.CalcType.Quintic,
			"SINUSOIDAL" => Easing.CalcType.Sinusoidal,
			"EXPONENTIAL" => Easing.CalcType.Exponential,
			"CIRCULAR" => Easing.CalcType.Circular,
			"LINEAR" => Easing.CalcType.Linear,
			"QUADRATIC" or _ => Easing.CalcType.Quadratic,
		};

	private void InitializeChartDefinitionBody() {
		// apply global offset
		var msOFFSET_Signed = this.isOFFSET_Negative ? -this.msOFFSET_Abs : this.msOFFSET_Abs;
		msOFFSET_Signed += OpenTaiko.ConfigIni.nGlobalOffsetMs;
		this.msOFFSET_Abs = Math.Abs(msOFFSET_Signed);
		this.isOFFSET_Negative = (msOFFSET_Signed < 0);

		// add initial SCROLL chip
		var chipInitScroll = this.NewEventChipAtDefCursor(0x9D, argInt: 0x00);
		chipInitScroll.dbSCROLL = this.dbScrollSpeed;
		this.listChip.Add(chipInitScroll);

		// apply initial BPM
		CBPM bpmInit = new() { n内部番号 = this.n内部番号BPM1to - 1, n表記上の番号 = this.n内部番号BPM1to - 1, dbBPM値 = this.BASEBPM, };
		this.listBPM.Add(this.n内部番号BPM1to - 1, bpmInit);
		this.n内部番号BPM1to++;

		// add initial BPM chip
		this.listChip.Add(this.NewEventChipAtDefCursor(0x03, 1, 0x00));

		// add initial BPMCHANGE chip
		// Previously this was set up with the first BPMCHANGE during chip post-processing as a part of DTX processing.
		// However, `BPM:` in TJA is usually used for the actually initial BPM,
		// and HBScroll gimmicks regarding `BPM:` are also supported in TaikoJiro,
		// so it is now handled here for simplicity.
		this.listChip.Add(this.NewEventChipAtDefCursor(0x08, bpmInit.n内部番号, 0)); // 拡張BPM

		// add music start chip
		//#STARTと同時に鳴らすのはどうかと思うけどしゃーなしだな。
		this.listChip.Add(this.NewEventChipAtDefCursor(0x01, 1, 0x01));

		// add movie start chip
		var chipMovie = this.NewEventChipAtDefCursor(0x54, 1, 0x01);
		chipMovie.n発声時刻ms += (this.isMOVIEOFFSET_Negative ? -this.msMOVIEOFFSET_Abs : this.msMOVIEOFFSET_Abs);
		this.listChip.Add(chipMovie);
	}

	void t現在のチップ情報を記録する(bool bInPut) {
		//2020.04.21 こうなってしまったのは仕方がないな。。
		if (bInPut) {
			#region [ 記録する ]
			cBranchStart.dbTime = this.dbNowTime;
			cBranchStart.dbSCROLL = this.dbNowScroll;
			cBranchStart.dbSCROLLY = this.dbNowScrollY;
			cBranchStart.dbBMScollTime = this.dbNowBMScollTime;
			cBranchStart.dbBPM = this.dbNowBPM;
			cBranchStart.fMeasure_s = this.fNow_Measure_s;
			cBranchStart.fMeasure_m = this.fNow_Measure_m;
			cBranchStart.nMeasureCount = this.n現在の小節数;
			cBranchStart.db移動待機時刻 = this.db移動待機時刻;
			cBranchStart.db出現時刻 = this.db出現時刻;
			#endregion
		} else {
			#region [ 記録した情報をNow~に適応 ]
			this.dbNowTime = cBranchStart.dbTime;
			this.dbNowScroll = cBranchStart.dbSCROLL;
			this.dbNowScrollY = cBranchStart.dbSCROLLY;
			this.dbNowBMScollTime = cBranchStart.dbBMScollTime;
			this.dbNowBPM = cBranchStart.dbBPM;
			this.fNow_Measure_s = cBranchStart.fMeasure_s;
			this.fNow_Measure_m = cBranchStart.fMeasure_m;
			this.n現在の小節数 = cBranchStart.nMeasureCount;
			this.db移動待機時刻 = cBranchStart.db移動待機時刻;
			this.db出現時刻 = cBranchStart.db出現時刻;
			#endregion
		}
	}

	/// <summary>
	/// 一小節前の小節線情報を返すMethod 2020.04.21.akasoko26
	/// </summary>
	/// <param name="listChips"></param>
	/// <returns></returns>
	private CChip c一小節前の小節線情報を返す(List<CChip> listChips, EBranchConditionType e分岐種類, bool b分岐前の連打開始 = false) {
		//2020.04.20 c一小節前の小節線情報を返すMethodを追加
		//連打分岐時は現在の小節以降の連打の終わり部分の時刻を取得する

		int? nReturnChip = null;

		//--して取得しないとだめよ～ダメダメ💛
		//:damedane:
		for (int i = listChips.Count - 1; i >= 0; i--) {
			if (b分岐前の連打開始) {
				//if (listChips[i].nチャンネル番号 == 0x15 || listChips[i].nチャンネル番号 == 0x16)
				if (NotesManager.IsRoll(listChips[i]) || NotesManager.IsFuzeRoll(listChips[i])) {
					if (nReturnChip == null)
						nReturnChip = i;

					//ReturnChipがnullであったら適応
				}
			} else {
				var Flag = e分岐種類 == EBranchConditionType.Drumroll ? 0x18 : 0x50;

				if (listChips[i].nChannelNo == Flag) {
					if (nReturnChip == null)
						nReturnChip = i;
					//ReturnChipがnullであったら適応
				}
			}
		}

		//もし、nReturnChipがnullだったらlistChipのCount - 1にセットする。
		return listChips[nReturnChip == null ? listChips.Count - 1 : (int)nReturnChip];
	}

	private void WarnSplitLength(string name, string[] strArray, int minimumLength) {
		if (strArray.Length < minimumLength) {
			Trace.TraceWarning(
				$"命令 {name} のパラメータが足りません。少なくとも {minimumLength} つのパラメータが必要です。 (現在のパラメータ数: {strArray.Length}). ({strファイル名の絶対パス})");
		}
	}

	private void t入力_行解析譜面_V4(string InputText) {
		if (!String.IsNullOrEmpty(InputText)) {
			int n文字数 = 16;

			//現在のコース、小節に当てはまるものをリストから探して文字数を返す。
			for (int i = 0; i < this.listLine.Count; i++) {
				if (this.listLine[i].n小節番号 == this.n現在の小節数 && this.listLine[i].nコース == this.n現在のコース) {
					n文字数 = this.listLine[i].n文字数;
				}

			}

			if (InputText.StartsWith("#")) {
				// Call orders here
				this.t命令を挿入する(InputText);
				return;
			} else if (InputText.StartsWith("EXAM")) {
				this.tDanExamLoad(InputText);
				return;
			} else {
				if (this.b小節線を挿入している == false) {
					// 小節線にもやってあげないと
					// IsEndedBranchingがfalseで1回
					// trueで3回だよ3回
					for (int i = 0; i < (IsEndedBranching == true ? 3 : 1); i++) {
						CChip chip = this.NewScrolledChipAtDefCursor(0x50, 0, n文字数, IsEndedBranching ? (ECourse)i : n現在のコース);
						chip.n整数値 = this.n現在の小節数;
						chip.n整数値_内部番号 = this.n現在の小節数;
						chip.bHideBarLine = this.bBARLINECUE[0] == 1;
						#region [ 作り直し ]
						if (IsEndedBranching) {
							if (this.IsBranchBarDraw[i])
								chip.bBranch = true;
						} else {
							if (this.IsBranchBarDraw[(int)n現在のコース])
								chip.bBranch = true;
						}
						#endregion

						this.listChip.Add(chip);
						this.listBarLineChip.Add(chip);

						#region [ 作り直し ]
						if (IsEndedBranching)
							this.IsBranchBarDraw[i] = false;
						else this.IsBranchBarDraw[(int)n現在のコース] = false;
						#endregion
					}


					this.dbLastTime = this.dbNowTime;
					this.b小節線を挿入している = true;
				}

				for (int n = 0; n < InputText.Length; n++) {
					if (InputText.Substring(n, 1) == ",") {
						if (n文字数 == 0) {
							this.dbLastTime = this.dbNowTime;
							this.dbLastBMScrollTime = this.dbNowBMScollTime;
							this.dbNowTime += (15000.0 / this.dbNowBPM * (this.fNow_Measure_s / this.fNow_Measure_m) * (16.0 / 1));
							this.dbNowBMScollTime += (((this.fNow_Measure_s / this.fNow_Measure_m)) * (16.0 / 1));
						}
						this.n現在の小節数++;
						this.b小節線を挿入している = false;
						return;
					}

					if (InputText.Substring(0, 1) == "F") {
						bool bTest = true;
					}


					int nObjectNum = this.CharConvertNote(InputText.Substring(n, 1));

					if (nObjectNum != 0) {
						// IsEndedBranchingがfalseで1回
						// trueで3回だよ3回
						for (int i = 0; i < (IsEndedBranching == true ? 3 : 1); i++) {
							ECourse branch = this.IsEndedBranching ? (ECourse)i : this.n現在のコース;
							int iBranch = (int)branch;

							// TODO: add judge-by-note-type methods to NotesManager
							bool isRollHead = (nObjectNum >= 5 && nObjectNum <= 7) || nObjectNum == 9 || nObjectNum == 13 || nObjectNum == 16 || nObjectNum == 17;
							if (this.nNowRollCountBranch[iBranch] >= 0) {
								if (isRollHead) {
									// repeated roll head; treated as blank
									continue; // process this note symbol in the next branch
								}
								if (nObjectNum != 8) {
									// TaikoJiro compatibility: A non-roll ends an unended roll
									if (branch == ECourse.eNormal || this.bHasBranch[this.n参照中の難易度]) {
										Trace.TraceWarning(this.bHasBranch[this.n参照中の難易度] ?
											$"{nameof(CTja)}: An unended roll is ended by a non-roll of type {nObjectNum} in branch {branch} at measure {this.n現在の小節数}. Input: {InputText} In {this.strファイル名の絶対パス}"
											: $"{nameof(CTja)}: An unended roll is ended by a non-roll of type {nObjectNum} at measure {this.n現在の小節数}. Input: {InputText} In {this.strファイル名の絶対パス}"
										);
									}
									InsertNoteAtDefCursor(8, n, n文字数, branch);

								}
							}

							if (isRollHead) {
								// real roll head; predict chip index
								this.nNowRollCountBranch[iBranch] = listChip_Branch[iBranch].Count;
							}

							InsertNoteAtDefCursor(nObjectNum, n, n文字数, branch);
						}
					}

					if (IsEnabledFixSENote) IsEnabledFixSENote = false;

					this.dbLastTime = this.dbNowTime;
					this.dbLastBMScrollTime = this.dbNowBMScollTime;
					this.dbNowTime += (15000.0 / this.dbNowBPM * (this.fNow_Measure_s / this.fNow_Measure_m) * (16.0 / n文字数));
					this.dbNowBMScollTime += (((this.fNow_Measure_s / this.fNow_Measure_m)) * (16.0 / (double)n文字数));
				}
			}
		}
	}

	private CChip NewEventChipAtDefCursor(int channelNo, int argIndex = default, int argInt = default, double argDb = default)
		=> new() {
			nChannelNo = channelNo,
			n発声位置 = (this.n現在の小節数 * 384),
			dbBPM = this.dbNowBPM,
			dbSCROLL = this.dbNowScroll,
			dbSCROLL_Y = this.dbNowScrollY,
			n発声時刻ms = (int)this.dbNowTime,
			fBMSCROLLTime = this.dbNowBMScollTime,
			fNow_Measure_m = this.fNow_Measure_m,
			fNow_Measure_s = this.fNow_Measure_s,
			n整数値 = argInt,
			db実数値 = argDb,
			n整数値_内部番号 = argIndex,
		};

	private CChip NewScrolledChipAtDefCursor(int channelNo, int iDiv, int divsPerMeasure, ECourse branch) {
		CChip chip = this.NewEventChipAtDefCursor(channelNo);
		chip.n発声位置 = (int)((this.n現在の小節数 * 384.0) + ((384.0 * iDiv) / divsPerMeasure));
		chip.n文字数 = divsPerMeasure;
		chip.eScrollMode = this.eScrollMode;

		chip.IsEndedBranching = this.IsEndedBranching;
		chip.nBranch = branch;

		chip.bVisible = true;
		return chip;
	}

	private void InsertNoteAtDefCursor(int noteType, int iDiv, int divsPerMeasure, ECourse branch) {
		int iBranch = (int)branch;

		CChip chip = this.NewScrolledChipAtDefCursor(0x10 + noteType, iDiv, divsPerMeasure, branch);
		chip.IsMissed = false;
		chip.bHit = false;
		chip.bShow = true;
		chip.bShowRoll = true;
		chip.db発声位置 = this.dbNowTime;
		chip.n整数値 = noteType;
		chip.n整数値_内部番号 = 1;
		chip.nScrollDirection = this.nスクロール方向;
		chip.n分岐回数 = 0; // unused; placeholder value
		chip.nノーツ出現時刻ms = (int)(this.db出現時刻 * 1000.0);
		chip.nノーツ移動開始時刻ms = (int)(this.db移動待機時刻 * 1000.0);
		chip.nPlayerSide = this.nPlayerSide;
		chip.bGOGOTIME = this.bGOGOTIME;

		if (NotesManager.IsKusudama(chip)) {
			if (OpenTaiko.ConfigIni.bTokkunMode) {
				chip.nChannelNo = 0x17; // Placeholder until kusudama visuals can be fixed in training mode
			} else if (IsEndedBranching) {
			} else {
				// Balloon in branches
				chip.nChannelNo = 0x19;
			}
		}

		if (NotesManager.IsGenericBalloon(chip)) {
			//this.n現在のコースをswitchで分岐していたため風船の値がうまく割り当てられていない 2020.04.21 akasoko26
			var listBalloon = this.listBalloon_Branch[iBranch];
			if (listBalloon.Count == 0) {
				chip.nBalloon = 5;
			}
			else if (listBalloon.Count > this.listBalloon_Branch_数値管理[iBranch]) {
				chip.nBalloon = listBalloon[this.listBalloon_Branch_数値管理[iBranch]];
				this.listBalloon_Branch_数値管理[iBranch]++;
			}
		}
		if (NotesManager.IsRollEnd(chip)) {
			if (this.nNowRollCountBranch[iBranch] < 0) {
				// stray roll end; treated as blank
				return; // process this note symbol in the next branch
			}

			CChip chipHead = this.listChip_Branch[iBranch][this.nNowRollCountBranch[iBranch]];
			chipHead.nNoteEndPosition = chip.nNoteEndPosition = chip.n発声位置;
			chipHead.nNoteEndTimems = chip.nNoteEndTimems = chip.n発声時刻ms;
			chipHead.fBMSCROLLTime_end = chip.fBMSCROLLTime_end = chip.fBMSCROLLTime;
			chipHead.dbBPM_end = chip.dbBPM_end = chip.dbBPM;
			chipHead.dbSCROLL_end = chip.dbSCROLL_end = chip.dbSCROLL;
			chipHead.dbSCROLL_Y_end = chip.dbSCROLL_Y_end = chip.dbSCROLL_Y;
			chipHead.eScrollMode_end = chip.eScrollMode_end = chip.eScrollMode;

			chip.nノーツ出現時刻ms = chipHead.nノーツ出現時刻ms;
			chip.nノーツ移動開始時刻ms = chipHead.nノーツ移動開始時刻ms;
			chip.n連打音符State = chipHead.nChannelNo - 0x10;

			this.nNowRollCountBranch[iBranch] = -1;
		}

		if (IsEnabledFixSENote) {
			chip.IsFixedSENote = true;
			chip.nSenote = FixSENote - 1;
		}

		#region[ 固定される種類のsenotesはここで設定しておく。 ]
		chip.nSenote = noteType switch {
			3 => 5,
			4 => 6,
			5 => 7,
			6 => 0xA,
			7 => 0xB,
			8 => 0xC,
			9 => 0xB,
			0xA => 5,
			0xB => 6,
			0xD => 0xB,
			0xF1 => 5,
			_ => chip.nSenote,
		};
		#endregion


		if (NotesManager.IsMissableNote(chip)) {
			#region [ 作り直し ]
			//譜面分岐がない譜面でも値は加算されてしまうがしゃあない
			//分岐を開始しない間は共通譜面としてみなす。
			if (IsEndedBranching) {
				this.nノーツ数_Branch[iBranch]++;

				if (branch == ECourse.eNormal) {
					if (this.n参照中の難易度 == (int)Difficulty.Dan) {
						this.nDan_NotesCount[DanSongs.Number - 1]++;
					}
					this.nノーツ数[3]++;
				}
			} else {
				this.nノーツ数_Branch[(int)chip.nBranch]++;
				if (this.n参照中の難易度 == (int)Difficulty.Dan && chip.nBranch == ECourse.eMaster) {
					this.nDan_NotesCount[DanSongs.Number - 1]++;
				}

				if (!this.b分岐を一回でも開始した) {
					//IsEndedBranching==false = forloopが行われていないときのみ
					for (int l = 0; l < 3; l++)
						this.nノーツ数_Branch[l]++;
				}
			}

			#endregion
		} else if (NotesManager.IsGenericBalloon(chip)) {
			//風船はこのままでも機能しているので何もしない.
			if (IsEndedBranching) {
				if (this.n参照中の難易度 == (int)Difficulty.Dan) {
					this.nDan_BalloonCount[DanSongs.Number - 1]++;
				}
			} else {
				if (this.n参照中の難易度 == (int)Difficulty.Dan && chip.nBranch == ECourse.eMaster) {
					this.nDan_BalloonCount[DanSongs.Number - 1]++;
				}
			}

			if (this.b最初の分岐である == false) {
				this.n風船数[(int)this.n現在のコース]++;
			} else {
				this.n風船数[3]++;
			}

		}

		Array.Resize(ref nDan_NotesCount, nDan_NotesCount.Length + 1);
		Array.Resize(ref nDan_BalloonCount, nDan_BalloonCount.Length + 1);
		if (IsEndedBranching) {
			this.listChip_Branch[iBranch].Add(chip);
			if (branch == ECourse.eNormal) {
				this.listChip.Add(chip);
				this.listNoteChip.Add(chip);
			}
		} else {
			this.listChip_Branch[(int)chip.nBranch].Add(chip);
			this.listChip.Add(chip);
			this.listNoteChip.Add(chip);
		}
	}

	/// <summary>
	/// 難易度ごとによって変わるヘッダ値を読み込む。
	/// (BALLOONなど。)
	/// </summary>
	/// <param name="InputText"></param>
	private void t難易度別ヘッダ(string InputText) {
		if (OpenTaiko.actEnumSongs != null && OpenTaiko.actEnumSongs.IsDeActivated) {
			if (InputText.Equals("#NMSCROLL")) {
				eScrollMode = EScrollMode.Normal;
			} else if (InputText.Equals("#HBSCROLL")) {
				eScrollMode = EScrollMode.HBScroll;
			}
			if (InputText.Equals("#BMSCROLL")) {
				eScrollMode = EScrollMode.BMScroll;
			}
		}

		string[] strArray = InputText.Split(new char[] { ':' });
		string strCommandName = "";
		string strCommandParam = "";

		if (strArray.Length == 2) {
			strCommandName = strArray[0].Trim();
			strCommandParam = strArray[1].Trim();
		}

		void ParseOptionalInt16(Action<short> setValue) {
			this.ParseOptionalInt16(strCommandName, strCommandParam, setValue);
		}

		if (strCommandName.Equals("BALLOON") || strCommandName.Equals("BALLOONNOR")) {
			ParseBalloon(strCommandParam, this.listBalloon_Branch[(int)ECourse.eNormal]);
		} else if (strCommandName.Equals("BALLOONEXP")) {
			ParseBalloon(strCommandParam, this.listBalloon_Branch[(int)ECourse.eExpert]);
			//tbBALLOON.Text = strCommandParam;
		} else if (strCommandName.Equals("BALLOONMAS")) {
			ParseBalloon(strCommandParam, this.listBalloon_Branch[(int)ECourse.eMaster]);
			//tbBALLOON.Text = strCommandParam;
		} else if (strCommandName.Equals("SCOREMODE")) {
			ParseOptionalInt16(value => this.nScoreModeTmp = value);
		} else if (strCommandName.Equals("SCOREINIT")) {
			if (!string.IsNullOrEmpty(strCommandParam)) {
				string[] scoreinit = strCommandParam.Split(',');

				this.ParseOptionalInt16("SCOREINIT first value", scoreinit[0], value => {
					this.nScoreInit[0, this.n参照中の難易度] = value;
					this.b配点が指定されている[0, this.n参照中の難易度] = true;
				});

				if (scoreinit.Length == 2) {
					this.ParseOptionalInt16("SCOREINIT second value", scoreinit[1], value => {
						this.nScoreInit[1, this.n参照中の難易度] = value;
						this.b配点が指定されている[2, this.n参照中の難易度] = true;
					});
				}
			}
		} else if (strCommandName.Equals("SCOREDIFF")) {
			ParseOptionalInt16(value => {
				this.nScoreDiff[this.n参照中の難易度] = value;
				this.b配点が指定されている[1, this.n参照中の難易度] = true;
			});
		} else if (strCommandName.Equals("SCOREMODE")) {
			if (!string.IsNullOrEmpty(strCommandParam)) {
				this.nScoreModeTmp = Convert.ToInt16(strCommandParam);
			}
		} else if (strCommandName.Equals("SCOREINIT")) {
			if (!string.IsNullOrEmpty(strCommandParam)) {
				string[] scoreinit = strCommandParam.Split(',');

				this.nScoreInit[0, this.n参照中の難易度] = Convert.ToInt16(scoreinit[0]);
				this.b配点が指定されている[0, this.n参照中の難易度] = true;
				if (scoreinit.Length == 2) {
					this.nScoreInit[1, this.n参照中の難易度] = Convert.ToInt16(scoreinit[1]);
					this.b配点が指定されている[2, this.n参照中の難易度] = true;
				}
			}
		} else if (strCommandName.Equals("SCOREDIFF")) {
			if (!string.IsNullOrEmpty(strCommandParam)) {
				this.nScoreDiff[this.n参照中の難易度] = Convert.ToInt16(strCommandParam);
				this.b配点が指定されている[1, this.n参照中の難易度] = true;
			}
		}
		if (this.nScoreModeTmp == 99) //2017.01.28 DD SCOREMODEを入力していない場合のみConfigで設定したモードにする
		{
			this.nScoreModeTmp = OpenTaiko.ConfigIni.nScoreMode;
		}
		if (OpenTaiko.ConfigIni.nScoreMode == 3 && !this.b配点が指定されている[2, this.n参照中の難易度]) { //2017.06.04 kairera0467
			this.nScoreModeTmp = 3;
		}
	}

	private void tDanExamLoad(string input) {
		string[] strArray = input.Split(new char[] { ':' });
		string strCommandName = "";
		string strCommandParam = "";

		if (strArray.Length == 2) {
			strCommandName = strArray[0].Trim();
			strCommandParam = strArray[1].Trim();
		}

		// Adapt to EXAM until 7, optimise condition

		if (strCommandName.StartsWith("EXAM")) {
			if (!string.IsNullOrEmpty(strCommandParam)) {
				int[] examValue;
				var splitExam = strCommandParam.Split(',');
				int examNumber = int.Parse(strCommandName.Substring(4)) - 1;

				if (examNumber > CExamInfo.cMaxExam)
					return;
				var examType = splitExam[0] switch {
					"jp" => Exam.Type.JudgePerfect,
					"jg" => Exam.Type.JudgeGood,
					"jb" => Exam.Type.JudgeBad,
					"s" => Exam.Type.Score,
					"r" => Exam.Type.Roll,
					"h" => Exam.Type.Hit,
					"c" => Exam.Type.Combo,
					"a" => Exam.Type.Accuracy,
					"ja" => Exam.Type.JudgeADLIB,
					"jm" => Exam.Type.JudgeMine,
					"g" or _ => Exam.Type.Gauge,
				};
				try {
					examValue = new int[] { int.Parse(splitExam[1]), int.Parse(splitExam[2]) };
				} catch (Exception) {
					examValue = new int[] { 100, 100 };
				}

				var examRange = splitExam[3] switch {
					"l" => Exam.Range.Less,
					"m" or _ => Exam.Range.More,
				};
				if (Dan_C[examNumber] == null)
					Dan_C[examNumber] = new Dan_C(examType, examValue, examRange);

				if (DanSongs.Number > 0)
					List_DanSongs[DanSongs.Number - 1].Dan_C[examNumber] = new Dan_C(examType, examValue, examRange);
			}
		}
	}

	private void ParseOptionalInt16(string name, string unparsedValue, Action<short> setValue) {
		if (string.IsNullOrEmpty(unparsedValue)) {
			return;
		}

		if (short.TryParse(unparsedValue, out var value)) {
			setValue(value);
		} else {
			Trace.TraceWarning($"命令名: {name} のパラメータの値が正しくないことを検知しました。値: {unparsedValue} ({strファイル名の絶対パス})");
		}
	}


	private void ParseBalloon(string strCommandParam, List<int> listBalloon) {
		string[] strParam = strCommandParam.Split(',');
		for (int n = 0; n < strParam.Length; n++) {
			int n打数;
			try {
				if (strParam[n] == null || strParam[n] == "")
					break;

				n打数 = Convert.ToInt32(strParam[n]);
			} catch (Exception ex) {
				Trace.TraceError($"おや?エラーが出たようです。お兄様。 ({strファイル名の絶対パス})");
				Trace.TraceError(ex.ToString());
				Trace.TraceError("例外が発生しましたが処理を継続します。 (95327158-4e83-4fa9-b5e9-ad3c3d4c2a22)");
				break;
			}

			listBalloon.Add(n打数);
		}
	}
	private void t入力_行解析ヘッダ(string InputText) {
		//やべー。先頭にコメント行あったらやばいやん。
		string[] strArray = InputText.Split(new char[] { ':' }, 2);
		string strCommandName = "";
		string strCommandParam = "";

		if (InputText.StartsWith("#BRANCHSTART")) {
			//2015.08.18 kairera0467
			//本来はヘッダ命令ではありませんが、難易度ごとに違う項目なのでここで読み込ませます。
			//Lengthのチェックをされる前ににif文を入れています。
			this.bHasBranch[this.n参照中の難易度] = true;

			if (this.n参照中の難易度 == (int)Difficulty.Dan) {
				this.bHasBranchDan[this.bHasBranchDan.Length - 1] = true;
			}
		}

		//まずは「:」でSplitして割り当てる。
		if (strArray.Length == 2) {
			strCommandName = strArray[0].Trim();
			strCommandParam = strArray[1].Trim();
		} else if (strArray.Length > 2) {
			//strArrayが2じゃない場合、ヘッダのSplitを通していない可能性がある。
			//この処理自体は「t入力」を改造したもの。STARTでSplitしていない等、一部の処理が異なる。

			#region [Header]
			InputText = InputText.Replace(Environment.NewLine, "\n"); //改行文字を別の文字列に差し替え。
			InputText = InputText.Replace('\t', ' '); //何の文字か知らないけどスペースに差し替え。
			InputText = InputText + "\n";

			string[] strDelimiter2 = { "\n" };
			strArray = InputText.Split(strDelimiter2, StringSplitOptions.RemoveEmptyEntries);


			strArray = strArray[0].Split(new char[] { ':' });
			WarnSplitLength("Header Name & Value", strArray, 2);

			strCommandName = strArray[0].Trim();
			strCommandParam = strArray[1].Trim();

			#endregion
			//lblMessage.Text = "おや?strArrayのLengthが2じゃないようですね。お兄様。";
		}

		void ParseOptionalInt16(Action<short> setValue) {
			this.ParseOptionalInt16(strCommandName, strCommandParam, setValue);
		}

		//パラメータを分別、そこから割り当てていきます。
		if (strCommandName.Equals("TITLE")) {
			this.TITLE.SetString("default", strCommandParam);
		} else if (strCommandName.StartsWith("TITLE")) {
			string _lang = strCommandName.Substring(5).ToLowerInvariant();
			this.TITLE.SetString(_lang, strCommandParam);
		} else if (strCommandName.Equals("SUBTITLE")) {
			if (strCommandParam.StartsWith("--") || strCommandParam.StartsWith("++"))
				this.SUBTITLE.SetString("default", strCommandParam.Substring(2));
			else
				this.SUBTITLE.SetString("default", strCommandParam);
		} else if (strCommandName.StartsWith("SUBTITLE")) {
			string _lang = strCommandName.Substring(8).ToLowerInvariant();
			this.SUBTITLE.SetString(_lang, strCommandParam);
		} else if (strCommandName.Equals("LEVEL")) {
			var level_dec = Convert.ToDouble(strCommandParam);
			var level = (int)level_dec;
			if (strCommandParam != level.ToString()) {
				int frac_part = Int32.Parse(level_dec.ToString("0.0", CultureInfo.InvariantCulture).Split('.')[1]);
				this.LEVELtaikoIcon[this.n参照中の難易度] = (frac_part >= 5) ? ELevelIcon.ePlus : ELevelIcon.eMinus;
			}
			this.LEVEL.Drums = (int)level;
			this.LEVEL.Taiko = (int)level;
			this.LEVELtaiko[this.n参照中の難易度] = (int)level;
		} else if (strCommandName.StartsWith("NOTESDESIGNER")) {
			this.NOTESDESIGNER[this.n参照中の難易度] = strCommandParam;
		} else if (strCommandName.Equals("LIFE")) {
			// LIFE here
			var life = (int)Convert.ToDouble(strCommandParam);
			this.LIFE = life;
		} else if (strCommandName.Equals("PREIMAGE")) {
			this.PREIMAGE = strCommandParam;
		} else if (strCommandName.Equals("TOWERTYPE")) {
			this.TOWERTYPE = strCommandParam;
		} else if (strCommandName.Equals("DANTICK")) {
			var tick = (int)Convert.ToDouble(strCommandParam);
			this.DANTICK = tick;
		} else if (strCommandName.Equals("DANTICKCOLOR")) {
			var tickcolor = ColorTranslator.FromHtml(strCommandParam);
			this.DANTICKCOLOR = tickcolor;
		} else if (strCommandName.Equals("BPM")) {
			if (strCommandParam.IndexOf(",") != -1)
				strCommandParam = strCommandParam.Replace(',', '.');

			double dbBPM = Convert.ToDouble(strCommandParam);
			this.BPM = dbBPM;
			this.BASEBPM = dbBPM;
			this.MinBPM = dbBPM;
			this.MaxBPM = dbBPM;
			this.dbNowBPM = dbBPM;
		} else if (strCommandName.Equals("WAVE")) {
			if (strBGM_PATH != null) {
				Trace.TraceWarning($"{nameof(CTja)} is ignoring an extra WAVE header in {this.strファイル名の絶対パス}");
			} else {
				this.strBGM_PATH = CDTXCompanionFileFinder.FindFileName(this.strフォルダ名, strファイル名, strCommandParam);
				//tbWave.Text = strCommandParam;
				if (this.listWAV != null) {
					// 2018-08-27 twopointzero - DO attempt to load (or queue scanning) loudness metadata here.
					//                           TJAP3 is either launching, enumerating songs, or is about to
					//                           begin playing a song. If metadata is available, we want it now.
					//                           If is not yet available then we wish to queue scanning.
					var absoluteBgmPath = Path.Combine(this.strフォルダ名, this.strBGM_PATH);
					this.SongLoudnessMetadata = LoudnessMetadataScanner.LoadForAudioPath(absoluteBgmPath);

					var wav = new CWAV() {
						n内部番号 = this.n内部番号WAV1to,
						n表記上の番号 = 1,
						nチップサイズ = this.n無限管理SIZE[this.n内部番号WAV1to],
						n位置 = this.n無限管理PAN[this.n内部番号WAV1to],
						SongVol = this.SongVol,
						SongLoudnessMetadata = this.SongLoudnessMetadata,
						strファイル名 = this.strBGM_PATH,
						strコメント文 = "TJA BGM",
					};

					this.listWAV.Add(this.n内部番号WAV1to, wav);
					this.n内部番号WAV1to++;
				}
			}
		} else if (strCommandName.Equals("OFFSET") && !string.IsNullOrEmpty(strCommandParam)) {
			this.msOFFSET_Abs = (int)(Convert.ToDouble(strCommandParam) * 1000);
			this.isOFFSET_Negative = this.msOFFSET_Abs < 0 ? true : false;

			if (this.isOFFSET_Negative == true)
				this.msOFFSET_Abs = this.msOFFSET_Abs * -1; //OFFSETは秒を加算するので、必ず正の数にすること。
												  //tbOFFSET.Text = strCommandParam;
		} else if (strCommandName.Equals("MOVIEOFFSET")) {
			this.msMOVIEOFFSET_Abs = (int)(Convert.ToDouble(strCommandParam) * 1000);
			this.isMOVIEOFFSET_Negative = this.msMOVIEOFFSET_Abs < 0 ? true : false;

			if (this.isMOVIEOFFSET_Negative == true)
				this.msMOVIEOFFSET_Abs = this.msMOVIEOFFSET_Abs * -1; //OFFSETは秒を加算するので、必ず正の数にすること。
															//tbOFFSET.Text = strCommandParam;
		}
		#region[移動→不具合が起こるのでここも一応復活させておく]
		else if (strCommandName.Equals("BALLOON") || strCommandName.Equals("BALLOONNOR")) {
			ParseBalloon(strCommandParam, this.listBalloon_Branch[(int)ECourse.eNormal]);
		} else if (strCommandName.Equals("BALLOONEXP")) {
			ParseBalloon(strCommandParam, this.listBalloon_Branch[(int)ECourse.eExpert]);
			//tbBALLOON.Text = strCommandParam;
		} else if (strCommandName.Equals("BALLOONMAS")) {
			ParseBalloon(strCommandParam, this.listBalloon_Branch[(int)ECourse.eMaster]);
			//tbBALLOON.Text = strCommandParam;
		} else if (strCommandName.Equals("SCOREMODE")) {
			ParseOptionalInt16(value => this.nScoreModeTmp = value);
		} else if (strCommandName.Equals("SCOREINIT")) {
			if (!string.IsNullOrEmpty(strCommandParam)) {
				string[] scoreinit = strCommandParam.Split(',');

				this.ParseOptionalInt16("SCOREINIT first value", scoreinit[0], value => {
					this.nScoreInit[0, this.n参照中の難易度] = value;
				});

				if (scoreinit.Length == 2) {
					this.ParseOptionalInt16("SCOREINIT second value", scoreinit[1], value => {
						this.nScoreInit[1, this.n参照中の難易度] = value;
					});
				}
			}
		} else if (strCommandName.Equals("GAUGEINCR")) {
			if (!string.IsNullOrEmpty(strCommandParam)) {
				GaugeIncreaseMode = strCommandParam.ToLower() switch {
					"floor" => GaugeIncreaseMode.Floor,
					"round" => GaugeIncreaseMode.Round,
					"ceiling" => GaugeIncreaseMode.Ceiling,
					"notfix" => GaugeIncreaseMode.NotFix,
					"normal" or _ => GaugeIncreaseMode.Normal,
				};
			}
		} else if (strCommandName.Equals("SCOREDIFF")) {
			ParseOptionalInt16(value => this.nScoreDiff[this.n参照中の難易度] = value);
		}
		#endregion
		else if (strCommandName.Equals("SONGVOL") && !string.IsNullOrEmpty(strCommandParam)) {
			this.SongVol = Convert.ToInt32(strCommandParam).Clamp(CSound.MinimumSongVol, CSound.MaximumSongVol);

			foreach (var kvp in this.listWAV) {
				kvp.Value.SongVol = this.SongVol;
			}
		} else if (strCommandName.Equals("SEVOL")) {
			//tbSeVol.Text = strCommandParam;
		} else if (strCommandName.Equals("COURSE")) {
			if (!string.IsNullOrEmpty(strCommandParam)) {
				//this.n参照中の難易度 = Convert.ToInt16( strCommandParam );
				this.n参照中の難易度 = this.strConvertCourse(strCommandParam);
			}
		} else if (strCommandName.Equals("HEADSCROLL")) {
			//新定義:初期スクロール速度設定(というよりこのシステムに合わせるには必須。)
			//どうしても一番最初に1小節挿入されるから、こうするしかなかったんだ___

			this.dbScrollSpeed = Convert.ToDouble(strCommandParam);
		} else if (strCommandName.Equals("GENRE")) {
			//2015.03.28 kairera0467
			//ジャンルの定義。DTXから入力もできるが、tjaからも入力できるようにする。
			//日本語名だと選曲画面でバグが出るので、そこもどうにかしていく予定。

			if (!string.IsNullOrEmpty(strCommandParam)) {
				this.GENRE = strCommandParam;
			}
		} else if (strCommandName.Equals("MAKER")) {
			if (!string.IsNullOrEmpty(strCommandParam)) {
				this.MAKER = strCommandParam;
			}
		} else if (strCommandName.Equals("SIDE")) {
			if (!string.IsNullOrEmpty(strCommandParam) && strCommandParam.Equals("Normal"))
				this.SIDE = ESide.eNormal;
		} else if (strCommandName.Equals("EXPLICIT")) {
			if (!string.IsNullOrEmpty(strCommandParam)) {
				this.EXPLICIT = CConversion.bONorOFF(strCommandParam[0]);
			}
		} else if (strCommandName.Equals("SELECTBG")) {
			if (!string.IsNullOrEmpty(strCommandParam)) {
				this.SELECTBG = strCommandParam;
			}
		} else if (strCommandName.Equals("SCENEPRESET")) {
			if (!string.IsNullOrEmpty(strCommandParam)) {
				this.scenePreset = strCommandParam;
			}
		} else if (strCommandName.Equals("DEMOSTART")) {
			//2015.04.10 kairera0467

			if (!string.IsNullOrEmpty(strCommandParam)) {
				int nOFFSETms;
				try {
					nOFFSETms = (int)(Convert.ToDouble(strCommandParam) * 1000.0);
				} catch {
					nOFFSETms = 0;
				}


				this.nデモBGMオフセット = nOFFSETms;
			}
		} else if (strCommandName.Equals("BGMOVIE")) {
			//2016.02.02 kairera0467
			//背景動画の定義。DTXから入力もできるが、tjaからも入力できるようにする。

			if (!string.IsNullOrEmpty(strCommandParam)) {
				this.strBGVIDEO_PATH =
					CDTXCompanionFileFinder.FindFileName(this.strフォルダ名, strファイル名, strCommandParam);
			}

			string strVideoFilename;
			if (!string.IsNullOrEmpty(this.PATH_WAV))
				strVideoFilename = this.PATH_WAV + this.strBGVIDEO_PATH;
			else
				strVideoFilename = this.strフォルダ名 + this.strBGVIDEO_PATH;

			try {
				CVideoDecoder vd = new CVideoDecoder(strVideoFilename);

				if (this.listVD.ContainsKey(1))
					this.listVD.Remove(1);

				this.listVD.Add(1, vd);
			} catch (Exception e) {
				Trace.TraceWarning(e.ToString() + "\n" +
								   "動画のデコーダー生成で例外が発生しましたが、処理を継続します。");
				if (this.listVD.ContainsKey(1))
					this.listVD.Remove(1);
			}
		} else if (strCommandName.Contains("BGA")) {
			//2016.02.02 kairera0467
			//背景動画の定義。DTXから入力もできるが、tjaからも入力できるようにする。

			string videoPath = "";
			if (!string.IsNullOrEmpty(strCommandParam)) {
				videoPath =
					CDTXCompanionFileFinder.FindFileName(this.strフォルダ名, strファイル名, strCommandParam);
			}

			string strVideoFilename;
			if (!string.IsNullOrEmpty(this.PATH_WAV))
				strVideoFilename = this.PATH_WAV + videoPath;
			else
				strVideoFilename = this.strフォルダ名 + videoPath;

			try {
				CVideoDecoder vd = new CVideoDecoder(strVideoFilename);

				var indexText = strCommandName.Remove(0, 3);

				this.listVD.Add((10 * int.Parse(indexText[0].ToString())) + int.Parse(indexText[1].ToString()) + 2, vd);
			} catch (Exception e) {
				Trace.TraceWarning(e.ToString() + "\n" +
								   "動画のデコーダー生成で例外が発生しましたが、処理を継続します。");
				if (this.listVD.ContainsKey(1))
					this.listVD.Remove(1);
			}
		} else if (strCommandName.Equals("BGIMAGE")) {
			//2016.02.02 kairera0467
			if (!string.IsNullOrEmpty(strCommandParam)) {
				this.strBGIMAGE_PATH = strCommandParam;
			}
		} else if (strCommandName.Equals("HIDDENBRANCH")) {
			//2016.04.01 kairera0467 パラメーターは
			if (!string.IsNullOrEmpty(strCommandParam)) {
				this.bHIDDENBRANCH = true;
			}
		} else if (strCommandName.Equals("LYRICS") && !usingLyricsFile && OpenTaiko.ConfigIni.nPlayerCount < 4) {
			if (!string.IsNullOrEmpty(strCommandParam)) {
				string[] files = SplitComma(strCommandParam);
				string[] filePaths = new string[files.Length];
				for (int i = 0; i < files.Length; i++) {
					filePaths[i] = this.strフォルダ名 + files[i];

					if (File.Exists(filePaths[i])) {
						try {
							if (OpenTaiko.rCurrentStage.eStageID == CStage.EStage.SongLoading) {
								if (filePaths[i].EndsWith(".vtt")) {
									using (VTTParser parser = new VTTParser()) {
										this.listLyric2.AddRange(parser.ParseVTTFile(filePaths[i], 0, 0));
									}
									this.bLyrics = true;
									this.usingLyricsFile = true;
								} else if (filePaths[i].EndsWith(".lrc")) {
									this.LyricFileParser(filePaths[i], i);
									this.bLyrics = true;
									this.usingLyricsFile = true;
								}
							}
						} catch (Exception e) {
							Trace.TraceError("Something went wrong while parsing a lyric file at {0}. More details : {1}", filePaths[i], e);
						}
					}
				}
			}
		} else if (strCommandName.Equals("LYRICFILE") && !usingLyricsFile && OpenTaiko.ConfigIni.nPlayerCount < 4) {
			if (!string.IsNullOrEmpty(strCommandParam)) {
				string[] strFiles = SplitComma(strCommandParam);
				string[] strFilePath = new string[strFiles.Length];
				for (int index = 0; index < strFiles.Length; index++) {
					strFilePath[index] = this.strフォルダ名 + strFiles[index];
					if (File.Exists(strFilePath[index])) {
						try {
							if (OpenTaiko.rCurrentStage.eStageID == CStage.EStage.SongLoading)//起動時に重たくなってしまう問題の修正用
								this.LyricFileParser(strFilePath[index], index);
							this.bLyrics = true;
							this.usingLyricsFile = true;
						} catch {
							Console.WriteLine("lrcファイルNo.{0}の読み込みに失敗しましたが、", index);
							Console.WriteLine("処理を続行します。");
						}
					}
				}
			}
		}
		if (this.nScoreModeTmp == 99) {
			//2017.01.28 DD
			this.nScoreModeTmp = OpenTaiko.ConfigIni.nScoreMode;
		}
	}
	/// <summary>
	/// 指定した文字が数値かを返すメソッド
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public bool bIsNumber(char Char) {
		if ((Char >= '0') && (Char <= '9'))
			return true;
		else
			return false;
	}

	/// <summary>
	/// string型からint型に変換する。
	/// TJAP2から持ってきた。
	/// </summary>
	private int CharConvertNote(string str) {
		return (NotesManager.GetNoteValueFromChar(str));
	}

	private int strConvertCourse(string str) {
		//2016.08.24 kairera0467
		//正規表現を使っているため、easyでもEASYでもOK。

		// 小文字大文字区別しない正規表現で仮対応。 (AioiLight)
		// 相変わらず原始的なやり方だが、正常に動作した。
		string[] Matchptn = new string[7] { "easy", "normal", "hard", "oni", "edit", "tower", "dan" };
		for (int i = 0; i < Matchptn.Length; i++) {
			if (string.Equals(str, Matchptn[i], StringComparison.InvariantCultureIgnoreCase)) {
				return i;
			}
		}

		if (int.TryParse(str, out int iDiff) && (iDiff >= 0 && iDiff <= 6)) {
			return iDiff;
		}
		return 3;
	}

	/// <summary>
	/// Lyricファイルのパースもどき
	/// 自力で作ったので、うまくパースしてくれないかも
	/// </summary>
	/// <param name="strFilePath">lrcファイルのパス</param>
	private void LyricFileParser(string strFilePath, int ordnumber)//lrcファイルのパース用
	{
		string str = CJudgeTextEncoding.ReadTextFile(strFilePath);
		var strSplit後 = str.Split(this.dlmtEnter, StringSplitOptions.RemoveEmptyEntries);
		Regex timeRegex = new Regex(@"^(\[)(\d{2})(:)(\d{2})([:.])(\d{2})(\])", RegexOptions.Multiline | RegexOptions.Compiled);
		Regex timeRegexO = new Regex(@"^(\[)(\d{2})(:)(\d{2})(\])", RegexOptions.Multiline | RegexOptions.Compiled);
		List<long> list;
		for (int i = 0; i < strSplit後.Length; i++) {
			list = new List<long>();
			if (!String.IsNullOrEmpty(strSplit後[i])) {
				if (strSplit後[i].StartsWith("[")) {
					Match timestring = timeRegex.Match(strSplit後[i]), timestringO = timeRegexO.Match(strSplit後[i]);
					while (timestringO.Success || timestring.Success) {
						long time;
						if (timestring.Success) {
							time = Int32.Parse(timestring.Groups[2].Value) * 60000 + Int32.Parse(timestring.Groups[4].Value) * 1000 + Int32.Parse(timestring.Groups[6].Value) * 10;
							strSplit後[i] = strSplit後[i].Remove(0, 10);
						} else if (timestringO.Success) {
							time = Int32.Parse(timestringO.Groups[2].Value) * 60000 + Int32.Parse(timestringO.Groups[4].Value) * 1000;
							strSplit後[i] = strSplit後[i].Remove(0, 7);
						} else
							break;
						list.Add(time);
						timestring = timeRegex.Match(strSplit後[i]);
						timestringO = timeRegexO.Match(strSplit後[i]);
					}
					strSplit後[i] = strSplit後[i].Replace("\r", "").Replace("\n", "");

					for (int listindex = 0; listindex < list.Count; listindex++) {
						STLYRIC stlrc;
						stlrc.Text = strSplit後[i];
						stlrc.TextTex = this.pf歌詞フォント.DrawText(strSplit後[i], OpenTaiko.Skin.Game_Lyric_ForeColor, OpenTaiko.Skin.Game_Lyric_BackColor, null, 30);
						stlrc.Time = list[listindex];
						stlrc.index = ordnumber;
						this.listLyric2.Add(stlrc);
					}
				}
			}
		}
	}


	/// <summary>
	/// 複素数のパースもどき
	/// </summary>
	private void tParsedComplexNumber(string strScroll, ref double[] dbScroll) {
		var cpx = strScroll.ParseComplex();
		dbScroll[0] = cpx[0];
		dbScroll[1] = cpx[1];
	}

	private void tSetSenotes() {
		#region[ list作成 ]
		//ひとまずチップだけのリストを作成しておく。
		List<CChip> list音符のみのリスト;
		list音符のみのリスト = new List<CChip>();
		int nCount = 0;
		int dkdkCount = 0;

		foreach (CChip chip in this.listChip) {
			if (NotesManager.IsCommonNote(chip)) {
				list音符のみのリスト.Add(chip);
			}
		}
		#endregion

		//時間判定は、「次のチップの発声時刻」から「現在(過去)のチップの発声時刻」で引く必要がある。
		//逆にしてしまうと計算がとてつもないことになるので注意。

		try {
			this.tSenotes_Core_V2(list音符のみのリスト, true);
		} catch (Exception ex) {
			Trace.TraceError(ex.ToString());
			Trace.TraceError("例外が発生しましたが処理を継続します。 (b67473e4-1930-44f1-b320-4ead5786e74c)");
		}

	}

	/// <summary>
	/// 譜面分岐がある場合はこちらを使う
	/// </summary>
	private void tSetSenotes_branch() {
		#region[ list作成 ]
		//ひとまずチップだけのリストを作成しておく。
		List<CChip>[] list音符のみのリスト_Branch = new[] { new List<CChip>(), new List<CChip>(), new List<CChip>() };
		int nCount = 0;
		int dkdkCount = 0;

		foreach (CChip chip in this.listChip) {
			if (NotesManager.IsCommonNote(chip)) {
				list音符のみのリスト_Branch[(int)chip.nBranch].Add(chip);
			}
		}
		#endregion

		//forで処理。
		for (int n = 0; n < list音符のみのリスト_Branch.Length; n++) {
			this.tSenotes_Core_V2(list音符のみのリスト_Branch[n], true);
		}

	}

	/// <summary>
	/// コア部分Ver2。TJAP2から移植しただけ。
	/// </summary>
	/// <param name="list音符のみのリスト"></param>
	private void tSenotes_Core_V2(List<CChip> list音符のみのリスト, bool ignoreSENote = false) {
		const int DATA = 3;
		int doco_count = 0;
		int[] sort = new int[7];
		double[] time = new double[7];
		double[] scroll = new double[7];
		double time_tmp;

		for (int i = 0; i < list音符のみのリスト.Count; i++) {
			for (int j = 0; j < 7; j++) {
				if (i + (j - 3) < 0) {
					sort[j] = -1;
					time[j] = -1000000000;
					scroll[j] = 1.0;
				} else if (i + (j - 3) >= list音符のみのリスト.Count) {
					sort[j] = -1;
					time[j] = 1000000000;
					scroll[j] = 1.0;
				} else {
					sort[j] = list音符のみのリスト[i + (j - 3)].nChannelNo;
					time[j] = list音符のみのリスト[i + (j - 3)].fBMSCROLLTime;
					scroll[j] = list音符のみのリスト[i + (j - 3)].dbSCROLL;
				}
			}
			time_tmp = time[DATA];
			for (int j = 0; j < 7; j++) {
				time[j] = (time[j] - time_tmp) * scroll[j];
				if (time[j] < 0) time[j] *= -1;
			}

			if (ignoreSENote && list音符のみのリスト[i].IsFixedSENote) continue;

			switch (list音符のみのリスト[i].nChannelNo) {
				case 0x11:

					//（左2より離れている｜）_右2_右ドン_右右4_右右ドン…
					if ((time[DATA - 1] > 2/* || (sort[DATA-1] != 1 && time[DATA-1] >= 2 && time[DATA-2] >= 4 && time[DATA-3] <= 5)*/) && time[DATA + 1] == 2 && sort[DATA + 1] == 1 && time[DATA + 2] == 4 && sort[DATA + 2] == 0x11 && time[DATA + 3] == 6 && sort[DATA + 3] == 0x11) {
						list音符のみのリスト[i].nSenote = 1;
						doco_count = 1;
						break;
					}
					//ドコドコ中_左2_右2_右ドン
					else if (doco_count != 0 && time[DATA - 1] == 2 && time[DATA + 1] == 2 && (sort[DATA + 1] == 0x11 || sort[DATA + 1] == 0x11)) {
						if (doco_count % 2 == 0)
							list音符のみのリスト[i].nSenote = 1;
						else
							list音符のみのリスト[i].nSenote = 2;
						doco_count++;
						break;
					} else {
						doco_count = 0;
					}

					//8分ドコドン
					if ((time[DATA - 2] >= 4.1 && time[DATA - 1] == 2 && time[DATA + 1] == 2 && time[DATA + 2] >= 4.1) && (sort[DATA - 1] == 0x11 && sort[DATA + 1] == 0x11)) {
						if (list音符のみのリスト[i].dbBPM >= 120.0) {
							list音符のみのリスト[i - 1].nSenote = 1;
							list音符のみのリスト[i].nSenote = 2;
							list音符のみのリスト[i + 1].nSenote = 0;
							break;
						} else if (list音符のみのリスト[i].dbBPM < 120.0) {
							list音符のみのリスト[i - 1].nSenote = 0;
							list音符のみのリスト[i].nSenote = 0;
							list音符のみのリスト[i + 1].nSenote = 0;
							break;
						}
					}

					//BPM120以下のみ
					//8分間隔の「ドドド」→「ドンドンドン」

					if (time[DATA - 1] >= 2 && time[DATA + 1] >= 2) {
						if (list音符のみのリスト[i].dbBPM < 120.0) {
							list音符のみのリスト[i].nSenote = 0;
							break;
						}
					}

					//ドコドコドン
					if (time[DATA - 3] >= 3.4 && time[DATA - 2] == 2 && time[DATA - 1] == 1 && time[DATA + 1] == 1 && time[DATA + 2] == 2 && time[DATA + 3] >= 3.4 && sort[DATA - 2] == 0x11 && sort[DATA - 1] == 0x11 && sort[DATA + 1] == 0x11 && sort[DATA + 2] == 0x11) {
						list音符のみのリスト[i - 2].nSenote = 1;
						list音符のみのリスト[i - 1].nSenote = 2;
						list音符のみのリスト[i + 0].nSenote = 1;
						list音符のみのリスト[i + 1].nSenote = 2;
						list音符のみのリスト[i + 2].nSenote = 0;
						i += 2;
						//break;
					}
					//ドコドン
					else if (time[DATA - 2] >= 2.4 && time[DATA - 1] == 1 && time[DATA + 1] == 1 && time[DATA + 2] >= 2.4 && sort[DATA - 1] == 0x11 && sort[DATA + 1] == 0x11) {
						list音符のみのリスト[i].nSenote = 2;
					}
					//右の音符が2以上離れている
					else if (time[DATA + 1] > 2) {
						list音符のみのリスト[i].nSenote = 0;
					}
					//右の音符が1.4以上_左の音符が1.4以内
					else if (time[DATA + 1] >= 1.4 && time[DATA - 1] <= 1.4) {
						list音符のみのリスト[i].nSenote = 0;
					}
					//右の音符が2以上_右右の音符が3以内
					else if (time[DATA + 1] >= 2 && time[DATA + 2] <= 3) {
						list音符のみのリスト[i].nSenote = 0;
					}
					//右の音符が2以上_大音符
					else if (time[DATA + 1] >= 2 && (sort[DATA + 1] == 0x13 || sort[DATA + 1] == 0x14)) {
						list音符のみのリスト[i].nSenote = 0;
					} else {
						list音符のみのリスト[i].nSenote = 1;
					}
					break;
				case 0x12:
					doco_count = 0;

					//BPM120以下のみ
					//8分間隔の「ドドド」→「ドンドンドン」
					if (time[DATA - 1] == 2 && time[DATA + 1] == 2) {
						if (list音符のみのリスト[i - 1].dbBPM < 120.0 && list音符のみのリスト[i].dbBPM < 120.0 && list音符のみのリスト[i + 1].dbBPM < 120.0) {
							list音符のみのリスト[i].nSenote = 3;
							break;
						}
					}

					//右の音符が2以上離れている
					if (time[DATA + 1] > 2) {
						list音符のみのリスト[i].nSenote = 3;
					}
					//右の音符が1.4以上_左の音符が1.4以内
					else if (time[DATA + 1] >= 1.4 && time[DATA - 1] <= 1.4) {
						list音符のみのリスト[i].nSenote = 3;
					}
					//右の音符が2以上_右右の音符が3以内
					else if (time[DATA + 1] >= 2 && time[DATA + 2] <= 3) {
						list音符のみのリスト[i].nSenote = 3;
					}
					//右の音符が2以上_大音符
					else if (time[DATA + 1] >= 2 && (sort[DATA + 1] == 0x13 || sort[DATA + 1] == 0x14)) {
						list音符のみのリスト[i].nSenote = 3;
					} else {
						list音符のみのリスト[i].nSenote = 4;
					}
					break;
				default:
					doco_count = 0;
					break;
			}
		}
	}

	/// <summary>
	/// サウンドミキサーにサウンドを登録_削除する時刻を事前に算出する
	/// </summary>
	public void PlanToAddMixerChannel() {
		if (OpenTaiko.SoundManager.GetCurrentSoundDeviceType() == "DirectSound") // DShowでの再生の場合はミキシング負荷が高くないため、
		{                                                                       // チップのライフタイム管理を行わない
			return;
		}

		List<CChip> listAddMixerChannel = new List<CChip>(128); ;
		List<CChip> listRemoveMixerChannel = new List<CChip>(128);
		List<CChip> listRemoveTiming = new List<CChip>(128);

		foreach (CChip pChip in listChip) {
			switch (pChip.nChannelNo) {
				// BGM, 演奏チャネル, 不可視サウンド, フィルインサウンド, 空打ち音はミキサー管理の対象
				// BGM:
				case 0x01:
					// Dr演奏チャネル
					//case 0x11:	case 0x12:	case 0x13:	case 0x14:	case 0x15:	case 0x16:	case 0x17:	case 0x18:	case 0x19:	case 0x1A:  case 0x1B:  case 0x1C:
					// Gt演奏チャネル
					//case 0x20:	case 0x21:	case 0x22:	case 0x23:	case 0x24:	case 0x25:	case 0x26:	case 0x27:	case 0x28:
					// Bs演奏チャネル
					//case 0xA0:	case 0xA1:	case 0xA2:	case 0xA3:	case 0xA4:	case 0xA5:	case 0xA6:	case 0xA7:	case 0xA8:
					// Dr不可視チップ
					//case 0x31:	case 0x32:	case 0x33:	case 0x34:	case 0x35:	case 0x36:	case 0x37:
					//case 0x38:	case 0x39:	case 0x3A:
					// Dr/Gt/Bs空打ち
					//case 0xB1:	case 0xB2:	case 0xB3:	case 0xB4:	case 0xB5:	case 0xB6:	case 0xB7:	case 0xB8:
					//case 0xB9:	case 0xBA:	case 0xBB:	case 0xBC:
					// フィルインサウンド
					//case 0x1F:	case 0x2F:	case 0xAF:
					// 自動演奏チップ
					//case 0x61:	case 0x62:	case 0x63:	case 0x64:	case 0x65:	case 0x66:	case 0x67:	case 0x68:	case 0x69:
					//case 0x70:	case 0x71:	case 0x72:	case 0x73:	case 0x74:	case 0x75:	case 0x76:	case 0x77:	case 0x78:	case 0x79:
					//case 0x80:	case 0x81:	case 0x82:	case 0x83:	case 0x84:	case 0x85:	case 0x86:	case 0x87:	case 0x88:	case 0x89:
					//case 0x90:	case 0x91:	case 0x92:

					#region [ 発音1秒前のタイミングを算出 ]
					int n発音前余裕ms = 1000, n発音後余裕ms = 800; {
						int ch = pChip.nChannelNo >> 4;
						if (ch == 0x02 || ch == 0x0A) {
							n発音前余裕ms = 800;
							n発音前余裕ms = 500;
						}
						if (ch == 0x06 || ch == 0x07 || ch == 0x08 || ch == 0x09) {
							n発音前余裕ms = 200;
							n発音前余裕ms = 500;
						}
					}
					#endregion
					#region [ 発音1秒前のタイミングを算出 ]
					int nAddMixer時刻ms, nAddMixer位置 = 0;
					t発声時刻msと発声位置を取得する(pChip.n発声時刻ms - n発音前余裕ms, out nAddMixer時刻ms, out nAddMixer位置);

					CChip c_AddMixer = new CChip() {
						nChannelNo = 0xDA,
						n整数値 = pChip.n整数値,
						n整数値_内部番号 = pChip.n整数値_内部番号,
						n発声時刻ms = nAddMixer時刻ms,
						n発声位置 = nAddMixer位置,
						b演奏終了後も再生が続くチップである = false
					};
					listAddMixerChannel.Add(c_AddMixer);
					#endregion

					int duration = 0;
					if (listWAV.TryGetValue(pChip.n整数値_内部番号, out CTja.CWAV wc)) {
						duration = wc.rSound[0]?.TotalPlayTime ?? 0;
					}
					int n新RemoveMixer時刻ms, n新RemoveMixer位置;
					t発声時刻msと発声位置を取得する(pChip.n発声時刻ms + duration + n発音後余裕ms, out n新RemoveMixer時刻ms, out n新RemoveMixer位置);
					if (n新RemoveMixer時刻ms < pChip.n発声時刻ms + duration)   // 曲の最後でサウンドが切れるような場合は
					{
						CChip c_AddMixer_noremove = c_AddMixer;
						c_AddMixer_noremove.b演奏終了後も再生が続くチップである = true;
						listAddMixerChannel[listAddMixerChannel.Count - 1] = c_AddMixer_noremove;
						break;
					}

					#region [ 発音終了2秒後にmixerから削除するが、その前に再発音することになるのかを確認(再発音ならmixer削除タイミングを延期) ]
					int n整数値 = pChip.n整数値;
					int index = listRemoveTiming.FindIndex(
						delegate (CChip cchip) { return cchip.n整数値 == n整数値; }
					);
					if (index >= 0)                                                 // 過去に同じチップで発音中のものが見つかった場合
					{                                                                   // 過去の発音のmixer削除を確定させるか、延期するかの2択。
						int n旧RemoveMixer時刻ms = listRemoveTiming[index].n発声時刻ms;
						int n旧RemoveMixer位置 = listRemoveTiming[index].n発声位置;

						if (pChip.n発声時刻ms - n発音前余裕ms <= n旧RemoveMixer時刻ms)  // mixer削除前に、同じ音の再発音がある場合は、
						{                                                                   // mixer削除時刻を遅延させる(if-else後に行う)
																							//Debug.WriteLine( "remove TAIL of listAddMixerChannel. TAIL INDEX=" + listAddMixerChannel.Count );
																							//DebugOut_CChipList( listAddMixerChannel );
							listAddMixerChannel.RemoveAt(listAddMixerChannel.Count - 1);    // また、同じチップ音の「mixerへの再追加」は削除する
																							//Debug.WriteLine( "removed result:" );
																							//DebugOut_CChipList( listAddMixerChannel );
						} else                                                            // 逆に、時間軸上、mixer削除後に再発音するような流れの場合は
						{
							listRemoveMixerChannel.Add(listRemoveTiming[index]);    // mixer削除を確定させる
																					//Debug.WriteLine( "listRemoveMixerChannel:" );
																					//DebugOut_CChipList( listRemoveMixerChannel );
																					//listRemoveTiming.RemoveAt( index );
						}
						CChip c = new CChip()                                           // mixer削除時刻を更新(遅延)する
						{
							nChannelNo = 0xDB,
							n整数値 = listRemoveTiming[index].n整数値,
							n整数値_内部番号 = listRemoveTiming[index].n整数値_内部番号,
							n発声時刻ms = n新RemoveMixer時刻ms,
							n発声位置 = n新RemoveMixer位置
						};
						listRemoveTiming[index] = c;
					} else                                                                // 過去に同じチップを発音していないor
					{                                                                   // 発音していたが既にmixer削除確定していたなら
						CChip c = new CChip()                                           // 新しくmixer削除候補として追加する
						{
							nChannelNo = 0xDB,
							n整数値 = pChip.n整数値,
							n整数値_内部番号 = pChip.n整数値_内部番号,
							n発声時刻ms = n新RemoveMixer時刻ms,
							n発声位置 = n新RemoveMixer位置
						};
						listRemoveTiming.Add(c);
					}
					#endregion
					break;
			}
		}

		listChip.AddRange(listAddMixerChannel);
		listChip.AddRange(listRemoveMixerChannel);
		listChip.AddRange(listRemoveTiming);
		listChip = listChip.OrderBy(x => x).ToList();
	}
	private void DebugOut_CChipList(List<CChip> c) {
		for (int i = 0; i < c.Count; i++) {
			Debug.WriteLine(i + ": ch=" + c[i].nChannelNo.ToString("x2") + ", WAV番号=" + c[i].n整数値 + ", time=" + c[i].n発声時刻ms);
		}
	}
	private bool t発声時刻msと発声位置を取得する(int n希望発声時刻ms, out int n新発声時刻ms, out int n新発声位置) {
		// 発声時刻msから発声位置を逆算することはできないため、近似計算する。
		// 具体的には、希望発声位置前後の2つのチップの発声位置の中間を取る。

		int index_min = int.MaxValue, index_max = int.MaxValue;
		for (int i = 0; i < listChip.Count; i++)        // 希望発声位置前後の「前」の方のチップを検索
		{
			int n発声時刻ms = listChip[i].n発声時刻ms;
			if (n発声時刻ms >= n希望発声時刻ms) {
				if (n発声時刻ms > n希望発声時刻ms)
					--i; // is max chip
				index_min = i;
				index_max = i + 1;
				break;
			}
		}
		CChip? chip_min = listChip.ElementAtOrDefault(index_min);
		if (index_min < 0 || chip_min?.n発声時刻ms < n希望発声時刻ms) { // not on chip nor exceeding end
			n新発声時刻ms = n希望発声時刻ms;
			n新発声位置 = chip_min?.n発声位置 ?? 0;
			return true;
		}

		bool isOutOfBound = (index_min >= listChip.Count); // 希望発声時刻に至らずに曲が終了してしまう場合
		if (index_max >= listChip.Count) {
			// listの最終項目の時刻をそのまま使用する
			//___のではダメ。BGMが尻切れになる。
			// そこで、listの最終項目の発声時刻msと発生位置から、希望発声時刻に相当する希望発声位置を比例計算して求める。
			index_min = index_max = listChip.Count - 1;
		}
		n新発声時刻ms = (listChip[index_max].n発声時刻ms + listChip[index_min].n発声時刻ms) / 2;
		n新発声位置 = (listChip[index_max].n発声位置 + listChip[index_min].n発声位置) / 2;
		return !isOutOfBound;
	}

	public void SwapGuitarBassInfos() {
	}

	// SwapGuitarBassInfos_AutoFlags()は、CDTXからCConfigIniに移動。

	// CActivity 実装
	private CCachedFontRenderer pf歌詞フォント;
	public override void Activate() {
		if (OpenTaiko.rCurrentStage.eStageID == CStage.EStage.SongLoading) {
			//まさかこれが原因で曲の読み込みが停止するとは思わなかった...
			//どういうことかというとスキンを読み込むときに...いや厳密には
			//RefleshSkinを呼び出した後一回Disposeしてnullにして解放(その後にまたインスタンスを作成する)するんだけど
			//その時にここでTJAPlayer3.Skinを参照して例外が出ていたんだ....
			//いやいや! なんでTJAPlayer3.Skinをnullにした瞬間に参照されるんだ!と思った方もいるかもしれないですが
			//実は曲の読み込みはマルチスレッドで実行されているのでnullにした瞬間に参照される可能性も十分にある
			//それならアプリが終了するんじゃないかと思ったのだけどtryを使ってい曲の読み込みを続行していた...
			//いやーマルチスレッドって難しいね!
			if (!string.IsNullOrEmpty(OpenTaiko.Skin.Game_Lyric_FontName)) {
				this.pf歌詞フォント = new CCachedFontRenderer(OpenTaiko.Skin.Game_Lyric_FontName, OpenTaiko.Skin.Game_Lyric_FontSize);
			} else {
				this.pf歌詞フォント = new CCachedFontRenderer(CFontRenderer.DefaultFontName, OpenTaiko.Skin.Game_Lyric_FontSize);
			}
		}
		this.listWAV = new Dictionary<int, CWAV>();
		this.listBPM = new Dictionary<int, CBPM>();
		this.listJPOSSCROLL = new List<CJPOSSCROLL>();
		this.listVD = new Dictionary<int, CVideoDecoder>();
		this.listChip = new List<CChip>();
		this.listChip_Branch = new List<CChip>[3];
		this.listChip_Branch[0] = new List<CChip>();
		this.listChip_Branch[1] = new List<CChip>();
		this.listChip_Branch[2] = new List<CChip>();
		this.listBarLineChip = new List<CChip>();
		this.listNoteChip = new List<CChip>();
		this.listBalloon = new List<int>();
		this.listBalloon_Branch = new[] { new List<int>(), new List<int>(), new List<int>() };
		this.listLine = new List<CLine>();
		this.listLyric = new List<SKBitmap>();
		this.listLyric2 = new List<STLYRIC>();
		this.List_DanSongs = new List<DanSongs>();
		this.listObj = new Dictionary<string, CSongObject>();
		this.listTextures = new Dictionary<string, CTexture>();
		this.listOriginalTextures = new Dictionary<string, CTexture>();
		this.currentObjAnimations = new Dictionary<string, CChip>();
		base.Activate();
	}
	public override void DeActivate() {
		if (this.listWAV != null) {
			foreach (CWAV cwav in this.listWAV.Values) {
				cwav.Dispose();
			}
			this.listWAV.Clear();
		}
		if (this.listVD != null) {
			foreach (CVideoDecoder cvd in this.listVD.Values) {
				cvd.Dispose();
			}
			this.listVD.Clear();
		}
		this.listBPM?.Clear();
		this.listJPOSSCROLL?.Clear();
		this.List_DanSongs?.Clear();
		this.listChip?.Clear();
		this.listBarLineChip?.Clear();
		this.listNoteChip?.Clear();

		this.listBalloon?.Clear();
		foreach (var listBalloon in this.listBalloon_Branch)
			listBalloon?.Clear();

		this.listLyric?.Clear();
		this.listLyric2?.Clear();

		if (this.listObj != null) {
			foreach (KeyValuePair<string, CSongObject> pair in this.listObj) {
				pair.Value.tDispose();
			}
			this.listObj.Clear();
		}

		if (this.listOriginalTextures != null) {
			foreach (KeyValuePair<string, CTexture> pair in this.listOriginalTextures) {
				string txPath = pair.Key;
				CTexture originalTx = pair.Value;
				OpenTaiko.Tx.trackedTextures.TryGetValue(txPath, out CTexture oldTx);

				if (oldTx != originalTx) {
					oldTx.UpdateTexture(originalTx, originalTx.sz画像サイズ.Width, originalTx.sz画像サイズ.Height);
				}
			}
			this.listOriginalTextures.Clear();
		}

		if (this.listTextures != null) {
			foreach (KeyValuePair<string, CTexture> pair in this.listTextures) {
				pair.Value.Dispose();
			}
			this.listTextures.Clear();
		}

		base.DeActivate();
	}
	public override void CreateManagedResource() {
		if (!base.IsDeActivated) {
			this.tAVIの読み込み();
			base.CreateManagedResource();
		}
	}
	public override void ReleaseManagedResource() {
		if (!base.IsDeActivated) {
			if (this.listVD != null) {
				foreach (CVideoDecoder cvd in this.listVD.Values) {
					cvd.Dispose();
				}
				this.listVD = null;
			}
			OpenTaiko.tDisposeSafely(ref this.pf歌詞フォント);
			base.ReleaseManagedResource();
		}
	}

	// その他

	#region [ private ]
	private bool bヘッダのみ;
	private Stack<bool> bstackIFからENDIFをスキップする;

	private int n現在の行数;
	private int n現在の乱数;

	private int nPolyphonicSounds = 4;                          // #28228 2012.5.1 yyagi

	private int n内部番号BPM1to;
	private int n内部番号WAV1to;
	private int[] n無限管理BPM;
	private int[] n無限管理PAN;
	private int[] n無限管理SIZE;
	private int[] n無限管理WAV;
	private int[] nRESULTIMAGE用優先順位;
	private int[] nRESULTMOVIE用優先順位;
	private int[] nRESULTSOUND用優先順位;

	private CChip currentCamVMoveChip;
	private CChip currentCamHMoveChip;
	private CChip currentCamRotateChip;
	private CChip currentCamZoomChip;
	private CChip currentCamVScaleChip;
	private CChip currentCamHScaleChip;

	private Dictionary<string, CChip> currentObjAnimations;

	private void t行のコメント処理(ref string strText) {
		int nCommentPos = strText.IndexOf("//");
		if (nCommentPos != -1)
			strText = strText.Remove(nCommentPos);
	}

	/// <summary>
	/// 音源再生前の空白を追加するメソッド。
	/// </summary>
	private void AddPreBakedMusicPreTimeMs() {
		this.dbNowTime += OpenTaiko.ConfigIni.MusicPreTimeMs;
		this.dbNowBMScollTime += OpenTaiko.ConfigIni.MusicPreTimeMs * this.dbNowBPM / 15000;
	}
	//-----------------
	#endregion

	// Time coordination converters

	// DefTime is the time relative to the initial measure of the first song.
	// RawTjaTime is time for chip.n発声時刻ms just before the post-processing of tja.t入力_V4().
	// * RawTjaTime is DefTime with additional initial padding time
	// MusicPreTimeMs is pre-baked to only Dan
	public double DefTimeToRawTjaTime(double msTime)
		=> (this.n参照中の難易度 != (int)Difficulty.Dan) ? msTime
			: msTime + OpenTaiko.ConfigIni.MusicPreTimeMs;
	public double RawTjaTimeToDefTime(double msTime)
		=> (this.n参照中の難易度 != (int)Difficulty.Dan) ? msTime
			: msTime - OpenTaiko.ConfigIni.MusicPreTimeMs;

	// TjaTime is the time for chip.n発声時刻ms after the post-processing of tja.t入力_V4().
	// * For positive msOFFSET, all and only music-time-relative events are delayed by msOFFSET_Abs.
	// * For negative msOFFSET, all and only note-time-relative events are delayed by msOFFSET_Abs.
	public double RawTjaTimeToTjaTimeMusic(double msTime)
		=> msTime + (!this.isOFFSET_Negative ? this.msOFFSET_Abs : 0);
	public double TjaTimeToRawTjaTimeMusic(double msTime)
		=> msTime - (!this.isOFFSET_Negative ? this.msOFFSET_Abs : 0);
	public double RawTjaTimeToTjaTimeNote(double msTime)
		=> msTime + (this.isOFFSET_Negative ? this.msOFFSET_Abs : 0);
	public double TjaTimeToRawTjaTimeNote(double msTime)
		=> msTime - (this.isOFFSET_Negative ? this.msOFFSET_Abs : 0);

	// GameTime is the real elapsed time of gameplay.
	// SongPlaybackSpeed scales the GameTime into the corresponding TjaTime
	// MusicPreTimeMs is applied in real time to non-Dan
	public double GameTimeToTjaTime(double msTime)
		=> GameDurationToTjaDuration((this.n参照中の難易度 == (int)Difficulty.Dan) ?
			msTime
			: msTime - OpenTaiko.ConfigIni.MusicPreTimeMs);
	public double TjaTimeToGameTime(double msTime) {
		msTime = TjaDurationToGameDuration(msTime);
		return (this.n参照中の難易度 == (int)Difficulty.Dan) ? msTime
			: msTime + OpenTaiko.ConfigIni.MusicPreTimeMs;
	}

	// GameDuration is time duration per beat.
	// These converters are unit-independent.
	public static double GameDurationToTjaDuration(double duration)
		=> duration * OpenTaiko.ConfigIni.SongPlaybackSpeed;
	public static double TjaDurationToGameDuration(double duration)
		=> duration / OpenTaiko.ConfigIni.SongPlaybackSpeed;

	// BeatSpeed (including BPM) is the reciprocal of time duration per beat.
	public static double GameBeatSpeedToTjaBeatSpeed(double beatSpeed)
		=> beatSpeed / OpenTaiko.ConfigIni.SongPlaybackSpeed;
	public static double TjaBeatSpeedToGameBeatSpeed(double beatSpeed)
		=> beatSpeed * OpenTaiko.ConfigIni.SongPlaybackSpeed;
}
