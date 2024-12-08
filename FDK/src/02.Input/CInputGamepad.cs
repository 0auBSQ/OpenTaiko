using Silk.NET.Input;

namespace FDK;

public class CInputGamepad : CInputButtonsBase, IInputDevice, IDisposable {
	private IGamepad Gamepad { get; set; }

	public CInputGamepad(IGamepad gamepad) : base(15) {
		this.Gamepad = gamepad;
		this.CurrentType = InputDeviceType.Gamepad;
		this.GUID = gamepad.Index.ToString();
		this.ID = gamepad.Index;
		this.Name = gamepad.Name;

		gamepad.ButtonDown += Joystick_ButtonDown;
		gamepad.ButtonUp += Joystick_ButtonUp;
	}

	private void Joystick_ButtonDown(IGamepad joystick, Button button) {
		if (button.Name != ButtonName.Unknown) {
			base.ButtonDown((int)button.Name);
		}
	}

	private void Joystick_ButtonUp(IGamepad joystick, Button button) {
		if (button.Name != ButtonName.Unknown) {
			base.ButtonUp((int)button.Name);
		}
	}
}
