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

	public static readonly Dictionary<string, int> listPreviewAnimation = new Dictionary<string, int>();
	public static readonly Dictionary<string, int>[] listEssentialAnimation = new Dictionary<string, int>[5] {
		new Dictionary<string, int>(),
		new Dictionary<string, int>(),
		new Dictionary<string, int>(),
		new Dictionary<string, int>(),
		new Dictionary<string, int>()
	};

	public static readonly Dictionary<string, int> listPreviewVoices = new Dictionary<string, int>();
	public static readonly Dictionary<string, int>[] listEssentialVoices = new Dictionary<string, int>[5] {
		new Dictionary<string, int>(),
		new Dictionary<string, int>(),
		new Dictionary<string, int>(),
		new Dictionary<string, int>(),
		new Dictionary<string, int>()
	};

	public static CCharacter GetCharacter(int player) {
		int _charaId = OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(player)].data.Character;
		return OpenTaiko.Tx.Characters[_charaId];
	}




	public static void AddPreviewAnimation(string animationName) {
		if (!listPreviewAnimation.ContainsKey(animationName)) {
			listPreviewAnimation.Add(animationName, 0);
		}

		if (listPreviewAnimation[animationName] == 0) {
			foreach (CCharacter characterLua in OpenTaiko.Tx.Characters) {
				characterLua.LoadAnimation(0, animationName);
			}
		}
		listPreviewAnimation[animationName]++;
	}

	public static void RemovePreviewAnimation(string animationName) {
		if (!listPreviewAnimation.ContainsKey(animationName)) return;

		listPreviewAnimation[animationName]--;
		if (listPreviewAnimation[animationName] == 0) {
			foreach (CCharacter characterLua in OpenTaiko.Tx.Characters) {
				characterLua.DisposeAnimation(0, animationName);
			}

			listPreviewAnimation.Remove(animationName);
		}
	}

	public static void AddEssentialAnimation(int player, string animationName) {
		Dictionary<string, int> essentialAnimationCounts = listEssentialAnimation[player];
		if (!essentialAnimationCounts.ContainsKey(animationName)) {
			essentialAnimationCounts.Add(animationName, 0);
		}

		if (essentialAnimationCounts[animationName] == 0) {
			GetCharacter(player).LoadAnimation(player, animationName);
		}
		essentialAnimationCounts[animationName]++;
	}

	public static void RemoveEssentialAnimation(int player, string animationName) {
		Dictionary<string, int> essentialVoiceCounts = listEssentialAnimation[player];
		if (!essentialVoiceCounts.ContainsKey(animationName)) return;

		essentialVoiceCounts[animationName]--;
		if (essentialVoiceCounts[animationName] == 0) {
			GetCharacter(player).DisposeAnimation(player, animationName);

			essentialVoiceCounts.Remove(animationName);
		}
	}




	public static void AddPreviewVoice(string voiceName) {
		if (!listPreviewVoices.ContainsKey(voiceName)) {
			listPreviewVoices.Add(voiceName, 0);
		}

		if (listPreviewVoices[voiceName] == 0) {
			foreach (CCharacter characterLua in OpenTaiko.Tx.Characters) {
				characterLua.LoadVoice(0, voiceName);
			}
		}
		listPreviewVoices[voiceName]++;
	}

	public static void RemovePreviewVoice(string voiceName) {
		if (!listPreviewVoices.ContainsKey(voiceName)) return;

		listPreviewVoices[voiceName]--;
		if (listPreviewVoices[voiceName] == 0) {
			foreach (CCharacter characterLua in OpenTaiko.Tx.Characters) {
				characterLua.DisposeVoice(0, voiceName);
			}

			listPreviewVoices.Remove(voiceName);
		}
	}

	public static void AddEssentialVoice(int player, string voiceName) {
		Dictionary<string, int> essentialVoiceCounts = listEssentialVoices[player];
		if (!essentialVoiceCounts.ContainsKey(voiceName)) {
			essentialVoiceCounts.Add(voiceName, 0);
		}

		if (essentialVoiceCounts[voiceName] == 0) {
			GetCharacter(player).LoadVoice(player, voiceName);
		}
		essentialVoiceCounts[voiceName]++;
	}

	public static void RemoveEssentialVoice(int player, string voiceName) {
		Dictionary<string, int> essentialVoiceCounts = listEssentialVoices[player];
		if (!essentialVoiceCounts.ContainsKey(voiceName)) return;

		essentialVoiceCounts[voiceName]--;
		if (essentialVoiceCounts[voiceName] == 0) {
			GetCharacter(player).DisposeVoice(player, voiceName);

			essentialVoiceCounts.Remove(voiceName);
		}
	}

	public static void CharaLoad(int player, CCharacter character) {
		Dictionary<string, int> essentialAnimationCounts = listEssentialAnimation[player];
		foreach (var item in essentialAnimationCounts) {
			character.LoadAnimation(player, item.Key);
		}

		Dictionary<string, int> essentialVoiceCounts = listEssentialVoices[player];
		foreach (var item in essentialVoiceCounts) {
			character.LoadVoice(player, item.Key);
		}
	}

	public static void CharaUnload(int player, CCharacter character) {
		Dictionary<string, int> essentialAnimationCounts = listEssentialAnimation[player];
		foreach (var item in essentialAnimationCounts) {
			character.DisposeAnimation(player, item.Key);
		}

		Dictionary<string, int> essentialVoiceCounts = listEssentialVoices[player];
		foreach (var item in essentialVoiceCounts) {
			character.DisposeVoice(player, item.Key);
		}
	}

	public static string GetAlternativeAnimation(string animation) {
		if (!AlternativeAnimations.ContainsKey(animation)) return ANIM_NONE;
		string nextAnimation = AlternativeAnimations[animation];
		return nextAnimation;
	}

	public static string GetAnimation(int player, CCharacter character, string animation) {
		for (int i = 0; i < 5; i++) {
			bool avaiable = character.AvaiableAnimation(player, animation, false);
			if (avaiable) return animation;
			string nextAnimation = GetAlternativeAnimation(animation);
			animation = nextAnimation;
		}
		return ANIM_NONE;
	}

	public DBCharacter.CharacterData metadata;
	public DBCharacter.CharacterEffect effect;
	public CUnlockCondition? unlock;
	public string _path;
	public int _idx;
	public string dirName;

	public float GetEffectCoinMultiplier() {
		float mult = 1f;

		mult *= HRarity.tRarityToRarityToCoinMultiplier(metadata.Rarity);
		mult *= effect.GetCoinMultiplier();

		return mult;
	}

	public void tGetUnlockedItems(int _player, ModalQueue mq) {
		int player = OpenTaiko.GetActualPlayer(_player);
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
						this,
						null
					//OpenTaiko.Tx.Characters_Heya_Render[_idx]
					),
					_player);

				DBSaves.RegisterStringUnlockedAsset(OpenTaiko.SaveFileInstances[player].data.SaveId, "unlocked_characters", dirName);
			}
		}

		if (_edited)
			OpenTaiko.SaveFileInstances[player].tApplyHeyaChanges();
	}

	public CCharacter(string path, int i) {
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

	public virtual void Dispose() {

	}

	public virtual bool Update(int player, string animationType, bool looping = true) {
		return false;
	}

	public virtual void Draw(int player, string animationType, float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {

	}

	public virtual void LoadAnimation(int player, string voice) {

	}

	public virtual void DisposeAnimation(int player, string voice) {

	}

	public virtual bool AvaiableAnimation(int player, string voice, bool useAlternative = true) {
		return false;
	}

	public virtual void SetAnimationDuration(int player, string animationType, double duration) {

	}

	public virtual void ResetAnimationCounter(int player, string animationType) {

	}

	public void SetAnimationCyclesFromBPM(int player, string animationType, double bpm) {
		SetAnimationDuration(player, animationType, 60000 / Math.Abs(CTja.TjaBeatSpeedToGameBeatSpeed(bpm)));
	}

	//voice-------------
	public virtual void LoadVoice(int player, string voice) {

	}

	public virtual void DisposeVoice(int player, string voice) {

	}

	public virtual void PlayVoice(int player, string voice) {

	}
	//------------------
}
