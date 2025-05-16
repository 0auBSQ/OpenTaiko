namespace FDK;

public interface IInputDevice : IDisposable {
	// Properties

	Silk.NET.Input.IInputDevice Device {
		get;
	}
	InputDeviceType CurrentType {
		get;
	}
	string GUID {
		get;
	}
	int ID {
		get;
	}
	string Name {
		get;
	}
	List<STInputEvent> InputEvents {
		get;
	}
	bool useBufferInput { get; set; }


	// メソッドインターフェース

	void Polling();
	bool KeyPressed(int nKey);
	bool KeyPressed(List<int> nKey) { return nKey.Any(key => KeyPressed(key)); }
	bool KeyPressing(int nKey);
	bool KeyPressing(List<int> nKey) { return nKey.Any(key => KeyPressing(key)); }
	bool KeyReleased(int nKey);
	bool KeyReleased(List<int> nKey) { return nKey.Any(key => KeyReleased(key)); }
	bool KeyReleasing(int nKey);
	bool KeyReleasing(List<int> nKey) { return nKey.Any(key => KeyReleasing(key)); }
	string GetButtonName(int nKey) { return $"Button{nKey}"; }
}
