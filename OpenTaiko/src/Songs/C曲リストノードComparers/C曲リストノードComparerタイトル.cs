namespace TJAPlayer3.C曲リストノードComparers {
	internal sealed class C曲リストノードComparerタイトル : IComparer<CSongListNode> {
		private readonly int _order;

		public C曲リストノードComparerタイトル(int order) {
			this._order = order;
		}

		public int Compare(CSongListNode n1, CSongListNode n2) {
			return _order * n1.ldTitle.GetString("").CompareTo(n2.ldTitle.GetString(""));
		}
	}
}
