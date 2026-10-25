using Silk.NET.Input;

namespace FDK;

/// <summary>
/// The input context the ImGui controller reads, on the game thread. Its keyboard answers IsKeyPressed from a plain
/// array its key events fill on the window thread: some backends (SDL) answer it by walking a list that their event
/// pump changes, which throws when the two threads meet. Typed characters reach ImGui through Game's own queue, so
/// this keyboard never raises KeyChar. The mice are the window's own.
/// </summary>
internal sealed class ImGuiInputContext : IInputContext {
	private readonly IInputContext _inner;
	private readonly IKeyboard[] _keyboards;

	public ImGuiInputContext(IInputContext inner) {
		_inner = inner;
		_keyboards = inner.Keyboards.Count > 0 ? new IKeyboard[] { new Keyboard(inner.Keyboards[0]) } : Array.Empty<IKeyboard>();
	}

	public nint Handle => _inner.Handle;
	public IReadOnlyList<IGamepad> Gamepads => _inner.Gamepads;
	public IReadOnlyList<IJoystick> Joysticks => _inner.Joysticks;
	public IReadOnlyList<IKeyboard> Keyboards => _keyboards;
	public IReadOnlyList<IMouse> Mice => _inner.Mice;
	public IReadOnlyList<Silk.NET.Input.IInputDevice> OtherDevices => _inner.OtherDevices;

	public event Action<Silk.NET.Input.IInputDevice, bool>? ConnectionChanged {
		add { }
		remove { }
	}

	// the window's context is not ours to dispose
	public void Dispose() {
		foreach (var k in _keyboards) (k as Keyboard)?.Detach();
	}

	private sealed class Keyboard : IKeyboard {
		private readonly IKeyboard _inner;
		private readonly bool[] _down;

		public Keyboard(IKeyboard inner) {
			_inner = inner;
			int max = 0;
			foreach (Key k in Enum.GetValues<Key>()) max = Math.Max(max, (int)k);
			_down = new bool[max + 1];
			_inner.KeyDown += OnKeyDown;
			_inner.KeyUp += OnKeyUp;
		}

		public void Detach() {
			_inner.KeyDown -= OnKeyDown;
			_inner.KeyUp -= OnKeyUp;
		}

		private void OnKeyDown(IKeyboard _, Key key, int scancode) => Set(key, true);
		private void OnKeyUp(IKeyboard _, Key key, int scancode) => Set(key, false);

		private void Set(Key key, bool down) {
			int i = (int)key;
			if (i >= 0 && i < _down.Length) _down[i] = down;
		}

		public bool IsKeyPressed(Key key) {
			int i = (int)key;
			return i >= 0 && i < _down.Length && _down[i];
		}

		public string Name => _inner.Name;
		public int Index => _inner.Index;
		public bool IsConnected => _inner.IsConnected;
		public IReadOnlyList<Key> SupportedKeys => _inner.SupportedKeys;
		public string ClipboardText {
			get => _inner.ClipboardText;
			set => _inner.ClipboardText = value;
		}
		public bool IsScancodePressed(int scancode) => false;

		public event Action<IKeyboard, Key, int>? KeyDown {
			add { }
			remove { }
		}
		public event Action<IKeyboard, Key, int>? KeyUp {
			add { }
			remove { }
		}
		public event Action<IKeyboard, char>? KeyChar {
			add { }
			remove { }
		}

		public void BeginInput() { }
		public void EndInput() { }
	}
}
