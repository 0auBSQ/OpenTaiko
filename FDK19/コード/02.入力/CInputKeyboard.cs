using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SharpDX;
using SharpDX.DirectInput;

using SlimDXKey = SlimDXKeys.Key;
using SharpDXKey = SharpDX.DirectInput.Key;

namespace FDK
{
	public class CInputKeyboard : IInputDevice, IDisposable
	{
		// コンストラクタ

		public CInputKeyboard(IntPtr hWnd, DirectInput directInput)
		{
			this.e入力デバイス種別 = E入力デバイス種別.Keyboard;
			this.GUID = "";
			this.ID = 0;
			try
			{
				this.devKeyboard = new Keyboard(directInput);
				this.devKeyboard.SetCooperativeLevel(hWnd, CooperativeLevel.NoWinKey | CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
				this.devKeyboard.Properties.BufferSize = 32;
				Trace.TraceInformation(this.devKeyboard.Information.ProductName.Trim(new char[] { '\0' }) + " を生成しました。");    // なぜか0x00のゴミが出るので削除
				this.strDeviceName = this.devKeyboard.Information.ProductName.Trim(new char[] { '\0' });
			}
			catch
			{
				if(this.devKeyboard != null)
				{
					this.devKeyboard.Dispose();
					this.devKeyboard = null;
				}
				Trace.TraceWarning("Keyboard デバイスの生成に失敗しました。");
				throw;
			}
			try
			{
				this.devKeyboard.Acquire();
			}
			catch
			{
			}

			for (int i = 0; i < this.bKeyState.Length; i++)
				this.bKeyState[i] = false;

			//this.timer = new CTimer( CTimer.E種別.MultiMedia );
			this.list入力イベント = new List<STInputEvent>(32);
			// this.ct = new CTimer( CTimer.E種別.PerformanceCounter );
		}


		// メソッド

		#region [ IInputDevice 実装 ]
		//-----------------
		public E入力デバイス種別 e入力デバイス種別 { get; private set; }
		public string GUID { get; private set; }
		public int ID { get; private set; }
		public List<STInputEvent> list入力イベント { get; private set; }
		public string strDeviceName { get; set; }

		public void tポーリング(bool bWindowがアクティブ中, bool bバッファ入力を使用する)
		{
			for (int i = 0; i < 256; i++)
			{
				this.bKeyPushDown[i] = false;
				this.bKeyPullUp[i] = false;
			}

			if (bWindowがアクティブ中 && (this.devKeyboard != null))
			{
				this.devKeyboard.Acquire();
				this.devKeyboard.Poll();

				//this.list入力イベント = new List<STInputEvent>( 32 );
				this.list入力イベント.Clear();            // #xxxxx 2012.6.11 yyagi; To optimize, I removed new();
				int posEnter = -1;
				//string d = DateTime.Now.ToString( "yyyy/MM/dd HH:mm:ss.ffff" );

				if (bバッファ入力を使用する)
				{
					#region [ a.バッファ入力 ]
					//-----------------------------
					var bufferedData = this.devKeyboard.GetBufferedData();
					//if ( Result.Last.IsSuccess && bufferedData != null )
					{
						foreach (KeyboardUpdate data in bufferedData)
						{
							// #xxxxx: 2017.5.7: from: DIK (SharpDX.DirectInput.Key) を SlimDX.DirectInput.Key に変換。
							var key = DeviceConstantConverter.DIKtoKey(data.Key);
							if (SlimDXKey.Unknown == key)
								continue;   // 未対応キーは無視。

							//foreach ( Key key in data.PressedKeys )
							if (data.IsPressed)
							{
								// #23708 2016.3.19 yyagi; Even if we remove ALT+ENTER key input by SuppressKeyPress = true in Form,
								// it doesn't affect to DirectInput (ALT+ENTER does not remove)
								// So we ignore ENTER input in ALT+ENTER combination here.
								// Note: ENTER will be alived if you keyup ALT after ALT+ENTER.
								if (key != SlimDXKey.Return || (bKeyState[(int)SlimDXKey.LeftAlt] == false && bKeyState[(int)SlimDXKey.RightAlt] == false))
								{
									STInputEvent item = new STInputEvent()
									{
										nKey = (int)key,
										b押された = true,
										b離された = false,
										nTimeStamp = CSound管理.rc演奏用タイマ.nサウンドタイマーのシステム時刻msへの変換(data.Timestamp),
										nVelocity = CInput管理.n通常音量
									};
									this.list入力イベント.Add(item);

									this.bKeyState[(int)key] = true;
									this.bKeyPushDown[(int)key] = true;
								}
								//if ( item.nKey == (int) SlimDXKey.Space )
								//{
								//    Trace.TraceInformation( "FDK(buffered): SPACE key registered. " + ct.nシステム時刻 );
								//}
							}
							//foreach ( Key key in data.ReleasedKeys )
							if (data.IsReleased)
							{
								STInputEvent item = new STInputEvent()
								{
									nKey = (int)key,
									b押された = false,
									b離された = true,
									nTimeStamp = CSound管理.rc演奏用タイマ.nサウンドタイマーのシステム時刻msへの変換(data.Timestamp),
									nVelocity = CInput管理.n通常音量
								};
								this.list入力イベント.Add(item);
								this.bKeyState[(int)key] = false;
								this.bKeyPullUp[(int)key] = true;
							}
						}
					}
					//-----------------------------
					#endregion
				}
				else
				{
					#region [ b.状態入力 ]
					//-----------------------------
					KeyboardState currentState = this.devKeyboard.GetCurrentState();
					//if ( Result.Last.IsSuccess && currentState != null )
					{
						foreach (SharpDXKey dik in currentState.PressedKeys)
						{
							// #xxxxx: 2017.5.7: from: DIK (SharpDX.DirectInput.Key) を SlimDX.DirectInput.Key に変換。
							var key = DeviceConstantConverter.DIKtoKey(dik);
							if (SlimDXKey.Unknown == key)
								continue;   // 未対応キーは無視。

							if (this.bKeyState[(int)key] == false)
							{
								if (key != SlimDXKey.Return || (bKeyState[(int)SlimDXKey.LeftAlt] == false && bKeyState[(int)SlimDXKey.RightAlt] == false))    // #23708 2016.3.19 yyagi
								{
									var ev = new STInputEvent()
									{
										nKey = (int)key,
										b押された = true,
										b離された = false,
										nTimeStamp = CSound管理.rc演奏用タイマ.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
										nVelocity = CInput管理.n通常音量,
									};
									this.list入力イベント.Add(ev);

									this.bKeyState[(int)key] = true;
									this.bKeyPushDown[(int)key] = true;
								}

								//if ( (int) key == (int) SlimDXKey.Space )
								//{
								//    Trace.TraceInformation( "FDK(direct): SPACE key registered. " + ct.nシステム時刻 );
								//}
							}
						}
						//foreach ( Key key in currentState.ReleasedKeys )
						foreach (SharpDXKey dik in currentState.AllKeys)
						{
							// #xxxxx: 2017.5.7: from: DIK (SharpDX.DirectInput.Key) を SlimDX.DirectInput.Key に変換。
							var key = DeviceConstantConverter.DIKtoKey(dik);
							if (SlimDXKey.Unknown == key)
								continue;   // 未対応キーは無視。

							if (this.bKeyState[(int)key] == true && !currentState.IsPressed(dik)) // 前回は押されているのに今回は押されていない → 離された
							{
								var ev = new STInputEvent()
								{
									nKey = (int)key,
									b押された = false,
									b離された = true,
									nTimeStamp = CSound管理.rc演奏用タイマ.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInput管理.n通常音量,
								};
								this.list入力イベント.Add(ev);

								this.bKeyState[(int) key] = false;
								this.bKeyPullUp[(int) key] = true;
							}
						}
					}
					//-----------------------------
					#endregion
				}
				#region [#23708 2011.4.8 yyagi Altが押されているときは、Enter押下情報を削除する -> 副作用が見つかり削除]
				//if ( this.bKeyState[ (int) SlimDXKey.RightAlt ] ||
				//     this.bKeyState[ (int) SlimDXKey.LeftAlt ] )
				//{
				//    int cr = (int) SlimDXKey.Return;
				//    this.bKeyPushDown[ cr ] = false;
				//    this.bKeyPullUp[ cr ] = false;
				//    this.bKeyState[ cr ] = false;
				//}
				#endregion
			}
		}
		/// <param name="nKey">
		///		調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
		/// </param>
		public bool bキーが押された(int nKey)
		{
			return this.bKeyPushDown[nKey];
		}
		/// <param name="nKey">
		///		調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
		/// </param>
		public bool bキーが押されている(int nKey)
		{
			return this.bKeyState[ nKey ];
		}
		/// <param name="nKey">
		///		調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
		/// </param>
		public bool bキーが離された(int nKey)
		{
			return this.bKeyPullUp[nKey];
		}
		/// <param name="nKey">
		///		調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
		/// </param>
		public bool bキーが離されている(int nKey)
		{
			return !this.bKeyState[nKey];
		}
		//-----------------
		#endregion

		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose()
		{
			if(!this.bDispose完了済み)
			{
				if(this.devKeyboard != null)
				{
					this.devKeyboard.Dispose();
					this.devKeyboard = null;
				}
				//if( this.timer != null )
				//{
				//    this.timer.Dispose();
				//    this.timer = null;
				//}
				if (this.list入力イベント != null)
				{
					this.list入力イベント = null;
				}
				this.bDispose完了済み = true;
			}
		}
		//-----------------
		#endregion


		// その他

		#region [ private ]
		//-----------------
		private bool[] bKeyPullUp = new bool[256];
		private bool[] bKeyPushDown = new bool[256];
		private bool[] bKeyState = new bool[256];
		private bool bDispose完了済み;
		private Keyboard devKeyboard;
	    //private CTimer timer;
		//private CTimer ct;
		//-----------------
		#endregion
	}
}
