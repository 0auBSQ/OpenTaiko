using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenCvSharp;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace FDK
{
    public class CVideo : IDisposable
    {
        public CVideo() 
        {
            vidCapture = null;
            Input = null;
            Frame = new Bitmap(1, 1);
            CurrentFrameNumber = -1;
            Loaded = false;
        }

        public CVideo(string fileInput)
        {
            try
            {
                vidCapture = new VideoCapture(fileInput);
            }
            catch (Exception e)
            {
                vidCapture = null;
                Trace.TraceWarning("(CVideo) Something went wrong while trying to open a video file located at {0}. More info : {1}", fileInput, e);
                return;
            }
            if (vidCapture.IsOpened()) { Trace.TraceWarning("The video file located at {0} was not opened. Did you use the correct directory?"); }

            Input = fileInput;
            Frame = new Bitmap(1, 1);
            CurrentFrameNumber = -1;

            Resolution = new System.Drawing.Size(vidCapture.FrameWidth, vidCapture.FrameHeight);

            Loaded = true;
        }

        //public void SetResolution(System.Drawing.Size size)
        //{
        //    if (!Loaded) return;

        //    Resolution = size;
        //}

        public void UpdateFrame(float timestamp)
        {
            if (!Loaded)
            {
                Frame = new Bitmap(1, 1);
                CurrentFrameNumber = -1;
                return;
            }

            if (CurrentFrameNumber == (int)(vidCapture.Fps * timestamp)) return; // Don't waste resources updating if the frame has not changed

            CurrentFrameNumber = (int)(vidCapture.Fps * timestamp);
            vidCapture.PosFrames = CurrentFrameNumber;

            var frameOutput = new Mat();
            if (!vidCapture.Read(frameOutput)) // Grab frame, stop here if no frame could be grabbed
            {
                Frame = new Bitmap(1, 1);
                CurrentFrameNumber = -1;
                return;
            }

            Frame = CVideoBitmap.ToBitmap(frameOutput);

            
            //

            //if (!CustomResolution.IsEmpty)
            //{
            //    Frame = new Bitmap(CustomResolution.Width, CustomResolution.Height);
            //    using (Graphics g = Graphics.FromImage(videoFrame))
            //    {
            //        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
            //        g.DrawImage(Frame, 0, 0, CustomResolution.Width, CustomResolution.Height);
            //    }
            //}
            //else
            //{
            //    Frame = videoFrame;
            //}
        }

        public override string ToString()
        {
            if (!Loaded) return "No Active Video";

            return string.Format("Input: {0}\nFrameWidth: {1}\nFrameHeight: {2}\nFrameCount:{3} \nCurrentFrame: {4}",
                Input,
                vidCapture.FrameWidth,
                vidCapture.FrameHeight,
                vidCapture.FrameCount,
                vidCapture.PosFrames);
        }

        //private CTexture txVideoFrame;
        public System.Drawing.Size Resolution { get; private set; }
        public bool Loaded { get; private set; }
        public string Input { get; private set; }
        public Bitmap Frame { get; private set; }
        public int CurrentFrameNumber { get; private set; }

        private VideoCapture vidCapture;

        #region Dispose
        private bool isDisposing;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposing)
            {
                if (disposing)
                {
                    if (this.vidCapture != null)
                    {
                        if (this.vidCapture.IsOpened())
                        {
                            vidCapture.Release();
                        }
                        if (!this.vidCapture.IsDisposed)
                        {
                            vidCapture.Dispose();
                        }
                        vidCapture = null;
                    }
                    if (Frame != null)
                    {
                        Frame.Dispose();
                        Frame = null;
                    }
                    
                }
                isDisposing = true;
            }
            Loaded = false;
            Input = null;
            CurrentFrameNumber = 0;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}