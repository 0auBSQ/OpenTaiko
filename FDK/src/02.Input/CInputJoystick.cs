using Silk.NET.Input;

namespace FDK;

public class CInputJoystick : CInputButtonsBase, IInputDevice, IDisposable {
	public IJoystick Joystick { get; private set; }

	public CInputJoystick(IJoystick joystick) : base(18) {
		this.Joystick = joystick;
		this.CurrentType = InputDeviceType.Joystick;
		this.GUID = joystick.Index.ToString();
		this.ID = joystick.Index;
		this.Name = joystick.Name;

		joystick.ButtonDown += Joystick_ButtonDown;
		joystick.ButtonUp += Joystick_ButtonUp;
	}

	private void Joystick_ButtonDown(IJoystick joystick, Button button) {
		if (button.Name != ButtonName.Unknown) {
			base.ButtonDown((int)button.Name);
		}
	}

	private void Joystick_ButtonUp(IJoystick joystick, Button button) {
		if (button.Name != ButtonName.Unknown) {
			base.ButtonUp((int)button.Name);
		}
	}
}
