using System.Diagnostics;
using System.Numerics;
using Silk.NET.Input;

namespace FDK;

public class CInputMouse : CInputButtonsBase, IInputDevice, IDisposable {
	public const int MouseButtonCount = 8;
	public (int x, int y) Position = (-1, -1);

	// wheel movement since the last TakeScroll (notch units): added on the window thread, taken on the game thread
	private readonly object scrollLock = new();
	private double scrollX, scrollY;

	/// <summary>Wheel movement since the last call (notch units); resets it.</summary>
	public (double x, double y) TakeScroll() {
		lock (this.scrollLock) {
			var moved = (this.scrollX, this.scrollY);
			this.scrollX = this.scrollY = 0;
			return moved;
		}
	}

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
		mouse.Scroll += Mouse_Scroll;
	}

	public (uint isPressed, int state)[] MouseStates => this.ButtonStates;

	/// <summary>Lock + hide the cursor for free-look (Disabled = relative/virtual movement),
	/// or restore the normal cursor.</summary>
	public void SetCursorLocked(bool locked) {
		if (this.Device is IMouse m && m.Cursor != null) {
			Game.RunOnWindowThread(() => {
				try { m.Cursor.CursorMode = locked ? CursorMode.Disabled : CursorMode.Normal; } catch { }
			});
		}
	}

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
		Position = ((int)mouse.Position.X, (int)mouse.Position.Y);
	}

	private void Mouse_Scroll(IMouse mouse, ScrollWheel wheel) {
		lock (this.scrollLock) {
			this.scrollX += wheel.X;
			this.scrollY += wheel.Y;
		}
	}
}
