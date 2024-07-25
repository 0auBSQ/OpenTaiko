using System.Drawing;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;


namespace FDK {
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

		public AVFrame* Convert(AVFrame* framep) {
			if (this.IsConvert) {
				ffmpeg.sws_scale(convert_context, framep->data, framep->linesize, 0, framep->height, _dstData, _dstLinesize);

				AVFrame* tmp = ffmpeg.av_frame_alloc();
				tmp = ffmpeg.av_frame_alloc();
				tmp->best_effort_timestamp = framep->best_effort_timestamp;
				tmp->width = FrameSize.Width;
				tmp->height = FrameSize.Height;
				tmp->data = new byte_ptrArray8();
				tmp->data.UpdateFrom(_dstData);
				tmp->linesize = new int_array8();
				tmp->linesize.UpdateFrom(_dstLinesize);

				ffmpeg.av_frame_unref(framep);
				return tmp;
			} else {
				return framep;
			}
		}

		public void Dispose() {
			Marshal.FreeHGlobal(_convertedFrameBufferPtr);
			ffmpeg.sws_freeContext(convert_context);
		}

		private SwsContext* convert_context;
		private readonly byte_ptrArray4 _dstData;
		private readonly int_array4 _dstLinesize;
		private readonly IntPtr _convertedFrameBufferPtr;
		private const AVPixelFormat CVPxfmt = AVPixelFormat.AV_PIX_FMT_RGBA;
		private bool IsConvert = false;
		private Size FrameSize;
	}
}
