namespace OpenTaiko {
	internal class LuaDanplateInfo {
		private SaveFile _sf;

		public string Title {
			get {
				return _sf.data.Dan;
			}
		}

		public bool Gold {
			get {
				return _sf.data.DanGold;
			}
		}

		public int ClearStatus {
			get {
				return _sf.data.DanType;
			}
		}

		public LuaDanplateInfo(SaveFile sf) {
			this._sf = sf;
		}
	}
}
