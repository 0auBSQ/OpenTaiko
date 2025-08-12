using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FDK;

namespace OpenTaiko {
	public class LuaCharacter : IDisposable {
		public string FolderName { get; private set; }
		public string FullPath { get; private set; }

		public Dictionary<string, Dictionary<string, LuaTexture[]>> Sprites { get; private set; } = new();
		public Dictionary<string, Dictionary<string, LuaSound>> Sounds { get; private set; } = new();
		public Dictionary<string, object> Config { get; private set; } = new(); 

		public LuaCharacter(string folder_name) {
			FolderName = folder_name;
			FullPath = Path.Combine(OpenTaiko.strEXEのあるフォルダ, TextureLoader.GLOBAL, TextureLoader.CHARACTERS, FolderName);
		}

		public void LoadCharacter() {
			#region Initialize
			// Textures
			string[] directories = Directory.GetDirectories(FullPath, "*", SearchOption.TopDirectoryOnly).Where(item => !item.EndsWith("Sounds")).ToArray();

			// Sounds
			string sound_path = Path.Combine(FullPath, "Sounds");
			string[] sound_dirs = Directory.GetDirectories(sound_path, "*", SearchOption.TopDirectoryOnly);

			// Config
			string config_path = Path.Combine(FullPath, "CharaConfig.txt");
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
		private Dictionary<string, object> LoadConfigFile(string file_path) {
			// Default values pulled from TextureLoader.cs, for legacy support
			Dictionary<string, object> chara_config = new() {
				{"Chara_Resolution", new int[] {1280, 720}},
				// positions
				{"Heya_Chara_Render_Offset", new int[] {0, 0}},
				{"Characters_Menu_Offset", new int[] {0, 0}},
				{"Characters_Result_Offset", new int[] {0, 0}},
				{"Game_Chara_X", new int[] {0, 0}},
				{"Game_Chara_Y", new int[] {0, 537}},
				{"Game_Chara_4P", new int[] {165, 68}},
				{"Game_Chara_5P", new int[] {165, 40}},
				{"Game_Chara_X_AI", new int[] {472, 602}},
				{"Game_Chara_Y_AI", new int[] {152, 152}},
				{"Game_Chara_Balloon_X", new int[] {240, 240, 0, 0}},
				{"Game_Chara_Balloon_Y", new int[] {0, 297, 0, 0}},
				{"Game_Chara_Balloon_4P", new int[] {0, -176}},
				{"Game_Chara_Balloon_5P", new int[] {0, -168}},
				{"Game_Chara_Kusudama_X", new int[] {290, 690, 90, 890, 490}},
				{"Game_Chara_Kusudama_Y", new int[] {420, 420, 420, 420, 420}},
				// keyframes
				{"Game_Chara_Motion_Normal", new int[] {}},
				{"Game_Chara_Motion_10Combo", new int[] {}},
				{"Game_Chara_Motion_10Combo_Clear", new int[] {}},
				{"Game_Chara_Motion_10Combo_Max", new int[] {}},
				{"Game_Chara_Motion_Miss", new int[] {}},
				{"Game_Chara_Motion_MissDown", new int[] {}},
				{"Game_Chara_Motion_ClearIn", new int[] {}},
				{"Game_Chara_Motion_Clear", new int[] {}},
				{"Game_Chara_Motion_ClearMax", new int[] {}},
				{"Game_Chara_Motion_MissIn", new int[] {}},
				{"Game_Chara_Motion_MissDownIn", new int[] {}},
				{"Game_Chara_Motion_GoGoStart", new int[] {}},
				{"Game_Chara_Motion_GoGoStart_Clear", new int[] {}},
				{"Game_Chara_Motion_GoGoStart_Max", new int[] {}},
				{"Game_Chara_Motion_GoGo", new int[] {}},
				{"Game_Chara_Motion_GoGo_Max", new int[] {}},
				{"Game_Chara_Motion_SoulIn", new int[] {}},
				{"Game_Chara_Motion_SoulOut", new int[] {}},
				{"Game_Chara_Motion_ClearOut", new int[] {}},
				{"Game_Chara_Motion_Return", new int[] {}},
				{"Game_Chara_Motion_Tower_Standing", new int[] {}},
				{"Game_Chara_Motion_Tower_Climbing", new int[] {}},
				{"Game_Chara_Motion_Tower_Running", new int[] {}},
				{"Game_Chara_Motion_Tower_Clear", new int[] {}},
				{"Game_Chara_Motion_Tower_Fail", new int[] {}},
				{"Game_Chara_Motion_Tower_Standing_Tired", new int[] {}},
				{"Game_Chara_Motion_Tower_Climbing_Tired", new int[] {}},
				{"Game_Chara_Motion_Tower_Running_Tired", new int[] {}},
				{"Game_Chara_Motion_Tower_Clear_Tired", new int[] {}},
				// beats
				{"Game_Chara_Beat_Normal", 1.0f },
				{"Game_Chara_Beat_10Combo", 1.5f },
				{"Game_Chara_Beat_10ComboMax", 1.5f },
				{"Game_Chara_Beat_Miss", 1.0f },
				{"Game_Chara_Beat_MissIn", 1.5f },
				{"Game_Chara_Beat_MissDown", 1.0f },
				{"Game_Chara_Beat_MissDownIn", 1.5f },
				{"Game_Chara_Beat_ClearIn", 1.5f },
				{"Game_Chara_Beat_Clear", 2.0f },
				{"Game_Chara_Beat_ClearMax", 1.5f },
				{"Game_Chara_Beat_GoGoStart", 1.5f },
				{"Game_Chara_Beat_GoGoStartClear", 1.5f },
				{"Game_Chara_Beat_GoGoStartMax", 1.5f },
				{"Game_Chara_Beat_GoGo", 2.0f },
				{"Game_Chara_Beat_GoGoMax", 2.0f },
				{"Game_Chara_Beat_SoulIn", 1.5f },
				{"Game_Chara_Beat_SoulOut", 1.5f },
				{"Game_Chara_Beat_ClearOut", 1.5f },
				{"Game_Chara_Beat_Return", 1.5f },
				{"Game_Chara_Beat_Tower_Standing", 1.0f },
				{"Game_Chara_Beat_Tower_Standing_Tired", 1.0f },
				{"Game_Chara_Beat_Tower_Fail", 1.0f },
				{"Game_Chara_Beat_Tower_Clear", 1.0f },
				{"Game_Chara_Beat_Tower_Clear_Tired", 1.0f },
				// "booleans"
				{"Result_UseResult1P", 0},
				{"Game_Chara_Tower_Clear_IsLooping", 0},
				{"Game_Chara_Tower_Clear_Tired_IsLooping", 0},
				{"Game_Chara_Tower_Fail_IsLooping", 0},
				// duration (ms)
				{"Game_Chara_Balloon_Timer", 28},
				{"Game_Chara_Balloon_Delay", 500},
				{"Game_Chara_Balloon_FadeOut", 84},
				{"Chara_Entry_AnimationDuration", 1000},
				{"Chara_Normal_AnimationDuration", 1000},
				{"Chara_Menu_Loop_AnimationDuration", 1000},
				{"Chara_Menu_Select_AnimationDuration", 1000},
				{"Chara_Menu_Start_AnimationDuration", 1000},
				{"Chara_Menu_Wait_AnimationDuration", 1000},
				{"Chara_Result_Normal_AnimationDuration", 1000},
				{"Chara_Result_Clear_AnimationDuration", 1000},
				{"Chara_Result_Failed_In_AnimationDuration", 1000},
				{"Chara_Result_Failed_AnimationDuration", 1000},
			};

			return chara_config;
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
