using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing.Text;

using SharpDX;
using FDK;

using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;
using Color = System.Drawing.Color;

namespace TJAPlayer3
{
    /// <summary>
    /// 難易度選択画面。
    /// この難易度選択画面はAC7～AC14のような方式であり、WiiまたはAC15移行の方式とは異なる。
    /// </summary>
	internal class CActSelect難易度選択画面 : CActivity
	{
		// プロパティ

        public bool bIsDifficltSelect;

		// コンストラクタ

        public CActSelect難易度選択画面()
        {
            for(int i = 0; i < 10; i++)
            {
                st小文字位置[i].ptX = i * 18;
                st小文字位置[i].ch = i.ToString().ToCharArray()[0];
            }
            base.b活性化してない = true;
		}

        public void t次に移動(int player)
        {
            if (n現在の選択行[player] < 5)
            {
                ctBarAnime[player].t開始(0, 180, 1, TJAPlayer3.Timer);
                if (!b裏譜面)
                {
                    n現在の選択行[player]++;
                }
                else
                {
                    if (n現在の選択行[player] == 4)
                    {
                        n現在の選択行[player] += 2;
                    }
                    else
                    {
                        n現在の選択行[player]++;
                    }
                }
            }
            else if (n現在の選択行[player] >= 5)
            {
                if (TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[4] < 0 || TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[3] < 0)
                    return;

                if (nスイッチカウント < 0)
                {
                    nスイッチカウント++;
                }
                else if (nスイッチカウント == 0)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if(!bSelect[i])
                        {
                            if (n現在の選択行[i] == 5)
                            {
                                n現在の選択行[i] = 6;
                            }
                            else if (n現在の選択行[i] == 6)
                            {
                                n現在の選択行[i] = 5;
                            }
                        }
                    }

                    b裏譜面 = !b裏譜面;
                    nスイッチカウント = 0;
                }
            }
        }

		public void t前に移動(int player)
		{
            if(n現在の選択行[player] - 1 >= 0)
            {
                ctBarAnime[player].t開始(0, 180, 1, TJAPlayer3.Timer);
                nスイッチカウント = 0;
                if(n現在の選択行[player] == 6)
                    n現在の選択行[player] -= 2;
                else
                    n現在の選択行[player]--;
            }
		}

		public void t選択画面初期化()
        {
            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
            {
                this.pfTitle = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 28);
                this.pfSubTitle = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 16);
            }
            else
            {
                this.pfTitle = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 28);
                this.pfSubTitle = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 16);
            }

            this.txTitle = TJAPlayer3.tテクスチャの生成(pfTitle.DrawPrivateFont(TJAPlayer3.stage選曲.r現在選択中の曲.strタイトル, Color.White, Color.Black ));
            this.txSubTitle = TJAPlayer3.tテクスチャの生成(pfSubTitle.DrawPrivateFont(TJAPlayer3.stage選曲.r現在選択中の曲.strサブタイトル, Color.White, Color.Black));
            
            this.n現在の選択行 = new int[2];
            this.bSelect[0] = false;
            this.bSelect[1] = false;

            this.b裏譜面 = (TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Edit] >= 0 && TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Oni] < 0);

            this.b初めての進行描画 = true;
		}

		// CActivity 実装

		public override void On活性化()
		{
			if( this.b活性化してる )
				return;
            ctBarAnime = new CCounter[2];
            ctBarAnime[0] = new CCounter();
            ctBarAnime[1] = new CCounter();

            base.On活性化();
		}
		public override void On非活性化()
		{
			if( this.b活性化してない )
				return;

            ctBarAnime = null;

            base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if( this.b活性化してない )
				return;

            // this.soundSelectAnnounce = TJAPlayer3.Sound管理.tサウンドを生成する( CSkin.Path( @"Sounds\DiffSelect.ogg" ), ESoundGroup.SoundEffect );

			base.OnManagedリソースの作成();
		}
		public override void OnManagedリソースの解放()
		{
			if( this.b活性化してない )
				return;

            // TJAPlayer3.t安全にDisposeする( ref this.soundSelectAnnounce );

			base.OnManagedリソースの解放();
		}
        public override int On進行描画()
        {
            if (this.b活性化してない)
                return 0;

            #region [ 初めての進行描画 ]
            //-----------------
            if (this.b初めての進行描画)
            {
                ctBarAnimeIn = new CCounter(0, 170, 4, TJAPlayer3.Timer);
                // this.soundSelectAnnounce?.tサウンドを再生する();
                //TJAPlayer3.Skin.soundSelectAnnounce.t再生する();
                TJAPlayer3.Skin.voiceMenuDiffSelect[TJAPlayer3.SaveFile]?.t再生する();
                base.b初めての進行描画 = false;
            }
            //-----------------
            #endregion

            ctBarAnimeIn.t進行();
            ctBarAnime[0].t進行();
            ctBarAnime[1].t進行();

            bool uraExists = TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Edit] >= 0;
            bool omoteExists = TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Oni] >= 0;

            #region [ キー入力 ]

            if (this.ctBarAnimeIn.b終了値に達した)
            {
                if (!bSelect[0] && !isOnOption())
                {
                    if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RightChange) || TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.RightArrow))
                    {
                        TJAPlayer3.Skin.sound変更音.t再生する();
                        this.t次に移動(0);
                    }
                    else if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LeftChange) || TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.LeftArrow))
                    {
                        TJAPlayer3.Skin.sound変更音.t再生する();
                        this.t前に移動(0);
                    }
                    if (TJAPlayer3.Pad.b押されたDGB(Eパッド.Decide) ||
                             (TJAPlayer3.ConfigIni.bEnterがキー割り当てのどこにも使用されていない && TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Return)))
                    {
                        if (n現在の選択行[0] == 0)
                        {
                            TJAPlayer3.Skin.sound決定音.t再生する();
                            TJAPlayer3.stage選曲.act曲リスト.ctBarOpen.t開始(100, 260, 2, TJAPlayer3.Timer);
                            this.bIsDifficltSelect = false;
                        }
                        else if (n現在の選択行[0] == 1)
                        {
                            TJAPlayer3.Skin.sound決定音.t再生する();
                            bOption[0] = true;
                        }
                        else
                        {
                            if (TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[n現在の選択行[0] - 2] > 0)
                            {
                                //TJAPlayer3.stage選曲.ctDonchan_Jump[0].t開始(0, TJAPlayer3.Tx.SongSelect_Donchan_Jump.Length - 1, 1000 / 45, TJAPlayer3.Timer);
                                CMenuCharacter.tMenuResetTimer(0, CMenuCharacter.ECharacterAnimation.START);

                                this.bSelect[0] = true;
                                TJAPlayer3.Skin.sound決定音.t再生する();
                                TJAPlayer3.Skin.voiceMenuSongDecide[TJAPlayer3.SaveFile]?.t再生する();

                                if(TJAPlayer3.ConfigIni.nPlayerCount == 2)
                                {
                                    if (bSelect[1])
                                    {
                                        TJAPlayer3.stage選曲.t曲を選択する(n現在の選択行[0] - 2, 0);
                                        TJAPlayer3.stage選曲.t曲を選択する(n現在の選択行[1] - 2, 1);
                                    }
                                }
                                else
                                {
                                    TJAPlayer3.stage選曲.t曲を選択する(n現在の選択行[0] - 2, 0);
                                }
                            }
                        }
                    }
                    if (TJAPlayer3.Pad.b押されたDGB(Eパッド.Cancel) || TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Escape))
                    {
                        TJAPlayer3.Skin.sound決定音.t再生する();
                        TJAPlayer3.stage選曲.act曲リスト.ctBarOpen.t開始(100, 260, 2, TJAPlayer3.Timer);
                        this.bIsDifficltSelect = false;
                    }
                }

                if (!bSelect[1] && !isOnOption() && TJAPlayer3.ConfigIni.nPlayerCount > 1)
                {
                    if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue2P))
                    {
                        TJAPlayer3.Skin.sound変更音.t再生する();
                        this.t次に移動(1);
                    }
                    else if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue2P))
                    {
                        TJAPlayer3.Skin.sound変更音.t再生する();
                        this.t前に移動(1);
                    }
                    if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed2P) || TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed2P))
                    {
                        if (n現在の選択行[1] == 0)
                        {
                            TJAPlayer3.Skin.sound決定音.t再生する();
                            TJAPlayer3.stage選曲.act曲リスト.ctBarOpen.t開始(100, 260, 2, TJAPlayer3.Timer);
                            this.bIsDifficltSelect = false;
                            CMenuCharacter.tDisableCounter(CMenuCharacter.ECharacterAnimation.START);

                        }
                        else if (n現在の選択行[1] == 1)
                        {
                            TJAPlayer3.Skin.sound決定音.t再生する();
                            bOption[1] = true;
                        }
                        else
                        {
                            if (TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[n現在の選択行[1] - 2] > 0)
                            {
                                //TJAPlayer3.stage選曲.ctDonchan_Jump[1].t開始(0, TJAPlayer3.Tx.SongSelect_Donchan_Jump.Length - 1, 1000 / 45, TJAPlayer3.Timer);
                                CMenuCharacter.tMenuResetTimer(1, CMenuCharacter.ECharacterAnimation.START);

                                this.bSelect[1] = true;
                                TJAPlayer3.Skin.sound決定音.t再生する();
                                TJAPlayer3.Skin.voiceMenuSongDecide[TJAPlayer3.GetActualPlayer(1)]?.t再生する();

                                if (bSelect[0])
                                {
                                    TJAPlayer3.stage選曲.t曲を選択する(n現在の選択行[0] - 2, 0);
                                    TJAPlayer3.stage選曲.t曲を選択する(n現在の選択行[1] - 2, 1);
                                }
                            }
                        }
                    }
                }
            }

            #endregion

            #region [ 画像描画 ]


            // int boxType = nStrジャンルtoNum(TJAPlayer3.stage選曲.r現在選択中の曲.strジャンル);
            int boxType = TJAPlayer3.stage選曲.r現在選択中の曲.BoxType;

            if (!TJAPlayer3.stage選曲.r現在選択中の曲.isChangedBoxType)
            {
                boxType = nStrジャンルtoNum(TJAPlayer3.stage選曲.r現在選択中の曲.strジャンル);
            }


            TJAPlayer3.Tx.Difficulty_Back[boxType].Opacity =
                (TJAPlayer3.stage選曲.act曲リスト.ctDifficultyIn.n現在の値 - 1255);
            TJAPlayer3.Tx.Difficulty_Bar.Opacity = (TJAPlayer3.stage選曲.act曲リスト.ctDifficultyIn.n現在の値 - 1255);
            TJAPlayer3.Tx.Difficulty_Number.Opacity = (TJAPlayer3.stage選曲.act曲リスト.ctDifficultyIn.n現在の値 - 1255);
            TJAPlayer3.Tx.Difficulty_Crown.Opacity = (TJAPlayer3.stage選曲.act曲リスト.ctDifficultyIn.n現在の値 - 1255);
            TJAPlayer3.Tx.SongSelect_ScoreRank.vc拡大縮小倍率.X = 0.65f;
            TJAPlayer3.Tx.SongSelect_ScoreRank.vc拡大縮小倍率.Y = 0.65f;
            TJAPlayer3.Tx.SongSelect_ScoreRank.Opacity = (TJAPlayer3.stage選曲.act曲リスト.ctDifficultyIn.n現在の値 - 1255);
            TJAPlayer3.Tx.Difficulty_Star.Opacity = (TJAPlayer3.stage選曲.act曲リスト.ctDifficultyIn.n現在の値 - 1255);

            TJAPlayer3.Tx.Difficulty_Back[boxType].color4 = C変換.ColorToColor4(TJAPlayer3.stage選曲.r現在選択中の曲.BoxColor);

            TJAPlayer3.Tx.Difficulty_Back[boxType].t2D中心基準描画(TJAPlayer3.app.Device, 640, 290);

            for(int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                TJAPlayer3.Tx.Difficulty_Select_Bar[i].Opacity = (int)(ctBarAnimeIn.n現在の値 >= 80 ? (ctBarAnimeIn.n現在の値 - 80) * 2.84f : 0);
                TJAPlayer3.Tx.Difficulty_Select_Bar[i].t2D描画(TJAPlayer3.app.Device, (float)this.BarX[n現在の選択行[i]], 242, new RectangleF(0, (n現在の選択行[i] >= 2 ? 114 : 387), 259, 275 - (n現在の選択行[i] >= 2 ? 0 : 164)));
            }

            TJAPlayer3.Tx.Difficulty_Bar.color4 = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
            TJAPlayer3.Tx.Difficulty_Bar.t2D描画(TJAPlayer3.app.Device, 255, 270, new RectangleF(0, 0, 171, 236));    //閉じる、演奏オプション


            for (int i = 0; i <= (int)Difficulty.Edit; i++)
            {
                if (i == (int)Difficulty.Edit && (!uraExists || !b裏譜面))
                    break;
                else if (i == (int)Difficulty.Oni && (!omoteExists && uraExists || b裏譜面))
                    continue;

                int screenPos = Math.Min((int)Difficulty.Oni, i);
                int level = TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[i];
                bool avaliable = TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[i] >= 0;

                if (avaliable)
                    TJAPlayer3.Tx.Difficulty_Bar.color4 = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
                else
                    TJAPlayer3.Tx.Difficulty_Bar.color4 = new Color4(0.5f, 0.5f, 0.5f, 1.0f);

                TJAPlayer3.Tx.Difficulty_Bar.t2D描画(TJAPlayer3.app.Device, 255 + 171 + 143 * screenPos, 270, new RectangleF(171 + 143 * i, 0, 138, 236));

                if (!avaliable)
                    continue;


                for (int j = 0; j < TJAPlayer3.ConfigIni.nPlayerCount; j++)
                {
                    int p = TJAPlayer3.GetActualPlayer(j);

                    Cスコア.ST譜面情報 idx = TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報;

                    var GPInfo = TJAPlayer3.stage選曲.r現在選択中のスコア.GPInfo[p];

                    //TJAPlayer3.Tx.Difficulty_Crown.t2D描画(TJAPlayer3.app.Device, 445 + screenPos * 144, 284, new RectangleF(idx.nクリア[i] * 24.5f, 0, 24.5f, 26));

                    TJAPlayer3.Tx.Difficulty_Crown.t2D描画(TJAPlayer3.app.Device, 
                        445 + screenPos * 144 + 74 * j, 
                        284, 
                        new RectangleF(GPInfo.nClear[i] * 24.5f, 0, 24.5f, 26));

                    if (GPInfo.nScoreRank[i] != 0)
                        TJAPlayer3.Tx.SongSelect_ScoreRank.t2D描画(TJAPlayer3.app.Device, 
                            467 + screenPos * 144 + 24 * j, 
                            281, 
                            new RectangleF(0, (GPInfo.nScoreRank[i] - 1) * 42.71f, 50, 42.71f));

                    /*
                    if (idx.nスコアランク[i] != 0)
                        TJAPlayer3.Tx.SongSelect_ScoreRank.t2D描画(TJAPlayer3.app.Device, 467 + screenPos * 144, 281, new RectangleF(0, (idx.nスコアランク[i] - 1) * 42.71f, 50, 42.71f));
                    */
                }
                
                if (level > 0)
                    t小文字表示(TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[i].ToString(), 498 + screenPos * 143, 434.5f);

                for (int g = 0; g < 10; g++)
                {
                    if (level > g + 10)
                    {
                        TJAPlayer3.Tx.Difficulty_Star.color4 = new Color4(1f, 0.2f, 0.2f, 1.0f);
                        TJAPlayer3.Tx.Difficulty_Star?.t2D描画(TJAPlayer3.app.Device, 444 + screenPos * 143 + g * 10, 459);
                    }
                    else if (level > g)
                    {
                        TJAPlayer3.Tx.Difficulty_Star.color4 = new Color4(1f, 1f, 1f, 1.0f);
                        TJAPlayer3.Tx.Difficulty_Star?.t2D描画(TJAPlayer3.app.Device, 444 + screenPos * 143 + g * 10, 459);
                    }
                        
                }
            }

            this.txTitle.t2D中心基準描画(TJAPlayer3.app.Device, 640 + TJAPlayer3.Skin.SongSelect_Title_X, 140 + TJAPlayer3.Skin.SongSelect_Title_Y);
            this.txSubTitle.t2D中心基準描画(TJAPlayer3.app.Device, 640 + TJAPlayer3.Skin.SongSelect_Title_X, 180 + TJAPlayer3.Skin.SongSelect_Title_Y);

            #region [ バーの描画 ]

            for(int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                TJAPlayer3.Tx.Difficulty_Select_Bar[i].t2D描画(TJAPlayer3.app.Device,
                    TJAPlayer3.ConfigIni.nPlayerCount == 2 ? n現在の選択行[0] != n現在の選択行[1] ? (float)this.BarX[n現在の選択行[i]] : i == 0 ? (float)this.BarX[n現在の選択行[i]] - 25 : (float)this.BarX[n現在の選択行[i]] + 25 : (float)this.BarX[n現在の選択行[i]], 
                    126 + ((float)Math.Sin((float)(ctBarAnimeIn.n現在の値 >= 80 ? (ctBarAnimeIn.n現在の値 - 80) : 0) * (Math.PI / 180)) * 50) + (float)Math.Sin((float)ctBarAnime[i].n現在の値 * (Math.PI / 180)) * 10, 
                    new RectangleF(0, 0, 259, 114));
            }

            #endregion

            #endregion

            return 0;
        }

        // その他

        #region [ private ]
        //-----------------

        public bool[] bSelect = new bool[2];
        public bool[] bOption = new bool[2];

        private CPrivateFastFont pfTitle;
        private CPrivateFastFont pfSubTitle;
        private CTexture txTitle;
        private CTexture txSubTitle;

        private CCounter ctBarAnimeIn;
        private CCounter[] ctBarAnime = new CCounter[2];

        //0 閉じる 1 演奏オプション 2~ 難易度
		public int[] n現在の選択行;
        private int nスイッチカウント;

        private bool b裏譜面;
        //176
        private int[] BarX = new int[] { 163, 251, 367, 510, 653, 797, 797 };

        private CSound soundSelectAnnounce;

        [StructLayout(LayoutKind.Sequential)]
        private struct STレベル数字
        {
            public char ch;
            public int ptX;
        }
        private STレベル数字[] st小文字位置 = new STレベル数字[10];

        private void t小文字表示(string str, float x, float y)
        {
            foreach (char ch in str)
            {
                for (int i = 0; i < this.st小文字位置.Length; i++)
                {
                    if (this.st小文字位置[i].ch == ch)
                    {
                        Rectangle rectangle = new Rectangle(this.st小文字位置[i].ptX, 0, 18, 23);
                        if (TJAPlayer3.Tx.Difficulty_Number != null)
                        {
                            TJAPlayer3.Tx.Difficulty_Number.t2D描画(TJAPlayer3.app.Device, x, y, rectangle);
                        }
                        break;
                    }
                }
                x += 11;
            }
        }

        private bool isOnOption()
        {
            return bOption[1] || bOption[0];
        }

        public int nStrジャンルtoNum(string strジャンル)
        {
            return TJAPlayer3.stage選曲.act曲リスト.nStrジャンルtoNumBox(strジャンル);
        }
        //-----------------
        #endregion
    }
}
