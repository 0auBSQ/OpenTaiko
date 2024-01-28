using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace TJAPlayer3
{

    /// <summary>
    /// 難易度。
    /// </summary>
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
        Oni,
        Edit,
        Tower,
        Dan,
        Total
    }

    public enum EScrollMode
    {
        Normal,
        BMSCROLL,
        HBSCROLL
    }
    public enum Eジャンル
    {
        None = 0,
        JPOP = 1,
        ゲーム = 2,
        ナムコ = 3,
        クラシック = 4,
        バラエティ = 5,
        どうよう = 6,
        ボーカロイド = 7,
        アニメ = 8
    }
    public enum EGame
    {
        OFF = 0,
        完走叩ききりまショー = 1,
        完走叩ききりまショー激辛 = 2
    }
    public enum E難易度表示タイプ
    {
        OFF = 0,
        n曲目に表示 = 1,
        mtaikoに画像で表示 = 2,
    }
	public enum Eダークモード
	{
		OFF,
		HALF,
		FULL
	}
    public enum EWindowMovieMode
    {
        OFF = 0,
        左下 = 1,
        中央下 = 2
    }
	public enum Eダメージレベル
	{
		少ない	= 0,
		普通	= 1,
		大きい	= 2
	}
	public enum EPad			// 演奏用のenum。ここを修正するときは、次に出てくる EKeyConfigPad と EパッドFlag もセットで修正すること。
	{
		HH		= 0,
		R		= 0,
		SD		= 1,
		G		= 1,
		BD		= 2,
		B		= 2,
		HT		= 3,
		Pick	= 3,
		LT		= 4,
		Wail	= 4,
		FT		= 5,
		Cancel	= 5,
		CY		= 6,
		Decide	= 6,
		HHO		= 7,
		RD		= 8,
		LC		= 9,
		LP		= 10,	// #27029 2012.1.4 from
        LBD     = 11,

        LRed    = 12,
        RRed    = 13,
        LBlue   = 14,
        RBlue   = 15,

        LRed2P  = 16,
        RRed2P  = 17,
        LBlue2P = 18,
        RBlue2P = 19,

		LRed3P = 20,
		RRed3P = 21,
		LBlue3P = 22,
		RBlue3P = 23,

		LRed4P = 24,
		RRed4P = 25,
		LBlue4P = 26,
		RBlue4P = 27,

		LRed5P = 28,
		RRed5P = 29,
		LBlue5P = 30,
		RBlue5P = 31,

		CLAP	= 32,
		CLAP2P	= 33,
		CLAP3P = 34,
		CLAP4P = 35,
		CLAP5P = 36,
		LeftChange = 37,
		RightChange = 38,

		MAX,			// 門番用として定義
		UNKNOWN = 99
	}
	public enum EKeyConfigPad		// #24609 キーコンフィグで使うenum。capture要素あり。
	{
		HH		= EPad.HH,
		R		= EPad.R,
		SD		= EPad.SD,
		G		= EPad.G,
		BD		= EPad.BD,
		B		= EPad.B,
		HT		= EPad.HT,
		Pick	= EPad.Pick,
		LT		= EPad.LT,
		Wail	= EPad.Wail,
		FT		= EPad.FT,
		Cancel	= EPad.Cancel,
		CY		= EPad.CY,
		Decide	= EPad.Decide,
		HHO		= EPad.HHO,
		RD		= EPad.RD,
		LC		= EPad.LC,
		LP		= EPad.LP,		// #27029 2012.1.4 from
        LBD     = EPad.LBD,
		#region [Gameplay Keys]
        LRed    = EPad.LRed,
        RRed    = EPad.RRed,
        LBlue   = EPad.LBlue,
        RBlue   = EPad.RBlue,

        LRed2P  = EPad.LRed2P,
        RRed2P  = EPad.RRed2P,
        LBlue2P = EPad.LBlue2P,
        RBlue2P = EPad.RBlue2P,

		LRed3P = EPad.LRed3P,
		RRed3P = EPad.RRed3P,
		LBlue3P = EPad.LBlue3P,
		RBlue3P = EPad.RBlue3P,

		LRed4P = EPad.LRed4P,
		RRed4P = EPad.RRed4P,
		LBlue4P = EPad.LBlue4P,
		RBlue4P = EPad.RBlue4P,

		LRed5P = EPad.LRed5P,
		RRed5P = EPad.RRed5P,
		LBlue5P = EPad.LBlue5P,
		RBlue5P = EPad.RBlue5P,

		Clap	= EPad.CLAP,
		Clap2P = EPad.CLAP2P,
		Clap3P = EPad.CLAP3P,
		Clap4P = EPad.CLAP4P,
		Clap5P = EPad.CLAP5P,
		LeftChange = EPad.LeftChange,
		RightChange = EPad.RightChange,
		#endregion
		#region [System Keys]
		Capture,
		SongVolumeIncrease,
		SongVolumeDecrease,
		DisplayHits,
		DisplayDebug,
		#region [Song Select only]
		QuickConfig,
		NewHeya,
		SortSongs,
		ToggleAutoP1,
		ToggleAutoP2,
		ToggleTrainingMode,
		#endregion
		MAX,
		#endregion
		UNKNOWN = EPad.UNKNOWN
	}
	[Flags]
	public enum EパッドFlag		// #24063 2011.1.16 yyagi コマンド入力用 パッド入力のフラグ化
	{
		None	= 0,
		HH		= 1,
		R		= 1,
		SD		= 2,
		G		= 2,
		B		= 4,
		BD		= 4,
		HT		= 8,
		Pick	= 8,
		LT		= 16,
		Wail	= 16,
		FT		= 32,
		Cancel	= 32,
		CY		= 64,
		Decide	= 128,
		HHO		= 128,
		RD		= 256,
		LC		= 512,
		LP		= 1024,				// #27029
        LBD     = 2048,
        LRed    = 0,
        RRed    = 1,
        LBlue   = 2,
        RBlue   = 4,
        LRed2P  = 8,
        RRed2P  = 16,
        LBlue2P = 32,
        RBlue2P = 64,
		UNKNOWN = 4096
	}
	public enum Eランダムモード
	{
		OFF,
		RANDOM,
        MIRROR,
		SUPERRANDOM,
		HYPERRANDOM
	}

	public enum EFunMods
    {
		NONE,
		AVALANCHE,
		MINESWEEPER,
		TOTAL,
    }

	public enum EGameType
    {
		TAIKO = 0,
		KONGA = 1,
    }

	public enum EInstrumentPad		// ここを修正するときは、セットで次の EKeyConfigPart も修正すること。
	{
		DRUMS	= 0,
		GUITAR	= 1,
		BASS	= 2,
        TAIKO   = 3,
		UNKNOWN	= 99
	}
	public enum EKeyConfigPart	// : E楽器パート
	{
		DRUMS	= EInstrumentPad.DRUMS,
		GUITAR	= EInstrumentPad.GUITAR,
		BASS	= EInstrumentPad.BASS,
        TAIKO   = EInstrumentPad.TAIKO,
		SYSTEM,
		UNKNOWN	= EInstrumentPad.UNKNOWN
	}

	public enum E打ち分け時の再生の優先順位
	{
		ChipがPadより優先,
		PadがChipより優先
	}
	internal enum EInputDevice
	{
		Keyboard		= 0,
		MIDIInput		= 1,
		Joypad	= 2,
		Mouse			= 3,
		Gamepad = 4,
		Unknown			= -1
	}
	public enum ENoteJudge
	{
		Perfect	= 0,
		Great	= 1,
		Good	= 2,
		Poor	= 3,
		Miss	= 4,
		Bad		= 5,
		Auto	= 6,
		ADLIB	= 7,
		Mine	= 8,
	}
	internal enum E判定文字表示位置
	{
		表示OFF,
		レーン上,
		判定ライン上,
		コンボ下
	}
	internal enum E判定位置
	{
		標準	= 0,
		Lower,
		MAX
	}
	internal enum E判定表示優先度
	{
		Chipより下,
		Chipより上
	}
	internal enum EAVI種別
	{
		Unknown,
		AVI,
		AVIPAN
	}
	internal enum EFIFOモード
	{
		フェードイン,
		フェードアウト
	}
	internal enum Eレーン
	{
		LC = 0,
		HH,
		SD,
		BD,
		HT,
		LT,
		FT,
		CY,
        LP,
		RD,		// 将来の独立レーン化/独立AUTO設定を見越して追加
        LBD = 10,
		Guitar,	// AUTOレーン判定を容易にするため、便宜上定義しておく(未使用)
		Bass,	// (未使用)
		GtR,
		GtG,
		GtB,
		GtPick,
		GtW,
		BsR,
		BsG,
		BsB,
		BsPick,
		BsW,
		MAX,	// 要素数取得のための定義 ("BGM"は使わない前提で)
		BGM
	}
	internal enum Eレーン数
	{
		物理 = 8,	   // LC, HH,             SD, BD, HT, LT, FT, CY
		論理 = 10,	   // LC, HO, HC,         SD, BD, HT, LT, FT, RC, RD
        DTXG物理 = 10, // LC, HH,     LP,     SD, BD, HT, LT, FT, CY, RD
        DTXG論理 = 12  // LC, HO, HC, LP, LB, SD, BD, HT, LT, FT, CY, RD 
	}
	internal enum Eログ出力
	{
		OFF,
		ON通常,
		ON詳細あり
	}
	internal enum E演奏画面の戻り値
	{
		継続,
		演奏中断,
		ステージ失敗,
		ステージクリア,
		再読込_再演奏,
		再演奏
	}
	internal enum E曲読込画面の戻り値
	{
		継続 = 0,
		読込完了,
		読込中止
	}

    public enum ENoteState
    {
        none,
        wait,
        perfect,
        grade,
        bad
    }

    public enum E連打State
    {
        none,
        roll,
        rollB,
        balloon,
        potato
    }

    public enum EStealthMode
    {
        OFF = 0,
        DORON = 1,
        STEALTH = 2
    }

	/// <summary>
	/// 透明チップの種類
	/// </summary>
	public enum EInvisible
	{
		OFF,		// チップを透明化しない
		SEMI,		// Poor/Miss時だけ、一時的に透明解除する
		FULL		// チップを常に透明化する
	}

	/// <summary>
	/// Drum/Guitar/Bass の値を扱う汎用の構造体。
	/// </summary>
	/// <typeparam name="T">値の型。</typeparam>
	[Serializable]
	[StructLayout( LayoutKind.Sequential )]
	public struct STDGBVALUE<T>			// indexはE楽器パートと一致させること
	{
		public T Drums;
		public T Guitar;
		public T Bass;
        public T Taiko;
		public T Unknown;
		public T this[ int index ]
		{
			get
			{
				switch( index )
				{
					case (int) EInstrumentPad.DRUMS:
						return this.Drums;

					case (int) EInstrumentPad.GUITAR:
						return this.Guitar;

					case (int) EInstrumentPad.BASS:
						return this.Bass;

                    case (int) EInstrumentPad.TAIKO:
                        return this.Taiko;

					case (int) EInstrumentPad.UNKNOWN:
						return this.Unknown;
				}
				throw new IndexOutOfRangeException();
			}
			set
			{
				switch( index )
				{
					case (int) EInstrumentPad.DRUMS:
						this.Drums = value;
						return;

					case (int) EInstrumentPad.GUITAR:
						this.Guitar = value;
						return;

					case (int) EInstrumentPad.BASS:
						this.Bass = value;
						return;

                    case (int) EInstrumentPad.TAIKO:
                        this.Taiko = value;
                        return;

					case (int) EInstrumentPad.UNKNOWN:
						this.Unknown = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}
	}

	/// <summary>
	/// レーンの値を扱う汎用の構造体。列挙型"Eドラムレーン"に準拠。
	/// </summary>
	/// <typeparam name="T">値の型。</typeparam>
	[StructLayout( LayoutKind.Sequential )]
	public struct STLANEVALUE<T>
	{
		public T LC;
		public T HH;
		public T SD;
        public T LP;
        public T LBD;
		public T BD;
		public T HT;
		public T LT;
		public T FT;
		public T CY;
		public T RD;
		public T Guitar;
		public T Bass;
		public T GtR;
		public T GtG;
		public T GtB;
		public T GtPick;
		public T GtW;
		public T BsR;
		public T BsG;
		public T BsB;
		public T BsPick;
		public T BsW;
		public T BGM;

		public T this[ int index ]
		{
			get
			{
				switch ( index )
				{
					case (int) Eレーン.LC:
						return this.LC;
					case (int) Eレーン.HH:
						return this.HH;
					case (int) Eレーン.SD:
						return this.SD;
                    case (int) Eレーン.LP:
                        return this.LP;
                    case (int) Eレーン.LBD:
                        return this.LBD;
					case (int) Eレーン.BD:
						return this.BD;
					case (int) Eレーン.HT:
						return this.HT;
					case (int) Eレーン.LT:
						return this.LT;
					case (int) Eレーン.FT:
						return this.FT;
					case (int) Eレーン.CY:
						return this.CY;
					case (int) Eレーン.RD:
						return this.RD;
					case (int) Eレーン.Guitar:
						return this.Guitar;
					case (int) Eレーン.Bass:
						return this.Bass;
					case (int) Eレーン.GtR:
						return this.GtR;
					case (int) Eレーン.GtG:
						return this.GtG;
					case (int) Eレーン.GtB:
						return this.GtB;
					case (int) Eレーン.GtPick:
						return this.GtPick;
					case (int) Eレーン.GtW:
						return this.GtW;
					case (int) Eレーン.BsR:
						return this.BsR;
					case (int) Eレーン.BsG:
						return this.BsG;
					case (int) Eレーン.BsB:
						return this.BsB;
					case (int) Eレーン.BsPick:
						return this.BsPick;
					case (int) Eレーン.BsW:
						return this.BsW;
				}
				throw new IndexOutOfRangeException();
			}
			set
			{
				switch ( index )
				{
					case (int) Eレーン.LC:
						this.LC = value;
						return;
					case (int) Eレーン.HH:
						this.HH = value;
						return;
					case (int) Eレーン.SD:
						this.SD = value;
						return;
                    case (int) Eレーン.LP:
                        this.LP = value;
                        return;
                    case (int) Eレーン.LBD:
                        this.LBD = value;
                        return;
					case (int) Eレーン.BD:
						this.BD = value;
						return;
					case (int) Eレーン.HT:
						this.HT = value;
						return;
					case (int) Eレーン.LT:
						this.LT = value;
						return;
					case (int) Eレーン.FT:
						this.FT = value;
						return;
					case (int) Eレーン.CY:
						this.CY = value;
						return;
					case (int) Eレーン.RD:
						this.RD = value;
						return;
					case (int) Eレーン.Guitar:
						this.Guitar = value;
						return;
					case (int) Eレーン.Bass:
						this.Bass = value;
						return;
					case (int) Eレーン.GtR:
						this.GtR = value;
						return;
					case (int) Eレーン.GtG:
						this.GtG = value;
						return;
					case (int) Eレーン.GtB:
						this.GtB = value;
						return;
					case (int) Eレーン.GtPick:
						this.GtPick = value;
						return;
					case (int) Eレーン.GtW:
						this.GtW = value;
						return;
					case (int) Eレーン.BsR:
						this.BsR = value;
						return;
					case (int) Eレーン.BsG:
						this.BsG = value;
						return;
					case (int) Eレーン.BsB:
						this.BsB = value;
						return;
					case (int) Eレーン.BsPick:
						this.BsPick = value;
						return;
					case (int) Eレーン.BsW:
						this.BsW = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}
	}


	[Serializable]
	[StructLayout( LayoutKind.Sequential )]
	public struct STAUTOPLAY								// Eレーンとindexを一致させること
	{
		public bool LC;			// 0
		public bool HH;			// 1
		public bool SD;			// 2
		public bool BD;			// 3
		public bool HT;			// 4
		public bool LT;			// 5
		public bool FT;			// 6
		public bool CY;			// 7
        public bool LP;
		public bool RD;			// 8
        public bool LBD;
		public bool Guitar;		// 9	(not used)
		public bool Bass;		// 10	(not used)
		public bool GtR;		// 11
		public bool GtG;		// 12
		public bool GtB;		// 13
		public bool GtPick;		// 14
		public bool GtW;		// 15
		public bool BsR;		// 16
		public bool BsG;		// 17
		public bool BsB;		// 18
		public bool BsPick;		// 19
		public bool BsW;		// 20
		public bool this[ int index ]
		{
			get
			{
				switch ( index )
				{
					case (int) Eレーン.LC:
						return this.LC;
					case (int) Eレーン.HH:
						return this.HH;
					case (int) Eレーン.SD:
						return this.SD;
					case (int) Eレーン.BD:
						return this.BD;
					case (int) Eレーン.HT:
						return this.HT;
					case (int) Eレーン.LT:
						return this.LT;
					case (int) Eレーン.FT:
						return this.FT;
					case (int) Eレーン.CY:
						return this.CY;
                    case (int) Eレーン.LP:
                        return this.LP;
					case (int) Eレーン.RD:
						return this.RD;
                    case (int) Eレーン.LBD:
                        return this.LBD;
					case (int) Eレーン.Guitar:
						if ( !this.GtR ) return false;
						if ( !this.GtG ) return false;
						if ( !this.GtB ) return false;
						if ( !this.GtPick ) return false;
						if ( !this.GtW ) return false;
						return true;
					case (int) Eレーン.Bass:
						if ( !this.BsR ) return false;
						if ( !this.BsG ) return false;
						if ( !this.BsB) return false;
						if ( !this.BsPick ) return false;
						if ( !this.BsW ) return false;
						return true;
					case (int) Eレーン.GtR:
						return this.GtR;
					case (int) Eレーン.GtG:
						return this.GtG;
					case (int) Eレーン.GtB:
						return this.GtB;
					case (int) Eレーン.GtPick:
						return this.GtPick;
					case (int) Eレーン.GtW:
						return this.GtW;
					case (int) Eレーン.BsR:
						return this.BsR;
					case (int) Eレーン.BsG:
						return this.BsG;
					case (int) Eレーン.BsB:
						return this.BsB;
					case (int) Eレーン.BsPick:
						return this.BsPick;
					case (int) Eレーン.BsW:
						return this.BsW;
				}
				throw new IndexOutOfRangeException();
			}
			set
			{
				switch ( index )
				{
					case (int) Eレーン.LC:
						this.LC = value;
						return;
					case (int) Eレーン.HH:
						this.HH = value;
						return;
					case (int) Eレーン.SD:
						this.SD = value;
						return;
					case (int) Eレーン.BD:
						this.BD = value;
						return;
					case (int) Eレーン.HT:
						this.HT = value;
						return;
					case (int) Eレーン.LT:
						this.LT = value;
						return;
					case (int) Eレーン.FT:
						this.FT = value;
						return;
					case (int) Eレーン.CY:
						this.CY = value;
						return;
                    case (int) Eレーン.LP:
                        this.LP = value;
                        return;
					case (int) Eレーン.RD:
						this.RD = value;
						return;
                    case (int) Eレーン.LBD:
                        this.LBD = value;
                        return;
					case (int) Eレーン.Guitar:
						this.GtR = this.GtG = this.GtB = this.GtPick = this.GtW = value;
						return;
					case (int) Eレーン.Bass:
						this.BsR = this.BsG = this.BsB = this.BsPick = this.BsW = value;
						return;
					case (int) Eレーン.GtR:
						this.GtR = value;
						return;
					case (int) Eレーン.GtG:
						this.GtG = value;
						return;
					case (int) Eレーン.GtB:
						this.GtB = value;
						return;
					case (int) Eレーン.GtPick:
						this.GtPick = value;
						return;
					case (int) Eレーン.GtW:
						this.GtW = value;
						return;
					case (int) Eレーン.BsR:
						this.BsR = value;
						return;
					case (int) Eレーン.BsG:
						this.BsG = value;
						return;
					case (int) Eレーン.BsB:
						this.BsB = value;
						return;
					case (int) Eレーン.BsPick:
						this.BsPick = value;
						return;
					case (int) Eレーン.BsW:
						this.BsW = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}
    }

	public enum EReturnValue : int
	{
		Continuation,
		ReturnToTitle,
		SongChoosen
	}

	#region[Ver.K追加]
	public enum Eレーンタイプ
    {
        TypeA,
        TypeB,
        TypeC,
        TypeD
    }
    public enum Eミラー
    {
        TypeA,
        TypeB
    }
    public enum EClipDispType
    {
        背景のみ           = 1,
        ウィンドウのみ     = 2,
        両方               = 3,
        OFF                = 0
    }
    #endregion

    internal class C定数
	{
	}
}
