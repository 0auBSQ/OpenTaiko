using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace OpenTaiko.AviUtl2 {
	// One "[name]" block of an .aup2 file: its "key=value" lines, in file order.
	public sealed class Aup2Section {
		public string Name { get; }
		public List<KeyValuePair<string, string>> Items { get; } = new();

		public Aup2Section(string name) { Name = name; }

		public string? Get(string key) {
			foreach (var kv in Items)
				if (kv.Key == key) return kv.Value;
			return null;
		}

		public int GetInt(string key, int fallback = 0)
			=> int.TryParse(Get(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;

		public double GetDouble(string key, double fallback = 0)
			=> double.TryParse(Get(key), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : fallback;
	}

	// An effect of an object ("[object.effect]"): the input, the drawing settings or a filter.
	public sealed class Aup2Effect {
		public string Name { get; }
		public Aup2Section Section { get; }
		private readonly Dictionary<string, Aup2Track> _tracks = new();

		public Aup2Effect(Aup2Section section) {
			Section = section;
			Name = section.Get("effect.name") ?? "";
		}

		public string? Get(string key) => Section.Get(key);
		public bool Has(string key) => Section.Get(key) != null;

		// the setting as a value over time; a missing or text setting is the constant fallback
		public Aup2Track Track(string key, double fallback = 0) {
			if (!_tracks.TryGetValue(key, out var t)) {
				t = Aup2Track.Parse(Section.Get(key), fallback);
				_tracks[key] = t;
			}
			return t;
		}

		public bool Flag(string key) => Section.GetInt(key) != 0;
	}

	// A timeline object ("[id]"): a layer, its frame range (with its middle points) and its effects.
	public sealed class Aup2Object {
		public int Id { get; }
		public int Layer { get; }
		// the first frame, the frame of each middle point, the last frame (inclusive)
		public int[] Frames { get; }
		public int Start => Frames[0];
		public int End => Frames[^1];
		public Aup2Section Section { get; }
		public List<Aup2Effect> Effects { get; } = new();

		public Aup2Object(int id, Aup2Section section) {
			Id = id;
			Section = section;
			Layer = section.GetInt("layer");
			var parts = (section.Get("frame") ?? "0,0").Split(',');
			var frames = new List<int>();
			foreach (var p in parts)
				if (int.TryParse(p.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int f)) frames.Add(f);
			if (frames.Count == 0) frames.Add(0);
			if (frames.Count == 1) frames.Add(frames[0]);
			Frames = frames.ToArray();
		}

		public Aup2Effect? Find(string name) {
			foreach (var e in Effects)
				if (e.Name == name) return e;
			return null;
		}
	}

	public sealed class Aup2Scene {
		public int Index { get; }
		public string Name { get; }
		public int Width { get; }
		public int Height { get; }
		public double Rate { get; }
		public int AudioRate { get; }
		// the scene's background colour (video.background, rrggbb); null when it has none
		public System.Numerics.Vector3? Background { get; }
		public Aup2Section Section { get; }
		public List<Aup2Object> Objects { get; } = new();

		public Aup2Scene(Aup2Section section, int fallbackIndex) {
			Section = section;
			Index = section.GetInt("scene", fallbackIndex);
			Name = section.Get("name") ?? "";
			string colour = (section.Get("video.background") ?? "").Trim().TrimStart('#');
			if (colour.Length >= 6 && int.TryParse(colour.AsSpan(0, 6), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out int rgb))
				Background = new System.Numerics.Vector3(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
			Width = Math.Max(1, section.GetInt("video.width", 1920));
			Height = Math.Max(1, section.GetInt("video.height", 1080));
			// video.rate over video.scale is the frame rate
			double scale = Math.Max(1, section.GetDouble("video.scale", 1));
			Rate = Math.Max(1e-3, section.GetDouble("video.rate", 30) / scale);
			AudioRate = section.GetInt("audio.rate", 44100);
		}

		// frames 0 .. FrameCount - 1
		public int FrameCount {
			get {
				int last = -1;
				foreach (var o in Objects) last = Math.Max(last, o.End);
				return last + 1;
			}
		}
	}

	// A parsed .aup2 project. Every section and key is kept, in order, whether or not this player knows it.
	public sealed class Aup2Document {
		public List<Aup2Section> Sections { get; } = new();
		public Aup2Section? Project { get; private set; }
		public List<Aup2Scene> Scenes { get; } = new();
		public int Version => Project?.GetInt("version") ?? 0;
		// the path the project was saved at, on the machine that saved it
		public string SavedPath => Project?.Get("file") ?? Project?.Get("ファイル") ?? "";

		public static Aup2Document Load(string path) => Parse(File.ReadAllText(path, Encoding.UTF8));

		public static Aup2Document Parse(string text) {
			var doc = new Aup2Document();
			Aup2Section? cur = null;
			foreach (var raw in text.Split('\n')) {
				string line = raw.EndsWith('\r') ? raw[..^1] : raw;
				if (line.Length == 0) continue;
				if (line[0] == '[' && line[^1] == ']') {
					cur = new Aup2Section(line[1..^1]);
					doc.Sections.Add(cur);
					continue;
				}
				int eq = line.IndexOf('=');
				if (eq <= 0 || cur == null) continue;
				cur.Items.Add(new(line[..eq], line[(eq + 1)..]));
			}

			// objects belong to the scene named by their "scene" key, else to the scene section before them
			var objects = new Dictionary<int, Aup2Object>();   // the latest object of each id: its effects follow it
			Aup2Scene? scene = null;
			foreach (var s in doc.Sections) {
				if (s.Name == "project") { doc.Project = s; continue; }
				if (s.Name.StartsWith("scene.", StringComparison.Ordinal)) {
					scene = new Aup2Scene(s, doc.Scenes.Count);
					doc.Scenes.Add(scene);
					continue;
				}
				int dot = s.Name.IndexOf('.');
				string idText = dot < 0 ? s.Name : s.Name[..dot];
				if (!int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
				if (scene == null) {
					scene = new Aup2Scene(new Aup2Section("scene.0"), 0);
					doc.Scenes.Add(scene);
				}
				if (dot < 0) {
					var owner = scene;
					if (s.Get("scene") != null) owner = doc.Scenes.Find(x => x.Index == s.GetInt("scene")) ?? scene;
					var obj = new Aup2Object(id, s);
					owner.Objects.Add(obj);
					objects[id] = obj;
				} else if (objects.TryGetValue(id, out var obj)) {
					obj.Effects.Add(new Aup2Effect(s));
				}
			}
			return doc;
		}

		// the file's text again, as AviUtl2 writes it (CRLF, no BOM)
		public string Serialize() {
			var sb = new StringBuilder();
			foreach (var s in Sections) {
				sb.Append('[').Append(s.Name).Append("]\r\n");
				foreach (var kv in s.Items) sb.Append(kv.Key).Append('=').Append(kv.Value).Append("\r\n");
			}
			return sb.ToString();
		}
	}
}
