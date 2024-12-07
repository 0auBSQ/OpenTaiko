using Silk.NET.Input;

namespace FDK;

public class CInputJoystick : CInputButtonsBase, IInputDevice, IDisposable {
	public IJoystick Joystick { get; private set; }

	public CInputJoystick(IJoystick joystick) : base() {
		this.Joystick = joystick;
		this.CurrentType = InputDeviceType.Joystick;
		this.GUID = joystick.Index.ToString();
		this.ID = joystick.Index;
		this.Name = joystick.Name;
		this.ButtonStates = new (bool, int)[18];

		joystick.ButtonDown += Joystick_ButtonDown;
		joystick.ButtonUp += Joystick_ButtonUp;
	}

	public override void Polling(bool useBufferInput) {
		// BUG: In Silk.NET, GLFW input does not fire events, so we have to poll
		// 			them instead.
		// 			https://github.com/dotnet/Silk.NET/issues/1889
		foreach (var button in Joystick.Buttons) {
			// also, in GLFW the buttons don't have names, so the indices are the names
			ButtonStates[button.Index].isPressed = button.Pressed;
		}

		base.Polling(useBufferInput);
	}

	private void Joystick_ButtonDown(IJoystick joystick, Button button) {
		if (button.Name != ButtonName.Unknown) {
			ButtonStates[(int)button.Name].isPressed = true;
		}
	}

	private void Joystick_ButtonUp(IJoystick joystick, Button button) {
		if (button.Name != ButtonName.Unknown) {
			ButtonStates[(int)button.Name].isPressed = false;
		}
	}
}
