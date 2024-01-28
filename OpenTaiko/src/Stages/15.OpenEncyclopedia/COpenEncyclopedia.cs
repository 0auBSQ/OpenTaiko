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
            base.eStageID = EStage.TEMPLATE;
            base.ePhaseID = CStage.EPhase.Common_NORMAL;

            // Load CActivity objects here
            // base.list子Activities.Add(this.act = new CAct());

            base.ChildActivities.Add(this.actFOtoTitle = new CActFIFOBlack());

        }

        public override void Activate()
        {
            // On activation

            if (base.IsActivated)
                return;

            base.ePhaseID = CStage.EPhase.Common_NORMAL;
            this.eフェードアウト完了時の戻り値 = EReturnValue.Continuation;

            TJAPlayer3.Skin.soundEncyclopediaBGM?.tPlay();

            _controler = new CEncyclopediaControler();

            Background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.OPENENCYCLOPEDIA}Script.lua"));
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

            base.CreateManagedResource();
        }

        public override void ReleaseManagedResource()
        {
            // Ressource freeing

            base.ReleaseManagedResource();
        }

        public override int Draw()
        {
            #region [Fetch variables]

            _arePagesOpened = _controler.tArePagesOpened();
            bool _backToMain = false;

            #endregion

            #region [Displayables]

            Background.Update();
            Background.Draw();

            //OpenEncyclopedia_Background?.t2D描画(0, 0);

            if (_arePagesOpened)
            {
                TJAPlayer3.Tx.OpenEncyclopedia_Context?.t2D描画(0, 0);

                if (_controler.Pages.Length > 0)
                {
                    var _page = _controler.Pages[_controler.PageIndex];

                    _page.Item2?.t2D中心基準描画(TJAPlayer3.Skin.OpenEncyclopedia_Context_Item2[0], TJAPlayer3.Skin.OpenEncyclopedia_Context_Item2[1]);
                    if (_page.Item3 != null)
                    {
                        _page.Item3.vcScaleRatio.X = TJAPlayer3.Skin.Resolution[0] / (2f * _page.Item3.szTextureSize.Width);
                        _page.Item3.vcScaleRatio.Y = TJAPlayer3.Skin.Resolution[1] / (2f * _page.Item3.szTextureSize.Height);
                        _page.Item3.t2D描画(TJAPlayer3.Skin.OpenEncyclopedia_Context_Item3[0], TJAPlayer3.Skin.OpenEncyclopedia_Context_Item3[1]);
                    }
                    _controler.PageText?.t2D下中央基準描画(TJAPlayer3.Skin.OpenEncyclopedia_Context_PageText[0], TJAPlayer3.Skin.OpenEncyclopedia_Context_PageText[1]);
                }
            }

            for (int i = -7; i < 7; i++)
            {
                var _pos = (_controler.MenuIndex + i + (_controler.Submenus.Length * 7)) % _controler.Submenus.Length;
                var _menu = _controler.Submenus[_pos];

                if (i != 0)
                {
                    TJAPlayer3.Tx.OpenEncyclopedia_Return_Box?.tUpdateColor4(CConversion.ColorToColor4(Color.DarkGray));
                    TJAPlayer3.Tx.OpenEncyclopedia_Side_Menu?.tUpdateColor4(CConversion.ColorToColor4(Color.DarkGray));
                    _menu.Item2?.tUpdateColor4(CConversion.ColorToColor4(Color.DarkGray));
                }
                else
                {
                    TJAPlayer3.Tx.OpenEncyclopedia_Return_Box?.tUpdateColor4(CConversion.ColorToColor4(Color.White));
                    TJAPlayer3.Tx.OpenEncyclopedia_Side_Menu?.tUpdateColor4(CConversion.ColorToColor4(Color.White));
                    _menu.Item2?.tUpdateColor4(CConversion.ColorToColor4(Color.White));
                }

                int x = TJAPlayer3.Skin.OpenEncyclopedia_Side_Menu[0] + TJAPlayer3.Skin.OpenEncyclopedia_Side_Menu_Move[0] * i;
                int y = TJAPlayer3.Skin.OpenEncyclopedia_Side_Menu[1] + TJAPlayer3.Skin.OpenEncyclopedia_Side_Menu_Move[1] * i;

                if (_pos == 0)
                    TJAPlayer3.Tx.OpenEncyclopedia_Return_Box?.t2D中心基準描画(x, y);
                else
                    TJAPlayer3.Tx.OpenEncyclopedia_Side_Menu?.t2D中心基準描画(x, y);
                _menu.Item2?.t2D中心基準描画(
                    x + TJAPlayer3.Skin.OpenEncyclopedia_Side_Menu_Text_Offset[0], 
                    y + TJAPlayer3.Skin.OpenEncyclopedia_Side_Menu_Text_Offset[1]);
            }

            #endregion

            #region [Inputs]

            if (TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow) ||
                    TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RightChange))
            {
                _controler.tHandleRight();
                TJAPlayer3.Skin.soundChangeSFX.tPlay();
            }

            else if (TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow) ||
                    TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LeftChange))
            {
                _controler.tHandleLeft();
                TJAPlayer3.Skin.soundChangeSFX.tPlay();
            }

            else if (TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape) ||
                    TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.Cancel))
            {
                _backToMain = _controler.tHandleBack();
                TJAPlayer3.Skin.soundCancelSFX.tPlay();
            }

            else if (TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return) ||
                    TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.Decide))
            {
                var (_b1, _b2) = _controler.tHandleEnter();
                _backToMain = _b2;

                if (_b1)
                    TJAPlayer3.Skin.soundDecideSFX.tPlay();
                else
                    TJAPlayer3.Skin.soundCancelSFX.tPlay();
            }

            #endregion

            #region [Postprocessing]

            if (_backToMain)
            {
                TJAPlayer3.Skin.soundEncyclopediaBGM?.tStop();
                this.eフェードアウト完了時の戻り値 = EReturnValue.ReturnToTitle;
                this.actFOtoTitle.tフェードアウト開始();
                base.ePhaseID = CStage.EPhase.Common_FADEOUT;
            }

            #endregion


            #region [FadeOut]

            // Menu exit fade out transition
            switch (base.ePhaseID)
            {
                case CStage.EPhase.Common_FADEOUT:
                    if (this.actFOtoTitle.Draw() == 0)
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
