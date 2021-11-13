using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using FDK;
using SlimDX.DirectInput;
using static TJAPlayer3.CActSelect�ȃ��X�g;

namespace TJAPlayer3
{
    class CStageHeya : CStage
    {
        public CStageHeya()
        {
            base.e�X�e�[�WID = E�X�e�[�W.Heya;
            base.e�t�F�[�YID = CStage.E�t�F�[�Y.����_�ʏ���;

            base.list�qActivities.Add(this.actFOtoTitle = new CActFIFOBlack());

            base.list�qActivities.Add(this.PuchiChara = new PuchiChara());
        }


        public override void On������()
        {
            if (base.b���������Ă�)
                return;

            base.e�t�F�[�YID = CStage.E�t�F�[�Y.����_�ʏ���;
            this.e�t�F�[�h�A�E�g�������̖߂�l = E�߂�l.�p��;

            ctDonchan_In = new CCounter();
            ctDonchan_Normal = new CCounter(0, TJAPlayer3.Tx.SongSelect_Donchan_Normal.Length - 1, 1000 / 45, TJAPlayer3.Timer);

            bInSongPlayed = false;


            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
                this.pfHeyaFont = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 14);
            else
                this.pfHeyaFont = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 14);


            // 1P, configure later for default 2P
            iPlayer = 0;

            #region [Main menu]

            this.ttkMainMenuOpt = new TitleTextureKey[5];
            
            for (int i = 0; i < ttkMainMenuOpt.Length; i++)
            {
                this.ttkMainMenuOpt[i] = new TitleTextureKey(CLangManager.LangInstance.GetString(1030 + i), this.pfHeyaFont, Color.White, Color.DarkGreen, 1000);
            }

            #endregion

            #region [Dan title]

            int amount = 1;
            if (TJAPlayer3.NamePlateConfig.data.DanTitles[iPlayer] != null)
                amount += TJAPlayer3.NamePlateConfig.data.DanTitles[iPlayer].Count;

            this.ttkDanTitles = new TitleTextureKey[amount];

            // Silver Shinjin (default rank) always avaliable by default
            this.ttkDanTitles[0] = new TitleTextureKey("�V�l", this.pfHeyaFont, Color.White, Color.Black, 1000);

            int idx = 1;
            if (TJAPlayer3.NamePlateConfig.data.DanTitles[iPlayer] != null)
            {
                foreach (var item in TJAPlayer3.NamePlateConfig.data.DanTitles[iPlayer])
                {
                    if (item.Value.isGold == true)
                        this.ttkDanTitles[idx] = new TitleTextureKey(item.Key, this.pfHeyaFont, Color.Gold, Color.Black, 1000);
                    else 
                        this.ttkDanTitles[idx] = new TitleTextureKey(item.Key, this.pfHeyaFont, Color.White, Color.Black, 1000);
                    idx++;
                }
            }

            #endregion

            #region [Plate title]

            amount = 1;

            this.ttkTitles = new TitleTextureKey[amount];

            // Wood shojinsha (default title) always avaliable by default
            this.ttkTitles[0] = new TitleTextureKey("���S��", this.pfHeyaFont, Color.White, Color.Black, 1000);

            #endregion

            // -1 : Main Menu, >= 0 : See Main menu opt
            iCurrentMenu = -1;
            iMainMenuCurrent = 0;

            // Tmp variables
            iPuchiCharaCount = 120;
            iCharacterCount = 1;

            this.tResetOpts();

            this.PuchiChara.IdleAnimation();

            base.On������();
        }

        public override void On�񊈐���()
        {
            base.On�񊈐���();
        }

        public override void OnManaged���\�[�X�̍쐬()
        {
            base.OnManaged���\�[�X�̍쐬();
        }

        public override void OnManaged���\�[�X�̉��()
        {
            base.OnManaged���\�[�X�̉��();
        }

        public override int On�i�s�`��()
        {
            ctDonchan_Normal.t�i�sLoop();
            ctDonchan_In.t�i�s();

            TJAPlayer3.Tx.Heya_Background.t2D�`��(TJAPlayer3.app.Device, 0, 0);

            #region [Menus display]

            #region [Main menu (Side bar)]

            for (int i = 0; i < this.ttkMainMenuOpt.Length; i++)
            {
                CTexture tmpTex = TJAPlayer3.stage�I��.act�ȃ��X�g.ResolveTitleTexture(this.ttkMainMenuOpt[i]);

                if (iCurrentMenu != -1 || iMainMenuCurrent != i)
                {
                    tmpTex.color4 = Color.DarkGray;
                    TJAPlayer3.Tx.Heya_Side_Menu.color4 = Color.DarkGray;
                }
                else
                {
                    tmpTex.color4 = Color.White;
                    TJAPlayer3.Tx.Heya_Side_Menu.color4 = Color.White;
                }

                TJAPlayer3.Tx.Heya_Side_Menu.t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device, 164, 26 + 80 * i);
                tmpTex.t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device, 164, 40 + 80 * i);
            }

            #endregion

            #region [Dan title]

            if (iCurrentMenu == 2)
            {
                for (int i = -5; i < 6; i++)
                {
                    int pos = (this.ttkDanTitles.Length * 5 + iDanTitleCurrent + i) % this.ttkDanTitles.Length;

                    CTexture tmpTex = TJAPlayer3.stage�I��.act�ȃ��X�g.ResolveTitleTexture(this.ttkDanTitles[pos]);

                    if (i != 0)
                    {
                        tmpTex.color4 = Color.DarkGray;
                        TJAPlayer3.Tx.Heya_Side_Menu.color4 = Color.DarkGray;
                    }
                    else
                    {
                        tmpTex.color4 = Color.White;
                        TJAPlayer3.Tx.Heya_Side_Menu.color4 = Color.White;
                    }

                    TJAPlayer3.Tx.Heya_Side_Menu.t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device, 730 + -10 * Math.Abs(i), 340 + 70 * i);
                    tmpTex.t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device, 730 + -10 * Math.Abs(i), 354 + 70 * i);
                }

                for (int i = 0; i < this.ttkDanTitles.Length; i++)
                {
                    int pos = i - iDanTitleCurrent;
                }
            }

            #endregion


            #endregion

            #region [General Don animations]

            if (!ctDonchan_In.b�J�n����)
            {
                TJAPlayer3.Skin.soundHeyaBGM.t�Đ�����();
                ctDonchan_In.t�J�n(0, 180, 1.25f, TJAPlayer3.Timer);    
            }

            TJAPlayer3.NamePlate.tNamePlateDraw(TJAPlayer3.Skin.SongSelect_NamePlate_X[0], TJAPlayer3.Skin.SongSelect_NamePlate_Y[0] + 5, 0);

            #endregion

            #region [ �ǂ񂿂��֘A ]

            if (ctDonchan_In.n���݂̒l != 90)
                {
                    float DonchanX = 0f, DonchanY = 0f;

                    DonchanX = (float)Math.Sin(ctDonchan_In.n���݂̒l / 2 * (Math.PI / 180)) * 200f;
                    DonchanY = ((float)Math.Sin((90 + (ctDonchan_In.n���݂̒l / 2)) * (Math.PI / 180)) * 150f);

                    TJAPlayer3.Tx.SongSelect_Donchan_Normal[ctDonchan_Normal.n���݂̒l].Opacity = ctDonchan_In.n���݂̒l * 2;
                    TJAPlayer3.Tx.SongSelect_Donchan_Normal[ctDonchan_Normal.n���݂̒l].t2D�`��(TJAPlayer3.app.Device, -200 + DonchanX, 336 - DonchanY);

                    #region [PuchiChara]

                    this.PuchiChara.On�i�s�`��(0 + 100, 336 + 230, false);

                    #endregion
                }

            #endregion

            #region [ �L�[�֘A ]

            if (TJAPlayer3.Input�Ǘ�.Keyboard.b�L�[�������ꂽ((int)Key.RightArrow) ||
                TJAPlayer3.Pad.b�����ꂽ(E�y��p�[�g.DRUMS, E�p�b�h.RBlue))
            {
                if (this.tMove(1))
                {
                    TJAPlayer3.Skin.sound�ύX��.t�Đ�����();
                }
            }

            else if (TJAPlayer3.Input�Ǘ�.Keyboard.b�L�[�������ꂽ((int)Key.LeftArrow) ||
                TJAPlayer3.Pad.b�����ꂽ(E�y��p�[�g.DRUMS, E�p�b�h.LBlue))
            {
                if (this.tMove(-1))
                {
                    TJAPlayer3.Skin.sound�ύX��.t�Đ�����();
                }
            }

            else if (TJAPlayer3.Input�Ǘ�.Keyboard.b�L�[�������ꂽ((int)Key.Return) ||
                TJAPlayer3.Pad.b�����ꂽ(E�y��p�[�g.DRUMS, E�p�b�h.LRed) ||
                TJAPlayer3.Pad.b�����ꂽ(E�y��p�[�g.DRUMS, E�p�b�h.RRed))
            {

                #region [Decide]

                TJAPlayer3.Skin.sound���艹.t�Đ�����();

                // Return to main menu
                if (iCurrentMenu == -1 && iMainMenuCurrent == 0)
                {
                    TJAPlayer3.Skin.soundHeyaBGM.t��~����();
                    this.e�t�F�[�h�A�E�g�������̖߂�l = E�߂�l.�^�C�g���ɖ߂�;
                    this.actFOtoTitle.t�t�F�[�h�A�E�g�J�n();
                    base.e�t�F�[�YID = CStage.E�t�F�[�Y.����_�t�F�[�h�A�E�g;
                }

                else if (iCurrentMenu == -1)
                {
                    iCurrentMenu = iMainMenuCurrent - 1;
                }

                else if (iCurrentMenu == 2)
                {
                    bool iG = false;
                    int cs = 0;

                    if (iDanTitleCurrent > 0)
                    {
                        iG = TJAPlayer3.NamePlateConfig.data.DanTitles[iPlayer][this.ttkDanTitles[iDanTitleCurrent].str����].isGold;
                        cs = TJAPlayer3.NamePlateConfig.data.DanTitles[iPlayer][this.ttkDanTitles[iDanTitleCurrent].str����].clearStatus;
                    }

                    TJAPlayer3.NamePlateConfig.data.Dan[iPlayer] = this.ttkDanTitles[iDanTitleCurrent].str����;
                    TJAPlayer3.NamePlateConfig.data.DanGold[iPlayer] = iG;
                    TJAPlayer3.NamePlateConfig.data.DanType[iPlayer] = cs;

                    TJAPlayer3.NamePlate.tNamePlateRefreshTitles(iPlayer);

                    TJAPlayer3.NamePlateConfig.tApplyHeyaChanges();

                    iCurrentMenu = -1;
                    this.tResetOpts();
                }

                #endregion
            }

            else if (TJAPlayer3.Input�Ǘ�.Keyboard.b�L�[�������ꂽ((int)Key.Escape))
            {
                
                TJAPlayer3.Skin.sound�����.t�Đ�����();

                if (iCurrentMenu == -1)
                {
                    TJAPlayer3.Skin.soundHeyaBGM.t��~����();
                    this.e�t�F�[�h�A�E�g�������̖߂�l = E�߂�l.�^�C�g���ɖ߂�;
                    this.actFOtoTitle.t�t�F�[�h�A�E�g�J�n();
                    base.e�t�F�[�YID = CStage.E�t�F�[�Y.����_�t�F�[�h�A�E�g;
                }
                else
                {
                    iCurrentMenu = -1;
                    this.tResetOpts();
                }
                    

                return 0;
            }

            #endregion

            switch (base.e�t�F�[�YID)
            {
                case CStage.E�t�F�[�Y.����_�t�F�[�h�A�E�g:
                    if (this.actFOtoTitle.On�i�s�`��() == 0)
                    {
                        break;
                    }
                    return (int)this.e�t�F�[�h�A�E�g�������̖߂�l;

            }

            return 0;
        }

        public enum E�߂�l : int
        {
            �p��,
            �^�C�g���ɖ߂�,
            �I�Ȃ���
        }

        public bool bInSongPlayed;

        private CCounter ctDonchan_In;
        private CCounter ctDonchan_Normal;

        private PuchiChara PuchiChara;

        private int iPlayer;

        private int iMainMenuCurrent;
        private int iPuchiCharaCurrent;
        private int iCharacterCurrent;
        private int iDanTitleCurrent;
        private int iTitleCurrent;

        private int iCurrentMenu;

        private void tResetOpts()
        {
            iTitleCurrent = 0;
            iDanTitleCurrent = 0;
            iCharacterCurrent = TJAPlayer3.NamePlateConfig.data.Character[this.iPlayer];
            iPuchiCharaCurrent = TJAPlayer3.NamePlateConfig.data.PuchiChara[this.iPlayer];
        }

        private bool tMove(int off)
        {
            if (iCurrentMenu == -1)
                iMainMenuCurrent = (this.ttkMainMenuOpt.Length + iMainMenuCurrent + off) % this.ttkMainMenuOpt.Length;
            else if (iCurrentMenu == 0)
                iPuchiCharaCurrent = (iPuchiCharaCount + iPuchiCharaCurrent + off) % iPuchiCharaCount;
            else if (iCurrentMenu == 1)
                iCharacterCurrent = (iCharacterCount + iCharacterCurrent + off) % iCharacterCount;
            else if (iCurrentMenu == 2)
                iDanTitleCurrent = (this.ttkDanTitles.Length + iDanTitleCurrent + off) % this.ttkDanTitles.Length;
            else if (iCurrentMenu == 3)
                iTitleCurrent = (this.ttkTitles.Length + iTitleCurrent + off) % this.ttkTitles.Length;
            else
                return false;

            return true;
        }

        private TitleTextureKey[] ttkMainMenuOpt;
        private CPrivateFastFont pfHeyaFont;

        private TitleTextureKey[] ttkDanTitles;

        private TitleTextureKey[] ttkTitles;

        private int iPuchiCharaCount;
        private int iCharacterCount;

        public E�߂�l e�t�F�[�h�A�E�g�������̖߂�l;

        public CActFIFOBlack actFOtoTitle;
    }
}
