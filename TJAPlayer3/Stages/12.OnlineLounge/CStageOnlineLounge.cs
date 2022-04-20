using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    class CStageOnlineLounge : CStage
    {

        public CStageOnlineLounge()
        {
            base.eステージID = Eステージ.OnlineLounge;
            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;

            // Load CActivity objects here
            // base.list子Activities.Add(this.act = new CAct());
        }

        public override void On活性化()
        {
            // On activation

            if (base.b活性化してる)
                return;

            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
            this.eフェードアウト完了時の戻り値 = E戻り値.継続;

            this.currentMenu = ECurrentMenu.MAIN;
            this.menuPointer = ECurrentMenu.CDN_SELECT;
            this.menus = new CMenuInfo[(int)ECurrentMenu.TOTAL];

            for (int i = 0; i < (int)ECurrentMenu.TOTAL; i++)
                this.menus[i] = new CMenuInfo(CLangManager.LangInstance.GetString(400 + i));



            base.On活性化();
        }

        public override void On非活性化()
        {
            // On de-activation

            base.On非活性化();
        }

        public override void OnManagedリソースの作成()
        {
            // Ressource allocation

            base.OnManagedリソースの作成();
        }

        public override void OnManagedリソースの解放()
        {
            // Ressource freeing

            base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {
            TJAPlayer3.Tx.OnlineLounge_Background.t2D描画(TJAPlayer3.app.Device, 0, 0);

            #region [Input]

            if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.RightArrow) ||
                TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue))
            {
                if (this.tMove(1))
                {
                    TJAPlayer3.Skin.sound変更音.t再生する();
                }
            }

            else if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.LeftArrow) ||
                TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue))
            {
                if (this.tMove(-1))
                {
                    TJAPlayer3.Skin.sound変更音.t再生する();
                }
            }

            else if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Escape))
            {

                TJAPlayer3.Skin.sound取消音.t再生する();

                if (currentMenu == ECurrentMenu.MAIN)
                {
                    TJAPlayer3.Skin.soundOnlineLoungeBGM.t停止する();
                    this.eフェードアウト完了時の戻り値 = E戻り値.タイトルに戻る;
                    this.actFOtoTitle.tフェードアウト開始();
                    base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
                }
                else
                {

                }


                return 0;
            }

            #endregion

            // Menu exit fade out transition
            #region [FadeOut]

            switch (base.eフェーズID)
            {
                case CStage.Eフェーズ.共通_フェードアウト:
                    if (this.actFOtoTitle.On進行描画() == 0)
                    {
                        break;
                    }
                    return (int)this.eフェードアウト完了時の戻り値;

            }

            #endregion

            return 0;
        }

        public bool tMove(int val)
        {

            return true;
        }


        public enum E戻り値 : int
        {
            継続,
            タイトルに戻る,
            選曲した
        }

        public enum ECurrentMenu : int
        {
            RETURN,         // Return button
            MAIN,           // Choice between select CDN and Online multiplayer
            CDN_SELECT,     // Select a registered CDN
            CDN_OPTION,     // Select between Download songs, Download characters and Download puchicharas
            CDN_SONGS,      // List songs
            CDN_CHARACTERS, // List characters
            CDN_PUCHICHARAS,// List puchicharas
            MULTI_SELECT,   // Main online multiplayer menu
            TOTAL,          // Submenus count
        }

        #region [Private]

        private ECurrentMenu currentMenu;
        private ECurrentMenu menuPointer;
        private CMenuInfo[] menus;
        public E戻り値 eフェードアウト完了時の戻り値;
        public CActFIFOBlack actFOtoTitle;

        private class CMenuInfo
        {
            public CMenuInfo(string ttl)
            {
                title = ttl;
            }

            public string title;
        }

        #endregion

    }
}
