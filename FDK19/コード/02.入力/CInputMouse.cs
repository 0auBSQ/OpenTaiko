using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SharpDX;
using SharpDX.DirectInput;

namespace FDK
{
	public class CInputMouse : IInputDevice, IDisposable
	{
		// 定数

		public const int nマウスの最大ボタン数 = 8;


		// コンストラクタ

		public CInputMouse(IntPtr hWnd, DirectInput directInput)
		{
			this.e入力デバイス種別 = E入力デバイス種別.Mouse;
			this.GUID = "";
			this.ID = 0;
			try
			{
				this.devMouse = new Mouse(directInput);
				this.devMouse.SetCooperativeLevel(hWnd, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
				this.devMouse.Properties.BufferSize = 0x20;
				Trace.TraceInformation(this.devMouse.Information.ProductName.Trim(new char[] { '\0' }) + " を生成しました。");  // なぜか0x00のゴミが出るので削除
				this.strDeviceName = this.devMouse.Information.ProductName.Trim(new char[] { '\0' });
			}
			catch
			{
				if (this.devMouse != null)
				{
					this.devMouse.Dispose();
					this.devMouse = null;
				}
				Trace.TraceWarning("Mouse デバイスの生成に失敗しました。");
				throw;
			}
			try
			{
				this.devMouse.Acquire();
			}
			catch
			{
			}

			for (int i = 0; i < this.bMouseState.Length; i++)
				this.bMouseState[i] = false;

			//this.timer = new CTimer( CTimer.E種別.MultiMedia );
			this.list入力イベント = new List<STInputEvent>(32);
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
			for (int i = 0; i < 8; i++)
			{
				this.bMousePushDown[i] = false;
				this.bMousePullUp[i] = false;
			}

			if (bWindowがアクティブ中 && (this.devMouse != null))
			{
				this.devMouse.Acquire();
				this.devMouse.Poll();

				// this.list入力イベント = new List<STInputEvent>( 32 );
				this.list入力イベント.Clear();            // #xxxxx 2012.6.11 yyagi; To optimize, I removed new();

				if (bバッファ入力を使用する)
				{
					#region [ a.バッファ入力 ]
					//-----------------------------
					var bufferedData = this.devMouse.GetBufferedData();
					//if( Result.Last.IsSuccess && bufferedData != null )
					{
						foreach (MouseUpdate data in bufferedData)
						{
							var mouseButton = new[] {
								 MouseOffset.Buttons0,
								 MouseOffset.Buttons1,
								 MouseOffset.Buttons2,
								 MouseOffset.Buttons3,
								 MouseOffset.Buttons4,
								 MouseOffset.Buttons5,
								 MouseOffset.Buttons6,
								 MouseOffset.Buttons7,
							};

							for (int k = 0; k < 8; k++)
							{
								//if( data.IsPressed( k ) )
								if (data.Offset == mouseButton[k] && ((data.Value & 0x80) != 0))
								{
									STInputEvent item = new STInputEvent()
									{
										nKey = k,
										b押された = true,
										b離された = false,
										nTimeStamp = CSound管理.rc演奏用タイマ.nサウンドタイマーのシステム時刻msへの変換(data.Timestamp),
										nVelocity = CInput管理.n通常音量
									};
									this.list入力イベント.Add(item);

									this.bMouseState[k] = true;
									this.bMousePushDown[k] = true;
								}
								else if (data.Offset == mouseButton[k] && this.bMouseState[k] == true && ((data.Value & 0x80) == 0))
								//else if( data.IsReleased( k ) )
								{
									STInputEvent item = new STInputEvent()
									{
										nKey = k,
										b押された = false,
										b離された = true,
										nTimeStamp = CSound管理.rc演奏用タイマ.nサウンドタイマーのシステム時刻msへの変換(data.Timestamp),
										nVelocity = CInput管理.n通常音量
									};
									this.list入力イベント.Add(item);

									this.bMouseState[k] = false;
									this.bMousePullUp[k] = true;
								}
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
					MouseState currentState = this.devMouse.GetCurrentState();
					//if( Result.Last.IsSuccess && currentState != null )
					{
						bool[] buttons = currentState.Buttons;

						for (int j = 0; (j < buttons.Length) && (j < 8); j++)
						{
							if (this.bMouseState[j] == false && buttons[j] == true)
							{
								var ev = new STInputEvent()
								{
									nKey = j,
									b押された = true,
									b離された = false,
									nTimeStamp = CSound管理.rc演奏用タイマ.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInput管理.n通常音量,
								};
								this.list入力イベント.Add(ev);

								this.bMouseState[j] = true;
								this.bMousePushDown[j] = true;
							}
							else if(this.bMouseState[j] == true && buttons[j] == false)
							{
								var ev = new STInputEvent()
								{
									nKey = j,
									b押された = false,
									b離された = true,
									nTimeStamp = CSound管理.rc演奏用タイマ.nシステム時刻, // 演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。
									nVelocity = CInput管理.n通常音量,
								};
								this.list入力イベント.Add(ev);

								this.bMouseState[j] = false;
								this.bMousePullUp[j] = true;
							}
						}
					}
					//-----------------------------
					#endregion
				}
			}
		}
		public bool bキーが押された(int nButton)
		{
			return (((0 <= nButton) && (nButton < 8)) && this.bMousePushDown[nButton]);
		}
		public bool bキーが押されている(int nButton)
		{
			return (((0 <= nButton) && (nButton < 8)) && this.bMouseState[nButton]);
		}
		public bool bキーが離された(int nButton)
		{
			return (((0 <= nButton) && (nButton < 8)) && this.bMousePullUp[nButton]);
		}
		public bool bキーが離されている(int nButton)
		{
			return (((0 <= nButton) && (nButton < 8)) && !this.bMouseState[nButton]);
		}
		//-----------------
		#endregion

		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose()
		{
			if(!this.bDispose完了済み)
			{
				if(this.devMouse != null)
				{
					this.devMouse.Dispose();
					this.devMouse = null;
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
	    private bool bDispose完了済み;
		private bool[] bMousePullUp = new bool[8];
		private bool[] bMousePushDown = new bool[8];
		private bool[] bMouseState = new bool[8];
		private Mouse devMouse;
		//private CTimer timer;
		//-----------------
		#endregion
	}
}
