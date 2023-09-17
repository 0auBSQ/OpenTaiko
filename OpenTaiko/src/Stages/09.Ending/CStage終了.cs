using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
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
			base.IsDeActivated = true;
		}


		// CStage 実装

		public override void Activate()
		{
			Trace.TraceInformation( "終了ステージを活性化します。" );
			Trace.Indent();
			try
			{
				this.ct時間稼ぎ = new CCounter();

				Background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.EXIT}Script.lua"));
				Background.Init();

				base.Activate();
			}
			finally
			{
				Trace.TraceInformation( "終了ステージの活性化を完了しました。" );
				Trace.Unindent();
			}
		}
		public override void DeActivate()
		{
			Trace.TraceInformation( "終了ステージを非活性化します。" );
			Trace.Indent();
			try
			{
				TJAPlayer3.t安全にDisposeする(ref Background);
				base.DeActivate();
			}
			finally
			{
				Trace.TraceInformation( "終了ステージの非活性化を完了しました。" );
				Trace.Unindent();
			}
		}
		public override void CreateManagedResource()
		{

				//            this.tx文字 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\9_text.png" ) );
				//            this.tx文字2 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\9_text.png" ) );
				//            this.tx文字3 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\9_text.png" ) );
				//this.tx背景 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\9_background.jpg" ), false );
				//            this.tx白 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\Tile white 64x64.png" ), false );
				//            this.ds背景 = CDTXMania.t失敗してもスキップ可能なDirectShowを生成する( CSkin.Path( @"Graphics\9_background.mp4" ), CDTXMania.app.WindowHandle, true );
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{

				//CDTXMania.tテクスチャの解放( ref this.tx背景 );
				//            CDTXMania.tテクスチャの解放( ref this.tx文字 );
				//            CDTXMania.tテクスチャの解放( ref this.tx文字2 );
				//            CDTXMania.tテクスチャの解放( ref this.tx文字3 );
				//            CDTXMania.tテクスチャの解放( ref this.tx白 );
				//            CDTXMania.t安全にDisposeする( ref this.ds背景 );
			base.ReleaseManagedResource();
		}
		public override int Draw()
		{
			/*
            if( !TJAPlayer3.ConfigIni.bEndingAnime ) //2017.01.27 DD
            {
                return 1;
            }
			*/

			if( !base.IsDeActivated )
			{
				if( base.IsFirstDraw )
				{
					TJAPlayer3.Skin.soundゲーム終了音.t再生する();
					this.ct時間稼ぎ.Start( 0, TJAPlayer3.Skin.Exit_Duration, 1, TJAPlayer3.Timer );
                    base.IsFirstDraw = false;
				}


				this.ct時間稼ぎ.Tick();

				Background.Update();
				Background.Draw();

				//TJAPlayer3.Tx.Exit_Background?.t2D描画( 0, 0 );

                if( this.ct時間稼ぎ.IsEnded && !TJAPlayer3.Skin.soundゲーム終了音.b再生中 )
				{
					return 1;
				}
			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		private ScriptBG Background;
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
