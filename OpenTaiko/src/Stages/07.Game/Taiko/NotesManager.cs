using System.Drawing;
using FDK;

namespace OpenTaiko;

// Simple class containing functions to simplify readability of CChip elements
class NotesManager {

	#region [Parsing]

	public static Dictionary<string, int> NoteCorrespondanceDictionnary = new Dictionary<string, int>() {
		["0"] = 0, // Empty
		["1"] = 1, // Small Don (Taiko) | Red (right) hit (Konga)
		["2"] = 2, // Small Ka (Taiko) | Yellow (left) hit (Konga)
		["3"] = 3, // Big Don (Taiko) | Pink note (Konga)
		["4"] = 4, // Big Ka (Taiko) | Clap (Konga)
		["5"] = 5, // Small roll start | Konga red roll
		["6"] = 6, // Big roll start | Konga pink roll
		["7"] = 7, // Balloon
		["8"] = 8, // Roll/Balloon end
		["9"] = 9, // Kusudama
		["A"] = 10, // Joint Big Don (2P)
		["B"] = 11, // Joint Big Ka (2P)
		["C"] = 12, // Mine
		["D"] = 13, // ProjectOutfox's Fuse roll
		["F"] = 15, // ADLib
		["G"] = 0xF1, // Green (Purple) double hit note
		["H"] = 16, // Konga clap roll | Taiko big roll
		["I"] = 17, // Konga yellow roll | Taiko small roll
	};

	public static bool IsLikelyNoteDataLine(string leftTrimmed)
		=> char.IsAsciiDigit(leftTrimmed[0]) || (leftTrimmed[0] != '#' && !leftTrimmed.Contains(':'));

	public static int GetNoteValueFromChar(string chr) {
		if (NoteCorrespondanceDictionnary.ContainsKey(chr))
			return NoteCorrespondanceDictionnary[chr];
		return -1;
	}

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

	public static PlayerLane.FlashType PadToLane(EPad pad, EGameType gameType) => PadTo1P(pad) switch {
		EPad.LRed or EPad.RRed => PlayerLane.FlashType.Red,
		EPad.LBlue or EPad.RBlue => PlayerLane.FlashType.Blue,
		EPad.Clap when gameType is EGameType.Konga => PlayerLane.FlashType.Clap,
		_ => PlayerLane.FlashType.Total,
	};

	public static int PadToHand(EPad pad) => (PadTo1P(pad) is EPad.RRed or EPad.RBlue) ? 1 : 0;

	public static bool IsExpectedPadMissable(EPad hit, CChip chip, EGameType gt) {
		bool acceptRed = IsSmallNote(chip, false) || IsBigDonTaiko(chip, gt) || IsSwapNote(chip, gt);
		bool acceptBlue = IsSmallNote(chip, true) || IsBigKaTaiko(chip, gt) || IsSwapNote(chip, gt);
		bool acceptClap = IsClapKonga(chip, gt);

		return (acceptRed && hit is EPad.LRed or EPad.RRed)
			|| (acceptBlue && hit is EPad.LBlue or EPad.RBlue)
			|| (acceptClap && hit is EPad.Clap);
	}

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

	public static bool IsExpectedPadRoll(EPad hit, CChip chip, EGameType gt) {
		bool isBalloonType = IsGenericBalloon(chip);

		bool acceptKongaRed = (IsSmallRoll(chip) || IsBigRoll(chip));
		bool acceptKongaYellow = (IsYellowRoll(chip) || IsBigRoll(chip));

		bool acceptRed = isBalloonType || (gt is not EGameType.Konga || acceptKongaRed);
		bool acceptBlue = !isBalloonType && (gt is not EGameType.Konga || acceptKongaYellow);
		bool acceptClap = IsClapRoll(chip) && gt == EGameType.Konga;

		return (acceptRed && hit is EPad.LRed or EPad.RRed)
			|| (acceptBlue && hit is EPad.LBlue or EPad.RBlue)
			|| (acceptClap && hit is EPad.Clap);
	}

	#endregion

	#region [General]

	public static bool IsCommonNote(CChip chip) {
		if (chip == null) return false;
		return chip.nChannelNo >= 0x11 && chip.nChannelNo < 0x18;
	}
	public static bool IsMine(CChip chip) {
		if (chip == null) return false;
		return chip.nChannelNo == 0x1C;
	}

	public static bool IsDonNote(CChip chip) {
		if (chip == null) return false;
		return chip.nChannelNo == 0x11 || chip.nChannelNo == 0x13 || chip.nChannelNo == 0x1A;
	}

	public static bool IsKaNote(CChip chip) {
		if (chip == null) return false;
		return chip.nChannelNo == 0x12 || chip.nChannelNo == 0x14 || chip.nChannelNo == 0x1B;
	}

	public static bool IsSmallNote(CChip chip, bool blue) {
		if (chip == null) return false;
		return blue ? chip.nChannelNo == 0x12 : chip.nChannelNo == 0x11;
	}

	public static bool IsSmallNote(CChip chip) {
		if (chip == null) return false;
		return chip.nChannelNo == 0x12 || chip.nChannelNo == 0x11;
	}

	public static bool IsBigNote(CChip chip) {
		if (chip == null) return false;
		return (chip.nChannelNo == 0x13 || chip.nChannelNo == 0x14 || chip.nChannelNo == 0x1A || chip.nChannelNo == 0x1B);
	}

	public static bool IsBigKaTaiko(CChip chip, EGameType gt) {
		if (chip == null) return false;
		return (chip.nChannelNo == 0x14 || chip.nChannelNo == 0x1B) && gt == EGameType.Taiko;
	}

	public static bool IsBigDonTaiko(CChip chip, EGameType gt) {
		if (chip == null) return false;
		return (chip.nChannelNo == 0x13 || chip.nChannelNo == 0x1A) && gt == EGameType.Taiko;
	}

	public static bool IsClapKonga(CChip chip, EGameType gt) {
		if (chip == null) return false;
		return (chip.nChannelNo == 0x14 || chip.nChannelNo == 0x1B) && gt == EGameType.Konga;
	}

	public static bool IsSwapNote(CChip chip, EGameType gt) {
		if (chip == null) return false;
		return (
			IsKongaPink(chip, gt)                           // Konga Pink note
			|| IsPurpleNote(chip)                       // Purple (Green) note
		);
	}

	public static bool IsKongaPink(CChip chip, EGameType gt) {
		if (chip == null) return false;
		// Purple notes are treated as Pink in Konga
		return (chip.nChannelNo == 0x13 || chip.nChannelNo == 0x1A || IsPurpleNote(chip)) && gt == EGameType.Konga;
	}
	public static bool IsPurpleNote(CChip chip) {
		if (chip == null) return false;
		return (chip.nChannelNo == 0x101);
	}

	public static bool IsYellowRoll(CChip chip) {
		if (chip == null) return false;
		return chip.nChannelNo == 0x21;
	}

	public static bool IsClapRoll(CChip chip) {
		if (chip == null) return false;
		return chip.nChannelNo == 0x20;
	}

	public static bool IsKusudama(CChip chip) {
		if (chip == null) return false;
		return chip.nChannelNo == 0x19;
	}

	public static bool IsFuzeRoll(CChip chip) {
		if (chip == null) return false;
		return chip.nChannelNo == 0x1D;
	}

	public static bool IsRollEnd(CChip chip) {
		if (chip == null) return false;
		return chip.nChannelNo == 0x18;
	}

	public static bool IsBalloon(CChip chip) {
		if (chip == null) return false;
		return chip.nChannelNo == 0x17;
	}

	public static bool IsBigRoll(CChip chip) {
		if (chip == null) return false;
		return chip.nChannelNo == 0x16;
	}

	public static bool IsSmallRoll(CChip chip) {
		if (chip == null) return false;
		return chip.nChannelNo == 0x15;
	}

	public static bool IsADLIB(CChip chip) {
		if (chip == null) return false;
		return chip.nChannelNo == 0x1F;
	}

	public static bool IsRoll(CChip chip) {
		if (chip == null) return false;
		return IsBigRoll(chip) || IsSmallRoll(chip) || IsClapRoll(chip) || IsYellowRoll(chip);
	}

	public static bool IsGenericBalloon(CChip chip) {
		if (chip == null) return false;
		return IsBalloon(chip) || IsKusudama(chip) || IsFuzeRoll(chip);
	}

	public static bool IsGenericRoll(CChip chip) {
		if (chip == null) return false;
		return (0x15 <= chip.nChannelNo && chip.nChannelNo <= 0x19) ||
			   (chip.nChannelNo == 0x20 || chip.nChannelNo == 0x21)
			   || chip.nChannelNo == 0x1D;
	}

	public static bool IsMissableNote(CChip chip) {
		if (chip == null) return false;
		return (0x11 <= chip.nChannelNo && chip.nChannelNo <= 0x14)
			   || chip.nChannelNo == 0x1A
			   || chip.nChannelNo == 0x1B
			   || chip.nChannelNo == 0x101;
	}

	public static bool IsHittableNote(CChip chip) {
		if (chip == null) return false;
		return IsMissableNote(chip)
			   || IsGenericRoll(chip)
			   || IsADLIB(chip)
			   || IsMine(chip);
	}

	#endregion

	#region [Displayables]

	// Flying notes
	public static void DisplayNote(int player, int x, int y, int Lane) {
		EGameType _gt = OpenTaiko.ConfigIni.nGameType[OpenTaiko.GetActualPlayer(player)];

		switch (Lane) {
			case 1:
			case 2:
			case 3:
			case 4:
				OpenTaiko.Tx.Notes[(int)_gt]?.t2D中心基準描画(x, y, new Rectangle(Lane * OpenTaiko.Skin.Game_Notes_Size[0], OpenTaiko.Skin.Game_Notes_Size[1] * 3, OpenTaiko.Skin.Game_Notes_Size[0], OpenTaiko.Skin.Game_Notes_Size[1]));
				break;
			case 5:
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

		EGameType _gt = OpenTaiko.ConfigIni.nGameType[OpenTaiko.GetActualPlayer(player)];

		int noteType = 1;
		if (IsSmallNote(chip, true)) noteType = 2;
		else if (IsBigDonTaiko(chip, _gt) || IsKongaPink(chip, _gt)) noteType = 3;
		else if (IsBigKaTaiko(chip, _gt) || IsClapKonga(chip, _gt)) noteType = 4;
		else if (IsBalloon(chip)) noteType = 11;

		else if (IsMine(chip)) {
			OpenTaiko.Tx.Note_Mine?.t2D描画(x, y);
			return;
		} else if (IsPurpleNote(chip)) {
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

		OpenTaiko.Tx.Notes[(int)_gt]?.t2D描画(x, y, new Rectangle(noteType * OpenTaiko.Skin.Game_Notes_Size[0], frame, length, OpenTaiko.Skin.Game_Notes_Size[1]));
	}

	// Roll display
	public static void DisplayRoll(int player, int x, int y, CChip chip, int frame,
		Color4 normalColor, Color4 effectedColor, int x末端, int y末端) {
		EGameType _gt = OpenTaiko.ConfigIni.nGameType[OpenTaiko.GetActualPlayer(player)];

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

		if (IsSmallRoll(chip) || (_gt == EGameType.Taiko && IsYellowRoll(chip))) {
			_offset = 0;
		}
		if (IsBigRoll(chip) || (_gt == EGameType.Taiko && IsClapRoll(chip))) {
			_offset = OpenTaiko.Skin.Game_Notes_Size[0] * 3;
		} else if (IsClapRoll(chip) && _gt == EGameType.Konga) {
			_offset = OpenTaiko.Skin.Game_Notes_Size[0] * 11;
		} else if (IsYellowRoll(chip) && _gt == EGameType.Konga) {
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

		EGameType _gt = OpenTaiko.ConfigIni.nGameType[OpenTaiko.GetActualPlayer(player)];

		if (IsMine(chip)) {
			OpenTaiko.Tx.SENotesExtension?.t2D描画(x, y, new Rectangle(0, OpenTaiko.Skin.Game_SENote_Size[1], OpenTaiko.Skin.Game_SENote_Size[0], OpenTaiko.Skin.Game_SENote_Size[1]));
		} else if (IsPurpleNote(chip) && _gt != EGameType.Konga) {
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
