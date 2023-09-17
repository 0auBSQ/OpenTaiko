using System;
using FDK;

namespace TJAPlayer3
{
	internal class CActScanningLoudness :  CActivity
	{
	    public bool bIsActivelyScanning;

		// CActivity 実装

		public override void Activate()
		{
			if ( this.IsActivated )
				return;
			base.Activate();

			try
			{
				this.ctNowScanningLoudness = new CCounter();
				this.ctNowScanningLoudness.Start( 0, 200, 29, TJAPlayer3.Timer );
			}
			finally
			{
			}
		}

		public override void DeActivate()
		{
			if ( this.IsDeActivated )
				return;
			base.DeActivate();
			this.ctNowScanningLoudness = null;
		}

		public override int Draw()
		{
			if ( this.IsDeActivated )
			{
				return 0;
			}
			this.ctNowScanningLoudness.TickLoop();
			if ( bIsActivelyScanning && TJAPlayer3.Tx.Scanning_Loudness != null )
			{
                TJAPlayer3.Tx.Scanning_Loudness.Opacity = (int) ( 176.0 + 80.0 * Math.Sin( (double) (2 * Math.PI * this.ctNowScanningLoudness.CurrentValue / 100.0 ) ) );
                TJAPlayer3.Tx.Scanning_Loudness.t2D描画( 18 + 89 + 18, 7 ); // 2018-09-03 twopointzero: display right of Enum_Song, using its width and margin
			}

			return 0;
		}

		private CCounter ctNowScanningLoudness;
	}
}
