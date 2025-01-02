using System.Diagnostics;
using System.Numerics;
using Silk.NET.Input;

namespace FDK;

public class CInputMouse : CInputButtonsBase, IInputDevice, IDisposable {
	public const int MouseButtonCount = 8;

	public CInputMouse(IMouse mouse) : base(12) {
		this.Device = mouse;
		this.CurrentType = InputDeviceType.Mouse;
		this.GUID = "";
		this.ID = 0;
		this.Name = mouse.Name;

		mouse.Click += Mouse_Click;
		mouse.DoubleClick += Mouse_DoubleClick;
		mouse.MouseDown += Mouse_MouseDown;
		mouse.MouseUp += Mouse_MouseUp;
		mouse.MouseMove += Mouse_MouseMove;
	}

	public (bool isPressed, int state)[] MouseStates => this.ButtonStates;

	private void Mouse_Click(IMouse mouse, MouseButton mouseButton, Vector2 vector2) {

	}

	private void Mouse_DoubleClick(IMouse mouse, MouseButton mouseButton, Vector2 vector2) {

	}

	private void Mouse_MouseDown(IMouse mouse, MouseButton mouseButton) {
		if (mouseButton != MouseButton.Unknown) {
			base.ButtonDown((int)mouseButton);
		}
	}

	private void Mouse_MouseUp(IMouse mouse, MouseButton mouseButton) {
		if (mouseButton != MouseButton.Unknown) {
			base.ButtonUp((int)mouseButton);
		}
	}

	private void Mouse_MouseMove(IMouse mouse, Vector2 vector2) {

	}
}
