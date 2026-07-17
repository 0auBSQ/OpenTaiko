using FDK;
using Silk.NET.Maths;

namespace OpenTaiko;

class CCharacterLua : CCharacter {
	public CLuaCharacterScript Script { get; private set; }

	public CCharacterLua(string path, int i) : base(path, i) {
		Script = new CLuaCharacterScript(path, null, null, true);
		}

	public CCharacterLua(Info info) : base(info) {
		Script = new CLuaCharacterScript(info._path, null, null, true);
	}

	public override void Dispose() {
		base.Dispose();
		Script.Dispose();
	}

	protected override bool AvailableResolvedAnimation(string animationType) {
		return Script.AvailableAnimation(animationType);
	}

	public override void SetAnimationDuration(string animationType, double duration) {
		Script.SetAnimationDuration(this.GetAnimation(animationType), duration);
	}

	public override void ResetAnimationCounter(string animationType) {
		Script.ResetAnimationCounter(this.GetAnimation(animationType));
	}

	public override bool Update(string animationType, bool looping = true) {
		return Script.Update(OpenTaiko.FPS.DeltaTime, this.GetAnimation(animationType), looping);
	}

	public override void Draw(string animationType, float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, float rotation = 0f, string? blendMode = null, string? wrapMode = null, LuaGradientMap? gradientMap = null) {
		string resolvedAnimation = this.GetAnimation(animationType);
		string? contextType = (resolvedAnimation != animationType) ? animationType : null;
		Script.Draw(resolvedAnimation, x, y, scaleX, scaleY, opacity, new LuaColor(color ?? Color4.White), contextType, rotation, blendMode, wrapMode, gradientMap);
	}

	public override void DrawAtAnchor(string animationType, float x, float y, string anchor, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, float? clipW = null, float? clipH = null, float clipX = 0f, float clipY = 0f, float rotation = 0f, string? blendMode = null, string? wrapMode = null, LuaGradientMap? gradientMap = null) {
		string resolvedAnimation = this.GetAnimation(animationType);
		string? contextType = (resolvedAnimation != animationType) ? animationType : null;
		Script.DrawAtAnchor(resolvedAnimation, x, y, anchor, scaleX, scaleY, opacity, new LuaColor(color ?? Color4.White), contextType, clipW, clipH, clipX, clipY, rotation, blendMode, wrapMode, gradientMap);
	}

	public override LuaVector2 GetDrawSize(string animationType) {
		string resolvedAnimation = this.GetAnimation(animationType);
		return Script.GetDrawSize(resolvedAnimation);
	}

	public override (float x, float y) GetHeyaRenderOffset() {
		return Script.GetHeyaRenderOffset();
	}

	public override (float x, float y)? GetAIBattlePosition(int player, float charaScale = 1.0f) {
		return Script.GetAIBattlePosition(player, charaScale);
	}


	protected override void LoadResolvedAnimation(string animationType) {
		Script.LoadAnimation(animationType);
	}

	protected override void DisposeResolvedAnimation(string animationType) {
		Script.DisposeAnimation(animationType);
	}

	public override void PlayVoice(string voice) {
		Script.PlayVoice(voice);
	}

	protected override void LoadResolvedVoice(string voice) {
		Script.LoadVoice(voice);
	}

	protected override void DisposeResolvedVoice(string voice) {
		Script.DisposeVoice(voice);
	}

	//------------------
}
