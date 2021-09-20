using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace FDK
{
	public class CInputMIDI : IInputDevice, IDisposable
	{
		// プロパティ

		public IntPtr hMidiIn;
		public List<STInputEvent> listEventBuffer;

		// コンストラクタ

		public CInputMIDI(uint nID)
		{
			this.hMidiIn = IntPtr.Zero;
			this.listEventBuffer = new List<STInputEvent>(32);
			this.list入力イベント = new List<STInputEvent>(32);
			this.e入力デバイス種別 = E入力デバイス種別.MidiIn;
			this.GUID = "";
			this.ID = (int)nID;
			this.strDeviceName = "";    // CInput管理で初期化する
		}


		// メソッド

		public void tメッセージからMIDI信号のみ受信(uint wMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2, long n受信システム時刻)
		{
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
		}

		#region [ IInputDevice 実装 ]
		//-----------------
		public E入力デバイス種別 e入力デバイス種別 { get; private set; }
		public string GUID { get; private set; }
		public int ID { get; private set; }
		public List<STInputEvent> list入力イベント { get; private set; }
		public string strDeviceName { get; set; }

		public void tポーリング(bool bWindowがアクティブ中, bool bバッファ入力を使用する)
		{
			// this.list入力イベント = new List<STInputEvent>( 32 );
			this.list入力イベント.Clear();                                // #xxxxx 2012.6.11 yyagi; To optimize, I removed new();

			for (int i = 0; i < this.listEventBuffer.Count; i++)
				this.list入力イベント.Add(this.listEventBuffer[i]);

			this.listEventBuffer.Clear();
		}
		public bool bキーが押された(int nKey)
		{
			foreach (STInputEvent event2 in this.list入力イベント)
			{
				if ((event2.nKey == nKey) && event2.b押された)
				{
					return true;
				}
			}
			return false;
		}
		public bool bキーが押されている(int nKey)
		{
			return false;
		}
		public bool bキーが離された(int nKey)
		{
			return false;
		}
		public bool bキーが離されている(int nKey)
		{
			return false;
		}
		//-----------------
		#endregion

		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose()
		{
			if (this.listEventBuffer != null)
			{
				this.listEventBuffer = null;
			}
			if (this.list入力イベント != null)
			{
				this.list入力イベント = null;
			}
		}
		//-----------------
		#endregion
	}
}
