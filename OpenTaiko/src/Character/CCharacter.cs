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

	// reference trackers
	public static readonly Dictionary<string, int>[] PlayerToAnimationToRefCount = Enumerable.Range(0, OpenTaiko.MAX_PLAYERS)
		.Select(i => new Dictionary<string, int>())
		.ToArray();

	public static readonly Dictionary<string, int>[] PlayerToVoiceToRefCount = Enumerable.Range(0, OpenTaiko.MAX_PLAYERS)
		.Select(i => new Dictionary<string, int>())
		.ToArray();

	protected readonly Dictionary<string, int> animationLoadCounts = new();
	protected readonly Dictionary<string, int> voiceLoadCounts = new();

	public static CCharacter GetCharacter(int player) {
		int _charaId = CVirtualSlotManager.GetCharacterIndex(player);
		return OpenTaiko.Tx.Characters[_charaId][player];
	}


	private static void AddRemovePreviewResource(string resourceName, Action<CCharacter, string> addRemoveResource) {
		foreach (var characterLua in OpenTaiko.Tx.Characters)
			addRemoveResource(characterLua[0], resourceName); // use P1's instances for preview
		}

	private static void AddEssentialResource(int player, string resourceName, Dictionary<string, int>[] playerToResourceToRefCount, Action<CCharacter, string> addResource) {
		var resourceToRefCount = playerToResourceToRefCount[player];
		if (!resourceToRefCount.ContainsKey(resourceName))
			resourceToRefCount.Add(resourceName, 0);
		if (resourceToRefCount[resourceName] == 0)
			addResource(GetCharacter(player), resourceName);
		resourceToRefCount[resourceName]++;
	}

	private static void RemoveEssentialResource(int player, string resourceName, Dictionary<string, int>[] playerToResourceToRefCount, Action<CCharacter, string> removeResource) {
		var resourceToRefCount = playerToResourceToRefCount[player];
		if (!resourceToRefCount.ContainsKey(resourceName))
			return;

		resourceToRefCount[resourceName]--;
		if (resourceToRefCount[resourceName] == 0) {
			removeResource(GetCharacter(player), resourceName);
			resourceToRefCount.Remove(resourceName);
		}
	}

	private void AddResource(string resourceName, Dictionary<string, int> resourceLoadCounts, Action<CCharacter, string> loadResource, int count = 1) {
		if (!resourceLoadCounts.ContainsKey(resourceName))
			resourceLoadCounts.Add(resourceName, 0);
		if (resourceLoadCounts[resourceName] == 0)
			loadResource(this, resourceName);
		resourceLoadCounts[resourceName] += count;
	}

	private void RemoveResource(string resourceName, Dictionary<string, int> resourceLoadCounts, Action<CCharacter, string> disposeResource, int count = 1) {
		if (!resourceLoadCounts.ContainsKey(resourceName))
			return;
		resourceLoadCounts[resourceName] -= count;
		if (resourceLoadCounts[resourceName] <= 0) {
			disposeResource(this, resourceName);
			resourceLoadCounts.Remove(resourceName);
		}
	}


	public static void AddPreviewAnimation(string animationName) =>
		AddRemovePreviewResource(animationName, (chara, name) => chara.AddAnimation(name));
	public static void RemovePreviewAnimation(string animationName) =>
		AddRemovePreviewResource(animationName, (chara, name) => chara.RemoveAnimation(name));

	public static void AddEssentialAnimation(int player, string animationName)
		=> AddEssentialResource(player, animationName, PlayerToAnimationToRefCount, (chara, name) => chara.AddAnimation(name));
	public static void RemoveEssentialAnimation(int player, string animationName)
		=> RemoveEssentialResource(player, animationName, PlayerToAnimationToRefCount, (chara, name) => chara.RemoveAnimation(name));

	private void AddAnimation(string animationName, int count = 1)
		=> AddResource(animationName, animationLoadCounts, (chara, name) => chara.ImplLoadAnimation(name), count: count);
	private void RemoveAnimation(string animationName, int count = 1)
		=> RemoveResource(animationName, animationLoadCounts, (chara, name) => chara.ImplDisposeAnimation(name), count: count);


	public static void AddPreviewVoice(string voiceName) =>
		AddRemovePreviewResource(voiceName, (chara, name) => chara.AddVoice(name));
	public static void RemovePreviewVoice(string voiceName) =>
		AddRemovePreviewResource(voiceName, (chara, name) => chara.RemoveVoice(name));

	public static void AddEssentialVoice(int player, string voiceName)
		=> AddEssentialResource(player, voiceName, PlayerToVoiceToRefCount, (chara, name) => chara.AddVoice(name));
	public static void RemoveEssentialVoice(int player, string voiceName)
		=> RemoveEssentialResource(player, voiceName, PlayerToVoiceToRefCount, (chara, name) => chara.RemoveVoice(name));

	private void AddVoice(string voiceName, int count = 1)
		=> AddResource(voiceName, voiceLoadCounts, (chara, name) => chara.ImplLoadVoice(name), count: count);
	private void RemoveVoice(string voiceName, int count = 1)
		=> RemoveResource(voiceName, voiceLoadCounts, (chara, name) => chara.ImplDisposeVoice(name), count: count);


	public void CharaLoadFor(int player) {
		foreach (var (name, count) in PlayerToAnimationToRefCount[player])
			this.AddAnimation(name, count);
		foreach (var (name, count) in PlayerToVoiceToRefCount[player])
			this.AddVoice(name, count);
	}

	public void CharaUnloadFor(int player) {
		foreach (var (name, count) in PlayerToAnimationToRefCount[player])
			this.RemoveAnimation(name, count);
		foreach (var (name, count) in PlayerToVoiceToRefCount[player])
			this.RemoveVoice(name, count);
	}

	public static string GetAlternativeAnimation(string animation) {
		if (!AlternativeAnimations.ContainsKey(animation)) return ANIM_NONE;
		string nextAnimation = AlternativeAnimations[animation];
		return nextAnimation;
	}

	public string GetAnimation(string animation) {
		for (int i = 0; i < 5; i++) {
			bool available = this.AvailableAnimation(animation, false);
			if (available) return animation;
			string nextAnimation = GetAlternativeAnimation(animation);
			animation = nextAnimation;
		}
		return ANIM_NONE;
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
		for (int p = 0; p < OpenTaiko.MAX_PLAYERS; ++p) {
			foreach (var (name, count) in PlayerToAnimationToRefCount[p])
				this.ImplDisposeAnimation(name);
			foreach (var (name, count) in PlayerToVoiceToRefCount[p])
				this.ImplDisposeVoice(name);
		}
		animationLoadCounts.Clear();
		voiceLoadCounts.Clear();
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

	public virtual void LoadAnimation(string voice) {

	}

	public virtual void DisposeAnimation(string voice) {

	}

	protected virtual void ImplLoadAnimation(string animationType) {
	}

	protected virtual void ImplDisposeAnimation(string animationType) {
	}

	public virtual bool AvailableAnimation(string voice, bool useAlternative = true) {
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
	public virtual void LoadVoice(string voice) {

	}

	public virtual void DisposeVoice(string voice) {

	}

	protected virtual void ImplLoadVoice(string voice) {
	}

	protected virtual void ImplDisposeVoice(string voice) {
	}

	public virtual void PlayVoice(string voice) {

	}


	//------------------
}
