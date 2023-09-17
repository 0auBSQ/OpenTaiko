﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing.Text;

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
            base.IsDeActivated = true;
		}

        public void t次に移動(int player)
        {
            if (n現在の選択行[player] < 5)
            {
                ctBarAnime[player].Start(0, 180, 1, TJAPlayer3.Timer);
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
                    for (int i = 0; i < 5; i++)
                    {
                        if(!bSelect[i])
                        {
                            if (n現在の選択行[i] == 5)
                            {
                                // Extreme to Extra
                                TJAPlayer3.stage選曲.actExExtraTransAnime.BeginAnime(true);
                                n現在の選択行[i] = 6;
                            }
                            else if (n現在の選択行[i] == 6)
                            {
                                //Extra to Extreme
                                TJAPlayer3.stage選曲.actExExtraTransAnime.BeginAnime(false);
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
                ctBarAnime[player].Start(0, 180, 1, TJAPlayer3.Timer);
                nスイッチカウント = 0;
                if(n現在の選択行[player] == 6)
                    n現在の選択行[player] -= 2;
                else
                    n現在の選択行[player]--;
            }
		}

		public void t選択画面初期化()
        {
            this.txTitle = TJAPlayer3.tテクスチャの生成(pfTitle.DrawText(TJAPlayer3.stage選曲.r現在選択中の曲.strタイトル, Color.White, Color.Black, null, 30 ));
            this.txSubTitle = TJAPlayer3.tテクスチャの生成(pfSubTitle.DrawText(TJAPlayer3.stage選曲.r現在選択中の曲.strサブタイトル, Color.White, Color.Black, null, 30));
            
            this.n現在の選択行 = new int[5];
            this.bSelect[0] = false;
            this.bSelect[1] = false;
            this.bSelect[2] = false;
            this.bSelect[3] = false;
            this.bSelect[4] = false;

            this.b裏譜面 = (TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Edit] >= 0 && TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Oni] < 0);

            this.IsFirstDraw = true;
		}

		// CActivity 実装

		public override void Activate()
		{
			if( this.IsActivated )
				return;
            ctBarAnime = new CCounter[5];
            ctBarAnime[0] = new CCounter();
            ctBarAnime[1] = new CCounter();
            ctBarAnime[2] = new CCounter();
            ctBarAnime[3] = new CCounter();
            ctBarAnime[4] = new CCounter();

            base.Activate();
		}
		public override void DeActivate()
		{
			if( this.IsDeActivated )
				return;

            ctBarAnime = null;

            base.DeActivate();
		}
		public override void CreateManagedResource()
		{
            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
            {
                this.pfTitle = new CCachedFontRenderer(TJAPlayer3.ConfigIni.FontName, TJAPlayer3.Skin.SongSelect_MusicName_Scale);
                this.pfSubTitle = new CCachedFontRenderer(TJAPlayer3.ConfigIni.FontName, TJAPlayer3.Skin.SongSelect_Subtitle_Scale);
            }
            else
            {
                this.pfTitle = new CCachedFontRenderer(CFontRenderer.DefaultFontName, TJAPlayer3.Skin.SongSelect_MusicName_Scale);
                this.pfSubTitle = new CCachedFontRenderer(CFontRenderer.DefaultFontName, TJAPlayer3.Skin.SongSelect_Subtitle_Scale);
            }

            // this.soundSelectAnnounce = TJAPlayer3.Sound管理.tサウンドを生成する( CSkin.Path( @"Sounds{Path.DirectorySeparatorChar}DiffSelect.ogg" ), ESoundGroup.SoundEffect );

			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
			pfTitle.Dispose();
			pfSubTitle.Dispose();

            // TJAPlayer3.t安全にDisposeする( ref this.soundSelectAnnounce );

			base.ReleaseManagedResource();
		}
        public override int Draw()
        {
            if (this.IsDeActivated)
                return 0;

            #region [ 初めての進行描画 ]
            //-----------------
            if (this.IsFirstDraw)
            {
                ctBarAnimeIn = new CCounter(0, 170, 4, TJAPlayer3.Timer);
                // this.soundSelectAnnounce?.tサウンドを再生する();
                //TJAPlayer3.Skin.soundSelectAnnounce.t再生する();
                TJAPlayer3.Skin.voiceMenuDiffSelect[TJAPlayer3.SaveFile]?.t再生する();
                base.IsFirstDraw = false;
            }
            //-----------------
            #endregion

            ctBarAnimeIn.Tick();
            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                ctBarAnime[i].Tick();
            }

            bool uraExists = TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Edit] >= 0;
            bool omoteExists = TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Oni] >= 0;

            #region [ キー入力 ]

            if (this.ctBarAnimeIn.IsEnded && exextraAnimation == 0) // Prevent player actions if animation is active
            {
                for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
                    if (!bSelect[i] && !isOnOption())
                    {
                        bool right = false;
                        bool left = false;
                        bool decide = false;

                        bool cancel = false;

                        switch (i)
                        {
                            case 0:
                                right = (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RightChange) || TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow));
                                left = (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LeftChange) || TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow));
                                decide = (TJAPlayer3.Pad.b押されたDGB(Eパッド.Decide) || 
                                    (TJAPlayer3.ConfigIni.bEnterがキー割り当てのどこにも使用されていない && TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return)));
                                cancel = (TJAPlayer3.Pad.b押されたDGB(Eパッド.Cancel) || TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape));
                                break;
                            case 1:
                                if (!TJAPlayer3.ConfigIni.bAIBattleMode)
                                {
                                    right = (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue2P));
                                    left = (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue2P));
                                    decide = (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed2P) || TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed2P));
                                }
                                break;
                            case 2:
                                right = (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue3P));
                                left = (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue3P));
                                decide = (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed3P) || TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed3P));
                                break;
                            case 3:
                                right = (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue4P));
                                left = (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue4P));
                                decide = (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed4P) || TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed4P));
                                break;
                            case 4:
                                right = (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue5P));
                                left = (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue5P));
                                decide = (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed5P) || TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed5P));
                                break;
                        }

                        if (right)
                        {
                            TJAPlayer3.Skin.sound変更音.t再生する();
                            this.t次に移動(i);
                        }
                        else if (left)
                        {
                            TJAPlayer3.Skin.sound変更音.t再生する();
                            this.t前に移動(i);
                        }
                        if (decide)
                        {
                            if (n現在の選択行[i] == 0)
                            {
                                TJAPlayer3.Skin.sound決定音.t再生する();
                                TJAPlayer3.stage選曲.act曲リスト.ctBarOpen.Start(100, 260, 2, TJAPlayer3.Timer);
                                this.bIsDifficltSelect = false;
                            }
                            else if (n現在の選択行[i] == 1)
                            {
                                TJAPlayer3.Skin.sound決定音.t再生する();
                                bOption[i] = true;
                            }
                            else
                            {
                                if (TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[n現在の選択行[i] - 2] > 0)
                                {
                                    //TJAPlayer3.stage選曲.ctDonchan_Jump[0].t開始(0, SongSelect_Donchan_Jump.Length - 1, 1000 / 45, TJAPlayer3.Timer);
                                    

                                    this.bSelect[i] = true;

                                    bool allPlayerSelected = true;

                                    for (int i2 = 0; i2 < TJAPlayer3.ConfigIni.nPlayerCount; i2++)
                                    {
                                        if (TJAPlayer3.ConfigIni.bAIBattleMode && i2 == 1) break;

                                        if (!bSelect[i2])
                                        {
                                            allPlayerSelected = false;
                                            break;
                                        }
                                    }

                                    if (allPlayerSelected)
                                    {
                                        if (TJAPlayer3.Skin.soundSongDecide_AI.b読み込み成功 && TJAPlayer3.ConfigIni.bAIBattleMode)
                                        {
                                            TJAPlayer3.Skin.soundSongDecide_AI.t再生する();
                                        }
                                        else if (TJAPlayer3.Skin.sound曲決定音.b読み込み成功)
                                        {
                                            TJAPlayer3.Skin.sound曲決定音.t再生する();
                                        }
                                        else
                                        {
                                            TJAPlayer3.Skin.sound決定音.t再生する();
                                        }

                                        for (int i2 = 0; i2 < TJAPlayer3.ConfigIni.nPlayerCount; i2++)
                                        {
                                            if (TJAPlayer3.ConfigIni.bAIBattleMode)
                                            {
                                                TJAPlayer3.Skin.voiceMenuSongDecide_AI[TJAPlayer3.GetActualPlayer(i2)]?.t再生する();
                                            }
                                            else
                                            {
                                                TJAPlayer3.Skin.voiceMenuSongDecide[TJAPlayer3.GetActualPlayer(i2)]?.t再生する();
                                            }
                                            CMenuCharacter.tMenuResetTimer(i2, CMenuCharacter.ECharacterAnimation.START);
                                            if (TJAPlayer3.ConfigIni.bAIBattleMode)
                                            {
                                                TJAPlayer3.stage選曲.t曲を選択する(n現在の選択行[0] - 2, i2);
                                            }
                                            else
                                            {
                                                TJAPlayer3.stage選曲.t曲を選択する(n現在の選択行[i2] - 2, i2);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        TJAPlayer3.Skin.sound決定音.t再生する();
                                    }
                                }
                            }
                        }
                        if (cancel)
                        {
                            TJAPlayer3.Skin.sound決定音.t再生する();
                            TJAPlayer3.stage選曲.act曲リスト.ctBarOpen.Start(100, 260, 2, TJAPlayer3.Timer);
                            this.bIsDifficltSelect = false;
                        }
                    }
                }
            }

            #endregion

            bool consideMultiPlay = TJAPlayer3.ConfigIni.nPlayerCount >= 2 && !TJAPlayer3.ConfigIni.bAIBattleMode;

            #region [ 画像描画 ]


            // int boxType = nStrジャンルtoNum(TJAPlayer3.stage選曲.r現在選択中の曲.strジャンル);
            int boxType = TJAPlayer3.stage選曲.r現在選択中の曲.BoxType;

            if (!TJAPlayer3.stage選曲.r現在選択中の曲.isChangedBoxType)
            {
                boxType = nStrジャンルtoNum(TJAPlayer3.stage選曲.r現在選択中の曲.strジャンル);
            }


            TJAPlayer3.Tx.Difficulty_Back[boxType].Opacity =
                (TJAPlayer3.stage選曲.act曲リスト.ctDifficultyIn.CurrentValue - 1255);
            TJAPlayer3.Tx.Difficulty_Bar.Opacity = (TJAPlayer3.stage選曲.act曲リスト.ctDifficultyIn.CurrentValue - 1255);
            TJAPlayer3.Tx.Difficulty_Number.Opacity = (TJAPlayer3.stage選曲.act曲リスト.ctDifficultyIn.CurrentValue - 1255);
            TJAPlayer3.Tx.Difficulty_Crown.Opacity = (TJAPlayer3.stage選曲.act曲リスト.ctDifficultyIn.CurrentValue - 1255);
            TJAPlayer3.Tx.SongSelect_ScoreRank.vc拡大縮小倍率.X = 0.65f;
            TJAPlayer3.Tx.SongSelect_ScoreRank.vc拡大縮小倍率.Y = 0.65f;
            TJAPlayer3.Tx.SongSelect_ScoreRank.Opacity = (TJAPlayer3.stage選曲.act曲リスト.ctDifficultyIn.CurrentValue - 1255);
            TJAPlayer3.Tx.Difficulty_Star.Opacity = (TJAPlayer3.stage選曲.act曲リスト.ctDifficultyIn.CurrentValue - 1255);

            TJAPlayer3.Tx.Difficulty_Back[boxType].color4 = CConversion.ColorToColor4(TJAPlayer3.stage選曲.r現在選択中の曲.BoxColor);

            TJAPlayer3.Tx.Difficulty_Back[boxType].t2D中心基準描画(TJAPlayer3.Skin.SongSelect_Difficulty_Back[0], TJAPlayer3.Skin.SongSelect_Difficulty_Back[1]);

            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                if (TJAPlayer3.ConfigIni.bAIBattleMode && i == 1) break;
                
                /*
                Difficulty_Select_Bar[i].Opacity = (int)(ctBarAnimeIn.n現在の値 >= 80 ? (ctBarAnimeIn.n現在の値 - 80) * 2.84f : 0);
                Difficulty_Select_Bar[i].t2D描画((float)this.BarX[n現在の選択行[i]], 242, new RectangleF(0, (n現在の選択行[i] >= 2 ? 114 : 387), 259, 275 - (n現在の選択行[i] >= 2 ? 0 : 164)));
                */
                TJAPlayer3.Tx.Difficulty_Select_Bar[i].Opacity = (int)(ctBarAnimeIn.CurrentValue >= 80 ? (ctBarAnimeIn.CurrentValue - 80) * 2.84f : 0);

                int backType = n現在の選択行[i] >= 2 ? 1 : 2;

                TJAPlayer3.Tx.Difficulty_Select_Bar[i].t2D描画(
                    TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Back_X[n現在の選択行[i]],
                    TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Back_Y[n現在の選択行[i]],
                    new RectangleF(TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Rect[backType][0], TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Rect[backType][1],
                    TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Rect[backType][2], TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Rect[backType][3]));
            }

            TJAPlayer3.Tx.Difficulty_Bar.color4 = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
            //Difficulty_Bar.t2D描画(255, 270, new RectangleF(0, 0, 171, 236));    //閉じる、演奏オプション
            for (int i = 0; i < 2; i++)
            {
                TJAPlayer3.Tx.Difficulty_Bar.t2D描画(TJAPlayer3.Skin.SongSelect_Difficulty_Bar_X[i], TJAPlayer3.Skin.SongSelect_Difficulty_Bar_Y[i],
                    new RectangleF(TJAPlayer3.Skin.SongSelect_Difficulty_Bar_Rect[i][0], 
                    TJAPlayer3.Skin.SongSelect_Difficulty_Bar_Rect[i][1],
                    TJAPlayer3.Skin.SongSelect_Difficulty_Bar_Rect[i][2],
                    TJAPlayer3.Skin.SongSelect_Difficulty_Bar_Rect[i][3]));    //閉じる
            }

            exextraAnimation = TJAPlayer3.stage選曲.actExExtraTransAnime.Draw();

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

                if (!(i >= (int)Difficulty.Oni && exextraAnimation > 0)) // Hide Oni/Ura during transition
                {
                    TJAPlayer3.Tx.Difficulty_Bar.t2D描画(TJAPlayer3.Skin.SongSelect_Difficulty_Bar_X[i + 2], TJAPlayer3.Skin.SongSelect_Difficulty_Bar_Y[i + 2],
                        new RectangleF(TJAPlayer3.Skin.SongSelect_Difficulty_Bar_Rect[i + 2][0],
                        TJAPlayer3.Skin.SongSelect_Difficulty_Bar_Rect[i + 2][1],
                        TJAPlayer3.Skin.SongSelect_Difficulty_Bar_Rect[i + 2][2],
                        TJAPlayer3.Skin.SongSelect_Difficulty_Bar_Rect[i + 2][3]));
                }

                if (!avaliable)
                    continue;


                for (int j = 0; j < TJAPlayer3.ConfigIni.nPlayerCount; j++)
                {
                    if (j >= 2) continue;

                    if (TJAPlayer3.ConfigIni.bAIBattleMode && i == 1) break;

                    int p = TJAPlayer3.GetActualPlayer(j);

                    Cスコア.ST譜面情報 idx = TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報;

                    var GPInfo = TJAPlayer3.stage選曲.r現在選択中のスコア.GPInfo[p];

                    //Difficulty_Crown.t2D描画(445 + screenPos * 144, 284, new RectangleF(idx.nクリア[i] * 24.5f, 0, 24.5f, 26));

                    int crown_width = TJAPlayer3.Tx.Difficulty_Crown.szテクスチャサイズ.Width / 4;
                    int crown_height = TJAPlayer3.Tx.Difficulty_Crown.szテクスチャサイズ.Height;
                    TJAPlayer3.Tx.Difficulty_Crown.t2D描画(
                        TJAPlayer3.Skin.SongSelect_Difficulty_Crown_X[j][i], 
                        TJAPlayer3.Skin.SongSelect_Difficulty_Crown_Y[j][i], 
                        new RectangleF(GPInfo.nClear[i] * crown_width, 0, crown_width, crown_height));

                    int scoreRank_width = TJAPlayer3.Tx.SongSelect_ScoreRank.szテクスチャサイズ.Width;
                    int scoreRank_height = TJAPlayer3.Tx.SongSelect_ScoreRank.szテクスチャサイズ.Height / 7;

                    if (GPInfo.nScoreRank[i] != 0)
                        TJAPlayer3.Tx.SongSelect_ScoreRank.t2D描画(
                            TJAPlayer3.Skin.SongSelect_Difficulty_ScoreRank_X[j][i],
                            TJAPlayer3.Skin.SongSelect_Difficulty_ScoreRank_Y[j][i],
                            new RectangleF(0, (GPInfo.nScoreRank[i] - 1) * scoreRank_height, scoreRank_width, scoreRank_height));

                    /*
                    if (idx.nスコアランク[i] != 0)
                        SongSelect_ScoreRank.t2D描画(467 + screenPos * 144, 281, new RectangleF(0, (idx.nスコアランク[i] - 1) * 42.71f, 50, 42.71f));
                    */
                }
                
                if (level >= 0 && (!(i >= (int)Difficulty.Oni && exextraAnimation > 0)))
                    t小文字表示(TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[i], 
                        TJAPlayer3.Skin.SongSelect_Difficulty_Number_X[i], 
                        TJAPlayer3.Skin.SongSelect_Difficulty_Number_Y[i],
                        i,
                        TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nLevelIcon[i]
                        );

                if (!(i >= (int)Difficulty.Oni && exextraAnimation > 0))
                {
                    for (int g = 0; g < 10; g++)
                    {
                        if (level > g + 10)
                        {
                            TJAPlayer3.Tx.Difficulty_Star.color4 = new Color4(1f, 0.2f, 0.2f, 1.0f);
                            TJAPlayer3.Tx.Difficulty_Star?.t2D描画(TJAPlayer3.Skin.SongSelect_Difficulty_Star_X[i] + g * TJAPlayer3.Skin.SongSelect_Difficulty_Star_Interval[0], TJAPlayer3.Skin.SongSelect_Difficulty_Star_Y[i] + g * TJAPlayer3.Skin.SongSelect_Difficulty_Star_Interval[1]);
                        }
                        else if (level > g)
                        {
                            TJAPlayer3.Tx.Difficulty_Star.color4 = new Color4(1f, 1f, 1f, 1.0f);
                            TJAPlayer3.Tx.Difficulty_Star?.t2D描画(TJAPlayer3.Skin.SongSelect_Difficulty_Star_X[i] + g * TJAPlayer3.Skin.SongSelect_Difficulty_Star_Interval[0], TJAPlayer3.Skin.SongSelect_Difficulty_Star_Y[i] + g * TJAPlayer3.Skin.SongSelect_Difficulty_Star_Interval[1]);
                        }

                    }
                }

                if (TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.b譜面分岐[i])
                    TJAPlayer3.Tx.SongSelect_Branch_Text?.t2D描画(
                        
                        TJAPlayer3.Skin.SongSelect_Difficulty_Bar_X[i + 2] + TJAPlayer3.Skin.SongSelect_Branch_Text_Offset[0], 
                        TJAPlayer3.Skin.SongSelect_Difficulty_Bar_Y[i + 2] + TJAPlayer3.Skin.SongSelect_Branch_Text_Offset[1]
                    );
            }

            this.txTitle.t2D中心基準描画(TJAPlayer3.Skin.SongSelect_Difficulty_Select_Title[0], TJAPlayer3.Skin.SongSelect_Difficulty_Select_Title[1]);
            this.txSubTitle.t2D中心基準描画(TJAPlayer3.Skin.SongSelect_Difficulty_Select_SubTitle[0], TJAPlayer3.Skin.SongSelect_Difficulty_Select_SubTitle[1]);

            #region [ バーの描画 ]

            for(int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                if (TJAPlayer3.ConfigIni.bAIBattleMode && i == 1) break;

                /*
                Difficulty_Select_Bar[i].t2D描画(
                    TJAPlayer3.ConfigIni.nPlayerCount == 2 ? n現在の選択行[0] != n現在の選択行[1] ? (float)this.BarX[n現在の選択行[i]] : i == 0 ? (float)this.BarX[n現在の選択行[i]] - 25 : (float)this.BarX[n現在の選択行[i]] + 25 : (float)this.BarX[n現在の選択行[i]], 
                    126 + ((float)Math.Sin((float)(ctBarAnimeIn.n現在の値 >= 80 ? (ctBarAnimeIn.n現在の値 - 80) : 0) * (Math.PI / 180)) * 50) + (float)Math.Sin((float)ctBarAnime[i].n現在の値 * (Math.PI / 180)) * 10, 
                    new RectangleF(0, 0, 259, 114));
                */

                //bool overlap = n現在の選択行[0] == n現在の選択行[1] && consideMultiPlay;

                int overlapCount = 0;
                int nowOverlapIndex = 0;

                for (int j = 0; j < TJAPlayer3.ConfigIni.nPlayerCount; j++)
                {
                    if (n現在の選択行[i] == n現在の選択行[j] && j != i) overlapCount++;
                    if (j == i)
                    {
                        nowOverlapIndex = overlapCount;
                    }
                }

                /*float moveX = overlap ? (i == 0 ? -TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Move[0] : TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Move[0]) : 0;
                moveX += (((float)Math.Sin((float)ctBarAnime[i].n現在の値 * (Math.PI / 180)) * TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Anime[0]) -
                    (((float)Math.Cos((float)ctBarAnimeIn.n現在の値 * (Math.PI / 170)) + 1.0f) * TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_AnimeIn[0]));

                float moveY = overlap ? (i == 0 ? -TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Move[1] : TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Move[1]) : 0;
                moveY += (((float)Math.Sin((float)ctBarAnime[i].n現在の値 * (Math.PI / 180)) * TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Anime[1]) -
                    (((float)Math.Cos((float)ctBarAnimeIn.n現在の値 * (Math.PI / 170)) + 1.0f) * TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Anime[1]));
                */

                float moveX = TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Move[0] * (nowOverlapIndex - (overlapCount / 2.0f)) * (2.0f / Math.Max(overlapCount, 1));
                float moveY = TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Move[1] * (nowOverlapIndex - (overlapCount / 2.0f)) * (2.0f / Math.Max(overlapCount, 1));

                if (!consideMultiPlay)
                {
                    moveX = 0;
                    moveY = 0;
                }

                moveX += (((float)Math.Sin((float)ctBarAnime[i].CurrentValue * (Math.PI / 180)) * TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Anime[0]) -
                    (((float)Math.Cos((float)ctBarAnimeIn.CurrentValue * (Math.PI / 170)) + 1.0f) * TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_AnimeIn[0]));
                moveY += (((float)Math.Sin((float)ctBarAnime[i].CurrentValue * (Math.PI / 180)) * TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Anime[1]) -
                    (((float)Math.Cos((float)ctBarAnimeIn.CurrentValue * (Math.PI / 170)) + 1.0f) * TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Anime[1]));



                TJAPlayer3.Tx.Difficulty_Select_Bar[i].t2D描画(
                    TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_X[n現在の選択行[i]] + moveX,
                    TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Y[n現在の選択行[i]] + moveY,
                    new RectangleF(TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Rect[0][0], TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Rect[0][1], 
                    TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Rect[0][2], TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Rect[0][3]));
            }

            #endregion

            #endregion

            return 0;
        }

        // その他

        #region [ private ]
        //-----------------

        public bool[] bSelect = new bool[5];
        public bool[] bOption = new bool[5];

        private CCachedFontRenderer pfTitle;
        private CCachedFontRenderer pfSubTitle;
        private CTexture txTitle;
        private CTexture txSubTitle;

        private CCounter ctBarAnimeIn;
        private CCounter[] ctBarAnime = new CCounter[2];

        private int exextraAnimation;

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

        private void t小文字表示(int num, float x, float y, int diff, CDTX.ELevelIcon icon)
        {
            int[] nums = CConversion.SeparateDigits(num);
            float[] icon_coords = new float[2] { -999, -999 };
            for (int j = 0; j < nums.Length; j++)
            {
                float offset = j - (nums.Length / 2.0f);
                float _x = x - (TJAPlayer3.Skin.SongSelect_Difficulty_Number_Interval[0] * offset);
                float _y = y - (TJAPlayer3.Skin.SongSelect_Difficulty_Number_Interval[1] * offset);

                int width = TJAPlayer3.Tx.Difficulty_Number.sz画像サイズ.Width / 10;
                int height = TJAPlayer3.Tx.Difficulty_Number.sz画像サイズ.Height;

                icon_coords[0] = Math.Max(icon_coords[0], _x + width);
                icon_coords[1] = _y;

                TJAPlayer3.Tx.Difficulty_Number.t2D描画(_x, _y, new Rectangle(width * nums[j], 0, width, height));

                if (TJAPlayer3.Tx.Difficulty_Number_Colored != null)
                {
                    TJAPlayer3.Tx.Difficulty_Number_Colored.color4 = CConversion.ColorToColor4(TJAPlayer3.Skin.SongSelect_Difficulty_Colors[diff]);
                    TJAPlayer3.Tx.Difficulty_Number_Colored.t2D描画(_x, _y, new RectangleF(width * nums[j], 0, width, height));
                }
            }
            TJAPlayer3.stage選曲.act曲リスト.tDisplayLevelIcon((int)icon_coords[0], (int)icon_coords[1], icon, TJAPlayer3.Tx.Difficulty_Number_Icon);
        }

        private bool isOnOption()
        {
            return bOption[0] || bOption[1] || bOption[2] || bOption[3] || bOption[4];
        }

        public int nStrジャンルtoNum(string strジャンル)
        {
            return TJAPlayer3.stage選曲.act曲リスト.nStrジャンルtoNumBox(strジャンル);
        }
        //-----------------
        #endregion
    }
}
