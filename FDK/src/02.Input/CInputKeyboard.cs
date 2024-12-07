using Silk.NET.Input;

namespace FDK;

public class CInputKeyboard : CInputButtonsBase, IInputDevice, IDisposable {
	public CInputKeyboard(IReadOnlyList<IKeyboard> keyboards) : base() {
		this.CurrentType = InputDeviceType.Keyboard;
		this.GUID = "";
		this.ID = 0;
		this.Name = keyboards.Count > 0 ? keyboards[0].Name : "";
		this.ButtonStates = new (bool, int)[144];

		foreach (var keyboard in keyboards) {
			keyboard.KeyDown += KeyDown;
			keyboard.KeyUp += KeyUp;
			keyboard.KeyChar += KeyChar;
		}
	}
	public (bool isPressed, int state)[] KeyStates => this.ButtonStates;

	private void KeyDown(IKeyboard keyboard, Key key, int keyCode) {
		if (key != Key.Unknown) {
			var keyNum = DeviceConstantConverter.DIKtoKey(key);
			ButtonStates[(int)keyNum].isPressed = true;
		}
	}

	private void KeyUp(IKeyboard keyboard, Key key, int keyCode) {
		if (key != Key.Unknown) {
			var keyNum = DeviceConstantConverter.DIKtoKey(key);
			ButtonStates[(int)keyNum].isPressed = false;
		}
	}

	private void KeyChar(IKeyboard keyboard, char ch) {

	}
}
