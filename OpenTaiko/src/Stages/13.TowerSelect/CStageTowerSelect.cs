using FDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Minimalist menu class to use for custom menus
namespace TJAPlayer3
{
    class CStageTowerSelect : CStage
    {
        public CStageTowerSelect()
        {
            base.eStageID = EStage.TaikoTowers;
            base.ePhaseID = CStage.EPhase.Common_NORMAL;

            // Load CActivity objects here
            // base.list子Activities.Add(this.act = new CAct());

            base.ChildActivities.Add(this.actFOtoNowLoading = new CActFIFOStart());
            base.ChildActivities.Add(this.actFOtoTitle = new CActFIFOBlack());

        }

        public override void Activate()
        {
            // On activation

            if (base.IsActivated)
                return;

            base.ePhaseID = CStage.EPhase.Common_NORMAL;
            this.eフェードアウト完了時の戻り値 = EReturnValue.Continuation;

            if (listSongs == null)
                listSongs = TJAPlayer3.Songs管理.list曲ルート_Tower;

            tUpdateBarInfos();

            Background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.TOWERSELECT}Script.lua"));
            Background.Init();

            base.Activate();
        }

        public override void DeActivate()
        {
            // On de-activation

            TJAPlayer3.t安全にDisposeする(ref Background);

            base.DeActivate();
        }

        public override void CreateManagedResource()
        {
            // Ressource allocation

            pfTitleFont = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.TowerSelect_Title_Size);
            pfSubTitleFont = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.TowerSelect_SubTitle_Size);

            base.CreateManagedResource();
        }

        public override void ReleaseManagedResource()
        {
            // Ressource freeing

            pfTitleFont.Dispose();
            pfSubTitleFont.Dispose();

            base.ReleaseManagedResource();
        }

        public override int Draw()
        {
            Background.Update();
            Background.Draw();

            for(int i = 0; i < TJAPlayer3.Skin.TowerSelect_Bar_Count; i++)
            {
                int currentSong = nCurrentSongIndex + i - ((TJAPlayer3.Skin.TowerSelect_Bar_Count - 1) / 2);
                if (currentSong < 0 || currentSong >= BarInfos.Length || currentSong >= TJAPlayer3.Skin.TowerSelect_Bar_Count) continue;
                var bar = BarInfos[currentSong];

                int x = TJAPlayer3.Skin.TowerSelect_Bar_X[i];
                int y = TJAPlayer3.Skin.TowerSelect_Bar_Y[i];
                tDrawTower(x, y, bar);
            }

            #region [Input]

            if (this.eフェードアウト完了時の戻り値 == EReturnValue.Continuation)
            {
                int returnTitle()
                {
                    TJAPlayer3.Skin.sound取消音.t再生する();
                    this.eフェードアウト完了時の戻り値 = EReturnValue.ReturnToTitle;
                    this.actFOtoTitle.tフェードアウト開始();
                    base.ePhaseID = CStage.EPhase.Common_FADEOUT;
                    return 0;
                }

                if (TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RightChange))
                {
                    TJAPlayer3.Skin.sound変更音.t再生する();

                    if (nCurrentSongIndex < BarInfos.Length - 1)
                    {
                        nCurrentSongIndex++;
                    }
                }

                else if (TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LeftChange))
                {
                    TJAPlayer3.Skin.sound変更音.t再生する();

                    if (nCurrentSongIndex > 0)
                    {
                        nCurrentSongIndex--;
                    }
                }

                else if (TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape) ||
                TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.Cancel))
                {

                    #region [Fast return (Escape)]

                    TJAPlayer3.Skin.sound取消音.t再生する();
                    returnTitle();

                    #endregion
                }

                else if (TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.Decide))
                {
                    #region [Decide]

                    TJAPlayer3.Skin.sound決定音.t再生する();

                    switch(currentSong.eノード種別)
                    {
                        case C曲リストノード.Eノード種別.SCORE:
                            tSelectSong();
                            break;
                        case C曲リストノード.Eノード種別.RANDOM:
                            tSelectSongRandomly();
                            break;
                        case C曲リストノード.Eノード種別.BOX:
                            tOpenFolder(currentSong);
                            break;
                        case C曲リストノード.Eノード種別.BACKBOX:
                            {
                                if (TJAPlayer3.Songs管理.list曲ルート.Contains(currentSong.r親ノード) && currentSong.r親ノード.strジャンル == "太鼓タワー")
                                {
                                    returnTitle();
                                }
                                else
                                {
                                    tCloseFolder(currentSong);
                                }
                            }
                            break;
                    }

                    #endregion
                }
            }

            #endregion



            // Menu exit fade out transition
            switch (base.ePhaseID)
            {
                case CStage.EPhase.SongSelect_FadeOutToNowLoading:
                    if (this.actFOtoNowLoading.Draw() == 0)
                    {
                        break;
                    }
                    return (int)this.eフェードアウト完了時の戻り値;
                case CStage.EPhase.Common_FADEOUT:
                    if (this.actFOtoTitle.Draw() == 0)
                    {
                        break;
                    }
                    return (int)this.eフェードアウト完了時の戻り値;

            }

            return 0;
        }

        #region [Private]

        private class BarInfo
        {
            public string strTitle;
            public string strSubTitle;
            public C曲リストノード.Eノード種別 eノード種別;
            public CActSelect曲リスト.TitleTextureKey ttkTitle;
            public CActSelect曲リスト.TitleTextureKey ttkSubTitle;
        }

        public void tSelectSong()
        {
            TJAPlayer3.stage選曲.r確定された曲 = listSongs[nCurrentSongIndex];
            TJAPlayer3.stage選曲.r確定されたスコア = listSongs[nCurrentSongIndex].arスコア[(int)Difficulty.Tower];
            TJAPlayer3.stage選曲.n確定された曲の難易度[0] = (int)Difficulty.Tower;
            TJAPlayer3.stage選曲.str確定された曲のジャンル = listSongs[nCurrentSongIndex].strジャンル;
            if ((TJAPlayer3.stage選曲.r確定された曲 != null) && (TJAPlayer3.stage選曲.r確定されたスコア != null))
            {
                this.eフェードアウト完了時の戻り値 = EReturnValue.SongChoosen;
                this.actFOtoNowLoading.tフェードアウト開始();                // #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
                base.ePhaseID = CStage.EPhase.SongSelect_FadeOutToNowLoading;
            }
            // TJAPlayer3.Skin.bgm選曲画面.t停止する();
            CSongSelectSongManager.stopSong();
        }

        private bool tSelectSongRandomly()
        {
            var mandatoryDiffs = new List<int>();
            C曲リストノード song = currentSong;

            song.stackランダム演奏番号.Clear();
            song.listランダム用ノードリスト = null;

            if ((song.stackランダム演奏番号.Count == 0) || (song.listランダム用ノードリスト == null))
            {
                if (song.listランダム用ノードリスト == null)
                {
                    List<C曲リストノード> songs = new List<C曲リストノード>();
                    TJAPlayer3.stage選曲.t指定された曲の子リストの曲を列挙する_孫リスト含む(song.r親ノード, ref songs, ref mandatoryDiffs, true, Difficulty.Tower);
                    song.listランダム用ノードリスト = songs;
                }
                int count = song.listランダム用ノードリスト.Count;
                if (count == 0)
                {
                    return false;
                }
                int[] numArray = new int[count];
                for (int i = 0; i < count; i++)
                {
                    numArray[i] = i;
                }
                for (int j = 0; j < (count * 1.5); j++)
                {
                    int index = TJAPlayer3.Random.Next(count);
                    int num5 = TJAPlayer3.Random.Next(count);
                    int num6 = numArray[num5];
                    numArray[num5] = numArray[index];
                    numArray[index] = num6;
                }
                for (int k = 0; k < count; k++)
                {
                    song.stackランダム演奏番号.Push(numArray[k]);
                }

                if (TJAPlayer3.ConfigIni.bLogDTX詳細ログ出力)
                {
                    StringBuilder builder = new StringBuilder(0x400);
                    builder.Append(string.Format("ランダムインデックスリストを作成しました: {0}曲: ", song.stackランダム演奏番号.Count));
                    for (int m = 0; m < count; m++)
                    {
                        builder.Append(string.Format("{0} ", numArray[m]));
                    }
                    Trace.TraceInformation(builder.ToString());
                }
            }

            // Third assignment
            TJAPlayer3.stage選曲.r確定された曲 = song.listランダム用ノードリスト[song.stackランダム演奏番号.Pop()];
            TJAPlayer3.stage選曲.n確定された曲の難易度[0] = (int)Difficulty.Tower;

            TJAPlayer3.stage選曲.r確定されたスコア = TJAPlayer3.stage選曲.r確定された曲.arスコア[TJAPlayer3.stage選曲.act曲リスト.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(TJAPlayer3.stage選曲.r確定された曲)];
            TJAPlayer3.stage選曲.str確定された曲のジャンル = TJAPlayer3.stage選曲.r確定された曲.strジャンル;

            //TJAPlayer3.Skin.sound曲決定音.t再生する();

            this.eフェードアウト完了時の戻り値 = EReturnValue.SongChoosen;
            this.actFOtoNowLoading.tフェードアウト開始();                    // #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
            base.ePhaseID = CStage.EPhase.SongSelect_FadeOutToNowLoading;

            #region [Log]

            if (TJAPlayer3.ConfigIni.bLogDTX詳細ログ出力)
            {
                int[] numArray2 = song.stackランダム演奏番号.ToArray();
                StringBuilder builder2 = new StringBuilder(0x400);
                builder2.Append("ランダムインデックスリスト残り: ");
                if (numArray2.Length > 0)
                {
                    for (int n = 0; n < numArray2.Length; n++)
                    {
                        builder2.Append(string.Format("{0} ", numArray2[n]));
                    }
                }
                else
                {
                    builder2.Append("(なし)");
                }
                Trace.TraceInformation(builder2.ToString());
            }

            #endregion

            CSongSelectSongManager.stopSong();

            return true;
        }

        private void tDrawTower(int x, int y, BarInfo barInfo)
        {
            switch(barInfo.eノード種別)
            {
                case C曲リストノード.Eノード種別.SCORE:
                    TJAPlayer3.Tx.TowerSelect_Tower.t2D中心基準描画(x, y);
                    break;
                case C曲リストノード.Eノード種別.RANDOM:
                    TJAPlayer3.Tx.TowerSelect_Tower.t2D中心基準描画(x, y);
                    break;
                case C曲リストノード.Eノード種別.BOX:
                    TJAPlayer3.Tx.TowerSelect_Tower.t2D中心基準描画(x, y);
                    break;
                case C曲リストノード.Eノード種別.BACKBOX:
                    TJAPlayer3.Tx.TowerSelect_Tower.t2D中心基準描画(x, y);
                    break;
            }

            TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(barInfo.ttkTitle).t2D拡大率考慮中央基準描画(x + TJAPlayer3.Skin.TowerSelect_Title_Offset[0], y + TJAPlayer3.Skin.TowerSelect_Title_Offset[1]);
            TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(barInfo.ttkSubTitle).t2D拡大率考慮中央基準描画(x + TJAPlayer3.Skin.TowerSelect_SubTitle_Offset[0], y + TJAPlayer3.Skin.TowerSelect_SubTitle_Offset[1]);
        }

        private void tUpdateBarInfos()
        {
            BarInfos = new BarInfo[listSongs.Count];
            tSetBarInfos();
        }

        private void tOpenFolder(C曲リストノード song)
        {
            nCurrentSongIndex = 0;
            listSongs = song.list子リスト;
            tUpdateBarInfos();
        }

        private void tCloseFolder(C曲リストノード song)
        {
            nCurrentSongIndex = 0;
            listSongs = song.r親ノード.r親ノード.list子リスト;
            tUpdateBarInfos();
        }

        private void tSetBarInfos()
        {
            for(int i = 0; i < BarInfos.Length; i++)
            {
                BarInfos[i] = new BarInfo();
                BarInfo bar = BarInfos[i];
                C曲リストノード song = listSongs[i];

                bar.strTitle = song.strタイトル;
                bar.strSubTitle = song.strサブタイトル;
                bar.eノード種別 = song.eノード種別;

                bar.ttkTitle = new CActSelect曲リスト.TitleTextureKey(bar.strTitle, pfTitleFont, Color.Black, Color.Transparent, TJAPlayer3.Skin.TowerSelect_Title_MaxWidth);
                bar.ttkSubTitle = new CActSelect曲リスト.TitleTextureKey(bar.strSubTitle, pfTitleFont, Color.Black, Color.Transparent, TJAPlayer3.Skin.TowerSelect_SubTitle_MaxWidth);
            }
        }

        private BarInfo[] BarInfos;
        private List<C曲リストノード> listSongs;

        private ScriptBG Background;

        private CCachedFontRenderer pfTitleFont;
        private CCachedFontRenderer pfSubTitleFont;

        public EReturnValue eフェードアウト完了時の戻り値;
        public CActFIFOStart actFOtoNowLoading;
        public CActFIFOBlack actFOtoTitle;

        private int nCurrentSongIndex;
        private C曲リストノード currentSong
        {
            get
            {
                return listSongs[nCurrentSongIndex];
            }
        }

        #endregion
    }
}
