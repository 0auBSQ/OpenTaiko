using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using FDK;
using System.Windows.Forms.VisualStyles;

namespace TJAPlayer3
{
	internal class CActSelectPresound : CActivity
	{
		// メソッド

		public CActSelectPresound()
		{
			base.b活性化してない = true;
		}
		public void tサウンド停止()
		{
			if( this.sound != null )
			{
				this.sound.t再生を停止する();
				TJAPlayer3.Sound管理.tサウンドを破棄する( this.sound );
				this.sound = null;
			}
		}
		public void t選択曲が変更された()
		{
			Cスコア cスコア = TJAPlayer3.stage選曲.r現在選択中のスコア;
			
            if( ( cスコア != null ) && ( ( !( cスコア.ファイル情報.フォルダの絶対パス + cスコア.譜面情報.strBGMファイル名 ).Equals( this.str現在のファイル名 ) || ( this.sound == null ) ) || !this.sound.b再生中 ) )
			{
				this.tサウンド停止();
				this.tBGMフェードイン開始();
                this.long再生位置 = -1;
				if( ( cスコア.譜面情報.strBGMファイル名 != null ) && ( cスコア.譜面情報.strBGMファイル名.Length > 0 ) )
				{
					//this.ct再生待ちウェイト = new CCounter( 0, CDTXMania.ConfigIni.n曲が選択されてからプレビュー音が鳴るまでのウェイトms, 1, CDTXMania.Timer );
                    if(TJAPlayer3.Sound管理.GetCurrentSoundDeviceType() != "DirectSound")
                    {
                        this.ct再生待ちウェイト = new CCounter(0, 1, 270, TJAPlayer3.Timer);
                    } else
                    {
                        this.ct再生待ちウェイト = new CCounter(0, 1, 500, TJAPlayer3.Timer);
                    }
                }
			}

            //if( ( cスコア != null ) && ( ( !( cスコア.ファイル情報.フォルダの絶対パス + cスコア.譜面情報.Presound ).Equals( this.str現在のファイル名 ) || ( this.sound == null ) ) || !this.sound.b再生中 ) )
            //{
            //    this.tサウンド停止();
            //    this.tBGMフェードイン開始();
            //    if( ( cスコア.譜面情報.Presound != null ) && ( cスコア.譜面情報.Presound.Length > 0 ) )
            //    {
            //        this.ct再生待ちウェイト = new CCounter( 0, CDTXMania.ConfigIni.n曲が選択されてからプレビュー音が鳴るまでのウェイトms, 1, CDTXMania.Timer );
            //    }
            //}
		}


		// CActivity 実装

		public override void On活性化()
		{
			this.sound = null;
			this.str現在のファイル名 = "";
			this.ct再生待ちウェイト = null;
			this.ctBGMフェードアウト用 = null;
			this.ctBGMフェードイン用 = null;
            this.long再生位置 = -1;
            this.long再生開始時のシステム時刻 = -1;
			base.On活性化();
		}
		public override void On非活性化()
		{
			this.tサウンド停止();
			this.ct再生待ちウェイト = null;
			this.ctBGMフェードイン用 = null;
			this.ctBGMフェードアウト用 = null;
			base.On非活性化();
		}
		public override int On進行描画()
		{
			if( !base.b活性化してない )
			{
				if( ( this.ctBGMフェードイン用 != null ) && this.ctBGMフェードイン用.b進行中 )
				{
					this.ctBGMフェードイン用.t進行();
					TJAPlayer3.Skin.bgm選曲画面.nAutomationLevel_現在のサウンド = this.ctBGMフェードイン用.n現在の値;
					if( this.ctBGMフェードイン用.b終了値に達した )
					{
						this.ctBGMフェードイン用.t停止();
					}
				}
				if( ( this.ctBGMフェードアウト用 != null ) && this.ctBGMフェードアウト用.b進行中 )
				{
					this.ctBGMフェードアウト用.t進行();
					TJAPlayer3.Skin.bgm選曲画面.nAutomationLevel_現在のサウンド = CSound.MaximumAutomationLevel - this.ctBGMフェードアウト用.n現在の値;
					if( this.ctBGMフェードアウト用.b終了値に達した )
					{
						this.ctBGMフェードアウト用.t停止();
					}
				}
				this.t進行処理_プレビューサウンド();

                if (this.sound != null)
                {
                    Cスコア cスコア = TJAPlayer3.stage選曲.r現在選択中のスコア;
                    if (long再生位置 == -1)
                    {
                        this.long再生開始時のシステム時刻 = CSound管理.rc演奏用タイマ.nシステム時刻ms;
                        this.long再生位置 = cスコア.譜面情報.nデモBGMオフセット;
                        this.sound.t再生位置を変更する(cスコア.譜面情報.nデモBGMオフセット);
                    }
                    else
                    {
						this.long再生位置 = CSound管理.rc演奏用タイマ.nシステム時刻ms - this.long再生開始時のシステム時刻;
						if (this.long再生位置 >= this.sound.n総演奏時間ms - cスコア.譜面情報.nデモBGMオフセット) //2020.04.18 Mr-Ojii #DEMOSTARTから何度も再生するために追加
							this.long再生位置 = -1;
					}
					//if (this.long再生位置 >= (this.sound.n総演奏時間ms - cスコア.譜面情報.nデモBGMオフセット) - 1 && this.long再生位置 <= (this.sound.n総演奏時間ms - cスコア.譜面情報.nデモBGMオフセット) + 0)
					//this.long再生位置 = -1;

					//CDTXMania.act文字コンソール.tPrint( 0, 0, C文字コンソール.Eフォント種別.白, this.long再生位置.ToString() );
					//CDTXMania.act文字コンソール.tPrint( 0, 20, C文字コンソール.Eフォント種別.白, (this.sound.n総演奏時間ms - cスコア.譜面情報.nデモBGMオフセット).ToString() );
				}
			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		private CCounter ctBGMフェードアウト用;
		private CCounter ctBGMフェードイン用;
		private CCounter ct再生待ちウェイト;
        private long long再生位置;
        private long long再生開始時のシステム時刻;
		private CSound sound;
		private string str現在のファイル名;
		
		private void tBGMフェードアウト開始()
		{
			if( this.ctBGMフェードイン用 != null )
			{
				this.ctBGMフェードイン用.t停止();
			}
			this.ctBGMフェードアウト用 = new CCounter( 0, 100, 10, TJAPlayer3.Timer );
			this.ctBGMフェードアウト用.n現在の値 = 100 - TJAPlayer3.Skin.bgm選曲画面.nAutomationLevel_現在のサウンド;
		}
		private void tBGMフェードイン開始()
		{
			if( this.ctBGMフェードアウト用 != null )
			{
				this.ctBGMフェードアウト用.t停止();
			}
			this.ctBGMフェードイン用 = new CCounter( 0, 100, 20, TJAPlayer3.Timer );
			this.ctBGMフェードイン用.n現在の値 = TJAPlayer3.Skin.bgm選曲画面.nAutomationLevel_現在のサウンド;
		}
		private void tプレビューサウンドの作成()
		{
			Cスコア cスコア = TJAPlayer3.stage選曲.r現在選択中のスコア;
			if( ( cスコア != null ) && !string.IsNullOrEmpty( cスコア.譜面情報.strBGMファイル名 ) && TJAPlayer3.stage選曲.eフェーズID != CStage.Eフェーズ.選曲_NowLoading画面へのフェードアウト )
			{
				string strPreviewFilename = cスコア.ファイル情報.フォルダの絶対パス + cスコア.譜面情報.Presound;
				try
                {
                    strPreviewFilename = cスコア.ファイル情報.フォルダの絶対パス + cスコア.譜面情報.strBGMファイル名;
					if(TJAPlayer3.ConfigIni.bBGM音を発声する)
                    this.sound = TJAPlayer3.Sound管理.tサウンドを生成する( strPreviewFilename, ESoundGroup.SongPreview );

                    // 2018-08-27 twopointzero - DO attempt to load (or queue scanning) loudness metadata here.
                    //                           Initialization, song enumeration, and/or interactions may have
                    //                           caused background scanning and the metadata may now be available.
                    //                           If is not yet available then we wish to queue scanning.
                    var loudnessMetadata = cスコア.譜面情報.SongLoudnessMetadata
                                           ?? LoudnessMetadataScanner.LoadForAudioPath(strPreviewFilename);
                    TJAPlayer3.SongGainController.Set( cスコア.譜面情報.SongVol, loudnessMetadata, this.sound );

					// Disable song if playing while playing the preview song
					CSongSelectSongManager.disable();

					this.sound.t再生を開始する( true );

                    if( long再生位置 == -1 )
                    {
                        this.long再生開始時のシステム時刻 = CSound管理.rc演奏用タイマ.nシステム時刻ms;
                        this.long再生位置 = cスコア.譜面情報.nデモBGMオフセット;
                        this.sound.t再生位置を変更する( cスコア.譜面情報.nデモBGMオフセット );
                        this.long再生位置 = CSound管理.rc演奏用タイマ.nシステム時刻ms - this.long再生開始時のシステム時刻;
                    }
                    //if( long再生位置 == this.sound.n総演奏時間ms - 10 )
                    //    this.long再生位置 = -1;

                    this.str現在のファイル名 = strPreviewFilename;
                    this.tBGMフェードアウト開始();
                    Trace.TraceInformation( "プレビューサウンドを生成しました。({0})", strPreviewFilename );
                    #region[ DTXMania(コメントアウト) ]
                    //this.sound = CDTXMania.Sound管理.tサウンドを生成する( strPreviewFilename );
                    //this.sound.t再生を開始する( true );
                    //this.str現在のファイル名 = strPreviewFilename;
                    //this.tBGMフェードアウト開始();
                    //Trace.TraceInformation( "プレビューサウンドを生成しました。({0})", strPreviewFilename );
                    #endregion
                }
				catch (Exception e)
				{
					Trace.TraceError( e.ToString() );
					Trace.TraceError( "プレビューサウンドの生成に失敗しました。({0})", strPreviewFilename );
					if( this.sound != null )
					{
						this.sound.Dispose();
					}
					this.sound = null;
				}
			}
		}
		private void t進行処理_プレビューサウンド()
		{
			if( ( this.ct再生待ちウェイト != null ) && !this.ct再生待ちウェイト.b停止中 )
			{
				this.ct再生待ちウェイト.t進行();
				if( !this.ct再生待ちウェイト.b終了値に達してない )
				{
					this.ct再生待ちウェイト.t停止();
					if( !TJAPlayer3.stage選曲.bスクロール中 )
					{
                        this.tプレビューサウンドの作成();
					}
				}
			}
		}
		//-----------------
		#endregion
	}
}
