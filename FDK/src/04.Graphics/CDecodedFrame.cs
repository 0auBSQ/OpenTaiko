using System.Drawing;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace FDK {
	public class CDecodedFrame : IDisposable {
		public CDecodedFrame(Size texsize) {
			this.Using = false;
			this.TexSize = texsize;
			this.TexPointer = Marshal.AllocHGlobal(texsize.Width * TexSize.Height * 4);
		}

		public bool Using {
			get;
			private set;
		}
		public double Time {
			get;
			private set;
		}
		public IntPtr TexPointer {
			get;
			private set;
		}
		public Size TexSize {
			get;
			private set;
		}

		public unsafe CDecodedFrame UpdateFrame(double time, AVFrame* frame) {
			this.Time = time;
			Buffer.MemoryCopy(frame->data[0], (void*)this.TexPointer, frame->linesize[0] * frame->height, frame->linesize[0] * frame->height);
			this.Using = true;
			return this;
		}

		public void RemoveFrame() {
			this.Using = false;
		}

		public void Dispose() {
			this.Using = false;
			Marshal.FreeHGlobal(this.TexPointer);
		}
	}
}
