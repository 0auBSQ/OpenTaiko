using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.IO;
using FDK;
using static TJAPlayer3.CActSelect曲リスト;

namespace TJAPlayer3
{
    class CEncyclopediaControler
    {
        
        public CEncyclopediaControler()
        {
            _callStack = new Stack<DBEncyclopediaMenus.EncyclopediaMenu>();

            _callStack.Push(TJAPlayer3.Databases.DBEncyclopediaMenus.data);
            _current = _callStack.Peek();

            MenuIndex = 0;
            _lang = CLangManager.fetchLang();
            tReloadFonts();
            tReallocateSubMenus();
        }

        private void tReloadFonts()
        {
            _pfEncyclopediaMenu?.Dispose();
            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
            {
                _pfEncyclopediaMenu = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 14);
            }
            else
            {
                _pfEncyclopediaMenu = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 14);
            }
        }

        private string _GetPathTextFile(string parent)
        {
            string _expected = parent + @"\" + _lang + ".txt";
            string _default = parent + @"\" + CLangManager.DefaultLanguage.Item1 + ".txt";

            if (File.Exists(_expected))
                return _expected;
            return _default;
        }

        private string _GetSectionContents(ref KeyValuePair<int, DBEncyclopediaMenus.EncyclopediaMenu> menu)
        {
            try
            {
                string _path = _GetPathTextFile(@".\Encyclopedia\" + (tIsMenu(menu.Value) ? @"Menus\" : @"Pages\") + menu.Key.ToString());

                return File.ReadAllText(_path);
            }
            catch
            {
                return "[File fetching failed]";
            }
        }
        private void tReallocateSubMenus()
        {
            if (Submenus != null)
            {
                for (int i = 0; i < Submenus.Length; i++)
                {
                    Submenus[i].Item2?.Dispose();
                }
            }

            int _count = _current.Menus.Length + 1;
            Submenus = new (int, CTexture)[_count];

            Submenus[0].Item1 = -1;
            Submenus[0].Item2 = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(
                          new TitleTextureKey(CLangManager.LangInstance.GetString(401), _pfEncyclopediaMenu, Color.White, Color.DarkOrange, 1000));

            for (int i = 1; i < _count; i++)
            {
                int _idx = i - 1; // Excluding return
                var _menu = _current.Menus[_idx];
                Submenus[i].Item1 = _menu.Key;
                Submenus[i].Item2 = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(
                          new TitleTextureKey(_GetSectionContents(ref _menu), _pfEncyclopediaMenu, Color.White, Color.DarkOrange, 1000));
            }
        }

        // Verify if submenu has pages or not
        public bool tIsMenu(DBEncyclopediaMenus.EncyclopediaMenu menu)
        {
            return (menu.Menus == null || menu.Menus.Length == 0);
        }

        #region [public]

        public (int, CTexture)[] Submenus;
        public int MenuIndex;

        #endregion

        #region [private]

        private Stack<DBEncyclopediaMenus.EncyclopediaMenu> _callStack;
        private DBEncyclopediaMenus.EncyclopediaMenu _current;
        private string _lang;
        private static CPrivateFastFont _pfEncyclopediaMenu;

        #endregion
    }
}
