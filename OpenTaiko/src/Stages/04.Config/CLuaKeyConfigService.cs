using System;
using System.Collections.Generic;
using FDK;

namespace OpenTaiko;

/// <summary>One rebindable action row, handed to Lua for the key-config sub-view.</summary>
public sealed class CLuaKeyAction {
	public string Part { get; init; } = "Taiko";   // "Taiko" | "System"
	public int Pad { get; init; }                   // (int)EKeyConfigPad
	public string Name { get; init; } = "";         // localized
	public string Desc { get; init; } = "";         // localized
	public string Group { get; init; } = "";        // "Drums 1P".."5P" | "Menu" | "System" | "Training"
}

/// <summary>
/// Exposes key-binding read/clear + multi-frame capture to the Lua config UI. The Lua INPUT API can't poll raw
/// gamepad/MIDI/all-keyboard input or read/write bindings, so capture is owned by C# here (reusing the
/// <see cref="CActConfigKeyAssign"/> sweep) and DRIVEN by <see cref="CStageConfig"/> calling
/// <see cref="PollCaptureFrame"/> each frame while <see cref="IsCapturing"/>.
/// </summary>
public sealed class CLuaKeyConfigService {
	// ── capture state ──
	public bool IsCapturing { get; private set; }
	private EKeyConfigPart _capPart;
	private int _capPad;
	private int _capSlot;
	private Action<bool>? _onDone;

	private CConfigIni.CKeyAssign Keys => OpenTaiko.ConfigIni.KeyAssign;

	private static EKeyConfigPart ParsePart(string part)
		=> string.Equals(part, "System", StringComparison.OrdinalIgnoreCase) ? EKeyConfigPart.System : EKeyConfigPart.Taiko;

	// ── action lists (mirror CActConfigList.tItemListSettings_KeyAssign*) ──
	public IList<CLuaKeyAction> ListActions(string group) {
		var L = CLangManager.LangInstance;
		var list = new List<CLuaKeyAction>();
		// localized key-binding GROUP headers (skin locale via the config builder's helper; English fallback). The
		// Group string doubles as the Lua section header AND a grouping key, so resolving it once keeps grouping intact.
		string grpSystem = CConfigOptionBuilder.L("SETTINGS_KEYGROUP_SYSTEM", "System");
		string grpMenu = CConfigOptionBuilder.L("SETTINGS_KEYGROUP_MENU", "Menu");
		string grpTraining = CConfigOptionBuilder.L("SETTINGS_KEYGROUP_TRAINING", "Training");
		void Add(string part, EKeyConfigPad pad, string key, string grp)
			=> list.Add(new CLuaKeyAction { Part = part, Pad = (int)pad, Group = grp,
				Name = L.GetString(key), Desc = L.GetString(key + "_DESC") });

		switch ((group ?? "").ToLowerInvariant()) {
			case "system":
				Add("System", EKeyConfigPad.Capture, "SETTINGS_KEYASSIGN_SYSTEM_CAPTURE", grpSystem);
				Add("System", EKeyConfigPad.SongVolumeIncrease, "SETTINGS_KEYASSIGN_SYSTEM_INCREASEVOL", grpSystem);
				Add("System", EKeyConfigPad.SongVolumeDecrease, "SETTINGS_KEYASSIGN_SYSTEM_DECREASEVOL", grpSystem);
				Add("System", EKeyConfigPad.DisplayHits, "SETTINGS_KEYASSIGN_SYSTEM_DISPLAYHITS", grpSystem);
				Add("System", EKeyConfigPad.DisplayDebug, "SETTINGS_KEYASSIGN_SYSTEM_DISPLAYDEBUG", grpSystem);
				Add("System", EKeyConfigPad.QuickConfig, "SETTINGS_KEYASSIGN_SYSTEM_QUICKCONFIG", grpSystem);
				Add("System", EKeyConfigPad.SortSongs, "SETTINGS_KEYASSIGN_SYSTEM_SONGSORT", grpSystem);
				Add("System", EKeyConfigPad.ToggleAutoP1, "SETTINGS_KEYASSIGN_SYSTEM_AUTO1P", grpSystem);
				Add("System", EKeyConfigPad.ToggleAutoP2, "SETTINGS_KEYASSIGN_SYSTEM_AUTO2P", grpSystem);
				Add("System", EKeyConfigPad.ToggleTrainingMode, "SETTINGS_KEYASSIGN_SYSTEM_TRAINING", grpSystem);
				Add("System", EKeyConfigPad.CycleVideoDisplayMode, "SETTINGS_KEYASSIGN_SYSTEM_BGMOVIEDISPLAY", grpSystem);
				break;
			case "drums":
				void Player(int p) {
					string sfx = p == 1 ? "" : p + "P";
					string grp = string.Format(CConfigOptionBuilder.L("SETTINGS_KEYGROUP_DRUMS", "Drums {0}P"), p);
					Add("Taiko", Pad("LRed", sfx), "SETTINGS_KEYASSIGN_GAME_LEFTRED" + sfx, grp);
					Add("Taiko", Pad("RRed", sfx), "SETTINGS_KEYASSIGN_GAME_RIGHTRED" + sfx, grp);
					Add("Taiko", Pad("LBlue", sfx), "SETTINGS_KEYASSIGN_GAME_LEFTBLUE" + sfx, grp);
					Add("Taiko", Pad("RBlue", sfx), "SETTINGS_KEYASSIGN_GAME_RIGHTBLUE" + sfx, grp);
					Add("Taiko", Pad("Clap", sfx), "SETTINGS_KEYASSIGN_GAME_CLAP" + sfx, grp);
				}
				for (int p = 1; p <= 5; p++) Player(p);
				Add("Taiko", EKeyConfigPad.Decide, "SETTINGS_KEYASSIGN_GAME_DECIDE", grpMenu);
				Add("Taiko", EKeyConfigPad.Cancel, "SETTINGS_KEYASSIGN_GAME_CANCEL", grpMenu);
				Add("Taiko", EKeyConfigPad.LeftChange, "SETTINGS_KEYASSIGN_GAME_LEFTCHANGE", grpMenu);
				Add("Taiko", EKeyConfigPad.RightChange, "SETTINGS_KEYASSIGN_GAME_RIGHTCHANGE", grpMenu);
				break;
			case "training":
				Add("Taiko", EKeyConfigPad.TrainingPause, "SETTINGS_KEYASSIGN_TRAINING_PAUSE", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingToggleAuto, "SETTINGS_KEYASSIGN_TRAINING_AUTO", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingBookmark, "SETTINGS_KEYASSIGN_TRAINING_BOOKMARK", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingIncreaseScrollSpeed, "SETTINGS_KEYASSIGN_TRAINING_INCREASESCROLL", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingDecreaseScrollSpeed, "SETTINGS_KEYASSIGN_TRAINING_DECREASESCROLL", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingIncreaseSongSpeed, "SETTINGS_KEYASSIGN_TRAINING_INCREASESPEED", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingDecreaseSongSpeed, "SETTINGS_KEYASSIGN_TRAINING_DECREASESPEED", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingBranchNormal, "SETTINGS_KEYASSIGN_TRAINING_BRANCHNORMAL", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingBranchExpert, "SETTINGS_KEYASSIGN_TRAINING_BRANCHEXPERT", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingBranchMaster, "SETTINGS_KEYASSIGN_TRAINING_BRANCHMASTER", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingMoveForwardMeasure, "SETTINGS_KEYASSIGN_TRAINING_MOVEFORWARD", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingMoveBackMeasure, "SETTINGS_KEYASSIGN_TRAINING_MOVEBACKWARD", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingSkipForwardMeasure, "SETTINGS_KEYASSIGN_TRAINING_SKIPFORWARD", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingSkipBackMeasure, "SETTINGS_KEYASSIGN_TRAINING_SKIPBACKWARD", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingJumpToFirstMeasure, "SETTINGS_KEYASSIGN_TRAINING_JUMPTOFIRST", grpTraining);
				Add("Taiko", EKeyConfigPad.TrainingJumpToLastMeasure, "SETTINGS_KEYASSIGN_TRAINING_JUMPTOLAST", grpTraining);
				break;
		}
		return list;
	}

	private static EKeyConfigPad Pad(string baseName, string sfx)
		=> Enum.Parse<EKeyConfigPad>(baseName + sfx);

	// ── binding display (mirror CActConfigKeyAssign.tAssignCodeDraw_*) ──
	public string[] GetBindingDisplays(CLuaKeyAction a) {
		var arr = Keys[(int)ParsePart(a.Part)][a.Pad];
		var outp = new string[16];
		for (int i = 0; i < 16; i++) outp[i] = DescribeBinding(arr[i]);
		return outp;
	}
	public string GetBindingDisplay(CLuaKeyAction a, int slot) {
		if (slot < 0 || slot > 15) return "";
		return DescribeBinding(Keys[(int)ParsePart(a.Part)][a.Pad][slot]);
	}
	/// <summary>The first non-empty binding (for a compact one-line row), or "(none)".</summary>
	public string GetPrimaryBinding(CLuaKeyAction a) {
		var arr = Keys[(int)ParsePart(a.Part)][a.Pad];
		for (int i = 0; i < 16; i++)
			if (arr[i].InputDevice != InputDeviceType.Unknown) return DescribeBinding(arr[i]);
		return CConfigOptionBuilder.L("SETTINGS_KEYASSIGN_NONE", "(none)");
	}

	/// <summary>All non-empty bindings joined for a one-line multi-bind display, or "(none)".</summary>
	public string GetAllBindings(CLuaKeyAction a) {
		var arr = Keys[(int)ParsePart(a.Part)][a.Pad];
		var parts = new List<string>();
		for (int i = 0; i < 16; i++)
			if (arr[i].InputDevice != InputDeviceType.Unknown) parts.Add(DescribeBinding(arr[i]));
		return parts.Count > 0 ? string.Join(", ", parts) : CConfigOptionBuilder.L("SETTINGS_KEYASSIGN_NONE", "(none)");
	}
	/// <summary>First empty slot (to add a new binding), or -1 if all 16 are full.</summary>
	public int FirstFreeSlot(CLuaKeyAction a) {
		var arr = Keys[(int)ParsePart(a.Part)][a.Pad];
		for (int i = 0; i < 16; i++) if (arr[i].InputDevice == InputDeviceType.Unknown) return i;
		return -1;
	}
	/// <summary>Highest occupied slot (to remove the most-recently-added binding), or -1 if none.</summary>
	public int LastBoundSlot(CLuaKeyAction a) {
		var arr = Keys[(int)ParsePart(a.Part)][a.Pad];
		for (int i = 15; i >= 0; i--) if (arr[i].InputDevice != InputDeviceType.Unknown) return i;
		return -1;
	}

	private static string DescribeBinding(CConfigIni.CKeyAssign.STKEYASSIGN k) {
		switch (k.InputDevice) {
			case InputDeviceType.Keyboard: return "Key " + KeyboardLabel(k.Code);
			case InputDeviceType.MidiIn: return $"MIDI #{k.ID} {CInputMIDI.GetButtonName(k.Code)}";
			case InputDeviceType.Joystick: return $"Joy #{k.ID} {OpenTaiko.InputManager.Joystick(k.ID)?.GetButtonName(k.Code) ?? "?"}";
			case InputDeviceType.Gamepad: return $"Pad #{k.ID} {OpenTaiko.InputManager.Gamepad(k.ID)?.GetButtonName(k.Code) ?? "?"}";
			case InputDeviceType.Mouse: return $"Mouse {k.Code}";
			default: return "";
		}
	}

	private static string KeyboardLabel(int code) {
		foreach (var kl in _keyLabel) if (kl.code == code) return kl.label;
		return $"0x{code:X2}";
	}

	// ── clear ──
	public void ClearBinding(CLuaKeyAction a, int slot) {
		if (slot < 0 || slot > 15) return;
		var part = ParsePart(a.Part);
		Keys[(int)part][a.Pad][slot].InputDevice = InputDeviceType.Unknown;
		Keys[(int)part][a.Pad][slot].ID = 0;
		Keys[(int)part][a.Pad][slot].Code = 0;
		OpenTaiko.Pad.InvalidateInputToPadCache();
		OpenTaiko.Skin.soundCancelSFX.tPlay();
	}

	// ── capture ──
	public void StartCapture(CLuaKeyAction a, int slot, Action<bool> onDone) {
		if (IsCapturing || slot < 0 || slot > 15) { onDone?.Invoke(false); return; }
		_capPart = ParsePart(a.Part); _capPad = a.Pad; _capSlot = slot; _onDone = onDone;
		IsCapturing = true;
		OpenTaiko.InputManager.Polling();   // flush the press that opened capture
	}
	public void CancelCapture() {
		if (!IsCapturing) return;
		IsCapturing = false; var cb = _onDone; _onDone = null;
		OpenTaiko.InputManager.Polling();
		cb?.Invoke(false);
	}

	/// <summary>One capture poll, driven by the stage while IsCapturing. Returns true when it resolved.</summary>
	public bool PollCaptureFrame() {
		if (!IsCapturing) return false;
		if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape)) {
			OpenTaiko.Skin.soundCancelSFX.tPlay();
			Finish(false); return true;
		}
		if (CheckKeyboard() || CheckMidi() || CheckJoystick() || CheckGamepad() || CheckMouse()) {
			OpenTaiko.Pad.InvalidateInputToPadCache();
			Finish(true); return true;
		}
		return false;
	}
	private void Finish(bool ok) {
		IsCapturing = false; var cb = _onDone; _onDone = null;
		OpenTaiko.InputManager.Polling();
		cb?.Invoke(ok);
	}

	private void Bind(InputDeviceType dev, int id, int code) {
		if ((EKeyConfigPad)_capPad < EKeyConfigPad.Capture)
			OpenTaiko.ConfigIni.RemoveDuplicateKeyAssignments(dev, id, code, (EKeyConfigPad)_capPad);
		Keys[(int)_capPart][_capPad][_capSlot].InputDevice = dev;
		Keys[(int)_capPart][_capPad][_capSlot].ID = id;
		Keys[(int)_capPart][_capPad][_capSlot].Code = code;
	}

	private bool CheckKeyboard() {
		for (int i = 0; i < 144; i++) {
			if (i != (int)SlimDXKeys.Key.Escape && i != (int)SlimDXKeys.Key.Return && OpenTaiko.InputManager.Keyboard.KeyPressed(i)) {
				OpenTaiko.Skin.soundDecideSFX.tPlay();
				Bind(InputDeviceType.Keyboard, 0, i);
				return true;
			}
		}
		return false;
	}
	private bool CheckMidi() => CheckDevice(InputDeviceType.MidiIn);
	private bool CheckJoystick() => CheckDevice(InputDeviceType.Joystick);
	private bool CheckGamepad() => CheckDevice(InputDeviceType.Gamepad);
	private bool CheckDevice(InputDeviceType type) {
		foreach (CInputButtonsBase device in OpenTaiko.InputManager.InputDevices) {
			if (device.CurrentType != type) continue;
			for (int i = 0; i < device.ButtonStates.Length; i++) {
				if (device.KeyPressed(i)) {
					OpenTaiko.Skin.soundDecideSFX.tPlay();
					Bind(type, device.ID, i);
					return true;
				}
			}
		}
		return false;
	}
	private bool CheckMouse() {
		for (int i = 0; i < 8; i++) {
			if (OpenTaiko.InputManager.Mouse.KeyPressed(i)) {
				OpenTaiko.Skin.soundDecideSFX.tPlay();
				Bind(InputDeviceType.Mouse, 0, i);
				return true;
			}
		}
		return false;
	}

	// keyboard code → label (copied from CActConfigKeyAssign.KeyLabel)
	private readonly record struct KL(int code, string label);
	private static readonly KL[] _keyLabel = {
		new(0x35,"[ESC]"), new(1,"[ 1 ]"), new(2,"[ 2 ]"), new(3,"[ 3 ]"), new(4,"[ 4 ]"), new(5,"[ 5 ]"), new(6,"[ 6 ]"), new(7,"[ 7 ]"), new(8,"[ 8 ]"), new(9,"[ 9 ]"), new(0,"[ 0 ]"), new(0x53,"[ - ]"), new(0x34,"[ = ]"), new(0x2a,"[BSC]"), new(0x81,"[TAB]"), new(0x1a,"[ Q ]"),
		new(0x20,"[ W ]"), new(14,"[ E ]"), new(0x1b,"[ R ]"), new(0x1d,"[ T ]"), new(0x22,"[ Y ]"), new(30,"[ U ]"), new(0x12,"[ I ]"), new(0x18,"[ O ]"), new(0x19,"[ P ]"), new(0x4a,"[ [ ]"), new(0x73,"[ ] ]"), new(0x75,"[Enter]"), new(0x4b,"[L-Ctrl]"), new(10,"[ A ]"), new(0x1c,"[ S ]"), new(13,"[ D ]"),
		new(15,"[ F ]"), new(0x10,"[ G ]"), new(0x11,"[ H ]"), new(0x13,"[ J ]"), new(20,"[ K ]"), new(0x15,"[ L ]"), new(0x7b,"[ ; ]"), new(0x26,"[ ' ]"), new(0x45,"[ ` ]"), new(0x4e,"[L-Shift]"), new(0x2b,@"[ \]"), new(0x23,"[ Z ]"), new(0x21,"[ X ]"), new(12,"[ C ]"), new(0x1f,"[ V ]"), new(11,"[ B ]"),
		new(0x17,"[ N ]"), new(0x16,"[ M ]"), new(0x2f,"[ , ]"), new(0x6f,"[ . ]"), new(0x7c,"[ / ]"), new(120,"[R-Shift]"), new(0x6a,"[ * ]"), new(0x4d,"[L-Alt]"), new(0x7e,"[Space]"), new(0x2d,"[CAPS]"), new(0x36,"[F1]"), new(0x37,"[F2]"), new(0x38,"[F3]"), new(0x39,"[F4]"), new(0x3a,"[F5]"), new(0x3b,"[F6]"),
		new(60,"[F7]"), new(0x3d,"[F8]"), new(0x3e,"[F9]"), new(0x3f,"[F10]"), new(0x58,"[NumLock]"), new(0x7a,"[Scroll]"), new(0x60,"[NPad7]"), new(0x61,"[NPad8]"), new(0x62,"[NPad9]"), new(0x66,"[NPad-]"), new(0x5d,"[NPad4]"), new(0x5e,"[NPad5]"), new(0x5f,"[NPad6]"), new(0x68,"[NPad+]"), new(90,"[NPad1]"), new(0x5b,"[NPad2]"),
		new(0x5c,"[NPad3]"), new(0x59,"[NPad0]"), new(0x67,"[NPad.]"), new(0x40,"[F11]"), new(0x41,"[F12]"), new(0x42,"[F13]"), new(0x43,"[F14]"), new(0x44,"[F15]"), new(0x48,"[Kana]"), new(0x24,"[ ? ]"), new(0x30,"[Henkan]"), new(0x57,"[MuHenkan]"), new(0x8f,@"[ \ ]"), new(0x25,"[NPad.]"), new(0x65,"[NPad=]"), new(0x72,"[ ^ ]"),
		new(40,"[ @ ]"), new(0x2e,"[ : ]"), new(130,"[ _ ]"), new(0x49,"[Kanji]"), new(0x7f,"[Stop]"), new(0x29,"[AX]"), new(100,"[NPEnter]"), new(0x74,"[R-Ctrl]"), new(0x54,"[Mute]"), new(0x2c,"[Calc]"), new(0x70,"[PlayPause]"), new(0x52,"[MediaStop]"), new(0x85,"[Volume-]"), new(0x86,"[Volume+]"), new(0x8b,"[WebHome]"), new(0x63,"[NPad,]"),
		new(0x69,"[ / ]"), new(0x80,"[PrtScn]"), new(0x77,"[R-Alt]"), new(110,"[Pause]"), new(70,"[Home]"), new(0x84,"[Up]"), new(0x6d,"[PageUp]"), new(0x4c,"[Left]"), new(0x76,"[Right]"), new(0x33,"[End]"), new(50,"[Down]"), new(0x6c,"[PageDown]"), new(0x47,"[Insert]"), new(0x31,"[Delete]"), new(0x4f,"[L-Win]"), new(0x79,"[R-Win]"),
		new(0x27,"[APP]"), new(0x71,"[Power]"), new(0x7d,"[Sleep]"), new(0x87,"[Wake]")
	};
}
