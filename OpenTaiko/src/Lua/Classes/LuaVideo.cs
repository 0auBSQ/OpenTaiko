using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;

namespace OpenTaiko {
	public class LuaVideo : IDisposable {
		private CVideoDecoder? _video = null;
		private CTexture? _tmpTex = null;
		internal HashSet<LuaVideo>? _disposeList = null;

		public LuaVideo() {
			_video = null;
		}

		public LuaVideo(CVideoDecoder video) {
			_video = video;
		}

		public void Start() {
			_video?.Start();
		}

		public void Resume() {
			_video?.Resume();
		}

		public void Pause() {
			_video?.Pause();
		}

		public void Stop() {
			_video?.Stop();
		}

		public void Reset() {
			_video?.Seek(0);
		}


		#region Gets
		public int Width => _video?.FrameSize.Width ?? -1;
		public int Height => _video?.FrameSize.Height ?? -1;
		public double Duration => _video?.Duration ?? 1;
		public LuaTexture Texture {
			get {
				if (_video != null) {
					_video.GetNowFrame(ref _tmpTex);

					LuaTexture frametex = new LuaTexture(_tmpTex);
					return frametex;
				}

				LuaTexture emptytex = new LuaTexture();
				return emptytex;
			}
		}

		public double GetPlayPosition() {
			return (_video?.msPlayPosition ?? 0) / 1000.0;
		}

		public double GetPlaySpeed() {
			return _video?.dbPlaySpeed ?? 1;
		}
		#region Sets
		public void SetPlayPosition(double position) {
			_video?.Seek((long)(position * 1000.0));
		}

		public void SetPlaySpeed(double playSpeed) {
			if (_video == null) return;

			_video.dbPlaySpeed = playSpeed;
		}

		#endregion


		private bool _disposedValue;
		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {
				OpenTaiko.tDisposeSafely(ref _video);
				OpenTaiko.tDisposeSafely(ref _tmpTex);
				_disposeList?.Remove(this);
				_disposedValue = true;
			}
		}
		public void Dispose() {
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
	public class LuaVideoFunc {
		private HashSet<LuaVideo> Videos;
		private string DirPath;

		public LuaVideoFunc(HashSet<LuaVideo> videos, string dirPath) {
			Videos = videos;
			DirPath = dirPath;
		}

		public LuaVideo CreateVideo(string path) {
			string full_path = $@"{DirPath}{Path.DirectorySeparatorChar}{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}";

			LuaVideo luavid = new();
			if (File.Exists(full_path)) {
				try {
					var vid = new CVideoDecoder(full_path);
					vid.InitRead();
					luavid = new LuaVideo(vid);
					Videos.Add(luavid);
					luavid._disposeList = this.Videos;
				} catch (Exception e) {
					LogNotification.PopWarning($"Lua Video failed to load: {e}");
					luavid?.Dispose();
					luavid = new();
				}
			}
			else {
				LogNotification.PopWarning($"Lua Video failed to load because the file located at '{full_path}' does not exist.");
			}
			return luavid;
		}
	}
}
