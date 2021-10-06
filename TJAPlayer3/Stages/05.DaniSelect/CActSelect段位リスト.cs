using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;
using static TJAPlayer3.CActSelect曲リスト;

namespace TJAPlayer3
{
    class CActSelect段位リスト : CStage
    {
        public CActSelect段位リスト()
        {
            for(int i = 0; i < 10; i++)
            {
                stLevel[i].ch = i.ToString().ToCharArray()[0];
                stLevel[i].pt = new Point(i * 14, 0);

                stSoulNumber[i].ch = i.ToString().ToCharArray()[0];
                stSoulNumber[i].pt = new Point(i * 23, 0);

                stExamNumber[i].ch = i.ToString().ToCharArray()[0];
                stExamNumber[i].pt = new Point(i * 19, 0);
            }
        }

        public bool bスクロール中
        {
            get
            {
                return ctDaniMoveAnime.b進行中;
            }
        }

        public override void On活性化()
        {
            if (this.b活性化してる)
                return;

            DaniInAnime = false;

            ctDaniMoveAnime = new CCounter();
            ctDanAnimeIn = new CCounter();
            ctDaniIn = new CCounter(0, 3000, 1, TJAPlayer3.Timer);

            stバー情報 = new STバー情報[TJAPlayer3.Songs管理.list曲ルート_Dan.Count];

            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
                pfDanSong = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 24);
            else
                pfDanSong = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 16);
            
            //一応チェックしておく。
            if(TJAPlayer3.Songs管理.list曲ルート_Dan.Count > 0)
                this.tバーの初期化();

            base.On活性化();
        }

        public override void On非活性化()
        {
            base.On非活性化();
        }

        public override void OnManagedリソースの作成()
        {
            base.OnManagedリソースの作成();
        }

        public override void OnManagedリソースの解放()
        {
            base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {
            ctDaniMoveAnime.t進行();
            ctDaniIn.t進行();
            ctDanAnimeIn.t進行();

            if (ctDaniIn.n現在の値 == 3000)
            {
                if(!DaniInAnime)
                {
                    ctDanAnimeIn.t開始(0, 90, 2f, TJAPlayer3.Timer);
                    DaniInAnime = true;
                }
            }

            #region [ バー表示 ]

            if (stバー情報.Length != 0)
            {
                TJAPlayer3.Tx.DanC_ExamType.vc拡大縮小倍率.X = 0.81f;
                TJAPlayer3.Tx.DanC_ExamType.vc拡大縮小倍率.Y = 0.81f;

                float Anime = ctDanAnimeIn.n現在の値 == 90 ? bLeftMove ? (float)Math.Sin(ctDaniMoveAnime.n現在の値 * (Math.PI / 180)) * 1280 : -((float)Math.Sin(ctDaniMoveAnime.n現在の値 * (Math.PI / 180)) * 1280) : 1280 - (float)Math.Sin(ctDanAnimeIn.n現在の値 * (Math.PI / 180)) * 1280;

                #region [ 中央バーの表示 ]

                if (stバー情報[n現在の選択行].txBarCenter != null) stバー情報[n現在の選択行].txBarCenter?.t2D描画(TJAPlayer3.app.Device, Anime, 0);
                else TJAPlayer3.Tx.Dani_Bar_Center.t2D描画(TJAPlayer3.app.Device, Anime, 0);
                if (stバー情報[n現在の選択行].txDanPlate != null) stバー情報[n現在の選択行].txDanPlate.Opacity = 255;
                stバー情報[n現在の選択行].txDanPlate?.t2D中心基準描画(TJAPlayer3.app.Device, 173 + Anime, 301);

                #region [Goukaku plate]

                int currentRank = Math.Min(stバー情報[n現在の選択行].clearGrade, 6) - 1;

                if (currentRank >= 0)
                {
                    TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.X = 0.8f;
                    TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.Y = 0.8f;
                    TJAPlayer3.Tx.DanResult_Rank.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 173 + Anime, 422, new Rectangle(334 * currentRank, 0, 334, 334));
                }
                
                #endregion

                if (stバー情報[n現在の選択行].List_DanSongs[0].Dan_C[0] != null)
                    tSoulDraw(370 + Anime, 462, stバー情報[n現在の選択行].List_DanSongs[0].Dan_C[0].Value[0].ToString());

                for (int i = 0; i < stバー情報[n現在の選択行].ttkタイトル.Length; i++)
                    TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(stバー情報[n現在の選択行].ttkタイトル[i]).t2D描画(TJAPlayer3.app.Device, 401 + Anime, 173 + i * 73);
                for (int i = 0; i < stバー情報[n現在の選択行].n曲難易度.Length; i++)
                    TJAPlayer3.Tx.Dani_Difficulty_Cymbol.t2D中心基準描画(TJAPlayer3.app.Device, 377 + Anime, 180 + i * 73, new Rectangle(stバー情報[n現在の選択行].n曲難易度[i] * 53, 0, 53, 53));
                for (int i = 0; i < stバー情報[n現在の選択行].n曲レベル.Length; i++)
                    this.tLevelNumberDraw(383 + Anime, 207 + i * 73, stバー情報[n現在の選択行].n曲レベル[i].ToString());

                for (int j = 1; j < 4; j++)  //段位条件のループ(魂ゲージを除く) 縦(y)
                {
                    if (stバー情報[n現在の選択行].List_DanSongs[0].Dan_C[j] != null)
                    {
                        TJAPlayer3.Tx.DanC_ExamType?.t2D描画(TJAPlayer3.app.Device, 515 + Anime, 412 + (j - 1) * 88, new Rectangle(0, TJAPlayer3.Skin.Game_DanC_ExamType_Size[1] * (int)stバー情報[n現在の選択行].List_DanSongs[0].Dan_C[j].GetExamType(), TJAPlayer3.Skin.Game_DanC_ExamType_Size[0], TJAPlayer3.Skin.Game_DanC_ExamType_Size[1]));
                    }

                    for (int i = 0; i < stバー情報[n現在の選択行].List_DanSongs.Count; i++)  //曲ごとのループ(魂ゲージを除く) 横(x)
                    {
                        if (stバー情報[n現在の選択行].List_DanSongs[i].Dan_C[j] != null)
                        {
                            if (stバー情報[n現在の選択行].List_DanSongs[stバー情報[n現在の選択行].List_DanSongs.Count - 1].Dan_C[j] != null) //個別の条件がありますよー
                            {
                                tExamDraw(590 + Anime + i * 220, 455 + (j - 1) * 88, stバー情報[n現在の選択行].List_DanSongs[i].Dan_C[j].Value[0].ToString(), stバー情報[n現在の選択行].List_DanSongs[i].Dan_C[j].GetExamRange());
                            }
                            else    //全体の条件ですよー
                            {
                                tExamDraw(590 + Anime, 455 + (j - 1) * 88, stバー情報[n現在の選択行].List_DanSongs[0].Dan_C[j].Value[0].ToString(), stバー情報[n現在の選択行].List_DanSongs[0].Dan_C[j].GetExamRange());
                            }
                        }
                    }
                }

                #endregion

                if (bLeftMove && n現在の選択行 - 1 >= 0)
                {
                    #region [ 左バーの表示 ]

                    if (stバー情報[n現在の選択行 - 1].txBarCenter != null) stバー情報[n現在の選択行 - 1].txBarCenter?.t2D描画(TJAPlayer3.app.Device, -1280 + Anime, 0);
                    else TJAPlayer3.Tx.Dani_Bar_Center.t2D描画(TJAPlayer3.app.Device, -1280 + Anime, 0);
                    
                    if (stバー情報[n現在の選択行 - 1].txDanPlate != null) stバー情報[n現在の選択行 - 1].txDanPlate.Opacity = 255;
                    stバー情報[n現在の選択行 - 1].txDanPlate?.t2D中心基準描画(TJAPlayer3.app.Device, -1280 + 173 + Anime, 301);

                    #region [Goukaku plate]

                    currentRank = Math.Min(stバー情報[n現在の選択行 - 1].clearGrade, 6) - 1;

                    if (currentRank >= 0)
                    {
                        TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.X = 0.8f;
                        TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.Y = 0.8f;
                        TJAPlayer3.Tx.DanResult_Rank.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, -1280 + 173 + Anime, 422, new Rectangle(334 * currentRank, 0, 334, 334));
                    }

                    #endregion

                    if (stバー情報[n現在の選択行 - 1].List_DanSongs[0].Dan_C[0] != null)
                        tSoulDraw(-1280 + 370 + Anime, 462, stバー情報[n現在の選択行 - 1].List_DanSongs[0].Dan_C[0].Value[0].ToString());

                    for (int i = 0; i < stバー情報[n現在の選択行 - 1].ttkタイトル.Length; i++)
                        TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(stバー情報[n現在の選択行 - 1].ttkタイトル[i]).t2D描画(TJAPlayer3.app.Device, -879 + Anime, 173 + i * 73);
                    for (int i = 0; i < stバー情報[n現在の選択行 - 1].n曲難易度.Length; i++)
                        TJAPlayer3.Tx.Dani_Difficulty_Cymbol.t2D中心基準描画(TJAPlayer3.app.Device, -1280 + 377 + +Anime, 180 + i * 73, new Rectangle(stバー情報[n現在の選択行 - 1].n曲難易度[i] * 53, 0, 53, 53));
                    for (int i = 0; i < stバー情報[n現在の選択行 - 1].n曲レベル.Length; i++)
                        this.tLevelNumberDraw(-1280 + 383 + Anime, 207 + i * 73, stバー情報[n現在の選択行 - 1].n曲レベル[i].ToString());

                    for (int j = 1; j < 4; j++)  //段位条件のループ(魂ゲージを除く) 縦(y)
                    {
                        if (stバー情報[n現在の選択行 - 1].List_DanSongs[0].Dan_C[j] != null)
                        {
                            TJAPlayer3.Tx.DanC_ExamType?.t2D描画(TJAPlayer3.app.Device, -1280 + 515 + Anime, 412 + (j - 1) * 88, new Rectangle(0, TJAPlayer3.Skin.Game_DanC_ExamType_Size[1] * (int)stバー情報[n現在の選択行 - 1].List_DanSongs[0].Dan_C[j].GetExamType(), TJAPlayer3.Skin.Game_DanC_ExamType_Size[0], TJAPlayer3.Skin.Game_DanC_ExamType_Size[1]));
                        }

                        for (int i = 0; i < stバー情報[n現在の選択行 - 1].List_DanSongs.Count; i++)  //曲ごとのループ(魂ゲージを除く) 横(x)
                        {
                            if (stバー情報[n現在の選択行 - 1].List_DanSongs[i].Dan_C[j] != null)
                            {
                                if (stバー情報[n現在の選択行 - 1].List_DanSongs[stバー情報[n現在の選択行 - 1].List_DanSongs.Count - 1].Dan_C[j] != null) //個別の条件がありますよー
                                {
                                    tExamDraw(-1280 + 590 + Anime + i * 220, 455 + (j - 1) * 88, stバー情報[n現在の選択行 - 1].List_DanSongs[i].Dan_C[j].Value[0].ToString(), stバー情報[n現在の選択行 - 1].List_DanSongs[i].Dan_C[j].GetExamRange());
                                }
                                else    //全体の条件ですよー
                                {
                                    tExamDraw(-1280 + 590 + Anime, 455 + (j - 1) * 88, stバー情報[n現在の選択行 - 1].List_DanSongs[0].Dan_C[j].Value[0].ToString(), stバー情報[n現在の選択行 - 1].List_DanSongs[0].Dan_C[j].GetExamRange());
                                }
                            }
                        }
                    }

                    #endregion
                }
                if (!bLeftMove && n現在の選択行 + 1 <= stバー情報.Length - 1)
                {
                    #region [ 右バーの表示 ]

                    if (stバー情報[n現在の選択行 + 1].txBarCenter != null) stバー情報[n現在の選択行 + 1].txBarCenter?.t2D描画(TJAPlayer3.app.Device, 1280 + Anime, 0);
                    else TJAPlayer3.Tx.Dani_Bar_Center.t2D描画(TJAPlayer3.app.Device, 1280 + Anime, 0);

                    if(stバー情報[n現在の選択行 + 1].txDanPlate != null) stバー情報[n現在の選択行 + 1].txDanPlate.Opacity = 255;
                    stバー情報[n現在の選択行 + 1].txDanPlate?.t2D中心基準描画(TJAPlayer3.app.Device, 1280 + 173 + Anime, 301);

                    #region [Goukaku plate]

                    currentRank = Math.Min(stバー情報[n現在の選択行 + 1].clearGrade, 6) - 1;

                    if (currentRank >= 0)
                    {
                        TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.X = 0.8f;
                        TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.Y = 0.8f;
                        TJAPlayer3.Tx.DanResult_Rank.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 1280 + 173 + Anime, 422, new Rectangle(334 * currentRank, 0, 334, 334));
                    }

                    #endregion

                    if (stバー情報[n現在の選択行 + 1].List_DanSongs[0].Dan_C[0] != null)
                        tSoulDraw(1280 + 370 + Anime, 462, stバー情報[n現在の選択行 + 1].List_DanSongs[0].Dan_C[0].Value[0].ToString());

                    for (int i = 0; i < stバー情報[n現在の選択行 + 1].ttkタイトル.Length; i++)
                        TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(stバー情報[n現在の選択行 + 1].ttkタイトル[i]).t2D描画(TJAPlayer3.app.Device, 1280 + 401 + Anime, 173 + i * 73);
                    for (int i = 0; i < stバー情報[n現在の選択行 + 1].n曲難易度.Length; i++)
                        TJAPlayer3.Tx.Dani_Difficulty_Cymbol.t2D中心基準描画(TJAPlayer3.app.Device, 1280 + 377 + Anime, 180 + i * 73, new Rectangle(stバー情報[n現在の選択行 + 1].n曲難易度[i] * 53, 0, 53, 53));
                    for (int i = 0; i < stバー情報[n現在の選択行 + 1].n曲レベル.Length; i++)
                        this.tLevelNumberDraw(1280 + 383 + Anime, 207 + i * 73, stバー情報[n現在の選択行 + 1].n曲レベル[i].ToString());

                    for (int j = 1; j < 4; j++)  //段位条件のループ(魂ゲージを除く) 縦(y)
                    {
                        if (stバー情報[n現在の選択行 + 1].List_DanSongs[0].Dan_C[j] != null)
                        {
                            TJAPlayer3.Tx.DanC_ExamType?.t2D描画(TJAPlayer3.app.Device, 1280 + 515 + Anime, 412 + (j - 1) * 88, new Rectangle(0, TJAPlayer3.Skin.Game_DanC_ExamType_Size[1] * (int)stバー情報[n現在の選択行 + 1].List_DanSongs[0].Dan_C[j].GetExamType(), TJAPlayer3.Skin.Game_DanC_ExamType_Size[0], TJAPlayer3.Skin.Game_DanC_ExamType_Size[1]));
                        }

                        for (int i = 0; i < stバー情報[n現在の選択行 + 1].List_DanSongs.Count; i++)  //曲ごとのループ(魂ゲージを除く) 横(x)
                        {
                            if (stバー情報[n現在の選択行 + 1].List_DanSongs[i].Dan_C[j] != null)
                            {
                                if (stバー情報[n現在の選択行 + 1].List_DanSongs[stバー情報[n現在の選択行 + 1].List_DanSongs.Count - 1].Dan_C[j] != null) //個別の条件がありますよー
                                {
                                    tExamDraw(1280 + 590 + Anime + i * 220, 455 + (j - 1) * 88, stバー情報[n現在の選択行 + 1].List_DanSongs[i].Dan_C[j].Value[0].ToString(), stバー情報[n現在の選択行 + 1].List_DanSongs[i].Dan_C[j].GetExamRange());
                                }
                                else    //全体の条件ですよー
                                {
                                    tExamDraw(1280 + 590 + Anime, 455 + (j - 1) * 88, stバー情報[n現在の選択行 + 1].List_DanSongs[0].Dan_C[j].Value[0].ToString(), stバー情報[n現在の選択行 + 1].List_DanSongs[0].Dan_C[j].GetExamRange());
                                }
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion

            #region [ バー移動 ]

            if (ctDaniMoveAnime.n現在の値 == 90)
            {
                if (bLeftMove)
                {
                    this.n現在の選択行 -= n現在の選択行 - 1 >= 0 ? 1 : 0;
                }
                else
                {
                    this.n現在の選択行 += n現在の選択行 + 1 < this.stバー情報.Length ? 1 : 0;
                }
                ctDaniMoveAnime.t停止();
                ctDaniMoveAnime.n現在の値 = 0;
            }

            #endregion

            return 0;
        }

        private bool DaniInAnime;
        public CCounter ctDaniIn;

        private CCounter ctDanAnimeIn;

        private CCounter ctDaniMoveAnime;
        public int n現在の選択行;

        private bool bLeftMove;

        private CPrivateFastFont pfDanSong;

        private CStage選曲.STNumber[] stLevel = new CStage選曲.STNumber[10];
        private CStage選曲.STNumber[] stSoulNumber = new CStage選曲.STNumber[10];
        private CStage選曲.STNumber[] stExamNumber = new CStage選曲.STNumber[10];
        
        public STバー情報[] stバー情報;

        public struct STバー情報
        {
            public TitleTextureKey[] ttkタイトル;
            public int[] n曲難易度;
            public int[] n曲レベル;
            public List<CDTX.DanSongs> List_DanSongs;
            public CTexture txBarCenter;
            public CTexture txDanPlate;
            public int clearGrade;
        }

        private void tバーの初期化()
        {
            for(int i = 0; i < stバー情報.Length; i++)
            {
                var song = TJAPlayer3.Songs管理.list曲ルート_Dan[i];

                stバー情報[i].ttkタイトル = new TitleTextureKey[TJAPlayer3.Songs管理.list曲ルート_Dan[i].DanSongs.Count];
                stバー情報[i].n曲難易度 = new int[TJAPlayer3.Songs管理.list曲ルート_Dan[i].DanSongs.Count];
                stバー情報[i].n曲レベル = new int[TJAPlayer3.Songs管理.list曲ルート_Dan[i].DanSongs.Count];
                for (int j = 0; j < TJAPlayer3.Songs管理.list曲ルート_Dan[i].DanSongs.Count; j++)
                {
                    stバー情報[i].ttkタイトル[j] = new TitleTextureKey(song.DanSongs[j].bTitleShow ? "???" : song.DanSongs[j].Title, pfDanSong, Color.White, Color.Black, 700);
                    stバー情報[i].n曲難易度[j] = song.DanSongs[j].Difficulty;
                    stバー情報[i].n曲レベル[j] = song.DanSongs[j].Level;
                    stバー情報[i].List_DanSongs = song.DanSongs;
                }
                stバー情報[i].clearGrade = song.arスコア[6].譜面情報.nクリア[0];
                stバー情報[i].txBarCenter = TJAPlayer3.tテクスチャの生成(Path.GetDirectoryName(song.arスコア[6].ファイル情報.ファイルの絶対パス) + @"\Bar_Center.png");
                stバー情報[i].txDanPlate = TJAPlayer3.tテクスチャの生成(Path.GetDirectoryName(song.arスコア[6].ファイル情報.ファイルの絶対パス) + @"\Dan_Plate.png");
            }
        }

        public void t右に移動()
        {
            if(n現在の選択行 < stバー情報.Length - 1)
            {
                TJAPlayer3.Skin.sound変更音.t再生する();
                this.bLeftMove = false;
                this.ctDaniMoveAnime.t開始(0, 90, 2f, TJAPlayer3.Timer);
            }
        }

        public void t左に移動()
        {
            if (n現在の選択行 > 0)
            {
                TJAPlayer3.Skin.sound変更音.t再生する();
                this.bLeftMove = true;
                this.ctDaniMoveAnime.t開始(0, 90, 2f, TJAPlayer3.Timer);
            }
        }

        public void tLevelNumberDraw(float x, float y, string str)
        {
            for (int j = 0; j < str.Length; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (str[j] == stLevel[i].ch)
                    {
                        TJAPlayer3.Tx.Dani_Level_Number.t2D描画(TJAPlayer3.app.Device, x - (str.Length * 14 + 10 * str.Length - str.Length * 14) / 2 + 14 / 2, (float)y - 18 / 2, new RectangleF(stLevel[i].pt.X, stLevel[i].pt.Y, 14, 18));
                        x += 10;
                    }
                }
            }
        }

        public void tSoulDraw(float x, float y, string str)
        {
            TJAPlayer3.Tx.Dani_Soul_Number.t2D描画(TJAPlayer3.app.Device, x + 16 * str.Length, y - 30 / 2, new RectangleF(0, 30, 80, 30));

            for (int j = 0; j < str.Length; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (str[j] == stSoulNumber[i].ch)
                    {
                        TJAPlayer3.Tx.Dani_Soul_Number.t2D描画(TJAPlayer3.app.Device, x - (str.Length * 23 + 18 * str.Length - str.Length * 23) / 2 + 23 / 2, (float)y - 30 / 2, new RectangleF(stSoulNumber[i].pt.X, stSoulNumber[i].pt.Y, 23, 30));
                        x += 16;
                    }
                }
            }
        }

        public void tExamDraw(float x, float y, string str, Exam.Range Range)
        {
            TJAPlayer3.Tx.Dani_Exam_Number.t2D描画(TJAPlayer3.app.Device, x + 19 * str.Length, y - 24 / 2, new RectangleF(45 * (int)Range, 24, 45, 24));

            for (int j = 0; j < str.Length; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (str[j] == stExamNumber[i].ch)
                    {
                        TJAPlayer3.Tx.Dani_Exam_Number.t2D描画(TJAPlayer3.app.Device, x, (float)y - 24 / 2, new RectangleF(stExamNumber[i].pt.X, stExamNumber[i].pt.Y, 19, 24));
                        x += 16;
                    }
                }
            }
        }
    }
}
