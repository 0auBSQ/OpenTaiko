namespace OpenTaiko.CSongListNodeComparers;

internal sealed class CSongListNodeComparerUnlockStatus : IComparer<CSongListNode> {

	public int Compare(CSongListNode n1, CSongListNode n2) {
		int _n1s = (n1.nodeType != CSongListNode.ENodeType.SCORE) ? 0 : 1;
		int _n2s = (n2.nodeType != CSongListNode.ENodeType.SCORE) ? 0 : 1;


		if (_n1s == 0 || _n2s == 0) {
			return 0;
		}
		return _unlockStatusToInt(n1).CompareTo(_unlockStatusToInt(n2));
	}

	private int _unlockStatusToInt(CSongListNode n1) {
		if (!OpenTaiko.Databases.DBSongUnlockables.tIsSongLocked(n1)) return 0;
		return (int)OpenTaiko.Databases.DBSongUnlockables.tGetSongHiddenIndex(n1) + 1;
	}
}
