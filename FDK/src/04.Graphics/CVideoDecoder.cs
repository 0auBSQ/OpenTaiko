using System.Collections.Concurrent;
using System.Diagnostics;
using FFmpeg.AutoGen;
using Size = System.Drawing.Size;

namespace FDK;

/// <summary>
/// ビデオのデコードをするクラス
/// ファイル名・nullのCTextureをもらえれば、勝手に、CTextureに映像を格納して返す。
/// 演奏とは別のタイマーを使用しているので、ずれる可能性がある。
/// </summary>
public unsafe class CVideoDecoder : IDisposable {
	static CVideoDecoder() {
		// iOS links FFmpeg into the app as one framework (scripts/build-ffmpeg.sh), so resolve
		// symbols from the main image instead of dlopen'ing per-library dylibs from RootPath.
		if (OperatingSystem.IsIOS())
			ffmpeg.GetOrLoadLibrary = _ => System.Runtime.InteropServices.NativeLibrary.GetMainProgramHandle();
		// Android ships FFmpeg as unversioned lib*.so in the APK (scripts/download-ffmpeg.ps1), so
		// load by bare soname: the linker searches the app's native library directory and resolves
		// inter-library dependencies itself, bypassing the binding's versioned-name + RootPath scheme.
		if (OperatingSystem.IsAndroid())
			ffmpeg.GetOrLoadLibrary = name => System.Runtime.InteropServices.NativeLibrary.Load($"lib{name}.so");
	}

	public CVideoDecoder(string filename) {
		if (!File.Exists(filename))
			throw new FileNotFoundException(filename + " not found...");

		fixed (AVFormatContext** format_contexttmp = &format_context) {
			if (ffmpeg.avformat_open_input(format_contexttmp, filename, null, null) != 0)
				throw new FileLoadException("avformat_open_input failed\n");

			if (ffmpeg.avformat_find_stream_info(*format_contexttmp, null) < 0)
				throw new FileLoadException("avformat_find_stream_info failed\n");

			// find audio stream
			for (int i = 0; i < (int)format_context->nb_streams; i++) {
				if (format_context->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO) {
					video_stream = format_context->streams[i];
					break;
				}
			}
			if (video_stream == null)
				throw new FileLoadException("No video stream ...\n");

			// find decoder
			AVCodec* codec = ffmpeg.avcodec_find_decoder(video_stream->codecpar->codec_id);
			if (codec == null)
				throw new NotSupportedException("No supported decoder ...\n");

			codec_context = ffmpeg.avcodec_alloc_context3(codec);

			if (ffmpeg.avcodec_parameters_to_context(codec_context, video_stream->codecpar) < 0)
				Trace.WriteLine("avcodec_parameters_to_context failed\n");

			if (ffmpeg.avcodec_open2(codec_context, codec, null) != 0)
				Trace.WriteLine("avcodec_open2 failed\n");

			this.FrameSize = new Size(codec_context->width, codec_context->height);
			this.Duration = video_stream->nb_frames / (video_stream->avg_frame_rate.num / (double)video_stream->avg_frame_rate.den);
			this.Framerate = video_stream->avg_frame_rate;

			frameconv = new CFrameConverter(FrameSize, codec_context->pix_fmt);

			decodedframes = new ConcurrentQueue<CDecodedFrame>();

			for (int i = 0; i < framelist.Length; i++)
				framelist[i] = new CDecodedFrame(new Size(codec_context->width, codec_context->height));

			CTimer = new CTimer(CTimer.TimerType.MultiMedia);
		}
		Interlocked.Increment(ref LiveCount);
	}

	// Live-decoder gauge for the [MEMTRACE] debug line (FFmpeg contexts + frame buffers are large).
	public static int LiveCount;

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	// Finalizer fallback: if a decoder is ever collected without Dispose() — e.g. the constructor threw
	// after the FFmpeg contexts were allocated, or a caller forgot to dispose — still free the NATIVE
	// FFmpeg memory so it does not leak. Only native resources are touched from the finalizer; managed
	// and GL resources (frameconv, lastTexture, frames) are left to their own finalizers and must never
	// be freed from the finalizer thread.
	~CVideoDecoder() {
		Dispose(false);
	}

	protected virtual void Dispose(bool disposing) {
		if (this.close)
			return;
		this.close = true;
		Interlocked.Decrement(ref LiveCount);

		if (disposing) {
			bDrawing = false;
			cts?.Cancel();
			decodeStopped.Wait();
			frameconv?.Dispose();
		}

		// Native FFmpeg contexts (the large allocations from avformat_open_input / avcodec_alloc_context3).
		// Guarded for nulls so a constructor that threw part-way through can still be finalized safely.
		if (codec_context != null) {
			ffmpeg.avcodec_flush_buffers(codec_context);
			if (ffmpeg.avcodec_close(codec_context) < 0)
				Trace.TraceError("codec context close error.");
		}
		video_stream = null;
		fixed (AVFormatContext** format_contexttmp = &format_context) {
			if (*format_contexttmp != null)
				ffmpeg.avformat_close_input(format_contexttmp);
		}

		if (disposing) {
			if (lastTexture != null)
				lastTexture.Dispose();
			if (decodedframes != null)
				while (decodedframes.TryDequeue(out CDecodedFrame frame))
					frame.Dispose();
		}
	}

	public void Start() {
		CTimer.Reset();
		CTimer.Resume();
		this.bPlaying = true;
		bDrawing = true;

	}

	public void Pause() {
		CTimer.Pause();
		this.bPlaying = false;
	}

	public void Resume() {
		CTimer.Resume();
		this.bPlaying = true;
	}

	public void TogglePause() {
		if (this.bPlaying) {
			this.Pause();
		} else {
			this.Resume();
		}
	}

	public void Stop() {
		CTimer.Pause();
		this.bPlaying = false;
		bDrawing = false;
		this.bStreamEnded = true;
	}

	public void InitRead() {
		if (!bqueueinitialized) {
			this.Seek(0);
			bqueueinitialized = true;
		} else
			Trace.TraceError("The class has already been initialized.\n");
	}

	public void Seek(long timestampms) {
		this.bStreamEnded = false;
		cts?.Cancel();
		decodeStopped.Wait();
		if (ffmpeg.av_seek_frame(format_context, video_stream->index, timestampms, ffmpeg.AVSEEK_FLAG_BACKWARD) < 0)
			Trace.TraceError("av_seek_frame failed\n");
		ffmpeg.avcodec_flush_buffers(codec_context);
		CTimer.NowTimeMs = timestampms;
		cts?.Dispose();
		while (decodedframes.TryDequeue(out CDecodedFrame frame))
			frame.RemoveFrame();
		this.EnqueueFrames();
		if (lastTexture != null)
			lastTexture.Dispose();
		lastTexture = new CTexture(FrameSize.Width, FrameSize.Height);
	}

	public void GetNowFrame(ref CTexture Texture) {
		if (this.bPlaying && decodedframes.Count != 0) {
			CTimer.Update();
			if (decodedframes.TryPeek(out CDecodedFrame frame)) {
				while (frame.Time <= (CTimer.NowTimeMs * _dbPlaySpeed)) {
					if (decodedframes.TryDequeue(out CDecodedFrame cdecodedframe)) {

						if (decodedframes.Count != 0)
							if (decodedframes.TryPeek(out frame))
								if (frame.Time <= (CTimer.NowTimeMs * _dbPlaySpeed)) {
									cdecodedframe.RemoveFrame();
									continue;
								}

						lastTexture.UpdateTexture(cdecodedframe.TexPointer, cdecodedframe.TexSize.Width, cdecodedframe.TexSize.Height, Silk.NET.OpenGLES.PixelFormat.Rgba);

						cdecodedframe.RemoveFrame();
					}
					break;
				}
			}

			if (DS == DecodingState.Stopped)
				this.EnqueueFrames();
		}

		if (lastTexture == null)
			lastTexture = new CTexture(FrameSize.Width, FrameSize.Height);

		if (Texture == lastTexture)
			return;

		Texture = lastTexture;

	}

	private void EnqueueFrames() {
		if (DS != DecodingState.Running && !close) {
			cts = new CancellationTokenSource();
			decodeStopped.Reset();
			// LongRunning → dedicated thread, not a thread-pool worker. EnqueueOneFrame is a loop that lives
			// for the whole video (it Thread.Sleep(1)s while the frame queue is full); on the pool it would
			// occupy a worker for minutes and make the pool inject/retire extra threads (the "[thread]
			// exited with code 0" churn seen in-game) and risk starving other pooled work.
			Task.Factory.StartNew(() => EnqueueOneFrame(), TaskCreationOptions.LongRunning);
		}
	}

	private void EnqueueOneFrame() {
		DS = DecodingState.Running;
		AVFrame* frame = ffmpeg.av_frame_alloc();
		AVPacket* packet = ffmpeg.av_packet_alloc();
		try {
			while (true) {
				if (cts.IsCancellationRequested || close)
					return;

				//2020/10/27 Mr-Ojii 閾値フレームごとにパケット生成するのは無駄だと感じたので、ループに入ったら、パケット生成し、シークによるキャンセルまたは、EOFまで無限ループ
				if (decodedframes.Count < framelist.Length - 1)//-1をして、余裕を持たせておく。
				{
					int error = ffmpeg.av_read_frame(format_context, packet);

					if (error >= 0) {
						if (packet->stream_index == video_stream->index) {
							if (ffmpeg.avcodec_send_packet(codec_context, packet) >= 0) {
								if (ffmpeg.avcodec_receive_frame(codec_context, frame) == 0) {
									AVFrame* outframe = null;

									outframe = frameconv.Convert(frame);

									decodedframes.Enqueue(PickUnusedDcodedFrame().UpdateFrame((outframe->best_effort_timestamp - video_stream->start_time) * ((double)video_stream->time_base.num / (double)video_stream->time_base.den) * 1000, outframe));

									ffmpeg.av_frame_unref(frame);
									ffmpeg.av_frame_unref(outframe);
									ffmpeg.av_frame_free(&outframe);
								}
							}
						}

						//2020/10/27 Mr-Ojii packetが解放されない周回があった問題を修正。
						ffmpeg.av_packet_unref(packet);
					} else if (error == ffmpeg.AVERROR_EOF) {
						this.bStreamEnded = true;
						return;
					} else {
						// Treat any other read error as the end of the stream.
						Trace.TraceError($"av_read_frame failed ({error}); stopping decode.");
						this.bStreamEnded = true;
						return;
					}
				} else {
					//ポーズ中に無限ループに入り、CPU使用率が異常に高くなってしまうため、1ms待つ。
					//ネットを調べると、await Task.Delay()を使えというお話が出てくるが、unsafeなので、使えない
					Thread.Sleep(1);
				}
			}
		} catch (Exception e) {
			Trace.TraceError(e.ToString());
		} finally {
			ffmpeg.av_packet_free(&packet);
			ffmpeg.av_frame_unref(frame);
			ffmpeg.av_free(frame);
			DS = DecodingState.Stopped;
			decodeStopped.Set();
		}
	}

	public CDecodedFrame PickUnusedDcodedFrame() {
		for (int i = 0; i < framelist.Length; i++) {
			if (framelist[i].Using == false) {
				return framelist[i];
			}
		}
		return null;
	}

	public double msPlayPosition => CTimer.NowTimeMs * _dbPlaySpeed;

	public Size FrameSize {
		get;
		private set;
	}
	public double Duration {
		get;
		private set;
	}

	public double dbPlaySpeed {
		get {
			return this._dbPlaySpeed;
		}
		set {
			if (value > 0) {
				this._dbPlaySpeed = value;
			} else {
				throw new ArgumentOutOfRangeException();
			}
		}
	}

	#region[private]
	//for read & decode
	private bool close = false;
	private double _dbPlaySpeed = 1.0;
	private AVFormatContext* format_context;
	private AVStream* video_stream;
	private AVCodecContext* codec_context;
	private ConcurrentQueue<CDecodedFrame> decodedframes;
	private CancellationTokenSource cts;
	private CDecodedFrame[] framelist = new CDecodedFrame[6];
	private DecodingState DS = DecodingState.Stopped;
	// Set while the decode thread is stopped; lets Dispose/Seek wait for it without polling.
	private readonly ManualResetEventSlim decodeStopped = new ManualResetEventSlim(true);
	private enum DecodingState {
		Stopped,
		Running
	}

	//for play
	public bool bPlaying { get; private set; } = false;
	public bool bDrawing { get; private set; } = false;
	// Reader reached the end of the stream (or Stop was called); frames may still be queued.
	private bool bStreamEnded = false;
	// End of playback (stream fully read and all frames shown), unlike msPlayPosition which can stall.
	public bool IsFinishedPlaying => bStreamEnded && decodedframes.IsEmpty;
	private CTimer CTimer;
	private AVRational Framerate;
	private CTexture lastTexture;
	private bool bqueueinitialized = false;

	//for convert
	private CFrameConverter frameconv;
	#endregion
}
