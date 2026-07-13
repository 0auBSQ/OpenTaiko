using System.Diagnostics;
using FDK;
using Foundation;
using GameController;
using Silk.NET.Input;

namespace OpenTaiko.iOS;

/// <summary>
/// Game controller input for iOS via GCController. Supports MFi, Xbox, PlayStation controllers.
/// A fixed set of slot devices is registered at startup. A controller claims a free slot when it
/// connects and frees it on disconnect. Button codes follow CInputGamepad.
/// </summary>
// IInputDevice is re-listed so GetButtonName here remaps the interface default
// (a derived-class method alone does not take over a default interface member).
internal sealed class CInputGamepad_iOS : CInputButtonsBase, FDK.IInputDevice {
	// Silk.NET ButtonName indices first (A..DPadLeft), then 2 triggers, then 2 thumbsticks.
	private const int ButtonCount = (int)ButtonName.DPadLeft + 1;
	private const int TriggerBase = ButtonCount;
	private const int ThumbstickBase = TriggerBase + 2;

	private GCController? _controller;
	private readonly List<GCControllerButtonInput> _hooked = new();

	private CInputGamepad_iOS(int slot) : base(ThumbstickBase + 8) {
		this.CurrentType = InputDeviceType.Gamepad;
		this.GUID = $"ios-gamepad-{slot}";
		this.ID = slot;
		this.Name = $"Game Controller {slot}";
	}

	public new Silk.NET.Input.IInputDevice? Device => null;

	public string GetButtonName(int index) {
		if (index >= ThumbstickBase)
			return $"Thumbstick{(index - ThumbstickBase) / 4} - {(ThumbstickDirection)((index - ThumbstickBase) % 4)}";
		if (index >= TriggerBase)
			return $"Trigger{index - TriggerBase}";
		return ((ButtonName)index).ToString();
	}

	// Matches the direction order desktop CInputGamepad encodes thumbsticks with.
	private enum ThumbstickDirection {
		Up = 0,
		Right = 1,
		Down = 2,
		Left = 3,
	}

	private void Attach(GCController controller) {
		GCExtendedGamepad pad = controller.ExtendedGamepad!;
		_controller = controller;
		Hook(pad.ButtonA, (int)ButtonName.A);
		Hook(pad.ButtonB, (int)ButtonName.B);
		Hook(pad.ButtonX, (int)ButtonName.X);
		Hook(pad.ButtonY, (int)ButtonName.Y);
		Hook(pad.LeftShoulder, (int)ButtonName.LeftBumper);
		Hook(pad.RightShoulder, (int)ButtonName.RightBumper);
		Hook(pad.ButtonOptions, (int)ButtonName.Back);
		Hook(pad.ButtonMenu, (int)ButtonName.Start);
		Hook(pad.ButtonHome, (int)ButtonName.Home);
		Hook(pad.LeftThumbstickButton, (int)ButtonName.LeftStick);
		Hook(pad.RightThumbstickButton, (int)ButtonName.RightStick);
		Hook(pad.DPad.Up, (int)ButtonName.DPadUp);
		Hook(pad.DPad.Right, (int)ButtonName.DPadRight);
		Hook(pad.DPad.Down, (int)ButtonName.DPadDown);
		Hook(pad.DPad.Left, (int)ButtonName.DPadLeft);
		Hook(pad.LeftTrigger, TriggerBase);
		Hook(pad.RightTrigger, TriggerBase + 1);
		Hook(pad.LeftThumbstick.Up, ThumbstickBase + (int)ThumbstickDirection.Up);
		Hook(pad.LeftThumbstick.Right, ThumbstickBase + (int)ThumbstickDirection.Right);
		Hook(pad.LeftThumbstick.Down, ThumbstickBase + (int)ThumbstickDirection.Down);
		Hook(pad.LeftThumbstick.Left, ThumbstickBase + (int)ThumbstickDirection.Left);
		Hook(pad.RightThumbstick.Up, ThumbstickBase + 4 + (int)ThumbstickDirection.Up);
		Hook(pad.RightThumbstick.Right, ThumbstickBase + 4 + (int)ThumbstickDirection.Right);
		Hook(pad.RightThumbstick.Down, ThumbstickBase + 4 + (int)ThumbstickDirection.Down);
		Hook(pad.RightThumbstick.Left, ThumbstickBase + 4 + (int)ThumbstickDirection.Left);
		Trace.TraceInformation($"Game controller connected to slot {ID}: {controller.VendorName ?? "(unknown)"}");
	}

	private void Hook(GCControllerButtonInput? button, int index) {
		if (button == null) return;
		button.PressedChangedHandler = (_, _, pressed) => {
			if (pressed) ButtonDown(index);
			else ButtonUp(index);
		};
		_hooked.Add(button);
	}

	private void Detach() {
		Trace.TraceInformation($"Game controller disconnected from slot {ID}: {_controller?.VendorName ?? "(unknown)"}");
		foreach (var button in _hooked)
			button.PressedChangedHandler = null;
		_hooked.Clear();
		_controller = null;
		// Release everything so no key stays held after an unplug mid-press.
		for (int i = 0; i < ButtonStates.Length; i++)
			ButtonUp(i);
	}

	public override void Dispose() {
		if (_controller != null) Detach();
		base.Dispose();
	}

	// ---- slot management -------------------------------------------------------------

	// Notification tokens; kept alive for the app lifetime so observation is not GC'd away.
	private static readonly List<NSObject> _observers = new();

	/// <summary>Creates the fixed slot devices and starts watching for controllers.</summary>
	public static CInputGamepad_iOS[] CreateSlots(int count) {
		var slots = new CInputGamepad_iOS[count];
		for (int i = 0; i < count; i++)
			slots[i] = new CInputGamepad_iOS(i);
		_observers.Add(GCController.Notifications.ObserveDidConnect((_, e) => {
			if (e.Notification.Object is GCController controller) Bind(slots, controller);
		}));
		_observers.Add(GCController.Notifications.ObserveDidDisconnect((_, e) => {
			if (e.Notification.Object is not GCController controller) return;
			foreach (var slot in slots)
				if (slot._controller == controller)
					slot.Detach();
		}));
		foreach (var controller in GCController.Controllers)
			Bind(slots, controller);
		return slots;
	}

	private static void Bind(CInputGamepad_iOS[] slots, GCController controller) {
		if (controller.ExtendedGamepad == null) {
			Trace.TraceWarning($"Game controller without an extended profile ignored: {controller.VendorName ?? "(unknown)"}");
			return;
		}
		foreach (var slot in slots)
			if (slot._controller == controller)
				return;
		foreach (var slot in slots) {
			if (slot._controller == null) {
				slot.Attach(controller);
				return;
			}
		}
		Trace.TraceWarning($"All game controller slots in use; ignoring: {controller.VendorName ?? "(unknown)"}");
	}
}
