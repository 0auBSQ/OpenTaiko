namespace OpenTaiko {
	internal class LuaSongList {
		private LuaSongNodeRoot? _root;
		private List<LuaSongNode> _currentPage = new List<LuaSongNode>();
		private LuaSongNode? _currentNode = null;
		private LuaSongListSettings _settings;

		public void ReloadSongList() {
			_root = new LuaSongNodeRoot();

			var _scanningList = OpenTaiko.Songs管理.list曲ルート;

			if (_settings.RootGenreFolder != null) {

				#region [If a specific root genre folder is given, process a DFS to get the first potential root node found]

				T? FindNode<T>(IEnumerable<T> roots, Func<T, bool> predicate, Func<T, IEnumerable<T>> childrenSelector) {
					foreach (var node in roots) {
						if (predicate(node))
							return node;

						var children = childrenSelector(node);
						if (children != null) {
							var found = FindNode(children, predicate, childrenSelector);
							if (found != null)
								return found;
						}
					}
					return default;
				}

				var _found = FindNode(
					_scanningList,
					node => node.songGenre == _settings.RootGenreFolder && node.nodeType == CSongListNode.ENodeType.BOX,
					node => node.childrenList
				);

				if (_found != null) _scanningList = _found.childrenList;

				#endregion

			}

			_scanningList.ForEach((song) => {
				if (_settings.IsNodeExcludedAtGeneration(song) == false) {
					LuaSongNode _node = new LuaSongNode(song, _root, true, _settings);
					_root.AppendChild(_node);
				}
			});
			_settings.AlterRootNodeWithRequestedNodes(_root);

			_currentNode = null;
			_currentPage = GetRootPage();
			if (_currentPage.Count > 0) _currentNode = _currentPage[0];
		}

		public LuaSongList(LuaSongListSettings settings) {
			_settings = settings;
			ReloadSongList();
		}

		private List<LuaSongNode> GetLeaves() {
			if (_root == null) return new List<LuaSongNode>();

			List<LuaSongNode> _leaves = new List<LuaSongNode>();

			void DFS(LuaSongNode node) {
				if (node.IsLeaf)
					_leaves.Add(node);
				else {
					// Don't crawl closen folder which are considered leaves
					foreach (LuaSongNode child in node.Children)
						DFS(child);
				}
			}

			DFS(_root);
			return _leaves;
		}

		private List<LuaSongNode> GetRootPage() {
			List<LuaSongNode> _page = _root?.Children ?? new List<LuaSongNode>();

			return _page;
		}

		private List<LuaSongNode> GetCurrentPage() {
			List<LuaSongNode> _page = new List<LuaSongNode>();

			if (_settings.FlattenOpennedFolders == false) _page = _currentNode?.Siblings ?? new List<LuaSongNode>();
			else _page = GetLeaves();

			_page.RemoveAll(node => _settings.IsNodeExcludedAtExecution(node) == true);

			return _page;
		}

		private int GetIndexInPage(LuaSongNode? _node) {
			if (_node == null || _currentPage.Count == 0) return -1;
			return _currentPage.IndexOf(_node);
		}

		public LuaSongNode? GetSongNodeAtOffset(int offset) {
			int _curidx = GetIndexInPage(_currentNode);
			if (_curidx < 0) return null;

			int _idx = _curidx + offset;

			if (_settings.ModuloPagination == true) {
				int _count = _currentPage.Count;
				int _modidx = ((_idx % _count) + _count) % _count;
				return _currentPage[_modidx];
			} else if (_idx >= 0 && _idx < _currentPage.Count) return _currentPage[_idx];
			return null;
		}

		public LuaSongNode? GetSelectedSongNode() {
			return _currentNode;
		}

		public void Move(int offset) {
			int _curidx = GetIndexInPage(_currentNode);
			if (_curidx < 0) return;

			int _newidx = _curidx + offset;
			if (_settings.ModuloMovement == true) {
				int _count = _currentPage.Count;
				int _modidx = ((_newidx % _count) + _count) % _count;
				_currentNode = _currentPage[_modidx];
			} else {
				int _fixidx = Math.Max(0, Math.Min(_currentPage.Count - 1, _newidx));
				_currentNode = _currentPage[_fixidx];
			}
		}

		public bool OpenFolder() {
			if (_currentNode == null) return false;

			if (_currentNode.IsFolder && !_currentNode.Opened && _currentNode.ChildrenCount > 0) {
				_currentNode.Opened = true;
				_currentNode = _currentNode.Child(0);
				_currentPage = GetCurrentPage();
				return true;
			}
			return false;
		}

		public bool CloseFolder() {
			if (_currentNode == null) return false;

			if (!_currentNode.IsRoot && _currentNode.Parent.IsFolder && _currentNode.Parent.Opened) {
				_currentNode = _currentNode.Parent;
				_currentNode.Opened = false;
				_currentPage = GetCurrentPage();
				return true;
			}
			return false;
		}
	}
}
