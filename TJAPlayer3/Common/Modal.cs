using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;
using System.Drawing;

namespace TJAPlayer3
{
    internal class Modal
    {
        public Modal(EModalType mt, int ra, int re, EModalFormat mf, int p = 0)
        {
            modalType = mt;
            modalFormat = mf;
            rarity = ra;
            reference = re;
            player = p;

            tSetupModal();
        }

        private void tSetupModal()
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
        }

        public void tDisplayModal()
        {
            

            _box?.t2D描画(TJAPlayer3.app.Device, 0, 0, _boxRect);
        }

        public enum EModalType
        {
            Coin,
            Puchichara,
            Character,
            Title,
            Text,
            Confirm,
        }

        public enum EModalFormat
        {
            Full,
            Half,
        }

        // Coin number for coin; database/unlockable asset for puchichara, character and title; no effect on text, confirm
        public int reference;

        public int rarity;
        public EModalType modalType;
        public EModalFormat modalFormat;

        // For modalFormat = Half only
        public int player;

        private CTexture _box;
        private Rectangle _boxRect;
    }
}
