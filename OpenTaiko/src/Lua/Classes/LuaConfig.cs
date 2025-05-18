using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OpenTaiko {
	public class LuaConfigFunc {
		private string DirPath;

		public LuaConfigFunc(string dirPath) {
			DirPath = dirPath;
		}

		public JsonNode LoadConfig(string name) {
			using Stream stream = File.OpenRead($"{DirPath}{Path.DirectorySeparatorChar}{name}");
			JsonNode jsonNode = JsonNode.Parse(stream);
			return jsonNode;
		}
	}
}
