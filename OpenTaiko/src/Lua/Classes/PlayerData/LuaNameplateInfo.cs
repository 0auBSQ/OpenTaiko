namespace OpenTaiko {
	internal class LuaNameplateInfo {
		private DBNameplateUnlockables.NameplateUnlockable _npu;
		private LuaUnlockCondition _lUC;
		private int _id;

		public string Title {
			get {
				return _npu.nameplateInfo.cld.GetString("");
			}
		}

		public int Type {
			get {
				return _npu.nameplateInfo.iType;
			}
		}

		public int Id {
			get {
				return _id;
			}
		}

		#region [Generic]

		public string Rarity {
			get {
				return _npu.rarity;
			}
		}

		#endregion

		public LuaNameplateInfo(DBNameplateUnlockables.NameplateUnlockable npu, int id) {
			this._npu = npu;
			this._lUC = new LuaUnlockCondition(npu.unlockConditions);
			this._id = id;
		}

		public LuaNameplateInfo() {
			#region [Generate a temporary beginner nameplate on the go]
			var _nnp = new DBNameplateUnlockables.NameplateUnlockable();
			_nnp.rarity = "Common";
			_nnp.nameplateInfo = new SaveFile.CNamePlateTitle(0);
			_nnp.nameplateInfo.cld.SetString("default", "Beginner");
			_nnp.nameplateInfo.cld.SetString("ja", "初心者");
			#endregion
			this._npu = _nnp;
			this._id = -1;
			// placeholder
			this._lUC = null;
		}
	}
}
