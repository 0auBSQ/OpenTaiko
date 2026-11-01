using System.Collections.Generic;
using System.Linq;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// Tree lists (LuaSongListSettings.TreeSongPredicate): every song the predicate takes wherever it is, a folder only when
	// it holds one itself or several folders holding some, the folders the folder predicate takes giving way, each song
	// once. The dan and tower lists take songs with a Dan / Tower chart, as node:GetChart reads it (the course's level).
	public class SongTreeTests {
		private static CSongListNode Box(string title, string genre, params CSongListNode[] children) {
			var box = new CSongListNode { nodeType = CSongListNode.ENodeType.BOX, songGenre = genre, childrenList = children.ToList() };
			box.ldTitle.SetString("default", title);
			foreach (var child in children) child.rParentNode = box;
			return box;
		}

		private static CSongListNode Song(string title, Difficulty course, string? id = null) {
			var song = new CSongListNode { nodeType = CSongListNode.ENodeType.SCORE };
			song.ldTitle.SetString("default", title);
			song.nLevel = Enumerable.Repeat(-1, (int)Difficulty.Total).ToArray();   // a .tja marks its missing courses -1
			song.nLevel[(int)course] = 5;
			song.score[(int)course] = new CScore();
			song.uniqueId = new CSongUniqueID();
			song.uniqueId.data.id = id ?? title;
			return song;
		}

		// a .tcm dan course as the old loader built it (and songlist.db may still hold it): its Dan score and level, its
		// other levels left at 0
		private static CSongListNode TcmDan(string title) {
			var song = new CSongListNode { nodeType = CSongListNode.ENodeType.SCORE };
			song.ldTitle.SetString("default", title);
			song.nLevel[(int)Difficulty.Dan] = 10;
			song.score[(int)Difficulty.Dan] = new CScore();
			song.uniqueId = new CSongUniqueID();
			song.uniqueId.data.id = title;
			return song;
		}

		private static void ClearLevels(List<CSongListNode> nodes) {
			foreach (var node in nodes) {
				CSongManager.tClearLevelsWithoutChart(node);
				if (node.childrenList != null) ClearLevels(node.childrenList);
			}
		}

		// the list a skin builds for a course: the songs with that chart, the top-level folders of the mode's genre opened
		private static List<LuaSongList.TreeEntry> Gather(Difficulty course, string genre, params CSongListNode[] roots) {
			var list = roots.ToList();
			ClearLevels(list);   // as the song list postprocessing does
			var settings = new LuaSongListSettings { ExcludedGenreFolders = ["Favorite"] };
			return LuaSongList.GatherTree(list, node => node.nLevel[(int)course] != -1,
				(folder, depth) => depth == 0 && folder.songGenre == genre, settings.IsNodeExcludedAtGeneration);
		}

		// "Folder A [Song A, Song B]" for the entries, in order
		private static string Describe(List<LuaSongList.TreeEntry> entries)
			=> string.Join(", ", entries.Select(e => e.Node.ldTitle.GetString("")
				+ (e.Node.nodeType == CSongListNode.ENodeType.BOX ? " [" + Describe(e.Children) + "]" : "")));

		[Fact]
		public void FolderShowsWhenItHoldsASong_OthersGiveWay() {
			var entries = Gather(Difficulty.Dan, "段位道場",
				Box("Folder A", "", Song("Dan A", Difficulty.Dan), Box("Folder B", "", Song("Regular Chart A", Difficulty.Oni))),
				Box("Folder C", "", Box("Folder D", "", Song("Dan B", Difficulty.Dan))),
				Song("Dan C", Difficulty.Dan));
			Assert.Equal("Folder A [Dan A], Folder D [Dan B], Dan C", Describe(entries));
		}

		[Fact]
		public void OpenedFolderGivesWay_EvenWithLooseSongs() {
			var entries = Gather(Difficulty.Dan, "段位道場",
				Box("Dan-i Dojo", "段位道場",
					Box("2023 Dans", "段位道場", Song("Dan 1", Difficulty.Dan)),
					Song("Example Dan", Difficulty.Dan)),
				Box("Custom", "", Box("My Dojo", "段位道場", Song("Dan 2", Difficulty.Dan))));
			Assert.Equal("2023 Dans [Dan 1], Example Dan, My Dojo [Dan 2]", Describe(entries));
		}

		// the folder predicate opens every folder it takes, at any depth, whatever it holds; the others keep the folder rule
		[Fact]
		public void FolderPredicateOpensEveryFolderItTakes() {
			var roots = new List<CSongListNode> {
				Box("Dan-i Dojo", "段位道場", Box("Pack 1", "", Song("Dan 1", Difficulty.Dan)), Song("Loose Dan", Difficulty.Dan)),
				Box("My Dojo", "Custom Dojo", Box("Pack 2", "", Song("Dan 2", Difficulty.Dan)), Box("Pack 3", "", Song("Dan 3", Difficulty.Dan))),
				Box("Custom", "", Box("Open Me", "", Song("Dan 4", Difficulty.Dan), Box("Pack 4", "", Song("Dan 5", Difficulty.Dan)))),
				Box("Kept", "", Song("Dan 6", Difficulty.Dan), Box("Pack 5", "", Song("Dan 7", Difficulty.Dan))),
			};
			var opened = new List<string>();
			var entries = LuaSongList.GatherTree(roots, node => node.nLevel[(int)Difficulty.Dan] != -1, (folder, depth) => {
				bool open = (depth == 0 && (folder.songGenre == "段位道場" || folder.songGenre == "Custom Dojo"))
					|| folder.ldTitle.GetString("") == "Open Me";
				if (open) opened.Add(folder.ldTitle.GetString("") + "@" + depth);
				return open;
			}, node => false);
			Assert.Equal("Pack 1 [Dan 1], Loose Dan, Pack 2 [Dan 2], Pack 3 [Dan 3], Dan 4, Pack 4 [Dan 5], Kept [Dan 6, Pack 5 [Dan 7]]",
				Describe(entries));
			Assert.Equal(new[] { "Dan-i Dojo@0", "My Dojo@0", "Open Me@1" }, opened);   // each asked once, with its real depth
		}

		[Fact]
		public void FolderWithSeveralSongFoldersStays() {
			var entries = Gather(Difficulty.Dan, "段位道場",
				Box("Dan-i Dojo", "段位道場",
					Box("2023 Dans", "段位道場", Song("Dan 1", Difficulty.Dan)),
					Box("2023 Gaidens", "段位道場",
						Box("GA. Gaiden", "段位道場", Song("Gaiden A", Difficulty.Dan)),
						Box("GB. Gaiden", "段位道場", Song("Gaiden B", Difficulty.Dan)))),
				Box("Custom", "",
					Box("Pack 1", "", Song("Dan 2", Difficulty.Dan)),
					Box("Pack 2", "", Box("Only Child", "", Song("Dan 3", Difficulty.Dan))),
					Box("Regular Songs", "", Song("Regular", Difficulty.Oni))));
			Assert.Equal("2023 Dans [Dan 1], 2023 Gaidens [GA. Gaiden [Gaiden A], GB. Gaiden [Gaiden B]], "
				+ "Custom [Pack 1 [Dan 2], Only Child [Dan 3]]", Describe(entries));
		}

		[Fact]
		public void NoDepthCap() {
			var entries = Gather(Difficulty.Dan, "段位道場",
				Box("Folder E", "", Song("Dan E", Difficulty.Dan),
					Box("Folder F", "", Box("Folder G", "", Song("Dan G", Difficulty.Dan),
						Box("Folder H", "", Song("Dan H", Difficulty.Dan))))));
			Assert.Equal("Folder E [Dan E, Folder G [Dan G, Folder H [Dan H]]]", Describe(entries));
		}

		[Fact]
		public void EachSongOnce_TheOpenedFolderCopyFirst() {
			var entries = Gather(Difficulty.Dan, "段位道場",
				Box("Custom Copies", "", Song("Copied Dan", Difficulty.Dan, "same")),
				Box("Dan-i Dojo", "段位道場", Box("Pack", "段位道場", Song("Shipped Dan", Difficulty.Dan, "same"))),
				Box("Later", "", Song("Later Copy", Difficulty.Dan, "same")));
			Assert.Equal("Pack [Shipped Dan]", Describe(entries));
		}

		[Fact]
		public void ExcludedGenreFoldersAreSkipped() {
			var entries = Gather(Difficulty.Dan, "段位道場",
				Box("Favorite", "Favorite", Song("Favorite Dan", Difficulty.Dan, "fav")),
				Box("Folder", "", Song("Dan", Difficulty.Dan)));
			Assert.Equal("Folder [Dan]", Describe(entries));
		}

		[Fact]
		public void TowersTakeTowerSongsOnly() {
			var entries = Gather(Difficulty.Tower, "太鼓タワー",
				Box("Taiko Towers", "太鼓タワー",
					Box("Sweet", "太鼓タワー", Song("Tower 1", Difficulty.Tower)),
					Box("Spicy", "太鼓タワー", Song("Tower 2", Difficulty.Tower))),
				Box("Pack", "", Song("Tower 3", Difficulty.Tower), Song("Dan", Difficulty.Dan), TcmDan("Course Dan")),
				Box("Dan-i Dojo", "段位道場", TcmDan("Shipped Course Dan")));
			Assert.Equal("Sweet [Tower 1], Spicy [Tower 2], Pack [Tower 3]", Describe(entries));
		}

		[Fact]
		public void TcmDansAreDans() {
			var entries = Gather(Difficulty.Dan, "段位道場",
				Box("Dan-i Dojo", "段位道場", Box("2024 Dans", "段位道場", TcmDan("Course Dan"), Song("Chart Dan", Difficulty.Dan))));
			Assert.Equal("2024 Dans [Course Dan, Chart Dan]", Describe(entries));
		}

		[Fact]
		public void CoursesWithoutAChartHaveNoLevel() {
			var dan = TcmDan("Course Dan");
			CSongManager.tClearLevelsWithoutChart(dan);
			Assert.Equal(new[] { -1, -1, -1, -1, -1, -1, 10 }, dan.nLevel);

			var box = Box("Folder", "");
			var levels = box.nLevel.ToArray();
			CSongManager.tClearLevelsWithoutChart(box);   // folders keep theirs
			Assert.Equal(levels, box.nLevel);
		}

		// the predicates as a skin sets them from Lua, called the way the list calls them
		[Fact]
		public void PredicatesFromLua() {
			var settings = new LuaSongListSettings();
			using var lua = new NLua.Lua();
			lua.State.Encoding = System.Text.Encoding.UTF8;   // as CLuaScript sets it
			lua["settings"] = settings;
			lua.DoString(@"
				settings:SetTreeSongPredicate(function(node) return node.Title == nil end)
				settings:SetTreeFolderPredicate(function(folder, depth) return depth == 0 and folder.Title == nil end)");
			var song = settings.TreeSongPredicate.AsPredicate<LuaSongNode>()!;
			var folder = settings.TreeFolderPredicate.AsPredicate<LuaSongNode, int>()!;
			Assert.True(song(new LuaSongNode()));
			Assert.True(folder(new LuaSongNode(), 0));
			Assert.False(folder(new LuaSongNode(), 1));
		}
	}
}
