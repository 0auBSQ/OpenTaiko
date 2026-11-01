namespace OpenTaiko {
	public class LuaNameplateInfo {
		private DBNameplateUnlockables.NameplateUnlockable _npu;
		private LuaUnlockCondition _lUC;
		private string _id;

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

		/// <summary>The nameplate id: the number as text for the shipped ones ("66"), "" for no nameplate.</summary>
		public string Id {
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

		/// <summary>The rarity as the number DrawTitlePlate expects (Poor 0 … Mythical 6).</summary>
		public int RarityInt => HRarity.tRarityToLangInt(_npu.rarity);

		/// <summary>The id of the pack the nameplate comes from, or "" for the shipped nameplates.</summary>
		public string Pack => _npu.pack;

		/// <summary>The name of the nameplate's pack in the current language (its "name", else its id; "OpenTaiko" for the shipped nameplates).</summary>
		public string PackName => _npu.packName?.GetString(_npu.pack) ?? _npu.pack;

		public LuaUnlockCondition UnlockCondition => _lUC;

		#endregion

		internal LuaNameplateInfo(DBNameplateUnlockables.NameplateUnlockable npu, string id) {
			this._npu = npu;
			this._lUC = new LuaUnlockCondition(npu.unlockConditions);
			this._id = id;
		}

		/// <summary>No nameplate: no title, plate 0, Common.</summary>
		public LuaNameplateInfo() {
			var _nnp = new DBNameplateUnlockables.NameplateUnlockable();
			_nnp.rarity = "Common";
			_nnp.nameplateInfo = new SaveFile.CNamePlateTitle(0);
			this._npu = _nnp;
			this._id = "";
			this._lUC = new LuaUnlockCondition();
		}
	}
}
