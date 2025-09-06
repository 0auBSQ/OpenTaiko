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
