using FDK;
using NLua;

namespace TJAPlayer3 {
	internal class CLuaNamePlateScript : CLuaScript {
		private LuaFunction lfGetCharaOffset;
		private LuaFunction lfSetInfos;
		private LuaFunction lfDrawDan;
		private LuaFunction lfDrawTitlePlate;
		private LuaFunction lfUpdate;
		private LuaFunction lfDraw;

		public CLuaNamePlateScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets) {
			lfGetCharaOffset = (LuaFunction)LuaScript["getCharaOffset"];
			lfSetInfos = (LuaFunction)LuaScript["setInfos"];
			lfDrawDan = (LuaFunction)LuaScript["drawDan"];
			lfDrawTitlePlate = (LuaFunction)LuaScript["drawTitlePlate"];
			lfUpdate = (LuaFunction)LuaScript["update"];
			lfDraw = (LuaFunction)LuaScript["draw"];
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

			RunLuaCode(lfDrawDan, x, y, opacity, danGrade, titleTex);
		}

		// For My Room
		public void DrawTitlePlate(int x, int y, int opacity, int type, CTexture titleTex, int rarity, int nameplateId) {
			if (!Available) return;

			RunLuaCode(lfDrawTitlePlate, x, y, opacity, type, titleTex, rarity, nameplateId);
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
}
