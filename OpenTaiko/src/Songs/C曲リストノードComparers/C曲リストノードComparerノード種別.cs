namespace TJAPlayer3.C曲リストノードComparers {
	internal sealed class C曲リストノードComparerノード種別 : IComparer<CSongListNode> {
		public int Compare(CSongListNode x, CSongListNode y) {
			return ToComparable(x.eノード種別).CompareTo(ToComparable(y.eノード種別));
		}

		private static int ToComparable(CSongListNode.ENodeType eノード種別) {
			switch (eノード種別) {
				case CSongListNode.ENodeType.BOX:
					return 0;
				case CSongListNode.ENodeType.SCORE:
				case CSongListNode.ENodeType.SCORE_MIDI:
					return 1;
				case CSongListNode.ENodeType.UNKNOWN:
					return 2;
				case CSongListNode.ENodeType.RANDOM:
					return 4;
				case CSongListNode.ENodeType.BACKBOX:
					return 3;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
