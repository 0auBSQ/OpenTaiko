using NLua;

namespace OpenTaiko {
	internal class LuaSongList {
		private LuaSongNodeRoot? _root;
		private List<LuaSongNode> _currentPage = new List<LuaSongNode>();
		private LuaSongNode? _currentNode = null;
		private LuaSongListSettings _settings;

		// Cursor saved before entering a virtual folder; restored by CloseFolder().
		private LuaSongNode? _preVirtualFolderNode = null;

		public void ReloadSongList() {
			_root = new LuaSongNodeRoot();

			var _scanningList = OpenTaiko.SongManager.listSongRoot;

			if (_settings.RootGenreFolderNode != null) {
				var _col = _settings.RootGenreFolderNode.INTERNAL_GetChildrenList(new CBlankClass());
				if (_col != null) _scanningList = _col;
			} else if (_settings.RootGenreFolder != null) {

				#region [If a specific root genre folder is given, process a DFS to get the first potential root node found]

				T? FindNode<T>(IEnumerable<T> roots, Func<T, bool> predicate, Func<T, IEnumerable<T>?> childrenSelector) {
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

				if (_found != null) _scanningList = _found.childrenList!;

				#endregion

			}

			if (_settings.TreeSongPredicate != null) {
				var _song = _settings.TreeSongPredicate.AsPredicate<LuaSongNode>()!;
				var _folder = _settings.TreeFolderPredicate.AsPredicate<LuaSongNode, int>();
				var _entries = GatherTree(OpenTaiko.SongManager.listSongRoot,
					node => _song(new LuaSongNode(node, null, false, _settings)),
					_folder == null ? null : (node, depth) => _folder(new LuaSongNode(node, null, false, _settings), depth),
					_settings.IsNodeExcludedAtGeneration);
				var _gathered = new List<LuaSongNode>();
				MountTree(_entries, _root, _gathered);
				_gathered.ForEach(_root.AppendChild);
			} else {
				_scanningList.ForEach((song) => {
					if (_settings.IsNodeExcludedAtGeneration(song) == false) {
						LuaSongNode _node = new LuaSongNode(song, _root, true, _settings);
						_root.AppendChild(_node);
					}
				});
			}
			_settings.AlterRootNodeWithRequestedNodes(_root);

			_currentNode = null;
			_currentPage = GetRootPage();
			if (_currentPage.Count > 0) _currentNode = _currentPage[0];
			// Apply execution-time filters (ExcludeLockedSongs, ExcludeHiddenSongs, HideEmptyFolders…)
			// so that Move() and GetSongNodeAtOffset() never expose excluded nodes.
			_currentPage = GetCurrentPage();
			if (_currentPage.Count > 0 && (_currentNode == null || !_currentPage.Contains(_currentNode)))
				_currentNode = _currentPage[0];
		}

		public LuaSongList(LuaSongListSettings settings) {
			_settings = settings;
			ReloadSongList();
		}

		#region [(Private) Tree lists]

		// A song, or a folder with the entries it shows
		internal sealed class TreeEntry {
			public CSongListNode Node = null!;
			public List<TreeEntry> Children = [];
		}

		// The entries of a tree list (LuaSongListSettings.TreeSongPredicate) in path order: the songs accepts takes, the folders
		// that hold one themselves or several folders holding some, and in place of any other folder what it holds; the
		// folders opens takes (with their depth, 0 = the top level) always give way. Each song shows once: a copy inside such
		// a folder first, then the first in path order.
		internal static List<TreeEntry> GatherTree(List<CSongListNode> roots, Func<CSongListNode, bool> accepts,
			Func<CSongListNode, int, bool>? opens, Func<CSongListNode, bool> excluded) {
			var accepted = new Dictionary<CSongListNode, bool>(System.Collections.Generic.ReferenceEqualityComparer.Instance);
			bool IsSong(CSongListNode node) {
				if (node.nodeType != CSongListNode.ENodeType.SCORE || excluded(node)) return false;
				if (!accepted.TryGetValue(node, out bool result)) accepted[node] = result = accepts(node);
				return result;
			}

			var opened = new Dictionary<CSongListNode, bool>(System.Collections.Generic.ReferenceEqualityComparer.Instance);
			bool Opens(CSongListNode box, int depth) {
				if (opens == null) return false;
				if (!opened.TryGetValue(box, out bool result)) opened[box] = result = opens(box, depth);
				return result;
			}

			var kept = new Dictionary<string, CSongListNode>();
			var keptInOpened = new HashSet<string>();
			void Keep(List<CSongListNode> nodes, bool inOpened, int depth) {
				foreach (var node in nodes) {
					if (excluded(node)) continue;
					if (node.nodeType == CSongListNode.ENodeType.BOX) {
						if (node.childrenList != null) Keep(node.childrenList, inOpened || Opens(node, depth), depth + 1);
					} else if (IsSong(node)) {
						string id = node.tGetUniqueId();
						if (id == "") continue;
						if (!kept.ContainsKey(id) || (inOpened && !keptInOpened.Contains(id))) {
							kept[id] = node;
							if (inOpened) keptInOpened.Add(id);
						}
					}
				}
			}
			Keep(roots, false, 0);

			bool Shown(CSongListNode node) {
				if (!IsSong(node)) return false;
				string id = node.tGetUniqueId();
				return id == "" || !kept.TryGetValue(id, out var copy) || ReferenceEquals(copy, node);
			}

			var holds = new Dictionary<CSongListNode, bool>(System.Collections.Generic.ReferenceEqualityComparer.Instance);
			bool Holds(CSongListNode box) {
				if (box.nodeType != CSongListNode.ENodeType.BOX || box.childrenList == null || excluded(box)) return false;
				if (!holds.TryGetValue(box, out bool result)) holds[box] = result = box.childrenList.Any(child => Shown(child) || Holds(child));
				return result;
			}

			void Gather(List<CSongListNode> nodes, List<TreeEntry> into, int depth) {
				foreach (var node in nodes) {
					if (excluded(node)) continue;
					if (node.nodeType == CSongListNode.ENodeType.SCORE) {
						if (Shown(node)) into.Add(new TreeEntry { Node = node });
					} else if (node.nodeType == CSongListNode.ENodeType.BOX && node.childrenList != null) {
						if (Opens(node, depth) || (!node.childrenList.Any(Shown) && node.childrenList.Count(Holds) < 2)) {
							Gather(node.childrenList, into, depth + 1);
						} else {
							var folder = new TreeEntry { Node = node };
							Gather(node.childrenList, folder.Children, depth + 1);
							into.Add(folder);
						}
					}
				}
			}

			var entries = new List<TreeEntry>();
			Gather(roots, entries, 0);
			return entries;
		}

		private void MountTree(List<TreeEntry> entries, LuaSongNode parent, List<LuaSongNode> into) {
			foreach (var entry in entries) {
				var node = new LuaSongNode(entry.Node, parent, false, _settings);
				if (entry.Node.nodeType == CSongListNode.ENodeType.BOX) {
					var children = new List<LuaSongNode>();
					MountTree(entry.Children, node, children);
					_settings.AlterNodeListWithRequestedNodes(entry.Node, children, node);
					children.ForEach(node.AppendChildInternal);
				}
				into.Add(node);
			}
		}

		#endregion

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

		/// <summary>Returns the root <see cref="LuaSongNode"/> of the song tree.</summary>
	public LuaSongNode? GetRoot() => _root;

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

		public LuaSongNode? GetRandomNodeInFolder(LuaSongNode randomBoxLocation, bool recursive = true, LuaFunction? predicateFn = null) {
			Func<LuaSongNode, bool>? predicate = predicateFn.AsPredicate<LuaSongNode>();
			List<LuaSongNode> _randomPool = new List<LuaSongNode>();
			bool includeAllLocked = _settings.IgnoreUnlockables;

			predicate ??= nd => true;
			randomBoxLocation.Siblings.ForEach(node => {
				if (node.IsSong && (includeAllLocked || !node.IsLocked) && predicate(node)) _randomPool.Add(node);
				if (recursive == true && node.IsFolder) _randomPool.AddRange(FindAll(cnode => cnode.IsSong && (includeAllLocked || !cnode.IsLocked) && predicate(cnode), node));
			});

			if (_randomPool.Count == 0) return null;

			Random _random = new Random();
			int _randomIdx = _random.Next(_randomPool.Count);
			return _randomPool[_randomIdx];
		}

		#region [Temporary, give a method to attach it to a folder instead?]

		public List<LuaSongNode> SearchSongsByPredicate(LuaFunction predicate) {
			return this.FindAll(predicate.AsPredicate<LuaSongNode>()!, _root, true);
		}

		// Placeholder for testing the predicate
		public LuaSongNode? SearchFirstSongByPredicate(LuaFunction predicate) {
			return this.FindFirst(predicate.AsPredicate<LuaSongNode>()!, _root, true);
		}

		public List<LuaSongNode> SearchNodesByPredicate(LuaFunction predicate) {
			return this.FindAll(predicate.AsPredicate<LuaSongNode>()!, _root, false);
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

			if (!_currentNode.IsRoot && _currentNode.Parent!.IsFolder && _currentNode.Parent.Opened) {
				_currentNode = _currentNode.Parent;
				_currentNode.Opened = false;
				_currentPage = GetCurrentPage();

				// If the closed folder is not part of the real page (virtual folder scenario),
				// restore the cursor to where it was before OpenVirtualFolder was called.
				if (GetIndexInPage(_currentNode) < 0) {
					_currentNode = (_preVirtualFolderNode != null && GetIndexInPage(_preVirtualFolderNode) >= 0)
						? _preVirtualFolderNode
						: (_currentPage.Count > 0 ? _currentPage[0] : null);
					_preVirtualFolderNode = null;
				}

				return true;
			}
			return false;
		}

		/// <summary>
		/// Opens a virtual folder populated with a Lua table of song nodes.
		/// Back boxes (one at start, then one every <paramref name="backBoxFrequency"/> songs) and
		/// a random box at the end are created automatically with <paramref name="title"/> as the folder name.
		/// <paramref name="baseFolder"/> is set as the virtual folder's parent (one-sided — not added to its children).
		/// The cursor position before this call is saved and restored when <see cref="CloseFolder"/> exits the virtual folder.
		/// </summary>
		private static List<LuaSongNode> SongsFromTable(object songTableObj) {
			// song nodes from a Lua table (array, keys 1…n)
			var songs = new List<LuaSongNode>();
			if (songTableObj is LuaTable table) {
				int i = 1;
				while (true) {
					var val = table[i];
					if (val == null) break;
					if (val is LuaSongNode sn) songs.Add(sn);
					i++;
				}
			}
			return songs;
		}

		/// <summary>Builds a folder node holding <paramref name="songs"/> (re-wrapped under it) with a back box at the
		/// start, one every SubBackBoxFrequency songs, and a random box at the end.</summary>
		private LuaSongNode BuildVirtualFolder(LuaSongNode? parent, List<LuaSongNode> songs, string title, string? genre) {
			var cFolder = new CSongListNode { nodeType = CSongListNode.ENodeType.BOX };
			cFolder.ldTitle.SetString("default", title);
			if (!string.IsNullOrEmpty(genre)) cFolder.songGenre = genre;
			cFolder.childrenList = new List<CSongListNode>();
			var virtualFolder = new LuaSongNode(cFolder, parent, false, _settings) { IsVirtual = true };

			LuaSongNode MakeBack() {
				var cBack = new CSongListNode { nodeType = CSongListNode.ENodeType.BACKBOX };
				return new LuaSongNode(cBack, virtualFolder, false, _settings);
			}
			int backBoxFrequency = _settings.SubBackBoxFrequency;
			virtualFolder.AppendChildInternal(MakeBack());
			int songCount = 0;
			foreach (var song in songs) {
				virtualFolder.AppendChildInternal(new LuaSongNode(song.InternalNode!, virtualFolder, false, _settings));
				songCount++;
				if (backBoxFrequency > 0 && songCount % backBoxFrequency == 0)
					virtualFolder.AppendChildInternal(MakeBack());
			}
			var cRandom = new CSongListNode { nodeType = CSongListNode.ENodeType.RANDOM };
			virtualFolder.AppendChildInternal(new LuaSongNode(cRandom, virtualFolder, false, _settings));
			return virtualFolder;
		}

		/// <summary>
		/// Inserts a virtual box into <paramref name="baseFolder"/>'s children at <paramref name="index"/> (0 = first),
		/// holding the song nodes of <paramref name="songTableObj"/>; it opens and closes like any folder and
		/// stays until the list is rebuilt. <paramref name="genre"/> (optional) is the box's genre for the skin's
		/// per-genre art. The cursor is not moved. Returns the box, or null when the table holds no songs.
		/// </summary>
		public LuaSongNode? InsertVirtualFolder(LuaSongNode? baseFolder, int index, object songTableObj, string title, string? genre = null) {
			baseFolder ??= _root;
			if (baseFolder == null) return null;
			var songs = SongsFromTable(songTableObj);
			if (songs.Count == 0) return null;
			var box = BuildVirtualFolder(baseFolder, songs, title, genre);
			baseFolder.InsertChildInternal(index, box);
			if (_currentNode != null) _currentPage = GetCurrentPage();
			return box;
		}

		public bool OpenVirtualFolder(LuaSongNode? baseFolder, object songTableObj, string title) {
			var songs = SongsFromTable(songTableObj);
			if (songs.Count == 0) return false;

			// Create the virtual folder CSongListNode
			var cFolder = new CSongListNode { nodeType = CSongListNode.ENodeType.BOX };
			cFolder.ldTitle.SetString("default", title);
			cFolder.childrenList = new List<CSongListNode>();
			var virtualFolder = new LuaSongNode(cFolder, baseFolder, false, _settings) { IsVirtual = true };

			// Helper: fresh back box — its Title will be "Return ({title})" because _parent = virtualFolder
			LuaSongNode MakeBack() {
				var cBack = new CSongListNode { nodeType = CSongListNode.ENodeType.BACKBOX };
				return new LuaSongNode(cBack, virtualFolder, false, _settings);
			}

			int backBoxFrequency = _settings.SubBackBoxFrequency;

			// Back box at the start
			virtualFolder.AppendChildInternal(MakeBack());

			// Songs, with a back box inserted after every SubBackBoxFrequency songs
			int songCount = 0;
			foreach (var song in songs) {
				// Re-wrap so _parent = virtualFolder → Siblings returns the virtual page
				var rewrapped = new LuaSongNode(song.InternalNode!, virtualFolder, false, _settings);
				virtualFolder.AppendChildInternal(rewrapped);
				songCount++;
				if (backBoxFrequency > 0 && songCount % backBoxFrequency == 0)
					virtualFolder.AppendChildInternal(MakeBack());
			}

			// Random box at the end
			var cRandom = new CSongListNode { nodeType = CSongListNode.ENodeType.RANDOM };
			virtualFolder.AppendChildInternal(new LuaSongNode(cRandom, virtualFolder, false, _settings));

			// Save current cursor so CloseFolder() can restore it
			_preVirtualFolderNode = _currentNode;

			// Navigate into the virtual folder
			virtualFolder.Opened = true;
			_currentNode = virtualFolder.Child(0);
			_currentPage = _currentNode!.Siblings;   // = virtualFolder.Children directly
			return true;
		}

		#endregion
	}
}
