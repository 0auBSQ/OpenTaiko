using System.Drawing;
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

	public static int GetNoteX(double msDTime, double th16DBeat, double bpm, double scroll, EScrollMode eScrollMode) {
		if (eScrollMode is EScrollMode.BMScroll) {
			scroll = 1.0;
		}
		int pxPer4Beats = OpenTaiko.Skin.Game_Notes_Interval;
		double screenScale = OpenTaiko.Skin.Resolution[0] / 1280.0;
		double n4Beats = getN4Beats(msDTime, th16DBeat, bpm, eScrollMode);
		return (int)(n4Beats * pxPer4Beats * scroll * screenScale);
	}

	public static int GetNoteY(double msDTime, double th16DBeat, double bpm, double scroll, EScrollMode eScrollMode) {
		if (scroll == 0.0 || eScrollMode is EScrollMode.BMScroll) {
			return 0;
		}
		int pxPer4Beats = OpenTaiko.Skin.Game_Notes_Interval;
		double screenScale = OpenTaiko.Skin.Resolution[1] / 720.0;
		double n4Beats = getN4Beats(msDTime, th16DBeat, bpm, eScrollMode);
		return (int)(n4Beats * pxPer4Beats * scroll * screenScale);
	}

	public static double getN4Beats(double msDTime, double th16DBeat, double bpm, EScrollMode eScrollMode)
		=> eScrollMode switch {
			EScrollMode.Normal => msDTime * bpm / 240000.0,
			EScrollMode.BMScroll or EScrollMode.HBScroll => th16DBeat / 16.0,
			_ => 0,
		};

	public static CChip GetVelocityRefChip(CChip chip)
		=> (IsRollEnd(chip) && true /* TJAP3/OOS */) ? chip.start : chip; // && !StretchRoll

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
		=> pChip.eGameType ?? OpenTaiko.ConfigIni.nGameType[OpenTaiko.GetActualPlayer(nPlayer)];
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

	public static void DisplayNote(int player, int x, int y, ENoteType Lane, EGameType gt) {
		switch (Lane) {
			case ENoteType.Don:
			case ENoteType.Ka:
			case ENoteType.DonBig:
			case ENoteType.KaBig:
				OpenTaiko.Tx.Notes[(int)gt]?.t2D中心基準描画(x, y, new Rectangle(NoteTextureColumnFast(Lane) * OpenTaiko.Skin.Game_Notes_Size[0], OpenTaiko.Skin.Game_Notes_Size[1] * 3, OpenTaiko.Skin.Game_Notes_Size[0], OpenTaiko.Skin.Game_Notes_Size[1]));
				break;
			case ENoteType.Kadon:
				OpenTaiko.Tx.Note_Swap?.t2D中心基準描画(x, y, new Rectangle(0, OpenTaiko.Skin.Game_Notes_Size[1] * 3, OpenTaiko.Skin.Game_Notes_Size[0], OpenTaiko.Skin.Game_Notes_Size[1]));
				break;
		}
	}

	// Regular display
	public static void DisplayNote(int player, int x, int y, CChip chip, int frame, int length = -1) {
		if (OpenTaiko.ConfigIni.eSTEALTH[OpenTaiko.GetActualPlayer(player)] != EStealthMode.Off || !chip.bShow)
			return;

		if (length == -1) {
			length = OpenTaiko.Skin.Game_Notes_Size[0];
		}

		EGameType _gt = GetChipGameType(chip, player);

		if (IsMine(chip)) {
			OpenTaiko.Tx.Note_Mine?.t2D描画(x, y);
			return;
		} else if (IsPurpleNoteTaiko(chip, _gt)) {
			OpenTaiko.Tx.Note_Swap?.t2D描画(x, y, new Rectangle(0, frame, OpenTaiko.Skin.Game_Notes_Size[0], OpenTaiko.Skin.Game_Notes_Size[1]));
			return;
		} else if (IsKusudama(chip)) {
			OpenTaiko.Tx.Note_Kusu?.t2D描画(x, y, new Rectangle(0, frame, length, OpenTaiko.Skin.Game_Notes_Size[1]));
			return;
		} else if (IsADLIB(chip)) {
			var puchichara = OpenTaiko.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(OpenTaiko.GetActualPlayer(player))];
			if (puchichara.effect.ShowAdlib) {
				OpenTaiko.Tx.Note_Adlib?.tUpdateOpacity(50);
				OpenTaiko.Tx.Note_Adlib?.t2D描画(x, y, new Rectangle(0, frame, length, OpenTaiko.Skin.Game_Notes_Size[1]));
			}
			return;
		}

		int noteType = NoteTextureColumn(chip, _gt);
		OpenTaiko.Tx.Notes[(int)_gt]?.t2D描画(x, y, new Rectangle(noteType * OpenTaiko.Skin.Game_Notes_Size[0], frame, length, OpenTaiko.Skin.Game_Notes_Size[1]));
	}

	// Roll display
	public static void DisplayRoll(int player, int x, int y, CChip chip, int frame,
		Color4 normalColor, Color4 effectedColor, int x末端, int y末端) {
		EGameType _gt = GetChipGameType(chip, player);

		if (OpenTaiko.ConfigIni.eSTEALTH[OpenTaiko.GetActualPlayer(player)] != EStealthMode.Off || !chip.bShow)
			return;

		int _offset = 0;
		var _texarr = OpenTaiko.Tx.Notes[(int)_gt];
		int rollOrigin = (OpenTaiko.Skin.Game_Notes_Size[0] * 5);
		float wImage = OpenTaiko.Skin.Game_Notes_Size[0];
		float hImage = OpenTaiko.Skin.Game_Notes_Size[1];

		// Hit-type notes are drawn anchoring to the top-left and are off center, but roll-type notes are not
		float xHitNoteOffset = wImage / 2.0f;
		float yHitNoteOffset = hImage / 2.0f;

		if (IsSmallRollTaiko(chip, _gt)) {
			_offset = 0;
		} else if (IsBigRollTaiko(chip, _gt) || IsPinkRollKonga(chip, _gt)) {
			_offset = OpenTaiko.Skin.Game_Notes_Size[0] * 3;
		} else if (IsClapRollKonga(chip, _gt)) {
			_offset = OpenTaiko.Skin.Game_Notes_Size[0] * 11;
		} else if (IsYellowRollKonga(chip, _gt)) {
			_offset = OpenTaiko.Skin.Game_Notes_Size[0] * 8;
		} else if (IsFuzeRoll(chip)) {
			_texarr = OpenTaiko.Tx.Note_FuseRoll;
			_offset = -rollOrigin;
		}

		if (_texarr == null) return;

		if (chip.bShowRoll) {
			var theta = -Math.Atan2(y末端 - y, x末端 - x);

			var dist = Math.Sqrt(Math.Pow(x末端 - x, 2) + Math.Pow(y末端 - y, 2));
			var div = (dist + 2) / wImage; // + 2 (1 for head, 1 for back) to avoid the gap before tail

			if (OpenTaiko.Skin.Game_RollColorMode != CSkin.RollColorMode.None)
				_texarr.color4 = effectedColor;
			else
				_texarr.color4 = normalColor;

			// Body
			_texarr.vcScaleRatio.X = (float)div;
			_texarr.fZ軸中心回転 = (float)theta;

			var _center_x = (x + x末端) / 2 + xHitNoteOffset;
			var _center_y = (y + y末端) / 2 + yHitNoteOffset;
			_texarr.t2D_DisplayImage_RollNote((int)_center_x, (int)_center_y, new Rectangle(
				rollOrigin + OpenTaiko.Skin.Game_Notes_Size[0] + _offset,
				0,
				OpenTaiko.Skin.Game_Notes_Size[0],
				OpenTaiko.Skin.Game_Notes_Size[1]));

			// Tail
			_texarr.vcScaleRatio.X = 1.0f;
			var _xc = x末端 + xHitNoteOffset;
			var _yc = y末端 + yHitNoteOffset;
			// notice that the texture for bar tail is centered at the mid-left of the image rect
			// rotate around image rect center, find bar tail center relative to top-left of image rect
			var xTailOrig = (Math.Cos(theta) * -wImage / 2) + wImage / 2;
			var yTailOrig = (-Math.Sin(theta) * -wImage / 2) + hImage / 2;
			_texarr.t2D描画((int)(_xc - xTailOrig), (int)(_yc - yTailOrig), 0, new Rectangle(
				rollOrigin + (OpenTaiko.Skin.Game_Notes_Size[0] * 2) + _offset,
				frame,
				OpenTaiko.Skin.Game_Notes_Size[0],
				OpenTaiko.Skin.Game_Notes_Size[1]));

			_texarr.fZ軸中心回転 = 0;
		}

		if (OpenTaiko.Skin.Game_RollColorMode == CSkin.RollColorMode.All)
			_texarr.color4 = effectedColor;
		else
			_texarr.color4 = normalColor;

		// Head
		_texarr.t2D描画(x, y, 0, new Rectangle(rollOrigin + _offset, frame, OpenTaiko.Skin.Game_Notes_Size[0], OpenTaiko.Skin.Game_Notes_Size[1]));
		_texarr.color4 = normalColor;
	}

	// SENotes
	public static void DisplaySENotes(int player, int x, int y, CChip chip) {
		if (OpenTaiko.ConfigIni.eSTEALTH[OpenTaiko.GetActualPlayer(player)] == EStealthMode.Stealth)
			return;

		EGameType _gt = GetChipGameType(chip, player);

		if (IsMine(chip)) {
			OpenTaiko.Tx.SENotesExtension?.t2D描画(x, y, new Rectangle(0, OpenTaiko.Skin.Game_SENote_Size[1], OpenTaiko.Skin.Game_SENote_Size[0], OpenTaiko.Skin.Game_SENote_Size[1]));
		} else if (IsPurpleNoteTaiko(chip, _gt)) {
			OpenTaiko.Tx.SENotesExtension?.t2D描画(x, y, new Rectangle(0, 0, OpenTaiko.Skin.Game_SENote_Size[0], OpenTaiko.Skin.Game_SENote_Size[1]));
		} else if (IsFuzeRoll(chip)) {
			OpenTaiko.Tx.SENotesExtension?.t2D描画(x, y, new Rectangle(0, OpenTaiko.Skin.Game_SENote_Size[1] * 2, OpenTaiko.Skin.Game_SENote_Size[0], OpenTaiko.Skin.Game_SENote_Size[1]));
		} else if (IsKusudama(chip)) {
			OpenTaiko.Tx.SENotesExtension?.t2D描画(x, y, new Rectangle(0, OpenTaiko.Skin.Game_SENote_Size[1] * 3, OpenTaiko.Skin.Game_SENote_Size[0], OpenTaiko.Skin.Game_SENote_Size[1]));
		} else {
			OpenTaiko.Tx.SENotes[(int)_gt]?.t2D描画(x, y, new Rectangle(0, OpenTaiko.Skin.Game_SENote_Size[1] * chip.nSenote, OpenTaiko.Skin.Game_SENote_Size[0], OpenTaiko.Skin.Game_SENote_Size[1]));
		}
	}


	#endregion

}
