using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FDK;
using OpenTaiko.AviUtl2;

namespace OpenTaiko {
	// An AviUtl2 project (.aup2) that plays like a video: a clock, and each frame drawn from the project's
	// objects. Every key of the file can be read; what the player does not draw is listed by Unsupported.
	public class LuaAviUtl2Project : IDisposable, ILuaSeekableInMs, ILuaLoopable, IAup2Sources, IAup2SampleSource {
		internal HashSet<LuaAviUtl2Project>? _disposeList = null;

		private Aup2Document? _doc;
		private Aup2Evaluator? _eval;
		private string _dir = "";
		private int _scene;

		// the project's own files: images by path, videos and sounds by object
		private readonly HashSet<LuaTexture> _textureList = new();
		private readonly HashSet<LuaVideo> _videoList = new();
		private readonly HashSet<LuaSound> _soundList = new();
		private readonly Dictionary<string, LuaTexture> _textures = new();
		private readonly Dictionary<int, LuaVideo> _videos = new();
		private readonly Dictionary<int, LuaSound> _sounds = new();
		private readonly Dictionary<string, string> _resolved = new();
		private readonly List<string> _missing = new();
		private Aup2Renderer? _renderer;
		// the project's script files: its Script folder, and the folders added from Lua
		private Aup2ScriptLibrary? _scripts;
		private readonly List<string> _scriptFolders = new();
		private readonly Dictionary<int, Aup2Evaluator?> _sceneEvaluators = new();

		private readonly Stopwatch _clock = new();
		private double _baseMs;
		private double _speed = 1;
		private bool _playing, _loop;
		private double _volume = 100;
		private float _scaleX = 1, _scaleY = 1, _opacity = 1;
		private int[]? _background;

		public LuaAviUtl2Project() { }

		internal LuaAviUtl2Project(string path) {
			_doc = Aup2Document.Load(path);
			_dir = Path.GetDirectoryName(Path.GetFullPath(path)) ?? "";
			string own = Path.Combine(_dir, "Script");
			if (Directory.Exists(own)) _scriptFolders.Add(own);
			Aup2Samples.Attach(_doc.Scenes, this);
			SelectScene(0);
		}

		private void SelectScene(int index) {
			if (_doc == null || _doc.Scenes.Count == 0) return;
			_scene = Math.Clamp(index, 0, _doc.Scenes.Count - 1);
			if (_scripts == null && _scriptFolders.Count > 0) _scripts = new Aup2ScriptLibrary(_scriptFolders);
			_eval = new Aup2Evaluator(_doc.Scenes[_scene], _scripts);
			_missing.Clear();
			// every picture is queued now, so playing never waits for a file
			bool prev = CTexture.AsyncLoad;
			CTexture.AsyncLoad = true;
			try {
				foreach (var o in _eval.Scene.Objects) {
					if (o.Effects.Count == 0 || o.Effects[0].Name != "画像ファイル") continue;
					Texture(o.Effects[0].Get("ファイル") ?? "");
				}
			} finally { CTexture.AsyncLoad = prev; }
		}

		// The file a stored path means here. A project keeps the full paths of the machine that saved it:
		// the path is tried as it is, then from this project's folder, by the part it shares with the
		// project's own saved path, then by its last folders.
		private string Resolve(string stored) {
			if (_resolved.TryGetValue(stored, out var known)) return known;
			string found = "";
			if (stored.Length > 0) {
				string norm = stored.Replace('\\', '/');
				if (File.Exists(stored)) {
					found = stored;
				} else {
					string saved = (_doc?.SavedPath ?? "").Replace('\\', '/');
					int cut = saved.LastIndexOf('/');
					if (cut > 0 && norm.StartsWith(saved[..(cut + 1)], StringComparison.OrdinalIgnoreCase)) {
						string c = Path.Combine(_dir, norm[(cut + 1)..].Replace('/', Path.DirectorySeparatorChar));
						if (File.Exists(c)) found = c;
					}
					var parts = norm.Split('/');
					for (int i = 1; found.Length == 0 && i < parts.Length; i++) {
						string c = Path.Combine(_dir, string.Join(Path.DirectorySeparatorChar, parts, i, parts.Length - i));
						if (File.Exists(c)) found = c;
					}
				}
				if (found.Length == 0 && !_missing.Contains(stored)) _missing.Add(stored);
			}
			_resolved[stored] = found;
			return found;
		}

		private LuaTexture? Texture(string stored) {
			if (_textures.TryGetValue(stored, out var tex)) return tex;
			string path = Resolve(stored);
			if (path.Length == 0) return null;
			bool prev = CTexture.AsyncLoad;
			CTexture.AsyncLoad = true;   // never decodes on the frame that first needs it
			try { tex = new LuaTextureFunc(_textureList, _dir).CreateTextureFromAbsolutePath(path, autoDispose: true); }
			finally { CTexture.AsyncLoad = prev; }
			_textures[stored] = tex;
			return tex;
		}

		#region The file
		public bool Loaded => _eval != null;
		public int Version => _doc?.Version ?? 0;
		public int Width => _eval?.Scene.Width ?? -1;
		public int Height => _eval?.Scene.Height ?? -1;
		public double FrameRate => _eval?.Scene.Rate ?? 0;
		public int FrameCount => _eval?.Scene.FrameCount ?? 0;
		public double DurationMs => _eval == null ? 0 : FrameCount / _eval.Scene.Rate * 1000.0;
		public int SceneCount => _doc?.Scenes.Count ?? 0;
		public int ObjectCount => _eval?.Scene.Objects.Count ?? 0;
		public int SectionCount => _doc?.Sections.Count ?? 0;

		public int GetScene() => _scene;
		public void SetScene(int index) {
			StopMedia();
			SelectScene(index);
		}
		public string GetSceneName(int index) => (_doc != null && index >= 0 && index < _doc.Scenes.Count) ? _doc.Scenes[index].Name : "";

		// any key of the file: of [project], of the current scene, or of a section by its name ("12", "12.1")
		public string? GetProjectValue(string key) => _doc?.Project?.Get(key);
		public string? GetSceneValue(string key) => _eval?.Scene.Section.Get(key);
		public string? GetValue(string section, string key) {
			if (_doc == null) return null;
			foreach (var s in _doc.Sections)
				if (s.Name == section) return s.Get(key);
			return null;
		}
		public string GetSectionName(int index) => (_doc != null && index >= 0 && index < _doc.Sections.Count) ? _doc.Sections[index].Name : "";

		// a setting of an object's effect at a frame, with its movement applied
		public double GetValueAtFrame(int objectId, string effect, string key, double frame) {
			if (_eval == null) return 0;
			foreach (var o in _eval.Scene.Objects) {
				if (o.Id != objectId) continue;
				var e = o.Find(effect);
				return e == null ? 0 : e.Track(key).Evaluate(frame - o.Start, o.Frames, o.Id * 131 + key.GetHashCode(StringComparison.Ordinal));
			}
			return 0;
		}

		// one line per thing the project uses that is not drawn, is drawn roughly, or is a missing file
		public int UnsupportedCount => (_eval?.Unsupported.Count ?? 0) + _missing.Count;
		public string GetUnsupported() {
			var lines = new List<string>(_eval?.Unsupported ?? new());
			foreach (var m in _missing) lines.Add("missing file: " + m);
			return string.Join("\n", lines);
		}
		// A folder of AviUtl script files (*.anm2, *.obj2, *.anm, *.obj) the project's effects may name.
		// The project's own "Script" folder is read without asking.
		public void AddScriptFolder(string path) {
			if (_doc == null || !Directory.Exists(path) || _scriptFolders.Contains(path)) return;
			_scriptFolders.Add(path);
			_scripts?.Dispose();
			_scripts = null;
			_sceneEvaluators.Clear();
			SelectScene(_scene);
		}
		// what the scripts reported: the first error of each, one per line
		public int ScriptErrorCount => _scripts?.Errors.Count ?? 0;
		public string GetScriptErrors() => _scripts == null ? "" : string.Join("\n", _scripts.Errors);
		public int ApproximatedCount => _eval?.Approximated.Count ?? 0;
		public string GetApproximated() => string.Join("\n", _eval?.Approximated ?? new());
		#endregion

		#region Playing
		public void Start() {
			_baseMs = 0;
			_playing = true;
			_clock.Restart();
			StopMedia();
		}
		public void Resume() {
			if (_playing) return;
			if (!_loop && _baseMs >= DurationMs) return;
			_playing = true;
			_clock.Restart();
		}
		public void Pause() {
			if (!_playing) return;
			_baseMs = GetTimestampMs();
			_playing = false;
			_clock.Reset();
			PauseMedia();
		}
		public void Stop() {
			_playing = false;
			_baseMs = 0;
			_clock.Reset();
			StopMedia();
		}
		public void Reset() => SetTimestampMs(0);

		public double GetTimestampMs() {
			double ms = _baseMs + (_playing ? _clock.Elapsed.TotalMilliseconds * _speed : 0);
			double length = DurationMs;
			if (length <= 0) return 0;
			if (_loop) return ms % length;
			return Math.Min(ms, length);
		}
		public void SetTimestampMs(double ms) {
			_baseMs = Math.Clamp(ms, 0, DurationMs);
			if (_playing) _clock.Restart();
			StopMedia();   // sounds and videos start again from the new time
		}
		public double GetFrame() => GetTimestampMs() / 1000.0 * FrameRate;
		public void SetFrame(double frame) => SetTimestampMs(FrameRate > 0 ? frame / FrameRate * 1000.0 : 0);

		public double GetSpeed() => _speed;
		public void SetSpeed(double speed) {
			_baseMs = GetTimestampMs();
			if (_playing) _clock.Restart();
			_speed = Math.Max(0, speed);
		}
		public bool GetLoop() => _loop;
		public void SetLoop(bool loop) => _loop = loop;
		public bool IsPlaying => _playing && !IsFinished();
		public bool IsFinished() => !_loop && DurationMs > 0 && GetTimestampMs() >= DurationMs;

		public double GetVolumePercent() => _volume;
		public void SetVolumePercent(double vol) => _volume = Math.Clamp(vol, 0, 100);
		#endregion

		#region Drawing
		public void SetScale(float scaleX, float scaleY) { _scaleX = scaleX; _scaleY = scaleY; }
		public void SetOpacity(float opacity) => _opacity = Math.Clamp(opacity, 0f, 1f);
		// A colour behind the objects, in place of the project's own (alpha 0: see-through). Without
		// one the scene's background colour is used, and a scene that has none is see-through where
		// nothing is drawn.
		public void SetBackground(int r, int g, int b, int a) => _background = new[] { r, g, b, a };
		public void ClearBackground() => _background = null;
		public bool HasProjectBackground => _eval?.Scene.Background != null;
		// the scene's own background as 0xRRGGBB, -1 when it has none
		public int ProjectBackground {
			get {
				if (_eval?.Scene.Background is not System.Numerics.Vector3 c) return -1;
				return ((int)Math.Round(c.X * 255) << 16) | ((int)Math.Round(c.Y * 255) << 8) | (int)Math.Round(c.Z * 255);
			}
		}

		// the frame's top left at (x, y), at the scale set by SetScale
		public void Draw(double x, double y) => Render(x, y, _scaleX, _scaleY);

		// the whole frame inside the box, keeping its shape, centred
		public void DrawFit(double x, double y, double w, double h) {
			if (_eval == null || w <= 0 || h <= 0) return;
			double k = Math.Min(w / Width, h / Height);
			Render(x + (w - Width * k) / 2, y + (h - Height * k) / 2, k, k);
		}

		private void Render(double ox, double oy, double kx, double ky) {
			if (_eval == null || _disposedValue) return;
			var scene = _eval.Scene;
			double frame = Math.Min(GetFrame(), Math.Max(0, scene.FrameCount - 1));   // the end holds the last frame
			_renderer ??= new Aup2Renderer(this);
			var gpu = _renderer.Gpu;
			if (!gpu.Ensure()) return;
			RunDrawers(gpu);
			gpu.Begin();
			try {
				TurnDrawn(gpu);
				System.Numerics.Vector4? bg = _background == null ? null
					: new System.Numerics.Vector4(_background[0] / 255f, _background[1] / 255f, _background[2] / 255f, _background[3] / 255f);
				var surface = _renderer.RenderFrame(_eval, frame, bg, out var fr);
				SyncMedia(fr, IsPlaying);
				if (surface != null) {
					_renderer.Present(surface, ox, oy, scene.Width * kx, scene.Height * ky, _opacity);
					gpu.Release(surface);
				}
			} finally {
				gpu.End();
			}
		}

		// the bytes the player holds on the graphics card for its own surfaces and text pictures
		public double GraphicsMemoryMB => _renderer == null ? 0 : (_renderer.Gpu.SurfaceBytes + _renderer.TextBytes) / (1024.0 * 1024.0);
		// the most it keeps of surfaces no object is using
		public void SetSurfaceBudgetMB(double mb) {
			_renderer ??= new Aup2Renderer(this);
			_renderer.Gpu.PoolBudgetBytes = (long)(Math.Max(0, mb) * 1024 * 1024);
		}

		#region Pictures drawn by the skin
		private sealed class Drawer {
			public string File = "";
			public int W, H;
			public NLua.LuaFunction Draw = null!;
			// the layers holding an object that shows the file, top first, and this frame's picture for each
			public List<int> Layers = new();
			public Aup2Surface?[] Pictures = Array.Empty<Aup2Surface?>();
			public bool Failed;
		}
		private readonly List<Drawer> _drawers = new();

		// The skin draws a picture of the project itself. Every object showing the image file named
		// `fileName` shows instead what `draw` draws, with the game's usual drawing calls, in a box
		// width x height whose top left is (0, 0). `draw` runs each frame, once per layer holding such
		// an object, and is given that layer's rank among them (0 for the topmost) and its number.
		public void SetImageDrawer(string fileName, int width, int height, NLua.LuaFunction draw) {
			ClearImageDrawer(fileName);
			if (_eval == null || draw == null || width <= 0 || height <= 0) return;
			var d = new Drawer { File = Path.GetFileName(fileName.Replace('\\', '/')), W = Math.Min(width, Aup2Gpu.MaxSide), H = Math.Min(height, Aup2Gpu.MaxSide), Draw = draw };
			foreach (var o in _eval.Scene.Objects) {
				if (o.Effects.Count == 0 || o.Effects[0].Name != "画像ファイル" || !Shows(o.Effects[0].Get("ファイル"), d.File)) continue;
				if (!d.Layers.Contains(o.Layer)) d.Layers.Add(o.Layer);
			}
			d.Layers.Sort();
			d.Pictures = new Aup2Surface?[d.Layers.Count];
			_drawers.Add(d);
		}

		public void ClearImageDrawer(string fileName) {
			string file = Path.GetFileName(fileName.Replace('\\', '/'));
			for (int i = _drawers.Count - 1; i >= 0; i--) {
				if (!string.Equals(_drawers[i].File, file, StringComparison.OrdinalIgnoreCase)) continue;
				ReleasePictures(_drawers[i]);
				_drawers.RemoveAt(i);
			}
		}

		private static bool Shows(string? stored, string file) =>
			stored != null && string.Equals(Path.GetFileName(stored.Replace('\\', '/')), file, StringComparison.OrdinalIgnoreCase);

		private void ReleasePictures(Drawer d) {
			if (_renderer == null) return;
			for (int i = 0; i < d.Pictures.Length; i++) { _renderer.Gpu.Release(d.Pictures[i]); d.Pictures[i] = null; }
		}

		// each drawer's pictures for this frame; a drawer that fails is left out from then on
		private void RunDrawers(Aup2Gpu gpu) {
			foreach (var d in _drawers) {
				if (d.Failed) continue;
				for (int i = 0; i < d.Layers.Count; i++) {
					Aup2Surface? raw = null;
					try {
						int rank = i, layer = d.Layers[i];
						raw = gpu.CallerDraws(d.W, d.H, () => d.Draw.Call(rank, layer));
					} catch (Exception e) {
						d.Failed = true;
						Trace.TraceError("AviUtl2 player: the drawer of " + d.File + " failed.\n" + e);
					}
					gpu.Release(d.Pictures[i]);
					d.Pictures[i] = raw;
					if (d.Failed) break;
				}
			}
		}

		// what the game drew stands bottom row first: turned over to the way the player keeps pictures
		private void TurnDrawn(Aup2Gpu gpu) {
			foreach (var d in _drawers) {
				for (int i = 0; i < d.Pictures.Length; i++) {
					if (d.Pictures[i] is not Aup2Surface raw) continue;
					var turned = gpu.Rent(raw.W, raw.H);
					gpu.Pass(turned, "void main(){ o = texture(u_src, vec2(v_uv.x, 1.0 - v_uv.y)); }", raw);
					gpu.Release(raw);
					d.Pictures[i] = turned;
				}
			}
		}

		private bool Drawn(string storedPath, int objectId, out Aup2Texture picture) {
			picture = default;
			if (_drawers.Count == 0 || _eval == null) return false;
			foreach (var d in _drawers) {
				if (d.Failed || !Shows(storedPath, d.File)) continue;
				int layer = -1;
				foreach (var o in _eval.Scene.Objects) if (o.Id == objectId) { layer = o.Layer; break; }
				int at = Math.Max(0, d.Layers.IndexOf(layer));
				if (at < d.Pictures.Length && d.Pictures[at] is Aup2Surface s)
					picture = new Aup2Texture { Id = s.Tex, W = s.W, H = s.H, Straight = false };
				return picture.Ok;
			}
			return false;
		}
		#endregion

		Aup2Texture IAup2Sources.Image(string storedPath, int objectId) {
			if (Drawn(storedPath, objectId, out var drawn)) return drawn;
			var tex = Texture(storedPath);
			var ctex = tex?._texture;
			if (ctex == null) return default;
			ctex.RealizeIfDeferred();
			if (ctex.Pointer == 0 || ctex.UploadPending) return default;
			return new Aup2Texture { Id = ctex.Pointer, W = tex!.Width, H = tex.Height, Straight = true };
		}

		Aup2Texture IAup2Sources.Video(int objectId) {
			if (!_videos.TryGetValue(objectId, out var video) || video.Width <= 0) return default;
			var ctex = video.Texture._texture;
			if (ctex == null || ctex.Pointer == 0) return default;
			return new Aup2Texture { Id = ctex.Pointer, W = video.Width, H = video.Height, Straight = true };
		}

		Aup2Texture IAup2Sources.ScriptImage(string path) => ((IAup2Sources)this).Image(path);

		private readonly Dictionary<string, Aup2Mesh?> _models = new();

		// read with the engine's own model reader; each face keeps its flat facing
		Aup2Mesh? IAup2Sources.Model(string storedPath) {
			if (_models.TryGetValue(storedPath, out var mesh)) return mesh;
			string path = Resolve(storedPath);
			if (path.Length > 0 && path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase)) {
				try {
					var positions = new List<float>();
					var colours = new List<int>();
					ObjModel.LoadUncached(path).CopyTriangles(positions, colours);
					var data = new float[positions.Count / 9 * 27];
					bool coloured = false;
					for (int t = 0; t < positions.Count / 9; t++) {
						var a = new System.Numerics.Vector3(positions[t * 9], positions[t * 9 + 1], positions[t * 9 + 2]);
						var b = new System.Numerics.Vector3(positions[t * 9 + 3], positions[t * 9 + 4], positions[t * 9 + 5]);
						var c = new System.Numerics.Vector3(positions[t * 9 + 6], positions[t * 9 + 7], positions[t * 9 + 8]);
						var n = System.Numerics.Vector3.Cross(b - a, c - a);
						n = n.LengthSquared() > 1e-12f ? System.Numerics.Vector3.Normalize(n) : new System.Numerics.Vector3(0, 0, -1);
						int rgb = colours[t];
						if (rgb != 0xB0A090) coloured = true;
						for (int k = 0; k < 3; k++) {
							int at = t * 27 + k * 9;
							data[at] = positions[t * 9 + k * 3]; data[at + 1] = positions[t * 9 + k * 3 + 1]; data[at + 2] = positions[t * 9 + k * 3 + 2];
							data[at + 3] = n.X; data[at + 4] = n.Y; data[at + 5] = n.Z;
							data[at + 6] = ((rgb >> 16) & 255) / 255f; data[at + 7] = ((rgb >> 8) & 255) / 255f; data[at + 8] = (rgb & 255) / 255f;
						}
					}
					// a model with no material is a middle grey
					if (!coloured) for (int i = 0; i < data.Length; i += 9) data[i + 6] = data[i + 7] = data[i + 8] = 0.5f;
					mesh = new Aup2Mesh { Data = data, Coloured = coloured };
				} catch (Exception) {
					mesh = null;
				}
			}
			_models[storedPath] = mesh;
			return mesh;
		}

		Aup2Evaluator? IAup2Sources.SceneEvaluator(int index) {
			if (_doc == null) return null;
			if (_sceneEvaluators.TryGetValue(index, out var ev)) return ev;
			var scene = _doc.Scenes.Find(s => s.Index == index);
			ev = scene == null ? null : new Aup2Evaluator(scene, _scripts);
			_sceneEvaluators[index] = ev;
			return ev;
		}
		#endregion

		#region Sounds and videos
		private const double DriftMs = 250;
		// a video or a sound that no frame used for this long is closed; it opens again if it comes back
		private const double IdleCloseMs = 4000;
		private readonly Dictionary<int, long> _mediaSeen = new();
		private readonly Stopwatch _wall = Stopwatch.StartNew();
		private readonly Aup2MonoMixes _mono = new();
		// the sound files the objects that draw sound read, decoded once each
		private const int MostSampleTables = 8;
		private readonly Dictionary<string, Aup2SampleTable?> _sampleTables = new();

		Aup2SampleTable? IAup2SampleSource.Samples(string storedPath) {
			if (_sampleTables.TryGetValue(storedPath, out var table)) return table;
			if (_disposedValue || _sampleTables.Count >= MostSampleTables) return null;
			string path = Resolve(storedPath);
			table = path.Length > 0 ? Aup2SampleTable.Load(path) : null;
			_sampleTables[storedPath] = table;
			return table;
		}

		private void SyncMedia(Aup2Frame fr, bool playing) {
			var live = new HashSet<int>();
			foreach (var item in fr.Draws) {
				if (item.Kind != Aup2ItemKind.Video) continue;
				live.Add(item.ObjectId);
				if (!_videos.TryGetValue(item.ObjectId, out var video)) {
					string path = Resolve(item.Path);
					if (path.Length == 0) continue;
					video = new LuaVideoFunc(_videoList, _dir).CreateFromAbsolutePath(path, autoDispose: true, withAudio: true);
					video.SetTimestampMs(item.SourceMs);
					_videos[item.ObjectId] = video;
				}
				video.SetSpeed(_speed);
				video.SetVolumePercent(_volume);
				if (playing) {
					video.Resume();
					if (Math.Abs(video.GetTimestampMs() - item.SourceMs) > DriftMs) video.SetTimestampMs(item.SourceMs);
				} else {
					video.Pause();
				}
			}
			foreach (var a in fr.Audio) {
				live.Add(a.ObjectId);
				if (!_sounds.TryGetValue(a.ObjectId, out var sound)) {
					string path = Resolve(a.Path);
					if (path.Length == 0) continue;
					sound = new LuaSoundFunc(_soundList, _dir).CreateSoundFromAbsolutePath(path, ESoundGroup.SongPlayback);
					_sounds[a.ObjectId] = sound;
				}
				sound.SetLoop(a.Loop);
				sound.SetSpeed(_speed * a.Speed);
				sound.SetVolumePercent(Math.Clamp(_volume * a.Volume, 0, 100));
				sound.SetPan((int)Math.Round(a.Pan * 100));
				_mono.Set(a.ObjectId, sound._sound, a.Mono);
				if (!playing) { sound.Pause(); continue; }
				if (!sound.Loaded) {
					sound.Play();
					sound.SetTimestampMs(a.OffsetMs);
				} else if (!sound.IsPlaying) {
					if (sound.IsFinished() && !a.Loop) continue;
					sound.Play();   // from 0, so the place is set after it
					sound.SetTimestampMs(a.OffsetMs);
				} else if (!a.Loop && Math.Abs(sound.GetTimestampMs() - a.OffsetMs) > DriftMs) {
					sound.SetTimestampMs(a.OffsetMs);
				}
			}
			long nowMs = _wall.ElapsedMilliseconds;
			foreach (int id in live) _mediaSeen[id] = nowMs;
			List<int>? idle = null;
			foreach (var kv in _videos) {
				if (live.Contains(kv.Key)) continue;
				kv.Value.Pause();
				if (nowMs - _mediaSeen.GetValueOrDefault(kv.Key) > IdleCloseMs) (idle ??= new()).Add(kv.Key);
			}
			if (idle != null) foreach (int id in idle) { _videos[id].Dispose(); _videos.Remove(id); _mediaSeen.Remove(id); }
			idle = null;
			foreach (var kv in _sounds) {
				if (live.Contains(kv.Key)) continue;
				kv.Value.Stop();
				if (nowMs - _mediaSeen.GetValueOrDefault(kv.Key) > IdleCloseMs) (idle ??= new()).Add(kv.Key);
			}
			if (idle != null) foreach (int id in idle) { _mono.Drop(id); _sounds[id].Dispose(); _sounds.Remove(id); _mediaSeen.Remove(id); }
		}

		private void PauseMedia() {
			foreach (var v in _videos.Values) v.Pause();
			foreach (var s in _sounds.Values) s.Pause();
		}

		private void StopMedia() {
			foreach (var v in _videos.Values) v.Pause();
			foreach (var s in _sounds.Values) s.Stop();
		}
		#endregion

		#region Dispose
		private bool _disposedValue;
		protected virtual void Dispose(bool disposing) {
			if (_disposedValue) return;
			_mono.Clear();
			if (_doc != null) Aup2Samples.Detach(_doc.Scenes);
			foreach (var t in _sampleTables.Values) t?.Drop();
			_sampleTables.Clear();
			foreach (var s in new List<LuaSound>(_soundList)) s.Dispose();
			foreach (var v in new List<LuaVideo>(_videoList)) v.Dispose();
			foreach (var t in new List<LuaTexture>(_textureList)) t.Dispose();
			_scripts?.Dispose();
			_scripts = null;
			foreach (var d in _drawers) ReleasePictures(d);
			_drawers.Clear();
			_renderer?.Dispose();
			_renderer = null;
			_sceneEvaluators.Clear();
			_sounds.Clear(); _videos.Clear(); _textures.Clear(); _models.Clear();
			_eval = null; _doc = null;
			_disposeList?.Remove(this);
			_disposedValue = true;
		}
		public void Dispose() {
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}

	public class LuaAviUtl2Func {
		private HashSet<LuaAviUtl2Project> Projects;
		private string DirPath;

		public LuaAviUtl2Func(HashSet<LuaAviUtl2Project> projects, string dirPath) {
			Projects = projects;
			DirPath = dirPath;
		}

		private LuaAviUtl2Project Open(string full_path) {
			if (!File.Exists(full_path)) {
				LogNotification.PopWarning($"AviUtl2 project failed to load because the file located at '{full_path}' does not exist.");
				return new();
			}
			try {
				var project = new LuaAviUtl2Project(full_path) { _disposeList = Projects };
				Projects.Add(project);
				return project;
			} catch (Exception e) {
				LogNotification.PopWarning($"AviUtl2 project failed to load: {e}");
				return new();
			}
		}

		public LuaAviUtl2Project LoadProject(string path)
			=> Open($@"{DirPath}{Path.DirectorySeparatorChar}{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}");
		public LuaAviUtl2Project LoadProjectFromAbsolutePath(string path)
			=> Open(path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
	}
}
