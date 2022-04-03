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
            ctDaniIn = new CCounter(0, 6000, 1, TJAPlayer3.Timer);

            ctDanTick = new CCounter(0, 510, 3, TJAPlayer3.Timer);

            ctExamConditionsAnim = new CCounter(0, 4000, 1, TJAPlayer3.Timer);

            stバー情報 = new STバー情報[TJAPlayer3.Songs管理.list曲ルート_Dan.Count];

            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
                pfDanSong = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 24);
            else
                pfDanSong = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 16);

            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
                this.pfExamFont = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 13);
            else
                this.pfExamFont = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 13);

            this.ttkExams = new TitleTextureKey[(int)Exam.Type.Accuracy + 1];
            for (int i = 0; i < this.ttkExams.Length; i++)
            {
                this.ttkExams[i] = new TitleTextureKey(CLangManager.LangInstance.GetString(1010 + i), this.pfExamFont, Color.White, Color.SaddleBrown, 1000);
            }


            //一応チェックしておく。
            if (TJAPlayer3.Songs管理.list曲ルート_Dan.Count > 0)
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
            ctDanTick.t進行Loop();

            ctExamConditionsAnim.t進行Loop();

            if (ctDaniIn.n現在の値 == 6000)
            {
                if(!DaniInAnime)
                {
                    ctDanAnimeIn.t開始(0, 90, 2f, TJAPlayer3.Timer);
                    DaniInAnime = true;
                }
            }

            #region [ バー表示 ]

            if (stバー情報.Length != 0 && ctDaniIn.n現在の値 == 6000)
            {
                TJAPlayer3.Tx.DanC_ExamType.vc拡大縮小倍率.X = 0.81f;
                TJAPlayer3.Tx.DanC_ExamType.vc拡大縮小倍率.Y = 0.81f;

                float Anime = ctDanAnimeIn.n現在の値 == 90 ? bLeftMove ? (float)Math.Sin(ctDaniMoveAnime.n現在の値 * (Math.PI / 180)) * 1280 : -((float)Math.Sin(ctDaniMoveAnime.n現在の値 * (Math.PI / 180)) * 1280) : 1280 - (float)Math.Sin(ctDanAnimeIn.n現在の値 * (Math.PI / 180)) * 1280;

                tDrawDanSelectedLevel(Anime);

                if (bLeftMove && n現在の選択行 - 1 >= 0)
                    tDrawDanSelectedLevel(Anime, -1);
                if (!bLeftMove && n現在の選択行 + 1 <= stバー情報.Length - 1)
                    tDrawDanSelectedLevel(Anime, 1);
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

            // To do : Display the 27 (max) bars one by one
            if (ctDaniIn.n現在の値 < 5000)
                return 0;

            #region [Upper plates]

            // stバー情報[n現在の選択行]

            int tickWidth = TJAPlayer3.Tx.Dani_Plate.szテクスチャサイズ.Width / 7;
            int tickHeight = TJAPlayer3.Tx.Dani_Plate.szテクスチャサイズ.Height;

            for (int idx = -13; idx < 14; idx++)
            {

                if (ctDaniIn.n現在の値 < 5000 + (idx + 13) * 33)
                    break;

                int currentSong = n現在の選択行 + idx;

                if (currentSong < 0)
                    continue;
                if (currentSong >= stバー情報.Length)
                    break;

                int xPos = 640 + idx * (tickWidth - 4);
                int yPos = idx == 0 ? 25 : 10;



                #region [Plate background]

                int tick = Math.Max(0, Math.Min(5, stバー情報[currentSong].nDanTick));
                Color tickColor = stバー情報[currentSong].cDanTickColor;

                TJAPlayer3.Tx.Dani_Plate.Opacity = 255;
                TJAPlayer3.Tx.Dani_Plate.color4 = C変換.ColorToColor4(tickColor);
                TJAPlayer3.Tx.Dani_Plate.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, xPos, yPos, new Rectangle(tickWidth * tick, 0, tickWidth, tickHeight));

                // Reset color for plate flash
                TJAPlayer3.Tx.Dani_Plate.color4 = C変換.ColorToColor4(Color.White);

                #endregion

                #region [Dan grade title]

                TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTextureTate(stバー情報[currentSong].ttkタイトル[stバー情報[currentSong].ttkタイトル.Length - 1])
                    .t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, xPos + 2, yPos + 36);

                #endregion


                #region [Plate flash]

                if (idx == 0)
                {
                    TJAPlayer3.Tx.Dani_Plate.Opacity = Math.Abs(255 - ctDanTick.n現在の値);
                    TJAPlayer3.Tx.Dani_Plate.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, xPos, yPos, new Rectangle(tickWidth * 6, 0, tickWidth, tickHeight));
                }

                #endregion

                #region [Goukaku plate]

                int currentRank = Math.Min(stバー情報[currentSong].clearGrade, 6) - 1;

                if (currentRank >= 0)
                {
                    TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.X = 0.20f;
                    TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.Y = 0.20f;
                    TJAPlayer3.Tx.DanResult_Rank.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, xPos - 2, yPos - 14, new Rectangle(334 * (currentRank + 1), 0, 334, 334));
                }

                #endregion
            }

            #endregion

            return 0;
        }

        #region [private]

        private CCounter ctExamConditionsAnim;

        private bool DaniInAnime;
        public CCounter ctDaniIn;

        private CCounter ctDanAnimeIn;

        private CCounter ctDanTick;

        private CCounter ctDaniMoveAnime;
        public int n現在の選択行;

        private bool bLeftMove;

        private CPrivateFastFont pfDanSong;

        public CPrivateFastFont pfExamFont;
        public TitleTextureKey[] ttkExams;

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

            // Extra parameters
            public int clearGrade;
            public int nDanTick;
            public Color cDanTickColor;
        }

        static CPrivateFastFont pfDanPlateTitle = null;

        public static void tDisplayDanPlate(CTexture givenPlate, STバー情報? songNode, int x, int y)
        {
            if (givenPlate != null)
            {
                givenPlate.Opacity = 255;
                givenPlate.t2D中心基準描画(TJAPlayer3.app.Device, x, y);
            }
            else
            {
                // Default Dan Plate

                int danTick = 0;
                Color danTickColor = Color.White;

                if (TJAPlayer3.stage選曲.r確定されたスコア != null)
                {
                    danTick = TJAPlayer3.stage選曲.r確定されたスコア.譜面情報.nDanTick;
                    danTickColor = TJAPlayer3.stage選曲.r確定されたスコア.譜面情報.cDanTickColor;
                }
                if (songNode != null)
                {
                    STバー情報 stNode = (STバー情報)songNode;

                    danTick = stNode.nDanTick;
                    danTickColor = stNode.cDanTickColor;
                }
                

                int unit = TJAPlayer3.Tx.Dani_DanPlates.szテクスチャサイズ.Width / 6;

                if (TJAPlayer3.Tx.Dani_DanPlates != null)
                {
                    TJAPlayer3.Tx.Dani_DanPlates.Opacity = 255;
                    TJAPlayer3.Tx.Dani_DanPlates.color4 = C変換.ColorToColor4(danTickColor);
                }
                TJAPlayer3.Tx.Dani_DanPlates?.t2D中心基準描画(TJAPlayer3.app.Device, x, y, new Rectangle(
                    unit * danTick,
                    0,
                    unit,
                    TJAPlayer3.Tx.Dani_DanPlates.szテクスチャサイズ.Height
                ));

                if (pfDanPlateTitle == null)
                    pfDanPlateTitle = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 60);

                string titleTmp = "";

                if (TJAPlayer3.stage選曲.r確定されたスコア != null)
                    titleTmp = TJAPlayer3.stage選曲.r確定された曲.strタイトル;
                if (songNode != null)
                {
                    STバー情報 stNode = (STバー情報)songNode;

                    titleTmp = stNode.ttkタイトル[stNode.ttkタイトル.Length - 1].str文字;
                }

                TitleTextureKey ttkTmp = new TitleTextureKey(titleTmp.Substring(0, 2), pfDanPlateTitle, Color.White, Color.Black, 1000);
                TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTextureTate(ttkTmp).t2D中心基準描画(TJAPlayer3.app.Device, x, y - 50);
            }
        }

        private void tDrawDanSelectedLevel(float Anime, int modifier = 0)
        {
            int scroll = 1280 * modifier;
            int currentSong = n現在の選択行 + modifier;
            bool over4 = false;

            #region [Center bar and Dan plate]

            int danTick = stバー情報[currentSong].nDanTick;
            Color danTickColor = stバー情報[currentSong].cDanTickColor;

            // Use the given bar center if provided, else use a default one
            if (stバー情報[currentSong].txBarCenter != null)
            {
                stバー情報[currentSong].txBarCenter.t2D描画(TJAPlayer3.app.Device, scroll + Anime, 0);
            }
            else
            {
                int unit = TJAPlayer3.Tx.Dani_DanSides.szテクスチャサイズ.Width / 6;
                TJAPlayer3.Tx.Dani_DanSides.color4 = C変換.ColorToColor4(danTickColor);

                TJAPlayer3.Tx.Dani_Bar_Center.t2D描画(TJAPlayer3.app.Device, scroll + Anime, 0);

                // Bar sides
                TJAPlayer3.Tx.Dani_DanSides.t2D描画(TJAPlayer3.app.Device, (int)(scroll + Anime + 243), 143, new Rectangle(
                    unit * danTick,
                    0,
                    unit,
                    TJAPlayer3.Tx.Dani_DanSides.szテクスチャサイズ.Height
                ));

                TJAPlayer3.Tx.Dani_DanSides.t2D左右反転描画(TJAPlayer3.app.Device, (int)(scroll + Anime + 1199), 143, new Rectangle(
                    unit * danTick,
                    0,
                    unit,
                    TJAPlayer3.Tx.Dani_DanSides.szテクスチャサイズ.Height
                ));
            }

            CActSelect段位リスト.tDisplayDanPlate(stバー情報[currentSong].txDanPlate, stバー情報[currentSong], (int)(scroll + 173 + Anime), 301);

            #endregion

            #region [Goukaku plate]

            int currentRank = Math.Min(stバー情報[currentSong].clearGrade, 6) - 1;

            if (currentRank >= 0)
            {
                TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.X = 0.8f;
                TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.Y = 0.8f;
                TJAPlayer3.Tx.DanResult_Rank.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, scroll + 173 + Anime, 422, new Rectangle(334 * (currentRank + 1), 0, 334, 334));
            }

            #endregion

            #region [Soul gauge condition]

            TJAPlayer3.Tx.Dani_Bloc[2]?.t2D描画(TJAPlayer3.app.Device, scroll + 291 + Anime, 412);

            if (stバー情報[currentSong].List_DanSongs[0].Dan_C[0] != null)
                tSoulDraw(scroll + 370 + Anime, 462, stバー情報[currentSong].List_DanSongs[0].Dan_C[0].Value[0].ToString());

            //TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkExams[0]).t2D下中央基準描画(TJAPlayer3.app.Device, (int)(scroll + 396 + Anime), 452);
            TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkExams[0]).t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, (int)(scroll + 396 + Anime), 440);

            #endregion

            #region [Song information]

            for (int i = 0; i < stバー情報[currentSong].ttkタイトル.Length - 1; i++)
                TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(stバー情報[currentSong].ttkタイトル[i]).t2D描画(TJAPlayer3.app.Device, scroll + 401 + Anime, 173 + i * 73);
            for (int i = 0; i < stバー情報[currentSong].n曲難易度.Length; i++)
                TJAPlayer3.Tx.Dani_Difficulty_Cymbol.t2D中心基準描画(TJAPlayer3.app.Device, scroll + 377 + Anime, 180 + i * 73, new Rectangle(stバー情報[currentSong].n曲難易度[i] * 53, 0, 53, 53));
            for (int i = 0; i < stバー情報[currentSong].n曲レベル.Length; i++)
                this.tLevelNumberDraw(scroll + 383 + Anime, 207 + i * 73, stバー情報[currentSong].n曲レベル[i].ToString());

            #endregion

            #region [Check if one of the EXAM5,6,7 slots are used]

            for (int j = 4; j < CExamInfo.cMaxExam; j++)
            {
                if (stバー情報[currentSong].List_DanSongs[0].Dan_C[j] != null)
                {
                    over4 = true;
                    break;
                }
            }

            #endregion

            #region [Display dan conditions]

            for (int j = 1; j < CExamInfo.cMaxExam; j++)  //段位条件のループ(魂ゲージを除く) 縦(y)
            {
                // Inner index within the exam 3-set
                int index = (j - 1) % 3;

                #region [Alter opacity if multi-screen exam display]

                int opacity = 255;

                if (over4 == true)
                {
                    int half = (j - 1) / 3;

                    int animJauge = ctExamConditionsAnim.n現在の値;

                    if (half == 0)
                    {
                        if (animJauge > 3745)
                            opacity = animJauge - 3745;
                        else if (animJauge > 1745)
                            opacity = 2000 - animJauge;
                    }
                    else
                    {
                        if (animJauge > 3745)
                            opacity = 4000 - animJauge;
                        else if (animJauge > 1745)
                            opacity = animJauge - 1745;
                        else
                            opacity = 0;
                    }
                }

                #endregion

                #region [Exam value (individual included)]

                for (int i = 0; i < stバー情報[currentSong].List_DanSongs.Count; i++)  //曲ごとのループ(魂ゲージを除く) 横(x)
                {
                    if (stバー情報[currentSong].List_DanSongs[i].Dan_C[j] != null)
                    {
                        TJAPlayer3.Tx.Dani_Exam_Number.Opacity = opacity;

                        if (stバー情報[currentSong].List_DanSongs[stバー情報[currentSong].List_DanSongs.Count - 1].Dan_C[j] != null) {
                            //個別の条件がありますよー

                            if (i == 0)
                            {
                                if (TJAPlayer3.Tx.Dani_Bloc[1] != null)
                                    TJAPlayer3.Tx.Dani_Bloc[1].Opacity = opacity;
                                TJAPlayer3.Tx.Dani_Bloc[1]?.t2D描画(TJAPlayer3.app.Device, 
                                    scroll + Anime + 515, 
                                    412 + 88 * index);
                            }

                            tExamDraw(scroll + 590 + Anime + i * 220, 455 + index * 88, stバー情報[currentSong].List_DanSongs[i].Dan_C[j].Value[0].ToString(), stバー情報[currentSong].List_DanSongs[i].Dan_C[j].GetExamRange());
                        } 
                        else    
                        {
                            //全体の条件ですよー

                            if (i == 0)
                            {
                                if (TJAPlayer3.Tx.Dani_Bloc[0] != null)
                                    TJAPlayer3.Tx.Dani_Bloc[0].Opacity = opacity;
                                TJAPlayer3.Tx.Dani_Bloc[0]?.t2D描画(TJAPlayer3.app.Device,
                                    scroll + Anime + 515,
                                    412 + 88 * index);
                            }

                            tExamDraw(scroll + 590 + Anime, 455 + index * 88, stバー情報[currentSong].List_DanSongs[0].Dan_C[j].Value[0].ToString(), stバー情報[currentSong].List_DanSongs[0].Dan_C[j].GetExamRange());

                        }

                        TJAPlayer3.Tx.Dani_Exam_Number.Opacity = 255;
                    }
                }

                #endregion

                #region [Exam title]

                if (stバー情報[currentSong].List_DanSongs[0].Dan_C[j] != null)
                {
                    CTexture tmpTex = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkExams[(int)stバー情報[currentSong].List_DanSongs[0].Dan_C[j].GetExamType()]);

                    tmpTex.Opacity = opacity;
                    //tmpTex.t2D下中央基準描画(TJAPlayer3.app.Device, (int)(scroll + 614 + Anime), 452 + index * 88);

                    tmpTex.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, (int)(scroll + 614 + Anime), 440 + index * 88);
                }

                #endregion
            }

            #endregion

        }

        private void tバーの初期化()
        {
            for (int i = 0; i < stバー情報.Length; i++)
            {
                var song = TJAPlayer3.Songs管理.list曲ルート_Dan[i];

                stバー情報[i].ttkタイトル = new TitleTextureKey[TJAPlayer3.Songs管理.list曲ルート_Dan[i].DanSongs.Count + 1];
                stバー情報[i].n曲難易度 = new int[TJAPlayer3.Songs管理.list曲ルート_Dan[i].DanSongs.Count];
                stバー情報[i].n曲レベル = new int[TJAPlayer3.Songs管理.list曲ルート_Dan[i].DanSongs.Count];
                for (int j = 0; j < TJAPlayer3.Songs管理.list曲ルート_Dan[i].DanSongs.Count; j++)
                {
                    stバー情報[i].ttkタイトル[j] = new TitleTextureKey(song.DanSongs[j].bTitleShow ? "???" : song.DanSongs[j].Title, pfDanSong, Color.White, Color.Black, 700);
                    stバー情報[i].n曲難易度[j] = song.DanSongs[j].Difficulty;
                    stバー情報[i].n曲レベル[j] = song.DanSongs[j].Level;
                    stバー情報[i].List_DanSongs = song.DanSongs;
                }

                // Two char header, will be used for grade unlocking too
                string tmp = song.strタイトル.Substring(0, 2);

                stバー情報[i].ttkタイトル[TJAPlayer3.Songs管理.list曲ルート_Dan[i].DanSongs.Count] = new TitleTextureKey(tmp, pfDanSong, Color.Black, Color.Transparent, 700);

                stバー情報[i].nDanTick = song.arスコア[6].譜面情報.nDanTick;
                stバー情報[i].cDanTickColor = song.arスコア[6].譜面情報.cDanTickColor;

                //stバー情報[i].clearGrade = song.arスコア[6].譜面情報.nクリア[0];
                stバー情報[i].clearGrade = song.arスコア[6].GPInfo[TJAPlayer3.SaveFile].nClear[0];

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

        #endregion

    }

}
