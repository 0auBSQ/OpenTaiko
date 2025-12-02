using FDK;
using NLua;

namespace OpenTaiko {
	class CLuaCharacterScript : CLuaScript {
		private const string CHARA_SCRIPT_FILE_PATH = "CharaScript.lua";
		private static readonly string DefaultScript;

		static CLuaCharacterScript() {
			using StreamReader streamReader = new StreamReader(CHARA_SCRIPT_FILE_PATH);
			DefaultScript = streamReader.ReadToEnd();
		}

		private LuaFunction lfLoadAnimation;
		private LuaFunction lfDisposeAnimation;
		private LuaFunction lfAvaialbeAnimation;
		private LuaFunction lfSetAnimationDuration;
		private LuaFunction lfResetAnimationCounter;
		private LuaFunction lfUpdate;
		private LuaFunction lfDraw;

		private LuaFunction lfLoadVoice;
		private LuaFunction lfDisposeVoice;
		private LuaFunction lfPlayVoice;

		public void LoadAnimation(string animationType) {
			RunLuaCode(lfLoadAnimation, animationType);
		}

		public void DisposeAnimation(string animationType) {
			RunLuaCode(lfDisposeAnimation, animationType);
		}

		public bool AvaialbeAnimation(string animationType) {
			object[] result = RunLuaCode(lfAvaialbeAnimation, animationType);
			if (result is not null && result.Length == 1 && result[0] is bool flag) {
				return flag;
			}
			return false;
		}

		public void SetAnimationDuration(string animationType, double duration) {
			RunLuaCode(lfSetAnimationDuration, animationType, duration);
		}

		public void ResetAnimationCounter(string animationType) {
			RunLuaCode(lfResetAnimationCounter, animationType);
		}

		public void LoadVoice(string voiceType) {
			RunLuaCode(lfLoadVoice, voiceType);
		}

		public void DisposeVoice(string voiceType) {
			RunLuaCode(lfDisposeVoice, voiceType);
		}

		public void PlayVoice(string voiceType) {
			RunLuaCode(lfPlayVoice, voiceType);
		}

		public bool Update(double delta, string animationType, bool looping = true) {
			if (animationType == CCharacter.ANIM_NONE) return false;
			object[] result = RunLuaCode(lfUpdate, delta, animationType, looping);
			if (result is not null && result.Length == 1 && result[0] is bool flag) {
				return flag;
			}
			return false;
		}

		public void Draw(string animationType, float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, LuaColor? color = null, bool flipX = false) {
			if (animationType == CCharacter.ANIM_NONE) return;
			RunLuaCode(lfDraw, animationType, x, y, scaleX, scaleY, opacity, color ?? new LuaColor(255, 255, 255), flipX);
		}

		public CLuaCharacterScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets, DefaultScript) {
			try {
				lfLoadAnimation = (LuaFunction)LuaScript["loadAnimation"];
				lfDisposeAnimation = (LuaFunction)LuaScript["disposeAnimation"];
				lfAvaialbeAnimation = (LuaFunction)LuaScript["avaialbeAnimation"];
				lfSetAnimationDuration = (LuaFunction)LuaScript["setAnimationDuration"];
				lfResetAnimationCounter = (LuaFunction)LuaScript["resetAnimationCounter"];
				lfUpdate = (LuaFunction)LuaScript["update"];
				lfDraw = (LuaFunction)LuaScript["draw"];

				lfLoadVoice = (LuaFunction)LuaScript["loadVoice"];
				lfDisposeVoice = (LuaFunction)LuaScript["disposeVoice"];
				lfPlayVoice = (LuaFunction)LuaScript["playVoice"];
			} catch (Exception e) {
				Crash(e);
			}
		}
	}
}
