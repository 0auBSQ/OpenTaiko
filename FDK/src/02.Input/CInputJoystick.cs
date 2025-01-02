using Silk.NET.Input;

namespace FDK;

public class CInputJoystick : CInputButtonsBase, IInputDevice, IDisposable {

	public CInputJoystick(IJoystick joystick) : base(18) {
		this.Device = joystick;
		this.CurrentType = InputDeviceType.Joystick;
		this.GUID = joystick.Index.ToString();
		this.ID = joystick.Index;
		this.Name = joystick.Name;

		joystick.ButtonDown += Joystick_ButtonDown;
		joystick.ButtonUp += Joystick_ButtonUp;
	}

	private void Joystick_ButtonDown(IJoystick joystick, Button button) {
		if (button.Index >= 0 && button.Index < ButtonStates.Length) {
			base.ButtonDown(button.Index);
		}
	}

	private void Joystick_ButtonUp(IJoystick joystick, Button button) {
		if (button.Index >= 0 && button.Index < ButtonStates.Length) {
			base.ButtonUp(button.Index);
		}
	}
}
