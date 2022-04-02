using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;

namespace TJAPlayer3
{
    class CMenuCharacter
    {
        private static CCounter[] ctCharacterNormal = new CCounter[4];
        private static CCounter[] ctCharacterSelect = new CCounter[4];
        private static CCounter[] ctCharacterStart = new CCounter[4];

        public enum ECharacterAnimation
        {
            NORMAL,
            START,
            SELECT
        }

        private static CTexture[] _getReferenceArray(int player, ECharacterAnimation eca)
        {
            CTexture[] _ref = null;

            int _charaId = TJAPlayer3.NamePlateConfig.data.Character[player];

            if (_charaId >= 0 && _charaId < TJAPlayer3.Skin.Characters_Ptn)
            {
                switch (eca)
                {
                    case (ECharacterAnimation.NORMAL):
                        {
                            if (TJAPlayer3.Tx.Characters_Menu_Loop[_charaId] != null)
                                return TJAPlayer3.Tx.Characters_Menu_Loop[_charaId];
                            if (TJAPlayer3.Tx.Characters_Normal[_charaId] != null)
                                return TJAPlayer3.Tx.Characters_Normal[_charaId];
                            break;
                        }
                    case (ECharacterAnimation.START):
                        {
                            if (TJAPlayer3.Tx.Characters_Menu_Start[_charaId] != null)
                                return TJAPlayer3.Tx.Characters_Menu_Start[_charaId];
                            if (TJAPlayer3.Tx.Characters_10Combo[_charaId] != null)
                                return TJAPlayer3.Tx.Characters_10Combo[_charaId];
                            break;
                        }
                    case (ECharacterAnimation.SELECT):
                        {
                            if (TJAPlayer3.Tx.Characters_Menu_Select[_charaId] != null)
                                return TJAPlayer3.Tx.Characters_Menu_Select[_charaId];
                            if (TJAPlayer3.Tx.Characters_10Combo_Maxed[_charaId] != null)
                                return TJAPlayer3.Tx.Characters_10Combo_Maxed[_charaId];
                            if (TJAPlayer3.Tx.Characters_10Combo[_charaId] != null)
                                return TJAPlayer3.Tx.Characters_10Combo[_charaId];
                            break;
                        }
                }
            }

            
            return _ref;
        }

        private static CCounter[] _getReferenceCounter(ECharacterAnimation eca)
        {
            switch (eca)
            {
                case (ECharacterAnimation.NORMAL):
                    {
                        return ctCharacterNormal;
                    }
                case (ECharacterAnimation.START):
                    {
                        return ctCharacterStart;
                    }
                case (ECharacterAnimation.SELECT):
                    {
                        return ctCharacterSelect;
                    }
            }
            return null;
        }


        public static void tMenuResetTimer(int player, ECharacterAnimation eca)
        {
            CTexture[] _ref = _getReferenceArray(player, eca);
            CCounter[] _ctref = _getReferenceCounter(eca);

            if (_ref != null && _ref.Length > 0 && _ctref != null)
            {
                _ctref[player] = new CCounter(0, _ref.Length - 1, 1000 / _ref.Length, TJAPlayer3.Timer);
            }
        }

        public static void tMenuDisplayCharacter(int player, int x, int y, ECharacterAnimation eca)
        {
            CTexture[] _ref = _getReferenceArray(player, eca);
            CCounter[] _ctref = _getReferenceCounter(eca);

            if (_ctref[player] != null)
            {
                if (eca == ECharacterAnimation.NORMAL)
                    _ctref[player].t進行Loop();
                else
                    _ctref[player].t進行();

                if (player % 2 == 0)
                    _ref[_ctref[player].n現在の値].t2D描画(TJAPlayer3.app.Device, x, y);
                else
                    _ref[_ctref[player].n現在の値].t2D左右反転描画(TJAPlayer3.app.Device, x, y);
            }

        }
    }
}
