namespace FDK;

/// <summary>
/// Null-object input device: all queries return false/empty.
/// Used on platforms without keyboard/mouse (e.g. iOS) to avoid null checks everywhere.
/// </summary>
public class CInputNull : IInputDevice {
	public Silk.NET.Input.IInputDevice Device => null;
	public InputDeviceType CurrentType => InputDeviceType.Unknown;
	public string GUID => "";
	public int ID { get; set; } = -1;
	public string Name => "Null";
	public List<STInputEvent> InputEvents => _empty;
	public bool useBufferInput { get; set; }

	private static readonly List<STInputEvent> _empty = new();

	public void Polling() { }
	public bool KeyPressed(int nKey) => false;
	public bool KeyPressing(int nKey) => false;
	public bool KeyReleased(int nKey) => false;
	public bool KeyReleasing(int nKey) => false;
	public void Dispose() { }
}
