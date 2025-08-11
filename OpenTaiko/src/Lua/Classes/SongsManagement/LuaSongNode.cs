using System.Drawing;

namespace OpenTaiko {
	internal class LuaSongNode {
		private CSongListNode? _node;
		private List<LuaSongChart> _charts = new List<LuaSongChart>();
		protected List<LuaSongNode> _children = new List<LuaSongNode>();
		protected LuaSongNode? _parent;

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

		public bool IsFolder {
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

		#region [Folder specific]

		private bool _opened = false;

		public bool Opened {
			get {
				return IsFolder && _opened;
			}
			set {
				_opened = value;
			}
		}

		#endregion

		#endregion

		#region [Tree structure]

		public int ChildrenCount {
			get {
				if (!IsFolder && !IsRoot) return 0;
				return _children.Count;
			}
		}

		public bool IsLeaf {
			get {
				return (ChildrenCount == 0 || !Opened) && !IsRoot;
			}
		}

		public bool IsRoot {
			get {
				return _parent == null;
			}
		}

		public LuaSongNode? Parent {
			get {
				return _parent;
			}
		}

		public List<LuaSongNode> Siblings {
			get {
				return (Parent != null) ? Parent.Children : new List<LuaSongNode>() { this };
			}
		}

		public LuaSongNode? Child(int i) {
			return _children[i];
		}

		public List<LuaSongNode> Children {
			get {
				return _children;
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

			_node?.childrenList?.ForEach((child) => {
				LuaSongNode _child = new LuaSongNode(child, this);
				_children.Add(_child);
			});
		}

		public void AttachSongListNode(CSongListNode node, bool recursive = true) {
			_node = node;
			_FetchCharts();
			if (recursive) _FetchChildren();
		}

		// Mount the song node so it gets played when transitioning to the gameplay screen
		public bool Mount(int p1diff = 0, int p2diff = 0, int p3diff = 0, int p4diff = 0, int p5diff = 0) {
			if (!IsSong || _node == null) return false;

			int[] diffs = { p1diff, p2diff, p3diff, p4diff, p5diff };

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				// Difficulty out of bounds
				if (diffs[i] < 0 || diffs[i] >= (int)Difficulty.Total) return false;

				// Difficulty not found in chart
				LuaSongChart? _chart = _charts.FirstOrDefault(x => (int)x.Difficulty == diffs[i]);
				if (_chart == null) return false;

				// Mount difficulty if valid
				_chart.Select(i);
			}

			OpenTaiko.stageSongSelect.rChoosenSong = _node;
			OpenTaiko.stageSongSelect.str確定された曲のジャンル = Genre ?? "???";

			return true;
		}

		public LuaSongNode() {

		}

		public LuaSongNode(CSongListNode node, LuaSongNode? parent = null, bool recursive = true) {
			AttachSongListNode(node, recursive);
			this._parent = parent;
		}
	}
}
