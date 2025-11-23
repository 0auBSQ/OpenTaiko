using FDK;

namespace OpenTaiko;

class CCharacterLua : CCharacter {
	private CLuaCharacterScript[] Script = new CLuaCharacterScript[5];

	public CLuaCharacterScript GetScript(int player) => Script[player];

	public CCharacterLua(string path, int i) : base(path, i) {
		for (int player = 0; player < 5; player++) {
			Script[player] = new CLuaCharacterScript(path, null, null, true);
		}
		Script[0].LEGACY_LoadPreviewTextures();
	}

	public override void LoadGeneralTextures(int player) {
		base.LoadGeneralTextures(player);
		Script[player].LoadGeneralTextures();
	}

	public override void DisposeGeneralTextures(int player) {
		base.DisposeGeneralTextures(player);
		Script[player].DisposeGeneralTextures();
	}

	public override void Dispose() {
		base.Dispose();
		for (int player = 0; player < 5; player++) {
			Script[player].Dispose();
		}
		Script[0].LEGACY_DisposePreviewTextures();
	}

	public override void GameInit(int player) {
		base.GameInit(player);
		Script[player].GameInit();
	}

	public override void Update(int player) {
		base.Update(player);
		Script[player].Update();
	}

	public override void TowerNextFloor() {
		base.TowerNextFloor();
		Script[0].TowerNextFloor();
	}

	public override void TowerFinish() {
		base.TowerFinish();
		Script[0].TowerFinish();
	}

	public override void Draw(int player, float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {
		base.Draw(player, x, y, scaleX, scaleY, opacity, color, flipX);
		Script[player].Draw(x, y, scaleX, scaleY, opacity, color != null ? new LuaColor(color) : new LuaColor(255, 255, 255, 255), flipX);
	}

	public override void DrawPreview(float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {
		base.DrawPreview(x, y, scaleX, scaleY, opacity, color, flipX);
		Script[0].DrawPreview(x, y, scaleX, scaleY, opacity, color != null ? new LuaColor(color) : new LuaColor(255, 255, 255, 255), flipX);
	}

	public override void DrawHeyaRender(float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {
		base.DrawHeyaRender(x, y, scaleX, scaleY, opacity, color, flipX);
		Script[0].DrawHeyaRender(x, y, scaleX, scaleY, opacity, color != null ? new LuaColor(color) : new LuaColor(255, 255, 255, 255), flipX);
	}

	public override void DrawTower() {
		base.DrawTower();
		Script[0].DrawTower();
	}


	public override void SetLoopAnimation(int player, string animationType, bool loop = true) {
		base.SetLoopAnimation(player, animationType, loop);
		Script[player].SetLoopAnimation(animationType, loop);
	}

	public override void PlayAnimation(int player, string animationType) {
		base.PlayAnimation(player, animationType);
		Script[player].PlayAnimation(animationType);
	}

	public override void PlayVoice(int player, string voiceType) {
		base.PlayVoice(player, voiceType);
		Script[player].PlayVoice(voiceType);
	}

	public override void SetAnimationDuration(int player, double ms) {
		base.SetAnimationDuration(player, ms);
		Script[player].SetAnimationDuration(ms);
	}

	public override void SetAnimationCyclesToBPM(int player, double bpm) {
		base.SetAnimationCyclesToBPM(player, bpm);
		Script[player].SetAnimationCyclesToBPM(bpm);
	}
}
