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

		// Null/shape-safe extraction: Lua indexes JsonNodes freely (node["maybe-missing"]), so a
		// missing key hands us null and a wrong-typed value throws on the cast — either took the
		// whole stage down (the "opening the computer crashes" bug). Missing/invalid = 0 / null.
		public double ExtractNumber(JsonValue x) {
			if (x == null) return 0;
			try { return (double)x; } catch { }
			try { return (string)x is string s && double.TryParse(s, out double v) ? v : 0; } catch { return 0; }
		}

		public string ExtractText(JsonValue x) {
			if (x == null) return null;
			try { return (string)x; } catch { }
			try { return x.ToString(); } catch { return null; }
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

		public Dictionary<string, object> JsonParseString(string json) {
			if (string.IsNullOrWhiteSpace(json))
				return new Dictionary<string, object>();

			var token = JToken.Parse(json);
			return ToDictionary(token);
		}

		/// <summary>Parse a JSON string whose root may be an OBJECT or an ARRAY (arrays become a 1-indexed
		/// Dictionary&lt;int,object&gt;). Returns null on empty/invalid input. Use with <see cref="JsonGet"/>.
		/// Handy for network payloads like a peer roster array.</summary>
		public object? JsonParseStringAny(string json) {
			if (string.IsNullOrWhiteSpace(json)) return null;
			try { return ConvertToken(JToken.Parse(json)); } catch { return null; }
		}

		/// <summary>Number of entries in a parsed JSON array/object dictionary (0 if not a dictionary).</summary>
		public int JsonCount(object? dict) {
			if (dict is Dictionary<int, object> id) return id.Count;
			if (dict is Dictionary<string, object> sd) return sd.Count;
			return 0;
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
