namespace OpenTaiko.CSongListNodeComparers;

internal sealed class CSongListNodeComparerPath : IComparer<CSongListNode> {
	private readonly int _order;

	public CSongListNodeComparerPath(int order) {
		this._order = order;
	}

	public int Compare(CSongListNode n1, CSongListNode n2) {
		if ((n1.nodeType == CSongListNode.ENodeType.BOX) && (n2.nodeType == CSongListNode.ENodeType.BOX)) {
			return _order * n1.score[0].ファイル情報.フォルダの絶対パス.CompareTo(n2.score[0].ファイル情報.フォルダの絶対パス);
		}

		var str = filePath(n1);
		var strB = filePath(n2);

		return _order * str.CompareTo(strB);
	}

	private static string filePath(CSongListNode songNode) {
		for (int i = 0; i < (int)Difficulty.Total; i++) {
			if (songNode.score[i] != null) {
				return songNode.score[i].ファイル情報.ファイルの絶対パス ?? "";
			}
		}

		return "";
	}
}
