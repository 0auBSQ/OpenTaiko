using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;
using System.Drawing;
using static TJAPlayer3.CActSelect曲リスト;

namespace TJAPlayer3
{
    internal class Modal
    {
        public Modal(EModalType mt, int ra, params object[] re)
        {
            modalType = mt;
            rarity = ra;
            reference = re;
            _isSet = false;
            // TODO: Add an int (?) or string to find the Puchichara/Character/Song asset to display it
        }

        public void tSetupModal()
        {
            CTexture[] arrRef;

            if (modalFormat == EModalFormat.Half)
                arrRef = TJAPlayer3.Tx.Modal_Half;
            else if (modalFormat == EModalFormat.Half_4P)
                arrRef = TJAPlayer3.Tx.Modal_Half_4P;
            else if (modalFormat == EModalFormat.Half_5P)
                arrRef = TJAPlayer3.Tx.Modal_Half_5P;
            else
                arrRef = TJAPlayer3.Tx.Modal_Full;

            if (modalType == EModalType.Coin)
                _box = arrRef[arrRef.Length - 1];
            else
            {
                int usedTex = Math.Max(0, Math.Min(arrRef.Length - 2, rarity));
                _box = arrRef[usedTex];
            }

            /*
            _boxRect = new Rectangle(
                (modalFormat == EModalFormat.Full || player == 0)
                    ? 0
                    : _box.szテクスチャサイズ.Width / 2,
                0,
                (modalFormat == EModalFormat.Full)
                    ? _box.szテクスチャサイズ.Width
                    : _box.szテクスチャサイズ.Width / 2,
                _box.szテクスチャサイズ.Height);
            */

            _boxRect = new Rectangle(
                (modalFormat == EModalFormat.Full || player == 0) ? 0 : _box.szTextureSize.Width / 2,
                0,
                (modalFormat == EModalFormat.Full) ? _box.szTextureSize.Width : _box.szTextureSize.Width / 2,
                _box.szTextureSize.Height / (((TJAPlayer3.ConfigIni.nPlayerCount - 1) / 2) + 1));

            tGenerateTextures();

            _isSet = true;
        }

        public void tDisplayModal()
        {
            if (_isSet == true)
            {
                _box?.t2D描画(_boxRect.Width * (player % 2), _boxRect.Height * (player / 2), _boxRect);

                int[] title_x;
                int[] title_y;
                int[] text_x;
                int[] text_y;
                int moveX;
                int moveY;

                if (modalFormat == EModalFormat.Full)
                {
                    title_x = new int[] { TJAPlayer3.Skin.Modal_Title_Full[0] };
                    title_y = new int[] { TJAPlayer3.Skin.Modal_Title_Full[1] };

                    text_x = new int[] { TJAPlayer3.Skin.Modal_Text_Full[0] };
                    text_y = new int[] { TJAPlayer3.Skin.Modal_Text_Full[1] };

                    moveX = TJAPlayer3.Skin.Modal_Text_Full_Move[0];
                    moveY = TJAPlayer3.Skin.Modal_Text_Full_Move[1];
                }
                else if (modalFormat == EModalFormat.Half)
                {
                    title_x = TJAPlayer3.Skin.Modal_Title_Half_X;
                    title_y = TJAPlayer3.Skin.Modal_Title_Half_Y;

                    text_x = TJAPlayer3.Skin.Modal_Text_Half_X;
                    text_y = TJAPlayer3.Skin.Modal_Text_Half_Y;

                    moveX = TJAPlayer3.Skin.Modal_Text_Half_Move[0];
                    moveY = TJAPlayer3.Skin.Modal_Text_Half_Move[1];
                }
                else if (modalFormat == EModalFormat.Half_4P)
                {
                    title_x = TJAPlayer3.Skin.Modal_Title_Half_X_4P;
                    title_y = TJAPlayer3.Skin.Modal_Title_Half_Y_4P;

                    text_x = TJAPlayer3.Skin.Modal_Text_Half_X_4P;
                    text_y = TJAPlayer3.Skin.Modal_Text_Half_Y_4P;

                    moveX = TJAPlayer3.Skin.Modal_Text_Half_Move_4P[0];
                    moveY = TJAPlayer3.Skin.Modal_Text_Half_Move_4P[1];
                }
                else// 5P
                {
                    title_x = TJAPlayer3.Skin.Modal_Title_Half_X_5P;
                    title_y = TJAPlayer3.Skin.Modal_Title_Half_Y_5P;

                    text_x = TJAPlayer3.Skin.Modal_Text_Half_X_5P;
                    text_y = TJAPlayer3.Skin.Modal_Text_Half_Y_5P;

                    moveX = TJAPlayer3.Skin.Modal_Text_Half_Move_5P[0];
                    moveY = TJAPlayer3.Skin.Modal_Text_Half_Move_5P[1];
                }

                /*
                Point[] Pos = new Point[]
                {
                    (modalFormat == EModalFormat.Full) ? new Point(TJAPlayer3.Skin.Modal_Title_Full[0], TJAPlayer3.Skin.Modal_Title_Full[1]) : new Point(TJAPlayer3.Skin.Modal_Title_Half_X[player], TJAPlayer3.Skin.Modal_Title_Half_Y[player]), // title
                    (modalFormat == EModalFormat.Full) ?
                    new Point(TJAPlayer3.Skin.Modal_Text_Full[0] +(tTextCentered () ?  TJAPlayer3.Skin.Modal_Text_Full_Move[0] : 0),
                    TJAPlayer3.Skin.Modal_Text_Full[1] + (tTextCentered () ? TJAPlayer3.Skin.Modal_Text_Full_Move[1] : 0)) :

                    new Point(TJAPlayer3.Skin.Modal_Text_Half_X[player] + (tTextCentered () ? TJAPlayer3.Skin.Modal_Text_Half_Move[0] : 0),
                    TJAPlayer3.Skin.Modal_Text_Half_Y[player] + (tTextCentered () ? TJAPlayer3.Skin.Modal_Text_Half_Move[1] : 0)), // content
                };
                */

                Point[] Pos = new Point[]
                {
                    new Point(title_x[player], title_y[player]),
                    new Point(text_x[player] + (tTextCentered () ? moveX : 0),
                    text_y[player] + (tTextCentered () ? moveY : 0)), // content
                };

                _ModalTitle?.t2D中心基準描画(Pos[0].X, Pos[0].Y);
                _ModalText?.t2D中心基準描画(Pos[1].X, Pos[1].Y);

                // Extra texture for Puchichara, Character and Titles next
            }
        }

        public void tPlayModalSfx()
        {
            if (modalType == EModalType.Coin)
                TJAPlayer3.Skin.soundModal[TJAPlayer3.Skin.soundModal.Length - 1].tPlay();
            else
                TJAPlayer3.Skin.soundModal[Math.Max(0, Math.Min(TJAPlayer3.Skin.soundModal.Length - 2, rarity))].tPlay();
        }

        public static void tInitModalFonts()
        {
            if (_pfModalContentHalf != null
                && _pfModalTitleHalf != null
                && _pfModalContentFull != null
                && _pfModalTitleFull != null)
                return;

            _pfModalContentHalf = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.Modal_Font_ModalContentHalf_Size);
            _pfModalTitleHalf = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.Modal_Font_ModalTitleHalf_Size);
            _pfModalContentFull = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.Modal_Font_ModalContentFull_Size);
            _pfModalTitleFull = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.Modal_Font_ModalTitleFull_Size);
        }

        #region [Enum definitions]

        public enum EModalType
        {
            Coin = 0,
            Character = 1,
            Puchichara = 2,
            Title = 3,
            Song = 4,
            Total = 5,
        }

        // Full : 1P standard modal, Half : Splitted screen modal
        public enum EModalFormat
        {
            Full,
            Half,
            Half_4P,
            Half_5P,
        }

        #endregion

        #region [Public variables]

        // Coin number for coin; database/unlockable asset for puchichara, character and title; no effect on text, confirm
        public object[] reference;

        public int rarity;
        public EModalType modalType;
        public EModalFormat modalFormat;

        // For modalFormat = Half only
        public int player;

        #endregion

        #region [private]

        // Check if the text is vertically centered or slightly up (to let enough space for the unlocked unit texture)
        private bool tTextCentered()
        {
            if (modalType == EModalType.Coin)
                return true;
            return false;
        }

        // Generate the modal title and content text textures
        private void tGenerateTextures()
        {
            string modalKey = "MODAL_TITLE_COIN";
            switch (modalType)
            {
                case EModalType.Character:
                    modalKey = "MODAL_TITLE_CHARA";
                    break;
                case EModalType.Puchichara:
                    modalKey = "MODAL_TITLE_PUCHI";
                    break;
                case EModalType.Title:
                    modalKey = "MODAL_TITLE_NAMEPLATE";
                    break;
                case EModalType.Song:
                    modalKey = "MODAL_TITLE_SONG";
                    break;
            }
            TitleTextureKey _title = new TitleTextureKey(
                CLangManager.LangInstance.GetString(modalKey), 
                (modalFormat == EModalFormat.Full)
                    ? _pfModalTitleFull
                    : _pfModalTitleHalf, 
                Color.White, 
                Color.Black, 
                1800);

            string content = "";

            if (modalType == EModalType.Coin)
            {
                content = CLangManager.LangInstance.GetString("MODAL_MESSAGE_COIN", reference[0].ToString(), TJAPlayer3.SaveFileInstances[player].data.Medals.ToString());
                //content = String.Format("+{0} {1} ({2}: {3})",
                //    (int)reference[0],
                //    CLangManager.LangInstance.GetString(306),
                //    CLangManager.LangInstance.GetString(307),
                //    TJAPlayer3.SaveFileInstances[player].data.Medals
                //    );
            }
            else if (modalType == EModalType.Title)
            {
                content = ((string)reference[0]).RemoveTags();
            }
            else if (modalType == EModalType.Character)
            {
                content = ((string)reference[0]).RemoveTags();
            }
            else if (modalType == EModalType.Puchichara)
            {
                content = ((string)reference[0]).RemoveTags();
            }
            else if (modalType == EModalType.Song)
            {
                content = ((string)reference[0]).RemoveTags();
            }

            TitleTextureKey _content = new TitleTextureKey(
                content,
                (modalFormat == EModalFormat.Full)
                    ? _pfModalContentFull
                    : _pfModalContentHalf,
                Color.White,
                Color.Black,
                1800);

            _ModalText = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(_content);
            _ModalTitle = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(_title);
        }

        private CTexture _box;
        private Rectangle _boxRect;

        private bool _isSet;

        private static CCachedFontRenderer _pfModalTitleHalf;
        private static CCachedFontRenderer _pfModalContentHalf;
        private static CCachedFontRenderer _pfModalTitleFull;
        private static CCachedFontRenderer _pfModalContentFull;

        private CTexture _ModalTitle;
        private CTexture _ModalText;

        #endregion
    }
}
