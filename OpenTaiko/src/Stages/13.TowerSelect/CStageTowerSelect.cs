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

            TJAPlayer3.tDisposeSafely(ref Background);

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

            pfTitleFont?.Dispose();
            pfSubTitleFont?.Dispose();

            base.ReleaseManagedResource();
        }

        public override int Draw()
        {
            Background.Update();
            Background.Draw();

            for(int i = 0; i < TJAPlayer3.Skin.TowerSelect_Bar_Count; i++)
            {
                int currentSong = nCurrentSongIndex + i - ((TJAPlayer3.Skin.TowerSelect_Bar_Count - 1) / 2);
                if (currentSong < 0 || currentSong >= BarInfos.Length) continue;
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
                    TJAPlayer3.Skin.soundCancelSFX.tPlay();
                    this.eフェードアウト完了時の戻り値 = EReturnValue.ReturnToTitle;
                    this.actFOtoTitle.tフェードアウト開始();
                    base.ePhaseID = CStage.EPhase.Common_FADEOUT;
                    return 0;
                }

                if (TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow) ||
                    TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RightChange))
                {
                    TJAPlayer3.Skin.soundChangeSFX.tPlay();

                    if (nCurrentSongIndex < BarInfos.Length - 1)
                    {
                        nCurrentSongIndex++;
                    }
                }

                else if (TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow) ||
                    TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LeftChange))
                {
                    TJAPlayer3.Skin.soundChangeSFX.tPlay();

                    if (nCurrentSongIndex > 0)
                    {
                        nCurrentSongIndex--;
                    }
                }

                else if (TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape) ||
                TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.Cancel))
                {

                    #region [Fast return (Escape)]

                    TJAPlayer3.Skin.soundCancelSFX.tPlay();
                    returnTitle();

                    #endregion
                }

                else if (TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return) ||
                    TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.Decide))
                {
                    #region [Decide]

                    TJAPlayer3.Skin.soundDecideSFX.tPlay();

                    switch(currentSong.eノード種別)
                    {
                        case CSongListNode.ENodeType.SCORE:
                            tSelectSong();
                            break;
                        case CSongListNode.ENodeType.RANDOM:
                            tSelectSongRandomly();
                            break;
                        case CSongListNode.ENodeType.BOX:
                            tOpenFolder(currentSong);
                            break;
                        case CSongListNode.ENodeType.BACKBOX:
                            {
                                if (TJAPlayer3.Songs管理.list曲ルート.Contains(currentSong.rParentNode) && currentSong.rParentNode.strジャンル == "太鼓タワー")
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
            public CSongListNode.ENodeType eノード種別;
            public CActSelect曲リスト.TitleTextureKey ttkTitle;
            public CActSelect曲リスト.TitleTextureKey ttkSubTitle;
        }

        public void tSelectSong()
        {
            TJAPlayer3.ConfigIni.bTokkunMode = false;
            TJAPlayer3.stageSongSelect.rChoosenSong = listSongs[nCurrentSongIndex];
            TJAPlayer3.stageSongSelect.r確定されたスコア = listSongs[nCurrentSongIndex].arスコア[(int)Difficulty.Tower];
            TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] = (int)Difficulty.Tower;
            TJAPlayer3.stageSongSelect.str確定された曲のジャンル = listSongs[nCurrentSongIndex].strジャンル;
            if ((TJAPlayer3.stageSongSelect.rChoosenSong != null) && (TJAPlayer3.stageSongSelect.r確定されたスコア != null))
            {
                CFloorManagement.reinitialize(TJAPlayer3.stageSongSelect.rChoosenSong.arスコア[(int)Difficulty.Tower].譜面情報.nLife);
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
            CSongListNode song = currentSong;

            List<CSongListNode> songs = new List<CSongListNode>();
            TJAPlayer3.stageSongSelect.t指定された曲の子リストの曲を列挙する_孫リスト含む(song.rParentNode, ref songs, ref mandatoryDiffs, true, Difficulty.Tower);
            song.listランダム用ノードリスト = songs;

            int selectableSongCount = song.listランダム用ノードリスト.Count;

            if (selectableSongCount == 0)
            {
                return false;
            }

            int randomSongIndex = TJAPlayer3.Random.Next(selectableSongCount);

            if (TJAPlayer3.ConfigIni.bLogDTX詳細ログ出力)
            {
                StringBuilder builder = new StringBuilder(0x400);
                builder.Append(string.Format("Total number of songs to randomly choose from {0}. Randomly selected index {0}.", selectableSongCount, randomSongIndex));
                Trace.TraceInformation(builder.ToString());
            }

            // Third assignment
            TJAPlayer3.stageSongSelect.rChoosenSong = song.listランダム用ノードリスト[randomSongIndex];
            TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] = (int)Difficulty.Tower;

            CFloorManagement.reinitialize(TJAPlayer3.stageSongSelect.rChoosenSong.arスコア[(int)Difficulty.Tower].譜面情報.nLife);
            TJAPlayer3.stageSongSelect.r確定されたスコア = TJAPlayer3.stageSongSelect.rChoosenSong.arスコア[TJAPlayer3.stageSongSelect.actSongList.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(TJAPlayer3.stageSongSelect.rChoosenSong)];
            TJAPlayer3.stageSongSelect.str確定された曲のジャンル = TJAPlayer3.stageSongSelect.rChoosenSong.strジャンル;

            //TJAPlayer3.Skin.sound曲決定音.t再生する();

            this.eフェードアウト完了時の戻り値 = EReturnValue.SongChoosen;
            this.actFOtoNowLoading.tフェードアウト開始();                    // #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
            base.ePhaseID = CStage.EPhase.SongSelect_FadeOutToNowLoading;

            CSongSelectSongManager.stopSong();

            return true;
        }

        private void tDrawTower(int x, int y, BarInfo barInfo)
        {
            switch(barInfo.eノード種別)
            {
                case CSongListNode.ENodeType.SCORE:
                    TJAPlayer3.Tx.TowerSelect_Tower.t2D中心基準描画(x, y);
                    break;
                case CSongListNode.ENodeType.RANDOM:
                    TJAPlayer3.Tx.TowerSelect_Tower.t2D中心基準描画(x, y);
                    break;
                case CSongListNode.ENodeType.BOX:
                    TJAPlayer3.Tx.TowerSelect_Tower.t2D中心基準描画(x, y);
                    break;
                case CSongListNode.ENodeType.BACKBOX:
                    TJAPlayer3.Tx.TowerSelect_Tower.t2D中心基準描画(x, y);
                    break;
            }

            TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(barInfo.ttkTitle).t2D拡大率考慮中央基準描画(x + TJAPlayer3.Skin.TowerSelect_Title_Offset[0], y + TJAPlayer3.Skin.TowerSelect_Title_Offset[1]);
            TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(barInfo.ttkSubTitle).t2D拡大率考慮中央基準描画(x + TJAPlayer3.Skin.TowerSelect_SubTitle_Offset[0], y + TJAPlayer3.Skin.TowerSelect_SubTitle_Offset[1]);
        }

        private void tUpdateBarInfos()
        {
            BarInfos = new BarInfo[listSongs.Count];
            tSetBarInfos();
        }

        private void tOpenFolder(CSongListNode song)
        {
            nCurrentSongIndex = 0;
            listSongs = song.list子リスト;
            tUpdateBarInfos();
        }

        private void tCloseFolder(CSongListNode song)
        {
            nCurrentSongIndex = 0;
            listSongs = song.rParentNode.rParentNode.list子リスト;
            tUpdateBarInfos();
        }

        private void tSetBarInfos()
        {
            for(int i = 0; i < BarInfos.Length; i++)
            {
                BarInfos[i] = new BarInfo();
                BarInfo bar = BarInfos[i];
                CSongListNode song = listSongs[i];

                bar.strTitle = song.ldTitle.GetString("");
                bar.strSubTitle = song.ldSubtitle.GetString("");
                bar.eノード種別 = song.eノード種別;

                bar.ttkTitle = new CActSelect曲リスト.TitleTextureKey(bar.strTitle, pfTitleFont, Color.Black, Color.Transparent, TJAPlayer3.Skin.TowerSelect_Title_MaxWidth);
                bar.ttkSubTitle = new CActSelect曲リスト.TitleTextureKey(bar.strSubTitle, pfTitleFont, Color.Black, Color.Transparent, TJAPlayer3.Skin.TowerSelect_SubTitle_MaxWidth);
            }
        }

        private BarInfo[] BarInfos;
        private List<CSongListNode> listSongs;

        private ScriptBG Background;

        private CCachedFontRenderer pfTitleFont;
        private CCachedFontRenderer pfSubTitleFont;

        public EReturnValue eフェードアウト完了時の戻り値;
        public CActFIFOStart actFOtoNowLoading;
        public CActFIFOBlack actFOtoTitle;

        private int nCurrentSongIndex;
        private CSongListNode currentSong
        {
            get
            {
                return listSongs[nCurrentSongIndex];
            }
        }

        #endregion
    }
}
