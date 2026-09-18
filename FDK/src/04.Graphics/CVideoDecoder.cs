using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using ManagedBass;
using Size = System.Drawing.Size;

namespace FDK;

/// <summary>
/// ビデオのデコードをするクラス
/// ファイル名・nullのCTextureをもらえれば、勝手に、CTextureに映像を格納して返す。
/// 演奏とは別のタイマーを使用しているので、ずれる可能性がある。
/// With an audio factory given, the file's audio track is decoded too: its samples wait in a ring buffer
/// that one BASS user stream, made by the factory around the decoder's stream procedure, fetches from for
/// the life of the video. The frames are then clocked by what that stream has played instead of the
/// timer, so picture and sound start together and cannot drift apart. A seek flushes the ring and the
/// clock restarts from the target; the speed is applied live on the same stream.
/// </summary>
public unsafe class CVideoDecoder : IDisposable {
	public delegate CSound AudioFactory(int frequency, int channels, double durationSeconds, StreamProcedure proc);

	// Decoded audio between the reader and the stream procedure. Frames are interleaved floats; the reader
	// waits when it is full and the procedure hands out what it has, or nothing (a stall) when it is empty.
	private sealed class AudioRing {
		private readonly float[] buf;
		private readonly int channels;
		private int head, count;   // in floats
		private readonly object gate = new();
		private readonly ManualResetEventSlim spaceFreed = new(true);
		public readonly int Rate;
		public long DeliveredFrames { get; private set; }   // handed to BASS since the last flush
		public bool Ended;

		public AudioRing(int rate, int channels, double seconds) {
			this.Rate = rate;
			this.channels = channels;
			this.buf = new float[(int)(rate * seconds) * channels];
		}

		public double QueuedMs { get { lock (this.gate) return this.count / (double)this.channels * 1000.0 / this.Rate; } }

		// the stream procedure: as many whole frames as fit, nothing when empty
		public int Read(IntPtr dst, int bytes) {
			lock (this.gate) {
				int floats = Math.Min(bytes / sizeof(float), this.count);
				floats -= floats % this.channels;
				if (floats <= 0) return 0;
				int first = Math.Min(floats, this.buf.Length - this.head);
				Marshal.Copy(this.buf, this.head, dst, first);
				if (floats > first) Marshal.Copy(this.buf, 0, dst + first * sizeof(float), floats - first);
				this.head = (this.head + floats) % this.buf.Length;
				this.count -= floats;
				this.DeliveredFrames += floats / this.channels;
				this.spaceFreed.Set();
				return floats * sizeof(float);
			}
		}

		// the reader: waits for room rather than dropping samples
		public void Write(IntPtr src, int floats, CancellationToken ct) {
			int done = 0;
			while (done < floats) {
				int written;
				lock (this.gate) {
					int room = Math.Min(floats - done, this.buf.Length - this.count);
					if (room > 0) {
						int tail = (this.head + this.count) % this.buf.Length;
						int first = Math.Min(room, this.buf.Length - tail);
						Marshal.Copy(src + done * sizeof(float), this.buf, tail, first);
						if (room > first) Marshal.Copy(src + (done + first) * sizeof(float), this.buf, 0, room - first);
						this.count += room;
					}
					written = room;
					if (written == 0) this.spaceFreed.Reset();
				}
				done += written;
				if (written == 0) this.spaceFreed.Wait(ct);
			}
		}

		public void Flush() {
			lock (this.gate) {
				this.head = this.count = 0;
				this.DeliveredFrames = 0;
				this.Ended = false;
				this.spaceFreed.Set();
			}
		}
	}
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

	public CVideoDecoder(string filename) : this(filename, null) { }

	public CVideoDecoder(string filename, AudioFactory? audioOut) {
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

			for (int i = 0; i < framelist.Length; i++)
				framelist[i] = new CDecodedFrame(new Size(codec_context->width, codec_context->height));
			framelistHead = framelistTail = 0;

			CTimer = new CTimer(CTimer.TimerType.GameTimeAtDraw);

			if (audioOut != null) this.OpenAudio(audioOut);
		}
		Interlocked.Increment(ref LiveCount);
	}

	// the first audio stream, decoded to interleaved float (surround folded to stereo) and pushed into a
	// sound of the music group so the volume settings apply to it
	private void OpenAudio(AudioFactory audioOut) {
		for (int i = 0; i < (int)format_context->nb_streams; i++) {
			if (format_context->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO) {
				audio_stream = format_context->streams[i];
				break;
			}
		}
		if (audio_stream == null) return;

		AVCodec* codec = ffmpeg.avcodec_find_decoder(audio_stream->codecpar->codec_id);
		if (codec == null) { audio_stream = null; Trace.TraceWarning("No decoder for the video's audio track."); return; }
		audio_codec_context = ffmpeg.avcodec_alloc_context3(codec);
		if (ffmpeg.avcodec_parameters_to_context(audio_codec_context, audio_stream->codecpar) < 0
			|| ffmpeg.avcodec_open2(audio_codec_context, codec, null) != 0) {
			Trace.TraceWarning("The video's audio track could not be opened.");
			this.CloseAudio();
			return;
		}

		audioRate = audio_codec_context->sample_rate;
		audioChannels = Math.Min(2, audio_codec_context->ch_layout.nb_channels);
		if (audioRate <= 0 || audioChannels <= 0) { this.CloseAudio(); return; }

		AVChannelLayout outLayout;
		ffmpeg.av_channel_layout_default(&outLayout, audioChannels);
		AVChannelLayout inLayout = audio_codec_context->ch_layout;
		SwrContext* s = null;
		if (ffmpeg.swr_alloc_set_opts2(&s, &outLayout, AVSampleFormat.AV_SAMPLE_FMT_FLT, audioRate,
				&inLayout, audio_codec_context->sample_fmt, audio_codec_context->sample_rate, 0, null) < 0
			|| ffmpeg.swr_init(s) < 0) {
			Trace.TraceWarning("The video's audio track could not be converted.");
			if (s != null) ffmpeg.swr_free(&s);
			this.CloseAudio();
			return;
		}
		swr = s;
		audioOutCapacity = 8192;
		audioOut_ = (byte*)ffmpeg.av_malloc((ulong)(audioOutCapacity * audioChannels * sizeof(float)));
		audioFrame = ffmpeg.av_frame_alloc();

		try {
			this.audioRing = new AudioRing(audioRate, audioChannels, AudioRingSeconds);
			this.audioProc = this.AudioStreamProc;
			this.audio = audioOut(audioRate, audioChannels, this.Duration, this.audioProc);
		} catch (Exception e) {
			Trace.TraceWarning("The video's audio track has no output: " + e.Message);
			this.CloseAudio();
		}
	}

	// BASS asks for samples on its mixing thread
	private int AudioStreamProc(int handle, IntPtr buffer, int length, IntPtr user) {
		var ring = this.audioRing;
		return ring == null ? 0 : ring.Read(buffer, length);
	}

	private void CloseAudio() {
		this.audio?.Dispose();
		this.audio = null;
		this.audioRing = null;
		this.audioProc = null;
		if (swr != null) { var s = swr; ffmpeg.swr_free(&s); swr = null; }
		if (audioFrame != null) { var f = audioFrame; ffmpeg.av_frame_free(&f); audioFrame = null; }
		if (audioOut_ != null) { ffmpeg.av_free(audioOut_); audioOut_ = null; }
		if (audio_codec_context != null) {
			var c = audio_codec_context;
			ffmpeg.avcodec_free_context(&c);
			audio_codec_context = null;
		}
		audio_stream = null;
	}

	/// <summary>The audio track's sound while one plays, for volume control; null without audio.</summary>
	public CSound? Audio => this.audio;
	public bool HasAudio => this.audio != null;

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
			this.StopEnqueuingFrames();
			frameconv?.Dispose();
			this.CloseAudio();
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
			foreach (var frame in framelist) {
				frame.Dispose();
				this.canEnqueueFrame.Set();
			}
			framelistHead = framelistTail = 0;
		}
	}

	public void Start() {
		if (this.bStreamEnded) this.Seek(0);   // after Stop or the end: from the top again
		CTimer.Reset();
		CTimer.Resume();
		this.audio?.PlayStart();
		this.bPlaying = true;
		bDrawing = true;

	}

	public void Pause() {
		CTimer.Pause();
		this.audio?.Pause();
		this.bPlaying = false;
	}

	public void Resume() {
		CTimer.Resume();
		this.audio?.Resume(false);
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
		this.audio?.tStop();
		this.bPlaying = false;
		bDrawing = false;
		this.IsFinishedPlaying = true;
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
		this.StopEnqueuingFrames();
		// the demuxer takes stream ticks: the keyframe at or before the target, from which the decoder
		// runs forward and drops what lies before it
		long ticks = ffmpeg.av_rescale_q(timestampms, new AVRational { num = 1, den = 1000 }, video_stream->time_base);
		if (video_stream->start_time != ffmpeg.AV_NOPTS_VALUE) ticks += video_stream->start_time;
		if (ffmpeg.av_seek_frame(format_context, video_stream->index, ticks, ffmpeg.AVSEEK_FLAG_BACKWARD) < 0)
			Trace.TraceError("av_seek_frame failed\n");
		ffmpeg.avcodec_flush_buffers(codec_context);
		CTimer.NowTimeMs = timestampms;
		if (this.audio != null) {
			// the ring starts over at the target; the mixer's own buffer of old samples is dropped when BASS
			// allows it, and counted as still playing otherwise, so the clock holds at the target until the
			// new samples are heard
			ffmpeg.avcodec_flush_buffers(audio_codec_context);
			this.audioRing!.Flush();
			this.audio.UserStreamFlush();
			audioBaseMs = timestampms;
		}
		foreach (var frame in framelist) {
			frame.RemoveFrame();
			this.canEnqueueFrame.Set();
		}
		framelistHead = framelistTail = 0;
		this.presentAfterSeek = true;   // the picture at the new time shows even while paused
		this.EnsureEnqueuingFrames();
		// callers draw the texture right after GetNowFrame, so there is always one: the last picture stays
		// up until the frame at the new time replaces it
		lastTexture ??= new CTexture(FrameSize.Width, FrameSize.Height);
	}

	// set by a seek: the next decoded frame is shown at once, even while paused, the way a player scrubs
	private bool presentAfterSeek;

	public void GetNowFrame(ref CTexture Texture) {
		if ((this.bPlaying || this.presentAfterSeek) && framelist[framelistHead].Using) {
			if (this.bPlaying) CTimer.Update();
			(int idx, CDecodedFrame frame)? nowFrame = null;
			for ((int idx, CDecodedFrame frame)? next;
				(next = this.FirstUsedFrame()) != null && next.Value.frame.TexPointer != 0 && next.Value.frame.Time <= this.msPlayPosition;
				) {
				if (nowFrame != null)
					this.RemoveFrameAt(nowFrame.Value.idx);
				this.PopFrameAt(next.Value.idx);
				nowFrame = next;
			}
			if (nowFrame == null && this.presentAfterSeek) {
				// paused past the target: the first frame decoded beyond it is the one to show
				var first = this.FirstUsedFrame();
				if (first != null && first.Value.frame.TexPointer != 0) {
					this.PopFrameAt(first.Value.idx);
					nowFrame = first;
				}
			}
			if (nowFrame != null) {
				// a paused seek keeps showing newer frames until the one at the target (within a frame) is up,
				// since the decoder starts at the keyframe before it and works forward
				if (this.presentAfterSeek) {
					double frameMs = Framerate.num > 0 ? 1000.0 * Framerate.den / Framerate.num : 1000.0 / 30;
					if (nowFrame.Value.frame.Time + frameMs > this.msPlayPosition) this.presentAfterSeek = false;
				}
				var frame = nowFrame.Value.frame;
				lastTexture ??= new CTexture(FrameSize.Width, FrameSize.Height);
				lastTexture.UpdateTexture(frame.TexPointer, frame.TexSize.Width, frame.TexSize.Height, Silk.NET.OpenGLES.PixelFormat.Rgba);
				this.RemoveFrameAt(nowFrame.Value.idx);
			}
			this.EnsureEnqueuingFrames();
		}

		if (Texture == lastTexture)
			return;

		Texture?.Dispose();
		Texture = lastTexture;

	}

	private void EnsureEnqueuingFrames() {
		if (decodeStopped.IsSet && !close) {
			this.StopEnqueuingFrames();
			cts = new CancellationTokenSource();
			decodeStopped.Reset();
			// LongRunning → dedicated thread, not a thread-pool worker. EnqueueFrames is a loop that lives
			// for the whole video (it canEnqueueFrame.Wait() while the frame queue is full); on the pool it would
			// occupy a worker for minutes and make the pool inject/retire extra threads (the "[thread]
			// exited with code 0" churn seen in-game) and risk starving other pooled work.
			Task.Factory.StartNew(() => EnqueueFrames(), TaskCreationOptions.LongRunning);
		}
	}
	private void StopEnqueuingFrames() {
		if (this.cts != null) {
			this.cts.Cancel();
			this.decodeStopped.Wait();
			cts.Dispose();
			cts = null;
		}
	}

	// Video packets read while every frame slot is taken and the sound still needs data. Compressed, so a
	// long run of video packets muxed ahead of the audio costs little; without this the reader would block
	// on the full frame queue while the picture waits for an audio clock that has nothing to play.
	private readonly Queue<IntPtr> pendingVideo = new();
	private const int MaxPendingVideoPackets = 256;
	private const double AudioLeadMs = 600;

	private void EnqueueFrames() {
		decodeStopped.Reset();
		AVPacket* packet = ffmpeg.av_packet_alloc();
		try {
			bool eof = false;
			while (true) {
				if (cts!.IsCancellationRequested || close)
					return;

				// stashed video packets go first, as slots free up
				while (pendingVideo.Count > 0) {
					var slot = this.PickUnusedDecodedFrame();
					if (slot == null) break;
					AVPacket* p = (AVPacket*)pendingVideo.Dequeue();
					this.DecodeVideoPacket(p, slot.Value);
					ffmpeg.av_packet_free(&p);
				}

				if (eof) {
					if (pendingVideo.Count == 0) {
						if (this.audioRing != null) this.audioRing.Ended = true;
						this.bStreamEnded = true;
						return;
					}
					this.WaitForFrameSlot();
					continue;
				}
				if (pendingVideo.Count >= MaxPendingVideoPackets) {
					this.WaitForFrameSlot();
					continue;
				}

				int error = ffmpeg.av_read_frame(format_context, packet);
				if (error < 0) {
					if (error != ffmpeg.AVERROR_EOF) {
						// Treat any other read error as the end of the stream.
						Trace.TraceError($"av_read_frame failed ({error}); stopping decode.");
					}
					eof = true;
					continue;
				}
				try {
					if (this.audio != null && packet->stream_index == audio_stream->index) {
						this.PushAudioPacket(packet);
						continue;
					}
					if (packet->stream_index != video_stream->index)
						continue;

					// anything stashed goes first, so a new packet never overtakes it and frames stay in order
					if (pendingVideo.Count > 0) {
						pendingVideo.Enqueue((IntPtr)ffmpeg.av_packet_clone(packet));
						continue;
					}
					var slot = this.PickUnusedDecodedFrame();
					if (slot == null) {
						if (this.audioRing != null && this.audioRing.QueuedMs < AudioLeadMs) {
							pendingVideo.Enqueue((IntPtr)ffmpeg.av_packet_clone(packet));
							continue;
						}
						while ((slot = this.PickUnusedDecodedFrame()) == null) {
							this.WaitForFrameSlot();
							if (cts!.IsCancellationRequested || close)
								return;
						}
					}
					this.DecodeVideoPacket(packet, slot.Value);
				} finally {
					//2020/10/27 Mr-Ojii packetが解放されない周回があった問題を修正。
					ffmpeg.av_packet_unref(packet);
				}
			}
		} catch (OperationCanceledException) {
			// canceled requested
		} catch (Exception e) {
			Trace.TraceError(e.ToString());
		} finally {
			ffmpeg.av_packet_free(&packet);
			this.ClearPendingVideo();
			decodeStopped.Set();
		}
	}

	private void WaitForFrameSlot() {
		// the sound has run dry while every slot holds a frame the clock has not reached: the clock cannot
		// move until the sound gets data, and the sound cannot get data until a slot frees. Drop the oldest
		// frame so the reader can go on; the picture skips instead of freezing with the sound.
		if (this.audioRing != null && !this.bStreamEnded && this.audioRing.QueuedMs <= 0) {
			var head = this.FirstUsedFrame();
			if (head != null) {
				Trace.TraceWarning("Video audio starved with a full frame queue; dropping a frame to keep the sound fed.");
				this.RemoveFrameAt(head.Value.idx);
				return;
			}
		}
		canEnqueueFrame.Reset();
		canEnqueueFrame.Wait(cts!.Token);
	}

	private void ClearPendingVideo() {
		while (pendingVideo.Count > 0) {
			AVPacket* p = (AVPacket*)pendingVideo.Dequeue();
			ffmpeg.av_packet_free(&p);
		}
	}

	// decodes one video packet into a reserved slot; the slot is given back when no frame comes out
	private void DecodeVideoPacket(AVPacket* packet, (int idx, CDecodedFrame frame) slot) {
		if (ffmpeg.avcodec_send_packet(codec_context, packet) < 0) {
			this.UnpickDecodedFrameAt(slot.idx);
			return;
		}
		var frame = slot.frame.GetEmptyFrame();
		if (ffmpeg.avcodec_receive_frame(codec_context, frame) != 0) {
			this.UnpickDecodedFrameAt(slot.idx);
			return;
		}
		frameconv.Convert(frame);

		double msVideoTime = (frame->best_effort_timestamp - video_stream->start_time) * (video_stream->time_base.num / (double)video_stream->time_base.den) * 1000;
		if (msVideoTime < this.msPlayPosition) { // evict non-empty outdated frames
			for ((int idx, CDecodedFrame frame)? f; (f = this.FirstUsedFrame()) != null && f.Value.idx != slot.idx;)
				this.RemoveFrameAt(f.Value.idx);
		}
		slot.frame.UpdateFrame(msVideoTime);
	}

	// decodes one audio packet and queues its samples; those before a seek target are dropped
	private void PushAudioPacket(AVPacket* packet) {
		if (ffmpeg.avcodec_send_packet(audio_codec_context, packet) < 0) return;
		while (ffmpeg.avcodec_receive_frame(audio_codec_context, audioFrame) == 0) {
			long start = audio_stream->start_time == ffmpeg.AV_NOPTS_VALUE ? 0 : audio_stream->start_time;
			double msAudio = (audioFrame->best_effort_timestamp - start) * (audio_stream->time_base.num / (double)audio_stream->time_base.den) * 1000;
			double msFrame = audioFrame->nb_samples * 1000.0 / Math.Max(1, audioFrame->sample_rate);
			if (msAudio + msFrame < audioBaseMs) continue;
			if (audioFrame->nb_samples > audioOutCapacity) {
				ffmpeg.av_free(audioOut_);
				audioOutCapacity = audioFrame->nb_samples;
				audioOut_ = (byte*)ffmpeg.av_malloc((ulong)(audioOutCapacity * audioChannels * sizeof(float)));
			}
			byte* outBuf = audioOut_;
			int samples = ffmpeg.swr_convert(swr, &outBuf, audioOutCapacity, audioFrame->extended_data, audioFrame->nb_samples);
			if (samples > 0) this.audioRing!.Write((IntPtr)audioOut_, samples * audioChannels, cts!.Token);
		}
	}

	public (int idx, CDecodedFrame frame)? PickUnusedDecodedFrame() {
		var idx = framelistTail;
		if (framelist[idx].Using)
			return null;
		Interlocked.CompareExchange(ref framelistTail, (idx + 1) % framelist.Length, idx);
		return (idx, framelist[idx]);
	}

	public void UnpickDecodedFrameAt(int idx) {
		framelist[idx].RemoveFrame();
		int tail = framelistTail;
		int beforeTail = (tail + (framelist.Length - 1)) % framelist.Length;
		if (idx == beforeTail)
			Interlocked.CompareExchange(ref framelistTail, idx, tail);
	}

	private (int idx, CDecodedFrame frame)? FirstUsedFrame() {
		for (int i = 0; i < framelist.Length; ++i) {
			int idx = (framelistHead + i) % framelist.Length;
			var frame = framelist[idx];
			if (frame.Using)
				return (idx, framelist[idx]);
			if (idx == framelistTail)
				break;
			this.PopFrameAt(idx);
		}
		return null;
	}

	private void RemoveFrameAt(int idx) {
		framelist[idx].RemoveFrame();
		this.PopFrameAt(idx);
	}

	private void PopFrameAt(int idx) {
		Interlocked.CompareExchange(ref framelistHead, (idx + 1) % framelist.Length, idx);
		this.canEnqueueFrame.Set();
	}

	// with an audio track the clock is what has actually been heard: the frames the stream procedure handed
	// out, less what the mixer still holds, from the last seek target
	public double msPlayPosition {
		get {
			if (this.audio == null || this.audioRing == null) return CTimer.NowTimeMs_Double * _dbPlaySpeed;
			long bytesPerFrame = audioChannels * sizeof(float);
			double heardFrames = this.audioRing.DeliveredFrames - this.audio.UserStreamBufferedBytes / (double)bytesPerFrame;
			return audioBaseMs + Math.Max(0.0, heardFrames) * 1000.0 / audioRate;
		}
	}

	/// <summary>Decoded audio waiting to be played, in milliseconds (0 without an audio track).</summary>
	public double AudioQueuedMs => this.audioRing?.QueuedMs ?? 0.0;

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
				if (value == this._dbPlaySpeed) return;
				this._dbPlaySpeed = value;
				this.audio?.SetSpeedLive(value);   // on the stream already in the mixer; the clock is in media time either way
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
	private CancellationTokenSource? cts;
	private CDecodedFrame[] framelist = new CDecodedFrame[6];
	private int framelistHead = 0;
	private int framelistTail = 0;
	// Set while the decode thread is stopped; lets Dispose/Seek wait for it without polling.
	private readonly ManualResetEventSlim decodeStopped = new ManualResetEventSlim(true);
	private readonly ManualResetEventSlim canEnqueueFrame = new ManualResetEventSlim(true);

	//for play
	public bool bPlaying { get; private set; } = false;
	public bool bDrawing { get; private set; } = false;
	// Reader reached the end of the stream (or Stop was called); frames may still be queued.
	private bool bStreamEnded = false;
	// End of playback (stream fully read and all frames shown), unlike msPlayPosition which can stall.
	public bool IsFinishedPlaying {
		get => bStreamEnded && !framelist[framelistHead].Using;
		private set {
			this.bStreamEnded = value;
			if (value) {
				this.StopEnqueuingFrames();
				for (CDecodedFrame frame; (frame = framelist[framelistHead]).Using; framelistHead = (framelistHead + 1) % framelist.Length) {
					frame.RemoveFrame();
					this.canEnqueueFrame.Set();
				}
				framelistHead = framelistTail = 0;
			}
		}
	}
	private CTimer CTimer;
	private AVRational Framerate;

	//for the audio track
	private AVStream* audio_stream;
	private AVCodecContext* audio_codec_context;
	private SwrContext* swr;
	private AVFrame* audioFrame;
	private byte* audioOut_;
	private int audioOutCapacity;
	private int audioRate, audioChannels;
	private CSound? audio;
	private AudioRing? audioRing;
	private StreamProcedure? audioProc;   // referenced for as long as BASS may call it
	private const double AudioRingSeconds = 12;   // more than the reader ever runs ahead
	private double audioBaseMs;   // media time the ring was last started from (a seek target)
	private CTexture lastTexture;
	private bool bqueueinitialized = false;

	//for convert
	private CFrameConverter frameconv;
	#endregion
}
