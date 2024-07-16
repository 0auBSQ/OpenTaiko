using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class CLuaConfigStageInfo
    {
        public int nCursorIndex => TJAPlayer3.stageコンフィグ.n現在のメニュー番号;
        public int nItembarIndex => TJAPlayer3.stageコンフィグ.actList.n現在の選択項目;
        public bool bWaitingKeyInput => TJAPlayer3.stageコンフィグ.actKeyAssign.bキー入力待ち;
        public List<CItemBase> listItemList => TJAPlayer3.stageコンフィグ.actList.list項目リスト;
    }
}
