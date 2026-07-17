using FDK;
using NLua;

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

		// Name-bound mode (non-null means we reference this instance)
		private CCharacterLua? _ownedCharacter;
		// False for non-owning wrappers — Dispose() will not destroy the underlying character.
		private readonly bool _ownsCharacter;

		private CCharacter? Character => _player >= 0
			? CCharacter.GetCharacter(_player)
			: _ownedCharacter;

		public bool IsValid => Character != null;

		public string FolderName => Character?.info.dirName ?? "";
		public string FullPath => Character?.info._path ?? "";
		public string DisplayName => Character?.info.metadata?.tGetName() ?? FolderName;

		// ── Character-scope visual state ─────────────────────────────────────────
		// All values are applied just before each draw call; frame textures are
		// restored afterwards (opacity→1, rotation→0, blendMode→"normal") so they
		// stay clean for other users.

		private int   _opacity   = 255;
		private float _scaleX    = 1f;
		private float _scaleY    = 1f;
		private float _colorR    = 1f;
		private float _colorG    = 1f;
		private float _colorB    = 1f;
		private float _rotation  = 0f;
		private string _blendMode = "normal";
		private string _wrapMode  = "edge";

		// Non-owning reference into PaletteManager's global cache.
		private PaletteGradientEntry? _paletteEntry;

		public void SetPaletteGradient(LuaTable? stops, float blend = 1.0f) {
			_paletteEntry = null;
			if (stops == null) {
				if (_player >= 0) PaletteManager.SetSlot(_player, null);
				return;
			}
			var list = PaletteManager.ParseLuaStops(stops);
			if (list.Count < 2) return;
			_paletteEntry = PaletteManager.GetOrCreate(PaletteManager.BuildCacheKey(list, blend), list, blend);
			if (_player >= 0) PaletteManager.SetSlot(_player, _paletteEntry);
		}

		public void ClearPaletteGradient() {
			_paletteEntry = null;
			if (_player >= 0) PaletteManager.SetSlot(_player, null);
		}


		// ── Setters ──────────────────────────────────────────────────────────────

		/// <summary>Sets draw opacity (0.0 = transparent, 1.0 = opaque). Multiplied with per-call opacity.</summary>
		public void SetOpacity(float opacity)                  => _opacity   = Math.Clamp((int)(opacity * 255), 0, 255);
		/// <summary>Sets scale applied before every draw call. Use negative X to mirror horizontally.</summary>
		public void SetScale(float scaleX, float scaleY)       { _scaleX = scaleX; _scaleY = scaleY; }
		/// <summary>Sets RGB colour tint (0.0–1.0 per channel).</summary>
		public void SetColor(LuaColor color)                   { _colorR = color.R / 255f; _colorG = color.G / 255f; _colorB = color.B / 255f; }
		/// <inheritdoc cref="SetColor(LuaColor)"/>
		public void SetColor(float r, float g, float b)        { _colorR = r; _colorG = g; _colorB = b; }
		/// <summary>Sets rotation in degrees.</summary>
		public void SetRotation(float degrees)                 => _rotation  = degrees;
		/// <summary>Sets blend mode: "normal", "add", "multi", "sub", "screen".</summary>
		public void SetBlendMode(string mode)                  => _blendMode = mode;
		/// <summary>Sets texture wrap mode: "edge", "border", "repeat", "mirror".</summary>
		public void SetWrapMode(string mode)                   => _wrapMode  = mode;

		// ── Getters ──────────────────────────────────────────────────────────────

		public LuaVector2 GetScale()                           => new(_scaleX, _scaleY);
		public (float Red, float Green, float Blue) GetColor() => (_colorR, _colorG, _colorB);
		public float  GetRotation()                            => _rotation;
		public string GetBlendMode()                           => _blendMode;
		public string GetWrapMode()                            => _wrapMode;

		// ── Helpers ──────────────────────────────────────────────────────────────

		/// <summary>Combines the stored opacity with a per-call opacity (multiplicative).</summary>
		private int BlendOpacity(int callOpacity) => (int)Math.Round(_opacity * (callOpacity / 255.0));

		private Color4 StoredColor() => new(_colorR, _colorG, _colorB, 1f);

		#region [Animation]

		// Resolve the gradient for this draw call:
		// - player-bound: resolve dynamically through the virtual slot system (save-scoped)
		// - name-bound (_player < 0): only use _paletteEntry; never bleed a slot from another player
		private LuaGradientMap? DrawGradient => _player >= 0
			? PaletteManager.GetEffectivePalette(_player)?.LuaMap
			: _paletteEntry?.LuaMap;

		public void Draw(float x, float y, string animation, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255) {
			Character?.Draw(animation, x, y, _scaleX * scaleX, _scaleY * scaleY, BlendOpacity(opacity), StoredColor(), _rotation, _blendMode, _wrapMode, DrawGradient);
		}

		public void DrawAtAnchor(float x, float y, string animation, string anchor = "bottom", float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255) {
			Character?.DrawAtAnchor(animation, x, y, anchor, _scaleX * scaleX, _scaleY * scaleY, BlendOpacity(opacity), StoredColor(), null, null, 0f, 0f, _rotation, _blendMode, _wrapMode, DrawGradient);
		}

		/// <summary>Draws the character using the top-left corner of a layout rect as the origin.</summary>
		public void DrawRect(float rect_x, float rect_y, float rect_w, float rect_h, string animation, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255) {
			Character?.Draw(animation, rect_x, rect_y, _scaleX * scaleX, _scaleY * scaleY, BlendOpacity(opacity), StoredColor(), _rotation, _blendMode, _wrapMode, DrawGradient);
		}

		/// <summary>
		/// Draws the character clipped to a rect whose top-left is at (<paramref name="x"/>, <paramref name="y"/>).
		/// Scale, colour, rotation and blend mode come from the stored character state (set via SetXxx methods).
		/// The caller is responsible for computing the exact draw position.
		/// </summary>
		public void DrawRectAtAnchor(float x, float y, float clip_w, float clip_h, string animation, int opacity = 255, float clipX = 0f, float clipY = 0f) {
			Character?.DrawAtAnchor(animation, x, y, "topleft", _scaleX, _scaleY, BlendOpacity(opacity), StoredColor(), clip_w, clip_h, clipX, clipY, _rotation, _blendMode, _wrapMode, DrawGradient);
		}

		public bool Update(string animation, bool looping = true) {
			return Character?.Update(animation, looping) ?? false;
		}

		public void LoadAnimation(string animation) {
			Character?.LoadAnimation(animation);
		}

		public void DisposeAnimation(string animation) {
			Character?.DisposeAnimation(animation);
		}

		public bool AvailableAnimation(string animation) {
			return Character?.AvailableAnimation(animation) ?? false;
		}

		public void SetAnimationDuration(string animation, double duration) {
			Character?.SetAnimationDuration(animation, duration);
		}

		public void SetAnimationCyclesFromBPM(string animation, double bpm) {
			Character?.SetAnimationCyclesFromBPM(animation, bpm);
		}

		public void ResetAnimationCounter(string animation) {
			Character?.ResetAnimationCounter(animation);
		}

		/// <summary>Returns the drawn dimensions of the current animation frame scaled to the theme resolution. Returns (X=0, Y=0) if unavailable.</summary>
		public LuaVector2 GetAnimationSize(string animation) {
			return Character?.GetDrawSize(animation) ?? new LuaVector2(0, 0);
		}

		#endregion

		#region [Voice]

		public void LoadVoice(string voice) {
			Character?.LoadVoice(voice);
		}

		public void DisposeVoice(string voice) {
			Character?.DisposeVoice(voice);
		}

		public void PlayVoice(string voice) {
			Character?.PlayVoice(voice);
		}

		#endregion

		#region [Constructors]

		/// <summary>Player-bound constructor. The character resolves dynamically from the player's save data.</summary>
		public LuaCharacter(int player) {
			_player = player;
			_ownedCharacter = null;
			_ownsCharacter = false;
			_paletteEntry = PaletteManager.GetSlot(player);  // restore any previously set palette
		}

		/// <summary>
		/// Name-bound constructor. Tries to build a standalone <see cref="CCharacterLua"/> for the
		/// given character folder name. Check <see cref="IsValid"/> before use.
		/// Call <see cref="Dispose"/> when done.
		/// </summary>
		public LuaCharacter(string characterName) {
			_player = -1;
			_ownsCharacter = true;
			string path = Path.Combine(
				OpenTaiko.strEXEFolder,
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

		/// <summary>
		/// Non-owning constructor. Wraps an existing <see cref="CCharacterLua"/> without taking
		/// ownership — <see cref="Dispose"/> will not destroy the underlying character.
		/// Uses player slot 0 for all draw operations.
		/// </summary>
		internal LuaCharacter(CCharacterLua character) {
			_player = -1;
			_ownedCharacter = character;
			_ownsCharacter = false;
		}

		#endregion

		#region [Dispose]

		private bool _disposed = false;

		public void Dispose() {
			if (_disposed) return;
			_disposed = true;
			_paletteEntry = null;  // cached globally, not owned — never disposed here
			if (_ownsCharacter)
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

		/// <summary>Returns a player-bound <see cref="LuaCharacter"/> for the given 0-based player index.
		/// The object resolves the active character dynamically and does not need to be disposed.</summary>
		public LuaCharacter GetPlayerCharacter(int player) {
			return new LuaCharacter(player);
		}

		/// <summary>
		/// Returns the active palette gradient map for the given player slot, or null if no palette is set.
		/// Use with <c>GRADIENT:SetActive</c>/<c>GRADIENT:ClearActive</c> to apply the palette to arbitrary textures.
		/// </summary>
		public LuaGradientMap? GetPlayerGradientMap(int player) {
			return PaletteManager.GetEffectivePalette(player)?.LuaMap;
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
