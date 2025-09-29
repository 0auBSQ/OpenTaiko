using Color = System.Drawing.Color;

namespace OpenTaiko {
	public class LuaNameplateFunc {

		// TODO: Improve the nameplate accessors by using LuaNameplateInfo / LuaDanplateInfo
		public void DrawTitlePlate(int x, int y, int opacity, int type, LuaTexture text, int rarity, int nameplateId) {
			OpenTaiko.NamePlate?.lcNamePlate?.DrawTitlePlate(x, y, opacity, type, text, rarity, nameplateId);
		}

		public void DrawDanPlate(int x, int y, int opacity, int danGrade, LuaTexture text) {
			OpenTaiko.NamePlate?.lcNamePlate?.DrawDan(x, y, opacity, danGrade, text);
		}

		public void DrawPlayerNameplate(int x, int y, int opacity, int player) {
			OpenTaiko.NamePlate?.lcNamePlate?.Draw(x, y, opacity, player, OpenTaiko.P1IsBlue() ? 1 : 0);
		}

		// TODO: Add getters to get nameplates from DBNameplateUnlockables for my room's gallery

		public void DrawNameplateTitleById(int id, int x, int y, int opacity, LuaText font) {
			DBNameplateUnlockables.NameplateUnlockable? _np = OpenTaiko.Databases.DBNameplateUnlockables.data?[id] ?? null;
			if (_np != null) {
				LuaTexture _tx = font.GetText(_np.nameplateInfo.cld.GetString(""), false, 1000, new LuaColor(Color.Black), new LuaColor(Color.Transparent));

				DrawTitlePlate(x, y, opacity, _np.nameplateInfo.iType, _tx, HRarity.tRarityToLangInt(_np.rarity), id);
			}
		}
	}
}
