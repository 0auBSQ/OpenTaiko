using System.Drawing;

namespace OpenTaiko {
	internal class LuaSongNode {
		private CSongListNode? _node;
		private List<LuaSongChart> _charts = new List<LuaSongChart>();
		private List<LuaSongNode> _children = new List<LuaSongNode>();
		private LuaSongNode? _parent;

		public bool NotNull {
			get {
				return _node != null;
			}
		}

		#region [Node type]

		public CSongListNode.ENodeType? NodeType {
			get {
				return _node?.nodeType ?? null;
			}
		}

		public bool IsBox {
			get {
				return NodeType == CSongListNode.ENodeType.BOX;
			}
		}

		public bool IsRandom {
			get {
				return NodeType == CSongListNode.ENodeType.RANDOM;
			}
		}

		public bool IsReturn {
			get {
				return NodeType == CSongListNode.ENodeType.BACKBOX;
			}
		}

		public bool IsSong {
			get {
				return NodeType == CSongListNode.ENodeType.SCORE;
			}
		}

		#endregion

		#region [Visuals]

		public string? BoxType {
			get {
				return _node?.BoxType ?? null;
			}
		}

		public string? BgType {
			get {
				return _node?.BgType ?? null;
			}
		}

		public string? BoxChara {
			get {
				return _node?.BoxChara ?? null;
			}
		}

		public Color? ForeColor {
			get {
				return _node?.ForeColor ?? null;
			}
		}

		public Color? BackColor {
			get {
				return _node?.BackColor ?? null;
			}
		}

		public Color? BoxColor {
			get {
				return _node?.BoxColor ?? null;
			}
		}


		#endregion

		#region [General metadata]

		public string? UniqueId {
			get {
				return _node?.tGetUniqueId() ?? null;
			}
		}

		public string? Title {
			get {
				return _node?.ldTitle.GetString("") ?? null;
			}
		}

		public string? Subtitle {
			get {
				return _node?.ldSubtitle.GetString("") ?? null;
			}
		}

		public string? Genre {
			get {
				return _node?.songGenre ?? null;
			}
		}

		public string? Maker {
			get {
				return _node?.strMaker ?? null;
			}
		}

		public string[]? Charters {
			get {
				return _node?.strMaker.SplitByCommas() ?? null;
			}
		}

		#endregion

		#region [Extended metadata]

		public bool? Explicit {
			get {
				return _node?.bExplicit ?? null;
			}
		}

		public bool? HasVideo {
			get {
				return _node?.bMovie ?? null;
			}
		}

		#endregion

		private void _FetchCharts() {
			_charts = new List<LuaSongChart>();

			for (int i = 0; i < (int)Difficulty.Total; i++) {
				if (_node?.nLevel[i] != -1) _charts.Add(new LuaSongChart(this, _node, i));
			}
		}

		private void _FetchChildren() {
			_children = new List<LuaSongNode>();

			_node?.childrenList.ForEach((child) => {
				LuaSongNode _child = new LuaSongNode(child, this);
				_children.Add(_child);
			});
		}

		public void AttachSongListNode(CSongListNode node) {
			_node = node;
			_FetchCharts();
			_FetchChildren();
		}

		public LuaSongNode() {

		}

		public LuaSongNode(CSongListNode node, LuaSongNode? parent = null) {
			AttachSongListNode(node);
			this._parent = parent;
		}
	}
}
