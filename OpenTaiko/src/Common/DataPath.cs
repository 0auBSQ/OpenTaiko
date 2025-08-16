namespace OpenTaiko {
	internal class DataPath {

		// Roaming folder in Windows, .config in Linux

		private static string BasePath {
			get {
				return $@"{Environment.SpecialFolder.ApplicationData}/OpenTaiko";
			}
		}
		public static string GetAbsoluteDataPath(string relPath) {
			return $@"{BasePath}/{relPath}".Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
		}
	}
}
