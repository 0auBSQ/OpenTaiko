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

		private NamedLuaFunction lfLoadAnimation = new("loadAnimation");
		private NamedLuaFunction lfDisposeAnimation = new("disposeAnimation");
		private NamedLuaFunction lfAvaialbeAnimation = new("avaialbeAnimation");
		private NamedLuaFunction lfSetAnimationDuration = new("setAnimationDuration");
		private NamedLuaFunction lfResetAnimationCounter = new("resetAnimationCounter");
		private NamedLuaFunction lfUpdate = new("update");
		private NamedLuaFunction lfDraw = new("draw");
		private NamedLuaFunction lfGetDrawSize = new("getDrawSize");
		private NamedLuaFunction lfGetHeyaRenderOffset = new("getHeyaRenderOffset");
		private NamedLuaFunction lfGetAIBattlePosition = new("getAIBattlePosition");

		private NamedLuaFunction lfLoadVoice = new("loadVoice");
		private NamedLuaFunction lfDisposeVoice = new("disposeVoice");
		private NamedLuaFunction lfPlayVoice = new("playVoice");

		public void LoadAnimation(string animationType) {
			RunLuaCode(lfLoadAnimation, animationType);
		}

		public void DisposeAnimation(string animationType) {
			RunLuaCode(lfDisposeAnimation, animationType);
		}

		public bool AvaialbeAnimation(string animationType) {
			object[] result = RunLuaCode(lfAvaialbeAnimation, animationType) ?? [];
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
			object[] result = RunLuaCode(lfUpdate, delta, animationType, looping) ?? [];
			if (result is not null && result.Length == 1 && result[0] is bool flag) {
				return flag;
			}
			return false;
		}

		public void Draw(string animationType, float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, LuaColor? color = null, string? contextType = null, float rotation = 0f, string? blendMode = null, string? wrapMode = null, LuaGradientMap? gradientMap = null) {
			if (animationType == CCharacter.ANIM_NONE) return;
			RunLuaCode(lfDraw, animationType, x, y, scaleX, scaleY, opacity, color ?? new LuaColor(255, 255, 255), contextType, (object?)null, (object?)null, (object?)null, (object?)null, (object?)null, rotation, (object?)blendMode, (object?)wrapMode, (object?)gradientMap);
		}

		public void DrawAtAnchor(string animationType, float x, float y, string anchor, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, LuaColor? color = null, string? contextType = null, float? clipW = null, float? clipH = null, float clipX = 0f, float clipY = 0f, float rotation = 0f, string? blendMode = null, string? wrapMode = null, LuaGradientMap? gradientMap = null) {
			if (animationType == CCharacter.ANIM_NONE) return;
			RunLuaCode(lfDraw, animationType, x, y, scaleX, scaleY, opacity, color ?? new LuaColor(255, 255, 255), contextType, anchor, (object?)clipW, (object?)clipH, (object)clipX, (object)clipY, rotation, (object?)blendMode, (object?)wrapMode, (object?)gradientMap);
		}

		public LuaVector2 GetDrawSize(string animationType) {
			object[] result = RunLuaCode(lfGetDrawSize, animationType) ?? [];
			if (result != null && result.Length >= 2) {
				double w = result[0] is double dw ? dw : 0;
				double h = result[1] is double dh ? dh : 0;
				return new LuaVector2(w, h);
			}
			return new LuaVector2(0, 0);
		}

		public (float x, float y)? GetAIBattlePosition(int player, float charaScale = 1.0f) {
			object[] result = RunLuaCode(lfGetAIBattlePosition, player, (double)charaScale) ?? [];
			if (result != null && result.Length >= 2 && result[0] is double x && result[1] is double y)
				return ((float)x, (float)y);
			return null;
		}

		public (float x, float y) GetHeyaRenderOffset() {
			object[] result = RunLuaCode(lfGetHeyaRenderOffset) ?? [];
			if (result != null && result.Length >= 2) {
				float x = result[0] is double dx ? (float)dx : 0f;
				float y = result[1] is double dy ? (float)dy : 0f;
				return (x, y);
			}
			return (0f, 0f);
		}

		public CLuaCharacterScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, false, DefaultScript) {
			try {
				lfLoadAnimation.Load(LuaScript);
				lfDisposeAnimation.Load(LuaScript);
				lfAvaialbeAnimation.Load(LuaScript);
				lfSetAnimationDuration.Load(LuaScript);
				lfResetAnimationCounter.Load(LuaScript);
				lfUpdate.Load(LuaScript);
				lfDraw.Load(LuaScript);
				lfGetDrawSize.Load(LuaScript);
				lfGetHeyaRenderOffset.Load(LuaScript);
				lfGetAIBattlePosition.Load(LuaScript);

				lfLoadVoice.Load(LuaScript);
				lfDisposeVoice.Load(LuaScript);
				lfPlayVoice.Load(LuaScript);
			} catch (Exception e) {
				Crash(e);
			}
		}
	}
}
