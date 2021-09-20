using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace FDK
{
	public class CSoundTimer : CTimerBase
	{
		public override long nシステム時刻ms
		{
			get
			{
				if( this.Device.e出力デバイス == ESoundDeviceType.ExclusiveWASAPI || 
					this.Device.e出力デバイス == ESoundDeviceType.SharedWASAPI ||
					this.Device.e出力デバイス == ESoundDeviceType.ASIO )
				{
					// BASS 系の ISoundDevice.n経過時間ms はオーディオバッファの更新間隔ずつでしか更新されないため、単にこれを返すだけではとびとびの値になる。
					// そこで、更新間隔の最中に呼ばれた場合は、システムタイマを使って補間する。
					// この場合の経過時間との誤差は更新間隔以内に収まるので問題ないと判断する。
					// ただし、ASIOの場合は、転送byte数から時間算出しているため、ASIOの音声合成処理の負荷が大きすぎる場合(処理時間が実時間を超えている場合)は
					// 動作がおかしくなる。(具体的には、ここで返すタイマー値の逆行が発生し、スクロールが巻き戻る)
					// この場合の対策は、ASIOのバッファ量を増やして、ASIOの音声合成処理の負荷を下げること。

					if ( this.Device.n経過時間を更新したシステム時刻ms == CTimer.n未使用 )	// #33890 2014.5.27 yyagi
					{
						// 環境によっては、ASIOベースの演奏タイマーが動作する前(つまりASIOのサウンド転送が始まる前)に
						// DTXデータの演奏が始まる場合がある。
						// その場合、"this.Device.n経過時間を更新したシステム時刻" が正しい値でないため、
						// 演奏タイマの値が正しいものとはならない。そして、演奏タイマーの動作が始まると同時に、
						// 演奏タイマの値がすっ飛ぶ(極端な負の値になる)ため、演奏のみならず画面表示もされない状態となる。
						// (画面表示はタイマの値に連動して行われるが、0以上のタイマ値に合わせて動作するため、
						//  不の値が来ると画面に何も表示されなくなる)

						// そこで、演奏タイマが動作を始める前(this.Device.n経過時間を更新したシステム時刻ms == CTimer.n未使用)は、
						// 補正部分をゼロにして、n経過時間msだけを返すようにする。
						// こうすることで、演奏タイマが動作を始めても、破綻しなくなる。
						return this.Device.n経過時間ms;
					}
					else
					{
						if ( FDK.CSound管理.bUseOSTimer )
						//if ( true )
						{
							return ctDInputTimer.nシステム時刻ms;				// 仮にCSoundTimerをCTimer相当の動作にしてみた
						}
						else
						{
							return this.Device.n経過時間ms
								+ ( this.Device.tmシステムタイマ.nシステム時刻ms - this.Device.n経過時間を更新したシステム時刻ms );
						}
					}
				}
				else if( this.Device.e出力デバイス == ESoundDeviceType.DirectSound )
				{
					//return this.Device.n経過時間ms;		// #24820 2013.2.3 yyagi TESTCODE DirectSoundでスクロールが滑らかにならないため、
					return ct.nシステム時刻ms;				// 仮にCSoundTimerをCTimer相当の動作にしてみた
				}
				return CTimerBase.n未使用;
			}
		}

		internal CSoundTimer( ISoundDevice device )
		{
			this.Device = device;

			if ( this.Device.e出力デバイス != ESoundDeviceType.DirectSound )
			{
				TimerCallback timerDelegate = new TimerCallback( SnapTimers );	// CSoundTimerをシステム時刻に変換するために、
				timer = new Timer( timerDelegate, null, 0, 1000 );				// CSoundTimerとCTimerを両方とも走らせておき、
				ctDInputTimer = new CTimer( CTimer.E種別.MultiMedia );			// 1秒に1回時差を測定するようにしておく
			}
			else																// TESTCODE DirectSound時のみ、CSoundTimerでなくCTimerを使う
			{
			    ct = new CTimer( CTimer.E種別.MultiMedia );
			}
		}
	
		private void SnapTimers(object o)	// 1秒に1回呼び出され、2つのタイマー間の現在値をそれぞれ保持する。
		{
			if ( this.Device.e出力デバイス != ESoundDeviceType.DirectSound )
			{
				try
				{
					this.nDInputTimerCounter = this.ctDInputTimer.nシステム時刻ms;
					this.nSoundTimerCounter = this.nシステム時刻ms;
					//Debug.WriteLine( "BaseCounter: " + nDInputTimerCounter + ", " + nSoundTimerCounter );
				}
				catch ( Exception e )
				// サウンド設定変更時に、timer.Dispose()した後、timerが実際に停止する前にここに来てしまう場合があり
				// その際にNullReferenceExceptionが発生する
				// timerが実際に停止したことを検出してから次の設定をすべきだが、実装が難しいため、
				// ここで単に例外破棄することで代替する
				{
					Trace.TraceInformation( e.ToString() );
					Trace.TraceInformation("FDK: CSoundTimer.SnapTimers(): 例外発生しましたが、継続します。" );
				}
			}
		}
		public long nサウンドタイマーのシステム時刻msへの変換( long nDInputのタイムスタンプ )
		{
			return nDInputのタイムスタンプ - this.nDInputTimerCounter + this.nSoundTimerCounter;	// Timer違いによる時差を補正する
		}

#if false
		// キーボードイベント(keybd_eventの引数と同様のデータ)
		[StructLayout( LayoutKind.Sequential )]
		private struct KEYBDINPUT
		{
			public ushort wVk;
			public ushort wScan;
			public uint dwFlags;
			public uint time;
			public IntPtr dwExtraInfo;
		};
		// 各種イベント(SendInputの引数データ)
		[StructLayout( LayoutKind.Sequential )]
		private struct INPUT
		{
			public int type;
			public KEYBDINPUT ki;
		};
		// キー操作、マウス操作をシミュレート(擬似的に操作する)
		[DllImport( "user32.dll" )]
		private extern static void SendInput(
			int nInputs, ref INPUT pInputs, int cbsize );

		// 仮想キーコードをスキャンコードに変換
		[DllImport( "user32.dll", EntryPoint = "MapVirtualKeyA" )]
		private extern static int MapVirtualKey(
			int wCode, int wMapType );
		
		[DllImport( "user32.dll" )]
		static extern IntPtr GetMessageExtraInfo();

		private const int INPUT_MOUSE = 0;                  // マウスイベント
		private const int INPUT_KEYBOARD = 1;               // キーボードイベント
		private const int INPUT_HARDWARE = 2;               // ハードウェアイベント
		private const int KEYEVENTF_KEYDOWN = 0x0;          // キーを押す
		private const int KEYEVENTF_KEYUP = 0x2;            // キーを離す
		private const int KEYEVENTF_EXTENDEDKEY = 0x1;      // 拡張コード
		private const int KEYEVENTF_SCANCODE = 0x8;
		private const int KEYEVENTF_UNIOCODE = 0x4;
		private const int VK_SHIFT = 0x10;                  // SHIFTキー

		private void pollingSendInput()
		{
//			INPUT[] inp = new INPUT[ 2 ];
			INPUT inp = new INPUT();
			while ( true )
			{
				// (2)キーボード(A)を押す
				//inp[0].type = INPUT_KEYBOARD;
				//inp[ 0 ].ki.wVk = ( ushort ) Key.B;
				//inp[ 0 ].ki.wScan = ( ushort ) MapVirtualKey( inp[ 0 ].ki.wVk, 0 );
				//inp[ 0 ].ki.dwFlags = KEYEVENTF_KEYDOWN;
				//inp[ 0 ].ki.dwExtraInfo = IntPtr.Zero;
				//inp[ 0 ].ki.time = 0;
				inp.type = INPUT_KEYBOARD;
				inp.ki.wVk = ( ushort ) Key.B;
				inp.ki.wScan = ( ushort ) MapVirtualKey( inp.ki.wVk, 0 );
				inp.ki.dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYDOWN;
				inp.ki.dwExtraInfo = GetMessageExtraInfo();
				inp.ki.time = 0;

				//// (3)キーボード(A)を離す
				//inp[ 1 ].type = INPUT_KEYBOARD;
				//inp[ 1 ].ki.wVk = ( short ) Key.B;
				//inp[ 1 ].ki.wScan = ( short ) MapVirtualKey( inp[ 1 ].ki.wVk, 0 );
				//inp[ 1 ].ki.dwFlags = KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP;
				//inp[ 1 ].ki.dwExtraInfo = 0;
				//inp[ 1 ].ki.time = 0;

				// キーボード操作実行
				SendInput( 1, ref inp, Marshal.SizeOf( inp ) );
Debug.WriteLine( "B" );
				Thread.Sleep( 1000 );
			}
		}
#endif
		public override void Dispose()
		{
			// 特になし； ISoundDevice の解放は呼び出し元で行うこと。

			//sendinputスレッド削除
			if ( timer != null )
			{
				timer.Change( System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite );
				// ここで、実際にtimerが停止したことを確認するコードを追加すべきだが、やり方わからず。
				// 代替策として、SnapTimers()中で、例外発生を破棄している。
				timer.Dispose();
				timer = null;
			}
			if ( ct != null )
			{
				ct.t一時停止();
				ct.Dispose();
				ct = null;
			}
		}

		internal ISoundDevice Device = null;	// debugのため、一時的にprotectedをpublicにする。後で元に戻しておくこと。
		//protected Thread thSendInput = null;
		//protected Thread thSnapTimers = null;
		private CTimer ctDInputTimer = null;
		private long nDInputTimerCounter = 0;
		private long nSoundTimerCounter = 0;
		Timer timer = null;

		private CTimer ct = null;								// TESTCODE
	}
}
