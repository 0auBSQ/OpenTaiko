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
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SampleFramework.Properties;

namespace SampleFramework
{
    /// <summary>
    /// Implements a specialized window for games and rendering.
    /// </summary>
    public class GameWindow : Form
    {
        const int DefaultWidth = 800;
        const int DefaultHeight = 600;
        const string DefaultTitle = "Game";

        Size cachedSize;
        bool minimized;
        bool maximized;
        bool inSizeMove;

        /// <summary>
        /// Occurs when the application is suspended.
        /// </summary>
        public event EventHandler Suspend;

        /// <summary>
        /// Occurs when the application is resumed.
        /// </summary>
        public event EventHandler Resume;

        /// <summary>
        /// Occurs when the user resizes the window.
        /// </summary>
        public event EventHandler UserResized;

        /// <summary>
        /// Occurs when the screen on which the window resides is changed.
        /// </summary>
        public event EventHandler ScreenChanged;

        /// <summary>
        /// Occurs when the application is activated.
        /// </summary>
        public event EventHandler ApplicationActivated;

        /// <summary>
        /// Occurs when the application is deactivated.
        /// </summary>
        public event EventHandler ApplicationDeactivated;

        /// <summary>
        /// Occurs when the system is suspended.
        /// </summary>
        public event EventHandler SystemSuspend;

        /// <summary>
        /// Occurs when the system is resumed.
        /// </summary>
        public event EventHandler SystemResume;

		/// <summary>
        /// Occurs when a screen saver is about to be activated.
        /// </summary>
        public event CancelEventHandler Screensaver;

		/// <summary>
        /// Gets a value indicating whether this instance is minimized.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is minimized; otherwise, <c>false</c>.
        /// </value>
        public bool IsMinimized
        {
            get { return minimized; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is maximized.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is maximized; otherwise, <c>false</c>.
        /// </value>
        public bool IsMaximized
        {
            get { return maximized; }
        }

		/// <summary>
		/// Gets or sets a value indicating whether System Menu is enabled.
		/// </summary>
		/// <value><c>true</c> if System Menu is enabled; otherwise, <c>false</c>.</value>
		public bool EnableSystemMenu			// #28200 2012.5.1 yyagi
		{
			get;
			set;
		}
		public string strMessage				// #28821 2014.1.23 yyagi
		{
			get;
			private set;
		}
		public bool IsReceivedMessage
		{
			get;
			set;
		}

		private Screen m_Screen;
        /// <summary>
        /// Gets the screen on which the window resides.
        /// </summary>
        /// <value>The screen.</value>
        public Screen Screen
        {
			get { return m_Screen; }
			private set { m_Screen = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameWindow"/> class.
        /// </summary>
        public GameWindow()
        {
            MinimumSize = new Size(200, 200);

            Screen = ScreenFromHandle(Handle);

            //Icon = GetDefaultIcon();
            Text = GetDefaultTitle();
			strMessage = "";
        }

        /// <summary>
        /// Raises the <see cref="E:Suspend"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected virtual void OnSuspend(EventArgs e)
        {
            if (Suspend != null)
                Suspend(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:Resume"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected virtual void OnResume(EventArgs e)
        {
            if (Resume != null)
                Resume(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:UserResized"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected virtual void OnUserResized(EventArgs e)
        {
            if (UserResized != null)
                UserResized(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:ScreenChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected virtual void OnScreenChanged(EventArgs e)
        {
            if (ScreenChanged != null)
                ScreenChanged(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:ApplicationActivated"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected virtual void OnApplicationActivated(EventArgs e)
        {
            if (ApplicationActivated != null)
                ApplicationActivated(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:ApplicationDeactivated"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected virtual void OnApplicationDeactivated(EventArgs e)
        {
            if (ApplicationDeactivated != null)
                ApplicationDeactivated(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:SystemSuspend"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected virtual void OnSystemSuspend(EventArgs e)
        {
            if (SystemSuspend != null)
                SystemSuspend(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:SystemResume"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected virtual void OnSystemResume(EventArgs e)
        {
            if (SystemResume != null)
                SystemResume(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:Screensaver"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        protected virtual void OnScreensaver(CancelEventArgs e)
        {
            if (Screensaver != null)
                Screensaver(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Form.Load"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            cachedSize = Size;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Form.ResizeBegin"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnResizeBegin(EventArgs e)
        {
            base.OnResizeBegin(e);

            // suspend any processing until we are done being minimized
            inSizeMove = true;
            cachedSize = Size;
            OnSuspend(EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Form.ResizeEnd"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);

            // check for screen and size changes
            OnUserResized(EventArgs.Empty);
            UpdateScreen();
            inSizeMove = false;

            // resume application processing
            OnResume(EventArgs.Empty);
		}


		#region #23510 2010.11.14 yyagi add: 縦横比固定でのウインドウサイズ変更 定数定義 from http://www.vcskicks.com/maintain-aspect-ratio.php
		//double so division keeps decimal points
		const double widthRatio = SampleFramework.GameWindowSize.Width;
		const double heightRatio = SampleFramework.GameWindowSize.Height;
		const int WM_SIZING = 0x214;
		const int WMSZ_LEFT = 1;
		const int WMSZ_RIGHT = 2;
		const int WMSZ_TOP = 3;
		const int WMSZ_TOPLEFT = 4;
		const int WMSZ_TOPRIGHT = 5;
		const int WMSZ_BOTTOM = 6;
		const int WMSZ_BOTTOMLEFT = 7;
		const int WMSZ_BOTTOMRIGHT = 8;

		public struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}
		#endregion



		/// <summary>
        /// Handles raw window messages.
        /// </summary>
        /// <param name="m">The raw message.</param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
			if (m.Msg == WindowConstants.WM_SIZE)
            {
                if (m.WParam == WindowConstants.SIZE_MINIMIZED)
                {
                    minimized = true;
                    maximized = false;

                    OnSuspend(EventArgs.Empty);
                }
                else
                {
                    Rectangle client = NativeMethods.GetClientRectangle(m.HWnd);
                    if (client.Height == 0)
                    {
                        // rapidly clicking the task bar to minimize and restore a window
                        // can cause a WM_SIZE message with SIZE_RESTORED when 
                        // the window has actually become minimized due to rapid change
                        // so just ignore this message
                    }
                    else if (m.WParam == WindowConstants.SIZE_MAXIMIZED)
                    {
                        if (minimized)
                            OnResume(EventArgs.Empty);

                        minimized = false;
                        maximized = true;

                        OnUserResized(EventArgs.Empty);
                        UpdateScreen();
                    }
                    else if (m.WParam == WindowConstants.SIZE_RESTORED)
                    {
                        if (minimized)
                            OnResume(EventArgs.Empty);

                        minimized = false;
                        maximized = false;

                        if (!inSizeMove && Size != cachedSize)
                        {
                            OnUserResized(EventArgs.Empty);
                            UpdateScreen();
                            cachedSize = Size;
                        }
                    }
                }
            }
            else if (m.Msg == WindowConstants.WM_ACTIVATEAPP)
            {
                if (m.WParam != IntPtr.Zero)
                    OnApplicationActivated(EventArgs.Empty);
                else
                    OnApplicationDeactivated(EventArgs.Empty);
            }
            else if (m.Msg == WindowConstants.WM_POWERBROADCAST)
            {
                if (m.WParam == WindowConstants.PBT_APMQUERYSUSPEND)
                {
                    OnSystemSuspend(EventArgs.Empty);
                    m.Result = (IntPtr)1;
                    return;
                }
                else if (m.WParam == WindowConstants.PBT_APMRESUMESUSPEND)
                {
                    OnSystemResume(EventArgs.Empty);
                    m.Result = (IntPtr)1;
                    return;
                }
            }
            else if (m.Msg == WindowConstants.WM_SYSCOMMAND)
            {
                long wparam = m.WParam.ToInt64() & 0xFFF0;
                if (wparam == WindowConstants.SC_MONITORPOWER || wparam == WindowConstants.SC_SCREENSAVE)
                {
                    CancelEventArgs e = new CancelEventArgs();
                    OnScreensaver(e);
                    if (e.Cancel)
                    {
                        m.Result = IntPtr.Zero;
                        return;
                    }
				}
				#region #28200 2012.5.1 yyagi: Disable system menu
				if ( ( m.WParam.ToInt32() & 0xFFFF ) == 0xF100 && !EnableSystemMenu )	// SC_KEYMENU
				{
					m.Result = IntPtr.Zero;
					return;
				}
				#endregion
				#region #23510 2010.11.13 yyagi: reset to 640x480
				if ((m.WParam.ToInt32() & 0xFFFF) == MENU_VIEW)		
				{
					base.ClientSize = new Size(SampleFramework.GameWindowSize.Width, SampleFramework.GameWindowSize.Height);
					this.OnResizeEnd(new EventArgs());		// #23510 2010.11.20 yyagi: to set window size to Config.ini
				}
				#endregion
			}
			#region #28821 2014.1.23 yyagi (WM_COPYDATA)
			else if ( m.Msg == WindowConstants.WM_COPYDATA )
			{
//Trace.WriteLine( "FDK;msg received" );
				COPYDATASTRUCT cds = (COPYDATASTRUCT) Marshal.PtrToStructure( m.LParam, typeof( COPYDATASTRUCT ) );
				strMessage = Marshal.PtrToStringUni( cds.lpData );
				IsReceivedMessage = true;
//Trace.WriteLine( "FDK;msg=" + strMessage + ", len=" + strMessage.Length + ", truelen=" + cds.cbData );
			}
			#endregion
			#region #23510 2010.11.16 yyagi add: 縦横比固定でのウインドウサイズ変更 from http://d.hatena.ne.jp/iselix/20080917/1221666614 http://hp.vector.co.jp/authors/VA016117/sizing.html
			else if ( m.Msg == WM_SIZING )
			{
				RECT rc = (RECT) Marshal.PtrToStructure( m.LParam, typeof( RECT ) );
				int w = rc.Right - rc.Left - ( Size.Width - ClientSize.Width );
				int h = rc.Bottom - rc.Top - ( Size.Height - ClientSize.Height );
				int dw = (int) ( h * widthRatio / heightRatio + 0.5 ) - w;
				int dh = (int) ( w / ( widthRatio / heightRatio ) + 0.5 ) - h;

				switch ( m.WParam.ToInt32() )
				{
					case WMSZ_LEFT:
					case WMSZ_RIGHT:
						rc.Bottom += dh;
						break;
					case WMSZ_TOP:
					case WMSZ_BOTTOM:
						rc.Right += dw;
						break;
					case WMSZ_BOTTOMRIGHT:
						if ( dw > 0 )
						{
							rc.Right += dw;
						}
						else
						{
							rc.Bottom += dh;
						}
						break;
					case WMSZ_TOPLEFT:
						if ( dw > 0 )
						{
							rc.Left -= dw;
						}
						else
						{
							rc.Top -= dh;
						}
						break;
					case WMSZ_TOPRIGHT:
						if ( dw > 0 )
						{
							rc.Right += dw;
						}
						else
						{
							rc.Top -= dh;
						}
						break;
					case WMSZ_BOTTOMLEFT:
						if ( dw > 0 )
						{
							rc.Left -= dw;
						}
						else
						{
							rc.Bottom += dh;
						}
						break;
					case 9:		// #32383 2013.11.2 yyagi; exitting maximized window by using Aero snap
						break;
					default:
						throw new ArgumentOutOfRangeException( "value", "Illegal WM_SIZING value." );
				}
				Marshal.StructureToPtr( rc, m.LParam, true );
			}
			#endregion


			base.WndProc(ref m);
        }

        void UpdateScreen()
        {
            Screen current = Screen.FromHandle(Handle);
            if (Screen == null || Screen.DeviceName != current.DeviceName)
            {
                Screen = current;
                if (Screen != null)
                    OnScreenChanged(EventArgs.Empty);
            }
        }

        static Screen ScreenFromHandle(IntPtr windowHandle)
        {
            Rectangle rectangle = NativeMethods.GetWindowRectangle(windowHandle);

            Screen bestScreen = null;
            int mostArea = 0;
            foreach (Screen screen in Screen.AllScreens)
            {
                Rectangle r = Rectangle.Intersect(rectangle, screen.Bounds);
                int area = r.Width * r.Height;

                if (area > mostArea)
                {
                    mostArea = area;
                    bestScreen = screen;
                }
            }

            if (bestScreen == null)
                bestScreen = Screen.PrimaryScreen;
            return bestScreen;
        }

        static string GetAssemblyTitle(Assembly assembly)
        {
            if (assembly == null)
                return null;

            AssemblyTitleAttribute[] customAttributes = (AssemblyTitleAttribute[])assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), true);
            if (customAttributes != null && customAttributes.Length > 0)
                return customAttributes[0].Title;

            return null;
        }

        static string GetDefaultTitle()
        {
            string assemblyTitle = GetAssemblyTitle(Assembly.GetEntryAssembly());
            if (!string.IsNullOrEmpty(assemblyTitle))
                return assemblyTitle;

            try
            {
                Uri uri = new Uri(Application.ExecutablePath);
                return Path.GetFileNameWithoutExtension(uri.LocalPath);
            }
            catch (ArgumentNullException e)
            {
                Trace.TraceError( e.ToString() );
                Trace.TraceError( "例外が発生しましたが処理を継続します。 (6216f3e1-e1a5-45ca-bfd8-30bbc44bfa9a)" );
            }
            catch (UriFormatException e)
            {
                Trace.TraceError( e.ToString() );
                Trace.TraceError( "例外が発生しましたが処理を継続します。 (771f37b5-0b56-4a47-933e-3c178b3e27a7)" );
            }

            return DefaultTitle;
        }

        static Icon GetDefaultIcon()
        {
            return (Icon)Resources.sdx_icon_black.Clone();
		}

		#region システムメニューに"640x480"を追加 #23510 2010.11.13 yyagi add: to set "640x480" menu in systemmenu. See also http://cs2ch.blog123.fc2.com/blog-entry-80.html
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private struct MENUITEMINFO
		{
			public uint cbSize;
			public uint fMask;
			public uint fType;
			public uint fState;
			public uint wID;
			public IntPtr hSubMenu;
			public IntPtr hbmpChecked;
			public IntPtr hbmpUnchecked;
			public IntPtr dwItemData;
			public string dwTypeData;
			public uint cch;
			public IntPtr hbmpItem;
		}

		[DllImport("user32", ExactSpelling = true)]
		private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
		[DllImport("user32", CharSet = CharSet.Auto)]
		private static extern bool InsertMenuItem(IntPtr hMenu, uint uItem, bool fByPosition, ref MENUITEMINFO lpmii);

		private const uint MENU_VIEW = 0x9999;
		private const uint MFT_SEPARATOR = 0x00000800;
		private const uint MIIM_FTYPE = 0x00000100;
		private const uint MIIM_STRING = 0x00000040;
		private const uint MIIM_ID = 0x00000002;

		protected override void OnCreateControl()
		{
			base.OnCreateControl();

			//システムメニューのハンドル取得   
			IntPtr hSysMenu = GetSystemMenu(this.Handle, false);

			//セパレーターの挿入   
			MENUITEMINFO item1 = new MENUITEMINFO();
			item1.cbSize = (uint)Marshal.SizeOf(item1);
			item1.fMask = MIIM_FTYPE;
			item1.fType = MFT_SEPARATOR;
			InsertMenuItem(hSysMenu, 5, true, ref item1);

			//メニュー項目の挿入   
			MENUITEMINFO item2 = new MENUITEMINFO();
			item2.cbSize = (uint)Marshal.SizeOf(item2);
			item2.fMask = MIIM_STRING | MIIM_ID;
			item2.wID = MENU_VIEW;
			item2.dwTypeData = "&" + SampleFramework.GameWindowSize.Width.ToString() + "x" + SampleFramework.GameWindowSize.Height.ToString();
			InsertMenuItem(hSysMenu, 6, true, ref item2);
		}   
		#endregion
	}
}
