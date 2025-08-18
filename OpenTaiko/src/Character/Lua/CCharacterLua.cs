using System.Collections.Frozen;
using FDK;

namespace OpenTaiko;

class CCharacterLua : CCharacter {
	private CLuaCharacterScript Script;

	public CCharacterLua(string path, int i) : base(path, i) {
		Script = new CLuaCharacterScript(path, null, null, true);
		Script.LoadPreviewTextures();
	}

	public override void LoadStoryTextures() {
		base.LoadStoryTextures();
		Script.LoadStoryTextures();
	}

	public override void LoadGeneralTextures() {
		base.LoadGeneralTextures();
		Script.LoadGeneralTextures();
	}

	public override void DisposeStoryTextures() {
		base.DisposeStoryTextures();
		Script.DisposeStoryTextures();
	}

	public override void DisposeGeneralTextures() {
		base.DisposeGeneralTextures();
		Script.DisposeGeneralTextures();
	}

	public override void Dispose() {
		base.Dispose();
		Script.DisposePreviewTextures();
		Script.Dispose();
	}

	public override void Update(int player) {
		base.Update(player);
		Script.Update(player);
	}

	public override void Draw(int player, float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {
		base.Draw(player, x, y, scaleX, scaleY, opacity, color, flipX);
		Script.Draw(player, x, y, scaleX, scaleY, opacity, color != null ? new LuaColor(color) : new LuaColor(255, 255, 255, 255), flipX);
	}

	public override void DrawPreview(float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {
		base.DrawPreview(x, y, scaleX, scaleY, opacity, color, flipX);
		Script.DrawPreview(x, y, scaleX, scaleY, opacity, color != null ? new LuaColor(color) : new LuaColor(255, 255, 255, 255), flipX);
	}

	public override void DrawHeyaRender(float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {
		base.DrawHeyaRender(x, y, scaleX, scaleY, opacity, color, flipX);
		Script.DrawHeyaRender(x, y, scaleX, scaleY, opacity, color != null ? new LuaColor(color) : new LuaColor(255, 255, 255, 255), flipX);
	}


	public override void SetLoopAnimation(int player, string animationType, bool loop = true) {
		base.SetLoopAnimation(player, animationType, loop);
		Script.SetLoopAnimation(player, animationType, loop);
	}

	public override void PlayAnimation(int player, string animationType) {
		base.PlayAnimation(player, animationType);
		Script.PlayAnimation(player, animationType);
	}

	public override void PlayVoice(string voiceType) {
		base.PlayVoice(voiceType);
		Script.PlayVoice(voiceType);
	}

	public override void SetAnimationDuration(int player, double ms) {
		base.SetAnimationDuration(player, ms);
		Script.SetAnimationDuration(player, ms);
	}

	public override void SetAnimationCyclesToBPM(int player, double bpm) {
		base.SetAnimationCyclesToBPM(player, bpm);
		Script.SetAnimationCyclesToBPM(player, bpm);
	}
}
