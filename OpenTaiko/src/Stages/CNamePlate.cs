namespace OpenTaiko {
	class CNamePlate {
		public CLuaNamePlateScript lcNamePlate { get; private set; }
		public void RefleshSkin() {
			lcNamePlate?.Dispose();
			lcNamePlate = new CLuaNamePlateScript(CSkin.Path("Modules/NamePlate"));

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
			RefleshSkin();
		}

		public void tNamePlateRefreshTitles(int player) {
			int actualPlayer = OpenTaiko.GetActualPlayer(player);

			string[] stages = { "初", "二", "三", "四", "五", "六", "七", "八", "九", "極" };

			string name;
			string title;
			string dan;

			bool isAI = OpenTaiko.ConfigIni.bAIBattleMode && player == 1;
			if (isAI) {
				name = CLangManager.LangInstance.GetString("AI_NAME");
				title = CLangManager.LangInstance.GetString("AI_TITLE");
				dan = stages[Math.Max(0, OpenTaiko.ConfigIni.nAILevel - 1)] + "面";
			} else {
				name = OpenTaiko.SaveFileInstances[actualPlayer].data.Name;
				title = OpenTaiko.SaveFileInstances[actualPlayer].data.Title;
				dan = OpenTaiko.SaveFileInstances[actualPlayer].data.Dan;
			}
			bIsPrevAI[player] = isAI;

			if (OpenTaiko.SaveFileInstances[player].data.DanGold)
				lcNamePlate.SetInfos(player, name, title, $"<g.#FFE34A.#EA9622>{dan}</g>", OpenTaiko.SaveFileInstances[actualPlayer].data);
			else
				lcNamePlate.SetInfos(player, name, title, dan, OpenTaiko.SaveFileInstances[actualPlayer].data);
		}


		public void tNamePlateDraw(int x, int y, int player, bool bTitle = false, int Opacity = 255) {
			float resolutionScaleX = OpenTaiko.Skin.Resolution[0] / 1280.0f;
			float resolutionScaleY = OpenTaiko.Skin.Resolution[1] / 720.0f;

			int basePlayer = player;
			player = OpenTaiko.GetActualPlayer(player);

			bool isAI = OpenTaiko.ConfigIni.bAIBattleMode && basePlayer == 1;
			if (bIsPrevAI[basePlayer] != isAI) {
				tNamePlateRefreshTitles(player);
			}
			bIsPrevAI[basePlayer] = isAI;

			lcNamePlate.Draw(x, y, Opacity, basePlayer, OpenTaiko.P1IsBlue() ? 1 : 0);
		}

		private bool[] bIsPrevAI = new bool[5];
	}
}
