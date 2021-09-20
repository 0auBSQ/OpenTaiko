using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using DirectShowLib;
using SlimDX;
using FDK;

namespace TJAPlayer3
{
	internal class CStage終了 : CStage
	{
		// コンストラクタ

		public CStage終了()
		{
			base.eステージID = CStage.Eステージ.終了;
			base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
			base.b活性化してない = true;
		}


		// CStage 実装

		public override void On活性化()
		{
			Trace.TraceInformation( "終了ステージを活性化します。" );
			Trace.Indent();
			try
			{
				this.ct時間稼ぎ = new CCounter();
				base.On活性化();
			}
			finally
			{
				Trace.TraceInformation( "終了ステージの活性化を完了しました。" );
				Trace.Unindent();
			}
		}
		public override void On非活性化()
		{
			Trace.TraceInformation( "終了ステージを非活性化します。" );
			Trace.Indent();
			try
			{
				base.On非活性化();
			}
			finally
			{
				Trace.TraceInformation( "終了ステージの非活性化を完了しました。" );
				Trace.Unindent();
			}
		}
		public override void OnManagedリソースの作成()
		{
			if( !base.b活性化してない )
			{
				//            this.tx文字 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\9_text.png" ) );
				//            this.tx文字2 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\9_text.png" ) );
				//            this.tx文字3 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\9_text.png" ) );
				//this.tx背景 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\9_background.jpg" ), false );
				//            this.tx白 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\Tile white 64x64.png" ), false );
				//            this.ds背景 = CDTXMania.t失敗してもスキップ可能なDirectShowを生成する( CSkin.Path( @"Graphics\9_background.mp4" ), CDTXMania.app.WindowHandle, true );
				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if( !base.b活性化してない )
			{
				//CDTXMania.tテクスチャの解放( ref this.tx背景 );
    //            CDTXMania.tテクスチャの解放( ref this.tx文字 );
    //            CDTXMania.tテクスチャの解放( ref this.tx文字2 );
    //            CDTXMania.tテクスチャの解放( ref this.tx文字3 );
    //            CDTXMania.tテクスチャの解放( ref this.tx白 );
    //            CDTXMania.t安全にDisposeする( ref this.ds背景 );
				base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
            if( !TJAPlayer3.ConfigIni.bEndingAnime ) //2017.01.27 DD
            {
                return 1;
            }
            //if( this.ds背景 != null )
            //{
            //    this.ds背景.t再生開始();
                
            //    this.ds背景.t現時点における最新のスナップイメージをTextureに転写する( this.tx背景 );
            //}
			if( !base.b活性化してない )
			{
				if( base.b初めての進行描画 )
				{
					TJAPlayer3.Skin.soundゲーム終了音.t再生する();
					this.ct時間稼ぎ.t開始( 0, 3000, 1, TJAPlayer3.Timer );
                    base.b初めての進行描画 = false;
				}


				this.ct時間稼ぎ.t進行();

				if( TJAPlayer3.Tx.Exit_Background != null )
				{
                    //if( this.ds背景 != null )
                    //{
                    //    if( this.ds背景.b上下反転 )
                    //        this.tx背景.t2D上下反転描画( CDTXMania.app.Device, 0, 0 );
                    //    else
                    //        this.tx背景.t2D描画( CDTXMania.app.Device, 0, 0 );
                    //}
                    //else
                    TJAPlayer3.Tx.Exit_Background.t2D描画( TJAPlayer3.app.Device, 0, 0 );
				}


           //     if( this.ct時間稼ぎ.n現在の値 < 2000 )
           //     {
           //         if( this.tx文字 != null )
           //         {
           //             this.tx文字2.fZ軸中心回転 = -0.8f;
           //             this.tx文字3.fZ軸中心回転 = ( -1.6f * ( this.ct時間稼ぎ.n現在の値 / 1280.0f ) ) >= -1.6f ? ( -1.6f * ( this.ct時間稼ぎ.n現在の値 / 1280.0f ) ) : -1.6f ;
           //             this.tx文字2.vc拡大縮小倍率 = new Vector3( 4.0f, 4.0f, 1.0f );
           //             this.tx文字3.vc拡大縮小倍率 = new Vector3( 4.0f, 4.0f, 1.0f );

           //             this.tx文字2.t2D描画( CDTXMania.app.Device, 1100 - (int)( 1.30f * this.ct時間稼ぎ.n現在の値), 1600 - (int)( 1.6f * this.ct時間稼ぎ.n現在の値), new System.Drawing.Rectangle( 0, 0, 620, 92 )  );
           //             this.tx文字3.t2D描画( CDTXMania.app.Device, -250 + (int)( 1.10f * this.ct時間稼ぎ.n現在の値), 1600 - (int)( 1.6f * this.ct時間稼ぎ.n現在の値), new System.Drawing.Rectangle( 0, 92, 620, 94 )  );
           //         }
           //     }
           //     else
           //     {

           //         if( this.tx文字 != null )
           //         {
           //             this.tx文字2.fZ軸中心回転 = 0f;
           //             this.tx文字3.fZ軸中心回転 = 0f;
           //             this.tx文字2.vc拡大縮小倍率 = new Vector3( 1.3f, 1.3f, 1.0f );
           //             this.tx文字3.vc拡大縮小倍率 = new Vector3( 1.3f, 1.3f, 1.0f );

           //             this.tx文字2.t2D描画( CDTXMania.app.Device, 480, 376, new System.Drawing.Rectangle( 0, 0, 620, 92 )  );
           //             this.tx文字3.t2D描画( CDTXMania.app.Device, 500, 486, new System.Drawing.Rectangle( 0, 92, 620, 95 )  );
           //             this.tx文字.t2D描画( CDTXMania.app.Device, 662, 613, new System.Drawing.Rectangle( 0, 187, 620, 44 )  );
           //         }

           //         if( this.tx白 != null )
			        //{
				       // this.tx白.n透明度 = ( 2255 + 300 ) - ( this.ct時間稼ぎ.n現在の値 );
				       // for( int i = 0; i <= ( SampleFramework.GameWindowSize.Width / 64 ); i++ )
				       // {
					      //  for( int j = 0; j <= ( SampleFramework.GameWindowSize.Height / 64 ); j++ )
					      //  {
           //                     this.tx白.t2D描画( CDTXMania.app.Device, i * 64, j * 64 );
					      //  }
				       // }
			        //}
           //     }

                if( this.ct時間稼ぎ.b終了値に達した && !TJAPlayer3.Skin.soundゲーム終了音.b再生中 )
				{
					return 1;
				}
			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		private CCounter ct時間稼ぎ;
		//private CTexture tx背景;
  //      private CTexture tx文字;
  //      private CTexture tx文字2;
  //      private CTexture tx文字3;
  //      private CDirectShow ds背景;
  //      private CTexture tx白;
		//-----------------
		#endregion
	}
}
