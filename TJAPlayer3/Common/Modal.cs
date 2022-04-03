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
        public Modal(EModalType mt, int ra, int re)
        {
            modalType = mt;
            rarity = ra;
            reference = re;
            _isSet = false;
        }

        public void tSetupModal()
        {
            CTexture[] arrRef;

            if (modalFormat == EModalFormat.Half)
                arrRef = TJAPlayer3.Tx.Modal_Half;
            else
                arrRef = TJAPlayer3.Tx.Modal_Full;

            if (modalType == EModalType.Coin)
                _box = arrRef[arrRef.Length - 1];
            else
            {
                int usedTex = Math.Max(0, Math.Min(arrRef.Length - 2, rarity));
                _box = arrRef[usedTex];
            }

            _boxRect = new Rectangle(
                (modalFormat == EModalFormat.Full || player == 0)
                    ? 0
                    : 640,
                0,
                (modalFormat == EModalFormat.Full)
                    ? 1280
                    : 640,
                720);

            tGenerateTextures();

            _isSet = true;
        }

        public void tDisplayModal()
        {
            if (_isSet == true)
            {
                _box?.t2D描画(TJAPlayer3.app.Device, 640 * player, 0, _boxRect);

                Point[] Pos = new Point[]
                {
                    (modalFormat == EModalFormat.Full) ? new Point(640, 140) : new Point(320 + 640 * player, 290), // title
                    (modalFormat == EModalFormat.Full) ? new Point(640, tTextCentered() ? 445 : 327) : new Point(320 + 640 * player, tTextCentered() ? 442 : 383), // content
                };

                _ModalTitle?.t2D中心基準描画(TJAPlayer3.app.Device, Pos[0].X, Pos[0].Y);
                _ModalText?.t2D中心基準描画(TJAPlayer3.app.Device, Pos[1].X, Pos[1].Y);

                // Extra texture for Puchichara, Character and Titles next
            }
        }

        public void tPlayModalSfx()
        {
            if (modalType == EModalType.Coin)
                TJAPlayer3.Skin.soundModal[TJAPlayer3.Skin.soundModal.Length - 1].t再生する();
            else
                TJAPlayer3.Skin.soundModal[Math.Max(0, Math.Min(TJAPlayer3.Skin.soundModal.Length - 2, rarity))].t再生する();
        }

        public static void tInitModalFonts()
        {
            if (_pfModalContentHalf != null
                && _pfModalTitleHalf != null
                && _pfModalContentFull != null
                && _pfModalTitleFull != null)
                return;

            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
            {
                _pfModalContentHalf = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 28);
                _pfModalTitleHalf = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 28);
                _pfModalContentFull = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 56);
                _pfModalTitleFull = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 56);
            }
            else
            {
                _pfModalContentHalf = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 28);
                _pfModalTitleHalf = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 28);
                _pfModalContentFull = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 56);
                _pfModalTitleFull = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 56);
            }
        }

        #region [Enum definitions]

        public enum EModalType
        {
            Coin = 0,
            Puchichara = 1,
            Character = 2,
            Title = 3,
            Text = 4,
            Confirm = 5,
        }

        // Full : 1P standard modal, Half : Splitted screen modal
        public enum EModalFormat
        {
            Full,
            Half,
        }

        #endregion

        #region [Public variables]

        // Coin number for coin; database/unlockable asset for puchichara, character and title; no effect on text, confirm
        public int reference;

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
            if (modalType == EModalType.Coin || modalType == EModalType.Text)
                return true;
            return false;
        }

        // Generate the modal title and content text textures
        private void tGenerateTextures()
        {
            TitleTextureKey _title = new TitleTextureKey(
                CLangManager.LangInstance.GetString(300 + (int)modalType), 
                (modalFormat == EModalFormat.Full)
                    ? _pfModalTitleFull
                    : _pfModalTitleHalf, 
                Color.White, 
                Color.Black, 
                1800);

            string content = "";

            if (modalType == EModalType.Coin)
            {
                content = String.Format("+{0} {1} ({2}: {3})",
                    reference,
                    CLangManager.LangInstance.GetString(306),
                    CLangManager.LangInstance.GetString(307),
                    TJAPlayer3.NamePlateConfig.data.Medals[player]
                    );
            }

            TitleTextureKey _content = new TitleTextureKey(
                content,
                (modalFormat == EModalFormat.Full)
                    ? _pfModalContentFull
                    : _pfModalContentHalf,
                Color.White,
                Color.Black,
                1800);

            _ModalText = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(_content);
            _ModalTitle = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(_title);
        }

        private CTexture _box;
        private Rectangle _boxRect;

        private bool _isSet;

        private static CPrivateFastFont _pfModalTitleHalf;
        private static CPrivateFastFont _pfModalContentHalf;
        private static CPrivateFastFont _pfModalTitleFull;
        private static CPrivateFastFont _pfModalContentFull;

        private CTexture _ModalTitle;
        private CTexture _ModalText;

        #endregion
    }
}
