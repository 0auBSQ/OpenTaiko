using System.Drawing;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace FDK;

public class CDecodedFrame : IDisposable {
	public CDecodedFrame(Size texsize) {
		this.Using = false;
		this.TexSize = texsize;
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

	private unsafe AVFrame* _frame = ffmpeg.av_frame_alloc();
	internal unsafe AVFrame* GetEmptyFrame() {
		this.RemoveFrame();
		this.Using = true;
		return _frame;
	}

	// the converted picture of this slot, allocated on first use
	private IntPtr _buffer;
	internal IntPtr GetConvertBuffer(int size) {
		if (this._buffer == 0) this._buffer = Marshal.AllocHGlobal(size);
		return this._buffer;
	}

	public unsafe void UpdateFrame(double time) {
		this.Time = time;
		this.TexPointer = (IntPtr)this._frame->data[0];
		this.Using = true;
	}

	public unsafe void RemoveFrame() {
		ffmpeg.av_frame_unref(this._frame);
		this.TexPointer = 0;
		this.Using = false;
	}

	public unsafe void Dispose() {
		if (this._frame != null) {
			fixed (AVFrame** pThisFrame = &this._frame) {
				ffmpeg.av_frame_unref(*pThisFrame);
				ffmpeg.av_frame_free(pThisFrame);
			}
			this._frame = null;
		}
		if (this._buffer != 0) {
			Marshal.FreeHGlobal(this._buffer);
			this._buffer = 0;
		}
		this.TexPointer = 0;
		this.Using = false;
	}
}
