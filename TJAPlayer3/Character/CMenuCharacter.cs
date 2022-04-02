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
        private static CCounter[] ctCharacterNormal = new CCounter[4] { new CCounter(), new CCounter(), new CCounter(), new CCounter() };
        private static CCounter[] ctCharacterSelect = new CCounter[4] { new CCounter(), new CCounter(), new CCounter(), new CCounter() };
        private static CCounter[] ctCharacterStart = new CCounter[4] { new CCounter(), new CCounter(), new CCounter(), new CCounter() };

        public enum ECharacterAnimation
        {
            NORMAL,
            START,
            SELECT
        }

        private static CTexture[] _getReferenceArray(int player, ECharacterAnimation eca)
        {
            int _charaId = TJAPlayer3.NamePlateConfig.data.Character[TJAPlayer3.GetActualPlayer(player)];

            if (_charaId >= 0 && _charaId < TJAPlayer3.Skin.Characters_Ptn)
            {
                switch (eca)
                {
                    case (ECharacterAnimation.NORMAL):
                        {
                            if (TJAPlayer3.Tx.Characters_Menu_Loop[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Menu_Loop[_charaId];
                            if (TJAPlayer3.Tx.Characters_Normal[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Normal[_charaId];
                            break;
                        }
                    case (ECharacterAnimation.START):
                        {
                            if (TJAPlayer3.Tx.Characters_Menu_Start[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Menu_Start[_charaId];
                            if (TJAPlayer3.Tx.Characters_10Combo[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_10Combo[_charaId];
                            break;
                        }
                    case (ECharacterAnimation.SELECT):
                        {
                            if (TJAPlayer3.Tx.Characters_Menu_Select[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Menu_Select[_charaId];
                            if (TJAPlayer3.Tx.Characters_10Combo_Maxed[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_10Combo_Maxed[_charaId];
                            if (TJAPlayer3.Tx.Characters_10Combo[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_10Combo[_charaId];
                            break;
                        }
                }
            }

            
            return null;
        }

        public static CCounter[] _getReferenceCounter(ECharacterAnimation eca)
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

        public static void tDisableCounter(ECharacterAnimation eca)
        {
            switch (eca)
            {
                case (ECharacterAnimation.NORMAL):
                    {
                        for (int i = 0; i < 4; i++)
                            ctCharacterNormal[i] = new CCounter();
                        break;
                    }
                case (ECharacterAnimation.START):
                    {
                        for (int i = 0; i < 4; i++)
                            ctCharacterStart[i] = new CCounter();
                        break;
                    }
                case (ECharacterAnimation.SELECT):
                    {
                        for (int i = 0; i < 4; i++)
                            ctCharacterSelect[i] = new CCounter();
                        break;
                    }
            }

        }


        public static void tMenuResetTimer(int player, ECharacterAnimation eca)
        {
            CTexture[] _ref = _getReferenceArray(player, eca);
            CCounter[] _ctref = _getReferenceCounter(eca);

            if (_ref != null &&_ref.Length > 0 && _ctref != null)
            {
                _ctref[player] = new CCounter(0, _ref.Length - 1, 1000 / (float)_ref.Length, TJAPlayer3.Timer);
            }
        }

        public static void tMenuResetTimer(ECharacterAnimation eca)
        {
            for (int i = 0; i < 2; i++)
            {
                tMenuResetTimer(i, eca);
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
                {
                    //_ref[_ctref[player].n現在の値].t2D描画(TJAPlayer3.app.Device, x, y);

                    _ref[_ctref[player].n現在の値].t2D中心基準描画(TJAPlayer3.app.Device, x + 150, y + 156);
                }
                else
                {
                    //_ref[_ctref[player].n現在の値].t2D左右反転描画(TJAPlayer3.app.Device, x, y);


                    _ref[_ctref[player].n現在の値].t2D中心基準描画Mirrored(TJAPlayer3.app.Device, x + 150, y + 156);
                }
                    
            }

        }
    }
}
