using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class CLuaSkinInfo
    {
        public int width => TJAPlayer3.Skin.Resolution[0];
        public int height => TJAPlayer3.Skin.Resolution[1];
    }
}
