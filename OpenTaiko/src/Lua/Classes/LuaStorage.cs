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

		/// <summary>Write text to a file under the stage directory (creating subdirectories as needed). Rejects
		/// absolute paths and ".." traversal so a stage can only write inside its own folder. Returns true on success.</summary>
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

		/// <summary>Read a file's raw text (under the stage dir, or an absolute path), or null if missing.</summary>
		public string ReadText(string name) {
			try {
				string full = Path.IsPathRooted(name) ? name : Path.Combine(DirPath, name);
				return File.Exists(full) ? File.ReadAllText(full) : null;
			} catch { return null; }
		}

		/// <summary>Absolute path of a file under the stage directory (for display), or null.</summary>
		public string GetFullPath(string name) {
			if (string.IsNullOrEmpty(name) || Path.IsPathRooted(name)) return null;
			return Path.GetFullPath(Path.Combine(DirPath, name));
		}

		// ── online lobby codes: written to the shared <exe>/Global/Lobbycodes/ folder (NOT a stage dir) ────────
		private static string LobbyCodesDir() => Path.Combine(OpenTaiko.strEXEのあるフォルダ, "Global", "Lobbycodes");
		/// <summary>Write a lobby code to <exe>/Global/Lobbycodes/&lt;name&gt;. Access is limited to that subfolder
		/// (rooted paths / ".." rejected). Returns true on success.</summary>
		public bool WriteLobbyCode(string name, string contents) {
			if (string.IsNullOrEmpty(name) || Path.IsPathRooted(name) || name.Contains("..")) return false;
			try {
				string dir = LobbyCodesDir();
				Directory.CreateDirectory(dir);
				File.WriteAllText(Path.Combine(dir, name), contents);
				return true;
			} catch { return false; }
		}
		/// <summary>Open the OS file browser at <exe>/Global/Lobbycodes/.</summary>
		public bool RevealLobbyCodes() {
			try {
				string dir = LobbyCodesDir(); Directory.CreateDirectory(dir);
				if (OperatingSystem.IsWindows())
					System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", Arguments = "\"" + dir + "\"", UseShellExecute = true });
				else
					System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dir, UseShellExecute = true });
				return true;
			} catch { return false; }
		}

		/// <summary>Open the OS file browser with this file selected (so a player can grab/share it — e.g. a room
		/// code .txt). Falls back to opening the containing folder. Windows/macOS/Linux. Returns true on success.</summary>
		public bool RevealInExplorer(string name) {
			try {
				string full = GetFullPath(name);
				if (full == null) return false;
				string dir = Path.GetDirectoryName(full);
				if (File.Exists(full) && OperatingSystem.IsWindows())
					System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", Arguments = "/select,\"" + full + "\"", UseShellExecute = true });
				else if (File.Exists(full) && OperatingSystem.IsMacOS())
					System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "open", Arguments = "-R \"" + full + "\"", UseShellExecute = false });
				else
					System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dir, UseShellExecute = true });   // just open the folder
				return true;
			} catch { return false; }
		}
	}
}
