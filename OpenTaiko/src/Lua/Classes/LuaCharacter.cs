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

		public bool LoadAnimationGroup(string category = "") {
			try {
				if (category == "Sounds") return false;
				if (string.IsNullOrWhiteSpace(category)) category = "";

				string path = FullPath;
				if (!string.IsNullOrWhiteSpace(category)) path = Path.Combine(FullPath, category);
				var dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).Where(dir => !dir.EndsWith("Sounds"));

				return dirs.All(dir => LoadAnimation(Path.GetRelativePath(path, dir), category));
			} catch { return false; }
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
				} else
					Sprites[category].Add(name, LoadLuaTextureArray(path));

				return true;
			} catch { return false; }
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
			} catch { return false; }
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
			} catch { return false; }
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
	public class LuaCharacterFunc {
		public LuaCharacterFunc() { }
		public string ANIM_PREVIEW => CCharacter.ANIM_PREVIEW;
		public string ANIM_RENDER => CCharacter.ANIM_RENDER;

		public string ANIM_GAME_NORMAL => CCharacter.ANIM_GAME_NORMAL;
		public string ANIM_GAME_CLEAR => CCharacter.ANIM_GAME_CLEAR;
		public string ANIM_GAME_MAX => CCharacter.ANIM_GAME_MAX;
		public string ANIM_GAME_GOGO => CCharacter.ANIM_GAME_GOGO;
		public string ANIM_GAME_GOGO_MAX => CCharacter.ANIM_GAME_GOGO_MAX;
		public string ANIM_GAME_MISS => CCharacter.ANIM_GAME_MISS;
		public string ANIM_GAME_MISS_DOWN => CCharacter.ANIM_GAME_MISS_DOWN;
		public string ANIM_GAME_10COMBO => CCharacter.ANIM_GAME_10COMBO;
		public string ANIM_GAME_10COMBO_MAX => CCharacter.ANIM_GAME_10COMBO_MAX;
		public string ANIM_GAME_CLEARED => CCharacter.ANIM_GAME_CLEARED;
		public string ANIM_GAME_FAILED => CCharacter.ANIM_GAME_FAILED;
		public string ANIM_GAME_CLEAR_OUT => CCharacter.ANIM_GAME_CLEAR_OUT;
		public string ANIM_GAME_CLEAR_IN => CCharacter.ANIM_GAME_CLEAR_IN;
		public string ANIM_GAME_MAX_OUT => CCharacter.ANIM_GAME_MAX_OUT;
		public string ANIM_GAME_MAX_IN => CCharacter.ANIM_GAME_MAX_IN;
		public string ANIM_GAME_MISS_IN => CCharacter.ANIM_GAME_MISS_IN;
		public string ANIM_GAME_MISS_DOWN_IN => CCharacter.ANIM_GAME_MISS_DOWN_IN;
		public string ANIM_GAME_RETURN => CCharacter.ANIM_GAME_RETURN;
		public string ANIM_GAME_GOGOSTART => CCharacter.ANIM_GAME_GOGOSTART;
		public string ANIM_GAME_GOGOSTART_CLEAR => CCharacter.ANIM_GAME_GOGOSTART_CLEAR;
		public string ANIM_GAME_GOGOSTART_MAX => CCharacter.ANIM_GAME_GOGOSTART_MAX;
		public string ANIM_GAME_BALLOON_BREAKING => CCharacter.ANIM_GAME_BALLOON_BREAKING;
		public string ANIM_GAME_BALLOON_BROKE => CCharacter.ANIM_GAME_BALLOON_BROKE;
		public string ANIM_GAME_BALLOON_MISS => CCharacter.ANIM_GAME_BALLOON_MISS;
		public string ANIM_GAME_KUSUDAMA_BREAKING => CCharacter.ANIM_GAME_KUSUDAMA_BREAKING;
		public string ANIM_GAME_KUSUDAMA_BROKE => CCharacter.ANIM_GAME_KUSUDAMA_BROKE;
		public string ANIM_GAME_KUSUDAMA_MISS => CCharacter.ANIM_GAME_KUSUDAMA_MISS;
		public string ANIM_GAME_KUSUDAMA_IDLE => CCharacter.ANIM_GAME_KUSUDAMA_IDLE;

		public string ANIM_GAME_TOWER_STANDING = CCharacter.ANIM_GAME_TOWER_STANDING;
		public string ANIM_GAME_TOWER_STANDING_TIRED = CCharacter.ANIM_GAME_TOWER_STANDING_TIRED;
		public string ANIM_GAME_TOWER_CLIMBING = CCharacter.ANIM_GAME_TOWER_CLIMBING;
		public string ANIM_GAME_TOWER_CLIMBING_TIRED = CCharacter.ANIM_GAME_TOWER_CLIMBING_TIRED;
		public string ANIM_GAME_TOWER_RUNNING = CCharacter.ANIM_GAME_TOWER_RUNNING;
		public string ANIM_GAME_TOWER_RUNNING_TIRED = CCharacter.ANIM_GAME_TOWER_RUNNING_TIRED;
		public string ANIM_GAME_TOWER_CLEAR = CCharacter.ANIM_GAME_TOWER_CLEAR;
		public string ANIM_GAME_TOWER_CLEAR_TIRED = CCharacter.ANIM_GAME_TOWER_CLEAR_TIRED;
		public string ANIM_GAME_TOWER_FAIL = CCharacter.ANIM_GAME_TOWER_FAIL;

		public string ANIM_MENU_WAIT => CCharacter.ANIM_MENU_WAIT;
		public string ANIM_MENU_START => CCharacter.ANIM_MENU_START;
		public string ANIM_MENU_NORMAL => CCharacter.ANIM_MENU_NORMAL;
		public string ANIM_MENU_SELECT => CCharacter.ANIM_MENU_SELECT;
		public string ANIM_ENTRY_NORMAL => CCharacter.ANIM_ENTRY_NORMAL;
		public string ANIM_ENTRY_JUMP => CCharacter.ANIM_ENTRY_JUMP;

		public string ANIM_RESULT_NORMAL => CCharacter.ANIM_RESULT_NORMAL;
		public string ANIM_RESULT_CLEAR => CCharacter.ANIM_RESULT_CLEAR;
		public string ANIM_RESULT_FAILED_IN => CCharacter.ANIM_RESULT_FAILED_IN;
		public string ANIM_RESULT_FAILED => CCharacter.ANIM_RESULT_FAILED;

		public string VOICE_END_FAILED => CCharacter.VOICE_END_FAILED;
		public string VOICE_END_CLEAR => CCharacter.VOICE_END_CLEAR;
		public string VOICE_END_FULLCOMBO => CCharacter.VOICE_END_FULLCOMBO;
		public string VOICE_END_ALLPERFECT => CCharacter.VOICE_END_ALLPERFECT;
		public string VOICE_END_AIBATTLE_WIN => CCharacter.VOICE_END_AIBATTLE_WIN;
		public string VOICE_END_AIBATTLE_LOSE => CCharacter.VOICE_END_AIBATTLE_LOSE;

		public string VOICE_MENU_SONGSELECT => CCharacter.VOICE_MENU_SONGSELECT;
		public string VOICE_MENU_SONGDECIDE => CCharacter.VOICE_MENU_SONGDECIDE;
		public string VOICE_MENU_SONGDECIDE_AI => CCharacter.VOICE_MENU_SONGDECIDE_AI;
		public string VOICE_MENU_DIFFSELECT => CCharacter.VOICE_MENU_DIFFSELECT;
		public string VOICE_MENU_DANSELECTSTART => CCharacter.VOICE_MENU_DANSELECTSTART;
		public string VOICE_MENU_DANSELECTPROMPT => CCharacter.VOICE_MENU_DANSELECTPROMPT;
		public string VOICE_MENU_DANSELECTCONFIRM => CCharacter.VOICE_MENU_DANSELECTCONFIRM;

		public string VOICE_TITLE_SANKA => CCharacter.VOICE_TITLE_SANKA;

		public string VOICE_TOWER_MISS => CCharacter.VOICE_TOWER_MISS;

		public string VOICE_RESULT_BESTSCORE => CCharacter.VOICE_RESULT_BESTSCORE;
		public string VOICE_RESULT_CLEARFAILED => CCharacter.VOICE_RESULT_CLEARFAILED;
		public string VOICE_RESULT_CLEARSUCCESS => CCharacter.VOICE_RESULT_CLEARSUCCESS;
		public string VOICE_RESULT_DANFAILED => CCharacter.VOICE_RESULT_DANFAILED;
		public string VOICE_RESULT_DANREDPASS => CCharacter.VOICE_RESULT_DANREDPASS;
		public string VOICE_RESULT_DANGOLDPASS => CCharacter.VOICE_RESULT_DANGOLDPASS;
	}
}
