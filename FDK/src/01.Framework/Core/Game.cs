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
using System.Threading;
using System.Collections.ObjectModel;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using SkiaSharp;

namespace SampleFramework
{
    public enum GraphicsDeviceType
    {
        OpenGL,
        Vulkan,
        DirectX11,
        DirectX12
    }

    /// <summary>
    /// Presents an easy to use wrapper for making games and samples.
    /// </summary>
    public abstract class Game : IDisposable
    {
        protected string _Text = "";
        protected string Text
        {
            get
            {
                return _Text;
            }
            set
            {
                _Text = value;
                if (Window_ != null)
                {
                    Window_.Title = value;
                }
            }
        }

        public static GraphicsDeviceType GraphicsDeviceType_ = GraphicsDeviceType.OpenGL;

        public IWindow Window_;

        public static IShader Shader_;

        public static IPolygon Polygon_;

        public static IGraphicsDevice GraphicsDevice;

        private Vector2D<int> _WindowSize;
        public Vector2D<int> WindowSize
        {
            get
            {
                return _WindowSize;
            }
            set
            {
                _WindowSize = value;
                if (Window_ != null)
                {
                    Window_.Size = value;
                }
            }
        }

        private Vector2D<int> _WindowPosition;
        public Vector2D<int> WindowPosition
        {
            get
            {
                return _WindowPosition;
            }
            set
            {
                _WindowPosition = value;
                if (Window_ != null)
                {
                    Window_.Position = value;
                }
            }
        }

        private int _Framerate;

        public int Framerate
        {
            get
            {
                return _Framerate;
            }
            set
            {
                _Framerate = value;
                if (Window_ != null)
                {
                    UpdateWindowFramerate(VSync, value);
                }
            }
        }

        private bool _FullScreen;
        public bool FullScreen
        {
            get
            {
                return _FullScreen;
            }
            set
            {
                _FullScreen = value;
                if (Window_ != null)
                {
                    Window_.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
                }
            }
        }

        private bool _VSync;
        public bool VSync
        {
            get
            {
                return _VSync;
            }
            set
            {
                _VSync = value;
                if (Window_ != null)
                {
                    UpdateWindowFramerate(value, Framerate);
                    Window_.VSync = value;
                }
            }
        }

        internal static int VerticalFix
        {
            get 
            {
                return GraphicsDeviceType_ == GraphicsDeviceType.OpenGL ? -1 : 1;
            }
        }

        private GraphicsAPI GetGraphicsAPI()
        {
            switch (GraphicsDeviceType_)
            {
                case GraphicsDeviceType.OpenGL:
                    return GraphicsAPI.Default;
                case GraphicsDeviceType.Vulkan:
                    return GraphicsAPI.DefaultVulkan;
                default:
                    return GraphicsAPI.None;
            }
        }

        public unsafe SKBitmap GetScreenShot()
        {
            return GraphicsDevice.GetScreenPixels();
        }

        public static long TimeMs;

        public static Matrix4X4<float> Camera;

        public static float ScreenAspect
        {
            get 
            {
                return (float)GameWindowSize.Width / GameWindowSize.Height;
            }
        }

        /// <summary>
        /// Initializes the <see cref="Game"/> class.
        /// </summary>
        static Game()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Game"/> class.
        /// </summary>
        protected Game()
        {
            Configuration();
            
            WindowOptions options = GraphicsDeviceType_ == GraphicsDeviceType.Vulkan ? WindowOptions.DefaultVulkan : WindowOptions.Default;

            options.Size = WindowSize;
            options.Position = WindowPosition;
            options.UpdatesPerSecond = VSync ? 0 : Framerate;
            options.FramesPerSecond = VSync ? 0 : Framerate;
            options.WindowState = FullScreen ? WindowState.Fullscreen : WindowState.Normal;
            options.VSync = VSync;
            options.API = GetGraphicsAPI();
            options.WindowBorder = WindowBorder.Resizable;
            options.Title = Text;
            
            Silk.NET.Windowing.Glfw.GlfwWindowing.Use();
            //Silk.NET.Windowing.Sdl.SdlWindowing.Use();

            Window_ = Window.Create(options);
            Window_.Load += Window_Load;
            Window_.Closing += Window_Closing;
            Window_.Update += Window_Update;
            Window_.Render += Window_Render;
            Window_.Resize += Window_Resize;
            Window_.Move += Window_Move;
            Window_.FramebufferResize += Window_FramebufferResize;
        }

        private void UpdateWindowFramerate(bool vsync, int value)
        {
            if (vsync)
            {
                Window_.UpdatesPerSecond = 0;
                Window_.FramesPerSecond = 0;
            }
            else
            {
                Window_.UpdatesPerSecond = value;
                Window_.FramesPerSecond = value;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Window_.Dispose();
        }

        public void Exit()
        {
            Window_.Close();
        }

        protected void ToggleWindowMode()
        {
            /*
			DeviceSettings settings = base.GraphicsDeviceManager.CurrentSettings.Clone();
			if ( ( ConfigIni != null ) && ( ConfigIni.bウィンドウモード != settings.Windowed ) )
			{
				settings.Windowed = ConfigIni.bウィンドウモード;
				if ( ConfigIni.bウィンドウモード == false )	// #23510 2010.10.27 yyagi: backup current window size before going fullscreen mode
				{
					currentClientSize = this.Window.ClientSize;
					ConfigIni.nウインドウwidth = this.Window.ClientSize.Width;
					ConfigIni.nウインドウheight = this.Window.ClientSize.Height;
//					FDK.CTaskBar.ShowTaskBar( false );
				}
				base.GraphicsDeviceManager.ChangeDevice( settings );
				if ( ConfigIni.bウィンドウモード == true )	// #23510 2010.10.27 yyagi: to resume window size from backuped value
				{
					base.Window.ClientSize =
						new Size( currentClientSize.Width, currentClientSize.Height );
                    base.Window.Icon = Properties.Resources.tjap3;
//					FDK.CTaskBar.ShowTaskBar( true );
				}
			}
			*/

            FullScreen = !FullScreen;
        }

        /// <summary>
        /// Runs the game.
        /// </summary>
        public void Run()
        {
            Window_.Run();
        }

        protected virtual void Configuration()
        {

        }

        protected virtual void Initialize()
        {

        }


        protected virtual void LoadContent()
        {

        }

        protected virtual void UnloadContent()
        {

        }

        protected virtual void OnExiting()
        {

        }

        protected virtual void Update()
        {

        }

        protected virtual void Draw()
        {

        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected internal virtual void Dispose(bool disposing)
        {

        }


        public void Window_Load()
        {
            switch (GraphicsDeviceType_)
            {
                case GraphicsDeviceType.OpenGL:
                    GraphicsDevice = new OpenGLDevice(Window_);
                    break;
                case GraphicsDeviceType.Vulkan:
                    GraphicsDevice = new VulkanDevice(Window_);
                    break;
                case GraphicsDeviceType.DirectX11:
                    GraphicsDevice = new DirectX11Device(Window_);
                    break;
                case GraphicsDeviceType.DirectX12:
                    GraphicsDevice = new DirectX12Device(Window_);
                    break;
            }


            GraphicsDevice.SetClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GraphicsDevice.SetViewPort(0, 0, (uint)Window_.Size.X, (uint)Window_.FramebufferSize.Y);
            GraphicsDevice.SetFrameBuffer((uint)Window_.FramebufferSize.X, (uint)Window_.FramebufferSize.Y);

            Shader_ = GraphicsDevice.GenShader($@"Shaders{Path.AltDirectorySeparatorChar}Common");

            Polygon_ = GraphicsDevice.GenPolygon(
                new float[]
                {
                    1, 1 * VerticalFix, 0.0f,
                    1, -1 * VerticalFix, 0.0f,
                    -1, -1 * VerticalFix, 0.0f,
                    -1, 1 * VerticalFix, 0.0f
                }
                ,
                new uint[]
                {
                    0u, 1u, 3u,
                    1u, 2u, 3u
                }
                ,
                new float[]
                {
                    1.0f, 0.0f,
                    1.0f, 1.0f,
                    0.0f, 1.0f,
                    0.0f, 0.0f,
                }
            );

            Initialize();
            LoadContent();
        }

        public void Window_Closing()
        {

            UnloadContent();
            OnExiting();
            
            Polygon_.Dispose();
            Shader_.Dispose();

            GraphicsDevice.Dispose();
        }

        public void Window_Update(double deltaTime)
        {
            double fps = 1.0f / deltaTime;
            TimeMs = (long)(Window_.Time * 1000);
            Update();
        }

        public void Window_Render(double deltaTime)
        {
            Camera = Matrix4X4<float>.Identity;
            GraphicsDevice.ClearBuffer();

            Draw();

            GraphicsDevice.SwapBuffer();

            double fps = 1.0f / deltaTime;
        }

        public void Window_Resize(Vector2D<int> size)
        {
            WindowSize = size;
            GraphicsDevice.SetViewPort(0, 0, (uint)size.X, (uint)size.Y);
        }

        public void Window_Move(Vector2D<int> size)
        {
            WindowPosition = size;
        }

        public void Window_FramebufferResize(Vector2D<int> size)
        {
            GraphicsDevice.SetFrameBuffer((uint)size.X, (uint)size.Y);
        }
    }
}
