using FDK;
using static OpenTaiko.CTja;

namespace OpenTaiko;

internal class CChip : IComparable<CChip>, ICloneable {
	public EScrollMode eScrollMode;
	public EScrollMode eScrollMode_end;
	public bool bHit;
	public bool bVisible = true;
	public bool bHideBarLine = true;
	public bool bProcessed = false;
	public bool bShow;
	public bool bShowRoll;
	public bool bBranch = false;
	public double dbChipSizeRatio = 1.0;
	public double db実数値;
	public double dbBPM;
	public double dbBPM_end;
	public float fNow_Measure_s = 4.0f;//強制分岐のために追加.2020.04.21.akasoko26
	public float fNow_Measure_m = 4.0f;//強制分岐のために追加.2020.04.21.akasoko26
	public bool IsEndedBranching = false;//分岐が終わった時の連打譜面が非可視化になってしまうためフラグを追加.2020.04.21.akasoko26
	public double dbSCROLL;
	public double dbSCROLL_Y;
	public double dbSCROLL_end;
	public double dbSCROLL_Y_end;
	public ECourse nBranch;
	public int nSenote;
	public int nState;
	public int nRollCount;
	public int nBalloon;
	public int nProcessTime;
	public int nScrollDirection;
	public int nDisplayPriority; //(特殊)現状連打との判断目的で使用
	public ENoteState eNoteState;
	public int nChannelNo;
	public int VideoStartTimeMs;
	public int nHorizontalChipDistance;
	public int nNoteTipDistance_X;
	public int nNoteTipDistance_Y;
	public int n整数値;
	public int n文字数 = 16;

	public int n整数値_内部番号;
	public int nOpacity = 255;
	public int n発声位置;
	public double nBranchCondition1_Professional;
	public double nBranchCondition2_Master;
	public EBranchConditionType eBranchCondition;

	public double db発声位置;  // 発声時刻を格納していた変数のうちの１つをfloat型からdouble型に変更。(kairera0467)
	public double fBMSCROLLTime;
	public double fBMSCROLLTime_end;
	public int n発声時刻ms;
	public double n分岐時刻ms;


	public double db発声時刻ms;
	public int nNoteEndPosition;
	public int nNoteEndTimems;
	public int nノーツ出現時刻ms;
	public int nノーツ移動開始時刻ms;
	public int n分岐回数;
	public int n連打音符State;
	public int nLag;                // 2011.2.1 yyagi
	public double db発声時刻;
	public double db判定終了時刻;//連打系音符で使用
	public double dbProcess_Time;
	public int nPlayerSide;
	public bool bGOGOTIME = false; //2018.03.11 k1airera0467 ゴーゴータイム内のチップであるか
	public int nListPosition;
	public bool IsFixedSENote;
	public bool IsHitted = false;
	public bool IsMissed = false;



	//EXTENDED COMMANDS
	public int fCamTimeMs;
	public string strCamEaseType;
	public Easing.CalcType fCamMoveType;

	public float fCamScrollStartX;
	public float fCamScrollStartY;
	public float fCamScrollEndX;
	public float fCamScrollEndY;

	public float fCamRotationStart;
	public float fCamRotationEnd;

	public float fCamZoomStart;
	public float fCamZoomEnd;

	public float fCamScaleStartX;
	public float fCamScaleStartY;
	public float fCamScaleEndX;
	public float fCamScaleEndY;

	public Color4 borderColor;

	public int fObjTimeMs;
	public string strObjName;
	public string strObjEaseType;
	public Easing.CalcType objCalcType;

	public float fObjX;
	public float fObjY;

	public float fObjStart;
	public float fObjEnd;

	public CSongObject obj;

	public string strTargetTxName;
	public string strNewPath;

	public string strConfigValue;

	public double dbAnimInterval;

	public int intFrame;

	public EGameType eGameType;
	//


	public bool b自動再生音チャンネルである {
		get {
			int num = this.nChannelNo;
			if ((((num != 1) && ((0x61 > num) || (num > 0x69))) && ((0x70 > num) || (num > 0x79))) && ((0x80 > num) || (num > 0x89))) {
				return ((0x90 <= num) && (num <= 0x92));
			}
			return true;
		}
	}



	public bool b演奏終了後も再生が続くチップである; // #32248 2013.10.14 yyagi
	public CCounter? RollDelay; // 18.9.22 AioiLight Add 連打時に赤くなるやつのタイマー
	public CCounter? RollInputTime; // 18.9.22 AioiLight Add  連打入力後、RollDelayが作動するまでのタイマー
	public int RollEffectLevel; // 18.9.22 AioiLight Add 連打時に赤くなるやつの度合い

	public void ResetRollEffect() {
		this.RollInputTime?.Stop();
		this.RollInputTime = null;
		this.RollDelay?.Stop();
		this.RollDelay = null;
		this.RollEffectLevel = 0;
	}

	public CChip() {
		this.nHorizontalChipDistance = 0;
	}
	public void t初期化() {
		this.bBranch = false;
		this.nChannelNo = 0;
		this.n整数値 = 0; //整数値をList上の番号として用いる。
		this.n整数値_内部番号 = 0;
		this.db実数値 = 0.0;
		this.n発声位置 = 0;
		this.db発声位置 = 0.0D;
		this.n発声時刻ms = 0;
		this.db発声時刻ms = 0.0D;
		this.fBMSCROLLTime = 0;
		this.nNoteEndPosition = 0;
		this.nNoteEndTimems = 0;
		this.nDisplayPriority = 0;
		this.nLag = -999;
		this.b演奏終了後も再生が続くチップである = false;
		this.nListPosition = 0;                                 // Unused
		this.dbChipSizeRatio = 1.0;                             // Unused
		this.bHit = false;
		this.IsMissed = false;
		this.bVisible = true;
		this.nOpacity = 0xff;
		this.nHorizontalChipDistance = 0;
		this.nNoteTipDistance_X = 0;
		this.nNoteTipDistance_Y = 0;
		this.dbBPM_end = this.dbBPM = 120.0;
		this.fNow_Measure_m = 4.0f;
		this.fNow_Measure_s = 4.0f;
		this.nScrollDirection = 0;
		this.dbSCROLL_end = this.dbSCROLL = 1.0;
		this.dbSCROLL_Y_end = this.dbSCROLL_Y = 0.0f;
	}
	public override string ToString() {

		//2016.10.07 kairera0467 近日中に再編成予定
		string[] chToStr =
		{
				//システム
				"??", "バックコーラス", "小節長変更", "BPM変更", "??", "??", "??", "??",
				"BPM変更(拡張)", "??", "??", "??", "??", "??", "??", "??",

				//太鼓1P(移動予定)
				"??", "ドン", "カツ", "ドン(大)", "カツ(大)", "連打", "連打(大)", "ふうせん連打",
				"連打終点", "芋", "ドン(手)", "カッ(手)", "Mine", "??", "??", "AD-LIB",

				//太鼓予備
				"??", "??", "??", "??", "??", "??", "??", "??",
				"??", "??", "??", "??", "??", "??", "??", "??",

				//太鼓予備
				"??", "??", "??", "??", "??", "??", "??", "??",
				"??", "??", "??", "??", "??", "??", "??", "??",

				//太鼓予備
				"??", "??", "??", "??", "??", "??", "??", "??",
				"??", "??", "??", "??", "??", "??", "??", "??",

				//システム
				"小節線", "拍線", "??", "??", "AVI", "??", "??", "??",
				"??", "??", "??", "??", "??", "??", "??", "??",

				//システム(移動予定)
				"SCROLL", "DELAY", "ゴーゴータイム開始", "ゴーゴータイム終了", "カメラ移動開始(縦)", "カメラ移動終了(縦)", "カメラ移動開始(横)", "カメラ移動終了(横)",
				"カメラズーム開始", "カメラズーム終了", "カメラ回転開始", "カメラ回転終了", "カメラスケーリング開始(横)", "カメラスケーリング終了(横)", "カメラスケーリング開始(縦)", "カメラスケーリング終了(縦)",

				"ボーダーカラー変更", "??", "??", "??", "??", "??", "??", "??",
				"??", "??", "??", "??", "??", "??", "??", "??",

				"??", "??", "??", "??", "??", "??", "??", "??",
				"??", "??", "??", "??", "??", "??", "??", "??",

				//太鼓1P、システム(現行)
				"??", "??", "??", "太鼓_赤", "太鼓_青", "太鼓_赤(大)", "太鼓_青(大)", "太鼓_黄",
				"太鼓_黄(大)", "太鼓_風船", "太鼓_連打末端", "太鼓_芋", "??", "SCROLL", "ゴーゴータイム開始", "ゴーゴータイム終了",

				"??", "??", "??", "??", "??", "??", "??", "??",
				"??", "??", "??", "??", "??", "??", "??", "太鼓 AD-LIB",

				"??", "??", "??", "??", "??", "??", "??", "??",
				"??", "??", "??", "??", "??", "??", "??", "??",

				"??", "??", "??", "??", "0xC4", "0xC5", "0xC6", "??",
				"??", "??", "0xCA", "??", "??", "??", "??", "0xCF",

				//システム(現行)
				"0xD0", "??", "??", "??", "??", "??", "??", "??",
				"??", "??", "ミキサー追加", "ミキサー削除", "DELAY", "譜面分岐リセット", "譜面分岐アニメ", "譜面分岐内部処理",

				//システム(現行)
				"小節線ON/OFF", "分岐固定", "判定枠移動", "", "", "", "", "",
				"", "", "", "", "", "", "", "",

				"0xF0", "歌詞", "??", "SUDDEN", "??", "??", "??", "??",
				"??", "??", "??", "??", "??", "??", "??", "??", "譜面終了",

				// Extra notes

				"KaDon", "??", "??", "??", "??", "??", "??", "??",
				"??", "??", "??", "??", "??", "??", "??", "??",
			};
		return string.Format("CChip: 位置:{0:D4}.{1:D3}, 時刻{2:D6}, Ch:{3:X2}({4}), Pn:{5}({11})(内部{6}), Pd:{7}, Sz:{8}, BMScroll:{9}, Auto:{10}, コース:{11}",
			this.n発声位置 / 384, this.n発声位置 % 384,
			this.n発声時刻ms,
			this.nChannelNo, chToStr[this.nChannelNo],
			this.n整数値, this.n整数値_内部番号,
			this.db実数値,
			this.dbChipSizeRatio,
			this.fBMSCROLLTime,
			this.b自動再生音チャンネルである,
			this.nBranch,
			CTja.tZZ(this.n整数値));
	}
	/// <summary>
	/// チップの再生長を取得する。現状、WAVチップとBGAチップでのみ使用可能。
	/// </summary>
	/// <returns>再生長(ms)</returns>
	public int GetDuration() {
		int nDuration = 0;

		if (this.nChannelNo == 0x01)       // WAV
		{
			CTja.CWAV wc;
			OpenTaiko.TJA.listWAV.TryGetValue(this.n整数値_内部番号, out wc);
			if (wc == null) {
				nDuration = 0;
			} else {
				nDuration = (wc.rSound[0] == null) ? 0 : wc.rSound[0].TotalPlayTime;
			}
		} else if (this.nChannelNo == 0x54) // AVI
		{
			CVideoDecoder wc;
			OpenTaiko.TJA.listVD.TryGetValue(this.n整数値_内部番号, out wc);
			if (wc == null) {
				nDuration = 0;
			} else {
				nDuration = (int)(wc.Duration * 1000);
			}
		}

		double _db再生速度 = OpenTaiko.TJA.db再生速度;
		return (int)(nDuration / _db再生速度);
	}

	#region [ IComparable 実装 ]
	//-----------------

	private static readonly byte[] n優先度 = new byte[] {
			5, 5, 3, 7, 5, 5, 5, 5, 3, 5, 5, 5, 5, 5, 5, 5, //0x00
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x10
			7, 7, 7, 7, 7, 7, 7, 7, 5, 5, 5, 5, 5, 5, 5, 5, //0x20
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x30
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x40
			9, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x50
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x60
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x70
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x80
			5, 5, 5, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 9, 9, 9, //0x90
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0xA0
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0xB0
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0xC0
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 3, 4, 4, //0xD0
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0xE0
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0xF0
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x100
		};

	public int CompareTo(CChip other) {
		// まずは位置で比較。

		//BGMチップだけ発声位置
		//if( this.nチャンネル番号 == 0x01 || this.nチャンネル番号 == 0x02 )
		//{
		//    if( this.n発声位置 < other.n発声位置 )
		//        return -1;

		//    if( this.n発声位置 > other.n発声位置 )
		//        return 1;
		//}

		//if( this.n発声位置 < other.n発声位置 )
		//    return -1;

		//if( this.n発声位置 > other.n発声位置 )
		//    return 1;

		//譜面解析メソッドV4では発声時刻msで比較する。
		var n発声時刻msCompareToResult = 0;
		n発声時刻msCompareToResult = this.n発声時刻ms.CompareTo(other.n発声時刻ms);
		if (n発声時刻msCompareToResult != 0) {
			return n発声時刻msCompareToResult;
		}

		n発声時刻msCompareToResult = this.db発声時刻ms.CompareTo(other.db発声時刻ms);
		if (n発声時刻msCompareToResult != 0) {
			return n発声時刻msCompareToResult;
		}

		// 位置が同じなら優先度で比較。
		return n優先度[this.nChannelNo].CompareTo(n優先度[other.nChannelNo]);
	}
	//-----------------
	#endregion


	/// <summary>
	/// shallow copy。
	/// </summary>
	/// <returns></returns>
	public object Clone() {
		return MemberwiseClone();
	}
}
