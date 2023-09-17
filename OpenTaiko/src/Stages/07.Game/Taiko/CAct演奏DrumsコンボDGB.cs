using System;
using System.Collections.Generic;
using System.Text;

namespace TJAPlayer3
{
	internal class CAct演奏DrumsコンボDGB : CAct演奏Combo共通
	{
		// CAct演奏Combo共通 実装

		protected override void tコンボ表示_ギター( int nCombo値, int nジャンプインデックス )
		{
		}
		protected override void tコンボ表示_ドラム( int nCombo値, int nジャンプインデックス )
        {
		}
		protected override void tコンボ表示_ベース( int nCombo値, int nジャンプインデックス )
		{
		}
		protected override void tコンボ表示_太鼓(int nCombo値, int nジャンプインデックス, int nPlayer)
        {
 	        base.tコンボ表示_太鼓( nCombo値, nジャンプインデックス, nPlayer );
        }
	}
}
