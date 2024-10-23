namespace OpenTaiko.CSongListNodeComparers;

internal sealed class CSongListNodeComparerPath : IComparer<CSongListNode> {
	private readonly int _order;

	public CSongListNodeComparerPath(int order) {
		this._order = order;
	}

	public int Compare(CSongListNode n1, CSongListNode n2) {
		if ((n1.eノード種別 == CSongListNode.ENodeType.BOX) && (n2.eノード種別 == CSongListNode.ENodeType.BOX)) {
			return _order * n1.arスコア[0].ファイル情報.フォルダの絶対パス.CompareTo(n2.arスコア[0].ファイル情報.フォルダの絶対パス);
		}

		var str = filePath(n1);
		var strB = filePath(n2);

		return _order * str.CompareTo(strB);
	}

	private static string filePath(CSongListNode songNode) {
		for (int i = 0; i < (int)Difficulty.Total; i++) {
			if (songNode.arスコア[i] != null) {
				return songNode.arスコア[i].ファイル情報.ファイルの絶対パス ?? "";
			}
		}

		return "";
	}
}
