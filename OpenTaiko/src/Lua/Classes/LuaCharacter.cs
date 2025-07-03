using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FDK;

namespace OpenTaiko {
	public class LuaCharacter : IDisposable {
		public string FolderName { get; private set; } = "";
		public string FullPath => Path.Combine(OpenTaiko.strEXEのあるフォルダ, TextureLoader.GLOBAL, TextureLoader.CHARACTERS, FolderName);

		public Dictionary<string, Dictionary<string, LuaTexture[]>> Sprites { get; private set; } = new();
		public Dictionary<string, Dictionary<string, LuaSound>> Sounds { get; private set; } = new();

		public LuaCharacter(string folder_name) {
			FolderName = folder_name;
		}

		public void LoadCharacter() {
			#region Initialize
			// Textures
			string[] directories = Directory.GetDirectories(FullPath, "*", SearchOption.TopDirectoryOnly).Where(item => !item.EndsWith("Sounds")).ToArray();

			// Sounds
			string sound_path = Path.Combine(FullPath, "Sounds");
			string[] sound_dirs = Directory.GetDirectories(sound_path, "*", SearchOption.TopDirectoryOnly);
			#endregion

			// Textures
			foreach (string dir in directories) {
				string[] files = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
				string name = Path.GetRelativePath(FullPath, dir);

				if (files.Length > 0)
					LoadAnimationGroup(name);
				else
					LoadAnimation(name, "");
			}

			// Sounds
			LoadSoundGroup();
			foreach (string dir in sound_dirs) {
				string name = Path.GetRelativePath(sound_path, dir);
				LoadSoundGroup(name);
			}
		}

		#region Load/Unload
		private LuaTexture[] LoadLuaTextureArray(string file_path) {
			int count = OpenTaiko.t連番画像の枚数を数える(file_path + Path.DirectorySeparatorChar);
			LuaTexture[] textures = new LuaTexture[count];

			for (int i = 0; i < count; i++) {
				try {
					textures[i] = new(new CTexture(file_path + Path.DirectorySeparatorChar + $"{i}.png", false));
				} catch {
					textures[i] = new();
				}
			}
			return textures;
		}
		private Dictionary<string, LuaSound> LoadLuaSoundDict(string file_path) {
			Dictionary<string, LuaSound> sounds = [];
			string[] files = Directory.GetFiles(file_path)
				.Where(item => item.ToLower().EndsWith(".ogg") || item.ToLower().EndsWith(".wav"))
				.ToArray();

			foreach (string file in files) {
				string name = Path.GetFileNameWithoutExtension(file);
				if (sounds.ContainsKey(name)) continue;
				sounds.Add(name, new(file, ESoundGroup.Voice));
			}

			return sounds;
		}

		public bool LoadAnimationGroup(string category = "") {
			try {
				if (category == "Sounds") return false;
				if (string.IsNullOrWhiteSpace(category)) category = "";

				string path = FullPath;
				if (!string.IsNullOrWhiteSpace(category)) path = Path.Combine(FullPath, category);
				var dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).Where(dir => !dir.EndsWith("Sounds"));

				return dirs.All(dir => LoadAnimation(Path.GetRelativePath(path, dir), category));
			}
			catch { return false; }
		}
		public bool LoadAnimation(string name, string category = "") {
			try {
				if (category == "Sounds") return false;
				if (string.IsNullOrWhiteSpace(category)) category = "";
				Sprites.TryAdd(category, new());

				string path = Path.Combine(FullPath, name);
				if (!string.IsNullOrWhiteSpace(category)) {
					path = Path.Combine(FullPath, category, name);
				}

				if (Sprites[category].ContainsKey(name)) {
					UnloadAnimation(name, category);
					Sprites[category][name] = LoadLuaTextureArray(path);
				}
				else
					Sprites[category].Add(name, LoadLuaTextureArray(path));

				return true;
			}
			catch { return false; }
		}
		public bool LoadSoundGroup(string category = "") {
			try {
				if (string.IsNullOrWhiteSpace(category)) category = "";
				Sounds.TryAdd(category, new());

				string path = Path.Combine(FullPath, "Sounds");
				if (!string.IsNullOrWhiteSpace(category)) path = Path.Combine(FullPath, "Sounds", category);

				UnloadSoundGroup(category);
				Sounds[category] = LoadLuaSoundDict(path);

				return true;
			}
			catch { return false; }
		}
		public bool LoadSound(string name, string category = "") {
			try {
				if (string.IsNullOrWhiteSpace(category)) category = "";
				Sounds.TryAdd(category, new());

				string path = Path.Combine(FullPath, "Sounds", name);
				if (!string.IsNullOrWhiteSpace(category)) {
					path = Path.Combine(FullPath, "Sounds", category, name);
				}

				string filename = File.Exists(path + ".ogg") ? path + ".ogg" : path + ".wav";
				if (Sounds[category].ContainsKey(name)) {
					UnloadSound(name, category);
					Sounds[category][name] = new(filename, ESoundGroup.Voice);
				} else
					Sounds[category].Add(name, new(filename, ESoundGroup.Voice));

				return true;
			}
			catch { return false; }
		}

		public void UnloadAnimationGroup(string category = "") {
			if (string.IsNullOrWhiteSpace(category)) category = "";

			if (Sprites.ContainsKey(category)) {
				foreach (string key in Sprites[category].Keys) {
					UnloadAnimation(key, category);
				}
			}
		}
		public void UnloadAnimation(string name, string category = "") {
			if (string.IsNullOrWhiteSpace(category)) category = "";

			if (Sprites.TryGetValue(category, out var sprite_sub)) {
				if (sprite_sub.TryGetValue(name, out var textures)) {
					foreach (var tex in textures) tex?.Dispose();
				}
			}
		}
		public void UnloadSoundGroup(string category = "") {
			if (string.IsNullOrWhiteSpace(category)) category = "";

			if (Sounds.ContainsKey(category)) {
				foreach (string key in Sounds[category].Keys) {
					UnloadSound(key, category);
				}
			}
		}
		public void UnloadSound(string name, string category = "") {
			if (string.IsNullOrWhiteSpace(category)) category = "";

			if (Sounds.TryGetValue(category, out var sound_sub)) {
				if (sound_sub.TryGetValue(name, out var sound)) {
					sound?.Dispose();
				}
			}
		}
		#endregion

		#region Dispose
		private bool _disposedValue;
		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {

				foreach (var animation in Sprites) {
					UnloadAnimationGroup(animation.Key);
				}
				foreach (var sounds in Sounds) {
					UnloadSoundGroup(sounds.Key);
				}

				if (disposing) {
					for (int i = Sprites.Count - 1; i >= 0; i--) {
						var key = Sprites.ElementAt(i).Key;
						Sprites[key].Clear();
					}
					Sprites.Clear();

					for (int i = Sounds.Count - 1; i >= 0; i--) {
						var key = Sounds.ElementAt(i).Key;
						Sounds[key].Clear();
					}
					Sounds.Clear();
				}

				_disposedValue = true;
			}
		}
		public void Dispose() {
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
