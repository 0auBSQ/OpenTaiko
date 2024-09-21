namespace FDK {
	public class CInputMIDI : IInputDevice, IDisposable {
		// プロパティ

		public IntPtr MidiInPtr;
		public List<STInputEvent> EventBuffers;

		// コンストラクタ

		public CInputMIDI(uint nID) {
			this.MidiInPtr = IntPtr.Zero;
			this.EventBuffers = new List<STInputEvent>(32);
			this.InputEvents = new List<STInputEvent>(32);
			this.CurrentType = InputDeviceType.MidiIn;
			this.GUID = "";
			this.ID = (int)nID;
			this.strDeviceName = "";    // CInput管理で初期化する
		}


		// メソッド

		public void tメッセージからMIDI信号のみ受信(uint wMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2, long n受信システム時刻) {
			/*
			if (wMsg == CWin32.MIM_DATA)
			{
				int nMIDIevent = (int)dwParam1 & 0xF0;
				int nPara1 = ((int)dwParam1 >> 8) & 0xFF;
				int nPara2 = ((int)dwParam1 >> 16) & 0xFF;
				int nPara3 = ((int)dwParam2 >> 8) & 0xFF;
				int nPara4 = ((int)dwParam2 >> 16) & 0xFF;

				// Trace.TraceInformation( "MIDIevent={0:X2} para1={1:X2} para2={2:X2}", nMIDIevent, nPara1, nPara2 ,nPara3,nPara4);

				if ((nMIDIevent == 0x90) && (nPara2 != 0))      // Note ON
				{
					STInputEvent item = new STInputEvent();
					item.nKey = nPara1;
					item.b押された = true;
					item.nTimeStamp = n受信システム時刻;
					item.nVelocity = nPara2;
					this.listEventBuffer.Add(item);
				}
				//else if ( ( nMIDIevent == 0xB0 ) && ( nPara1 == 4 ) )	// Ctrl Chg #04: Foot Controller
				//{
				//	STInputEvent item = new STInputEvent();
				//	item.nKey = nPara1;
				//	item.b押された = true;
				//	item.nTimeStamp = n受信システム時刻;
				//	item.nVelocity = nPara2;
				//	this.listEventBuffer.Add( item );
				//}
			}
			*/
		}

		#region [ IInputDevice 実装 ]
		//-----------------
		public InputDeviceType CurrentType { get; private set; }
		public string GUID { get; private set; }
		public int ID { get; private set; }
		public List<STInputEvent> InputEvents { get; private set; }
		public string strDeviceName { get; set; }

		public void Polling(bool bWindowがアクティブ中) {
			// this.list入力イベント = new List<STInputEvent>( 32 );
			this.InputEvents.Clear();                                // #xxxxx 2012.6.11 yyagi; To optimize, I removed new();

			for (int i = 0; i < this.EventBuffers.Count; i++)
				this.InputEvents.Add(this.EventBuffers[i]);

			this.EventBuffers.Clear();
		}
		public bool KeyPressed(int nKey) {
			foreach (STInputEvent event2 in this.InputEvents) {
				if ((event2.nKey == nKey) && event2.Pressed) {
					return true;
				}
			}
			return false;
		}
		public bool KeyPressing(int nKey) {
			return false;
		}
		public bool KeyReleased(int nKey) {
			return false;
		}
		public bool KeyReleasing(int nKey) {
			return false;
		}
		//-----------------
		#endregion

		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose() {
			if (this.EventBuffers != null) {
				this.EventBuffers = null;
			}
			if (this.InputEvents != null) {
				this.InputEvents = null;
			}
		}
		//-----------------
		#endregion
	}
}
