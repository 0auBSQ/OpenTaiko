using FDK;
using NLua;

namespace OpenTaiko;

internal class CLuaNamePlateScript : CLuaScript {
	private NamedLuaFunction lfGetCharaOffset = new("getCharaOffset");
	private NamedLuaFunction lfSetInfos = new("setInfos");
	private NamedLuaFunction lfDrawDan = new("drawDan");
	private NamedLuaFunction lfDrawTitlePlate = new("drawTitlePlate");
	private NamedLuaFunction lfUpdate = new("update");
	private NamedLuaFunction lfDraw = new("draw");

	public CLuaNamePlateScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets) {
		lfGetCharaOffset.Load(LuaScript);
		lfSetInfos.Load(LuaScript);
		lfDrawDan.Load(LuaScript);
		lfDrawTitlePlate.Load(LuaScript);
		lfUpdate.Load(LuaScript);
		lfDraw.Load(LuaScript);
	}

	public int GetCharaOffset() {
		if (!Available) return 0;
		double result = (double)RunLuaCode(lfGetCharaOffset)[0];
		return (int)result;
	}

	public void SetInfos(int player, string name, string title, string dan, SaveFile.Data data) {
		if (!Available) return;

		RunLuaCode(lfSetInfos, player, name ?? "", title ?? "", dan ?? "", data);
	}

	// For My Room
	public void DrawDan(int x, int y, int opacity, int danGrade, CTexture titleTex) {
		if (!Available) return;

		LuaTexture _conv = new LuaTexture(titleTex);
		RunLuaCode(lfDrawDan, x, y, opacity, danGrade, _conv);
	}

	public void DrawDan(int x, int y, int opacity, int danGrade, LuaTexture text) {
		if (!Available) return;

		RunLuaCode(lfDrawDan, x, y, opacity, danGrade, text);
	}

	// For My Room
	public void DrawTitlePlate(int x, int y, int opacity, int type, CTexture titleTex, int rarity, int nameplateId) {
		if (!Available) return;

		LuaTexture _conv = new LuaTexture(titleTex);
		RunLuaCode(lfDrawTitlePlate, x, y, opacity, type, _conv, rarity, nameplateId);
	}

	public void DrawTitlePlate(int x, int y, int opacity, int type, LuaTexture text, int rarity, int nameplateId) {
		if (!Available) return;

		RunLuaCode(lfDrawTitlePlate, x, y, opacity, type, text, rarity, nameplateId);
	}

	public void Update(params object[] args) {
		if (!Available) return;

		RunLuaCode(lfUpdate, args);
	}

	public void Draw(int x, int y, int opacity, int player, int side) {
		if (!Available) return;

		RunLuaCode(lfDraw, x, y, opacity, player, side);
	}
}
