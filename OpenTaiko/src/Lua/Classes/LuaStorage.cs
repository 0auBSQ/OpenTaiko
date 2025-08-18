using FDK;

namespace OpenTaiko {
	public class LuaStorageFunc {
		private string DirPath;

		public LuaStorageFunc(string dirPath) {
			DirPath = dirPath;
		}

		public string[] GetFiles(string dir, string ext) {
			string fullPath = Path.Combine(DirPath, dir);
			string[] files = Directory.GetFiles(fullPath, ext);
			string[] realFileNames = new string[files.Length];

			for (int i = 0; i < files.Length; i++) {
				realFileNames[i] = files[i].Remove(0, fullPath.Length - dir.Length);
			}

			return realFileNames;
		}

		public bool FileExists(string path) {
			string fullPath = Path.Combine(DirPath, path);
			return File.Exists(fullPath);
		}

		public bool DirectoryExists(string path) {
			string fullPath = Path.Combine(DirPath, path);
			return Directory.Exists(fullPath);
		}
	}
}
