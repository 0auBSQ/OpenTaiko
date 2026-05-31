using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;

namespace OpenTaiko {
	public class LuaJsonLoaderFunc {
		private string DirPath;

		public LuaJsonLoaderFunc(string dirPath) {
			DirPath = dirPath;
		}

		public JsonNode LoadJson(string name) {
			using Stream stream = File.OpenRead($"{DirPath}{Path.DirectorySeparatorChar}{name}");
			JsonNode jsonNode = JsonNode.Parse(stream);
			return jsonNode;
		}

		public double ExtractNumber(JsonValue x) {
			return (double)x;
		}

		public string ExtractText(JsonValue x) {
			return (string)x;
		}

		public Dictionary<string, object> JsonParseFile(string name) {
			string json = File.ReadAllText($"{DirPath}{Path.DirectorySeparatorChar}{name}");
			if (string.IsNullOrWhiteSpace(json))
				return new Dictionary<string, object>();

			var token = JToken.Parse(json);
			return ToDictionary(token);
		}

		/// <summary>
		/// Like <see cref="JsonParseFile"/> but works for JSON files whose root is an array as well as
		/// an object.  Returns null if the file does not exist or is empty.
		/// Arrays become 1-indexed <c>Dictionary&lt;int, object&gt;</c>; objects become
		/// <c>Dictionary&lt;string, object&gt;</c> — matching the existing conversion rules.
		/// </summary>
		/// <summary>
		/// Safe key lookup for a JSON-parsed dictionary. Returns null for missing keys instead of
		/// throwing <see cref="KeyNotFoundException"/>. Accepts both string-keyed and int-keyed
		/// dictionaries as produced by <see cref="JsonParseFileAny"/>.
		/// </summary>
		public object? JsonGet(object? dict, object? key) {
			if (dict is Dictionary<string, object> sd && key is string sk) {
				sd.TryGetValue(sk, out var sv);
				return sv;
			}
			if (dict is Dictionary<int, object> id) {
				int ik = key switch {
					long l  => (int)l,
					int  i  => i,
					double d => (int)d,
					_ => -1
				};
				if (ik >= 0) { id.TryGetValue(ik, out var iv); return iv; }
			}
			return null;
		}

		public object? JsonParseFileAny(string name) {
			string fullPath = Path.IsPathRooted(name) ? name : Path.Combine(DirPath, name);
			if (!File.Exists(fullPath)) return null;
			string json = File.ReadAllText(fullPath);
			if (string.IsNullOrWhiteSpace(json)) return null;
			var token = JToken.Parse(json);
			return ConvertToken(token);
		}

		/// <summary>Write text to a file under the stage directory (creating subdirectories as needed).
		/// Used by tools such as the map editor to save JSON. Rejects absolute paths and ".." traversal
		/// so a stage can only write inside its own folder. Returns true on success.</summary>
		public bool WriteText(string name, string contents) {
			if (string.IsNullOrEmpty(name) || Path.IsPathRooted(name)) return false;
			string fullPath = Path.GetFullPath(Path.Combine(DirPath, name));
			string root = Path.GetFullPath(DirPath);
			if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return false;
			try {
				string dir = Path.GetDirectoryName(fullPath);
				if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
				File.WriteAllText(fullPath, contents);
				return true;
			} catch { return false; }
		}

		public Dictionary<string, object> JsonParseString(string json) {
			if (string.IsNullOrWhiteSpace(json))
				return new Dictionary<string, object>();

			var token = JToken.Parse(json);
			return ToDictionary(token);
		}

		private Dictionary<string, object> ToDictionary(JToken token) {
			if (token is JObject obj) {
				var dict = new Dictionary<string, object>();
				foreach (var property in obj.Properties()) {
					dict[property.Name] = ConvertToken(property.Value);
				}
				return dict;
			}
			throw new ArgumentException("JSON root is not an object");
		}

		private object ConvertToken(JToken token) {
			switch (token.Type) {
				case JTokenType.Object:
					return ToDictionary(token);
				case JTokenType.Array:
					var dict = new Dictionary<int, object>();
					int index = 1; // 1-indexed
					foreach (var item in token) {
						dict[index++] = ConvertToken(item);
					}
					return dict;
				case JTokenType.Integer:
					return token.ToObject<long>();
				case JTokenType.Float:
					return token.ToObject<double>();
				case JTokenType.String:
					return token.ToObject<string>();
				case JTokenType.Boolean:
					return token.ToObject<bool>();
				case JTokenType.Null:
					return null;
				default:
					return token.ToString();
			}
		}
	}
}
