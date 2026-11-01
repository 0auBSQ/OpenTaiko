using Commons.Music.Midi;

namespace FDK;

public interface IInputDevice : IDisposable {
	// Properties

	// Default implement. Need to implement either Device or DeviceMidi.
	object DeviceGeneric => ((object?)Device ?? DeviceMidi)!;
	Silk.NET.Input.IInputDevice? Device { get => null; }
	IMidiPortDetails? DeviceMidi { get => null; }

	InputDeviceType CurrentType {
		get;
	}
	string GUID {
		get;
	}
	int ID {
		get; set;
	}
	string Name {
		get;
	}
	List<STInputEvent> InputEvents {
		get;
	}


	// Methods

	void Polling(bool accumulate = false);

	// Valid state combinations:
	// * (none): Device or inputs not available
	// * [releasing]: Continuing released; initial state, if device and inputs are available
	// * [pressed, pressing]: Just has been pressed
	// * [pressing]: Continuing pressing
	// * [released, releasing]: Just has been released
	// * [pressed, released, releasing]: Just has been pressed and immediately released
	// Restrictions:
	// * pressing and releasing are mutually exclusive
	// * pressed must come with either pressing or releasing
	// * released must come with releasing
	bool KeyAvailable(int nKey);
	bool KeyAvailable(List<int> nKey) { return nKey.Any(key => KeyAvailable(key)); }
	bool KeyPressed(int nKey);
	bool KeyPressed(List<int> nKey) { return nKey.Any(key => KeyPressed(key)); }
	bool KeyPressing(int nKey);
	bool KeyPressing(List<int> nKey) { return nKey.Any(key => KeyPressing(key)); }
	bool KeyReleased(int nKey);
	bool KeyReleased(List<int> nKey) { return nKey.Any(key => KeyReleased(key)); }
	bool KeyReleasing(int nKey) => KeyAvailable(nKey) && !KeyPressing(nKey);
	bool KeyReleasing(List<int> nKey) {
		var availables = nKey.Where(key => KeyAvailable(key));
		return availables.Any() && availables.All(key => !KeyPressing(key));
	}
	string GetButtonName(int nKey) { return $"Button{nKey}"; }
}
