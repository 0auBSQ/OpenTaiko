using Silk.NET.Input;

namespace FDK;

public class CInputGamepad : CInputButtonsBase, IInputDevice, IDisposable {
	private IGamepad Gamepad { get; set; }

	public CInputGamepad(IGamepad gamepad) : base() {
		this.Gamepad = gamepad;
		this.CurrentType = InputDeviceType.Gamepad;
		this.GUID = gamepad.Index.ToString();
		this.ID = gamepad.Index;
		this.Name = gamepad.Name;
		this.ButtonStates = new (bool, int)[15];

		gamepad.ButtonDown += Joystick_ButtonDown;
		gamepad.ButtonUp += Joystick_ButtonUp;
	}

	private void Joystick_ButtonDown(IGamepad joystick, Button button) {
		if (button.Name != ButtonName.Unknown) {
			ButtonStates[(int)button.Name].isPressed = true;
		}
	}

	private void Joystick_ButtonUp(IGamepad joystick, Button button) {
		if (button.Name != ButtonName.Unknown) {
			ButtonStates[(int)button.Name].isPressed = false;
		}
	}
}
