using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;

using FDK;

namespace TJAPlayer3
{
	internal class CAct演奏Drumsステータスパネル : CAct演奏ステータスパネル共通
	{
		// コンストラクタ
        public override void On活性化()
        {

            base.On活性化();
        }

		public override void OnManagedリソースの作成()
		{
            if( !base.b活性化してない )
            {

                base.OnManagedリソースの作成();
            }
		}
		public override void OnManagedリソースの解放()
		{

            base.OnManagedリソースの解放();
		}
		public override int On進行描画()
		{
            

            return 0;
		}


		// その他

		#region [ private ]
		//-----------------

		//-----------------
		#endregion
	}
}
