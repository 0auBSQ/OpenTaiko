using Silk.NET.Input;

namespace FDK;

public class CInputJoystick : CInputButtonsBase, IInputDevice, IDisposable {

	public CInputJoystick(IJoystick joystick) : base(32) {
		// While the Gamepad's button count can be read from the start,
		// the Joystick's button count can only be read after pressing
		// any button. To be safe, we'll just leave some room for a lot
		// of buttons.
		
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

	public string GetButtonName(int index) {
		return $"Button{index}";
	}
}
