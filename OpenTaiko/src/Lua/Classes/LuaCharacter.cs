namespace OpenTaiko {
	/// <summary>
	/// Lua-side wrapper around a <see cref="CCharacter"/> instance.
	/// <para>
	/// Two modes:<br/>
	/// - <b>Player-bound</b>: constructed with a player index; the underlying <see cref="CCharacter"/>
	///   is resolved dynamically via <see cref="CCharacter.GetCharacter"/> so it stays correct after
	///   character changes. The 5 permanent per-player instances live in <see cref="TextureLoader.PlayerCharacters"/>.<br/>
	/// - <b>Name-bound</b>: constructed with a character folder name; owns a dedicated
	///   <see cref="CCharacterLua"/> and should be disposed when no longer needed.
	///   All operations use player slot 0 internally.
	/// </para>
	/// </summary>
	public class LuaCharacter : IDisposable {
		// Player-bound mode (-1 means not player-bound)
		private readonly int _player;

		// Name-bound mode (non-null means we own this instance)
		private CCharacterLua? _ownedCharacter;

		private CCharacter? Character => _player >= 0
			? CCharacter.GetCharacter(_player)
			: _ownedCharacter;

		// Slot used for all CCharacter calls in name-bound mode
		// NOTE: I'd like CCharacter.Draw to not have a player number ideally, will think about how to design this
		private int Slot => _player >= 0 ? _player : 0;

		public bool IsValid => Character != null;

		public string FolderName => Character?.dirName ?? "";
		public string FullPath => Character?._path ?? "";

		#region [Animation]

		public void Draw(float x, float y, string animation, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255) {
			Character?.Draw(Slot, animation, x, y, scaleX, scaleY, opacity, null, false);
		}

		public void DrawAtAnchor(float x, float y, string animation, string anchor = "bottom", float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255) {
			Character?.DrawAtAnchor(Slot, animation, x, y, anchor, scaleX, scaleY, opacity, null, false);
		}

		public bool Update(string animation, bool looping = true) {
			return Character?.Update(Slot, animation, looping) ?? false;
		}

		public void LoadAnimation(string animation) {
			Character?.LoadAnimation(Slot, animation);
		}

		public void DisposeAnimation(string animation) {
			Character?.DisposeAnimation(Slot, animation);
		}

		public bool AvailableAnimation(string animation) {
			return Character?.AvaiableAnimation(Slot, animation) ?? false;
		}

		public void SetAnimationDuration(string animation, double duration) {
			Character?.SetAnimationDuration(Slot, animation, duration);
		}

		public void SetAnimationCyclesFromBPM(string animation, double bpm) {
			Character?.SetAnimationCyclesFromBPM(Slot, animation, bpm);
		}

		public void ResetAnimationCounter(string animation) {
			Character?.ResetAnimationCounter(Slot, animation);
		}

		#endregion

		#region [Voice]

		public void LoadVoice(string voice) {
			Character?.LoadVoice(Slot, voice);
		}

		public void DisposeVoice(string voice) {
			Character?.DisposeVoice(Slot, voice);
		}

		public void PlayVoice(string voice) {
			Character?.PlayVoice(Slot, voice);
		}

		#endregion

		#region [Constructors]

		/// <summary>Player-bound constructor. The character resolves dynamically from the player's save data.</summary>
		public LuaCharacter(int player) {
			_player = player;
			_ownedCharacter = null;
		}

		/// <summary>
		/// Name-bound constructor. Tries to build a standalone <see cref="CCharacterLua"/> for the
		/// given character folder name. Check <see cref="IsValid"/> before use.
		/// Call <see cref="Dispose"/> when done.
		/// </summary>
		public LuaCharacter(string characterName) {
			_player = -1;
			string path = Path.Combine(
				OpenTaiko.strEXEのあるフォルダ,
				TextureLoader.GLOBAL,
				TextureLoader.CHARACTERS,
				characterName);
			try {
				if (Directory.Exists(path))
					_ownedCharacter = new CCharacterLua(path, -1);
			} catch {
				_ownedCharacter = null;
			}
		}

		#endregion

		#region [Dispose]

		private bool _disposed = false;

		public void Dispose() {
			if (_disposed) return;
			_disposed = true;
			// Only dispose characters we own (name-bound mode)
			_ownedCharacter?.Dispose();
			_ownedCharacter = null;
		}

		#endregion
	}

	public class LuaCharacterFunc {
		public LuaCharacterFunc() { }

		public LuaCharacter CreateCharacter(string characterName) {
			return new LuaCharacter(characterName);
		}

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

		public string ANIM_GAME_TOWER_STANDING => CCharacter.ANIM_GAME_TOWER_STANDING;
		public string ANIM_GAME_TOWER_STANDING_TIRED => CCharacter.ANIM_GAME_TOWER_STANDING_TIRED;
		public string ANIM_GAME_TOWER_CLIMBING => CCharacter.ANIM_GAME_TOWER_CLIMBING;
		public string ANIM_GAME_TOWER_CLIMBING_TIRED => CCharacter.ANIM_GAME_TOWER_CLIMBING_TIRED;
		public string ANIM_GAME_TOWER_RUNNING => CCharacter.ANIM_GAME_TOWER_RUNNING;
		public string ANIM_GAME_TOWER_RUNNING_TIRED => CCharacter.ANIM_GAME_TOWER_RUNNING_TIRED;
		public string ANIM_GAME_TOWER_CLEAR => CCharacter.ANIM_GAME_TOWER_CLEAR;
		public string ANIM_GAME_TOWER_CLEAR_TIRED => CCharacter.ANIM_GAME_TOWER_CLEAR_TIRED;
		public string ANIM_GAME_TOWER_FAIL => CCharacter.ANIM_GAME_TOWER_FAIL;

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
