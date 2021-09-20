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
using System.Windows.Forms;
using SlimDX;
using SlimDX.Direct3D9;
using System.Collections.ObjectModel;

namespace SampleFramework
{
    /// <summary>
    /// Presents an easy to use wrapper for making games and samples.
    /// </summary>
    public abstract class Game : IDisposable
    {
        GameClock clock = new GameClock();
        GameTime gameTime = new GameTime();
        TimeSpan maximumElapsedTime = TimeSpan.FromMilliseconds(500.0);
        TimeSpan totalGameTime;
        TimeSpan accumulatedElapsedGameTime;
        TimeSpan lastFrameElapsedGameTime;
        TimeSpan lastFrameElapsedRealTime;
        TimeSpan targetElapsedTime = TimeSpan.FromTicks(166667);
        TimeSpan inactiveSleepTime = TimeSpan.FromMilliseconds(20.0);
        int updatesSinceRunningSlowly1 = int.MaxValue;
        int updatesSinceRunningSlowly2 = int.MaxValue;
        bool forceElapsedTimeToZero;
        bool drawRunningSlowly;
        long lastUpdateFrame;
        float lastUpdateTime;

        /// <summary>
        /// Occurs when the game is disposed.
        /// </summary>
        public event EventHandler Disposed;

        /// <summary>
        /// Occurs when the game is activated.
        /// </summary>
        public event EventHandler Activated;

        /// <summary>
        /// Occurs when the game is deactivated.
        /// </summary>
        public event EventHandler Deactivated;

        /// <summary>
        /// Occurs when the game is exiting.
        /// </summary>
        public event EventHandler Exiting;

        /// <summary>
        /// Occurs when a drawing frame is about to start.
        /// </summary>
        public event CancelEventHandler FrameStart;

        /// <summary>
        /// Occurs when a drawing frame ends.
        /// </summary>
        public event EventHandler FrameEnd;

        /// <summary>
        /// Gets or sets the inactive sleep time.
        /// </summary>
        /// <value>The inactive sleep time.</value>
        public TimeSpan InactiveSleepTime
        {
            get { return inactiveSleepTime; }
            set
            {
                // error checking
                if (value < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException("value", "Inactive sleep time cannot be less than zero.");
                inactiveSleepTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the target elapsed time.
        /// </summary>
        /// <value>The target elapsed time.</value>
        public TimeSpan TargetElapsedTime
        {
            get { return targetElapsedTime; }
            set
            {
                // error checking
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException("value", "Target elapsed time must be greater than zero.");
                targetElapsedTime = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the game is using a fixed time step.
        /// </summary>
        /// <value>
        /// <c>true</c> if the game is using a fixed time step; otherwise, <c>false</c>.
        /// </value>
        public bool IsFixedTimeStep
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Game"/> is exiting.
        /// </summary>
        /// <value><c>true</c> if exiting; otherwise, <c>false</c>.</value>
        public bool IsExiting
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the game window.
        /// </summary>
        /// <value>The game window.</value>
        public GameWindow Window
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the graphics device manager.
        /// </summary>
        /// <value>The graphics device manager.</value>
        public GraphicsDeviceManager GraphicsDeviceManager
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Game"/> is active.
        /// </summary>
        /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
        public bool IsActive
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the <see cref="Game"/> class.
        /// </summary>
        static Game()
        {
            // configure SlimDX
            Configuration.ThrowOnError = true;
            Configuration.AddResultWatch(ResultCode.DeviceLost, ResultWatchFlags.AlwaysIgnore);
            Configuration.AddResultWatch(ResultCode.WasStillDrawing, ResultWatchFlags.AlwaysIgnore);

#if DEBUG
            Configuration.DetectDoubleDispose = true;
            Configuration.EnableObjectTracking = true;
#else
            Configuration.DetectDoubleDispose = false;
            Configuration.EnableObjectTracking = false;
#endif

            // setup the application
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Game"/> class.
        /// </summary>
        protected Game()
        {
            IsFixedTimeStep = true;

            Window = new GameWindow();
            Window.ApplicationActivated += Window_ApplicationActivated;
            Window.ApplicationDeactivated += Window_ApplicationDeactivated;
            Window.Suspend += Window_Suspend;
            Window.Resume += Window_Resume;
            Window.Paint += Window_Paint;

            GraphicsDeviceManager = new GraphicsDeviceManager(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // GraphicsDeviceManager.Dispose will come around and call the Dispose(bool)
            // overload, so we don't need to do it here. It's convoluted, but it works well.
            if (GraphicsDeviceManager != null)
                GraphicsDeviceManager.Dispose();
            GraphicsDeviceManager = null;

            if (Disposed != null)
                Disposed(this, EventArgs.Empty);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Exits the game.
        /// </summary>
        public void Exit()
        {
            // request the game to terminate
            IsExiting = true;
        }

        /// <summary>
        /// Runs the game.
        /// </summary>
        public void Run()
        {
            IsRunning = true;

            try
            {
                gameTime.ElapsedGameTime = 0;
                gameTime.ElapsedRealTime = 0;
                gameTime.TotalGameTime = (float)totalGameTime.TotalSeconds;
                gameTime.TotalRealTime = (float)clock.CurrentTime.TotalSeconds;
                gameTime.IsRunningSlowly = false;

                Update(gameTime);

                Application.Idle += Application_Idle;
                Application.Run(Window);
            }
            finally
            {
                Application.Idle -= Application_Idle;
                IsRunning = false;
                OnExiting(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Performs one complete frame for the game.
        /// </summary>
        public void Tick()
        {
            // if we are exiting, do nothing
            if (IsExiting)
                return;

            // if we are inactive, sleep for a bit
            //if (!IsActive)
            //    Thread.Sleep((int)InactiveSleepTime.TotalMilliseconds);

            clock.Step();

            gameTime.TotalRealTime = (float)clock.CurrentTime.TotalSeconds;
            gameTime.ElapsedRealTime = (float)clock.ElapsedTime.TotalSeconds;
            lastFrameElapsedRealTime += clock.ElapsedTime;
            TimeSpan elapsedAdjustedTime = clock.ElapsedAdjustedTime;
            if (elapsedAdjustedTime < TimeSpan.Zero)
                elapsedAdjustedTime = TimeSpan.Zero;

            if (forceElapsedTimeToZero)
            {
                gameTime.ElapsedRealTime = 0;
                lastFrameElapsedRealTime = elapsedAdjustedTime = TimeSpan.Zero;
                forceElapsedTimeToZero = false;
            }

            // cap the adjusted time
            if (elapsedAdjustedTime > maximumElapsedTime)
                elapsedAdjustedTime = maximumElapsedTime;

            // check if we are using a fixed or variable time step
            if (IsFixedTimeStep)
            {
                accumulatedElapsedGameTime += elapsedAdjustedTime;
                long ratio = accumulatedElapsedGameTime.Ticks / TargetElapsedTime.Ticks;
                accumulatedElapsedGameTime = TimeSpan.FromTicks(accumulatedElapsedGameTime.Ticks % TargetElapsedTime.Ticks);
                lastFrameElapsedGameTime = TimeSpan.Zero;
                if (ratio == 0)
                    return;
                TimeSpan targetElapsedTime = TargetElapsedTime;

                if (ratio > 1)
                {
                    updatesSinceRunningSlowly2 = updatesSinceRunningSlowly1;
                    updatesSinceRunningSlowly1 = 0;
                }
                else
                {
                    if (updatesSinceRunningSlowly1 < int.MaxValue)
                        updatesSinceRunningSlowly1++;
                    if (updatesSinceRunningSlowly2 < int.MaxValue)
                        updatesSinceRunningSlowly2++;
                }

                drawRunningSlowly = updatesSinceRunningSlowly2 < 20;

                // update until it's time to draw the next frame
                while (ratio > 0 && !IsExiting)
                {
                    ratio -= 1;

                    try
                    {
                        gameTime.ElapsedGameTime = (float)targetElapsedTime.TotalSeconds;
                        gameTime.TotalGameTime = (float)totalGameTime.TotalSeconds;
                        gameTime.IsRunningSlowly = drawRunningSlowly;

                        Update(gameTime);
                    }
                    finally
                    {
                        lastFrameElapsedGameTime += targetElapsedTime;
                        totalGameTime += targetElapsedTime;
                    }
                }
            }
            else
            {
                drawRunningSlowly = false;
                updatesSinceRunningSlowly1 = int.MaxValue;
                updatesSinceRunningSlowly2 = int.MaxValue;

                // make sure we shouldn't be exiting
                if (!IsExiting)
                {
                    try
                    {
                        gameTime.ElapsedGameTime = 0;
                        lastFrameElapsedGameTime = elapsedAdjustedTime;
                        gameTime.TotalGameTime = (float)totalGameTime.TotalSeconds;
                        gameTime.IsRunningSlowly = false;

                        Update(gameTime);
                    }
                    finally
                    {
                        totalGameTime += elapsedAdjustedTime;
                    }
                }
            }

            DrawFrame();

            // refresh the FPS counter once per second
            lastUpdateFrame++;
            if ((float)clock.CurrentTime.TotalSeconds - lastUpdateTime > 1.0f)
            {
                gameTime.FramesPerSecond = (float)lastUpdateFrame / (float)(clock.CurrentTime.TotalSeconds - lastUpdateTime);
                lastUpdateTime = (float)clock.CurrentTime.TotalSeconds;
                lastUpdateFrame = 0;
            }
        }

        /// <summary>
        /// Resets the elapsed time.
        /// </summary>
        public void ResetElapsedTime()
        {
            forceElapsedTimeToZero = true;
            updatesSinceRunningSlowly1 = int.MaxValue;
            updatesSinceRunningSlowly2 = int.MaxValue;
        }

        /// <summary>
        /// Allows the game to perform logic processing.
        /// </summary>
        /// <param name="gameTime">The time passed since the last update.</param>
        protected virtual void Update(GameTime gameTime)
        {
        }

        /// <summary>
        /// Called when a frame is ready to be drawn.
        /// </summary>
        /// <param name="gameTime">The time passed since the last frame.</param>
        protected virtual void Draw(GameTime gameTime)
        {
        }

        /// <summary>
        /// Initializes the game.
        /// </summary>
        protected internal virtual void Initialize()
        {
        }

        /// <summary>
        /// Loads graphical resources.
        /// </summary>
        protected internal virtual void LoadContent()
        {
        }

        /// <summary>
        /// Unloads graphical resources.
        /// </summary>
        protected internal virtual void UnloadContent()
        {
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected internal virtual void Dispose(bool disposing)
        {

        }

        /// <summary>
        /// Raises the <see cref="E:Activated"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected virtual void OnActivated(EventArgs e)
        {
            if (Activated != null)
                Activated(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:Deactivated"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected virtual void OnDeactivated(EventArgs e)
        {
            if (Deactivated != null)
                Deactivated(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:Exiting"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected virtual void OnExiting(EventArgs e)
        {
            if (Exiting != null)
                Exiting(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:FrameStart"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        protected virtual void OnFrameStart(CancelEventArgs e)
        {
            if (FrameStart != null)
                FrameStart(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:FrameEnd"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected virtual void OnFrameEnd(EventArgs e)
        {
            if (FrameEnd != null)
                FrameEnd(this, e);
        }

        void DrawFrame()
        {
            try
            {
				if ( !IsExiting /* && !Window.IsMinimized */ )		// #28230 2012.5.1 yyagi
				{
                    CancelEventArgs e = new CancelEventArgs(false);
                    OnFrameStart(e);
                    if (!e.Cancel)
                    {
                        gameTime.TotalRealTime = (float)clock.CurrentTime.TotalSeconds;
                        gameTime.ElapsedRealTime = (float)lastFrameElapsedRealTime.TotalSeconds;
                        gameTime.TotalGameTime = (float)totalGameTime.TotalSeconds;
                        gameTime.ElapsedGameTime = (float)lastFrameElapsedGameTime.TotalSeconds;
                        gameTime.IsRunningSlowly = drawRunningSlowly;

                        Draw(gameTime);

                        OnFrameEnd(EventArgs.Empty);
                    }
                }
            }
            finally
            {
                lastFrameElapsedGameTime = TimeSpan.Zero;
                lastFrameElapsedRealTime = TimeSpan.Zero;
            }
        }

        void Application_Idle(object sender, EventArgs e)
        {
            NativeMessage message;
            while (!NativeMethods.PeekMessage(out message, IntPtr.Zero, 0, 0, 0))
            {
                if (IsExiting)
                    Window.Close();
                else
                    Tick();
            }
        }

        void Window_ApplicationDeactivated(object sender, EventArgs e)
        {
            if (IsActive)
            {
                IsActive = false;
                OnDeactivated(EventArgs.Empty);
            }
        }

        void Window_ApplicationActivated(object sender, EventArgs e)
        {
            if (!IsActive)
            {
                IsActive = true;
                OnActivated(EventArgs.Empty);
            }
        }

        void Window_Paint(object sender, PaintEventArgs e)
        {
            DrawFrame();
        }

        void Window_Resume(object sender, EventArgs e)
        {
            clock.Resume();
        }

        void Window_Suspend(object sender, EventArgs e)
        {
            clock.Suspend();
        }
    }
}
