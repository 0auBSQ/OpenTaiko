namespace OpenTaiko.CSongListNodeComparers;

internal sealed class CSongListNodeComparerSubtitle : IComparer<CSongListNode> {
	private readonly int _order;

	public CSongListNodeComparerSubtitle(int order) {
		this._order = order;
	}

	public int Compare(CSongListNode n1, CSongListNode n2) {
		return _order * n1.ldSubtitle.GetString("").CompareTo(n2.ldSubtitle.GetString(""));
	}
}
