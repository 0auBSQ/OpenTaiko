using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;

namespace TJAPlayer3
{
    class CResultCharacter
    {
        private static CCounter[] ctCharacterNormal = new CCounter[4] { new CCounter(), new CCounter(), new CCounter(), new CCounter() };
        private static CCounter[] ctCharacterClear = new CCounter[4] { new CCounter(), new CCounter(), new CCounter(), new CCounter() };
        private static CCounter[] ctCharacterFailed = new CCounter[4] { new CCounter(), new CCounter(), new CCounter(), new CCounter() };
        private static CCounter[] ctCharacterFailedIn = new CCounter[4] { new CCounter(), new CCounter(), new CCounter(), new CCounter() };


        public enum ECharacterResult
        {
            // Song select
            NORMAL,
            CLEAR,
            FAILED,
            FAILED_IN,
        }

        public static bool tIsCounterProcessing(int player, ECharacterResult eca)
        {
            CCounter[] _ctref = _getReferenceCounter(eca);

            if (_ctref[player] != null)
                return _ctref[player].b進行中;
            return false;
        }

        public static bool tIsCounterEnded(int player, ECharacterResult eca)
        {
            CCounter[] _ctref = _getReferenceCounter(eca);

            if (_ctref[player] != null)
                return _ctref[player].b終了値に達した;
            return false;
        }

        private static bool _usesSubstituteTexture(int player, ECharacterResult eca)
        {
            int _charaId = TJAPlayer3.NamePlateConfig.data.Character[TJAPlayer3.GetActualPlayer(player)];

            if (_charaId >= 0 && _charaId < TJAPlayer3.Skin.Characters_Ptn)
            {
                switch (eca)
                {
                    case (ECharacterResult.NORMAL):
                        {
                            if (TJAPlayer3.Tx.Characters_Result_Normal[_charaId].Length > 0)
                                return false;
                            break;
                        }
                    case (ECharacterResult.CLEAR):
                        {
                            if (TJAPlayer3.Tx.Characters_Result_Clear[_charaId].Length > 0)
                                return false;
                            break;
                        }
                    case (ECharacterResult.FAILED):
                        {
                            if (TJAPlayer3.Tx.Characters_Result_Failed[_charaId].Length > 0)
                                return false;
                            break;
                        }
                    case (ECharacterResult.FAILED_IN):
                        {
                            if (TJAPlayer3.Tx.Characters_Result_Failed_In[_charaId].Length > 0)
                                return false;
                            break;
                        }
                }
            }

            return true;
        }

        public static CTexture[] _getReferenceArray(int player, ECharacterResult eca)
        {
            int _charaId = TJAPlayer3.NamePlateConfig.data.Character[TJAPlayer3.GetActualPlayer(player)];

            if (_charaId >= 0 && _charaId < TJAPlayer3.Skin.Characters_Ptn)
            {
                switch (eca)
                {
                    case (ECharacterResult.NORMAL):
                        {
                            if (TJAPlayer3.Tx.Characters_Result_Normal[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Result_Normal[_charaId];
                            if (TJAPlayer3.Tx.Characters_Normal[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Normal[_charaId];
                            break;
                        }
                    case (ECharacterResult.CLEAR):
                        {
                            if (TJAPlayer3.Tx.Characters_Result_Clear[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Result_Clear[_charaId];
                            if (TJAPlayer3.Tx.Characters_10Combo[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_10Combo[_charaId];
                            break;
                        }
                    case (ECharacterResult.FAILED):
                        {
                            if (TJAPlayer3.Tx.Characters_Result_Failed[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Result_Failed[_charaId];
                            if (TJAPlayer3.Tx.Characters_Normal[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Normal[_charaId];
                            break;
                        }
                    case (ECharacterResult.FAILED_IN):
                        {
                            if (TJAPlayer3.Tx.Characters_Result_Failed_In[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Result_Failed_In[_charaId];
                            if (TJAPlayer3.Tx.Characters_Normal[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Normal[_charaId];
                            break;
                        }
                }
            }


            return null;
        }

        public static CCounter[] _getReferenceCounter(ECharacterResult eca)
        {
            switch (eca)
            {
                case (ECharacterResult.NORMAL):
                    {
                        return ctCharacterNormal;
                    }
                case (ECharacterResult.CLEAR):
                    {
                        return ctCharacterClear;
                    }
                case (ECharacterResult.FAILED):
                    {
                        return ctCharacterFailed;
                    }
                case (ECharacterResult.FAILED_IN):
                    {
                        return ctCharacterFailedIn;
                    }
            }
            return null;
        }

        public static void tDisableCounter(ECharacterResult eca)
        {
            switch (eca)
            {
                case (ECharacterResult.NORMAL):
                    {
                        for (int i = 0; i < 4; i++)
                            ctCharacterNormal[i] = new CCounter();
                        break;
                    }
                case (ECharacterResult.CLEAR):
                    {
                        for (int i = 0; i < 4; i++)
                            ctCharacterClear[i] = new CCounter();
                        break;
                    }
                case (ECharacterResult.FAILED):
                    {
                        for (int i = 0; i < 4; i++)
                            ctCharacterFailed[i] = new CCounter();
                        break;
                    }
                case (ECharacterResult.FAILED_IN):
                    {
                        for (int i = 0; i < 4; i++)
                            ctCharacterFailedIn[i] = new CCounter();
                        break;
                    }
            }

        }


        public static void tMenuResetTimer(int player, ECharacterResult eca)
        {
            CTexture[] _ref = _getReferenceArray(player, eca);
            CCounter[] _ctref = _getReferenceCounter(eca);

            if (_ref != null && _ref.Length > 0 && _ctref != null)
            {
                _ctref[player] = new CCounter(0, _ref.Length - 1, 1000 / (float)_ref.Length, TJAPlayer3.Timer);
            }
        }

        public static void tMenuResetTimer(ECharacterResult eca)
        {
            for (int i = 0; i < 2; i++)
            {
                tMenuResetTimer(i, eca);
            }
        }

        public static void tMenuDisplayCharacter(int player, int x, int y, ECharacterResult eca, int pos = 0, int opacity = 255)
        {
            CTexture[] _ref = _getReferenceArray(player, eca);
            CCounter[] _ctref = _getReferenceCounter(eca);
            bool _substitute = _usesSubstituteTexture(player, eca);

            if (_ctref[player] != null && _ref != null && _ctref[player].n現在の値 < _ref.Length)
            {
                if (eca == ECharacterResult.NORMAL
                    || eca == ECharacterResult.CLEAR
                    || eca == ECharacterResult.FAILED)
                    _ctref[player].t進行Loop();
                else
                    _ctref[player].t進行();

                // x0.8 if not substitute
                if (!_substitute)
                {
                    _ref[_ctref[player].n現在の値].vc拡大縮小倍率.X = 0.8f;
                    _ref[_ctref[player].n現在の値].vc拡大縮小倍率.Y = 0.8f;
                }

                _ref[_ctref[player].n現在の値].Opacity = opacity;

                if (pos % 2 == 0)
                {
                    _ref[_ctref[player].n現在の値].t2D中心基準描画(TJAPlayer3.app.Device,
                        x,
                        y + ((_substitute == true) ? 90 : 0)
                        );
                }
                else
                {
                    _ref[_ctref[player].n現在の値].t2D中心基準描画Mirrored(TJAPlayer3.app.Device,
                        1340 - x,
                        y + ((_substitute == true) ? 90 : 0)
                        );
                }

                // Restore if not substitute
                if (!_substitute)
                {
                    _ref[_ctref[player].n現在の値].vc拡大縮小倍率.X = 1f;
                    _ref[_ctref[player].n現在の値].vc拡大縮小倍率.Y = 1f;
                }

                _ref[_ctref[player].n現在の値].Opacity = 255;

            }

        }
    }
}
