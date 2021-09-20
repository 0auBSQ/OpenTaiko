/*
* Copyright (c) 2007-2009 SlimDX Group
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/
using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SlimDX;
using SlimDX.Direct3D9;
using SlimDX.DXGI;
using System.Diagnostics;

namespace SampleFramework
{
    /// <summary>
    /// Handles the configuration and management of the graphics device.
    /// </summary>
    public class GraphicsDeviceManager : IDisposable
    {
        Game game;
        bool ignoreSizeChanges;
        bool deviceLost;
//        bool doNotStoreBufferSize;
//        bool renderingOccluded;

        int fullscreenWindowWidth;
        int fullscreenWindowHeight;
        int windowedWindowWidth;
        int windowedWindowHeight;
        WINDOWPLACEMENT windowedPlacement;
        long windowedStyle;
        bool savedTopmost;

#if TEST_Direct3D9Ex
		internal static Direct3DEx Direct3D9Object			// yyagi
#else
		internal static Direct3D Direct3D9Object
#endif
		{
            get;
            private set;
        }

        public DeviceSettings CurrentSettings
        {
            get;
            private set;
        }
        public bool IsWindowed
        {
            get { return CurrentSettings.Windowed; }
        }
        public int ScreenWidth
        {
            get { return CurrentSettings.BackBufferWidth; }
        }
        public int ScreenHeight
        {
            get { return CurrentSettings.BackBufferHeight; }
        }
        public Size ScreenSize
        {
            get { return new Size(CurrentSettings.BackBufferWidth, CurrentSettings.BackBufferHeight); }
        }
        public Direct3D9Manager Direct3D9
        {
            get;
            private set;
        }
        public string DeviceStatistics
        {
            get;
            private set;
        }
        public string DeviceInformation
        {
            get;
            private set;
        }

		public GraphicsDeviceManager( Game game )
		{
			if( game == null )
				throw new ArgumentNullException( "game" );

			this.game = game;

			game.Window.ScreenChanged += Window_ScreenChanged;
			game.Window.UserResized += Window_UserResized;

			game.FrameStart += game_FrameStart;
			game.FrameEnd += game_FrameEnd;

			Direct3D9 = new Direct3D9Manager( this );
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

		public void ChangeDevice( DeviceSettings settings, DeviceSettings minimumSettings )
		{
			if( settings == null )
				throw new ArgumentNullException( "settings" );

			Enumeration9.MinimumSettings = minimumSettings;

			DeviceSettings validSettings = DeviceSettings.FindValidSettings( settings );

			validSettings.Direct3D9.PresentParameters.DeviceWindowHandle = game.Window.Handle;

			CreateDevice( validSettings );
		}
        public void ChangeDevice(bool windowed, int desiredWidth, int desiredHeight)
        {
            DeviceSettings desiredSettings = new DeviceSettings();
            desiredSettings.Windowed = windowed;
            desiredSettings.BackBufferWidth = desiredWidth;
            desiredSettings.BackBufferHeight = desiredHeight;

            ChangeDevice(desiredSettings, null);
        }
        public void ChangeDevice(DeviceSettings settings)
        {
            ChangeDevice(settings, null);
        }

        public void ToggleFullScreen()
        {
            if (!EnsureDevice())
                throw new InvalidOperationException("No valid device.");

            DeviceSettings newSettings = CurrentSettings.Clone();

            newSettings.Windowed = !newSettings.Windowed;

            int width = newSettings.Windowed ? windowedWindowWidth : fullscreenWindowWidth;
            int height = newSettings.Windowed ? windowedWindowHeight : fullscreenWindowHeight;

            newSettings.BackBufferWidth = width;
            newSettings.BackBufferHeight = height;

            ChangeDevice(newSettings);
        }
        public bool EnsureDevice()
        {
            if (Direct3D9.Device != null && !deviceLost)
                return true;

            return false;
        }

		protected virtual void Dispose( bool disposing )
		{
			if( this.bDisposed )
				return;
			this.bDisposed = true;

			if( disposing )
				ReleaseDevice();

		}
		private bool bDisposed = false;

        void CreateDevice(DeviceSettings settings)
        {
            DeviceSettings oldSettings = CurrentSettings;
            CurrentSettings = settings;

            ignoreSizeChanges = true;

            bool keepCurrentWindowSize = false;
            if (settings.BackBufferWidth == 0 && settings.BackBufferHeight == 0)
                keepCurrentWindowSize = true;

            // handle the window state in Direct3D9 (it will be handled for us in DXGI)
                // check if we are going to windowed or fullscreen mode
			if( settings.Windowed )
			{
				if( oldSettings != null && !oldSettings.Windowed )
					NativeMethods.SetWindowLong( game.Window.Handle, WindowConstants.GWL_STYLE, (uint) windowedStyle );
			}
			else
			{
				if( oldSettings == null || oldSettings.Windowed )
				{
					savedTopmost = game.Window.TopMost;
					long style = NativeMethods.GetWindowLong( game.Window.Handle, WindowConstants.GWL_STYLE );
					style &= ~WindowConstants.WS_MAXIMIZE & ~WindowConstants.WS_MINIMIZE;
					windowedStyle = style;

					windowedPlacement = new WINDOWPLACEMENT();
					windowedPlacement.length = WINDOWPLACEMENT.Length;
					NativeMethods.GetWindowPlacement( game.Window.Handle, ref windowedPlacement );
				}

				// hide the window until we are done messing with it
				game.Window.Hide();
				NativeMethods.SetWindowLong( game.Window.Handle, WindowConstants.GWL_STYLE, (uint) ( WindowConstants.WS_POPUP | WindowConstants.WS_SYSMENU ) );

				WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
				placement.length = WINDOWPLACEMENT.Length;
				NativeMethods.GetWindowPlacement( game.Window.Handle, ref placement );

				// check if we are in the middle of a restore
				if( ( placement.flags & WindowConstants.WPF_RESTORETOMAXIMIZED ) != 0 )
				{
					// update the flags to avoid sizing issues
					placement.flags &= ~WindowConstants.WPF_RESTORETOMAXIMIZED;
					placement.showCmd = WindowConstants.SW_RESTORE;
					NativeMethods.SetWindowPlacement( game.Window.Handle, ref placement );
				}
			}

            if (settings.Windowed)
            {
                if (oldSettings != null && !oldSettings.Windowed)
                {
                    fullscreenWindowWidth = oldSettings.BackBufferWidth;
                    fullscreenWindowHeight = oldSettings.BackBufferHeight;
                }
            }
            else
            {
                if (oldSettings != null && oldSettings.Windowed)
                {
                    windowedWindowWidth = oldSettings.BackBufferWidth;
                    windowedWindowHeight = oldSettings.BackBufferHeight;
                }
            }

            // check if the device can be reset, or if we need to completely recreate it
            Result result = SlimDX.Direct3D9.ResultCode.Success;
            bool canReset = CanDeviceBeReset(oldSettings, settings);
            if (canReset)
                result = ResetDevice();

            if (result == SlimDX.Direct3D9.ResultCode.DeviceLost)
                deviceLost = true;
            else if (!canReset || result.IsFailure)
            {
                if (oldSettings != null)
                    ReleaseDevice();

                InitializeDevice();
            }

            UpdateDeviceInformation();

            // check if we changed from fullscreen to windowed mode
            if (oldSettings != null && !oldSettings.Windowed && settings.Windowed)
            {
                NativeMethods.SetWindowPlacement(game.Window.Handle, ref windowedPlacement);
                game.Window.TopMost = savedTopmost;
            }

            // check if we need to resize
            if (settings.Windowed && !keepCurrentWindowSize)
            {
                int width;
                int height;
                if (NativeMethods.IsIconic(game.Window.Handle))
                {
                    WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
                    placement.length = WINDOWPLACEMENT.Length;
                    NativeMethods.GetWindowPlacement(game.Window.Handle, ref placement);

                    // check if we are being restored
                    if ((placement.flags & WindowConstants.WPF_RESTORETOMAXIMIZED) != 0 && placement.showCmd == WindowConstants.SW_SHOWMINIMIZED)
                    {
                        NativeMethods.ShowWindow(game.Window.Handle, WindowConstants.SW_RESTORE);

                        Rectangle rect = NativeMethods.GetClientRectangle(game.Window.Handle);

                        width = rect.Width;
                        height = rect.Height;
                        NativeMethods.ShowWindow(game.Window.Handle, WindowConstants.SW_MINIMIZE);
                    }
                    else
                    {
                        NativeRectangle frame = new NativeRectangle();
                        NativeMethods.AdjustWindowRect(ref frame, (uint)windowedStyle, false);
                        int frameWidth = frame.right - frame.left;
                        int frameHeight = frame.bottom - frame.top;

                        width = placement.rcNormalPosition.right - placement.rcNormalPosition.left - frameWidth;
                        height = placement.rcNormalPosition.bottom - placement.rcNormalPosition.top - frameHeight;
                    }
                }
                else
                {
                    Rectangle rect = NativeMethods.GetClientRectangle(game.Window.Handle);
                    width = rect.Width;
                    height = rect.Height;
                }

                // check if we have a different desired size
                if (width != settings.BackBufferWidth ||
                    height != settings.BackBufferHeight)
                {
                    if (NativeMethods.IsIconic(game.Window.Handle))
                        NativeMethods.ShowWindow(game.Window.Handle, WindowConstants.SW_RESTORE);
                    if (NativeMethods.IsZoomed(game.Window.Handle))
                        NativeMethods.ShowWindow(game.Window.Handle, WindowConstants.SW_RESTORE);

                    NativeRectangle rect = new NativeRectangle();
                    rect.right = settings.BackBufferWidth;
                    rect.bottom = settings.BackBufferHeight;
                    NativeMethods.AdjustWindowRect(ref rect,
                        NativeMethods.GetWindowLong(game.Window.Handle, WindowConstants.GWL_STYLE), false);

                    NativeMethods.SetWindowPos(game.Window.Handle, IntPtr.Zero, 0, 0, rect.right - rect.left,
                        rect.bottom - rect.top, WindowConstants.SWP_NOZORDER | WindowConstants.SWP_NOMOVE);

                    Rectangle r = NativeMethods.GetClientRectangle(game.Window.Handle);
                    int clientWidth = r.Width;
                    int clientHeight = r.Height;

                    // check if the size was modified by Windows
                    if (clientWidth != settings.BackBufferWidth ||
                        clientHeight != settings.BackBufferHeight)
                    {
                        DeviceSettings newSettings = CurrentSettings.Clone();
                        newSettings.BackBufferWidth = 0;
                        newSettings.BackBufferHeight = 0;
                        if (newSettings.Direct3D9 != null)
                        {
							newSettings.Direct3D9.PresentParameters.BackBufferWidth = GameWindowSize.Width;	// #23510 2010.10.31 add yyagi: to avoid setting BackBufferSize=ClientSize
							newSettings.Direct3D9.PresentParameters.BackBufferHeight = GameWindowSize.Height;	// #23510 2010.10.31 add yyagi: to avoid setting BackBufferSize=ClientSize
                        }

                        CreateDevice(newSettings);
                    }
                }
            }

            // if the window is still hidden, make sure it is shown
            if (!game.Window.Visible)
                NativeMethods.ShowWindow(game.Window.Handle, WindowConstants.SW_SHOW);

            // set the execution state of the thread
            if (!IsWindowed)
                NativeMethods.SetThreadExecutionState(WindowConstants.ES_DISPLAY_REQUIRED | WindowConstants.ES_CONTINUOUS);
            else
                NativeMethods.SetThreadExecutionState(WindowConstants.ES_CONTINUOUS);

            ignoreSizeChanges = false;
        }

		void Window_UserResized( object sender, EventArgs e )
		{
			if( ignoreSizeChanges || !EnsureDevice() || ( !IsWindowed ) )
				return;

			DeviceSettings newSettings = CurrentSettings.Clone();

			Rectangle rect = NativeMethods.GetClientRectangle( game.Window.Handle );
			if( rect.Width != newSettings.BackBufferWidth || rect.Height != newSettings.BackBufferHeight )
			{
				newSettings.BackBufferWidth = 0;
				newSettings.BackBufferHeight = 0;
				newSettings.Direct3D9.PresentParameters.BackBufferWidth = GameWindowSize.Width;		// #23510 2010.10.31 add yyagi: to avoid setting BackBufferSize=ClientSize
				newSettings.Direct3D9.PresentParameters.BackBufferHeight = GameWindowSize.Height;	// 
				CreateDevice( newSettings );
			}
		}
		void Window_ScreenChanged( object sender, EventArgs e )
		{
			if( !EnsureDevice() || !CurrentSettings.Windowed || ignoreSizeChanges )
				return;

			IntPtr windowMonitor = NativeMethods.MonitorFromWindow( game.Window.Handle, WindowConstants.MONITOR_DEFAULTTOPRIMARY );

			DeviceSettings newSettings = CurrentSettings.Clone();
			int adapterOrdinal = GetAdapterOrdinal( windowMonitor );
			if( adapterOrdinal == -1 )
				return;
			newSettings.Direct3D9.AdapterOrdinal = adapterOrdinal;

			newSettings.BackBufferWidth = 0;								// #23510 2010.11.1 add yyagi to avoid to reset to 640x480 for the first time in XP.
			newSettings.BackBufferHeight = 0;								//
			newSettings.Direct3D9.PresentParameters.BackBufferWidth = GameWindowSize.Width;		//
			newSettings.Direct3D9.PresentParameters.BackBufferHeight = GameWindowSize.Height;	//

			CreateDevice(newSettings);
		}

		void game_FrameEnd( object sender, EventArgs e )
		{
			Result result = SlimDX.Direct3D9.ResultCode.Success;
			try
			{
				result = Direct3D9.Device.Present();
			}
			catch (Direct3D9Exception)				// #23842 2011.1.6 yyagi: catch D3D9Exception to avoid unexpected termination by changing VSyncWait in fullscreen.
			{
				deviceLost = true;
			}
			if( result == SlimDX.Direct3D9.ResultCode.DeviceLost )
				deviceLost = true;
		}
        void game_FrameStart(object sender, CancelEventArgs e)
        {
            if (Direct3D9.Device == null )
            {
                e.Cancel = true;
                return;
            }

//            if (!game.IsActive || deviceLost)		// #23568 2010.11.3 yyagi: separate conditions to support valiable sleep value when !IsActive.
			if (deviceLost)
				Thread.Sleep(50);
			else if (!game.IsActive && !this.CurrentSettings.EnableVSync)	// #23568 2010.11.4 yyagi: Don't add sleep() while VSync is enabled.
				Thread.Sleep(this.game.InactiveSleepTime.Milliseconds);

            if (deviceLost)
            {
                Result result = Direct3D9.Device.TestCooperativeLevel();
                if (result == SlimDX.Direct3D9.ResultCode.DeviceLost)
                {
                    e.Cancel = true;
                    return;
                }

                // if we are windowed, check the adapter format to see if the user
                // changed the desktop format, causing a lost device
                if (IsWindowed)
                {
                    DisplayMode displayMode = GraphicsDeviceManager.Direct3D9Object.GetAdapterDisplayMode(CurrentSettings.Direct3D9.AdapterOrdinal);
                    if (CurrentSettings.Direct3D9.AdapterFormat != displayMode.Format)
                    {
                        DeviceSettings newSettings = CurrentSettings.Clone();
                        ChangeDevice(newSettings);
                        e.Cancel = true;
                        return;
                    }
                }

                result = ResetDevice();
                if (result.IsFailure)
                {
                    e.Cancel = true;
                    return;
                }
            }

            deviceLost = false;
        }

		bool CanDeviceBeReset( DeviceSettings oldSettings, DeviceSettings newSettings )
		{
			if( oldSettings == null )
				return false;

			return Direct3D9.Device != null &&
				oldSettings.Direct3D9.AdapterOrdinal == newSettings.Direct3D9.AdapterOrdinal &&
				oldSettings.Direct3D9.DeviceType == newSettings.Direct3D9.DeviceType &&
				oldSettings.Direct3D9.CreationFlags == newSettings.Direct3D9.CreationFlags;
		}

        void InitializeDevice()
        {
			try
			{
				EnsureD3D9();

#if TEST_Direct3D9Ex
				// 2011.4.26 yyagi
				// Direct3D9.DeviceExを呼ぶ際(IDirect3D9Ex::CreateDeviceExを呼ぶ際)、
				// フルスクリーンモードで初期化する場合はDisplayModeEx(D3DDISPLAYMODEEX *pFullscreenDisplayMode)に
				// 適切な値を設定する必要あり。
				// 一方、ウインドウモードで初期化する場合は、D3DDISPLAYMODEEXをNULLにする必要があるが、
				// DisplayModeExがNULL不可と定義されているため、DeviceExのoverloadの中でDisplayModeExを引数に取らないものを
				// 使う。(DeviceEx側でD3DDISPLAYMODEEXをNULLにしてくれる)
				// 結局、DeviceExの呼び出しの際に、フルスクリーンかどうかで場合分けが必要となる。
				if ( CurrentSettings.Direct3D9.PresentParameters.Windowed == false )
				{
					DisplayModeEx fullScreenDisplayMode = new DisplayModeEx();
					fullScreenDisplayMode.Width = CurrentSettings.Direct3D9.PresentParameters.BackBufferWidth;
					fullScreenDisplayMode.Height = CurrentSettings.Direct3D9.PresentParameters.BackBufferHeight;
					fullScreenDisplayMode.RefreshRate = CurrentSettings.Direct3D9.PresentParameters.FullScreenRefreshRateInHertz;
					fullScreenDisplayMode.Format = CurrentSettings.Direct3D9.PresentParameters.BackBufferFormat;

					Direct3D9.Device = new SlimDX.Direct3D9.DeviceEx( Direct3D9Object, CurrentSettings.Direct3D9.AdapterOrdinal,
						CurrentSettings.Direct3D9.DeviceType, game.Window.Handle,
						CurrentSettings.Direct3D9.CreationFlags, CurrentSettings.Direct3D9.PresentParameters, fullScreenDisplayMode );
				}
				else
				{
					Direct3D9.Device = new SlimDX.Direct3D9.DeviceEx( Direct3D9Object, CurrentSettings.Direct3D9.AdapterOrdinal,
						CurrentSettings.Direct3D9.DeviceType, game.Window.Handle,
						CurrentSettings.Direct3D9.CreationFlags, CurrentSettings.Direct3D9.PresentParameters );
				}
				Direct3D9.Device.MaximumFrameLatency = 1;
#else
				Direct3D9.Device = new DeviceCache( new SlimDX.Direct3D9.Device( Direct3D9Object, CurrentSettings.Direct3D9.AdapterOrdinal,
					CurrentSettings.Direct3D9.DeviceType, game.Window.Handle,
					CurrentSettings.Direct3D9.CreationFlags, CurrentSettings.Direct3D9.PresentParameters ) );
#endif
				if ( Result.Last == SlimDX.Direct3D9.ResultCode.DeviceLost )
				{
					deviceLost = true;
					return;
				}
#if TEST_Direct3D9Ex
				Direct3D9.Device.MaximumFrameLatency = 1;			// yyagi
#endif
			}
			catch( Exception e )
			{
				throw new DeviceCreationException( "Could not create graphics device.", e );
			}

            PropogateSettings();

            UpdateDeviceStats();

            game.Initialize();
            game.LoadContent();
        }

		Result ResetDevice()
		{
			game.UnloadContent();

			Result result = Direct3D9.Device.Reset( CurrentSettings.Direct3D9.PresentParameters );
			if( result == SlimDX.Direct3D9.ResultCode.DeviceLost )
				return result;

			PropogateSettings();
			UpdateDeviceStats();
			game.LoadContent();

			return Result.Last;
		}

		void ReleaseDevice()
        {
            ReleaseDevice9();
        }
        void ReleaseDevice9()
        {
            if (Direct3D9.Device == null)
                return;

            if (game != null)
            {
                game.UnloadContent();
                game.Dispose(true);
            }

			try
			{
				Direct3D9.Device.Dispose();
			}
			catch( ObjectDisposedException e )
			{
				// 時々発生するのでキャッチしておく。
				Trace.TraceError( e.ToString() );
				Trace.TraceError( "例外が発生しましたが処理を継続します。 (fc0b6e70-181e-410f-b47f-5490ca4ce0c3)" );
			}
            Direct3D9Object.Dispose();

            Direct3D9Object = null;
            Direct3D9.Device = null;
        }
		void PropogateSettings()
		{
			CurrentSettings.BackBufferCount = CurrentSettings.Direct3D9.PresentParameters.BackBufferCount;
			CurrentSettings.BackBufferWidth = CurrentSettings.Direct3D9.PresentParameters.BackBufferWidth;
			CurrentSettings.BackBufferHeight = CurrentSettings.Direct3D9.PresentParameters.BackBufferHeight;
			CurrentSettings.BackBufferFormat = CurrentSettings.Direct3D9.PresentParameters.BackBufferFormat;
			CurrentSettings.DepthStencilFormat = CurrentSettings.Direct3D9.PresentParameters.AutoDepthStencilFormat;
			CurrentSettings.DeviceType = CurrentSettings.Direct3D9.DeviceType;
			CurrentSettings.MultisampleQuality = CurrentSettings.Direct3D9.PresentParameters.MultisampleQuality;
			CurrentSettings.MultisampleType = CurrentSettings.Direct3D9.PresentParameters.Multisample;
			CurrentSettings.RefreshRate = CurrentSettings.Direct3D9.PresentParameters.FullScreenRefreshRateInHertz;
			CurrentSettings.Windowed = CurrentSettings.Direct3D9.PresentParameters.Windowed;
		}

		void UpdateDeviceInformation()
		{
			StringBuilder builder = new StringBuilder();

			if( CurrentSettings.Direct3D9.DeviceType == DeviceType.Hardware )
				builder.Append( "HAL" );
			else if( CurrentSettings.Direct3D9.DeviceType == DeviceType.Reference )
				builder.Append( "REF" );
			else if( CurrentSettings.Direct3D9.DeviceType == DeviceType.Software )
				builder.Append( "SW" );

			if( ( CurrentSettings.Direct3D9.CreationFlags & CreateFlags.HardwareVertexProcessing ) != 0 )
				if( CurrentSettings.Direct3D9.DeviceType == DeviceType.Hardware )
					builder.Append( " (hw vp)" );
				else
					builder.Append( " (simulated hw vp)" );
			else if( ( CurrentSettings.Direct3D9.CreationFlags & CreateFlags.MixedVertexProcessing ) != 0 )
				if( CurrentSettings.Direct3D9.DeviceType == DeviceType.Hardware )
					builder.Append( " (mixed vp)" );
				else
					builder.Append( " (simulated mixed vp)" );
			else
				builder.Append( " (sw vp)" );

			if( CurrentSettings.Direct3D9.DeviceType == DeviceType.Hardware )
			{
				// loop through each adapter until we find the right one
				foreach( AdapterInfo9 adapterInfo in Enumeration9.Adapters )
				{
					if( adapterInfo.AdapterOrdinal == CurrentSettings.Direct3D9.AdapterOrdinal )
					{
						builder.AppendFormat( ": {0}", adapterInfo.Description );
						break;
					}
				}
			}

			DeviceInformation = builder.ToString();
		}

		void UpdateDeviceStats()
		{
			StringBuilder builder = new StringBuilder();

			builder.Append( "D3D9 Vsync " );

			if( CurrentSettings.Direct3D9.PresentParameters.PresentationInterval == PresentInterval.Immediate )
				builder.Append( "off" );
			else
				builder.Append( "on" );

			builder.AppendFormat( " ({0}x{1}), ", CurrentSettings.Direct3D9.PresentParameters.BackBufferWidth, CurrentSettings.Direct3D9.PresentParameters.BackBufferHeight );

			if( CurrentSettings.Direct3D9.AdapterFormat == CurrentSettings.Direct3D9.PresentParameters.BackBufferFormat )
				builder.Append( Enum.GetName( typeof( SlimDX.Direct3D9.Format ), CurrentSettings.Direct3D9.AdapterFormat ) );
			else
				builder.AppendFormat( "backbuf {0}, adapter {1}",
					Enum.GetName( typeof( SlimDX.Direct3D9.Format ), CurrentSettings.Direct3D9.AdapterFormat ),
					Enum.GetName( typeof( SlimDX.Direct3D9.Format ), CurrentSettings.Direct3D9.PresentParameters.BackBufferFormat ) );

			builder.AppendFormat( " ({0})", Enum.GetName( typeof( SlimDX.Direct3D9.Format ), CurrentSettings.Direct3D9.PresentParameters.AutoDepthStencilFormat ) );

			if( CurrentSettings.Direct3D9.PresentParameters.Multisample == MultisampleType.NonMaskable )
				builder.Append( " (Nonmaskable Multisample)" );
			else if( CurrentSettings.Direct3D9.PresentParameters.Multisample != MultisampleType.None )
				builder.AppendFormat( " ({0}x Multisample)", (int) CurrentSettings.Direct3D9.PresentParameters.Multisample );

			DeviceStatistics = builder.ToString();
		}

		int GetAdapterOrdinal( IntPtr screen )
		{
			AdapterInfo9 adapter = null;
			foreach( AdapterInfo9 a in Enumeration9.Adapters )
			{
				if( Direct3D9Object.GetAdapterMonitor( a.AdapterOrdinal ) == screen )
				{
					adapter = a;
					break;
				}
			}

			if( adapter != null )
				return adapter.AdapterOrdinal;

			return -1;
		}

        internal static void EnsureD3D9()
        {
			if ( Direct3D9Object == null )
#if TEST_Direct3D9Ex
				Direct3D9Object = new Direct3DEx();		// yyagi
#else
				Direct3D9Object = new Direct3D();
#endif
        }
    }
}
