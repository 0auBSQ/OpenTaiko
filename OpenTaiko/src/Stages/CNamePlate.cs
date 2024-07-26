namespace TJAPlayer3 {
	class CNamePlate {
		public CLuaNamePlateScript lcNamePlate { get; private set; }
		public void RefleshSkin() {
			/*
            for (int player = 0; player < 5; player++)
            {
                this.pfName[player]?.Dispose();

                if (TJAPlayer3.SaveFileInstances[player].data.Title == "" || TJAPlayer3.SaveFileInstances[player].data.Title == null)
                    this.pfName[player] = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.NamePlate_Font_Name_Size_Normal);
                else
                    this.pfName[player] = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.NamePlate_Font_Name_Size_WithTitle);
            }

            this.pfTitle?.Dispose();
            this.pfdan?.Dispose();

            this.pfTitle = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.NamePlate_Font_Title_Size);
            this.pfdan = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.NamePlate_Font_Dan_Size);
            */
			lcNamePlate?.Dispose();
			lcNamePlate = new CLuaNamePlateScript(CSkin.Path("Modules/NamePlate"));

			for (int player = 0; player < 5; player++) {
				tNamePlateRefreshTitles(player);
			}
		}

		public CNamePlate() {
			for (int player = 0; player < 5; player++) {
				if (TJAPlayer3.SaveFileInstances[player].data.DanType < 0) TJAPlayer3.SaveFileInstances[player].data.DanType = 0;
				else if (TJAPlayer3.SaveFileInstances[player].data.DanType > 2) TJAPlayer3.SaveFileInstances[player].data.DanType = 2;

				if (TJAPlayer3.SaveFileInstances[player].data.TitleType < 0) TJAPlayer3.SaveFileInstances[player].data.TitleType = 0;

			}
			RefleshSkin();

			//ctNamePlateEffect = new CCounter(0, 120, 16.6f, TJAPlayer3.Timer);
			//ctAnimatedNamePlateTitle = new CCounter(0, 10000, 60.0f, TJAPlayer3.Timer);
		}

		/*
        public void tNamePlateDisplayNamePlateBase(int x, int y, int item)
        {
            int namePlateBaseX = TJAPlayer3.Tx.NamePlateBase.szTextureSize.Width;
            int namePlateBaseY = TJAPlayer3.Tx.NamePlateBase.szTextureSize.Height / 12;

            TJAPlayer3.Tx.NamePlateBase?.t2D描画(x, y, new RectangleF(0, item * namePlateBaseY, namePlateBaseX, namePlateBaseY));

        }

        public void tNamePlateDisplayNamePlate_Extension(int x, int y, int item)
        {
            int namePlateBaseX = TJAPlayer3.Tx.NamePlate_Extension.szTextureSize.Width;
            int namePlateBaseY = TJAPlayer3.Tx.NamePlate_Extension.szTextureSize.Height / 12;

            TJAPlayer3.Tx.NamePlate_Extension?.t2D描画(x, y, new RectangleF(0, item * namePlateBaseY, namePlateBaseX, namePlateBaseY));

        }
        */

		public void tNamePlateRefreshTitles(int player) {
			int actualPlayer = TJAPlayer3.GetActualPlayer(player);

			string[] stages = { "初", "二", "三", "四", "五", "六", "七", "八", "九", "極" };

			string name;
			string title;
			string dan;

			bool isAI = TJAPlayer3.ConfigIni.bAIBattleMode && player == 1;
			if (isAI) {
				name = CLangManager.LangInstance.GetString("AI_NAME");
				title = CLangManager.LangInstance.GetString("AI_TITLE");
				dan = stages[Math.Max(0, TJAPlayer3.ConfigIni.nAILevel - 1)] + "面";
			} else {
				name = TJAPlayer3.SaveFileInstances[player].data.Name;
				title = TJAPlayer3.SaveFileInstances[player].data.Title;
				dan = TJAPlayer3.SaveFileInstances[player].data.Dan;
			}
			bIsPrevAI[player] = isAI;

			/*
            txTitle[player] = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(new TitleTextureKey(title, pfTitle, Color.Black, Color.Empty, 1000));
            txName[player] = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(new TitleTextureKey(name, pfName[player], Color.White, Color.Black, 1000));
            if (TJAPlayer3.SaveFileInstances[player].data.DanGold) txdan[player] = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(new TitleTextureKey($"<g.#FFE34A.#EA9622>{dan}</g>", pfdan, Color.White, Color.Black, 1000));
            else txdan[player] = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(new TitleTextureKey(dan, pfdan, Color.White, Color.Black, 1000));
            */

			if (TJAPlayer3.SaveFileInstances[player].data.DanGold)
				lcNamePlate.SetInfos(player, name, title, $"<g.#FFE34A.#EA9622>{dan}</g>", TJAPlayer3.SaveFileInstances[player].data);
			else
				lcNamePlate.SetInfos(player, name, title, dan, TJAPlayer3.SaveFileInstances[player].data);
		}


		public void tNamePlateDraw(int x, int y, int player, bool bTitle = false, int Opacity = 255) {
			float resolutionScaleX = TJAPlayer3.Skin.Resolution[0] / 1280.0f;
			float resolutionScaleY = TJAPlayer3.Skin.Resolution[1] / 720.0f;

			int basePlayer = player;
			player = TJAPlayer3.GetActualPlayer(player);

			bool isAI = TJAPlayer3.ConfigIni.bAIBattleMode && basePlayer == 1;
			if (bIsPrevAI[basePlayer] != isAI) {
				tNamePlateRefreshTitles(player);
			}
			bIsPrevAI[basePlayer] = isAI;

			lcNamePlate.Draw(x, y, Opacity, basePlayer, player);
		}

		private bool[] bIsPrevAI = new bool[5];
	}
}
