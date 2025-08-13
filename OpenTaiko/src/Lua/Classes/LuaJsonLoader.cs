using System.Text.Json.Nodes;

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
	}
}
