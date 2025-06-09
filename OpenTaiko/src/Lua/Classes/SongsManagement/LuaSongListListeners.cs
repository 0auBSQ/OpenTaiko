namespace OpenTaiko {
	internal class LuaSongListListeners {
		private static List<LuaSongList> _luaSongLists = new List<LuaSongList>();


		public static void RegisterSongList(LuaSongList list) {
			_luaSongLists.Add(list);
		}

		// To call when reloading songs
		public static void ResetSongLists() {
			_luaSongLists.ForEach((sl) => {
				sl.ReloadSongList();
			});
		}
	}
}
