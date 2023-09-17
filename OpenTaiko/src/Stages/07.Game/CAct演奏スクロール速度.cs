using System;
using System.Collections.Generic;
using System.Text;
using FDK;

namespace TJAPlayer3
{
	internal class CAct演奏スクロール速度 : CActivity
	{
		// プロパティ

		public double[] db現在の譜面スクロール速度 = new double[5];


		// コンストラクタ

		public CAct演奏スクロール速度()
		{
			base.IsDeActivated = true;
		}


		// CActivity 実装

		public override void Activate()
		{
			for( int i = 0; i < 5; i++ )
			{
				this.db現在の譜面スクロール速度[ i ] = (double) TJAPlayer3.ConfigIni.nScrollSpeed[ TJAPlayer3.GetActualPlayer(i) ];
				this.n速度変更制御タイマ[ i ] = -1;
			}

		

			base.Activate();
		}
		public override unsafe int Draw()
		{
			if( !base.IsDeActivated )
			{
				if( base.IsFirstDraw )
				{
					//this.n速度変更制御タイマ.Drums = this.n速度変更制御タイマ.Guitar = this.n速度変更制御タイマ.Bass = (long)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
					for (int i = 0; i < 5; i++)
                    {
						this.n速度変更制御タイマ[i] = (long)(SoundManager.PlayTimer.NowTime * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));

					}
					
					base.IsFirstDraw = false;
				}
				long n現在時刻 = SoundManager.PlayTimer.NowTime;
				for( int i = 0; i < 5; i++ )
				{
					double db譜面スクロールスピード = (double) TJAPlayer3.ConfigIni.nScrollSpeed[ TJAPlayer3.GetActualPlayer(i) ];
					if( n現在時刻 < this.n速度変更制御タイマ[ i ] )
					{
						this.n速度変更制御タイマ[ i ] = n現在時刻;
					}
					while( ( n現在時刻 - this.n速度変更制御タイマ[ i ] ) >= 2 )								// 2msに1回ループ
					{
						if( this.db現在の譜面スクロール速度[ i ] < db譜面スクロールスピード )				// Config.iniのスクロール速度を変えると、それに追いつくように実画面のスクロール速度を変える
						{
							this.db現在の譜面スクロール速度[ i ] += 0.012;

							if( this.db現在の譜面スクロール速度[ i ] > db譜面スクロールスピード )
							{
								this.db現在の譜面スクロール速度[ i ] = db譜面スクロールスピード;
							}
						}
						else if ( this.db現在の譜面スクロール速度[ i ] > db譜面スクロールスピード )
						{
							this.db現在の譜面スクロール速度[ i ] -= 0.012;

							if( this.db現在の譜面スクロール速度[ i ] < db譜面スクロールスピード )
							{
								this.db現在の譜面スクロール速度[ i ] = db譜面スクロールスピード;
							}
						}
						this.n速度変更制御タイマ[ i ] += 2;
					}
				}
			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		private long[] n速度変更制御タイマ = new long[5];
		//-----------------
		#endregion
	}
}
