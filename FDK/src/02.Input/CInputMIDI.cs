using System.Collections.Immutable;
using System.Diagnostics;
using Commons.Music.Midi;

namespace FDK;

public class CInputMIDI : CInputButtonsBase, IInputDevice, IDisposable {
	// Properties

	public List<STInputEvent> EventBuffers;
	protected int[] velocities;

	public override int GetVelocity(int index) => velocities[index];
	protected override void SetVelocity(int index, int velocity) => velocities[index] = velocity;

	// Constructor

	public CInputMIDI(IMidiInput midiIn, int index) : base(128) {
		this.DeviceMidi = midiIn;
		this.CurrentType = InputDeviceType.MidiIn;
		this.GUID = midiIn.Details.Id;
		this.ID = index;
		this.Name = midiIn.Details.Name;
		this.velocities = new int[this.ButtonStates.Length];

		midiIn.MessageReceived += MidiIn_MessageReceived;
	}


	// メソッド

	private void MidiIn_MessageReceived(object? sender, MidiReceivedEventArgs? ev) {
		try {
			foreach (var msg in MidiEvent.Convert(ev!.Data, ev.Start, ev.Length)) {
				// ignore channel for now
				switch (msg.EventType) {
					case MidiEvent.NoteOn:
						if (msg.Lsb > 0) {
							this.ButtonUp(msg.Msb); // in case of missing note off or from another channel
							this.ButtonDown(msg.Msb, msg.Lsb);
							break;
						}
						goto case MidiEvent.NoteOff;
					case MidiEvent.NoteOff:
						this.ButtonUp(msg.Msb);
						break;
				}
			}
		} catch (Exception e) {
			Trace.TraceError(e.ToString());
		}
	}

	public static readonly ImmutableArray<string> KeyNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
	public static string GetButtonName(int index) => $"{KeyNames[index % 12]}{index / 12 - 1}[{index}]";

	#region [ IInputDevice 実装 ]
	//-----------------
	public object DeviceGeneric { get => DeviceMidi; }
	public IMidiInput DeviceMidi { get; private set; }

	//-----------------
	#endregion

	#region [ IDisposable 実装 ]
	//-----------------
	public override void Dispose() {
		if (!this.IsDisposed) {
			this.DeviceMidi?.Dispose();
			base.Dispose();
		}
	}
	//-----------------
	#endregion
}
