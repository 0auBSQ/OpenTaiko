using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;
using System.Drawing;

// Minimalist menu class to use for custom menus
namespace TJAPlayer3
{
    class COpenEncyclopedia : CStage
    {
        public COpenEncyclopedia()
        {
            base.eステージID = Eステージ.Template;
            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;

            // Load CActivity objects here
            // base.list子Activities.Add(this.act = new CAct());

            base.list子Activities.Add(this.actFOtoTitle = new CActFIFOBlack());

        }

        public override void On活性化()
        {
            // On activation

            if (base.b活性化してる)
                return;

            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
            this.eフェードアウト完了時の戻り値 = EReturnValue.Continuation;

            TJAPlayer3.Skin.soundEncyclopediaBGM?.t再生する();

            _controler = new CEncyclopediaControler();
            
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

            if (!base.b活性化してない)
            {
                Background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.OPENENCYCLOPEDIA}Script.lua"));
                Background.Init();

                base.OnManagedリソースの作成();
            }
        }

        public override void OnManagedリソースの解放()
        {
            // Ressource freeing

            if (!base.b活性化してない)
            {
                TJAPlayer3.t安全にDisposeする(ref Background);

                base.OnManagedリソースの解放();
            }
        }

        public override int On進行描画()
        {
            #region [Fetch variables]

            _arePagesOpened = _controler.tArePagesOpened();
            bool _backToMain = false;

            #endregion

            #region [Displayables]

            Background.Update();
            Background.Draw();

            //TJAPlayer3.Tx.OpenEncyclopedia_Background?.t2D描画(TJAPlayer3.app.Device, 0, 0);

            if (_arePagesOpened)
            {
                TJAPlayer3.Tx.OpenEncyclopedia_Context?.t2D描画(TJAPlayer3.app.Device, 0, 0);

                if (_controler.Pages.Length > 0)
                {
                    var _page = _controler.Pages[_controler.PageIndex];

                    _page.Item2?.t2D中心基準描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.OpenEncyclopedia_Context_Item2[0], TJAPlayer3.Skin.OpenEncyclopedia_Context_Item2[1]);
                    _page.Item3?.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.OpenEncyclopedia_Context_Item3[0], TJAPlayer3.Skin.OpenEncyclopedia_Context_Item3[1]);
                    _controler.PageText?.t2D下中央基準描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.OpenEncyclopedia_Context_PageText[0], TJAPlayer3.Skin.OpenEncyclopedia_Context_PageText[1]);
                }
            }

            for (int i = -7; i < 7; i++)
            {
                var _pos = (_controler.MenuIndex + i + (_controler.Submenus.Length * 7)) % _controler.Submenus.Length;
                var _menu = _controler.Submenus[_pos];

                if (i != 0)
                {
                    TJAPlayer3.Tx.OpenEncyclopedia_Return_Box?.tUpdateColor4(C変換.ColorToColor4(Color.DarkGray));
                    TJAPlayer3.Tx.OpenEncyclopedia_Side_Menu?.tUpdateColor4(C変換.ColorToColor4(Color.DarkGray));
                    _menu.Item2?.tUpdateColor4(C変換.ColorToColor4(Color.DarkGray));
                }
                else
                {
                    TJAPlayer3.Tx.OpenEncyclopedia_Return_Box?.tUpdateColor4(C変換.ColorToColor4(Color.White));
                    TJAPlayer3.Tx.OpenEncyclopedia_Side_Menu?.tUpdateColor4(C変換.ColorToColor4(Color.White));
                    _menu.Item2?.tUpdateColor4(C変換.ColorToColor4(Color.White));
                }

                int x = TJAPlayer3.Skin.OpenEncyclopedia_Side_Menu[0] + TJAPlayer3.Skin.OpenEncyclopedia_Side_Menu_Move[0] * i;
                int y = TJAPlayer3.Skin.OpenEncyclopedia_Side_Menu[1] + TJAPlayer3.Skin.OpenEncyclopedia_Side_Menu_Move[1] * i;

                if (_pos == 0)
                    TJAPlayer3.Tx.OpenEncyclopedia_Return_Box?.t2D中心基準描画(TJAPlayer3.app.Device, x, y);
                else
                    TJAPlayer3.Tx.OpenEncyclopedia_Side_Menu?.t2D中心基準描画(TJAPlayer3.app.Device, x, y);
                _menu.Item2?.t2D中心基準描画(TJAPlayer3.app.Device, 
                    x + TJAPlayer3.Skin.OpenEncyclopedia_Side_Menu_Text_Offset[0], 
                    y + TJAPlayer3.Skin.OpenEncyclopedia_Side_Menu_Text_Offset[1]);
            }

            #endregion

            #region [Inputs]

            if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.RightArrow) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RightChange))
            {
                _controler.tHandleRight();
                TJAPlayer3.Skin.sound変更音.t再生する();
            }

            else if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.LeftArrow) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LeftChange))
            {
                _controler.tHandleLeft();
                TJAPlayer3.Skin.sound変更音.t再生する();
            }

            else if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Escape) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.Cancel))
            {
                _backToMain = _controler.tHandleBack();
                TJAPlayer3.Skin.sound取消音.t再生する();
            }

            else if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Return) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.Decide))
            {
                var (_b1, _b2) = _controler.tHandleEnter();
                _backToMain = _b2;

                if (_b1)
                    TJAPlayer3.Skin.sound決定音.t再生する();
                else
                    TJAPlayer3.Skin.sound取消音.t再生する();
            }

            #endregion

            #region [Postprocessing]

            if (_backToMain)
            {
                TJAPlayer3.Skin.soundEncyclopediaBGM?.t停止する();
                this.eフェードアウト完了時の戻り値 = EReturnValue.ReturnToTitle;
                this.actFOtoTitle.tフェードアウト開始();
                base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
            }

            #endregion


            #region [FadeOut]

            // Menu exit fade out transition
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

        #region [Private]

        private ScriptBG Background;

        private CEncyclopediaControler _controler;
        private bool _arePagesOpened;

        public EReturnValue eフェードアウト完了時の戻り値;
        public CActFIFOBlack actFOtoTitle;

        #endregion
    }
}
