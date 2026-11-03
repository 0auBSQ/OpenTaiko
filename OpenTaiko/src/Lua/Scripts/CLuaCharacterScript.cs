using FDK;
using NLua;

namespace OpenTaiko {
	class CLuaCharacterScript : CLuaScript {
		private const string CHARA_SCRIPT_FILE_PATH = "CharaScript.lua";
		private static readonly string DefaultScript;

		static CLuaCharacterScript() {
			using StreamReader streamReader = new StreamReader(OpenTaiko.ResolveAssetPath(CHARA_SCRIPT_FILE_PATH));
			DefaultScript = streamReader.ReadToEnd();
		}

		private NamedLuaFunction lfLoadAnimation = new("loadAnimation");
		private NamedLuaFunction lfDisposeAnimation = new("disposeAnimation");
		private NamedLuaFunction lfAvaialbeAnimation = new("avaialbeAnimation"); // earlier typoed API
		private NamedLuaFunction lfAvailableAnimation = new("availableAnimation");
		private NamedLuaFunction lfSetAnimationDuration = new("setAnimationDuration");
		private NamedLuaFunction lfResetAnimationCounter = new("resetAnimationCounter");
		private NamedLuaFunction lfUpdate = new("update");
		private NamedLuaFunction lfDraw = new("draw");
		private NamedLuaFunction lfGetDrawSize = new("getDrawSize");
		private NamedLuaFunction lfGetAIBattlePosition = new("getAIBattlePosition");

		private NamedLuaFunction lfLoadVoice = new("loadVoice");
		private NamedLuaFunction lfDisposeVoice = new("disposeVoice");
		private NamedLuaFunction lfPlayVoice = new("playVoice");

		// argument arrays reused by the calls made every frame
		private readonly object?[] _nameArgs = new object?[1];
		private readonly object?[] _durationArgs = new object?[2];
		private readonly object?[] _updateArgs = new object?[3];
		private readonly object?[] _drawArgs = new object?[17];

		// availableAnimation's answer per animation, until an animation is loaded or disposed
		private readonly Dictionary<string, bool> _available = [];

		private object?[] NameArgs(string? animationType) {
			_nameArgs[0] = animationType;
			return _nameArgs;
		}

		public void LoadAnimation(string animationType) {
			RunLuaCode(lfLoadAnimation, animationType);
			_available.Clear();
		}

		public void DisposeAnimation(string animationType) {
			RunLuaCode(lfDisposeAnimation, animationType);
			_available.Clear();
		}

		public bool AvailableAnimation(string animationType) {
			if (animationType != null && _available.TryGetValue(animationType, out bool cached))
				return cached;
			object[] result = RunLuaCode(lfAvailableAnimation, NameArgs(animationType)) ?? RunLuaCode(lfAvaialbeAnimation, NameArgs(animationType)) ?? [];
			bool available = result is not null && result.Length == 1 && result[0] is bool flag && flag;
			if (animationType != null)
				_available[animationType] = available;
			return available;
		}

		public void SetAnimationDuration(string animationType, double duration) {
			_durationArgs[0] = animationType;
			SetArg(_durationArgs, 1, duration);
			RunLuaCode(lfSetAnimationDuration, _durationArgs);
		}

		public void ResetAnimationCounter(string animationType) {
			RunLuaCode(lfResetAnimationCounter, NameArgs(animationType));
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
			SetArg(_updateArgs, 0, delta);
			_updateArgs[1] = animationType;
			SetArg(_updateArgs, 2, looping);
			object[] result = RunLuaCode(lfUpdate, _updateArgs) ?? [];
			if (result is not null && result.Length == 1 && result[0] is bool flag) {
				return flag;
			}
			return false;
		}

		public void Draw(string animationType, float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, LuaColor? color = null, string? contextType = null, float rotation = 0f, string? blendMode = null, string? wrapMode = null, LuaGradientMap? gradientMap = null) {
			if (animationType == CCharacter.ANIM_NONE) return;
			SetDrawArgs(animationType, x, y, scaleX, scaleY, opacity, color, contextType, rotation, blendMode, wrapMode, gradientMap);
			_drawArgs[8] = null;
			_drawArgs[9] = null;
			_drawArgs[10] = null;
			_drawArgs[11] = null;
			_drawArgs[12] = null;
			RunLuaCode(lfDraw, _drawArgs);
		}

		public void DrawAtAnchor(string animationType, float x, float y, string anchor, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, LuaColor? color = null, string? contextType = null, float? clipW = null, float? clipH = null, float clipX = 0f, float clipY = 0f, float rotation = 0f, string? blendMode = null, string? wrapMode = null, LuaGradientMap? gradientMap = null) {
			if (animationType == CCharacter.ANIM_NONE) return;
			SetDrawArgs(animationType, x, y, scaleX, scaleY, opacity, color, contextType, rotation, blendMode, wrapMode, gradientMap);
			_drawArgs[8] = anchor;
			SetArg(_drawArgs, 9, clipW);
			SetArg(_drawArgs, 10, clipH);
			SetArg(_drawArgs, 11, clipX);
			SetArg(_drawArgs, 12, clipY);
			RunLuaCode(lfDraw, _drawArgs);
		}

		// draw(animationType, x, y, scaleX, scaleY, opacity, color, contextType, anchor, clipW, clipH, clipX, clipY, rotation, blendMode, wrapMode, gradientMap)
		private void SetDrawArgs(string animationType, float x, float y, float scaleX, float scaleY, int opacity, LuaColor? color, string? contextType, float rotation, string? blendMode, string? wrapMode, LuaGradientMap? gradientMap) {
			_drawArgs[0] = animationType;
			SetArg(_drawArgs, 1, x);
			SetArg(_drawArgs, 2, y);
			SetArg(_drawArgs, 3, scaleX);
			SetArg(_drawArgs, 4, scaleY);
			SetArg(_drawArgs, 5, opacity);
			_drawArgs[6] = color ?? new LuaColor(255, 255, 255);
			_drawArgs[7] = contextType;
			SetArg(_drawArgs, 13, rotation);
			_drawArgs[14] = blendMode;
			_drawArgs[15] = wrapMode;
			_drawArgs[16] = gradientMap;
		}

		public LuaVector2 GetDrawSize(string animationType) {
			object[] result = RunLuaCode(lfGetDrawSize, NameArgs(animationType)) ?? [];
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

		public CLuaCharacterScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, false, DefaultScript) {
			try {
				lfLoadAnimation.Load(LuaScript);
				lfDisposeAnimation.Load(LuaScript);
				lfAvaialbeAnimation.Load(LuaScript);
				lfAvailableAnimation.Load(LuaScript);
				lfSetAnimationDuration.Load(LuaScript);
				lfResetAnimationCounter.Load(LuaScript);
				lfUpdate.Load(LuaScript);
				lfDraw.Load(LuaScript);
				lfGetDrawSize.Load(LuaScript);
				lfGetAIBattlePosition.Load(LuaScript);

				lfLoadVoice.Load(LuaScript);
				lfDisposeVoice.Load(LuaScript);
				lfPlayVoice.Load(LuaScript);
			} catch (Exception e) {
				Crash(e, $"initializing {nameof(CLuaCharacterScript)}");
			}
		}
	}
}
