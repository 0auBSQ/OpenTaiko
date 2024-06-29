using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;

namespace TJAPlayer3
{
    class CActNewHeya : CActivity
    {
        public bool IsOpend { get; private set; }
        private CCachedFontRenderer MenuFont;

        private CActSelect曲リスト.TitleTextureKey[] MenuTitleKeys = new CActSelect曲リスト.TitleTextureKey[5];
        private CActSelect曲リスト.TitleTextureKey[] ttkPuchiCharaNames;
        private CActSelect曲リスト.TitleTextureKey[] ttkPuchiCharaAuthors;
        private CActSelect曲リスト.TitleTextureKey[] ttkCharacterNames;
        private CActSelect曲リスト.TitleTextureKey[] ttkCharacterAuthors;
        private CActSelect曲リスト.TitleTextureKey ttkInfoSection;
        private CActSelect曲リスト.TitleTextureKey[] ttkDanTitles;
        private CActSelect曲リスト.TitleTextureKey[] ttkTitles;
        private string[] titlesKeys;

        public CCounter InFade;

        public CCounter CharaBoxAnime;

        private int CurrentIndex;

        private int CurrentMaxIndex;

        private int CurrentPlayer;

        private enum SelectableInfo
        {
            PlayerSelect,
            ModeSelect,
            Select
        }

        private enum ModeType
        {
            None = -1,
            PuchiChara,
            Chara,
            DanTitle,
            SubTitle
        }

        private SelectableInfo CurrentState;

        private ModeType CurrentMode = ModeType.None;

        private void SetState(SelectableInfo selectableInfo)
        {
            CurrentState = selectableInfo;
            switch(selectableInfo)
            {
                case SelectableInfo.PlayerSelect:
                CurrentIndex = 1;
                CurrentMaxIndex = TJAPlayer3.ConfigIni.nPlayerCount + 1;
                break;
                case SelectableInfo.ModeSelect:
                CurrentIndex = 1;
                CurrentMaxIndex = 5;
                break;
                case SelectableInfo.Select:
                CurrentMode = (ModeType)(CurrentIndex - 1);
                switch(CurrentMode)
                {
                    case ModeType.Chara:
                    CurrentMaxIndex = TJAPlayer3.Skin.Characters_Ptn;
                    break;
                    case ModeType.PuchiChara:
                    CurrentMaxIndex = TJAPlayer3.Skin.Puchichara_Ptn;
                    break;
                    case ModeType.DanTitle:
                    {
                        int amount = 1;
                        if (TJAPlayer3.SaveFileInstances[CurrentPlayer].data.DanTitles != null)
                            amount += TJAPlayer3.SaveFileInstances[CurrentPlayer].data.DanTitles.Count;

                        this.ttkDanTitles = new CActSelect曲リスト.TitleTextureKey[amount];

                        // Silver Shinjin (default rank) always avaliable by default
                        this.ttkDanTitles[0] = new CActSelect曲リスト.TitleTextureKey("新人", this.MenuFont, Color.White, Color.Black, 1000);

                        int idx = 1;
                        if (TJAPlayer3.SaveFileInstances[CurrentPlayer].data.DanTitles != null)
                        {
                            foreach (var item in TJAPlayer3.SaveFileInstances[CurrentPlayer].data.DanTitles)
                            {
                                if (item.Value.isGold == true)
                                    this.ttkDanTitles[idx] = new CActSelect曲リスト.TitleTextureKey(item.Key, this.MenuFont, Color.Gold, Color.Black, 1000);
                                else 
                                    this.ttkDanTitles[idx] = new CActSelect曲リスト.TitleTextureKey(item.Key, this.MenuFont, Color.White, Color.Black, 1000);
                                idx++;
                            }
                        }

                        CurrentMaxIndex = amount;
                    }
                    break;
                    case ModeType.SubTitle:
                    {
                        int amount = 1;
                        if (TJAPlayer3.SaveFileInstances[CurrentPlayer].data.NamePlateTitles != null)
                            amount += TJAPlayer3.SaveFileInstances[CurrentPlayer].data.NamePlateTitles.Count;

                        this.ttkTitles = new CActSelect曲リスト.TitleTextureKey[amount];
                        this.titlesKeys = new string[amount];

                        // Wood shojinsha (default title) always avaliable by default
                        this.ttkTitles[0] = new CActSelect曲リスト.TitleTextureKey("初心者", this.MenuFont, Color.Black, Color.Transparent, 1000);
                        this.titlesKeys[0] = "初心者";

                        int idx = 1;
                        if (TJAPlayer3.SaveFileInstances[CurrentPlayer].data.NamePlateTitles != null)
                        {
                            foreach (var item in TJAPlayer3.SaveFileInstances[CurrentPlayer].data.NamePlateTitles)
                            {
                                this.ttkTitles[idx] = new CActSelect曲リスト.TitleTextureKey(item.Value.cld.GetString(item.Key), this.MenuFont, Color.Black, Color.Transparent, 1000);
                                this.titlesKeys[idx] = item.Key;
                                idx++;
                            }
                        }

                        CurrentMaxIndex = amount;
                    }
                    break;
                }
                CurrentIndex = 0;
                break;
            }
        }

        private void ChangeIndex(int change)
        {
            CurrentIndex += change;

            if (CurrentIndex < 0) CurrentIndex = CurrentMaxIndex - 1;
            else if (CurrentIndex >= CurrentMaxIndex) CurrentIndex = 0;
            if (CurrentState == SelectableInfo.Select)
            {
                switch(CurrentMode)
                {
                    case ModeType.PuchiChara:
                    tUpdateUnlockableTextPuchi();
                    break;
                    case ModeType.Chara:
                    tUpdateUnlockableTextChara();
                    break;
                    case ModeType.DanTitle:
                    break;
                    case ModeType.SubTitle:
                    break;
                }
            }
        }

        public void Open()
        {
            InFade = new CCounter(0, 255, 1.0, TJAPlayer3.Timer);
            IsOpend = true;
            CurrentMode = ModeType.None;

            SetState(SelectableInfo.PlayerSelect);
        }

        public void Close()
        {
            IsOpend = false;
        }

        public override void Activate()
        {
            InFade = new CCounter();
            CharaBoxAnime = new CCounter();

            for(int i = 0; i < 5; i++)
            {
                MenuTitleKeys[i] = new CActSelect曲リスト.TitleTextureKey(CLangManager.LangInstance.GetString(1030 + i), MenuFont, Color.White, Color.Black, 9999);
            }
            
            ttkPuchiCharaNames = new CActSelect曲リスト.TitleTextureKey[TJAPlayer3.Skin.Puchichara_Ptn];
            ttkPuchiCharaAuthors = new CActSelect曲リスト.TitleTextureKey[TJAPlayer3.Skin.Puchichara_Ptn];

            for (int i = 0; i < TJAPlayer3.Skin.Puchichara_Ptn; i++)
            {
                var textColor = HRarity.tRarityToColor(TJAPlayer3.Tx.Puchichara[i].metadata.Rarity);
                ttkPuchiCharaNames[i] = new CActSelect曲リスト.TitleTextureKey(TJAPlayer3.Tx.Puchichara[i].metadata.tGetName(), this.MenuFont, textColor, Color.Black, 1000);
                ttkPuchiCharaAuthors[i] = new CActSelect曲リスト.TitleTextureKey(TJAPlayer3.Tx.Puchichara[i].metadata.tGetAuthor(), this.MenuFont, Color.White, Color.Black, 1000);
            }

            
            ttkCharacterAuthors = new CActSelect曲リスト.TitleTextureKey[TJAPlayer3.Skin.Characters_Ptn];
            ttkCharacterNames = new CActSelect曲リスト.TitleTextureKey[TJAPlayer3.Skin.Characters_Ptn];

            for (int i = 0; i < TJAPlayer3.Skin.Characters_Ptn; i++)
            {
                var textColor = HRarity.tRarityToColor(TJAPlayer3.Tx.Characters[i].metadata.Rarity);
                ttkCharacterNames[i] = new CActSelect曲リスト.TitleTextureKey(TJAPlayer3.Tx.Characters[i].metadata.tGetName(), this.MenuFont, textColor, Color.Black, 1000);
                ttkCharacterAuthors[i] = new CActSelect曲リスト.TitleTextureKey(TJAPlayer3.Tx.Characters[i].metadata.tGetAuthor(), this.MenuFont, Color.White, Color.Black, 1000);
            }
            

            base.Activate();
        }

        public override void DeActivate()
        {
            
            base.DeActivate();
        }

        public override void CreateManagedResource()
        {
            this.MenuFont = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.Heya_Font_Scale);
            base.CreateManagedResource();
        }

        public override void ReleaseManagedResource()
        {
            MenuFont.Dispose();

            base.ReleaseManagedResource();
        }

        public override int Draw()
        {
			if ((TJAPlayer3.Pad.bPressedDGB(EPad.Decide)) || ((TJAPlayer3.ConfigIni.bEnterがキー割り当てのどこにも使用されていない && TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return))))
            {
                switch(CurrentState)
                {
                    case SelectableInfo.PlayerSelect:
                    {
                        switch(CurrentIndex)
                        {
                            case 0:
                            Close();
                            TJAPlayer3.Skin.soundCancelSFX.tPlay();
                            break;
                            default:
                            {
                                CurrentPlayer = TJAPlayer3.GetActualPlayer(CurrentIndex - 1);
                                SetState(SelectableInfo.ModeSelect);
                                TJAPlayer3.Skin.soundDecideSFX.tPlay();
                            }
                            break;
                        }
                    }
                    break;
                    case SelectableInfo.ModeSelect:
                    {
                        switch(CurrentIndex)
                        {
                            case 0:
                            SetState(SelectableInfo.PlayerSelect);
                            TJAPlayer3.Skin.soundCancelSFX.tPlay();
                            break;
                            default:
                            {
                                SetState(SelectableInfo.Select);
                                TJAPlayer3.Skin.soundDecideSFX.tPlay();
                            }
                            break;
                        }
                    }
                    break;
                    case SelectableInfo.Select:
                    {
                        switch(CurrentMode)
                        {
                            case ModeType.PuchiChara:
                            {
                                var ess = this.tSelectPuchi();

                                if (ess == ESelectStatus.SELECTED)
                                {
                                    //PuchiChara.tGetPuchiCharaIndexByName(p);
                                    //TJAPlayer3.NamePlateConfig.data.PuchiChara[iPlayer] = TJAPlayer3.Skin.Puchicharas_Name[iPuchiCharaCurrent];// iPuchiCharaCurrent;
                                    //TJAPlayer3.NamePlateConfig.tApplyHeyaChanges();
                                    TJAPlayer3.SaveFileInstances[CurrentPlayer].data.PuchiChara = TJAPlayer3.Skin.Puchicharas_Name[CurrentIndex];// iPuchiCharaCurrent;
                                    TJAPlayer3.SaveFileInstances[CurrentPlayer].tApplyHeyaChanges();
                                    TJAPlayer3.Skin.soundDecideSFX.tPlay();
                                    TJAPlayer3.Tx.Puchichara[CurrentIndex].welcome.tPlay();

                                    SetState(SelectableInfo.PlayerSelect);
                                }
                                else if (ess == ESelectStatus.SUCCESS)
                                {
                                    //TJAPlayer3.NamePlateConfig.data.UnlockedPuchicharas[iPlayer].Add(TJAPlayer3.Skin.Puchicharas_Name[iPuchiCharaCurrent]);
                                    //TJAPlayer3.NamePlateConfig.tSpendCoins(TJAPlayer3.Tx.Puchichara[iPuchiCharaCurrent].unlock.Values[0], iPlayer);
                                    TJAPlayer3.SaveFileInstances[CurrentPlayer].data.UnlockedPuchicharas.Add(TJAPlayer3.Skin.Puchicharas_Name[CurrentIndex]);
                                    TJAPlayer3.SaveFileInstances[CurrentPlayer].tSpendCoins(TJAPlayer3.Tx.Puchichara[CurrentIndex].unlock.Values[0]);
                                    TJAPlayer3.Skin.soundDecideSFX.tPlay();
                                }
                                else 
                                {
                                    TJAPlayer3.Skin.soundError.tPlay();
                                }
                            }
                            break;
                            case ModeType.Chara:
                            {
                                var ess = this.tSelectChara();

                                if (ess == ESelectStatus.SELECTED)
                                {
                                    //TJAPlayer3.Tx.Loading?.t2D描画(18, 7);

                                    // Reload character, a bit time expensive but with a O(N) memory complexity instead of O(N * M)
                                    TJAPlayer3.Tx.ReloadCharacter(TJAPlayer3.SaveFileInstances[CurrentPlayer].data.Character, CurrentIndex, CurrentPlayer);
                                    TJAPlayer3.SaveFileInstances[CurrentPlayer].data.Character = CurrentIndex;

                                    // Update the character
                                    TJAPlayer3.SaveFileInstances[CurrentPlayer].tUpdateCharacterName(TJAPlayer3.Skin.Characters_DirName[CurrentIndex]);

                                    // Welcome voice using Sanka
                                    TJAPlayer3.Skin.soundDecideSFX.tPlay();
                                    TJAPlayer3.Skin.voiceTitleSanka[CurrentPlayer]?.tPlay();

                                    CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.NORMAL);

                                    TJAPlayer3.SaveFileInstances[CurrentPlayer].tApplyHeyaChanges();

                                    SetState(SelectableInfo.PlayerSelect);
                                    CurrentMode = ModeType.None;
                                }
                                else if (ess == ESelectStatus.SUCCESS)
                                {
                                    TJAPlayer3.SaveFileInstances[CurrentPlayer].data.UnlockedCharacters.Add(TJAPlayer3.Skin.Characters_DirName[CurrentIndex]);
                                    TJAPlayer3.SaveFileInstances[CurrentPlayer].tSpendCoins(TJAPlayer3.Tx.Characters[CurrentIndex].unlock.Values[0]);
                                    TJAPlayer3.Skin.soundDecideSFX.tPlay();
                                }
                                else 
                                {
                                    TJAPlayer3.Skin.soundError.tPlay();
                                }
                            }
                            break;
                            case ModeType.DanTitle:
                            {
                                bool iG = false;
                                int cs = 0;

                                if (CurrentIndex > 0)
                                {
                                    iG = TJAPlayer3.SaveFileInstances[CurrentPlayer].data.DanTitles[this.ttkDanTitles[CurrentIndex].str文字].isGold;
                                    cs = TJAPlayer3.SaveFileInstances[CurrentPlayer].data.DanTitles[this.ttkDanTitles[CurrentIndex].str文字].clearStatus;
                                }

                                TJAPlayer3.SaveFileInstances[CurrentPlayer].data.Dan = this.ttkDanTitles[CurrentIndex].str文字;
                                TJAPlayer3.SaveFileInstances[CurrentPlayer].data.DanGold = iG;
                                TJAPlayer3.SaveFileInstances[CurrentPlayer].data.DanType = cs;

                                TJAPlayer3.NamePlate.tNamePlateRefreshTitles(CurrentPlayer);

                                TJAPlayer3.SaveFileInstances[CurrentPlayer].tApplyHeyaChanges();

                                TJAPlayer3.Skin.soundDecideSFX.tPlay();
                                SetState(SelectableInfo.PlayerSelect);
                            }
                            break;
                            case ModeType.SubTitle:
                            {
                                TJAPlayer3.SaveFileInstances[CurrentPlayer].data.Title = this.ttkTitles[CurrentIndex].str文字;

                                if (TJAPlayer3.SaveFileInstances[CurrentPlayer].data.NamePlateTitles != null
                                    && TJAPlayer3.SaveFileInstances[CurrentPlayer].data.NamePlateTitles.ContainsKey(this.titlesKeys[CurrentIndex]))
                                    TJAPlayer3.SaveFileInstances[CurrentPlayer].data.TitleType = TJAPlayer3.SaveFileInstances[CurrentPlayer].data.NamePlateTitles[this.titlesKeys[CurrentIndex]].iType;
                                else if (CurrentIndex == 0)
                                    TJAPlayer3.SaveFileInstances[CurrentPlayer].data.TitleType = 0;
                                else
                                    TJAPlayer3.SaveFileInstances[CurrentPlayer].data.TitleType = -1;

                                TJAPlayer3.NamePlate.tNamePlateRefreshTitles(CurrentPlayer);

                                TJAPlayer3.SaveFileInstances[CurrentPlayer].tApplyHeyaChanges();

                                TJAPlayer3.Skin.soundDecideSFX.tPlay();
                                SetState(SelectableInfo.PlayerSelect);
                            }
                            break;
                        }
                    }
                    break;
                }
            }
            else if ((TJAPlayer3.Pad.bPressedDGB(EPad.Cancel) || TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape)))
            {
                Close();
                TJAPlayer3.Skin.soundCancelSFX.tPlay();
            }
            else if (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LeftChange)
				|| TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow))
            {
                ChangeIndex(-1);
                TJAPlayer3.Skin.soundChangeSFX.tPlay();
            }
			else if (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RightChange)
				|| TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow))
            {
                ChangeIndex(1);
                TJAPlayer3.Skin.soundChangeSFX.tPlay();
            }

            InFade.Tick();
            
			if (TJAPlayer3.Tx.Tile_Black != null)
			{
                TJAPlayer3.Tx.Tile_Black.Opacity = InFade.CurrentValue / 2;
				for (int i = 0; i <= (SampleFramework.GameWindowSize.Width / TJAPlayer3.Tx.Tile_Black.szTextureSize.Width); i++)		// #23510 2010.10.31 yyagi: change "clientSize.Width" to "640" to fix FIFO drawing size
				{
					for (int j = 0; j <= (SampleFramework.GameWindowSize.Height / TJAPlayer3.Tx.Tile_Black.szTextureSize.Height); j++)	// #23510 2010.10.31 yyagi: change "clientSize.Height" to "480" to fix FIFO drawing size
					{
                        TJAPlayer3.Tx.Tile_Black.t2D描画( i * TJAPlayer3.Tx.Tile_Black.szTextureSize.Width, j * TJAPlayer3.Tx.Tile_Black.szTextureSize.Height);
					}
				}
			}


            switch (CurrentState)
            {
                case SelectableInfo.PlayerSelect:
                if (CurrentIndex == 0)
                {
                    TJAPlayer3.Tx.NewHeya_Close_Select.t2D描画(TJAPlayer3.Skin.SongSelect_NewHeya_Close_Select[0], TJAPlayer3.Skin.SongSelect_NewHeya_Close_Select[1]);
                }
                else 
                {
                    TJAPlayer3.Tx.NewHeya_PlayerPlate_Select.t2D描画(TJAPlayer3.Skin.SongSelect_NewHeya_PlayerPlate_X[CurrentIndex - 1], TJAPlayer3.Skin.SongSelect_NewHeya_PlayerPlate_Y[CurrentIndex - 1]);
                }
                break;
                case SelectableInfo.ModeSelect:
                {
                    TJAPlayer3.Tx.NewHeya_ModeBar_Select.t2D描画(TJAPlayer3.Skin.SongSelect_NewHeya_ModeBar_X[CurrentIndex], TJAPlayer3.Skin.SongSelect_NewHeya_ModeBar_Y[CurrentIndex]);
                }
                break;
                case SelectableInfo.Select:
                {
                    switch(CurrentMode)
                    {
                        case ModeType.Chara:
                        for(int i = 1; i < TJAPlayer3.Skin.SongSelect_NewHeya_Box_Count - 1; i++)
                        {
                            int x = TJAPlayer3.Skin.SongSelect_NewHeya_Box_X[i];
                            int y = TJAPlayer3.Skin.SongSelect_NewHeya_Box_Y[i];
                            int index = i - (TJAPlayer3.Skin.SongSelect_NewHeya_Box_Count / 2) + CurrentIndex;
                            while (index < 0) 
                            {
                                index += CurrentMaxIndex;
                            }
                            while (index >= CurrentMaxIndex) 
                            {
                                index -= CurrentMaxIndex; 
                            }
                            TJAPlayer3.Tx.NewHeya_Box.t2D描画(x, y);


                            float charaRatioX = 1.0f;
                            float charaRatioY = 1.0f;
                            if (TJAPlayer3.Skin.Characters_Resolution[index] != null)
                            {
                                charaRatioX = TJAPlayer3.Skin.Resolution[0] / (float)TJAPlayer3.Skin.Characters_Resolution[index][0];
                                charaRatioY = TJAPlayer3.Skin.Resolution[1] / (float)TJAPlayer3.Skin.Characters_Resolution[index][1];
                            }

                            if (TJAPlayer3.Tx.Characters_Heya_Preview[index] != null)
                            {
                                TJAPlayer3.Tx.Characters_Heya_Preview[index].vcScaleRatio.X = charaRatioX;
                                TJAPlayer3.Tx.Characters_Heya_Preview[index].vcScaleRatio.Y = charaRatioY;
                            }

                            TJAPlayer3.Tx.Characters_Heya_Preview[index]?.t2D拡大率考慮中央基準描画(x + TJAPlayer3.Skin.SongSelect_NewHeya_Box_Chara_Offset[0], y + TJAPlayer3.Skin.SongSelect_NewHeya_Box_Chara_Offset[1]);
                            TJAPlayer3.Tx.Characters_Heya_Preview[index]?.tUpdateColor4(CConversion.ColorToColor4(Color.White));
                            
                            if (ttkCharacterNames[index] != null)
                            {
                                CTexture tmpTex = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(ttkCharacterNames[index]);

                                tmpTex.t2D拡大率考慮上中央基準描画(x + TJAPlayer3.Skin.SongSelect_NewHeya_Box_Name_Offset[0], y + TJAPlayer3.Skin.SongSelect_NewHeya_Box_Name_Offset[1]);
                            }

                            if (ttkCharacterAuthors[index] != null)
                            {
                                CTexture tmpTex = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(ttkCharacterAuthors[index]);

                                tmpTex.t2D拡大率考慮上中央基準描画(x + TJAPlayer3.Skin.SongSelect_NewHeya_Box_Author_Offset[0], y + TJAPlayer3.Skin.SongSelect_NewHeya_Box_Author_Offset[1]);
                            }

                            if (TJAPlayer3.Tx.Characters[index].unlock != null
                                && !TJAPlayer3.SaveFileInstances[CurrentPlayer].data.UnlockedCharacters.Contains(TJAPlayer3.Skin.Characters_DirName[index]))
                            {
                                TJAPlayer3.Tx.NewHeya_Lock?.t2D描画(x + TJAPlayer3.Skin.SongSelect_NewHeya_Lock_Offset[0], y + TJAPlayer3.Skin.SongSelect_NewHeya_Lock_Offset[1]);
                                
                                if (this.ttkInfoSection != null)
                                    TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(this.ttkInfoSection)
                                        .t2D拡大率考慮上中央基準描画(x + TJAPlayer3.Skin.SongSelect_NewHeya_InfoSection_Offset[0], y + TJAPlayer3.Skin.SongSelect_NewHeya_InfoSection_Offset[1]);
                            }
                        }
                        break;
                        case ModeType.PuchiChara:
                        for(int i = 1; i < TJAPlayer3.Skin.SongSelect_NewHeya_Box_Count - 1; i++)
                        {
                            int x = TJAPlayer3.Skin.SongSelect_NewHeya_Box_X[i];
                            int y = TJAPlayer3.Skin.SongSelect_NewHeya_Box_Y[i];
                            int index = i - (TJAPlayer3.Skin.SongSelect_NewHeya_Box_Count / 2) + CurrentIndex;
                            while (index < 0) 
                            {
                                index += CurrentMaxIndex;
                            }
                            while (index >= CurrentMaxIndex) 
                            {
                                index -= CurrentMaxIndex; 
                            }
                            TJAPlayer3.Tx.NewHeya_Box.t2D描画(x, y);

                            int puriColumn = index % 5;
                            int puriRow = index / 5;

                            if (TJAPlayer3.Tx.Puchichara[index].tx != null)
                            {
                                float puchiScale = TJAPlayer3.Skin.Resolution[1] / 1080.0f;

                                TJAPlayer3.Tx.Puchichara[index].tx.vcScaleRatio.X = puchiScale;
                                TJAPlayer3.Tx.Puchichara[index].tx.vcScaleRatio.Y = puchiScale;
                            }

                            TJAPlayer3.Tx.Puchichara[index].tx?.t2D拡大率考慮中央基準描画(x + TJAPlayer3.Skin.SongSelect_NewHeya_Box_Chara_Offset[0], 
                                y + TJAPlayer3.Skin.SongSelect_NewHeya_Box_Chara_Offset[1] + (int)(TJAPlayer3.stageSongSelect.PuchiChara.sineY), 
                                new Rectangle((TJAPlayer3.stageSongSelect.PuchiChara.Counter.CurrentValue + 2 * puriColumn) * TJAPlayer3.Skin.Game_PuchiChara[0], 
                                puriRow * TJAPlayer3.Skin.Game_PuchiChara[1], 
                                TJAPlayer3.Skin.Game_PuchiChara[0], 
                                TJAPlayer3.Skin.Game_PuchiChara[1]));

                            TJAPlayer3.Tx.Puchichara[index].tx?.tUpdateColor4(CConversion.ColorToColor4(Color.White));


                            if (ttkCharacterNames[index] != null)
                            {
                                CTexture tmpTex = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(ttkPuchiCharaNames[index]);

                                tmpTex.t2D拡大率考慮上中央基準描画(x + TJAPlayer3.Skin.SongSelect_NewHeya_Box_Name_Offset[0], y + TJAPlayer3.Skin.SongSelect_NewHeya_Box_Name_Offset[1]);
                            }

                            if (ttkCharacterAuthors[index] != null)
                            {
                                CTexture tmpTex = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(ttkPuchiCharaAuthors[index]);

                                tmpTex.t2D拡大率考慮上中央基準描画(x + TJAPlayer3.Skin.SongSelect_NewHeya_Box_Author_Offset[0], y + TJAPlayer3.Skin.SongSelect_NewHeya_Box_Author_Offset[1]);
                            }

                            if (TJAPlayer3.Tx.Puchichara[index].unlock != null
                                && !TJAPlayer3.SaveFileInstances[CurrentPlayer].data.UnlockedPuchicharas.Contains(TJAPlayer3.Skin.Puchicharas_Name[index]))
                            {
                                TJAPlayer3.Tx.NewHeya_Lock?.t2D描画(x + TJAPlayer3.Skin.SongSelect_NewHeya_Lock_Offset[0], y + TJAPlayer3.Skin.SongSelect_NewHeya_Lock_Offset[1]);
                                
                                if (this.ttkInfoSection != null)
                                    TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(this.ttkInfoSection)
                                        .t2D拡大率考慮上中央基準描画(x + TJAPlayer3.Skin.SongSelect_NewHeya_InfoSection_Offset[0], y + TJAPlayer3.Skin.SongSelect_NewHeya_InfoSection_Offset[1]);
                            }
                        }
                        break;
                        case ModeType.SubTitle:
                        for(int i = 1; i < TJAPlayer3.Skin.SongSelect_NewHeya_Box_Count - 1; i++)
                        {
                            int x = TJAPlayer3.Skin.SongSelect_NewHeya_Box_X[i];
                            int y = TJAPlayer3.Skin.SongSelect_NewHeya_Box_Y[i];
                            int index = i - (TJAPlayer3.Skin.SongSelect_NewHeya_Box_Count / 2) + CurrentIndex;
                            while (index < 0) 
                            {
                                index += CurrentMaxIndex;
                            }
                            while (index >= CurrentMaxIndex) 
                            {
                                index -= CurrentMaxIndex; 
                            }
                            CTexture tmpTex = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(this.ttkTitles[index]);

                            if (i != 0)
                            {
                                tmpTex.color4 = CConversion.ColorToColor4(Color.DarkGray);
                            }
                            else
                            {
                                tmpTex.color4 = CConversion.ColorToColor4(Color.White);
                            }

                            TJAPlayer3.Tx.NewHeya_Box.t2D描画(x, y);

                            x += TJAPlayer3.Skin.SongSelect_NewHeya_Box_Chara_Offset[0];
                            y += TJAPlayer3.Skin.SongSelect_NewHeya_Box_Chara_Offset[1];

                            int iType = -1;

                            if (TJAPlayer3.SaveFileInstances[CurrentPlayer].data.NamePlateTitles != null &&
                                TJAPlayer3.SaveFileInstances[CurrentPlayer].data.NamePlateTitles.ContainsKey(this.titlesKeys[index]))
                                iType = TJAPlayer3.SaveFileInstances[CurrentPlayer].data.NamePlateTitles[this.titlesKeys[index]].iType;
                            else if (index == 0)
                                iType = 0;

                            if (iType >= 0 && iType < TJAPlayer3.Skin.Config_NamePlate_Ptn_Title)
                            {
                                TJAPlayer3.Tx.NamePlate_Title[iType][TJAPlayer3.NamePlate.ctAnimatedNamePlateTitle.CurrentValue % TJAPlayer3.Skin.Config_NamePlate_Ptn_Title_Boxes[iType]].t2D拡大率考慮上中央基準描画(
                                    x,
                                    y);
                            } 

                            tmpTex.t2D拡大率考慮上中央基準描画(x + TJAPlayer3.Skin.Heya_Side_Menu_Font_Offset[0], y + TJAPlayer3.Skin.Heya_Side_Menu_Font_Offset[1]);

                        }
                        break;
                        case ModeType.DanTitle:
                        for(int i = 1; i < TJAPlayer3.Skin.SongSelect_NewHeya_Box_Count - 1; i++)
                        {
                            int x = TJAPlayer3.Skin.SongSelect_NewHeya_Box_X[i];
                            int y = TJAPlayer3.Skin.SongSelect_NewHeya_Box_Y[i];
                            int index = i - (TJAPlayer3.Skin.SongSelect_NewHeya_Box_Count / 2) + CurrentIndex;
                            while (index < 0) 
                            {
                                index += CurrentMaxIndex;
                            }
                            while (index >= CurrentMaxIndex) 
                            {
                                index -= CurrentMaxIndex; 
                            }
                            CTexture tmpTex = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(this.ttkDanTitles[index]);

                            if (i != 0)
                            {
                                tmpTex.color4 = CConversion.ColorToColor4(Color.DarkGray);
                                TJAPlayer3.Tx.NamePlateBase.color4 = CConversion.ColorToColor4(Color.DarkGray);
                            }
                            else
                            {
                                tmpTex.color4 = CConversion.ColorToColor4(Color.White);
                                TJAPlayer3.Tx.NamePlateBase.color4 = CConversion.ColorToColor4(Color.White);
                            }

                            TJAPlayer3.Tx.NewHeya_Box.t2D描画(x, y);

                            x += TJAPlayer3.Skin.SongSelect_NewHeya_Box_Chara_Offset[0];
                            y += TJAPlayer3.Skin.SongSelect_NewHeya_Box_Chara_Offset[1];

                            int danGrade = 0;
                            if (index > 0)
                            {
                                danGrade = TJAPlayer3.SaveFileInstances[CurrentPlayer].data.DanTitles[this.ttkDanTitles[index].str文字].clearStatus;
                            }

                            TJAPlayer3.NamePlate.tNamePlateDisplayNamePlateBase(
                                x - TJAPlayer3.Tx.NamePlateBase.szTextureSize.Width / 2, 
                                y - TJAPlayer3.Tx.NamePlateBase.szTextureSize.Height / 24, 
                                (8 + danGrade));
                            TJAPlayer3.Tx.NamePlateBase.color4 = CConversion.ColorToColor4(Color.White);

                            tmpTex.t2D拡大率考慮上中央基準描画(x + TJAPlayer3.Skin.Heya_Side_Menu_Font_Offset[0], y + TJAPlayer3.Skin.Heya_Side_Menu_Font_Offset[1]);

                        }
                        break;
                    }
                }
                break;
            }

            TJAPlayer3.Tx.NewHeya_Close.t2D描画(0, 0);

            for(int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                TJAPlayer3.Tx.NewHeya_PlayerPlate[TJAPlayer3.GetActualPlayer(i)].t2D描画(TJAPlayer3.Skin.SongSelect_NewHeya_PlayerPlate_X[i], TJAPlayer3.Skin.SongSelect_NewHeya_PlayerPlate_Y[i]);
            }
            
            for(int i = 0; i < 5; i++)
            {
                TJAPlayer3.Tx.NewHeya_ModeBar.t2D描画(TJAPlayer3.Skin.SongSelect_NewHeya_ModeBar_X[i], TJAPlayer3.Skin.SongSelect_NewHeya_ModeBar_Y[i]);
                int title_x = TJAPlayer3.Skin.SongSelect_NewHeya_ModeBar_X[i] + TJAPlayer3.Skin.SongSelect_NewHeya_ModeBar_Font_Offset[0];
                int title_y = TJAPlayer3.Skin.SongSelect_NewHeya_ModeBar_Y[i] + TJAPlayer3.Skin.SongSelect_NewHeya_ModeBar_Font_Offset[1];
                TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(MenuTitleKeys[i], false).t2D拡大率考慮中央基準描画(title_x, title_y);
            }

            return base.Draw();
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

        private ESelectStatus tSelectPuchi()
        {
            // Add "If unlocked" to select directly

            if (TJAPlayer3.Tx.Puchichara[CurrentIndex].unlock != null
                && !TJAPlayer3.SaveFileInstances[CurrentPlayer].data.UnlockedPuchicharas.Contains(TJAPlayer3.Skin.Puchicharas_Name[CurrentIndex]))
            {
                (bool, string?) response = TJAPlayer3.Tx.Puchichara[CurrentIndex].unlock.tConditionMetWrapper(TJAPlayer3.SaveFile);
                //tConditionMet(
                //new int[] { TJAPlayer3.SaveFileInstances[TJAPlayer3.SaveFile].data.Medals });

                Color responseColor = (response.Item1) ? Color.Lime : Color.Red;

                // Send coins here for the unlock, considering that only coin-paid puchicharas can be unlocked directly from the Heya menu

                this.ttkInfoSection = new CActSelect曲リスト.TitleTextureKey(response.Item2 ?? this.ttkInfoSection.str文字, this.MenuFont, responseColor, Color.Black, 1000);

                return (response.Item1) ? ESelectStatus.SUCCESS : ESelectStatus.FAILED;
            }

            this.ttkInfoSection = null;
            return ESelectStatus.SELECTED;
        }

        private void tUpdateUnlockableTextPuchi()
        {
            #region [Check unlockable]

            if (TJAPlayer3.Tx.Puchichara[CurrentIndex].unlock != null
                && !TJAPlayer3.SaveFileInstances[CurrentPlayer].data.UnlockedPuchicharas.Contains(TJAPlayer3.Skin.Puchicharas_Name[CurrentIndex]))
            {
                this.ttkInfoSection = new CActSelect曲リスト.TitleTextureKey(TJAPlayer3.Tx.Puchichara[CurrentIndex].unlock.tConditionMessage()
                    , this.MenuFont, Color.White, Color.Black, 1000);
            }
            else
                this.ttkInfoSection = null;

            #endregion
        }
        private void tUpdateUnlockableTextChara()
        {
            #region [Check unlockable]

            if (TJAPlayer3.Tx.Characters[CurrentIndex].unlock != null
                && !TJAPlayer3.SaveFileInstances[CurrentPlayer].data.UnlockedCharacters.Contains(TJAPlayer3.Skin.Characters_DirName[CurrentIndex]))
            {
                this.ttkInfoSection = new CActSelect曲リスト.TitleTextureKey(TJAPlayer3.Tx.Characters[CurrentIndex].unlock.tConditionMessage()
                    , this.MenuFont, Color.White, Color.Black, 1000);
            }
            else
                this.ttkInfoSection = null;

            #endregion
        }

        private ESelectStatus tSelectChara()
        {
            // Add "If unlocked" to select directly

            if (TJAPlayer3.Tx.Characters[CurrentIndex].unlock != null
                && !TJAPlayer3.SaveFileInstances[CurrentPlayer].data.UnlockedCharacters.Contains(TJAPlayer3.Skin.Characters_DirName[CurrentIndex]))
            {
                (bool, string?) response = TJAPlayer3.Tx.Characters[CurrentIndex].unlock.tConditionMetWrapper(TJAPlayer3.SaveFile);
                    //TJAPlayer3.Tx.Characters[iCharacterCurrent].unlock.tConditionMet(
                    //new int[] { TJAPlayer3.SaveFileInstances[TJAPlayer3.SaveFile].data.Medals });

                Color responseColor = (response.Item1) ? Color.Lime : Color.Red;

                // Send coins here for the unlock, considering that only coin-paid puchicharas can be unlocked directly from the Heya menu

                this.ttkInfoSection = new CActSelect曲リスト.TitleTextureKey(response.Item2 ?? this.ttkInfoSection.str文字, this.MenuFont, responseColor, Color.Black, 1000);

                return (response.Item1) ? ESelectStatus.SUCCESS : ESelectStatus.FAILED;
            }

            this.ttkInfoSection = null;
            return ESelectStatus.SELECTED;
        }
    }
}
