using Silk.NET.Input;

namespace FDK;

public class CInputGamepad : CInputButtonsBase, IInputDevice, IDisposable {

	public CInputGamepad(IGamepad gamepad, float deadzone = 0.5f) : base(gamepad.Buttons.Count + gamepad.Triggers.Count + (gamepad.Thumbsticks.Count * 4)) {
		this.Device = gamepad;
		this.CurrentType = InputDeviceType.Gamepad;
		this.GUID = gamepad.Index.ToString();
		this.ID = gamepad.Index;
		this.Name = gamepad.Name;

		ButtonCount = gamepad.Buttons.Count;
		TriggerCount = gamepad.Triggers.Count;
		ThumbstickCount = gamepad.Thumbsticks.Count;

		gamepad.Deadzone = new Deadzone(deadzone, DeadzoneMethod.Traditional);
		gamepad.ButtonDown += Gamepad_ButtonDown;
		gamepad.ButtonUp += Gamepad_ButtonUp;
		gamepad.ThumbstickMoved += Gamepad_ThumbstickMoved;
		gamepad.TriggerMoved += Gamepad_TriggerMoved;
	}

	private void Gamepad_TriggerMoved(IGamepad gamepad, Trigger trigger) {
		int trigger_index = ButtonCount + trigger.Index;

		if (trigger.Position == 1) {
			if (!KeyPressing(trigger_index)) { base.ButtonDown(trigger_index); }
		} else {
			if (!KeyReleased(trigger_index)) { base.ButtonUp(trigger_index); }
		}
	}
	private void Gamepad_ThumbstickMoved(IGamepad gamepad, Thumbstick thumbstick) {
		ThumbstickDirection direction = GetDirectionFromThumbstick(thumbstick.Direction);
		if (direction == ThumbstickDirection.Unknown) return;

		int thumbstick_index = ButtonCount + TriggerCount +
			(thumbstick.Index * 4);

		if (gamepad.Deadzone.Apply(thumbstick.Position) > 0) {
			if (!KeyPressing(thumbstick_index)) {
				for (int i = 0; i < 4; i++) {
					if (i != (int)direction)
						base.ButtonUp(thumbstick_index + i);
				}
				base.ButtonDown(thumbstick_index + (int)direction);
			}
		} else {
			for (int i = 0; i < 4; i++) {
				base.ButtonUp(thumbstick_index + i);
			}
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
		Up = 0,
		Right = 1,
		Down = 2,
		Left = 3,
		Unknown = -1,
	}

	private int ButtonCount;
	private int TriggerCount;
	private int ThumbstickCount;

	public string GetButtonName(int index) {
		var gamepad = (IGamepad)Device;
		if (index >= ButtonCount + TriggerCount) {
			int thumbstick_index = index - (ButtonCount + TriggerCount);
			return $"Thumbstick{thumbstick_index / 4} - {(ThumbstickDirection)(thumbstick_index % 4)}";
		}
		if (index >= ButtonCount) {
			int trigger_index = index - ButtonCount;
			return $"Trigger{trigger_index}";
		}
		return gamepad.Buttons[index].Name.ToString();
	}
}
