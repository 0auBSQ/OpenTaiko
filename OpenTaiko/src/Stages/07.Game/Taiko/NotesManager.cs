using System.Drawing;
using System.Numerics;
using FDK;

namespace OpenTaiko;

// Simple class containing functions to simplify readability of CChip elements
class NotesManager {

	#region [Parsing]

	public enum ENoteType { // same value as channelNo
		Empty = 0x0,
		Don = 0x11, Po = 0x11,
		Ka = 0x12, Pa = 0x12,
		DonBig = 0x13, Double = 0x13,
		KaBig = 0x14, Clap = 0x14,
		Roll = 0x15, RollPo = 0x15,
		RollBig = 0x16, RollDouble = 0x16,
		Balloon = 0x17,
		EndRoll = 0x18,
		BalloonEx = 0x19,
		DonHand = 0x1A, DoubleHand = 0x1A,
		KaHand = 0x1B, ClapHand = 0x1B,
		Bomb = 0x1C,
		BalloonFuze = 0x1D,
		Adlib = 0x1F,
		Kadon = 0x101,
		RollClap = 0x20,
		RollPa = 0x21,
		Unknown = -1,
	}

	public static Dictionary<string, ENoteType> CharToNoteType = new() {
		["0"] = ENoteType.Empty, // Empty
		["1"] = ENoteType.Don, // Small Don (Taiko) | Red (right) hit (Konga)
		["2"] = ENoteType.Ka, // Small Ka (Taiko) | Yellow (left) hit (Konga)
		["3"] = ENoteType.DonBig, // Big Don (Taiko) | Pink note (Konga)
		["4"] = ENoteType.KaBig, // Big Ka (Taiko) | Clap (Konga)
		["5"] = ENoteType.Roll, // Small roll start | Konga red roll
		["6"] = ENoteType.RollBig, // Big roll start | Konga pink roll
		["7"] = ENoteType.Balloon, // Balloon
		["8"] = ENoteType.EndRoll, // Roll/Balloon end
		["9"] = ENoteType.BalloonEx, // Kusudama
		["A"] = ENoteType.DonHand, // Joint Big Don (2P)
		["B"] = ENoteType.KaHand, // Joint Big Ka (2P)
		["C"] = ENoteType.Bomb, // Mine
		["D"] = ENoteType.BalloonFuze, // ProjectOutfox's Fuse roll
		["F"] = ENoteType.Adlib, // ADLib
		["G"] = ENoteType.Kadon, // Green (Purple) double hit note
		["H"] = ENoteType.RollClap, // Konga clap roll | Taiko big roll
		["I"] = ENoteType.RollPa, // Konga yellow roll | Taiko small roll
	};

	public static Dictionary<ENoteType, string> NoteTypeToChar = CharToNoteType.Select(x => (x.Value, x.Key)).ToDictionary();

	public static bool IsLikelyNoteDataLine(string leftTrimmed)
		=> char.IsAsciiDigit(leftTrimmed[0]) || (leftTrimmed[0] != '#' && !leftTrimmed.Contains(':'));

	public static ENoteType GetNoteType(string chr)
		=> CharToNoteType.GetValueOrDefault(chr, ENoteType.Unknown);
	public static ENoteType GetNoteType(int channelNo)
		=> (ENoteType)channelNo;
	public static ENoteType GetNoteType(CChip? chip)
		=> (chip != null) ? (ENoteType)chip.nChannelNo : ENoteType.Unknown;

	public static string? ToNoteChar(ENoteType nt)
		=> NoteTypeToChar.GetValueOrDefault(nt);
	public static int ToChannelNo(ENoteType nt) => (int)nt;

	// HBScroll/BMScroll: the position as the complex product scroll × Δbeat in the screen's frame
	// (a complex #HISPEED gives the beat an imaginary part)
	// NMScroll: time-based (#HISPEED is ignored as in TaikoManyGimmicks)
	public static (double x, double y) GetNoteXY(double msDTime, Complex th16DBeat, double bpm, Complex scroll, EScrollMode eScrollMode) {
		var n4Beats = getN4Beats(msDTime, th16DBeat, bpm, scroll, eScrollMode);
		return (PxFromN4BeatsX(n4Beats.Real), PxFromN4BeatsY(n4Beats.Imaginary));
	}

	public static (double vx, double vy) GetNoteXYPerSec(Complex hispeed, double bpm, Complex scroll, EScrollMode eScrollMode) {
		var th16BeatPerSec = 4 * bpm / 60 * hispeed;
		return GetNoteXY(-1000, -th16BeatPerSec, bpm, scroll, eScrollMode);
	}

	public static double PxFromN4BeatsX(double n4Beats) {
		int pxPer4Beats = OpenTaiko.Skin.Game_Notes_Interval;
		double screenScale = OpenTaiko.Skin.Resolution[0] / 1280.0;
		return n4Beats * pxPer4Beats * screenScale;
	}

	public static double N4BeatsFromPxX(double px) {
		int pxPer4Beats = OpenTaiko.Skin.Game_Notes_Interval;
		double screenScale = OpenTaiko.Skin.Resolution[0] / 1280.0;
		return px / pxPer4Beats / screenScale;
	}

	public static double PxFromN4BeatsY(double n4Beats) {
		int pxPer4Beats = OpenTaiko.Skin.Game_Notes_Interval;
		double screenScale = OpenTaiko.Skin.Resolution[1] / 720.0;
		return n4Beats * pxPer4Beats * screenScale;
	}

	public static double N4BeatsFromPxY(double px) {
		int pxPer4Beats = OpenTaiko.Skin.Game_Notes_Interval;
		double screenScale = OpenTaiko.Skin.Resolution[1] / 720.0;
		return px * pxPer4Beats * screenScale;
	}

	public static Complex getN4Beats(Complex msDTime, Complex th16DBeat, double bpm, Complex scroll, EScrollMode eScrollMode) {
		if (eScrollMode is EScrollMode.BMScroll)
			scroll = 1.0;
		return getN4Beats(scroll * msDTime, scroll * th16DBeat, bpm, eScrollMode);
	}

	public static Complex getN4Beats(Complex msDTime, Complex th16DBeat, double bpm, EScrollMode eScrollMode)
		=> eScrollMode switch {
			EScrollMode.Normal => msDTime * bpm / 240000.0,
			EScrollMode.BMScroll or EScrollMode.HBScroll => th16DBeat / 16.0,
			_ => 0,
		};

	public static CChip GetVelocityRefChip(CChip chip, CTja.ETjaCompat compat)
		=> (IsRollEnd(chip) && compat is CTja.ETjaCompat.TJAP3 or CTja.ETjaCompat.OOS) ? chip.start : chip; // && !StretchRoll

	#endregion

	#region [Gameplay]
	public enum EInputType {
		Red,
		RedBig,
		Blue, Yellow = Blue,
		BlueBig,
		Clap,
		Unknown = -1,
	}

	public static PlayerLane.FlashType InputToLane(EInputType nInput) => nInput switch {
		EInputType.Red or EInputType.RedBig => PlayerLane.FlashType.Red,
		EInputType.Blue or EInputType.BlueBig => PlayerLane.FlashType.Blue,
		EInputType.Clap => PlayerLane.FlashType.Clap,
		_ => PlayerLane.FlashType.Total,
	};

	public static int GetPadPlayer(EPad nPad) => nPad switch {
		EPad.LRed or EPad.RRed or EPad.LBlue or EPad.RBlue or EPad.Clap => 0,
		EPad.LRed2P or EPad.RRed2P or EPad.LBlue2P or EPad.RBlue2P or EPad.Clap2P => 1,
		EPad.LRed3P or EPad.RRed3P or EPad.LBlue3P or EPad.RBlue3P or EPad.Clap3P => 2,
		EPad.LRed4P or EPad.RRed4P or EPad.LBlue4P or EPad.RBlue4P or EPad.Clap4P => 3,
		EPad.LRed5P or EPad.RRed5P or EPad.LBlue5P or EPad.RBlue5P or EPad.Clap5P => 4,
		_ => int.MaxValue, // invalid player
	};

	public static EPad PadTo1P(EPad pad) => pad switch {
		EPad.LRed or EPad.LRed2P or EPad.LRed3P or EPad.LRed4P or EPad.LRed5P => EPad.LRed,
		EPad.RRed or EPad.RRed2P or EPad.RRed3P or EPad.RRed4P or EPad.RRed5P => EPad.RRed,
		EPad.LBlue or EPad.LBlue2P or EPad.LBlue3P or EPad.LBlue4P or EPad.LBlue5P => EPad.LBlue,
		EPad.RBlue or EPad.RBlue2P or EPad.RBlue3P or EPad.RBlue4P or EPad.RBlue5P => EPad.RBlue,
		EPad.Clap or EPad.Clap2P or EPad.Clap3P or EPad.Clap4P or EPad.Clap5P => EPad.Clap,
		_ => pad,
	};

	public static EInputType PadToInputType(EPad pad, bool isBigInput = false) => PadTo1P(pad) switch {
		EPad.LRed or EPad.RRed => isBigInput ? EInputType.RedBig : EInputType.Red,
		EPad.LBlue or EPad.RBlue => isBigInput ? EInputType.BlueBig : EInputType.Blue,
		EPad.Clap => EInputType.Clap,
		_ => EInputType.Unknown,
	};

	public static PlayerLane.FlashType PadToLane(EPad pad, EGameType gameType) => PadTo1P(pad) switch {
		EPad.LRed or EPad.RRed => PlayerLane.FlashType.Red,
		EPad.LBlue or EPad.RBlue => PlayerLane.FlashType.Blue,
		EPad.Clap when gameType is EGameType.Konga => PlayerLane.FlashType.Clap,
		_ => PlayerLane.FlashType.Total,
	};

	public static int PadToHand(EPad pad) => (PadTo1P(pad) is EPad.RRed or EPad.RBlue) ? 1 : 0;

	public static bool IsExpectedPadAnyHit(EPad hit, CChip chip, EGameType gt) => IsAcceptLane(chip, gt, PadToLane(hit, gt));

	public static bool IsExpectedPadMultiHit(EPad stored, EPad hit, CChip chip, EGameType gt) {
		if (chip == null) return false;

		if (IsBigKaTaiko(chip, gt)) {
			return (hit == EPad.LBlue && stored == EPad.RBlue)
				|| (hit == EPad.RBlue && stored == EPad.LBlue);
		}

		if (IsBigDonTaiko(chip, gt)) {
			return (hit == EPad.LRed && stored == EPad.RRed)
				|| (hit == EPad.RRed && stored == EPad.LRed);
		}

		if (IsSwapNote(chip, gt)) {
			bool hitBlue = hit == EPad.LBlue || hit == EPad.RBlue;
			bool hitRed = hit == EPad.LRed || hit == EPad.RRed;
			bool storedBlue = stored == EPad.LBlue || stored == EPad.RBlue;
			bool storedRed = stored == EPad.LRed || stored == EPad.RRed;

			return (storedRed && hitBlue)
				|| (storedBlue && hitRed);
		}

		return false;
	}

	public static EGameType GetChipGameType(CChip pChip, int nPlayer)
		=> pChip.eGameType ?? OpenTaiko.ConfigIni.nGameType[nPlayer];
	#endregion

	#region [General]

	public static bool IsAcceptLane(ENoteType nt, EGameType gt, PlayerLane.FlashType lane = PlayerLane.FlashType.Total) => lane switch {
		PlayerLane.FlashType.Red => IsAcceptRed(nt, gt),
		PlayerLane.FlashType.Blue => IsAcceptBlue(nt, gt),
		PlayerLane.FlashType.Clap => IsAcceptClap(nt, gt),
		PlayerLane.FlashType.Total => IsAcceptRed(nt, gt) || IsAcceptBlue(nt, gt) || IsAcceptClap(nt, gt),
		_ => false,
	};
	public static bool IsAcceptRed(ENoteType nt, EGameType gt)
		=> IsADLIB(nt) || IsMine(nt) || IsSwapNote(nt, gt) || IsRedRollKonga(nt, gt) || IsPinkRollKonga(nt, gt) || IsSmallRollTaiko(nt, gt) || IsBigRollTaiko(nt, gt)
			|| IsGenericBalloon(nt)
			|| (nt, gt) is (ENoteType.Don or ENoteType.DonBig or ENoteType.DonHand, EGameType.Taiko)
				or (ENoteType.Po, EGameType.Konga);
	public static bool IsAcceptBlue(ENoteType nt, EGameType gt)
		=> IsADLIB(nt) || IsMine(nt) || IsSwapNote(nt, gt) || IsYellowRollKonga(nt, gt) || IsPinkRollKonga(nt, gt) || IsSmallRollTaiko(nt, gt) || IsBigRollTaiko(nt, gt)
			|| (IsGenericBalloon(nt) && gt is EGameType.Konga)
			|| (nt, gt) is (ENoteType.Ka or ENoteType.KaBig or ENoteType.KaHand, EGameType.Taiko)
				or (ENoteType.Pa, EGameType.Konga);
	public static bool IsAcceptClap(ENoteType nt, EGameType gt)
		=> IsADLIB(nt) || IsMine(nt) || IsClapRollKonga(nt, gt)
			|| (nt, gt) is (ENoteType.Clap or ENoteType.ClapHand, EGameType.Konga);
	public static bool IsSmallNote(ENoteType nt, EGameType gt, PlayerLane.FlashType lane = PlayerLane.FlashType.Total)
		=> IsSmallNoteTaiko(nt, gt, lane) || IsSmallNoteKonga(nt, gt, lane);
	public static bool IsSmallRed(ENoteType nt, EGameType gt) => IsSmallNote(nt, gt, PlayerLane.FlashType.Red);
	public static bool IsSmallBlue(ENoteType nt, EGameType gt) => IsSmallNote(nt, gt, PlayerLane.FlashType.Blue);
	public static bool IsSmallClap(ENoteType nt, EGameType gt) => IsSmallNote(nt, gt, PlayerLane.FlashType.Clap);
	public static bool IsSmallNoteTaiko(ENoteType nt, EGameType gt, PlayerLane.FlashType lane = PlayerLane.FlashType.Total)
		=> gt is EGameType.Taiko
			&& (nt, lane) is (ENoteType.Don, PlayerLane.FlashType.Red or PlayerLane.FlashType.Total)
				or (ENoteType.Ka, PlayerLane.FlashType.Blue or PlayerLane.FlashType.Total);
	public static bool IsSmallNoteKonga(ENoteType nt, EGameType gt, PlayerLane.FlashType lane = PlayerLane.FlashType.Total)
		=> gt is EGameType.Konga
			&& (nt, lane) is (ENoteType.Po, PlayerLane.FlashType.Red or PlayerLane.FlashType.Total)
				or (ENoteType.Pa, PlayerLane.FlashType.Yellow or PlayerLane.FlashType.Total)
				or (ENoteType.Clap or ENoteType.ClapHand, PlayerLane.FlashType.Clap or PlayerLane.FlashType.Total);
	public static bool IsBigNoteTaiko(ENoteType nt, EGameType gt, PlayerLane.FlashType lane = PlayerLane.FlashType.Total)
		=> gt is EGameType.Taiko
			&& (nt, lane) is (ENoteType.DonBig or ENoteType.DonHand, PlayerLane.FlashType.Red or PlayerLane.FlashType.Total)
				or (ENoteType.KaBig or ENoteType.KaHand, PlayerLane.FlashType.Blue or PlayerLane.FlashType.Total);
	public static bool IsBigKaTaiko(ENoteType nt, EGameType gt) => IsBigNoteTaiko(nt, gt, PlayerLane.FlashType.Blue);
	public static bool IsBigDonTaiko(ENoteType nt, EGameType gt) => IsBigNoteTaiko(nt, gt, PlayerLane.FlashType.Red);
	public static bool IsJointedNote(ENoteType nt) => nt is ENoteType.DonHand or ENoteType.KaHand;
	// the notes drawn at the small size: in Taiko the small Don and Ka, small roll, balloon, fuse roll, bomb and
	// ADLib; every Konga note
	public static bool IsSmallLookingNote(ENoteType nt, EGameType gt)
		=> gt is EGameType.Konga
			|| nt is ENoteType.Don or ENoteType.Ka or ENoteType.Balloon or ENoteType.BalloonFuze or ENoteType.Bomb or ENoteType.Adlib
			|| IsSmallRollTaiko(nt, gt);
	public static bool IsClapKonga(ENoteType nt, EGameType gt)
		=> (nt, gt) is (ENoteType.Clap or ENoteType.ClapHand, EGameType.Konga);
	public static bool IsSwapNote(ENoteType nt, EGameType gt)
		=> IsPinkKonga(nt, gt) || IsPurpleNoteTaiko(nt, gt);
	public static bool IsPinkKonga(ENoteType nt, EGameType gt) // Purple notes are treated as Pink in Konga
		=> (nt, gt) is (ENoteType.Double or ENoteType.DoubleHand or ENoteType.Kadon, EGameType.Konga);
	public static bool IsPurpleNoteTaiko(ENoteType nt, EGameType gt) => (nt, gt) is (ENoteType.Kadon, EGameType.Taiko);
	public static bool IsBigRollTaiko(ENoteType nt, EGameType gt) => (nt, gt) is (ENoteType.RollBig or ENoteType.RollClap, EGameType.Taiko);
	public static bool IsSmallRollTaiko(ENoteType nt, EGameType gt) => (nt, gt) is (ENoteType.Roll or ENoteType.RollPa, EGameType.Taiko);
	public static bool IsRedRollKonga(ENoteType nt, EGameType gt) => (nt, gt) is (ENoteType.RollPo, EGameType.Konga);
	public static bool IsYellowRollKonga(ENoteType nt, EGameType gt) => (nt, gt) is (ENoteType.RollPa, EGameType.Konga);
	public static bool IsPinkRollKonga(ENoteType nt, EGameType gt) => (nt, gt) is (ENoteType.RollDouble, EGameType.Konga);
	public static bool IsClapRollKonga(ENoteType nt, EGameType gt) => (nt, gt) is (ENoteType.RollClap, EGameType.Konga);
	public static bool IsBalloon(ENoteType nt) => nt is ENoteType.Balloon;
	public static bool IsKusudama(ENoteType nt) => nt is ENoteType.BalloonEx;
	public static bool IsFuzeRoll(ENoteType nt) => nt is ENoteType.BalloonFuze;
	public static bool IsRollEnd(ENoteType nt) => nt is ENoteType.EndRoll;
	public static bool IsADLIB(ENoteType nt) => nt is ENoteType.Adlib;
	public static bool IsMine(ENoteType nt) => nt is ENoteType.Bomb;
	public static bool IsRoll(ENoteType nt)
		=> nt is ENoteType.Roll or ENoteType.RollBig or ENoteType.RollPa or ENoteType.RollClap;
	public static bool IsGenericBalloon(ENoteType nt)
		=> IsBalloon(nt) || IsKusudama(nt) || IsFuzeRoll(nt);
	public static bool IsGenericRoll(ENoteType nt)
		=> IsRoll(nt) || IsGenericBalloon(nt) || IsRollEnd(nt);
	public static bool IsMissableNote(ENoteType nt)
		=> nt is ENoteType.Don or ENoteType.Ka or ENoteType.DonBig or ENoteType.KaBig or ENoteType.DonHand or ENoteType.KaHand or ENoteType.Kadon;
	public static bool IsJudgedFromNearest(ENoteType nt)
		=> IsADLIB(nt) || IsMine(nt);
	public static bool IsHittableNote(ENoteType nt)
		=> IsMissableNote(nt) || IsGenericRoll(nt) || IsJudgedFromNearest(nt);

	#endregion

	#region [Displayables]

	public static int PxSplitLaneDistance => OpenTaiko.Skin.Game_Notes_Size[1] / 3;
	public static int NoteTextureColumnFast(ENoteType nt) => (int)nt - 0x10;
	public static int NoteTextureColumn(ENoteType nt, EGameType _gt)
		=> IsSmallBlue(nt, _gt) ? 2
			: (IsBigDonTaiko(nt, _gt) || IsPinkKonga(nt, _gt)) ? 3
			: (IsBigKaTaiko(nt, _gt) || IsClapKonga(nt, _gt)) ? 4
			: IsBalloon(nt) ? 11
			: 1;

	// Flying notes
	public static ENoteType GetFlyNoteType(ENoteType nt, EGameType gt, bool isBigInput = false) => nt switch {
		ENoteType.DonBig or ENoteType.DonHand => (isBigInput || gt == EGameType.Konga) ? ENoteType.DonBig : ENoteType.Don,
		ENoteType.KaBig or ENoteType.KaHand => (isBigInput || gt == EGameType.Konga) ? ENoteType.KaBig : ENoteType.Ka,
		ENoteType.Kadon => ENoteType.Kadon,
		ENoteType.Adlib or ENoteType.Bomb => ENoteType.Empty,
		_ => nt,
	};

	// a flying note (or the note shown where it lands), drawn centred with the pack's flying-notes row
	public static void DisplayNote(int player, int x, int y, ENoteType Lane, EGameType gt) {
		if (Lane is not (ENoteType.Don or ENoteType.Ka or ENoteType.DonBig or ENoteType.KaBig or ENoteType.Kadon)) return;
		if (FlyingCell(Lane, gt) is not Cell cell) return;
		int w = OpenTaiko.Skin.Game_Notes_Size[0], h = OpenTaiko.Skin.Game_Notes_Size[1];
		int row = Math.Clamp(CNotesSkins.Def(gt).FlyingNotesFrame, 0, Math.Max(0, cell.Sheet.Frames - 1));
		cell.Tex.t2DCenterBasedDraw(x, y, new Rectangle(cell.Col * w, row * h, w, h));
	}

	private static Cell? FlyingCell(ENoteType Lane, EGameType gt)
		=> Lane is ENoteType.Kadon ? NoteCell(Lane, EGameType.Taiko) : KindCell(NoteTextureColumnFast(Lane) - 1, gt);

	// The flash over a note that has landed: the pack's ChipEffect.png cell (the same columns as Notes.png), else the
	// note itself drawn additively
	public static void DisplayChipEffect(int x, int y, ENoteType Lane, EGameType gt, Color4 color, int opacity) {
		if (Lane is not (ENoteType.Don or ENoteType.Ka or ENoteType.DonBig or ENoteType.KaBig or ENoteType.Kadon)) return;
		int w = OpenTaiko.Skin.Game_Notes_Size[0], h = OpenTaiko.Skin.Game_Notes_Size[1];
		var def = CNotesSkins.Def(gt);
		CTexture? fx = OpenTaiko.Tx.ChipEffect[(int)gt];
		int col;
		if (def.Legacy) {
			fx = OpenTaiko.Tx.ChipEffect[0];   // the former layout has one chip effect sheet, with the Taiko columns
			col = IsPurpleNoteTaiko(Lane, gt) ? NoteTextureColumnFast(ENoteType.DonBig)
				: (gt is EGameType.Konga || Lane > ENoteType.KaBig) ? NoteTextureColumnFast(ENoteType.Don)
				: NoteTextureColumnFast(Lane);
		} else {
			col = Lane is ENoteType.Kadon ? 4 : NoteTextureColumnFast(Lane) - 1;
		}
		if (fx != null) {
			fx.color4 = color;
			fx.Opacity = opacity;
			fx.t2DCenterBasedDraw(x, y, new Rectangle(col * w, 0, w, h));
			fx.color4 = new Color4(1f, 1f, 1f, 1f);
			fx.Opacity = 255;
			return;
		}
		if (FlyingCell(Lane, gt) is not Cell cell) return;
		var tex = cell.Tex;
		int savedOpacity = tex.Opacity;
		bool savedAdd = tex.bAddBlend;
		var savedColor = tex.color4;
		tex.bAddBlend = true;
		tex.Opacity = opacity;
		tex.color4 = color;
		tex.t2DCenterBasedDraw(x, y, new Rectangle(cell.Col * w, 0, w, h));
		tex.bAddBlend = savedAdd;
		tex.Opacity = savedOpacity;
		tex.color4 = savedColor;
	}

	// Draws with a texture faded to `opacity` (0..1) and restores its opacity afterwards: the Hidden mod's
	// fade-out, applied to every note-related texture without leaving state behind.
	private static void DrawFaded(CTexture tex, float opacity, Action<CTexture> draw) {
		if (tex == null) return;
		if (opacity >= 1f) { draw(tex); return; }
		if (opacity <= 0f) return;
		int saved = tex.Opacity;
		tex.Opacity = (int)(saved * opacity);
		draw(tex);
		tex.Opacity = saved;
	}

	// The cells of the notes packs (CNotesSkins), all Game_Notes_Size big:
	// - Notes.png: Don, Ka, big Don, big Ka, KaDon, ADLib (Bongo: red, yellow, pink, clap, ADLib), one row per frame
	// - Rolls.png: head, body and tail of the small roll, then of the big roll (Bongo: red, pink, yellow, clap rolls)
	// - Balloons.png (the Taiko set's, for every game type): balloon (2 cells), kusudama (2 cells), fuse roll (3 cells)
	// - Notes_Partner.png: the columns of Notes.png, as many rows, note-sized cells
	// - Notes_Giant.png: as many rows, cells whatever the sheet's size gives (see GiantColumns)
	//   Without them a partner or giant note is the note scaled.
	// The former layout (a skin without packs): Notes.png with the hit circle, the 4 notes, the rolls and the balloon,
	// then Swap.png, Adlib.png, Kusu.png and FuseRoll.png.
	// A giant note wobbles slightly; a #NOTEIF note that has just appeared scales in from bigger with a white flash,
	// and a glowing one then pulses.
	private const float GiantWobbleRad = 0.1f;    // about 6 degrees either way
	private const double GiantWobbleMs = 1600;
	private const double AppearMs = 300;
	private const float AppearExtraScale = 0.4f;  // the size it appears with, above its own
	private const float GlowPulse = 0.3f;         // the pulse's brightest flash
	private const double GlowPulseMs = 900;

	// a column (Span cells wide) of a sheet, whose rows follow Sheet's animation for the note Name (see CAnimTier.RowsByNote)
	private readonly record struct Cell(CTexture Tex, int Col, int Span, CSheetDef Sheet, bool MainSheet, string Name);

	private static readonly string[] TaikoNoteNames = { "Don", "Ka", "BigDon", "BigKa", "KaDon", "ADLib" };
	private static readonly string[] KongaNoteNames = { "Red", "Yellow", "Pink", "Clap", "", "ADLib" };

	// a note's cell by kind: 0 Don, 1 Ka, 2 big Don, 3 big Ka, 4 KaDon, 5 ADLib (the Bongo pink and clap are 2 and 3)
	private static Cell? KindCell(int kind, EGameType gt) {
		var tx = OpenTaiko.Tx;
		var def = CNotesSkins.Def(gt);
		var names = gt is EGameType.Konga ? KongaNoteNames : TaikoNoteNames;
		string name = kind >= 0 && kind < names.Length ? names[kind] : "";
		if (def.Legacy) {
			if (kind == 4) return tx.Note_Swap is CTexture swap ? new Cell(swap, 0, 1, def.Notes, false, name) : null;
			if (kind == 5) return tx.Note_Adlib is CTexture adlib ? new Cell(adlib, 0, 1, def.Notes, false, name) : null;
			return tx.Notes[(int)gt] is CTexture old ? new Cell(old, kind + 1, 1, def.Notes, true, name) : null;
		}
		if (gt is EGameType.Konga) {
			if (kind == 4) return null;
			if (kind == 5) kind = 4;
		}
		return tx.Notes[(int)gt] is CTexture notes ? new Cell(notes, kind, 1, def.Notes, true, name) : null;
	}

	private static int NoteKind(ENoteType nt, EGameType gt)
		=> IsPurpleNoteTaiko(nt, gt) ? 4 : IsADLIB(nt) ? 5 : NoteTextureColumn(nt, gt) - 1;

	// the cell of a note drawn by DisplayNote (a hit note, a balloon or a kusudama), or null
	private static Cell? NoteCell(ENoteType nt, EGameType gt) {
		if (IsBalloon(nt) || IsKusudama(nt)) {
			var tx = OpenTaiko.Tx;
			var taiko = CNotesSkins.Def(EGameType.Taiko);
			string name = IsKusudama(nt) ? "Kusudama" : "Balloon";
			if (!taiko.Legacy)
				return tx.Balloons is CTexture b ? new Cell(b, IsKusudama(nt) ? 2 : 0, 2, taiko.Balloons, false, name) : null;
			var def = CNotesSkins.Def(gt);
			if (IsKusudama(nt)) return tx.Note_Kusu is CTexture k ? new Cell(k, 0, 2, def.Notes, false, name) : null;
			return tx.Notes[(int)gt] is CTexture n ? new Cell(n, 11, 2, def.Notes, false, name) : null;
		}
		return KindCell(NoteKind(nt, gt), gt);
	}

	// the column count of a pack's Notes.png
	private static int NotesColumns(EGameType gt) => gt is EGameType.Konga ? 5 : 6;

	// a Taiko pack's Notes_Giant.png has no small Don and Ka (a giant note looks big whatever its size):
	// Don, Ka, KaDon, ADLib; a Bongo one has the columns of its Notes.png
	private static int GiantColumns(EGameType gt) => gt is EGameType.Konga ? 5 : 4;
	private static int GiantColumn(int notesCol, EGameType gt)
		=> gt is EGameType.Konga ? notesCol : notesCol switch { 0 or 2 => 0, 1 or 3 => 1, 4 => 2, _ => 3 };

	private readonly record struct NoteLook(float Scale, float Rotation, float Flash) {
		public bool IsPlain => this.Scale == 1f && this.Rotation == 0f && this.Flash <= 0f;
	}

	private static NoteLook GetNoteLook(CChip chip) {
		float scale = 1f, rot = 0f, flash = 0f;
		if (chip.IsGiantNote)
			rot = GiantWobbleRad * (float)Math.Sin(2 * Math.PI * (Game.TimeMs % GiantWobbleMs) / GiantWobbleMs);
		if (chip.msNoteIfShown >= 0) {
			double since = Game.TimeMs - chip.msNoteIfShown;
			if (since < AppearMs) {
				float left = (float)(1 - since / AppearMs);
				scale *= 1f + AppearExtraScale * left * left;
				flash = left;
			} else if (chip.NoteIfGlow) {
				flash = GlowPulse * (float)(0.5 - 0.5 * Math.Cos(2 * Math.PI * ((since - AppearMs) % GlowPulseMs) / GlowPulseMs));
			}
		}
		return new NoteLook(scale, rot, flash);
	}

	// t2DDraw at (x, y), scaled and turned around the drawn rectangle's centre, then drawn again additively for the flash
	private static void DrawWithLook(CTexture t, float x, float y, RectangleF? rect, NoteLook look) {
		if (look.IsPlain) {
			if (rect is RectangleF r0) t.t2DDraw(x, y, r0); else t.t2DDraw(x, y);
			return;
		}
		float w = rect?.Width ?? t.szTextureSize.Width, h = rect?.Height ?? t.szTextureSize.Height;
		float dx = w * (look.Scale - 1) / 2, dy = h * (look.Scale - 1) / 2;
		var savedScale = t.vcScaleRatio;
		float savedRot = t.fZAxisCenterRotate;
		t.vcScaleRatio = new Silk.NET.Maths.Vector3D<float>(savedScale.X * look.Scale, savedScale.Y * look.Scale, savedScale.Z);
		t.fZAxisCenterRotate = look.Rotation;
		try {
			Draw();
			if (look.Flash > 0f) {
				int savedOpacity = t.Opacity;
				bool savedAdd = t.bAddBlend;
				t.bAddBlend = true;
				t.Opacity = (int)(savedOpacity * Math.Min(1f, look.Flash));
				try { Draw(); } finally { t.bAddBlend = savedAdd; t.Opacity = savedOpacity; }
			}
		} finally {
			t.vcScaleRatio = savedScale;
			t.fZAxisCenterRotate = savedRot;
		}

		void Draw() {
			if (rect is RectangleF r) t.t2DDraw(x - dx, y - dy, r); else t.t2DDraw(x - dx, y - dy);
		}
	}

	// Regular display
	public static void DisplayNote(int player, int x, int y, CChip chip, CNoteAnim anim, EStealthMode hiddenMode = EStealthMode.Off, float opacity = 1f) {
		if (hiddenMode.HidesNotes())
			return;

		ENoteType nt = GetNoteType(chip);
		EGameType _gt = GetChipGameType(chip, player);
		if (IsADLIB(nt)) {
			if (!PuchiChara.tGetEffect(player).ShowAdlib) return;
			opacity *= 50f / 255f;
		}
		var def = CNotesSkins.Def(_gt);
		NoteLook look = GetNoteLook(chip);
		int wNote = OpenTaiko.Skin.Game_Notes_Size[0], hNote = OpenTaiko.Skin.Game_Notes_Size[1];
		bool giant = chip.IsGiantNote;
		bool partner = !giant && chip.IsPartnerNote && IsSmallLookingNote(nt, _gt);

		if (IsMine(nt)) {
			float scale = giant ? def.GiantNotesScale * def.PartnerNotesScale : partner ? def.PartnerNotesScale : 1f;
			DrawFaded(OpenTaiko.Tx.Note_Mine, opacity, t => DrawWithLook(t, x, y, null, look with { Scale = look.Scale * scale }));
			return;
		}

		if (NoteCell(nt, _gt) is not Cell cell) return;
		int row = cell.Sheet.Row(anim, cell.Name);

		// a giant or partner note: its cell of the giant or partner sheet, centred on the note's place
		if ((giant || partner) && cell.MainSheet && !def.Legacy) {
			var sheet = giant ? OpenTaiko.Tx.Notes_Giant[(int)_gt] : OpenTaiko.Tx.Notes_Partner[(int)_gt];
			if (sheet != null) {
				float cw = sheet.szTextureSize.Width / (float)(giant ? GiantColumns(_gt) : NotesColumns(_gt));
				float ch = sheet.szTextureSize.Height / (float)Math.Max(1, def.Notes.Frames);
				int col = giant ? GiantColumn(cell.Col, _gt) : cell.Col;
				DrawFaded(sheet, opacity,
					t => DrawWithLook(t, x + (wNote - cw) / 2, y + (hNote - ch) / 2, new RectangleF(col * cw, row * ch, cw, ch), look));
				return;
			}
		}
		// without one: a partner note takes its big look (a Taiko Don or Ka its big version, any other small-looking note
		// scaled by PartnerNotesScale); a giant note takes that look scaled by GiantNotesScale
		if (giant || partner) {
			float scale = giant ? def.GiantNotesScale : 1f;
			if (IsSmallLookingNote(nt, _gt)) {
				if (_gt is EGameType.Taiko && nt is ENoteType.Don or ENoteType.Ka && KindCell(nt is ENoteType.Don ? 2 : 3, _gt) is Cell big) {
					cell = big;
					row = cell.Sheet.Row(anim, cell.Name);
				} else
					scale *= def.PartnerNotesScale;
			}
			look = look with { Scale = look.Scale * scale };
		}

		var c = cell;
		DrawFaded(c.Tex, opacity,
			t => DrawWithLook(t, x, y, new RectangleF(c.Col * wNote, row * hNote, c.Span * wNote, hNote), look));
	}

	// Roll display
	public static void DisplayRoll(int player, int x, int y, CChip chip, CNoteAnim anim,
		Color4 normalColor, Color4 effectedColor, int xEnd, int yEnd, EStealthMode hiddenMode = EStealthMode.Off, float opacity = 1f
		) {
		if (hiddenMode.HidesNotes() || opacity <= 0f)
			return;

		ENoteType nt = GetNoteType(chip);
		EGameType _gt = GetChipGameType(chip, player);
		var def = CNotesSkins.Def(_gt);

		// the roll's sheet, the column of its head (the body and tail follow) and its name (see CAnimTier.RowsByNote)
		CTexture? _texarr;
		int originCol;
		CSheetDef sheetDef;
		string name;
		if (IsFuzeRoll(nt)) {
			var taiko = CNotesSkins.Def(EGameType.Taiko);
			_texarr = taiko.Legacy ? OpenTaiko.Tx.Note_FuseRoll : OpenTaiko.Tx.Balloons;
			originCol = taiko.Legacy ? 0 : 4;
			sheetDef = taiko.Legacy ? def.Notes : taiko.Balloons;
			name = "FuseRoll";
		} else {
			int part = (IsBigRollTaiko(nt, _gt) || IsPinkRollKonga(nt, _gt)) ? 1
				: IsYellowRollKonga(nt, _gt) ? 2
				: IsClapRollKonga(nt, _gt) ? 3
				: 0;
			name = _gt is EGameType.Konga
				? part switch { 1 => "PinkRoll", 2 => "YellowRoll", 3 => "ClapRoll", _ => "RedRoll" }
				: part == 1 ? "BigRoll" : "Roll";
			if (def.Legacy) {
				_texarr = OpenTaiko.Tx.Notes[(int)_gt];
				originCol = 5 + part switch { 1 => 3, 2 => 8, 3 => 11, _ => 0 };
				sheetDef = def.Notes;
			} else {
				_texarr = OpenTaiko.Tx.Rolls[(int)_gt];
				originCol = part * 3;
				sheetDef = def.Rolls;
			}
		}

		if (_texarr == null) return;
		int rollOrigin = OpenTaiko.Skin.Game_Notes_Size[0] * originCol;
		int frame = sheetDef.Row(anim, name) * OpenTaiko.Skin.Game_Notes_Size[1];
		float wImage = OpenTaiko.Skin.Game_Notes_Size[0];
		float hImage = OpenTaiko.Skin.Game_Notes_Size[1];

		// Hit-type notes are drawn anchoring to the top-left and are off center, but roll-type notes are not
		float xHitNoteOffset = wImage / 2.0f;
		float yHitNoteOffset = hImage / 2.0f;

		// the fade applies to the head, body and tail alike, restored once the roll is drawn
		int savedOpacity = _texarr.Opacity;
		if (opacity < 1f) _texarr.Opacity = (int)(savedOpacity * opacity);

		if (chip.canShowBody) {
			var theta = -Math.Atan2(yEnd - y, xEnd - x);

			var dist = Math.Sqrt(Math.Pow(xEnd - x, 2) + Math.Pow(yEnd - y, 2));
			var div = (dist + 2) / wImage; // + 2 (1 for head, 1 for back) to avoid the gap before tail

			if (OpenTaiko.Skin.Game_RollColorMode != CSkin.RollColorMode.None)
				_texarr.color4 = effectedColor;
			else
				_texarr.color4 = normalColor;

			// Body
			_texarr.vcScaleRatio.X = (float)div;
			_texarr.fZAxisCenterRotate = (float)theta;

			var _center_x = (x + xEnd) / 2 + xHitNoteOffset;
			var _center_y = (y + yEnd) / 2 + yHitNoteOffset;
			_texarr.t2D_DisplayImage_RollNote((int)_center_x, (int)_center_y, new Rectangle(
				rollOrigin + OpenTaiko.Skin.Game_Notes_Size[0],
				frame,
				OpenTaiko.Skin.Game_Notes_Size[0],
				OpenTaiko.Skin.Game_Notes_Size[1]));

			// Tail
			_texarr.vcScaleRatio.X = 1.0f;
			var _xc = xEnd + xHitNoteOffset;
			var _yc = yEnd + yHitNoteOffset;
			// notice that the texture for bar tail is centered at the mid-left of the image rect
			// rotate around image rect center, find bar tail center relative to top-left of image rect
			var xTailOrig = (Math.Cos(theta) * -wImage / 2) + wImage / 2;
			var yTailOrig = (-Math.Sin(theta) * -wImage / 2) + hImage / 2;
			_texarr.t2DDraw((int)(_xc - xTailOrig), (int)(_yc - yTailOrig), 0, new Rectangle(
				rollOrigin + (OpenTaiko.Skin.Game_Notes_Size[0] * 2),
				frame,
				OpenTaiko.Skin.Game_Notes_Size[0],
				OpenTaiko.Skin.Game_Notes_Size[1]));

			_texarr.fZAxisCenterRotate = 0;
		}

		if (OpenTaiko.Skin.Game_RollColorMode == CSkin.RollColorMode.All)
			_texarr.color4 = effectedColor;
		else
			_texarr.color4 = normalColor;

		// Head
		_texarr.t2DDraw(x, y, 0, new Rectangle(rollOrigin, frame, OpenTaiko.Skin.Game_Notes_Size[0], OpenTaiko.Skin.Game_Notes_Size[1]));
		_texarr.color4 = normalColor;
		_texarr.Opacity = savedOpacity;
	}

	// SENotes

	public static (int x, int y) GetSENotesPos(int nPlayer) => OpenTaiko.ConfigIni.nPlayerCount switch {
		<= 2 => (OpenTaiko.Skin.nSENotesX[nPlayer], OpenTaiko.Skin.nSENotesY[nPlayer]),
		3 or 4 => (OpenTaiko.Skin.nSENotes_4P[0], OpenTaiko.Skin.nSENotes_4P[1]),
		>= 5 => (OpenTaiko.Skin.nSENotes_5P[0], OpenTaiko.Skin.nSENotes_5P[1]),
	};

	public static void DisplaySENotes(int player, int x, int y, CChip chip, EStealthMode hiddenMode = EStealthMode.Off, float opacity = 1f) {
		if (hiddenMode.HidesSENotes())
			return;

		ENoteType nt = GetNoteType(chip);
		EGameType _gt = GetChipGameType(chip, player);
		int w = OpenTaiko.Skin.Game_SENote_Size[0], h = OpenTaiko.Skin.Game_SENote_Size[1];

		if (IsMine(nt)) {
			DrawFaded(OpenTaiko.Tx.SENotesExtension, opacity, t => t.t2DDraw(x, y, new Rectangle(0, h, w, h)));
		} else if (IsPurpleNoteTaiko(nt, _gt)) {
			DrawFaded(OpenTaiko.Tx.SENotesExtension, opacity, t => t.t2DDraw(x, y, new Rectangle(0, 0, w, h)));
		} else if (IsFuzeRoll(nt)) {
			DrawFaded(OpenTaiko.Tx.SENotesExtension, opacity, t => t.t2DDraw(x, y, new Rectangle(0, h * 2, w, h)));
		} else if (IsKusudama(nt)) {
			DrawFaded(OpenTaiko.Tx.SENotesExtension, opacity, t => t.t2DDraw(x, y, new Rectangle(0, h * 3, w, h)));
		} else {
			DrawFaded(OpenTaiko.Tx.SENotes[(int)_gt], opacity, t => t.t2DDraw(x, y, new Rectangle(0, h * chip.nSenote, w, h)));
		}
	}

	public static void DisplayNoteArm(int player, int x, int y, CChip chip, float moveCounterValue, EStealthMode hiddenMode = EStealthMode.Off, float opacity = 1f) {
		if (!chip.IsPartnerNote || chip.IsGiantNote || hiddenMode.HidesNotes() || opacity <= 0f)
			return;

		var arm = OpenTaiko.Tx.Notes_Arm;
		int savedArmOpacity = arm?.Opacity ?? 255;
		if (arm != null && opacity < 1f) arm.Opacity = (int)(savedArmOpacity * opacity);
		try {
			DisplayNoteArmInner(player, x, y, chip, moveCounterValue);
		} finally {
			if (arm != null) arm.Opacity = savedArmOpacity;
		}
	}

	private static void DisplayNoteArmInner(int player, int x, int y, CChip chip, float moveCounterValue) {

		float moveAmount = (moveCounterValue < 30 ? moveCounterValue : 60 - moveCounterValue) / 30.0f;
		int moveX = (int)(moveAmount * OpenTaiko.Skin.Game_Notes_Arm_Move[0]);
		int moveY = (int)(moveAmount * OpenTaiko.Skin.Game_Notes_Arm_Move[1]);

		bool isAdlib = IsADLIB(chip);
		if (isAdlib) {
			if (!PuchiChara.tGetEffect(player).ShowAdlib)
				return;
			OpenTaiko.Tx.Notes_Arm?.tUpdateOpacity(50);
		}
		// an ADLib links only to a player whose puchichara shows ADLibs too
		bool Linked(int other) => chip.multiLink?[other, 0] != null && (!isAdlib || PuchiChara.tGetEffect(other).ShowAdlib);

		if (player != OpenTaiko.ConfigIni.nPlayerCount - 1 && Linked(player + 1)) {
			//downward link
			OpenTaiko.Tx.Notes_Arm?.t2DFlipVDraw(
				x + OpenTaiko.Skin.Game_Notes_Arm_Offset_Left_X[0] + moveX,
				y + OpenTaiko.Skin.Game_Notes_Arm_Offset_Left_Y[0] + moveY);
			OpenTaiko.Tx.Notes_Arm?.t2DFlipVDraw(
				x + OpenTaiko.Skin.Game_Notes_Arm_Offset_Right_X[0] - moveX,
				y + OpenTaiko.Skin.Game_Notes_Arm_Offset_Right_Y[0] - moveY);
		}
		if (player != 0 && Linked(player - 1)) {
			//upward link
			OpenTaiko.Tx.Notes_Arm?.t2DDraw(
				x + OpenTaiko.Skin.Game_Notes_Arm_Offset_Left_X[1] + moveX,
				y + OpenTaiko.Skin.Game_Notes_Arm_Offset_Left_Y[1] + moveY);
			OpenTaiko.Tx.Notes_Arm?.t2DDraw(
				x + OpenTaiko.Skin.Game_Notes_Arm_Offset_Right_X[1] - moveX,
				y + OpenTaiko.Skin.Game_Notes_Arm_Offset_Right_Y[1] - moveY);
		}

		OpenTaiko.Tx.Notes_Arm?.tUpdateOpacity(255);
	}

	#endregion

}
