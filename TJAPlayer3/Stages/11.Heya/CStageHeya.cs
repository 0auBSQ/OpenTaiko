using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using FDK;
using SharpDX;
using static TJAPlayer3.CActSelect�ȃ��X�g;

using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;
using Color = System.Drawing.Color;

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
            iPlayer = TJAPlayer3.SaveFile;

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
            if (TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer] != null)
                amount += TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer].Count;

            this.ttkTitles = new TitleTextureKey[amount];

            // Wood shojinsha (default title) always avaliable by default
            this.ttkTitles[0] = new TitleTextureKey("���S��", this.pfHeyaFont, Color.Black, Color.Transparent, 1000);

            idx = 1;
            if (TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer] != null)
            {
                foreach (var item in TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer])
                {
                    this.ttkTitles[idx] = new TitleTextureKey(item.Key, this.pfHeyaFont, Color.Black, Color.Transparent, 1000);
                    idx++;
                }
            }

            #endregion

            // -1 : Main Menu, >= 0 : See Main menu opt
            iCurrentMenu = -1;
            iMainMenuCurrent = 0;

            #region [PuchiChara stuff]

            // Tmp variables
            iPuchiCharaCount = 120;

            ttkPuchiCharaNames = new TitleTextureKey[iPuchiCharaCount];

            var RarityToColor = new Dictionary<string, Color>
            {
                ["Common"] = Color.White,
                ["Uncommon"] = Color.Lime,
                ["Rare"] = Color.Blue,
                ["Epic"] = Color.Purple,
                ["Legendary"] = Color.Orange,
            };

            var dbData = TJAPlayer3.Databases.DBPuchichara.data;

            for (int i = 0; i < iPuchiCharaCount; i++)
            {
                if (dbData.ContainsKey(i))
                {
                    Color textColor = Color.White;

                    string rarity = dbData[i].Rarity;

                    if (RarityToColor.ContainsKey(rarity))
                        textColor = RarityToColor[rarity];

                    ttkPuchiCharaNames[i] = new TitleTextureKey(dbData[i].Name, this.pfHeyaFont, textColor, Color.Black, 1000);
                }
            }

            #endregion

            iCharacterCount = TJAPlayer3.Skin.Characters_Ptn;

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
                    tmpTex.color4 = C�ϊ�.ColorToColor4(Color.DarkGray);
                    TJAPlayer3.Tx.Heya_Side_Menu.color4 = C�ϊ�.ColorToColor4(Color.DarkGray);
                }
                else
                {
                    tmpTex.color4 = C�ϊ�.ColorToColor4(Color.White);
                    TJAPlayer3.Tx.Heya_Side_Menu.color4 = C�ϊ�.ColorToColor4(Color.White);
                }

                TJAPlayer3.Tx.Heya_Side_Menu.t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device, 164, 26 + 80 * i);
                tmpTex.t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device, 164, 40 + 80 * i);
            }

            #endregion

            #region [Petit chara]

            if (iCurrentMenu == 0)
            {
                for (int i = -5; i < 6; i++)
                {
                    int pos = (iPuchiCharaCount * 5 + iPuchiCharaCurrent + i) % iPuchiCharaCount;

                    if (i != 0)
                    {
                        TJAPlayer3.Tx.PuchiChara.color4 = C�ϊ�.ColorToColor4(Color.DarkGray);
                        TJAPlayer3.Tx.Heya_Center_Menu_Box_Slot.color4 = C�ϊ�.ColorToColor4(Color.DarkGray);
                    }
                    else
                    {
                        TJAPlayer3.Tx.PuchiChara.color4 = C�ϊ�.ColorToColor4(Color.White);
                        TJAPlayer3.Tx.Heya_Center_Menu_Box_Slot.color4 = C�ϊ�.ColorToColor4(Color.White);
                    }

                    TJAPlayer3.Tx.Heya_Center_Menu_Box_Slot.t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device, 620 + 302 * i, 200);

                    int puriColumn = pos % 5;
                    int puriRow = pos / 5;
                    
                    TJAPlayer3.Tx.PuchiChara.t2D�g�嗦�l��������`��(TJAPlayer3.app.Device, 620 + 302 * i, 320 + (int)(PuchiChara.sineY), 
                        new Rectangle((PuchiChara.Counter.n���݂̒l + 2 * puriColumn) * TJAPlayer3.Skin.Game_PuchiChara[0], 
                        puriRow * TJAPlayer3.Skin.Game_PuchiChara[1], 
                        TJAPlayer3.Skin.Game_PuchiChara[0], 
                        TJAPlayer3.Skin.Game_PuchiChara[1]));

                    TJAPlayer3.Tx.PuchiChara.color4 = C�ϊ�.ColorToColor4(Color.White);

                    if (ttkPuchiCharaNames[pos] != null)
                    {
                        CTexture tmpTex = TJAPlayer3.stage�I��.act�ȃ��X�g.ResolveTitleTexture(ttkPuchiCharaNames[pos]);

                        tmpTex.t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device, 620 + 302 * i, 448);
                    }

                    #region [Unlockable information zone]

                    if (i == 0 && this.ttkInfoSection != null)
                        TJAPlayer3.stage�I��.act�ȃ��X�g.ResolveTitleTexture(this.ttkInfoSection)
                            .t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device, 620, 560);

                    #endregion
                }
            }

            #endregion

            #region [Character]

            if (iCurrentMenu == 1)
            {
                for (int i = -5; i < 6; i++)
                {
                    int pos = (iCharacterCount * 5 + iCharacterCurrent + i) % iCharacterCount;

                    if (i != 0)
                    {
                        if (TJAPlayer3.Tx.Characters_Heya_Preview[pos] != null)
                            TJAPlayer3.Tx.Characters_Heya_Preview[pos].color4 = C�ϊ�.ColorToColor4(Color.DarkGray);
                        TJAPlayer3.Tx.Heya_Center_Menu_Box_Slot.color4 = C�ϊ�.ColorToColor4(Color.DarkGray);
                    }
                    else
                    {
                        if (TJAPlayer3.Tx.Characters_Heya_Preview[pos] != null)
                            TJAPlayer3.Tx.Characters_Heya_Preview[pos].color4 = C�ϊ�.ColorToColor4(Color.White);
                        TJAPlayer3.Tx.Heya_Center_Menu_Box_Slot.color4 = C�ϊ�.ColorToColor4(Color.White);
                    }

                    TJAPlayer3.Tx.Heya_Center_Menu_Box_Slot.t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device, 620 + 302 * i, 200);

                    TJAPlayer3.Tx.Characters_Heya_Preview[pos]?.t2D�g�嗦�l��������`��(TJAPlayer3.app.Device, 620 + 302 * i, 320);

                    if (TJAPlayer3.Tx.Characters_Heya_Preview[pos] != null)
                        TJAPlayer3.Tx.Characters_Heya_Preview[pos].color4 = C�ϊ�.ColorToColor4(Color.White);
                }
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
                        tmpTex.color4 = C�ϊ�.ColorToColor4(Color.DarkGray);
                        TJAPlayer3.Tx.Heya_Side_Menu.color4 = C�ϊ�.ColorToColor4(Color.DarkGray);
                        TJAPlayer3.Tx.NamePlateBase.color4 = C�ϊ�.ColorToColor4(Color.DarkGray);
                    }
                    else
                    {
                        tmpTex.color4 = C�ϊ�.ColorToColor4(Color.White);
                        TJAPlayer3.Tx.Heya_Side_Menu.color4 = C�ϊ�.ColorToColor4(Color.White);
                        TJAPlayer3.Tx.NamePlateBase.color4 = C�ϊ�.ColorToColor4(Color.White);
                    }

                    int danGrade = 0;
                    if (pos > 0)
                    {
                        danGrade = TJAPlayer3.NamePlateConfig.data.DanTitles[iPlayer][this.ttkDanTitles[pos].str����].clearStatus;
                    }

                    TJAPlayer3.Tx.Heya_Side_Menu.t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device, 730 + -10 * Math.Abs(i), 340 + 70 * i);

                    TJAPlayer3.Tx.NamePlateBase.t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device,
                        718 + -10 * Math.Abs(i),
                        331 + 70 * i,
                        new RectangleF(0, (8 + danGrade) * 54, 220, 54));
                    TJAPlayer3.Tx.NamePlateBase.color4 = C�ϊ�.ColorToColor4(Color.White);

                    tmpTex.t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device, 730 + -10 * Math.Abs(i), 354 + 70 * i);

                    
                }
            }

            #endregion

            #region [Title plate]

            if (iCurrentMenu == 3)
            {
                for (int i = -5; i < 6; i++)
                {
                    int pos = (this.ttkTitles.Length * 5 + iTitleCurrent + i) % this.ttkTitles.Length;

                    CTexture tmpTex = TJAPlayer3.stage�I��.act�ȃ��X�g.ResolveTitleTexture(this.ttkTitles[pos]);

                    if (i != 0)
                    {
                        tmpTex.color4 = C�ϊ�.ColorToColor4(Color.DarkGray);
                        TJAPlayer3.Tx.Heya_Side_Menu.color4 = C�ϊ�.ColorToColor4(Color.DarkGray);
                    }
                    else
                    {
                        tmpTex.color4 = C�ϊ�.ColorToColor4(Color.White);
                        TJAPlayer3.Tx.Heya_Side_Menu.color4 = C�ϊ�.ColorToColor4(Color.White);
                    }

                    TJAPlayer3.Tx.Heya_Side_Menu.t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device, 730 + -10 * Math.Abs(i), 340 + 70 * i);

                    int iType = -1;

                    if (TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer] != null &&
                        TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer].ContainsKey(this.ttkTitles[pos].str����))
                        iType = TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer][this.ttkTitles[pos].str����].iType;
                    else if (pos == 0)
                        iType = 0;

                    if (iType >= 0 && iType < TJAPlayer3.Skin.Config_NamePlate_Ptn_Title)
                    {
                        TJAPlayer3.Tx.NamePlate_Title[iType][TJAPlayer3.NamePlate.ctAnimatedNamePlateTitle.n���݂̒l % TJAPlayer3.Skin.Config_NamePlate_Ptn_Title_Boxes[iType]].t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device,
                            730 + -10 * Math.Abs(i),
                            348 + 70 * i);
                    } 

                    tmpTex.t2D�g�嗦�l���㒆����`��(TJAPlayer3.app.Device, 730 + -10 * Math.Abs(i), 354 + 70 * i);

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

            #endregion

            #region [ �L�[�֘A ]

            if (TJAPlayer3.Input�Ǘ�.Keyboard.b�L�[�������ꂽ((int)SlimDXKeys.Key.RightArrow) ||
                TJAPlayer3.Pad.b�����ꂽ(E�y��p�[�g.DRUMS, E�p�b�h.RBlue))
            {
                if (this.tMove(1))
                {
                    TJAPlayer3.Skin.sound�ύX��.t�Đ�����();
                }
            }

            else if (TJAPlayer3.Input�Ǘ�.Keyboard.b�L�[�������ꂽ((int)SlimDXKeys.Key.LeftArrow) ||
                TJAPlayer3.Pad.b�����ꂽ(E�y��p�[�g.DRUMS, E�p�b�h.LBlue))
            {
                if (this.tMove(-1))
                {
                    TJAPlayer3.Skin.sound�ύX��.t�Đ�����();
                }
            }

            else if (TJAPlayer3.Input�Ǘ�.Keyboard.b�L�[�������ꂽ((int)SlimDXKeys.Key.Return) ||
                TJAPlayer3.Pad.b�����ꂽ(E�y��p�[�g.DRUMS, E�p�b�h.LRed) ||
                TJAPlayer3.Pad.b�����ꂽ(E�y��p�[�g.DRUMS, E�p�b�h.RRed))
            {

                #region [Decide]

                ESelectStatus ess = ESelectStatus.SELECTED;

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

                    if (iCurrentMenu == 0)
                        this.tUpdateUnlockableTextPuchi();
                }

                else if (iCurrentMenu == 0)
                {
                    ess = this.tSelectPuchi();

                    if (ess == ESelectStatus.SELECTED)
                    {
                        TJAPlayer3.NamePlateConfig.data.PuchiChara[iPlayer] = iPuchiCharaCurrent;

                        TJAPlayer3.NamePlateConfig.tApplyHeyaChanges();

                        iCurrentMenu = -1;
                        this.tResetOpts();
                    }
                    else if (ess == ESelectStatus.SUCCESS)
                    {
                        TJAPlayer3.NamePlateConfig.data.UnlockedPuchicharas[iPlayer].Add(iPuchiCharaCurrent);

                        TJAPlayer3.NamePlateConfig.tSpendCoins(puchiUnlockables[iPuchiCharaCurrent].Values[0], iPlayer);
                    }
                }

                else if (iCurrentMenu == 1)
                {
                    // Reload character, a bit time expensive but with a O(N) memory complexity instead of O(N * M)
                    TJAPlayer3.Tx.ReloadCharacter(TJAPlayer3.NamePlateConfig.data.Character[iPlayer], iCharacterCurrent, iPlayer);

                    TJAPlayer3.NamePlateConfig.data.Character[iPlayer] = iCharacterCurrent;

                    TJAPlayer3.NamePlateConfig.tApplyHeyaChanges();

                    iCurrentMenu = -1;
                    this.tResetOpts();
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

                else if (iCurrentMenu == 3)
                {
                    TJAPlayer3.NamePlateConfig.data.Title[iPlayer] = this.ttkTitles[iTitleCurrent].str����;

                    if (TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer] != null
                        && TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer].ContainsKey(this.ttkTitles[iTitleCurrent].str����))
                        TJAPlayer3.NamePlateConfig.data.TitleType[iPlayer] = TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer][this.ttkTitles[iTitleCurrent].str����].iType;
                    else if (iTitleCurrent == 0)
                        TJAPlayer3.NamePlateConfig.data.TitleType[iPlayer] = 0;
                    else
                        TJAPlayer3.NamePlateConfig.data.TitleType[iPlayer] = -1;

                    TJAPlayer3.NamePlate.tNamePlateRefreshTitles(iPlayer);

                    TJAPlayer3.NamePlateConfig.tApplyHeyaChanges();

                    iCurrentMenu = -1;
                    this.tResetOpts();
                }

                if (ess == ESelectStatus.SELECTED)
                    TJAPlayer3.Skin.sound���艹.t�Đ�����();
                else if (ess == ESelectStatus.FAILED)
                    TJAPlayer3.Skin.soundError.t�Đ�����();
                else
                    TJAPlayer3.Skin.SoundBanapas.t�Đ�����(); // To change with a more appropriate sfx sooner or later

                #endregion
            }

            else if (TJAPlayer3.Input�Ǘ�.Keyboard.b�L�[�������ꂽ((int)SlimDXKeys.Key.Escape))
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

        private TitleTextureKey[] ttkPuchiCharaNames;
        private TitleTextureKey ttkInfoSection;

        private int iCharacterCurrent;
        private int iDanTitleCurrent;
        private int iTitleCurrent;

        private int iCurrentMenu;

        private void tResetOpts()
        {
            // Retrieve titles if they exist
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
            {
                iPuchiCharaCurrent = (iPuchiCharaCount + iPuchiCharaCurrent + off) % iPuchiCharaCount;
                tUpdateUnlockableTextPuchi();
            }
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

        #region [Puchi unlockables]
        private void tUpdateUnlockableTextPuchi()
        {
            #region [Check unlockable]

            if (puchiUnlockables.ContainsKey(iPuchiCharaCurrent)
                && !TJAPlayer3.NamePlateConfig.data.UnlockedPuchicharas[iPlayer].Contains(iPuchiCharaCurrent))
            {
                // To update then when bought unlockables will be implemented
                this.ttkInfoSection = new TitleTextureKey(puchiUnlockables[iPuchiCharaCurrent].tConditionMessage()
                    , this.pfHeyaFont, Color.White, Color.Black, 1000);
            }
            else
                this.ttkInfoSection = null;

            #endregion
        }

        private ESelectStatus tSelectPuchi()
        {
            // Add "If unlocked" to select directly
            if (puchiUnlockables.ContainsKey(iPuchiCharaCurrent) 
                && !TJAPlayer3.NamePlateConfig.data.UnlockedPuchicharas[iPlayer].Contains(iPuchiCharaCurrent))
            {

                (bool, string) response = puchiUnlockables[iPuchiCharaCurrent].tConditionMet(new int[]{ TJAPlayer3.NamePlateConfig.data.Medals[TJAPlayer3.SaveFile] } );
                Color responseColor = (response.Item1) ? Color.Lime : Color.Red;

                // Send coins here for the unlock, considering that only coin-paid puchicharas can be unlocked directly from the Heya menu

                this.ttkInfoSection = new TitleTextureKey(response.Item2, this.pfHeyaFont, responseColor, Color.Black, 1000);

                return (response.Item1) ? ESelectStatus.SUCCESS : ESelectStatus.FAILED;
            }

            this.ttkInfoSection = null;
            return ESelectStatus.SELECTED;
        }

        /*
         *  FAILED : Selection/Purchase failed (failed condition)
         *  SUCCESS : Purchase succeed (without selection)
         *  SELECTED : Selection succeed
        */
        private enum ESelectStatus
        {
            FAILED,
            SUCCESS,
            SELECTED
        };

        #endregion

        private TitleTextureKey[] ttkMainMenuOpt;
        private CPrivateFastFont pfHeyaFont;

        private Dictionary<int, DBUnlockables.CUnlockConditions> puchiUnlockables = TJAPlayer3.Databases.DBUnlockables.data.Puchichara;

        private TitleTextureKey[] ttkDanTitles;

        private TitleTextureKey[] ttkTitles;

        private int iPuchiCharaCount;
        private int iCharacterCount;

        public E�߂�l e�t�F�[�h�A�E�g�������̖߂�l;

        public CActFIFOBlack actFOtoTitle;
    }
}
