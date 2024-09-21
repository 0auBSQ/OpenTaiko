namespace OpenTaiko {
	class HSongTraverse {
		public static List<string> SpecialFolders = new List<string> { "Favorite", "最近遊んだ曲", "SearchD" };

		public static bool IsRegularFolder(CSongListNode node) {
			if (node.eノード種別 != CSongListNode.ENodeType.BOX) return false;
			if (SpecialFolders.Contains(node.strジャンル)) return false;
			return true;

		}
		public static int GetSongsMatchingCondition(CSongListNode parentBox, Func<CSongListNode, bool> payload) {
			int count = 0;

			foreach (CSongListNode child in parentBox.list子リスト) {
				if (IsRegularFolder(child)) count += GetSongsMatchingCondition(child, payload);
				else if (child.eノード種別 == CSongListNode.ENodeType.SCORE && payload(child)) count += 1;
			}

			return count;
		}
	}
}
