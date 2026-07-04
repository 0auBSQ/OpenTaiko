using NLua;

namespace OpenTaiko {
	internal class LuaSongListSettings {
		// Settings class used to customize which songs to include in the given song lists

		// Add a global random box within the root list
		public bool AppendMainRandomBox = true;

		// Add a Random box at the end of each folders 
		public bool AppendSubRandomBoxes = true;

		// Indicates the frequency of back boxes within folders, fe. 7 means one back box for 7 songs, if 0 no back box is generated
		public int SubBackBoxFrequency = 7;

		// Genre folders that are not displayed in the song select menu
		public string[] ExcludedGenreFolders = [];

		public void SetExcludedGenreFolders(object luaTable) {
			if (luaTable is LuaTable table) {
				ExcludedGenreFolders = table.Values.Cast<string>().ToArray();
			}
		}

		// Generate the root node from a specific folder (given by Genre) instead of the list song root note (fe. Dan song select, Tower song select)
		public string? RootGenreFolder = null;

		// The node version of RootGenreFolder, to use if there is a risk of having multiple folders with the same genre as a string, takes precedence over RootGenreFolder
		public LuaSongNode? RootGenreFolderNode = null;

		// Difficulty requirement for a SCORE node to be included. null = no requirement. How the list is
		// matched is controlled by MandatoryDifficultyMatchAll below.
		public Difficulty[]? MandatoryDifficultyList = null;

		// How MandatoryDifficultyList is matched:
		//   true  (default) = AND: the node must have ALL listed difficulties.
		//   false           = OR:  the node must have AT LEAST ONE listed difficulty (e.g. list Easy..Edit to
		//                          drop Tower/Dan-only charts, which have no standard playable difficulty, from
		//                          the regular song list).
		public bool MandatoryDifficultyMatchAll = true;

		// Hide folder where there is nothing to show (No song, no locked song having a non-hidden hidden index)
		public bool HideEmptyFolders = true;

		// If true, display openned folders as flatten and get all leaves instead of just the child folder
		public bool FlattenOpenedFolders = true;

		// If true, requested pages will cycle on overflow, else overflowing element will be set as null
		public bool ModuloPagination = true;

		// If true, when moving within the song folder the cursor will loop through the page in case of overflow, else the cursor will focus the last element on both sides
		public bool ModuloMovement = true;

		// If true, songs with HiddenIndex == HIDDEN are excluded from the list (default matches legacy behaviour)
		public bool ExcludeHiddenSongs = true;

		// If true, locked songs are excluded from the list entirely (navigation will never land on them)
		public bool ExcludeLockedSongs = false;

		// If true, the unlock system is ignored for this list: locked songs are treated as unlocked
		// (overrides ExcludeLockedSongs and suppresses the IsLocked filter in GetRandomNodeInFolder)
		public bool IgnoreUnlockables = false;


		public void AlterRootNodeWithRequestedNodes(LuaSongNodeRoot root) {
			if (this.AppendMainRandomBox == true) {
				CSongListNode _rd = CSongDict.tGenerateRandomButton(null, null);
				LuaSongNode _child = new LuaSongNode(_rd, root, false);
				root.AppendChild(_child);
			}
		}

		public void AlterNodeListWithRequestedNodes(CSongListNode? node, List<LuaSongNode> snlist, LuaSongNode parent) {
			if (node == null) return;

			if (this.SubBackBoxFrequency > 0) {
				CSongListNode _rb = CSongDict.tGenerateBackButton(node, null);
				for (int i = 0; i <= snlist.Count; i += this.SubBackBoxFrequency + 1) {
					LuaSongNode _child = new LuaSongNode(_rb, parent, false);
					snlist.Insert(i, _child);
				}
			}

			if (this.AppendSubRandomBoxes == true) {
				CSongListNode _rd = CSongDict.tGenerateRandomButton(node, null);
				LuaSongNode _child = new LuaSongNode(_rd, parent, false);
				snlist.Add(_child);
			}
		}

		public bool IsNodeExcludedAtGeneration(CSongListNode node) {
			// Exclude any pre-generated backbox and random box
			if (node.nodeType == CSongListNode.ENodeType.BACKBOX || node.nodeType == CSongListNode.ENodeType.RANDOM) return true;

			// Exclude folders depending on the song list settings
			if (node.nodeType == CSongListNode.ENodeType.BOX) {
				if (this.ExcludedGenreFolders.Contains(node.songGenre)) return true;
			}

			// Exclude any song node that fails the mandatory-difficulty requirement:
			//   AND → missing any listed difficulty; OR → missing all listed difficulties.
			if (node.nodeType == CSongListNode.ENodeType.SCORE) {
				if (this.MandatoryDifficultyList != null && this.MandatoryDifficultyList.Length > 0) {
					if (this.MandatoryDifficultyMatchAll) {
						foreach (Difficulty diff in this.MandatoryDifficultyList) {
							if ((int)diff < 0 || diff >= Difficulty.Total) continue;
							if (node.nLevel[(int)diff] < 0) return true;   // missing a required difficulty
						}
					} else {
						bool hasAny = false;
						foreach (Difficulty diff in this.MandatoryDifficultyList) {
							if ((int)diff < 0 || diff >= Difficulty.Total) continue;
							if (node.nLevel[(int)diff] >= 0) { hasAny = true; break; }
						}
						if (!hasAny) return true;   // has none of the acceptable difficulties
					}
				}
			}

			return false;
		}

		public bool IsNodeExcludedAtExecution(LuaSongNode node) {
			// Hide folders that have no visible song
			if (node.IsFolder) {
				if (this.HideEmptyFolders == true) {
					if (node.RecursiveVisibleSongCount == 0) return true;
				}
			}

			if (node.IsSong) {
				// Hide songs that are fully hidden in the unlock system
				if (this.ExcludeHiddenSongs && node.HiddenIndex == (int)DBSongUnlockables.EHiddenIndex.HIDDEN) return true;

				// Exclude locked songs when requested, unless the unlock system is ignored for this list
				if (this.ExcludeLockedSongs && !this.IgnoreUnlockables && node.IsLocked) return true;
			}

			return false;
		}

		public static LuaSongListSettings Generate() {
			return new LuaSongListSettings();
		}

		public void SetMandatoryDifficultyList(object luaTable) {
			if (luaTable is LuaTable table) {
				MandatoryDifficultyList = table.Values.Cast<long>()
					.Select(v => (Difficulty)(int)v)
					.ToArray();
			}
		}

		public LuaSongListSettings() {

		}
	}
}
