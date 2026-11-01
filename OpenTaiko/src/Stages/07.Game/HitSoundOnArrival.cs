using FDK;

namespace OpenTaiko;

// The don, ka or clap sound of a live press, played the moment the press arrives on whichever thread delivers it (window,
// MIDI or host thread) instead of when the frame processes the hit. Judgement is unchanged: it still uses the press's
// own timestamp, and the play screen skips the hit sound of presses played here.
internal static class HitSoundOnArrival {
	// held while a hit sound starts, here and on the game thread, and while the play screen changes what is published
	internal static readonly object Gate = new();

	private static Dictionary<(InputDeviceType type, int id, int key), (int player, NotesManager.EInputType input)[]>? _keys;
	private static CSound?[]? _red, _blue, _clap;
	private static readonly bool[] _live = new bool[5];

	// game thread, when the play screen activates: the drum keys of the current bindings and its hit sound slots
	internal static void Publish(CConfigIni config, CSound?[] red, CSound?[] blue, CSound?[] clap) {
		var keys = new Dictionary<(InputDeviceType, int, int), List<(int, NotesManager.EInputType)>>();
		for (EKeyConfigPart part = 0; part < EKeyConfigPart.System; ++part) {
			for (EPad pad = 0; pad < EPad.Max; ++pad) {
				var input = NotesManager.PadToInputType(pad);
				if (input is not (NotesManager.EInputType.Red or NotesManager.EInputType.Blue or NotesManager.EInputType.Clap))
					continue;
				int player = NotesManager.GetPadPlayer(pad);
				foreach (var k in config.KeyAssign[(int)part][(int)pad]) {
					if (k.InputDevice == InputDeviceType.Unknown)
						continue;
					var key = (k.InputDevice, k.ID, k.Code);
					if (!keys.TryGetValue(key, out var targets))
						keys[key] = targets = new();
					targets.Add((player, input));
				}
			}
		}
		lock (Gate) {
			_keys = keys.ToDictionary(e => e.Key, e => e.Value.ToArray());
			_red = red;
			_blue = blue;
			_clap = clap;
			Array.Clear(_live);
		}
	}

	// game thread, each frame: whether a press of this player's drums is processed now
	internal static void SetLive(int player, bool live) => Volatile.Write(ref _live[player], live);

	internal static void Clear() {
		lock (Gate) {
			_keys = null;
			_red = _blue = _clap = null;
			Array.Clear(_live);
		}
	}

	// any input thread: true when the press played its hit sound
	internal static bool OnPress(IInputDevice device, int key) {
		if (Volatile.Read(ref _keys) == null)
			return false;
		lock (Gate) {
			if (_keys == null || !_keys.TryGetValue((device.CurrentType, device.ID, key), out var targets))
				return false;
			bool played = false;
			foreach (var (player, input) in targets) {
				if (!Volatile.Read(ref _live[player]))
					continue;
				var sounds = input switch {
					NotesManager.EInputType.Red => _red,
					NotesManager.EInputType.Blue => _blue,
					_ => _clap,
				};
				var sound = sounds?[player];
				if (sound == null)
					continue;
				sound.PlayStart();
				played = true;
			}
			return played;
		}
	}
}
