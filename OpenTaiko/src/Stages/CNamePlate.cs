using FDK;

namespace OpenTaiko;

class CNamePlate {
	private static LuaROActivityWrapper? Script => LuaROActivityWrapper.GetROActivity("nameplate");

	public void RefleshSkin() {
		for (int player = 0; player < 5; player++) {
			tNamePlateRefreshTitles(player);
		}
	}

	public CNamePlate() {
		for (int player = 0; player < 5; player++) {
			if (OpenTaiko.SaveFileInstances[player].data.DanType < 0) OpenTaiko.SaveFileInstances[player].data.DanType = 0;
			else if (OpenTaiko.SaveFileInstances[player].data.DanType > 2) OpenTaiko.SaveFileInstances[player].data.DanType = 2;

			if (OpenTaiko.SaveFileInstances[player].data.TitleType < 0) OpenTaiko.SaveFileInstances[player].data.TitleType = 0;
		}
	}

	public void tNamePlateRefreshTitles(int player) {
		int actualPlayer = OpenTaiko.GetActualPlayer(player);

		string[] stages = { "初", "二", "三", "四", "五", "六", "七", "八", "九", "極" };

		string name;
		string title;
		string dan;

		bool isAI = OpenTaiko.ConfigIni.bAIBattleMode && player == 1;
		if (isAI) {
			name  = CLangManager.LangInstance.GetString("AI_NAME");
			title = CLangManager.LangInstance.GetString("AI_TITLE");
			dan   = stages[Math.Max(0, OpenTaiko.ConfigIni.nAILevel - 1)] + "面";
		} else {
			name  = OpenTaiko.SaveFileInstances[actualPlayer].data.Name;
			title = OpenTaiko.SaveFileInstances[actualPlayer].data.Title;
			dan   = OpenTaiko.SaveFileInstances[actualPlayer].data.Dan;
		}
		bIsPrevAI[player] = isAI;

		if (OpenTaiko.SaveFileInstances[player].data.DanGold)
			Script?.Call("setInfos", player, name, title, $"<g.#FFE34A.#EA9622>{dan}</g>", OpenTaiko.SaveFileInstances[actualPlayer].data);
		else
			Script?.Call("setInfos", player, name, title, dan, OpenTaiko.SaveFileInstances[actualPlayer].data);
	}

	public void tNamePlateDraw(int x, int y, int player, bool bTitle = false, int Opacity = 255) {
		int basePlayer = player;
		player = OpenTaiko.GetActualPlayer(player);

		bool isAI = OpenTaiko.ConfigIni.bAIBattleMode && basePlayer == 1;
		if (bIsPrevAI[basePlayer] != isAI) {
			tNamePlateRefreshTitles(player);
		}
		bIsPrevAI[basePlayer] = isAI;

		Draw(x, y, Opacity, basePlayer, OpenTaiko.P1IsBlue() ? 1 : 0);
	}

	/// <summary>Draws the full nameplate for a player slot.</summary>
	public void Draw(int x, int y, int opacity, int player, int side) {
		Script?.Call("draw", x, y, opacity, player, side);
	}

	/// <summary>Draws only the dan plate (used in My Room gallery).</summary>
	public void DrawDan(int x, int y, int opacity, int danGrade, LuaTexture text) {
		Script?.Call("drawDan", x, y, opacity, danGrade, text);
	}

	/// <inheritdoc cref="DrawDan(int,int,int,int,LuaTexture)"/>
	public void DrawDan(int x, int y, int opacity, int danGrade, CTexture text) =>
		DrawDan(x, y, opacity, danGrade, new LuaTexture(text));

	/// <summary>Draws only the title plate (used in My Room gallery and modal).</summary>
	public void DrawTitlePlate(int x, int y, int opacity, int type, LuaTexture text, int rarity, int nameplateId) {
		Script?.Call("drawTitlePlate", x, y, opacity, type, text, rarity, nameplateId);
	}

	/// <inheritdoc cref="DrawTitlePlate(int,int,int,int,LuaTexture,int,int)"/>
	public void DrawTitlePlate(int x, int y, int opacity, int type, CTexture text, int rarity, int nameplateId) =>
		DrawTitlePlate(x, y, opacity, type, new LuaTexture(text), rarity, nameplateId);

	/// <summary>Advances nameplate animation state; call once per frame.</summary>
	public void Update() {
		Script?.Update();
	}

	private bool[] bIsPrevAI = new bool[5];
}
