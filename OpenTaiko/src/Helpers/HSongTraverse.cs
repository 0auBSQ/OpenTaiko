namespace OpenTaiko;

class HSongTraverse {
	public static List<string> SpecialFolders = new List<string> { "Favorite", "最近遊んだ曲", "SearchD", "SearchT" };

	public static bool IsRegularFolder(CSongListNode node) {
		if (node.nodeType != CSongListNode.ENodeType.BOX) return false;
		if (SpecialFolders.Contains(node.songGenre)) return false;
		return true;

	}
	public static int GetSongsMatchingCondition(CSongListNode parentBox, Func<CSongListNode, bool> payload) {
		int count = 0;

		foreach (CSongListNode child in parentBox.childrenList) {
			if (IsRegularFolder(child)) count += GetSongsMatchingCondition(child, payload);
			else if (child.nodeType == CSongListNode.ENodeType.SCORE && payload(child)) count += 1;
		}

		return count;
	}
}
