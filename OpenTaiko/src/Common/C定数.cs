using System.Runtime.InteropServices;

namespace TJAPlayer3 {

	/// <summary>
	/// 難易度。
	/// </summary>
	public enum Difficulty {
		Easy,
		Normal,
		Hard,
		Oni,
		Edit,
		Tower,
		Dan,
		Total
	}

	public enum EScrollMode {
		Normal,
		BMSCROLL,
		HBSCROLL
	}

	public enum EGame {
		OFF = 0,
		完走叩ききりまショー = 1,
		完走叩ききりまショー激辛 = 2
	}
	public enum E難易度表示タイプ {
		OFF = 0,
		n曲目に表示 = 1,
		mtaikoに画像で表示 = 2,
	}

	public enum EPad            // 演奏用のenum。ここを修正するときは、次に出てくる EKeyConfigPad と EパッドFlag もセットで修正すること。
	{
		HH = 0,
		R = 0,
		SD = 1,
		G = 1,
		BD = 2,
		B = 2,
		HT = 3,
		Pick = 3,
		LT = 4,
		Wail = 4,
		FT = 5,
		Cancel = 5,
		CY = 6,
		Decide = 6,
		HHO = 7,
		RD = 8,
		LC = 9,
		LP = 10,    // #27029 2012.1.4 from
		LBD = 11,

		LRed = 12,
		RRed = 13,
		LBlue = 14,
		RBlue = 15,

		LRed2P = 16,
		RRed2P = 17,
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

		CLAP = 32,
		CLAP2P = 33,
		CLAP3P = 34,
		CLAP4P = 35,
		CLAP5P = 36,
		LeftChange = 37,
		RightChange = 38,

		MAX,            // 門番用として定義
		UNKNOWN = 99
	}
	public enum EKeyConfigPad       // #24609 キーコンフィグで使うenum。capture要素あり。
	{
		HH = EPad.HH,
		R = EPad.R,
		SD = EPad.SD,
		G = EPad.G,
		BD = EPad.BD,
		B = EPad.B,
		HT = EPad.HT,
		Pick = EPad.Pick,
		LT = EPad.LT,
		Wail = EPad.Wail,
		FT = EPad.FT,
		Cancel = EPad.Cancel,
		CY = EPad.CY,
		Decide = EPad.Decide,
		HHO = EPad.HHO,
		RD = EPad.RD,
		LC = EPad.LC,
		LP = EPad.LP,       // #27029 2012.1.4 from
		LBD = EPad.LBD,
		#region [Gameplay Keys]
		LRed = EPad.LRed,
		RRed = EPad.RRed,
		LBlue = EPad.LBlue,
		RBlue = EPad.RBlue,

		LRed2P = EPad.LRed2P,
		RRed2P = EPad.RRed2P,
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

		Clap = EPad.CLAP,
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
		#region [Gameplay/Training only]
		CycleVideoDisplayMode,
		#endregion
		#endregion
		#region [Training Keys]
		TrainingIncreaseScrollSpeed,
		TrainingDecreaseScrollSpeed,
		TrainingIncreaseSongSpeed,
		TrainingDecreaseSongSpeed,
		TrainingToggleAuto,
		TrainingBranchNormal,
		TrainingBranchExpert,
		TrainingBranchMaster,
		TrainingPause,
		TrainingBookmark,
		TrainingMoveForwardMeasure,
		TrainingMoveBackMeasure,
		TrainingSkipForwardMeasure,
		TrainingSkipBackMeasure,
		TrainingJumpToFirstMeasure,
		TrainingJumpToLastMeasure,
		#endregion
		MAX,
		UNKNOWN = EPad.UNKNOWN
	}
	[Flags]
	public enum EPadFlag        // #24063 2011.1.16 yyagi コマンド入力用 パッド入力のフラグ化
	{
		None = 0,
		HH = 1,
		R = 1,
		SD = 2,
		G = 2,
		B = 4,
		BD = 4,
		HT = 8,
		Pick = 8,
		LT = 16,
		Wail = 16,
		FT = 32,
		Cancel = 32,
		CY = 64,
		Decide = 128,
		HHO = 128,
		RD = 256,
		LC = 512,
		LP = 1024,              // #27029
		LBD = 2048,
		LRed = 0,
		RRed = 1,
		LBlue = 2,
		RBlue = 4,
		LRed2P = 8,
		RRed2P = 16,
		LBlue2P = 32,
		RBlue2P = 64,
		UNKNOWN = 4096
	}
	public enum ERandomMode {
		OFF,
		RANDOM,
		MIRROR,
		SUPERRANDOM,
		MIRRORRANDOM
	}

	public enum EFunMods {
		NONE,
		AVALANCHE,
		MINESWEEPER,
		TOTAL,
	}

	public enum EGameType {
		TAIKO = 0,
		KONGA = 1,
	}

	public enum EInstrumentPad      // ここを修正するときは、セットで次の EKeyConfigPart も修正すること。
	{
		DRUMS = 0,
		GUITAR = 1,
		BASS = 2,
		TAIKO = 3,
		UNKNOWN = 99
	}
	public enum EKeyConfigPart  // : E楽器パート
	{
		DRUMS = EInstrumentPad.DRUMS,
		GUITAR = EInstrumentPad.GUITAR,
		BASS = EInstrumentPad.BASS,
		TAIKO = EInstrumentPad.TAIKO,
		SYSTEM,
		UNKNOWN = EInstrumentPad.UNKNOWN
	}

	internal enum EInputDevice {
		Keyboard = 0,
		MIDIInput = 1,
		Joypad = 2,
		Mouse = 3,
		Gamepad = 4,
		Unknown = -1
	}
	public enum ENoteJudge {
		Perfect = 0,
		Great = 1,
		Good = 2,
		Poor = 3,
		Miss = 4,
		Bad = 5,
		Auto = 6,
		ADLIB = 7,
		Mine = 8,
	}
	internal enum E判定文字表示位置 {
		表示OFF,
		レーン上,
		判定ライン上,
		コンボ下
	}

	internal enum EFIFOモード {
		フェードイン,
		フェードアウト
	}

	internal enum E演奏画面の戻り値 {
		継続,
		演奏中断,
		ステージ失敗,
		ステージクリア,
		再読込_再演奏,
		再演奏
	}
	internal enum E曲読込画面の戻り値 {
		継続 = 0,
		読込完了,
		読込中止
	}

	public enum ENoteState {
		none,
		wait,
		perfect,
		grade,
		bad
	}

	public enum E連打State {
		none,
		roll,
		rollB,
		balloon,
		potato
	}

	public enum EStealthMode {
		OFF = 0,
		DORON = 1,
		STEALTH = 2
	}

	/// <summary>
	/// 透明チップの種類
	/// </summary>
	public enum EInvisible {
		OFF,        // チップを透明化しない
		SEMI,       // Poor/Miss時だけ、一時的に透明解除する
		FULL        // チップを常に透明化する
	}

	/// <summary>
	/// Drum/Guitar/Bass の値を扱う汎用の構造体。
	/// </summary>
	/// <typeparam name="T">値の型。</typeparam>
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct STDGBVALUE<T>         // indexはE楽器パートと一致させること
	{
		public T Drums;
		public T Guitar;
		public T Bass;
		public T Taiko;
		public T Unknown;
		public T this[int index] {
			get {
				switch (index) {
					case (int)EInstrumentPad.DRUMS:
						return this.Drums;

					case (int)EInstrumentPad.GUITAR:
						return this.Guitar;

					case (int)EInstrumentPad.BASS:
						return this.Bass;

					case (int)EInstrumentPad.TAIKO:
						return this.Taiko;

					case (int)EInstrumentPad.UNKNOWN:
						return this.Unknown;
				}
				throw new IndexOutOfRangeException();
			}
			set {
				switch (index) {
					case (int)EInstrumentPad.DRUMS:
						this.Drums = value;
						return;

					case (int)EInstrumentPad.GUITAR:
						this.Guitar = value;
						return;

					case (int)EInstrumentPad.BASS:
						this.Bass = value;
						return;

					case (int)EInstrumentPad.TAIKO:
						this.Taiko = value;
						return;

					case (int)EInstrumentPad.UNKNOWN:
						this.Unknown = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}
	}

	public enum EReturnValue : int {
		Continuation,
		ReturnToTitle,
		SongChoosen
	}

	#region[Ver.K追加]
	public enum Eレーンタイプ {
		TypeA,
		TypeB,
		TypeC,
		TypeD
	}
	public enum Eミラー {
		TypeA,
		TypeB
	}
	public enum EClipDispType {
		背景のみ = 1,
		ウィンドウのみ = 2,
		両方 = 3,
		OFF = 0
	}
	#endregion


}
