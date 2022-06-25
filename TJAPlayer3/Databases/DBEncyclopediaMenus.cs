using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    class DBEncyclopediaMenus : CSavableT<DBEncyclopediaMenus.EncyclopediaMenu>
    {
        public DBEncyclopediaMenus()
        {
            _fn = @".\Encyclopedia\Menus.json";
            base.tDBInitSavable();
        }

        #region [Auxiliary classes]
        public class EncyclopediaMenu
        {
            [JsonProperty("menus")]
            public KeyValuePair<int, EncyclopediaMenu>[] Menus;

            [JsonProperty("pages")]
            public int[] Pages;
        }

        #endregion
    }
}
