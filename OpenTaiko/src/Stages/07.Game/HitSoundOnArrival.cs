using FDK;

namespace OpenTaiko;

// The plain don/ka of a live drum press, played the moment the press arrives on whichever thread delivers it (window,
// MIDI or host thread) instead of when the frame processes the hit. Judgement is unchanged: it still uses the press's
// own timestamp, and the play screen skips the drum sound of presses played here.
internal static class HitSoundOnArrival {
	// held while a hit sound starts, here and on the game thread, and while the play screen changes what is published
	internal static readonly object Gate = new();

	private static Dictionary<(InputDeviceType type, int id, int key), (int player, bool red)[]>? _keys;
	private static CSound?[]? _red, _blue;
	private static readonly bool[] _live = new bool[5];

	// game thread, when the play screen activates: the drum keys of the current bindings and its hit sound slots
	internal static void Publish(CConfigIni config, CSound?[] red, CSound?[] blue) {
		var keys = new Dictionary<(InputDeviceType, int, int), List<(int, bool)>>();
		for (EKeyConfigPart part = 0; part < EKeyConfigPart.System; ++part) {
			for (EPad pad = 0; pad < EPad.Max; ++pad) {
				var input = NotesManager.PadToInputType(pad);
				if (input is not (NotesManager.EInputType.Red or NotesManager.EInputType.Blue))
					continue;
				int player = NotesManager.GetPadPlayer(pad);
				foreach (var k in config.KeyAssign[(int)part][(int)pad]) {
					if (k.InputDevice == InputDeviceType.Unknown)
						continue;
					var key = (k.InputDevice, k.ID, k.Code);
					if (!keys.TryGetValue(key, out var targets))
						keys[key] = targets = new();
					targets.Add((player, input == NotesManager.EInputType.Red));
				}
			}
		}
		lock (Gate) {
			_keys = keys.ToDictionary(e => e.Key, e => e.Value.ToArray());
			_red = red;
			_blue = blue;
			Array.Clear(_live);
		}
	}

	// game thread, each frame: whether a press of this player's drums is processed now
	internal static void SetLive(int player, bool live) => Volatile.Write(ref _live[player], live);

	internal static void Clear() {
		lock (Gate) {
			_keys = null;
			_red = _blue = null;
			Array.Clear(_live);
		}
	}

	// any input thread: true when the press played its drum sound
	internal static bool OnPress(IInputDevice device, int key) {
		if (Volatile.Read(ref _keys) == null)
			return false;
		lock (Gate) {
			if (_keys == null || !_keys.TryGetValue((device.CurrentType, device.ID, key), out var targets))
				return false;
			bool played = false;
			foreach (var (player, red) in targets) {
				if (!Volatile.Read(ref _live[player]))
					continue;
				var sound = (red ? _red : _blue)?[player];
				if (sound == null)
					continue;
				sound.PlayStart();
				played = true;
			}
			return played;
		}
	}
}
