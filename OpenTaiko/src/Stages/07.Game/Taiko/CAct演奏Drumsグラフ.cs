using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.IO;
using FDK;

namespace TJAPlayer3
{
	internal class CAct演奏Drumsグラフ : CActivity
	{
	
		// コンストラクタ

		public CAct演奏Drumsグラフ()
		{
			base.IsDeActivated = true;
		}


		// CActivity 実装

		public override void Activate()
        {
			base.Activate();
		}
		public override void DeActivate()
		{
			base.DeActivate();
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
			if( !base.IsDeActivated )
			{
				if( base.IsFirstDraw )
				{
                }
                
			}
			return 0;
		}


		// その他

		#region [ private ]
		//----------------
		//-----------------
		#endregion
	}
}
