using Silk.NET.Input;

namespace FDK;

public class CInputKeyboard : CInputButtonsBase, IInputDevice, IDisposable {
	public CInputKeyboard(IReadOnlyList<IKeyboard> keyboards) : base(144) {
		this.CurrentType = InputDeviceType.Keyboard;
		this.GUID = "";
		this.ID = 0;
		this.Name = keyboards.Count > 0 ? keyboards[0].Name : "";

		foreach (var keyboard in keyboards) {
			keyboard.KeyDown += KeyDown;
			keyboard.KeyUp += KeyUp;
			keyboard.KeyChar += KeyChar;
		}
	}

	public (bool isPressed, int state)[] KeyStates => this.ButtonStates;

	private void KeyDown(IKeyboard keyboard, Key key, int keyCode) {
		if ((int)key >= this.ButtonStates.Length) return;
		if (key != Key.Unknown) {
			var keyNum = DeviceConstantConverter.DIKtoKey(key);
			base.ButtonDown((int)keyNum);
		}
	}

	private void KeyUp(IKeyboard keyboard, Key key, int keyCode) {
		if ((int)key >= this.ButtonStates.Length) return;
		if (key != Key.Unknown) {
			var keyNum = DeviceConstantConverter.DIKtoKey(key);
			base.ButtonUp((int)keyNum);
		}
	}

	private void KeyChar(IKeyboard keyboard, char ch) {

	}
}
