namespace OpenTaiko {
	internal class DataPath {

		// Roaming folder in Windows, .config in Linux

		private static string BasePath {
			get {
				return $@"Global/ApplicationData";
			}
		}
		public static string GetAbsoluteDataPath(string relPath) {
			return $@"{BasePath}/{relPath}".Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
		}
	}
}
