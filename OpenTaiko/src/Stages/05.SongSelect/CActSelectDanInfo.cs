﻿using FDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Minimalist menu class to use for custom menus
namespace TJAPlayer3
{
    class CActSelectDanInfo : CStage
    {
        public CActSelectDanInfo()
        {
            base.IsDeActivated = true;
        }

        public override void Activate()
        {
            // On activation

            if (base.IsActivated)
                return;

            ctStep = new CCounter(0, 1000, 2, TJAPlayer3.Timer);
            ctStepFade = new CCounter(0, 255, 0.5, TJAPlayer3.Timer);

            ttkExams = new CActSelect曲リスト.TitleTextureKey[(int)Exam.Type.Total];
            for (int i = 0; i < ttkExams.Length; i++)
            {
                ttkExams[i] = new CActSelect曲リスト.TitleTextureKey(CLangManager.LangInstance.GetString(1010 + i), pfExamFont, Color.Black, Color.Transparent, 700);
            }

            base.Activate();
        }

        public override void DeActivate()
        {
            // On de-activation

            base.DeActivate();
        }

        public override void CreateManagedResource()
        {
            // Ressource allocation

            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
                pfTitleFont = new CCachedFontRenderer(TJAPlayer3.ConfigIni.FontName, TJAPlayer3.Skin.SongSelect_DanInfo_Title_Size);
            else
                pfTitleFont = new CCachedFontRenderer(CFontRenderer.DefaultFontName, TJAPlayer3.Skin.SongSelect_DanInfo_Title_Size);

            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
                pfExamFont = new CCachedFontRenderer(TJAPlayer3.ConfigIni.FontName, TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Size);
            else
                pfExamFont = new CCachedFontRenderer(CFontRenderer.DefaultFontName, TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Size);

            base.CreateManagedResource();
        }

        public override void ReleaseManagedResource()
        {
            // Ressource freeing
            TJAPlayer3.t安全にDisposeする(ref pfTitleFont);
            TJAPlayer3.t安全にDisposeする(ref pfExamFont);

            base.ReleaseManagedResource();
        }

        public override int Draw()
        {
            ctStep.Tick();
            ctStepFade.Tick();
            if (ctStep.CurrentValue == ctStep.EndValue)
            {
                ctStep = new CCounter(0, 1000, 2, TJAPlayer3.Timer);
                tNextStep();
            }

            if (TJAPlayer3.Skin.SongSelect_DanInfo_Show)
            {
                for(int i = 0; i < TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs.Count; i++)
                {
                    var dan = TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs[i];
                    int songIndex = i / 3;
                    int opacity = 255;
                    if (TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs.Count > 3)
                    {
                        if (nNowSongIndex == songIndex)
                        {
                            opacity = ctStepFade.CurrentValue;
                        }
                        else if (nPrevSongIndex == songIndex)
                        {
                            opacity = 255 - ctStepFade.CurrentValue;
                        }
                        else
                        {
                            opacity = 0;
                        }
                    }

                    int pos = i % 3;
                    CActSelect段位リスト.tDisplayDanIcon(i + 1, TJAPlayer3.Skin.SongSelect_DanInfo_Icon_X[pos], TJAPlayer3.Skin.SongSelect_DanInfo_Icon_Y[pos], opacity, TJAPlayer3.Skin.SongSelect_DanInfo_Icon_Scale, false);

                    int difficulty_cymbol_width = TJAPlayer3.Tx.Dani_Difficulty_Cymbol.szテクスチャサイズ.Width / 5;
                    int difficulty_cymbol_height = TJAPlayer3.Tx.Dani_Difficulty_Cymbol.szテクスチャサイズ.Height;

                    TJAPlayer3.Tx.Dani_Difficulty_Cymbol.Opacity = opacity;
                    TJAPlayer3.Tx.Dani_Difficulty_Cymbol.vc拡大縮小倍率.X = TJAPlayer3.Skin.SongSelect_DanInfo_Difficulty_Cymbol_Scale;
                    TJAPlayer3.Tx.Dani_Difficulty_Cymbol.vc拡大縮小倍率.Y = TJAPlayer3.Skin.SongSelect_DanInfo_Difficulty_Cymbol_Scale;
                    TJAPlayer3.Tx.Dani_Difficulty_Cymbol.t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.SongSelect_DanInfo_Difficulty_Cymbol_X[pos], TJAPlayer3.Skin.SongSelect_DanInfo_Difficulty_Cymbol_Y[pos], new Rectangle(dan.Difficulty * difficulty_cymbol_width, 0, difficulty_cymbol_width, difficulty_cymbol_height));
                    TJAPlayer3.Tx.Dani_Difficulty_Cymbol.Opacity = 255;
                    TJAPlayer3.Tx.Dani_Difficulty_Cymbol.vc拡大縮小倍率.X = 1;
                    TJAPlayer3.Tx.Dani_Difficulty_Cymbol.vc拡大縮小倍率.Y = 1;

                    TJAPlayer3.Tx.Dani_Level_Number.Opacity = opacity;
                    TJAPlayer3.stage段位選択.段位リスト.tLevelNumberDraw(TJAPlayer3.Skin.SongSelect_DanInfo_Level_Number_X[pos], TJAPlayer3.Skin.SongSelect_DanInfo_Level_Number_Y[pos], dan.Level, TJAPlayer3.Skin.SongSelect_DanInfo_Level_Number_Scale);
                    TJAPlayer3.Tx.Dani_Level_Number.Opacity = 255;

                    TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(ttkTitles[i]).Opacity = opacity;
                    TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(ttkTitles[i]).t2D描画(TJAPlayer3.Skin.SongSelect_DanInfo_Title_X[pos], TJAPlayer3.Skin.SongSelect_DanInfo_Title_Y[pos]);


                }

                for (int j = 0; j < CExamInfo.cMaxExam; j++)
                {
                    int index = j;
                    Dan_C danc0 = TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs[0].Dan_C[j];

                    if (danc0 != null)
                    {
                        TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkExams[(int)danc0.GetExamType()]).t2D中心基準描画(TJAPlayer3.Skin.SongSelect_DanInfo_Exam_X[index], TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Y[index]);
                    }

                    if (TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs[TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs.Count - 1].Dan_C[j] == null)
                    {
                        Dan_C danc = TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs[0].Dan_C[j];
                        if (danc != null)
                        {
                            TJAPlayer3.stage段位選択.段位リスト.tExamDraw(TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Value_X[0], TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Value_Y[index], danc.Value[0], danc.GetExamRange(), TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Value_Scale);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs.Count; i++)
                        {
                            Dan_C danc = TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs[i].Dan_C[j];
                            if (danc != null)
                            {
                                int opacity = 255;
                                if (TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs.Count > 3)
                                {
                                    if (nNowSongIndex == i / 3)
                                    {
                                        opacity = ctStepFade.CurrentValue;
                                    }
                                    else if (nPrevSongIndex == i / 3)
                                    {
                                        opacity = 255 - ctStepFade.CurrentValue;
                                    }
                                    else
                                    {
                                        opacity = 0;
                                    }
                                }

                                TJAPlayer3.Tx.Dani_Exam_Number.Opacity = opacity;
                                TJAPlayer3.stage段位選択.段位リスト.tExamDraw(TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Value_X[i % 3], TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Value_Y[index], danc.Value[0], danc.GetExamRange(), TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Value_Scale);
                                TJAPlayer3.Tx.Dani_Exam_Number.Opacity = 255;
                            }
                        }
                    }
                }
            }

            return 0;
        }

        public void UpdateSong()
        {
            if (TJAPlayer3.stage選曲.r現在選択中の曲 == null || TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs == null) return;

            ttkTitles = new CActSelect曲リスト.TitleTextureKey[TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs.Count];
            for (int i = 0; i < TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs.Count; i++)
            {
                var dan = TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs[i];
                ttkTitles[i] = new CActSelect曲リスト.TitleTextureKey(dan.Title, pfTitleFont, Color.Black, Color.Transparent, 700);
            }
        }

        #region [Private]
        
        private CActSelect曲リスト.TitleTextureKey[] ttkTitles;
        private CActSelect曲リスト.TitleTextureKey[] ttkExams;
        private CCachedFontRenderer pfTitleFont;
        private CCachedFontRenderer pfExamFont;

        private CCounter ctStep;
        private CCounter ctStepFade;

        private int nPrevSongIndex;
        private int nNowSongIndex;

        private void tNextStep()
        {
            nPrevSongIndex = nNowSongIndex;
            nNowSongIndex = (nNowSongIndex + 1) % (int)Math.Ceiling(TJAPlayer3.stage選曲.r現在選択中の曲.DanSongs.Count / 3.0);
            ctStepFade = new CCounter(0, 255, 1, TJAPlayer3.Timer);
        }

        #endregion
    }
}
