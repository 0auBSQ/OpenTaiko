using FDK;

namespace OpenTaiko;

abstract class CCharacter : IDisposable {
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

	public const int DEFAULT_DURATION = 1000;

	public static CCharacter GetCharacter(int player) {
		int _charaId = OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(player)].data.Character;
		return OpenTaiko.Tx.Characters[_charaId];
	}

	public DBCharacter.CharacterData metadata;
	public DBCharacter.CharacterEffect effect;
	public CUnlockCondition? unlock;
	public string _path;
	public int _idx;
	public string dirName;

	public bool[] bGeneralTextureLoaded { get; private set; } = new bool[5];
	public bool[] bStoryTextureLoaded { get; private set; } = new bool[5];

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

	public virtual void LoadStoryTextures(int player) {
		bStoryTextureLoaded[player] = true;
	}

	public virtual void LoadGeneralTextures(int player) {
		bGeneralTextureLoaded[player] = true;
	}

	public virtual void DisposeStoryTextures(int player) {
		bStoryTextureLoaded[player] = false;
	}

	public virtual void DisposeGeneralTextures(int player) {
		bGeneralTextureLoaded[player] = false;
	}

	public virtual void Dispose() {
		for (int player = 0; player < 5; player++)
		{
			DisposeStoryTextures(player);
			DisposeGeneralTextures(player);
		}
	}

	public virtual void GameInit(int player) {

	}

	public virtual void Update(int player) {

	}

	public virtual void TowerNextFloor() {

	}

	public virtual void TowerFinish() {

	}

	public virtual void Draw(int player, float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {

	}

	public virtual void DrawPreview(float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {

	}

	public virtual void DrawHeyaRender(float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {

	}

	public virtual void DrawTower() {

	}

	public virtual void SetLoopAnimation(int player, string animationType, bool loop = true) {

	}

	public virtual void PlayAnimation(int player, string animationType) {

	}

	public virtual void PlayVoice(int player, string voiceType) {

	}

	public virtual void SetAnimationDuration(int player, double ms) {

	}

	public virtual void SetAnimationCyclesToBPM(int player, double bpm) {

	}
}
