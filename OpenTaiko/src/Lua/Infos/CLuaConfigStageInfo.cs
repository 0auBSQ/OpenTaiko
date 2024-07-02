using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class CLuaConfigStageInfo
    {
        public int nCursorIndex { get; set; }
        public int nItembarIndex { get; set; }
        public bool bWaitingKeyInput { get; set; }
        public bool bEnumeratingSongs { get; set; }
        public List<CItemBase> listItemList
        {
            get
            {
                return TJAPlayer3.stageコンフィグ.actList.list項目リスト;
            }
        }
    }
}
