using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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




		private LuaFunction lfLoadPreviewTextures;
		private LuaFunction lfLoadStoryTextures;
		private LuaFunction lfLoadGeneralTextures;
		private LuaFunction lfDisposePreviewTextures;
		private LuaFunction lfDisposeStoryTextures;
		private LuaFunction lfDisposeGeneralTextures;
		private LuaFunction lfUpdate;
		private LuaFunction lfDraw;
		private LuaFunction lfDrawPreview;
		private LuaFunction lfDrawHeyaRender;
		private LuaFunction lfSetLoopAnimation;
		private LuaFunction lfPlayAnimation;
		private LuaFunction lfPlayVoice;
		private LuaFunction lfSetAnimationDuration;
		private LuaFunction lfSetAnimationCyclesToBPM;

		public void LoadPreviewTextures() {
			RunLuaCode(lfLoadPreviewTextures);
		}

		public void LoadStoryTextures() {
			RunLuaCode(lfLoadStoryTextures);
		}

		public void LoadGeneralTextures() {
			RunLuaCode(lfLoadGeneralTextures);
		}

		public void DisposePreviewTextures() {
			RunLuaCode(lfDisposePreviewTextures);
		}

		public void DisposeStoryTextures() {
			RunLuaCode(lfDisposeStoryTextures);
		}

		public void DisposeGeneralTextures() {
			RunLuaCode(lfDisposeGeneralTextures);
		}

		public void Update(int player) {
			RunLuaCode(lfUpdate, player);
		}

		public void Draw(int player, float x, float y, float scaleX, float scaleY, int opacity, LuaColor color, bool flipX) {
			RunLuaCode(lfDraw, player, x, y, scaleX, scaleY, opacity, color, flipX);
		}

		public void DrawPreview(float x, float y, float scaleX, float scaleY, int opacity, LuaColor color, bool flipX) {
			RunLuaCode(lfDrawPreview, x, y, scaleX, scaleY, opacity, color, flipX);
		}

		public void DrawHeyaRender(float x, float y, float scaleX, float scaleY, int opacity, LuaColor color, bool flipX) {
			RunLuaCode(lfDrawHeyaRender, x, y, scaleX, scaleY, opacity, color, flipX);
		}

		public void SetLoopAnimation(int player, string animationType, bool loop = true) {
			RunLuaCode(lfSetLoopAnimation, player, animationType, loop);
		}

		public void PlayAnimation(int player, string animationType) {
			RunLuaCode(lfPlayAnimation, player, animationType);
		}

		public void PlayVoice(string voiceType) {
			RunLuaCode(lfPlayVoice, voiceType);
		}

		public void SetAnimationDuration(int player, double ms) {
			RunLuaCode(lfSetAnimationDuration, player, ms);
		}

		public void SetAnimationCyclesToBPM(int player, double bpm) {
			RunLuaCode(lfSetAnimationCyclesToBPM, player, bpm);
		}

		public CLuaCharacterScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets, DefaultScript) {
			try {
				lfLoadPreviewTextures = (LuaFunction)LuaScript["loadPreviewTextures"];
				lfLoadStoryTextures = (LuaFunction)LuaScript["loadStoryTextures"];
				lfLoadGeneralTextures = (LuaFunction)LuaScript["loadGeneralTextures"];
				lfDisposePreviewTextures = (LuaFunction)LuaScript["disposePreviewTextures"];
				lfDisposeStoryTextures = (LuaFunction)LuaScript["disposeStoryTextures"];
				lfDisposeGeneralTextures = (LuaFunction)LuaScript["disposeGeneralTextures"];
				lfUpdate = (LuaFunction)LuaScript["update"];
				lfDraw = (LuaFunction)LuaScript["draw"];
				lfDrawPreview = (LuaFunction)LuaScript["drawPreview"];
				lfDrawHeyaRender = (LuaFunction)LuaScript["drawHeyaRender"];
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
