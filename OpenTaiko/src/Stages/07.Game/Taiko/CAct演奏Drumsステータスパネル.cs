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
        public override void Activate()
        {

            base.Activate();
        }

		public override void CreateManagedResource()
		{
            base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
            base.ReleaseManagedResource();
		}
		public override int Draw()
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
