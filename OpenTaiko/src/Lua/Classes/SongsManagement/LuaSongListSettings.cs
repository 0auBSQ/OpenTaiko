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

		// Generate the root node from a specific folder (given by Genre) instead of the list song root note (fe. Dan song select, Tower song select)
		public string? RootGenreFolder = null;

		// If the chart doesn't have all the mandatory specified difficulties, the node is not included
		public Difficulty[]? MandatoryDifficultyList = null;

		// Hide folder where there is nothing to show (No song, no locked song having a non-hidden hidden index)
		public bool HideEmptyFolders = true;

		// If true, display openned folders as flatten and get all leaves instead of just the child folder
		public bool FlattenOpennedFolders = true;

		// If true, requested pages will cycle on overflow, else overflowing element will be set as null
		public bool ModuloPagination = true;

		// If true, when moving within the song folder the cursor will loop through the page in case of overflow, else the cursor will focus the last element on both sides
		public bool ModuloMovement = true;


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

			// Exclude any song node that does not have all required difficulties
			if (node.nodeType == CSongListNode.ENodeType.SCORE) {
				if (this.MandatoryDifficultyList != null) {
					foreach (Difficulty diff in this.MandatoryDifficultyList) {
						if ((int)diff < 0 || diff >= Difficulty.Total) continue;
						if (node.nLevel[(int)diff] < 0) return true;
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

			// Hide locked hidden songs
			if (node.IsSong) {
				if (node.HiddenIndex == DBSongUnlockables.EHiddenIndex.HIDDEN) return true;
			}

			return false;
		}

		public static LuaSongListSettings Generate() {
			return new LuaSongListSettings();
		}

		public LuaSongListSettings() {

		}
	}
}
