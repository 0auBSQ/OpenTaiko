using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TJAPlayer3
{
    class DBEncyclopediaMenus : CSavableT<Dictionary<int, string>>
    {
        public DBEncyclopediaMenus()
        {
            _fn = @".\Encyclopedia\Menus.json";
            base.tDBInitSavable();
        }


    }
}
