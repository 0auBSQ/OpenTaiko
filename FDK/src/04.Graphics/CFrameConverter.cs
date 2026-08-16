using System.Drawing;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;


namespace FDK;

public unsafe class CFrameConverter : IDisposable {
	public CFrameConverter(Size FrameSize, AVPixelFormat pix_fmt) {
		this.FrameSize = FrameSize;
		if (pix_fmt != CVPxfmt) {
			convert_context = ffmpeg.sws_getContext(
				FrameSize.Width,
				FrameSize.Height,
				pix_fmt,
				FrameSize.Width,
				FrameSize.Height,
				CVPxfmt,
				ffmpeg.SWS_FAST_BILINEAR, null, null, null);
			this.IsConvert = true;
			if (convert_context == null) throw new ApplicationException("Could not initialize the conversion context.\n");
		}
		_convertedFrameBufferPtr = Marshal.AllocHGlobal(ffmpeg.av_image_get_buffer_size(CVPxfmt, FrameSize.Width, FrameSize.Height, 1));

		_dstData = new byte_ptrArray4();
		_dstLinesize = new int_array4();
		ffmpeg.av_image_fill_arrays(ref _dstData, ref _dstLinesize, (byte*)_convertedFrameBufferPtr, CVPxfmt, FrameSize.Width, FrameSize.Height, 1);
	}

	public void Convert(AVFrame* framep) {
		if (!this.IsConvert)
			return;

		var timestamp = framep->best_effort_timestamp;
		ffmpeg.sws_scale(convert_context, framep->data, framep->linesize, 0, framep->height, _dstData, _dstLinesize);
		ffmpeg.av_frame_unref(framep);

		framep->best_effort_timestamp = timestamp;
		framep->width = FrameSize.Width;
		framep->height = FrameSize.Height;
		framep->data = new byte_ptrArray8();
		framep->data.UpdateFrom(_dstData);
		framep->linesize = new int_array8();
		framep->linesize.UpdateFrom(_dstLinesize);
	}

	public void Dispose() {
		if (_convertedFrameBufferPtr != 0) {
			Marshal.FreeHGlobal(_convertedFrameBufferPtr);
			_convertedFrameBufferPtr = 0;
		}
		if (convert_context != null) {
			ffmpeg.sws_freeContext(convert_context);
			this.convert_context = null;
		}
	}

	private SwsContext* convert_context;
	private readonly byte_ptrArray4 _dstData;
	private readonly int_array4 _dstLinesize;
	private IntPtr _convertedFrameBufferPtr;
	private const AVPixelFormat CVPxfmt = AVPixelFormat.AV_PIX_FMT_RGBA;
	private bool IsConvert = false;
	private Size FrameSize;
}
