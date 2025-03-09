using System.Runtime.InteropServices;

namespace OpenTaiko;

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
	BMScroll,
	HBScroll
}

public enum EGame {
	Off = 0,
	Survival = 1,
	SurvivalHard = 2
}

public enum EDifficultyDisplayType {
	Off = 0,
	TextOnNthSong = 1,
	ImageOnMTaiko = 2,
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

	Clap = 32,
	Clap2P = 33,
	Clap3P = 34,
	Clap4P = 35,
	Clap5P = 36,
	LeftChange = 37,
	RightChange = 38,

	Max,            // 門番用として定義
	Unknown = 99
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

	Clap = EPad.Clap,
	Clap2P = EPad.Clap2P,
	Clap3P = EPad.Clap3P,
	Clap4P = EPad.Clap4P,
	Clap5P = EPad.Clap5P,
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
	Max,
	Unknown = EPad.Unknown
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
	Unknown = 4096
}
public enum ERandomMode {
	Off,
	Random,
	Mirror,
	SuperRandom,
	MirrorRandom
}

public enum EFunMods {
	None,
	Avalanche,
	Minesweeper,
	Total,
}

public enum EGameType {
	Taiko = 0,
	Konga = 1,
}

public enum EInstrumentPad      // ここを修正するときは、セットで次の EKeyConfigPart も修正すること。
{
	Drums = 0,
	Guitar = 1,
	Bass = 2,
	Taiko = 3,
	Unknown = 99
}
public enum EKeyConfigPart  // : E楽器パート
{
	Drums = EInstrumentPad.Drums,
	Guitar = EInstrumentPad.Guitar,
	Bass = EInstrumentPad.Bass,
	Taiko = EInstrumentPad.Taiko,
	System,
	Unknown = EInstrumentPad.Unknown
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
internal enum EJudgeTextDisplayPosition {
	OFF,
	AboveLane,
	OnJudgeLine,
	BelowCombo
}

internal enum EFIFOMode {
	FadeIn,
	FadeOut
}

internal enum EGameplayScreenReturnValue {
	Continue,
	PerformanceInterrupted,
	StageFailed,
	StageCleared,
	ReloadAndReplay,
	Replay
}
internal enum ESongLoadingScreenReturnValue {
	Continue = 0,
	LoadComplete,
	LoadCanceled
}

public enum ENoteState {
	None,
	Wait,
	Perfect,
	Grade,
	Bad
}

public enum ERollState {
	None,
	Roll,
	RollB,
	Balloon,
	Potato
}

public enum EStealthMode {
	Off = 0,
	Doron = 1,
	Stealth = 2
}

/// <summary>
/// 透明チップの種類
/// </summary>
public enum EInvisible {
	Off,        // チップを透明化しない
	Semi,       // Poor/Miss時だけ、一時的に透明解除する
	Full        // チップを常に透明化する
}

/// <summary>
/// Drum/Guitar/Bass の値を扱う汎用の構造体。
/// </summary>
/// <typeparam name="T">値の型。</typeparam>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct STDGBVALUE<T>         // indexはE楽器パートと一致させること
{
	public T Drums { get => Taiko; set => Taiko = value; }
	public T Taiko;
	public T this[int index] {
		get {
			return index switch {
				(int)EInstrumentPad.Drums or (int)EInstrumentPad.Taiko => this.Taiko,
				(int)EInstrumentPad.Guitar or (int)EInstrumentPad.Bass or (int)EInstrumentPad.Unknown => default,
				_ => throw new IndexOutOfRangeException()
			};
		}
		set {
			switch (index) {
				case (int)EInstrumentPad.Drums or (int)EInstrumentPad.Taiko:
					this.Taiko = value;
					return;

				case (int)EInstrumentPad.Guitar or (int)EInstrumentPad.Bass or (int)EInstrumentPad.Unknown:
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
public enum ELaneType {
	TypeA,
	TypeB,
	TypeC,
	TypeD
}
public enum EMirror {
	TypeA,
	TypeB
}
public enum EClipDispType {
	BackgroundOnly = 1,
	WindowOnly = 2,
	Both = 3,
	Off = 0
}
#endregion
