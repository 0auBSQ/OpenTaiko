using System.Drawing;
using FDK;
using static TJAPlayer3.CActSelect曲リスト;

namespace TJAPlayer3 {
	class CEncyclopediaControler {

		public CEncyclopediaControler() {
			_callStack = new Stack<DBEncyclopediaMenus.EncyclopediaMenu>();
			_idxStack = new Stack<int>();

			_current = TJAPlayer3.Databases.DBEncyclopediaMenus.data;

			_lang = CLangManager.fetchLang();

			tResetIndexes();
			tReloadFonts();
			tReallocateCurrentAccordingly();
		}

		#region [Fonts]

		private void tReloadFonts() {
			_pfEncyclopediaMenu?.Dispose();
			_pfEncyclopediaMenu = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.OpenEncyclopedia_Font_EncyclopediaMenu_Size);
		}

		#endregion

		#region [Menu files parsing]

		private string _GetPathTextFile(string parent) {
			string _expected = parent + Path.DirectorySeparatorChar + _lang + ".txt";
			string _default = parent + Path.DirectorySeparatorChar + CLangManager.DefaultLanguage.Item1 + ".txt";

			if (File.Exists(_expected))
				return _expected;
			return _default;
		}

		private string _GetSectionContents(int key, bool _fetchingMenus) {
			try {
				string _path = _GetPathTextFile(@$"{TJAPlayer3.strEXEのあるフォルダ}Encyclopedia{Path.DirectorySeparatorChar}" + (_fetchingMenus ? @$"Menus{Path.DirectorySeparatorChar}" : @$"Pages{Path.DirectorySeparatorChar}") + key.ToString());

				return File.ReadAllText(_path);
			} catch {
				return "[File not found]";
			}
		}

		private string _GetImagePath(int key) {
			return @$"{TJAPlayer3.strEXEのあるフォルダ}Encyclopedia{Path.DirectorySeparatorChar}Images{Path.DirectorySeparatorChar}" + key.ToString() + @".png";
		}

		#endregion

		#region [Memory management]

		private void tFreeRessources(bool pages) {
			if (pages) {
				if (Pages != null) {
					for (int i = 0; i < Pages.Length; i++) {
						Pages[i].Item3?.Dispose();
					}
				}
			}
		}

		private void tReallocateSubMenus() {
			tFreeRessources(false);

			int _count = _current.Menus.Length + 1;
			Submenus = new (int, CTexture)[_count];

			Submenus[0].Item1 = -1;
			Submenus[0].Item2 = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(
						  new TitleTextureKey(CLangManager.LangInstance.GetString("MENU_RETURN"), _pfEncyclopediaMenu, Color.White, Color.Brown, 1000));

			for (int i = 1; i < _count; i++) {
				int _idx = i - 1; // Excluding return
				var _menu = _current.Menus[_idx];
				Submenus[i].Item1 = _menu.Key;
				Submenus[i].Item2 = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(
						  new TitleTextureKey(_GetSectionContents(_menu.Key, true), _pfEncyclopediaMenu, Color.White, Color.Brown, 1000));
			}
		}

		private void tReallocatePages() {
			tFreeRessources(true);

			if (_current.Pages == null) {
				Pages = new (int, CTexture, CTexture)[0];
				return; // Menus and Pages are null
			}

			int _count = _current.Pages.Length;
			Pages = new (int, CTexture, CTexture)[_count];

			for (int i = 0; i < _count; i++) {
				var _page = _current.Pages[i];
				Pages[i].Item1 = _page;
				Pages[i].Item2 = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(
						  new TitleTextureKey(_GetSectionContents(_page, false), _pfEncyclopediaMenu, Color.White, Color.Brown, 1000));
				Pages[i].Item3 = TJAPlayer3.tテクスチャの生成(_GetImagePath(_page));
			}
		}

		#endregion

		#region [Input handlers]

		// Bool return value = "Back to main menu ?"
		public bool tHandleBack() {
			if (_callStack.Count() <= 0)
				return true;

			var _oldIndex = _idxStack.Pop();

			if (tArePagesOpened())
				PageIndex = 0;
			else
				MenuIndex = _oldIndex;

			_current = _callStack.Pop();

			tReallocateCurrentAccordingly();

			return false;
		}

		// Bool return value = ("Went forward ?", "Back to main menu ?"
		public (bool, bool) tHandleEnter() {
			// If not page and not return button
			if (!tArePagesOpened() && MenuIndex != 0) {
				_callStack.Push(_current);
				_idxStack.Push(MenuIndex);
				_current = _current.Menus[MenuIndex - 1].Value;

				bool _hasPages = tArePagesOpened();

				if (_hasPages)
					PageIndex = 0;
				else
					tResetIndexes();

				tReallocateCurrentAccordingly();

				if (_hasPages)
					tUpdatePageIndex();

				return (true, false);
			}
			var _mainMenu = tHandleBack();
			return (false, _mainMenu);
		}

		public void tHandleLeft() {
			tMove(tArePagesOpened(), -1);
		}

		public void tHandleRight() {
			tMove(tArePagesOpened(), 1);
		}


		#endregion

		#region [private utils methods]

		private void tUpdatePageIndex() {
			PageText = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(
						  new TitleTextureKey((PageIndex + 1).ToString() + "/" + Pages.Length.ToString(), _pfEncyclopediaMenu, Color.White, Color.Brown, 1000));
		}

		private void tMove(bool pages, int count) {
			if (pages) {
				if (Pages.Length > 0) {
					PageIndex = (PageIndex + count + Pages.Length) % Pages.Length;
					tUpdatePageIndex();
				}

			} else {
				if (Submenus.Length > 0)
					MenuIndex = (MenuIndex + count + Submenus.Length) % Submenus.Length;
			}
		}

		private void tResetIndexes() {
			MenuIndex = 0;
			PageIndex = 0;
		}

		private void tReallocateCurrentAccordingly() {
			if (tIsMenu(_current))
				tReallocateSubMenus();
			else
				tReallocatePages();
		}

		#endregion

		#region [public utils methods]

		public bool tArePagesOpened() {
			return (!tIsMenu(_current));
		}

		public bool tIsMenu(DBEncyclopediaMenus.EncyclopediaMenu menu) {
			return (!(menu.Menus == null || menu.Menus.Length == 0));
		}

		#endregion

		#region [public variables]

		public (int, CTexture)[] Submenus;
		public (int, CTexture, CTexture)[] Pages;
		public CTexture PageText;
		public int MenuIndex;
		public int PageIndex;

		#endregion

		#region [private variables]

		private Stack<DBEncyclopediaMenus.EncyclopediaMenu> _callStack;
		private Stack<int> _idxStack;
		private DBEncyclopediaMenus.EncyclopediaMenu _current;
		private string _lang;
		private static CCachedFontRenderer _pfEncyclopediaMenu;

		#endregion
	}
}
