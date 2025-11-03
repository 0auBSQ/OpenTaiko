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

			if (_settings.FlattenOpenedFolders == false) _page = _currentNode?.Siblings ?? new List<LuaSongNode>();
			else _page = GetLeaves();

			_page.RemoveAll(node => _settings.IsNodeExcludedAtExecution(node) == true);
			// TODO: Generate backboxes here instead of on generation to avoid weird gaps

			return _page;
		}

		private int GetIndexInPage(LuaSongNode? _node) {
			if (_node == null || _currentPage.Count == 0) return -1;
			return _currentPage.IndexOf(_node);
		}

		#region [(Public) Fetchers and search methods]

		#region [(Private) Graph search algorithms]

		private LuaSongNode? FindFirst(Func<LuaSongNode, bool> predicate, LuaSongNode? root = null, bool onlySongs = true) {
			if (root == null) return null;

			LuaSongNode? DFS(LuaSongNode node, bool onlySongs) {
				if (predicate(node) && (!onlySongs || node.IsSong))
					return node;

				foreach (var child in node.Children) {
					var found = DFS(child, onlySongs);
					if (found != null) return found;
				}
				return null;
			}

			return DFS(root, onlySongs);
		}

		private List<LuaSongNode> FindAll(Func<LuaSongNode, bool> predicate, LuaSongNode? root = null, bool onlySongs = true) {
			var results = new List<LuaSongNode>();
			if (root == null) return results;

			void DFS(LuaSongNode node, bool onlySongs) {
				if (predicate(node) && (!onlySongs || node.IsSong))
					results.Add(node);

				foreach (var child in node.Children)
					DFS(child, onlySongs);
			}

			DFS(root, onlySongs);
			return results;
		}

		#endregion

		public LuaSongNode? GetSongByUniqueId(string id) {
			return this.FindFirst((node) => node.UniqueId == id, _root);
		}

		public LuaSongNode? GetRandomNodeInFolder(LuaSongNode randomBoxLocation, bool recursive = true, Func<LuaSongNode, bool>? predicate = null) {
			List<LuaSongNode> _randomPool = new List<LuaSongNode>();

			predicate ??= nd => true;
			randomBoxLocation.Siblings.ForEach(node => {
				if (node.IsSong && !node.IsLocked && predicate(node)) _randomPool.Add(node);
				if (recursive == true && node.IsFolder) _randomPool.AddRange(FindAll(cnode => cnode.IsSong && !cnode.IsLocked && predicate(cnode), node));
			});

			if (_randomPool.Count == 0) return null;

			Random _random = new Random();
			int _randomIdx = _random.Next(_randomPool.Count);
			return _randomPool[_randomIdx];
		}

		#region [Temporary, give a method to attach it to a folder instead?]

		public List<LuaSongNode> SearchSongsByPredicate(Func<LuaSongNode, bool> predicate) {
			return this.FindAll(predicate, _root, true);
		}

		// Placeholder for testing the predicate
		public LuaSongNode? SearchFirstSongByPredicate(Func<LuaSongNode, bool> predicate) {
			return this.FindFirst(predicate, _root, true);
		}

		public List<LuaSongNode> SearchNodesByPredicate(Func<LuaSongNode, bool> predicate) {
			return this.FindAll(predicate, _root, false);
		}

		#endregion


		#endregion

		#region [(Public) Folder navigation]

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
				// BETTER: Track index of the selected node before closing the folder to go back to it directly when reopening the folder?
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

		#endregion
	}
}
