
namespace OpenTaiko {
	public class LuaIni {
		private Dictionary<string, string> _keyValuePairs;
		public LuaIni(Dictionary<string, string> keyValuePairs) {
			_keyValuePairs = keyValuePairs;
		}

		public bool GetBool(string key, bool defaultValue) {
			return GetInt(key, defaultValue ? 1 : 0) == 1;
		}

		public int GetInt(string key, int defaultValue) {
			if (!_keyValuePairs.ContainsKey(key)) return defaultValue;

			if (int.TryParse(_keyValuePairs[key], out int result)) return result;
			return 0;
		}

		public double GetDouble(string key, double defaultValue) {
			if (!_keyValuePairs.ContainsKey(key)) return defaultValue;

			if (double.TryParse(_keyValuePairs[key], out double result)) return result;
			return 0;
		}

		public string GetString(string key, string defaultValue) {
			if (!_keyValuePairs.ContainsKey(key)) return defaultValue;

			return _keyValuePairs[key].ToString();
		}

		public string[] GetStringArray(string key) {
			if (!_keyValuePairs.ContainsKey(key)) return [];

			string[] splited = _keyValuePairs[key].Split(',');
			return splited;
		}

		public int[] GetIntArray(string key) {
			if (!_keyValuePairs.ContainsKey(key)) return [];

			string[] splited = _keyValuePairs[key].Split(',');
			int[] array = new int[splited.Length];

			for (int i = 0; i < splited.Length; i++) {
				if (int.TryParse(splited[i], out int result)) array[i] = result;
				else array[i] = 0;
			}

			return array;
		}

		public double[] GetDoubleArray(string key) {
			if (!_keyValuePairs.ContainsKey(key)) return [];

			string[] splited = _keyValuePairs[key].Split(',');
			double[] array = new double[splited.Length];

			for (int i = 0; i < splited.Length; i++) {
				if (double.TryParse(splited[i], out double result)) array[i] = result;
				else array[i] = 0;
			}

			return array;
		}
	}
	public class LuaIniLoaderFunc {
		private string DirPath;

		public LuaIniLoaderFunc(string dirPath) {
			DirPath = dirPath;
		}

		public LuaIni LoadIni(string name) {
			string path = $"{DirPath}{Path.DirectorySeparatorChar}{name}";
			Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();

			if (File.Exists(path)) {
				using StreamReader stream = new StreamReader(path);

				while (!stream.EndOfStream) {
					string text = stream.ReadLine() ?? "";
					string[] splited = text.Split('=');

					if (splited.Length < 2) continue;

					string key = splited[0];
					string value = splited[1];

					if (!keyValuePairs.ContainsKey(key)) {
						keyValuePairs.Add(key, value);
					} else {
						keyValuePairs[key] = value;
					}
				}
			}

			LuaIni luaIni = new LuaIni(keyValuePairs);

			return luaIni;
		}
	}
}
