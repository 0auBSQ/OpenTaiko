using System.ArrayExtensions;
using System.Collections.Frozen;
using FDK;

namespace OpenTaiko;

abstract class CCharacter : IDisposable {
	public const string ANIM_NONE = "None";
	public const string ANIM_PREVIEW = "Preview";
	public const string ANIM_RENDER = "Render";

	public const string ANIM_GAME_NORMAL = "Game/Normal";
	public const string ANIM_GAME_CLEAR = "Game/Clear";
	public const string ANIM_GAME_MAX = "Game/Max";
	public const string ANIM_GAME_GOGO = "Game/Gogo";
	public const string ANIM_GAME_GOGO_MAX = "Game/Gogo_Max";
	public const string ANIM_GAME_MISS = "Game/Miss";
	public const string ANIM_GAME_MISS_DOWN = "Game/Miss_Down";
	public const string ANIM_GAME_10COMBO = "Game/10combo";
	public const string ANIM_GAME_10COMBO_MAX = "Game/10combo_Max";
	public const string ANIM_GAME_CLEARED = "Game/Cleared";
	public const string ANIM_GAME_FAILED = "Game/Failed";
	public const string ANIM_GAME_CLEAR_OUT = "Game/Clear_Out";
	public const string ANIM_GAME_CLEAR_IN = "Game/Clear_In";
	public const string ANIM_GAME_MAX_OUT = "Game/Max_Out";
	public const string ANIM_GAME_MAX_IN = "Game/Max_In";
	public const string ANIM_GAME_MISS_IN = "Game/Miss_In";
	public const string ANIM_GAME_MISS_DOWN_IN = "Game/Miss_Down_In";
	public const string ANIM_GAME_RETURN = "Game/Return";
	public const string ANIM_GAME_GOGOSTART = "Game/GoGoStart";
	public const string ANIM_GAME_GOGOSTART_CLEAR = "Game/GoGoStart_Clear";
	public const string ANIM_GAME_GOGOSTART_MAX = "Game/GoGoStart_Max";
	public const string ANIM_GAME_BALLOON_BREAKING = "Game/Balloon_Breaking";
	public const string ANIM_GAME_BALLOON_BROKE = "Game/Balloon_Broke";
	public const string ANIM_GAME_BALLOON_MISS = "Game/Balloon_Miss";
	public const string ANIM_GAME_KUSUDAMA_BREAKING = "Game/Kusudama_Breaking";
	public const string ANIM_GAME_KUSUDAMA_BROKE = "Game/Kusudama_Broke";
	public const string ANIM_GAME_KUSUDAMA_MISS = "Game/Kusudama_Miss";
	public const string ANIM_GAME_KUSUDAMA_IDLE = "Game/Kusudama_Idle";

	public const string ANIM_GAME_TOWER_STANDING = "Game/Tower/Standing";
	public const string ANIM_GAME_TOWER_STANDING_TIRED = "Game/Tower/Standing_Tired";
	public const string ANIM_GAME_TOWER_CLIMBING = "Game/Tower/Climbing";
	public const string ANIM_GAME_TOWER_CLIMBING_TIRED = "Game/Tower/Climbing_Tired";
	public const string ANIM_GAME_TOWER_RUNNING = "Game/Tower/Running";
	public const string ANIM_GAME_TOWER_RUNNING_TIRED = "Game/Tower/Running_Tired";
	public const string ANIM_GAME_TOWER_CLEAR = "Game/Tower/Clear";
	public const string ANIM_GAME_TOWER_CLEAR_TIRED = "Game/Tower/Clear_Tired";
	public const string ANIM_GAME_TOWER_FAIL = "Game/Tower/Fail";

	public const string ANIM_MENU_WAIT = "Menu/Wait";
	public const string ANIM_MENU_START = "Menu/Start";
	public const string ANIM_MENU_NORMAL = "Menu/Normal";
	public const string ANIM_MENU_SELECT = "Menu/Select";
	public const string ANIM_ENTRY_NORMAL = "Entry/Normal";
	public const string ANIM_ENTRY_JUMP = "Entry/Jump";

	public const string ANIM_RESULT_NORMAL = "Result/Normal";
	public const string ANIM_RESULT_CLEAR = "Result/Clear";
	public const string ANIM_RESULT_FAILED_IN = "Result/Failed_In";
	public const string ANIM_RESULT_FAILED = "Result/Failed";

	public const string VOICE_END_FAILED = "End/Failed";
	public const string VOICE_END_CLEAR = "End/Clear";
	public const string VOICE_END_FULLCOMBO = "End/FullCombo";
	public const string VOICE_END_ALLPERFECT = "End/AllPerfect";
	public const string VOICE_END_AIBATTLE_WIN = "End/AIBattle_Win";
	public const string VOICE_END_AIBATTLE_LOSE = "End/AIBattle_Lose";

	public const string VOICE_MENU_SONGSELECT = "Menu/SongSelect";
	public const string VOICE_MENU_SONGDECIDE = "Menu/SongDecide";
	public const string VOICE_MENU_SONGDECIDE_AI = "Menu/SongDecide_AI";
	public const string VOICE_MENU_DIFFSELECT = "Menu/DiffSelect";
	public const string VOICE_MENU_DANSELECTSTART = "Menu/DanSelectStart";
	public const string VOICE_MENU_DANSELECTPROMPT = "Menu/DanSelectPrompt";
	public const string VOICE_MENU_DANSELECTCONFIRM = "Menu/DanSelectConfirm";

	public const string VOICE_TITLE_SANKA = "Title/Sanka";

	public const string VOICE_TOWER_MISS = "Tower/Miss";

	public const string VOICE_RESULT_BESTSCORE = "Result/BestScore";
	public const string VOICE_RESULT_CLEARFAILED = "Result/ClearFailed";
	public const string VOICE_RESULT_CLEARSUCCESS = "Result/ClearSuccess";
	public const string VOICE_RESULT_DANFAILED = "Result/DanFailed";
	public const string VOICE_RESULT_DANREDPASS = "Result/DanRedPass";
	public const string VOICE_RESULT_DANGOLDPASS = "Result/DanGoldPass";

	public static readonly FrozenDictionary<string, string> AlternativeAnimations = new Dictionary<string, string>() {
		{ ANIM_GAME_CLEAR, ANIM_GAME_NORMAL },
		{ ANIM_GAME_MAX, ANIM_GAME_CLEAR },
		{ ANIM_GAME_MISS, ANIM_GAME_NORMAL },
		{ ANIM_GAME_MISS_DOWN, ANIM_GAME_MISS },

		{ ANIM_GAME_GOGO, ANIM_GAME_NORMAL },
		{ ANIM_GAME_GOGO_MAX, ANIM_GAME_GOGO },

		{ ANIM_GAME_10COMBO_MAX, ANIM_GAME_10COMBO },
		{ ANIM_GAME_GOGOSTART_CLEAR, ANIM_GAME_GOGOSTART },
		{ ANIM_GAME_GOGOSTART_MAX, ANIM_GAME_GOGOSTART_CLEAR },

		{ ANIM_GAME_TOWER_STANDING_TIRED, ANIM_GAME_TOWER_STANDING },
		{ ANIM_GAME_TOWER_CLIMBING_TIRED, ANIM_GAME_TOWER_CLIMBING },
		{ ANIM_GAME_TOWER_RUNNING_TIRED, ANIM_GAME_TOWER_RUNNING },
		{ ANIM_GAME_TOWER_CLEAR_TIRED, ANIM_GAME_TOWER_CLEAR },
		{ ANIM_GAME_TOWER_FAIL, ANIM_GAME_TOWER_STANDING_TIRED },

		{ ANIM_GAME_KUSUDAMA_IDLE, ANIM_GAME_NORMAL },

		{ ANIM_MENU_WAIT, ANIM_GAME_GOGO},
		{ ANIM_MENU_START, ANIM_GAME_10COMBO },
		{ ANIM_MENU_NORMAL, ANIM_GAME_NORMAL},
		{ ANIM_MENU_SELECT, ANIM_GAME_10COMBO },
		{ ANIM_ENTRY_NORMAL, ANIM_GAME_NORMAL },
		{ ANIM_ENTRY_JUMP, ANIM_GAME_10COMBO },

		{ ANIM_RESULT_NORMAL, ANIM_GAME_NORMAL },
		{ ANIM_RESULT_CLEAR, ANIM_GAME_CLEAR },
		{ ANIM_RESULT_FAILED_IN, ANIM_GAME_MISS_IN },
		{ ANIM_RESULT_FAILED, ANIM_GAME_MISS }
	}.ToFrozenDictionary();

	public const int DEFAULT_DURATION = 500;
	public const int ALTERNATIVE_MAX_TRY = 5;

	// reference count of requested resources for previews
	protected readonly Dictionary<string, int> animationPreviewRefCounts = new();
	protected readonly Dictionary<string, int> voicePreviewRefCounts = new();

	// reference count of requested resources for players, only used for switching characters
	protected Dictionary<string, int> animationRefCounts = new();
	protected Dictionary<string, int> voiceRefCounts = new();

	// reference count of actually loaded resolved resources
	protected readonly Dictionary<string, int> animationResolvedLoadCounts = new();
	protected readonly Dictionary<string, int> voiceResolvedLoadCounts = new();

	public static CCharacter GetCharacter(int player) {
		int _charaId = CVirtualSlotManager.GetCharacterIndex(player);
		return OpenTaiko.Tx.Characters[_charaId][player];
	}


	private static void AddRemovePreviewResource(string resourceName, Action<CCharacter, string> addRemoveResource) {
		foreach (var characterLua in OpenTaiko.Tx.Characters)
			addRemoveResource(characterLua[0], resourceName); // use P1's instances for preview
	}

	private void LoadResource(string name, Dictionary<string, int> refCounts, Dictionary<string, int> resolvedLoadCounts,
		Func<CCharacter, string, bool> available, Func<CCharacter, string, string> getAlternative, Action<CCharacter, string> loadResolved,
		string? noneName = null, int count = 1
		) {
		refCounts.TryAdd(name, 0);
		refCounts[name] += count;
		// try alternatives until successfully loaded (available)
		for (int t = 0; t < ALTERNATIVE_MAX_TRY; ++t) {
			if (name == noneName)
				return;
			if (resolvedLoadCounts.TryAdd(name, 0))
				loadResolved(this, name);
			if (available(this, name)) {
				resolvedLoadCounts[name] += count;
				break;
			}
			resolvedLoadCounts.Remove(name);
			name = getAlternative(this, name);
		}
	}

	private void DisposeResource(string name, Dictionary<string, int> refCounts, Dictionary<string, int> resolvedLoadCounts,
		Func<CCharacter, string, string> resolveAlternatives, Action<CCharacter, string> disposeResolved,
		int count = 1
		) {
		if (refCounts.TryGetValue(name, out int prevRefCount)) {
			refCounts[name] -= count;
			if (prevRefCount <= count)
				refCounts.Remove(name);
		}
		var resolvedName = resolveAlternatives(this, name);
		if (resolvedLoadCounts.TryGetValue(resolvedName, out int prevResolvedLoadCount)) {
			resolvedLoadCounts[resolvedName] -= count;
			if (prevResolvedLoadCount <= count) {
				resolvedLoadCounts.Remove(resolvedName);
				disposeResolved(this, resolvedName);
			}
		}
	}


	public static void AddPreviewAnimation(string animationName) =>
		AddRemovePreviewResource(animationName, (chara, name) => chara.LoadAnimation(name, forPreview: true));
	public static void RemovePreviewAnimation(string animationName) =>
		AddRemovePreviewResource(animationName, (chara, name) => chara.DisposeAnimation(name, forPreview: true));

	public static void AddEssentialAnimation(int player, string animationName)
		=> GetCharacter(player).LoadAnimation(animationName);
	public static void RemoveEssentialAnimation(int player, string animationName)
		=> GetCharacter(player).DisposeAnimation(animationName);

	public void LoadAnimation(string animationName, int count = 1, bool forPreview = false)
		=> LoadResource(animationName, forPreview ? animationPreviewRefCounts : animationRefCounts, animationResolvedLoadCounts,
			(chara, name) => chara.AvailableResolvedAnimation(name), (chara, name) => GetAlternativeAnimation(name), (chara, name) => chara.LoadResolvedAnimation(name),
			noneName: ANIM_NONE, count: count);
	public void DisposeAnimation(string animationName, int count = 1, bool forPreview = false)
		=> DisposeResource(animationName, forPreview ? animationPreviewRefCounts : animationRefCounts, animationResolvedLoadCounts,
			(chara, name) => chara.GetAnimation(name), (chara, name) => chara.DisposeResolvedAnimation(name),
			count: count);


	public static void AddPreviewVoice(string voiceName) =>
		AddRemovePreviewResource(voiceName, (chara, name) => chara.LoadVoice(name, forPreview: true));
	public static void RemovePreviewVoice(string voiceName) =>
		AddRemovePreviewResource(voiceName, (chara, name) => chara.DisposeVoice(name, forPreview: true));

	public static void AddEssentialVoice(int player, string voiceName)
		=> GetCharacter(player).LoadVoice(voiceName);
	public static void RemoveEssentialVoice(int player, string voiceName)
		=> GetCharacter(player).DisposeVoice(voiceName);

	public void LoadVoice(string voiceName, int count = 1, bool forPreview = false)
		=> LoadResource(voiceName, forPreview ? voicePreviewRefCounts : voiceRefCounts, voiceResolvedLoadCounts,
			(chara, name) => true, (chara, name) => name, (chara, name) => chara.LoadResolvedVoice(name), count: count);
	public void DisposeVoice(string voiceName, int count = 1, bool forPreview = false)
		=> DisposeResource(voiceName, forPreview ? voicePreviewRefCounts : voiceRefCounts, voiceResolvedLoadCounts,
			(chara, name) => name, (chara, name) => chara.DisposeResolvedVoice(name), count: count);

	public record struct ResourceRefCounts(IReadOnlyDictionary<string, int> animation, IReadOnlyDictionary<string, int> voice);

	public void CharaLoad(ResourceRefCounts? refCounts) {
		if (refCounts == null)
			return;
		foreach (var (name, count) in refCounts.Value.animation)
			this.LoadAnimation(name, count);
		foreach (var (name, count) in refCounts.Value.voice)
			this.LoadVoice(name, count);
	}

	public ResourceRefCounts CharaUnload() {
		ResourceRefCounts res = new(animationRefCounts, voiceRefCounts);
		animationRefCounts = [];
		voiceRefCounts = [];
		foreach (var (name, count) in res.animation)
			this.DisposeAnimation(name, count);
		foreach (var (name, count) in res.voice)
			this.DisposeVoice(name, count);
		return res;
	}

	protected static string GetAlternativeAnimation(string animation) {
		if (!AlternativeAnimations.ContainsKey(animation)) return ANIM_NONE;
		string nextAnimation = AlternativeAnimations[animation];
		return nextAnimation;
	}

	protected string GetAnimation(string animation) {
		for (int i = 0; i < ALTERNATIVE_MAX_TRY; i++) {
			if (animation == ANIM_NONE || this.AvailableResolvedAnimation(animation))
				return animation;
			animation = GetAlternativeAnimation(animation);
		}
		return ANIM_NONE;
	}

	public bool AvailableAnimation(string animationType) {
		for (int i = 0; i < ALTERNATIVE_MAX_TRY; i++) {
			if (animationType == ANIM_NONE)
				return false;
			if (this.AvailableResolvedAnimation(animationType))
				return true;
			animationType = GetAlternativeAnimation(animationType);
		}
		return false;
	}

	public class Info {
		public DBCharacter.CharacterData metadata;
		public DBCharacter.CharacterEffect effect;
		public CUnlockCondition? unlock;
		public string _path;
		public int _idx;
		public string dirName;

		public float GetEffectCoinMultiplier(bool gaugeEnabled = true) {
			float mult = 1f;

			mult *= HRarity.tRarityToRarityToCoinMultiplier(metadata.Rarity);
			mult *= effect.GetCoinMultiplier(1f, gaugeEnabled: gaugeEnabled);

			return mult;
		}

		public void tGetUnlockedItems(int _player, ModalQueue mq) {
			int player = _player;
			var _sf = OpenTaiko.SaveFileInstances[player].data.UnlockedCharacters;
			bool _edited = false;

			if (!_sf.Contains(dirName)) {
				var _fulfilled = unlock?.tConditionMet(player, CUnlockCondition.EScreen.Internal).Item1 ?? false;

				if (_fulfilled) {
					_sf.Add(dirName);
					_edited = true;
					mq.tAddModal(
						new Modal(
							Modal.EModalType.Character,
							HRarity.tRarityToModalInt(metadata.Rarity),
							new LuaCharacter(dirName)
						),
						_player);

					DBSaves.RegisterStringUnlockedAsset(OpenTaiko.SaveFileInstances[player].data.SaveId, "unlocked_characters", dirName);
				}
			}

			if (_edited)
				OpenTaiko.SaveFileInstances[player].tApplyHeyaChanges();
		}

		public Info(string path, int i) {
			_path = path;
			dirName = Path.GetFileName(path);
			_idx = i;

			// Character metadata
			if (File.Exists($@"{path}{Path.DirectorySeparatorChar}Metadata.json"))
				metadata = ConfigManager.GetConfig<DBCharacter.CharacterData>($@"{path}{Path.DirectorySeparatorChar}Metadata.json");
			else
				metadata = new DBCharacter.CharacterData();

			// Character metadata
			if (File.Exists($@"{path}{Path.DirectorySeparatorChar}Effects.json"))
				effect = ConfigManager.GetConfig<DBCharacter.CharacterEffect>($@"{path}{Path.DirectorySeparatorChar}Effects.json");
			else
				effect = new DBCharacter.CharacterEffect();

			// Character unlockables
			if (File.Exists($@"{path}{Path.DirectorySeparatorChar}Unlock.json"))
				unlock = OpenTaiko.UnlockConditionFactory.GenerateUnlockObjectFromJsonPath($@"{path}{Path.DirectorySeparatorChar}Unlock.json");
			else
				unlock = null;
		}
	}

	public readonly Info info;

	public CCharacter(string path, int i) {
		info = new(path, i);
	}

	public CCharacter(Info info) {
		this.info = info;
	}

	public virtual void Dispose() {
			foreach (var (name, count) in animationResolvedLoadCounts)
				this.DisposeResolvedAnimation(name);
			foreach (var (name, count) in voiceResolvedLoadCounts)
				this.DisposeResolvedVoice(name);
		animationPreviewRefCounts.Clear();
		animationRefCounts.Clear();
		animationResolvedLoadCounts.Clear();
		voicePreviewRefCounts.Clear();
		voiceRefCounts.Clear();
		voiceResolvedLoadCounts.Clear();
	}

	public virtual bool Update(string animationType, bool looping = true) {
		return false;
	}

	public virtual void Draw(string animationType, float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, float rotation = 0f, string? blendMode = null, string? wrapMode = null, LuaGradientMap? gradientMap = null) {

	}

	public virtual void DrawAtAnchor(string animationType, float x, float y, string anchor, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, float? clipW = null, float? clipH = null, float clipX = 0f, float clipY = 0f, float rotation = 0f, string? blendMode = null, string? wrapMode = null, LuaGradientMap? gradientMap = null) {
		Draw(animationType, x, y, scaleX, scaleY, opacity, color, rotation, blendMode, wrapMode, gradientMap);
	}

	public virtual LuaVector2 GetDrawSize(string animationType) => new LuaVector2(0, 0);

	public virtual (float x, float y) GetHeyaRenderOffset() => (0f, 0f);

	/// <summary>
	/// Returns the character-specific AI battle base position (theme pixels) for
	/// <paramref name="player"/> (0-based), or <c>null</c> if the character defers to
	/// the skin's <c>Game_Chara_AI_X/Y</c> defaults.
	/// </summary>
	public virtual (float x, float y)? GetAIBattlePosition(int player, float charaScale = 1.0f) => null;

	protected virtual void LoadResolvedAnimation(string animationType) {
	}

	protected virtual void DisposeResolvedAnimation(string animationType) {
	}

	protected virtual bool AvailableResolvedAnimation(string animationType) {
		return false;
	}

	public virtual void SetAnimationDuration(string animationType, double duration) {

	}

	public virtual void ResetAnimationCounter(string animationType) {

	}

	public void SetAnimationCyclesFromBPM(string animationType, double bpm) {
		SetAnimationDuration(animationType, 60000 / Math.Abs(CTja.TjaBeatSpeedToGameBeatSpeed(bpm)));
	}

	//voice-------------

	protected virtual void LoadResolvedVoice(string voice) {
	}

	protected virtual void DisposeResolvedVoice(string voice) {
	}

	public virtual void PlayVoice(string voice) {

	}


	//------------------
}
