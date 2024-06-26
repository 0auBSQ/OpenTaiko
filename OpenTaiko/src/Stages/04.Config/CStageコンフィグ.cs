using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using SkiaSharp;
using FDK;

namespace TJAPlayer3
{
	internal class CStageコンフィグ : CStage
	{
		// プロパティ

		public CActDFPFont actFont { get; private set; }
		public CActCalibrationMode actCalibrationMode;


		// コンストラクタ

		public CStageコンフィグ()
		{
			CActDFPFont font;
			base.eStageID = CStage.EStage.Config;
			base.ePhaseID = CStage.EPhase.Common_NORMAL;
			this.actFont = font = new CActDFPFont();
			base.ChildActivities.Add( font );
			base.ChildActivities.Add( this.actFIFO = new CActFIFOWhite() );
			base.ChildActivities.Add( this.actList = new CActConfigList() );
			base.ChildActivities.Add( this.actKeyAssign = new CActConfigKeyAssign() );
			base.ChildActivities.Add( this.actオプションパネル = new CActオプションパネル() );
			base.ChildActivities.Add( this.actCalibrationMode = new CActCalibrationMode() );
			base.IsDeActivated = true;
		}
		
		
		// メソッド

		public void tアサイン完了通知()															// CONFIGにのみ存在
		{																						//
			this.eItemPanelモード = EItemPanelモード.パッド一覧;								//
		}																						//
		public void tパッド選択通知( EKeyConfigPart part, EKeyConfigPad pad )							//
		{																						//
			this.actKeyAssign.t開始( part, pad, this.actList.ib現在の選択項目.str項目名 );		//
			this.eItemPanelモード = EItemPanelモード.キーコード一覧;							//
		}																						//
		public void t項目変更通知()																// OPTIONと共通
		{																						//
			this.t説明文パネルに現在選択されている項目の説明を描画する();						//
		}																						//

		
		// CStage 実装

		public override void Activate()
		{
			Trace.TraceInformation( "コンフィグステージを活性化します。" );
			Trace.Indent();
			try
			{
				TJAPlayer3.Skin.bgmコンフィグ画面.tPlay();

				this.n現在のメニュー番号 = 0;                                                    //
				for( int i = 0; i < 4; i++ )													//
				{																				//
					this.ctキー反復用[ i ] = new CCounter( 0, 0, 0, TJAPlayer3.Timer );			//
				}																				//
				this.bメニューにフォーカス中 = true;											// ここまでOPTIONと共通
				this.eItemPanelモード = EItemPanelモード.パッド一覧;
				
				ReloadMenus();

				Background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.CONFIG}Script.lua"));
				Background.Init();
				

				if( this.bメニューにフォーカス中 )
				{
					this.t説明文パネルに現在選択されているメニューの説明を描画する();
				}
				else
				{
					this.t説明文パネルに現在選択されている項目の説明を描画する();
				}
			}
			finally
			{
				Trace.TraceInformation( "コンフィグステージの活性化を完了しました。" );
				Trace.Unindent();
			}
			base.Activate();		// 2011.3.14 yyagi: On活性化()をtryの中から外に移動
		}
		public override void DeActivate()
		{
			Trace.TraceInformation( "コンフィグステージを非活性化します。" );
			Trace.Indent();
			try
			{
				TJAPlayer3.Skin.bgmコンフィグ画面.tStop();

				TJAPlayer3.ConfigIni.t書き出し( TJAPlayer3.strEXEのあるフォルダ + "Config.ini" );	// CONFIGだけ
				for( int i = 0; i < 4; i++ )
				{
					this.ctキー反復用[ i ] = null;
				}
					
				for ( int i = 0; i < txMenuItemLeft.GetLength( 0 ); i++ )
				{
					txMenuItemLeft[ i, 0 ].Dispose();
					txMenuItemLeft[ i, 0 ] = null;
					txMenuItemLeft[ i, 1 ].Dispose();
					txMenuItemLeft[ i, 1 ] = null;
				}
				txMenuItemLeft = null;

				TJAPlayer3.tDisposeSafely(ref Background);

				base.DeActivate();
			}
			catch ( UnauthorizedAccessException e )
			{
			    Trace.TraceError( e.ToString() );
				Trace.TraceError( "ファイルが読み取り専用になっていないか、管理者権限がないと書き込めなくなっていないか等を確認して下さい" );
				Trace.TraceError( "例外が発生しましたが処理を継続します。 (7a61f01b-1703-4aad-8d7d-08bd88ae8760)" );
			}
			catch ( Exception e )
			{
				Trace.TraceError( e.ToString() );
				Trace.TraceError( "例外が発生しましたが処理を継続します。 (83f0d93c-bb04-4a19-a596-bc32de39f496)" );
			}
			finally
			{
				Trace.TraceInformation( "コンフィグステージの非活性化を完了しました。" );
				Trace.Unindent();
			}
		}

		public void ReloadMenus()
        {
			string[] strMenuItem = {
					CLangManager.LangInstance.GetString("SETTINGS_SYSTEM"),
					CLangManager.LangInstance.GetString("SETTINGS_GAME"),
					CLangManager.LangInstance.GetString("SETTINGS_EXIT")
			};

			txMenuItemLeft = new CTexture[strMenuItem.Length, 2];

			using (var prvFont = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.Config_Font_Scale))
			{
				for (int i = 0; i < strMenuItem.Length; i++)
				{
					using (var bmpStr = prvFont.DrawText(strMenuItem[i], Color.White, Color.Black, null, 30))
					{
						txMenuItemLeft[i, 0]?.Dispose();
						txMenuItemLeft[i, 0] = TJAPlayer3.tテクスチャの生成(bmpStr, false);
					}
					using (var bmpStr = prvFont.DrawText(strMenuItem[i], 
						Color.White, 
						Color.Black,
						null,
                        TJAPlayer3.Skin.Config_Selected_Menu_Text_Grad_Color_1,
                        TJAPlayer3.Skin.Config_Selected_Menu_Text_Grad_Color_2,
						30))
					{
						txMenuItemLeft[i, 1]?.Dispose();
						txMenuItemLeft[i, 1] = TJAPlayer3.tテクスチャの生成(bmpStr, false);
					}
				}
			}
		}

		public override void CreateManagedResource()											// OPTIONと画像以外共通
		{
            //if (HPrivateFastFont.FontExists(TJAPlayer3.Skin.FontName))
            //{
            //    this.ftフォント = new CCachedFontRenderer(TJAPlayer3.Skin.FontName, (int)TJAPlayer3.Skin.Config_Font_Scale_Description, CFontRenderer.FontStyle.Bold);
            //}
            //else
            //{
            //    this.ftフォント = new CCachedFontRenderer(CFontRenderer.DefaultFontName, (int)TJAPlayer3.Skin.Config_Font_Scale_Description, CFontRenderer.FontStyle.Bold);
            //}
			this.ftフォント = HPrivateFastFont.tInstantiateMainFont((int)TJAPlayer3.Skin.Config_Font_Scale_Description, CFontRenderer.FontStyle.Bold);


            TJAPlayer3.Tx.Config_Cursor = TJAPlayer3.tテクスチャの生成(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.CONFIG}Cursor.png"));

				//ctBackgroundAnime = new CCounter(0, TJAPlayer3.Tx.Config_Background.szテクスチャサイズ.Width, 20, TJAPlayer3.Timer);

				/*
				string[] strMenuItem = {
					CLangManager.LangInstance.GetString(10085),
					CLangManager.LangInstance.GetString(10086),
					CLangManager.LangInstance.GetString(10087)
				};
			    
				txMenuItemLeft = new CTexture[strMenuItem.Length, 2];

			    using (var prvFont = new CPrivateFastFont(new FontFamily(string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName) ? "MS UI Gothic" :  TJAPlayer3.ConfigIni.FontName), 20))
			    {
			        for (int i = 0; i < strMenuItem.Length; i++)
			        {
			            using (var bmpStr = prvFont.DrawPrivateFont(strMenuItem[i], Color.White, Color.Black))
			            {
			                txMenuItemLeft[i, 0] = TJAPlayer3.tテクスチャの生成(bmpStr, false);
			            }
			            using (var bmpStr = prvFont.DrawPrivateFont(strMenuItem[i], Color.White, Color.Black, Color.Yellow, Color.OrangeRed))
			            {
			                txMenuItemLeft[i, 1] = TJAPlayer3.tテクスチャの生成(bmpStr, false);
			            }
			        }
			    }
				*/
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()											// OPTIONと同じ(COnfig.iniの書き出しタイミングのみ異なるが、無視して良い)
		{
			if( this.ftフォント != null )
			{
				this.ftフォント.Dispose();
				this.ftフォント = null;
			}
				//CDTXMania.tテクスチャの解放( ref this.tx背景 );
				//CDTXMania.tテクスチャの解放( ref this.tx上部パネル );
				//CDTXMania.tテクスチャの解放( ref this.tx下部パネル );
				//CDTXMania.tテクスチャの解放( ref this.txMenuカーソル );

			TJAPlayer3.tテクスチャの解放( ref this.tx説明文パネル );
			base.ReleaseManagedResource();
		}
		public override int Draw()
		{
			if( base.IsDeActivated )
				return 0;

			if( base.IsFirstDraw )
			{
				base.ePhaseID = CStage.EPhase.Common_FADEIN;
				this.actFIFO.tフェードイン開始();
				base.IsFirstDraw = false;
			}

			//ctBackgroundAnime.t進行Loop();

			// 描画

			#region [ Background ]

			//---------------------
			/*
			for(int i = 0; i < 2; i++)
				if (TJAPlayer3.Tx.Config_Background != null )
					TJAPlayer3.Tx.Config_Background.t2D描画( 0 + -(TJAPlayer3.Tx.Config_Background.szテクスチャサイズ.Width * i) + ctBackgroundAnime.n現在の値, 0 );
			if(TJAPlayer3.Tx.Config_Header != null )
                TJAPlayer3.Tx.Config_Header.t2D描画( 0, 0 );
			*/
			Background.Update();
			Background.Draw();
			//---------------------

			#endregion

			#region [ Menu Cursor ]
			//---------------------
			if ( TJAPlayer3.Tx.Config_Cursor != null )
			{
				#region Old
				/*
				Rectangle rectangle;
                TJAPlayer3.Tx.TJAPlayer3.Tx.Config_Cursor.Opacity = this.bメニューにフォーカス中 ? 255 : 128;
				int x = 110;
				int y = (int)( 145.5 + ( this.n現在のメニュー番号 * 37.5 ) );
				int num3 = 340;
                TJAPlayer3.Tx.TJAPlayer3.Tx.Config_Cursor.t2D描画( x, y, new Rectangle( 0, 0, 32, 48 ) );
                TJAPlayer3.Tx.TJAPlayer3.Tx.Config_Cursor.t2D描画( ( x + num3 ) - 32, y, new Rectangle( 20, 0, 32, 48 ) );
				x += 32;
				for( num3 -= 64; num3 > 0; num3 -= rectangle.Width )
				{
					rectangle = new Rectangle( 16, 0, 32, 48 );
					if( num3 < 32 )
					{
						rectangle.Width -= 32 - num3;
					}
                    TJAPlayer3.Tx.TJAPlayer3.Tx.Config_Cursor.t2D描画( x, y, rectangle );
					x += rectangle.Width;
				}
				*/
				#endregion


				int x = TJAPlayer3.Skin.Config_Item_X[this.n現在のメニュー番号];
				int y = TJAPlayer3.Skin.Config_Item_Y[this.n現在のメニュー番号];

				int width = TJAPlayer3.Tx.Config_Cursor.sz画像サイズ.Width / 3;
				int height = TJAPlayer3.Tx.Config_Cursor.sz画像サイズ.Height;

				int move = TJAPlayer3.Skin.Config_Item_Width;

				//Left
				TJAPlayer3.Tx.Config_Cursor.t2D中心基準描画(x - (width / 2) - move, y, 
					new Rectangle(0, 0, width, height));

				//Right
				TJAPlayer3.Tx.Config_Cursor.t2D中心基準描画(x + (width / 2) + move, y, 
					new Rectangle(width * 2, 0, width, height));

				//Center
				TJAPlayer3.Tx.Config_Cursor.vcScaleRatio.X = (move / (float)width) * 2.0f;
				TJAPlayer3.Tx.Config_Cursor.t2D拡大率考慮中央基準描画(x, y, 
					new Rectangle(width, 0, width, height));

				TJAPlayer3.Tx.Config_Cursor.vcScaleRatio.X = 1.0f;
			}
            //---------------------
            #endregion

            #region [ Menu ]
            //---------------------
            //int menuY = 162 - 22 + 13;
            //int stepY = 39;
            for ( int i = 0; i < txMenuItemLeft.GetLength( 0 ); i++ )
			{
				//Bitmap bmpStr = (this.n現在のメニュー番号 == i) ?
				//      prvFont.DrawPrivateFont( strMenuItem[ i ], Color.White, Color.Black, Color.Yellow, Color.OrangeRed ) :
				//      prvFont.DrawPrivateFont( strMenuItem[ i ], Color.White, Color.Black );
				//txMenuItemLeft = CDTXMania.tテクスチャの生成( bmpStr, false );

				int flag = (this.n現在のメニュー番号 == i) ? 1 : 0;
				txMenuItemLeft[i, flag].t2D中心基準描画(TJAPlayer3.Skin.Config_Item_X[i] + TJAPlayer3.Skin.Config_Item_Font_Offset[0], TJAPlayer3.Skin.Config_Item_Y[i] + TJAPlayer3.Skin.Config_Item_Font_Offset[1]); //55
				//txMenuItem.Dispose();
				//menuY += stepY;
			}
			//---------------------
			#endregion
			
			#region [ Explanation Panel ]
			//---------------------
			if( this.tx説明文パネル != null )
				this.tx説明文パネル.t2D描画( TJAPlayer3.Skin.Config_ExplanationPanel[0], TJAPlayer3.Skin.Config_ExplanationPanel[1]);
			//---------------------
			#endregion
			
			#region [ Item ]
			//---------------------
			switch( this.eItemPanelモード )
			{
				case EItemPanelモード.パッド一覧:
					this.actList.t進行描画( !this.bメニューにフォーカス中 );
					break;

				case EItemPanelモード.キーコード一覧:
					this.actKeyAssign.Draw();
					break;
			}
			//---------------------
			#endregion
			
			//#region [ 上部パネル ]
			////---------------------
			//if( this.tx上部パネル != null )
			//	this.tx上部パネル.t2D描画( CDTXMania.app.Device, 0, 0 );
			////---------------------
			//#endregion
			//#region [ 下部パネル ]
			////---------------------
			//if( this.tx下部パネル != null )
			//	this.tx下部パネル.t2D描画( CDTXMania.app.Device, 0, 720 - this.tx下部パネル.szテクスチャサイズ.Height );
			////---------------------
			//#endregion

			#region [ Option Panel ]
			//---------------------
            //this.actオプションパネル.On進行描画();
			//---------------------
			#endregion

			#region [ FadeOut ]
			//---------------------
			switch( base.ePhaseID )
			{
				case CStage.EPhase.Common_FADEIN:
					if( this.actFIFO.Draw() != 0 )
					{
						base.ePhaseID = CStage.EPhase.Common_NORMAL;
					}
					break;

				case CStage.EPhase.Common_FADEOUT:
					if( this.actFIFO.Draw() == 0 )
					{
						break;
					}
					return 1;
			}
			//---------------------
			#endregion

			#region [ Enumerating Songs ]
			// CActEnumSongs側で表示する
			#endregion

			// キー入力

			if( ( base.ePhaseID != CStage.EPhase.Common_NORMAL )
				|| this.actKeyAssign.bキー入力待ちの最中である
				|| TJAPlayer3.act現在入力を占有中のプラグイン != null )
				return 0;

			if (actCalibrationMode.IsStarted)
			{
				if (TJAPlayer3.Skin.bgmコンフィグ画面.bIsPlaying)
					TJAPlayer3.Skin.bgmコンフィグ画面.tStop();

				actCalibrationMode.Update();
				actCalibrationMode.Draw();
			}
			// 曲データの一覧取得中は、キー入力を無効化する
			else if ( !TJAPlayer3.EnumSongs.IsEnumerating || TJAPlayer3.actEnumSongs.bコマンドでの曲データ取得 != true )
            {
                if (!TJAPlayer3.Skin.bgmコンフィグ画面.bIsPlaying)
                    TJAPlayer3.Skin.bgmコンフィグ画面.tPlay();

                if ( ( TJAPlayer3.InputManager.Keyboard.KeyPressed( (int)SlimDXKeys.Key.Escape ) || TJAPlayer3.Pad.bPressed( EInstrumentPad.DRUMS, EPad.FT ) ) || TJAPlayer3.Pad.bPressedGB( EPad.FT ) )
				{
					TJAPlayer3.Skin.soundCancelSFX.tPlay();
					if ( !this.bメニューにフォーカス中 )
					{
						if ( this.eItemPanelモード == EItemPanelモード.キーコード一覧 )
						{
							TJAPlayer3.stageコンフィグ.tアサイン完了通知();
							return 0;
						}
						if ( !this.actList.bIsKeyAssignSelected && !this.actList.bIsFocusingParameter )	// #24525 2011.3.15 yyagi, #32059 2013.9.17 yyagi
						{
							this.bメニューにフォーカス中 = true;
						}
						this.t説明文パネルに現在選択されているメニューの説明を描画する();
						this.actList.tEsc押下();								// #24525 2011.3.15 yyagi ESC押下時の右メニュー描画用
					}
					else
					{
						this.actFIFO.tフェードアウト開始();
						base.ePhaseID = CStage.EPhase.Common_FADEOUT;
					}
				}
				else if ( ( TJAPlayer3.Pad.bPressedDGB( EPad.CY ) || TJAPlayer3.Pad.bPressed( EInstrumentPad.DRUMS, EPad.RD ) ) || ( TJAPlayer3.Pad.bPressed( EInstrumentPad.DRUMS, EPad.LC ) || ( TJAPlayer3.ConfigIni.bEnterがキー割り当てのどこにも使用されていない && TJAPlayer3.InputManager.Keyboard.KeyPressed( (int)SlimDXKeys.Key.Return ) ) ) )
				{
					if ( this.n現在のメニュー番号 == 2 )
					{
						// Exit
						TJAPlayer3.Skin.soundDecideSFX.tPlay();
						this.actFIFO.tフェードアウト開始();
						base.ePhaseID = CStage.EPhase.Common_FADEOUT;
					}
					else if ( this.bメニューにフォーカス中 )
					{
						TJAPlayer3.Skin.soundDecideSFX.tPlay();
						this.bメニューにフォーカス中 = false;
						this.t説明文パネルに現在選択されている項目の説明を描画する();
					}
					else
					{
						switch ( this.eItemPanelモード )
						{
							case EItemPanelモード.パッド一覧:
								bool bIsKeyAssignSelectedBeforeHitEnter = this.actList.bIsKeyAssignSelected;	// #24525 2011.3.15 yyagi
								this.actList.tEnter押下();

								this.t説明文パネルに現在選択されている項目の説明を描画する();

								if ( this.actList.b現在選択されている項目はReturnToMenuである )
								{
									this.t説明文パネルに現在選択されているメニューの説明を描画する();
									if ( bIsKeyAssignSelectedBeforeHitEnter == false )							// #24525 2011.3.15 yyagi
									{
										this.bメニューにフォーカス中 = true;
									}
								}
								break;

							case EItemPanelモード.キーコード一覧:
								this.actKeyAssign.tEnter押下();
								break;
						}
					}
				}
				this.ctキー反復用.Up.KeyIntervalFunc( TJAPlayer3.InputManager.Keyboard.KeyPressing( (int)SlimDXKeys.Key.UpArrow ), new CCounter.KeyProcess( this.tカーソルを上へ移動する ) );
				this.ctキー反復用.R.KeyIntervalFunc( TJAPlayer3.Pad.b押されているGB( EPad.HH ), new CCounter.KeyProcess( this.tカーソルを上へ移動する ) );
				if ( TJAPlayer3.Pad.bPressed( EInstrumentPad.DRUMS, EPad.SD ) )
				{
					this.tカーソルを上へ移動する();
				}
				this.ctキー反復用.Down.KeyIntervalFunc( TJAPlayer3.InputManager.Keyboard.KeyPressing( (int)SlimDXKeys.Key.DownArrow ), new CCounter.KeyProcess( this.tカーソルを下へ移動する ) );
				this.ctキー反復用.B.KeyIntervalFunc( TJAPlayer3.Pad.b押されているGB( EPad.BD ), new CCounter.KeyProcess( this.tカーソルを下へ移動する ) );
				if ( TJAPlayer3.Pad.bPressed( EInstrumentPad.DRUMS, EPad.LT ) )
				{
					this.tカーソルを下へ移動する();
				}
			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		private enum EItemPanelモード
		{
			パッド一覧,
			キーコード一覧
		}

		[StructLayout( LayoutKind.Sequential )]
		private struct STキー反復用カウンタ
		{
			public CCounter Up;
			public CCounter Down;
			public CCounter R;
			public CCounter B;
			public CCounter this[ int index ]
			{
				get
				{
					switch( index )
					{
						case 0:
							return this.Up;

						case 1:
							return this.Down;

						case 2:
							return this.R;

						case 3:
							return this.B;
					}
					throw new IndexOutOfRangeException();
				}
				set
				{
					switch( index )
					{
						case 0:
							this.Up = value;
							return;

						case 1:
							this.Down = value;
							return;

						case 2:
							this.R = value;
							return;

						case 3:
							this.B = value;
							return;
					}
					throw new IndexOutOfRangeException();
				}
			}
		}

		//private CCounter ctBackgroundAnime;
		private CActFIFOWhite actFIFO;
		private CActConfigKeyAssign actKeyAssign;
		public CActConfigList actList;
		private CActオプションパネル actオプションパネル;
		private bool bメニューにフォーカス中;
		private STキー反復用カウンタ ctキー反復用;
		private const int DESC_H = 0x80;
		private const int DESC_W = 220;
		private EItemPanelモード eItemPanelモード;
		internal CCachedFontRenderer ftフォント;
		private int n現在のメニュー番号;
		//private CTexture txMenuカーソル;
		//private CTexture tx下部パネル;
		//private CTexture tx上部パネル;
		private CTexture tx説明文パネル;
		//private CTexture tx背景;
		private CTexture[ , ] txMenuItemLeft;

		private ScriptBG Background;

		private void tカーソルを下へ移動する()
		{
			if( !this.bメニューにフォーカス中 )
			{
				switch( this.eItemPanelモード )
				{
					case EItemPanelモード.パッド一覧:
						this.actList.t次に移動();
						return;

					case EItemPanelモード.キーコード一覧:
						this.actKeyAssign.t次に移動();
						return;
				}
			}
			else
			{
				TJAPlayer3.Skin.soundカーソル移動音.tPlay();
				this.n現在のメニュー番号 = ( this.n現在のメニュー番号 + 1 ) % 3;
				switch( this.n現在のメニュー番号 )
				{
					case 0:
						this.actList.t項目リストの設定_System();
						break;

					case 1:
						this.actList.t項目リストの設定_Drums();
						break;

					case 2:
						this.actList.t項目リストの設定_Exit();
						break;
				}
				this.t説明文パネルに現在選択されているメニューの説明を描画する();
			}
		}
		private void tカーソルを上へ移動する()
		{
			if( !this.bメニューにフォーカス中 )
			{
				switch( this.eItemPanelモード )
				{
					case EItemPanelモード.パッド一覧:
						this.actList.t前に移動();
						return;

					case EItemPanelモード.キーコード一覧:
						this.actKeyAssign.t前に移動();
						return;
				}
			}
			else
			{
				TJAPlayer3.Skin.soundカーソル移動音.tPlay();
				this.n現在のメニュー番号 = ((this.n現在のメニュー番号 - 1) + 3) % 3;
				switch ( this.n現在のメニュー番号 )
				{
					case 0:
						this.actList.t項目リストの設定_System();
						break;

					case 1:
						this.actList.t項目リストの設定_Drums();
						break;

					case 2:
						this.actList.t項目リストの設定_Exit();
						break;
				}
				this.t説明文パネルに現在選択されているメニューの説明を描画する();
			}
		}
		private void t説明文パネルに現在選択されているメニューの説明を描画する()
		{
			try
			{
				string text = "";
				switch( this.n現在のメニュー番号 )
				{
					case 0:
						text = CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DESC");
						break;
					case 1:
						text = CLangManager.LangInstance.GetString("SETTINGS_GAME_DESC");
						break;
					case 2:
						text = CLangManager.LangInstance.GetString("SETTINGS_EXIT_DESC");
						break;
                }
				SKBitmap image = ftフォント.DrawText(text, Color.White, Color.Black, null, 30);
				if( this.tx説明文パネル != null )
				{
					this.tx説明文パネル.Dispose();
				}
				this.tx説明文パネル = new CTexture( image );
				image.Dispose();
			}
			catch( CTextureCreateFailedException e)
			{
				Trace.TraceError( e.ToString() );
				Trace.TraceError( "説明文テクスチャの作成に失敗しました。" );
				this.tx説明文パネル = null;
			}
		}
		private void t説明文パネルに現在選択されている項目の説明を描画する()
		{
			try
			{
				var image = new SKBitmap( 440, 288 );		// 説明文領域サイズの縦横 2 倍。（描画時に 0.5 倍で表示する___のは中止。処理速度向上のため。）

                CItemBase item = this.actList.ib現在の選択項目;
				if( ( item.str説明文 != null ) && ( item.str説明文.Length > 0 ) )
				{
					image.Dispose();
					image = ftフォント.DrawText(item.str説明文, Color.White, Color.Black, null, 30);
				}
				if( this.tx説明文パネル != null )
				{
					this.tx説明文パネル.Dispose();
				}
				this.tx説明文パネル = new CTexture( image );
				image.Dispose();
			}
			catch( CTextureCreateFailedException e )
			{
				Trace.TraceError( e.ToString() );
				Trace.TraceError( "説明文パネルテクスチャの作成に失敗しました。" );
				this.tx説明文パネル = null;
			}
		}
		//-----------------
		#endregion
	}
}
