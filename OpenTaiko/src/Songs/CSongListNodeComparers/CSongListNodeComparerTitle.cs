namespace OpenTaiko.CSongListNodeComparers;

internal sealed class CSongListNodeComparerTitle : IComparer<CSongListNode> {
	private readonly int _order;

	public CSongListNodeComparerTitle(int order) {
		this._order = order;
	}

	public int Compare(CSongListNode n1, CSongListNode n2) {
		return _order * n1.ldTitle.GetString("").CompareTo(n2.ldTitle.GetString(""));
	}
}
