using Color = System.Drawing.Color;

namespace OpenTaiko {
	public class LuaNameplateFunc {

		public void DrawTitlePlate(double x, double y, int opacity, int type, LuaTexture text, int rarity, object nameplateId) {
			OpenTaiko.NamePlate?.DrawTitlePlate(x, y, opacity, type, text, rarity, DBNameplateUnlockables.tKey(nameplateId));
		}

		public void DrawDanPlate(double x, double y, int opacity, int danGrade, LuaTexture text) {
			OpenTaiko.NamePlate?.DrawDan(x, y, opacity, danGrade, text);
		}

		public void DrawPlayerNameplate(double x, double y, int opacity, int player) {
			OpenTaiko.NamePlate?.Draw(x, y, opacity, player, OpenTaiko.P1IsBlue() ? 1 : 0);
		}

		public void DrawNameplateTitleById(object id, double x, double y, int opacity, LuaText font) {
			string key = DBNameplateUnlockables.tKey(id);
			var db = OpenTaiko.Databases.DBNameplateUnlockables;
			if (db != null && db.data.TryGetValue(key, out var _np)) {
				LuaTexture _tx = font.GetText(_np.nameplateInfo.cld.GetString(""), false, 1000, new LuaColor(Color.Black), new LuaColor(Color.Transparent));
				DrawTitlePlate(x, y, opacity, _np.nameplateInfo.iType, _tx, HRarity.tRarityToLangInt(_np.rarity), key);
			}
		}
	}
}
