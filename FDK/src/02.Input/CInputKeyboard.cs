using Silk.NET.Input;

namespace FDK;

public class CInputKeyboard : CInputButtonsBase, IInputDevice, IDisposable {
	public CInputKeyboard(IKeyboard keyboard) : base(144) {
		this.Device = keyboard;
		this.CurrentType = InputDeviceType.Keyboard;
		this.GUID = keyboard.Name;
		this.ID = 0;
		this.Name = keyboard.Name;

		keyboard.KeyDown += KeyDown;
		keyboard.KeyUp += KeyUp;
		keyboard.KeyChar += KeyChar;
	}

	private void KeyDown(IKeyboard keyboard, Key key, int keyCode) {
#if DEBUG
		if (IMGUI_WindowIsFocused) return;
#endif

		var keyNum = DeviceConstantConverter.DIKtoKey(key);
		if ((int)keyNum >= this.ButtonStates.Length || keyNum == SlimDXKeys.Key.Unknown) return;

		base.ButtonDown((int)keyNum);
	}

	private void KeyUp(IKeyboard keyboard, Key key, int keyCode) {
#if DEBUG
		if (IMGUI_WindowIsFocused) return;
#endif
		var keyNum = DeviceConstantConverter.DIKtoKey(key);
		if ((int)keyNum >= this.ButtonStates.Length || keyNum == SlimDXKeys.Key.Unknown) return;

		base.ButtonUp((int)keyNum);
	}

	private void KeyChar(IKeyboard keyboard, char ch) {

	}

	public bool IMGUI_WindowIsFocused = false;
}
