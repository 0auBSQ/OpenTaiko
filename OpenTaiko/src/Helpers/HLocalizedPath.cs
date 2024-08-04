namespace OpenTaiko {
	public static class HLocalizedPath {
		public static string GetFullPathWithoutExtension(string path) {
			return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path));
		}

		public static String GetLocalizedPath(String ogPath, String langCode = null) {
			if (String.IsNullOrEmpty(langCode))
				langCode = CLangManager.fetchLang();

			String ext = System.IO.Path.GetExtension(ogPath);
			String fp = HLocalizedPath.GetFullPathWithoutExtension(ogPath);
			return (fp + "_" + langCode + ext);
		}

		public static String GetAvailableLocalizedPath(String ogPath, String langCode = null) {
			string path = GetLocalizedPath(ogPath, langCode);

			if (File.Exists(path)) return path;
			return ogPath;
		}
	}
}
