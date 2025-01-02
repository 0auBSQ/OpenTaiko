using Silk.NET.Input;

namespace FDK;

public class CInputGamepad : CInputButtonsBase, IInputDevice, IDisposable {

	public CInputGamepad(IGamepad gamepad) : base(15) {
		this.Device = gamepad;
		this.CurrentType = InputDeviceType.Gamepad;
		this.GUID = gamepad.Index.ToString();
		this.ID = gamepad.Index;
		this.Name = gamepad.Name;

		gamepad.Deadzone = new Deadzone(0.5f, DeadzoneMethod.Traditional);
		gamepad.ButtonDown += Gamepad_ButtonDown;
		gamepad.ButtonUp += Gamepad_ButtonUp;
		gamepad.ThumbstickMoved += Gamepad_ThumbstickMoved;
		gamepad.TriggerMoved += Gamepad_TriggerMoved;
	}

	private void Gamepad_TriggerMoved(IGamepad gamepad, Trigger trigger) {
		if (trigger.Position == 1) {

		}
	}
	private void Gamepad_ThumbstickMoved(IGamepad gamepad, Thumbstick thumbstick) {
		if (gamepad.Deadzone.Apply(thumbstick.Position) > 0) {
			ThumbstickDirection direction = GetDirectionFromThumbstick(thumbstick.Direction);
		}
	}

	private void Gamepad_ButtonDown(IGamepad gamepad, Button button) {
		if (button.Name != ButtonName.Unknown) {
			base.ButtonDown((int)button.Name);
		}
	}

	private void Gamepad_ButtonUp(IGamepad gamepad, Button button) {
		if (button.Name != ButtonName.Unknown) {
			base.ButtonUp((int)button.Name);
		}
	}

	private ThumbstickDirection GetDirectionFromThumbstick(float raw) {
		float value = raw * (180 / MathF.PI);
		if (value >= -90 - 45 / 2f && value <= -90 + 45 / 2f) return ThumbstickDirection.Up;
		if (value >= 0 - 45 / 2f && value <= 0 + 45 / 2f) return ThumbstickDirection.Right;
		if (value >= 90 - 45 / 2f && value <= 90 + 45 / 2f) return ThumbstickDirection.Down;
		if (value >= 180 - 45 / 2f || value <= -180 + 45 / 2f) return ThumbstickDirection.Left;
		return ThumbstickDirection.Unknown;
	}

	private enum ThumbstickDirection {
		Right,
		Down,
		Left,
		Up,
		Unknown,
	}
}
