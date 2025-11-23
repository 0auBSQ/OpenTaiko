using NLua;

namespace OpenTaiko {
	class CLuaCharacterScript : CLuaScript {
		private const string CHARA_SCRIPT_FILE_PATH = "CharaScript.lua";
		private static readonly string DefaultScript;

		static CLuaCharacterScript() {
			using StreamReader streamReader = new StreamReader(CHARA_SCRIPT_FILE_PATH);
			DefaultScript = streamReader.ReadToEnd();
		}

		private LuaFunction lfLegacyLoadPreviewTextures;
		private LuaFunction lfLoadGeneralTextures;
		private LuaFunction lfLegacyDisposePreviewTextures;
		private LuaFunction lfDisposeGeneralTextures;
		private LuaFunction lfLoadIndividualAnimation;
		private LuaFunction lfDisposeIndividualAnimation;
		private LuaFunction lfGetAnimationInformation;
		private LuaFunction lfGameInit;
		private LuaFunction lfUpdate;
		private LuaFunction lfTowerNextFloor;
		private LuaFunction lfTowerFinish;
		private LuaFunction lfDraw;
		private LuaFunction lfDrawPreview;
		private LuaFunction lfDrawHeyaRender;
		private LuaFunction lfDrawTower;
		private LuaFunction lfSetLoopAnimation;
		private LuaFunction lfPlayAnimation;
		private LuaFunction lfPlayVoice;
		private LuaFunction lfSetAnimationDuration;
		private LuaFunction lfSetAnimationCyclesToBPM;

		private HashSet<string> _loadedIndividualAnimations = new HashSet<string>();

		#region [C# Linked methods]

		// My Room exclusive, will be deprecated on the My Room update
		public void LEGACY_LoadPreviewTextures() {
			RunLuaCode(lfLegacyLoadPreviewTextures);
		}

		public void LoadGeneralTextures() {
			RunLuaCode(lfLoadGeneralTextures);
		}

		// My Room exclusive, will be deprecated on the My Room update
		public void LEGACY_DisposePreviewTextures() {
			RunLuaCode(lfLegacyDisposePreviewTextures);
		}

		public void DisposeGeneralTextures() {
			RunLuaCode(lfDisposeGeneralTextures);
		}

		public void GameInit() {
			RunLuaCode(lfGameInit);
		}

		public void Update() {
			RunLuaCode(lfUpdate);
		}

		public void TowerNextFloor() {
			RunLuaCode(lfTowerNextFloor);
		}

		public void TowerFinish() {
			RunLuaCode(lfTowerFinish);
		}

		#endregion

		#region [Lua API exclusive extentions]

		// Optional methods, but can be a big help for (for example) modulable custom game mode textures as long as the chara script is flexible enough
		public void LoadIndividualAnimation(string name, params object[] args) {
			RunLuaCode(lfLoadIndividualAnimation, name, args);
			_loadedIndividualAnimations.Add(name);
		}

		public void DisposeIndividualAnimation(string name) {
			RunLuaCode(lfDisposeIndividualAnimation, name);
			_loadedIndividualAnimations.Remove(name);
		}

		public object[] GetAnimationInformation(string name) {
			return RunLuaCode(lfGetAnimationInformation, name);
		}

		#endregion

		public void Draw(float x, float y, float scaleX, float scaleY, int opacity, LuaColor color, bool flipX) {
			RunLuaCode(lfDraw, x, y, scaleX, scaleY, opacity, color, flipX);
		}

		public void DrawPreview(float x, float y, float scaleX, float scaleY, int opacity, LuaColor color, bool flipX) {
			RunLuaCode(lfDrawPreview, x, y, scaleX, scaleY, opacity, color, flipX);
		}

		public void DrawHeyaRender(float x, float y, float scaleX, float scaleY, int opacity, LuaColor color, bool flipX) {
			RunLuaCode(lfDrawHeyaRender, x, y, scaleX, scaleY, opacity, color, flipX);
		}

		public void DrawTower() {
			RunLuaCode(lfDrawTower);
		}

		public void SetLoopAnimation(string animationType, bool loop = true) {
			RunLuaCode(lfSetLoopAnimation, animationType, loop);
		}

		public void PlayAnimation(string animationType) {
			RunLuaCode(lfPlayAnimation, animationType);
		}

		public void PlayVoice(string voiceType) {
			RunLuaCode(lfPlayVoice, voiceType);
		}

		public void SetAnimationDuration(double ms) {
			RunLuaCode(lfSetAnimationDuration, ms);
		}

		public void SetAnimationCyclesToBPM(double bpm) {
			RunLuaCode(lfSetAnimationCyclesToBPM, bpm);
		}

		public CLuaCharacterScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets, DefaultScript) {
			try {
				// C# hardcoded animations
				lfLegacyLoadPreviewTextures = (LuaFunction)LuaScript["loadPreviewTextures"];
				lfLoadGeneralTextures = (LuaFunction)LuaScript["loadGeneralTextures"];
				lfLegacyDisposePreviewTextures = (LuaFunction)LuaScript["disposePreviewTextures"];
				lfDisposeGeneralTextures = (LuaFunction)LuaScript["disposeGeneralTextures"];
				// Custom/additional animations
				lfLoadIndividualAnimation = (LuaFunction)LuaScript["loadIndividualAnimation"];
				lfDisposeIndividualAnimation = (LuaFunction)LuaScript["disposeIndividualAnimation"];
				lfGameInit = (LuaFunction)LuaScript["gameInit"];
				lfUpdate = (LuaFunction)LuaScript["update"];
				lfTowerNextFloor = (LuaFunction)LuaScript["towerNextFloor"];
				lfTowerFinish = (LuaFunction)LuaScript["towerFinish"];
				lfDraw = (LuaFunction)LuaScript["draw"];
				lfDrawPreview = (LuaFunction)LuaScript["drawPreview"];
				lfDrawHeyaRender = (LuaFunction)LuaScript["drawHeyaRender"];
				lfDrawTower = (LuaFunction)LuaScript["drawTower"];
				lfSetLoopAnimation = (LuaFunction)LuaScript["setLoopAnimation"];
				lfPlayAnimation = (LuaFunction)LuaScript["playAnimation"];
				lfPlayVoice = (LuaFunction)LuaScript["playVoice"];
				lfSetAnimationDuration = (LuaFunction)LuaScript["setAnimationDuration"];
				lfSetAnimationCyclesToBPM = (LuaFunction)LuaScript["setAnimationCyclesToBPM"];
			} catch (Exception e) {
				Crash(e);
			}
		}
	}
}
