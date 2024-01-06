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
        private static CCounter[] ctCharacterNormal = new CCounter[5] { new CCounter(), new CCounter(), new CCounter(), new CCounter(), new CCounter() };
        private static CCounter[] ctCharacterSelect = new CCounter[5] { new CCounter(), new CCounter(), new CCounter(), new CCounter(), new CCounter() };
        private static CCounter[] ctCharacterStart = new CCounter[5] { new CCounter(), new CCounter(), new CCounter(), new CCounter(), new CCounter() };
        private static CCounter[] ctCharacterWait = new CCounter[5] { new CCounter(), new CCounter(), new CCounter(), new CCounter(), new CCounter() };
        private static CCounter[] ctCharacterEntry = new CCounter[5] { new CCounter(), new CCounter(), new CCounter(), new CCounter(), new CCounter() };
        private static CCounter[] ctCharacterEntryNormal = new CCounter[5] { new CCounter(), new CCounter(), new CCounter(), new CCounter(), new CCounter() };

        public enum ECharacterAnimation
        {
            // Song select
            NORMAL,
            START,
            SELECT,
            WAIT,
            // Main menu
            ENTRY,
            ENTRY_NORMAL,
        }


        private static bool _usesSubstituteTexture(int player, ECharacterAnimation eca)
        {
            int _charaId = TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data.Character;

            if (_charaId >= 0 && _charaId < TJAPlayer3.Skin.Characters_Ptn)
            {
                switch (eca)
                {
                    case (ECharacterAnimation.NORMAL):
                        {
                            if (TJAPlayer3.Tx.Characters_Menu_Loop[_charaId].Length > 0)
                                return false;
                            break;
                        }
                    case (ECharacterAnimation.START):
                        {
                            if (TJAPlayer3.Tx.Characters_Menu_Start[_charaId].Length > 0)
                                return false;
                            break;
                        }
                    case (ECharacterAnimation.SELECT):
                        {
                            if (TJAPlayer3.Tx.Characters_Menu_Select[_charaId].Length > 0)
                                return false;
                            break;
                        }
                    case (ECharacterAnimation.WAIT):
                        {
                            if (TJAPlayer3.Tx.Characters_Menu_Wait[_charaId].Length > 0)
                                return false;
                            break;
                        }
                    case (ECharacterAnimation.ENTRY):
                        {
                            if (TJAPlayer3.Tx.Characters_Title_Entry[_charaId].Length > 0)
                                return false;
                            break;
                        }
                    case (ECharacterAnimation.ENTRY_NORMAL):
                        {
                            if (TJAPlayer3.Tx.Characters_Title_Normal[_charaId].Length > 0)
                                return false;
                            break;
                        }
                }
            }

            return true;
        }

        public static CTexture[] _getReferenceArray(int player, ECharacterAnimation eca)
        {
            int _charaId = TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data.Character;

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
                            if (TJAPlayer3.Tx.Characters_10Combo[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_10Combo[_charaId];
                            break;
                        }
                    case (ECharacterAnimation.WAIT):
                        {
                            if (TJAPlayer3.Tx.Characters_Menu_Wait[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Menu_Wait[_charaId];
                            if (TJAPlayer3.Tx.Characters_Menu_Loop[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Menu_Loop[_charaId];
                            if (TJAPlayer3.Tx.Characters_GoGoTime[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_GoGoTime[_charaId];
                            break;
                        }
                    case (ECharacterAnimation.ENTRY):
                        {
                            if (TJAPlayer3.Tx.Characters_Title_Entry[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Title_Entry[_charaId];
                            if (TJAPlayer3.Tx.Characters_10Combo[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_10Combo[_charaId];
                            break;
                        }
                    case (ECharacterAnimation.ENTRY_NORMAL):
                        {
                            if (TJAPlayer3.Tx.Characters_Title_Normal[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Title_Normal[_charaId];
                            if (TJAPlayer3.Tx.Characters_Normal[_charaId].Length > 0)
                                return TJAPlayer3.Tx.Characters_Normal[_charaId];
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
                case (ECharacterAnimation.WAIT):
                    {
                        return ctCharacterWait;
                    }
                case (ECharacterAnimation.ENTRY):
                    {
                        return ctCharacterEntry;
                    }
                case (ECharacterAnimation.ENTRY_NORMAL):
                    {
                        return ctCharacterEntryNormal;
                    }
            }
            return null;
        }

        public static int _getReferenceAnimationDuration(int player, ECharacterAnimation eca)
        {
            int _charaId = TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data.Character;

            switch (eca)
            {
                case (ECharacterAnimation.NORMAL):
                    {
                        return TJAPlayer3.Skin.Characters_Menu_Loop_AnimationDuration[_charaId];
                    }
                case (ECharacterAnimation.START):
                    {
                        return TJAPlayer3.Skin.Characters_Menu_Start_AnimationDuration[_charaId];
                    }
                case (ECharacterAnimation.SELECT):
                    {
                        return TJAPlayer3.Skin.Characters_Menu_Select_AnimationDuration[_charaId];
                    }
                case (ECharacterAnimation.WAIT):
                    {
                        return TJAPlayer3.Skin.Characters_Menu_Wait_AnimationDuration[_charaId];
                    }
                case (ECharacterAnimation.ENTRY):
                    {
                        return TJAPlayer3.Skin.Characters_Title_Entry_AnimationDuration[_charaId];
                    }
                case (ECharacterAnimation.ENTRY_NORMAL):
                    {
                        return TJAPlayer3.Skin.Characters_Title_Normal_AnimationDuration[_charaId];
                    }
            }
            return 1000;
        }

        public static void tDisableCounter(ECharacterAnimation eca)
        {
            switch (eca)
            {
                case (ECharacterAnimation.NORMAL):
                    {
                        for (int i = 0; i < 5; i++)
                            ctCharacterNormal[i] = new CCounter();
                        break;
                    }
                case (ECharacterAnimation.START):
                    {
                        for (int i = 0; i < 5; i++)
                            ctCharacterStart[i] = new CCounter();
                        break;
                    }
                case (ECharacterAnimation.SELECT):
                    {
                        for (int i = 0; i < 5; i++)
                            ctCharacterSelect[i] = new CCounter();
                        break;
                    }
                case (ECharacterAnimation.WAIT):
                    {
                        for (int i = 0; i < 5; i++)
                            ctCharacterWait[i] = new CCounter();
                        break;
                    }
                case (ECharacterAnimation.ENTRY):
                    {
                        for (int i = 0; i < 5; i++)
                            ctCharacterEntry[i] = new CCounter();
                        break;
                    }
                case (ECharacterAnimation.ENTRY_NORMAL):
                    {
                        for (int i = 0; i < 5; i++)
                            ctCharacterEntryNormal[i] = new CCounter();
                        break;
                    }
            }

        }


        public static void tMenuResetTimer(int player, ECharacterAnimation eca)
        {
            CTexture[] _ref = _getReferenceArray(player, eca);
            CCounter[] _ctref = _getReferenceCounter(eca);
            int _animeref = _getReferenceAnimationDuration(player, eca);

            if (_ref != null && _ref.Length > 0 && _ctref != null)
            {
                _ctref[player] = new CCounter(0, _ref.Length - 1, _animeref / (float)_ref.Length, TJAPlayer3.Timer);
            }
        }

        public static void tMenuResetTimer(ECharacterAnimation eca)
        {
            for (int i = 0; i < 5; i++)
            {
                tMenuResetTimer(i, eca);
            }
        }

        public static void tMenuDisplayCharacter(int player, int x, int y, ECharacterAnimation eca, int opacity = 255)
        {
            int _charaId = TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data.Character;
            CTexture[] _ref = _getReferenceArray(player, eca);
            CCounter[] _ctref = _getReferenceCounter(eca);
            bool _substitute = _usesSubstituteTexture(player, eca);

            if (_ctref[player] != null && _ref != null && _ctref[player].CurrentValue < _ref.Length)
            {
                if (eca == ECharacterAnimation.NORMAL 
                    || eca == ECharacterAnimation.WAIT 
                    || eca == ECharacterAnimation.ENTRY
                    || eca == ECharacterAnimation.ENTRY_NORMAL)
                    _ctref[player].TickLoop();
                else
                    _ctref[player].Tick();

                // Quick fix
                if (_ctref[player].CurrentValue >= _ref.Length) return;

                var _tex = _ref[_ctref[player].CurrentValue];

                _tex.Opacity = opacity;

                float resolutionRatioX = TJAPlayer3.Skin.Resolution[0] / (float)TJAPlayer3.Skin.Characters_Resolution[_charaId][0];
                float resolutionRatioY = TJAPlayer3.Skin.Resolution[1] / (float)TJAPlayer3.Skin.Characters_Resolution[_charaId][1];

                //float _x = (x + (150.0f * (TJAPlayer3.Skin.Characters_Resolution[_charaId][0] / 1280.0f))) * resolutionRatioX;
                //float _y = (y + (((_substitute == true) ? 290 : _ref[_ctref[player].n現在の値].szテクスチャサイズ.Height)) * (TJAPlayer3.Skin.Characters_Resolution[_charaId][1] / 720.0f)) * resolutionRatioY;

                _tex.vc拡大縮小倍率.X *= resolutionRatioX;
                _tex.vc拡大縮小倍率.Y *= resolutionRatioY;

                float _x = x;
                float _y = y;

                if (player % 2 == 0)
                {
                    //_ref[_ctref[player].n現在の値].t2D描画(x, y);
                    //_ref[_ctref[player].n現在の値].t2D中心基準描画(x + 150, y + 156);

                    _tex.t2D拡大率考慮下中心基準描画(
                        _x,
                        _y // 312
                        );



                }
                else
                {
                    //_ref[_ctref[player].n現在の値].t2D左右反転描画(x, y);
                    //_ref[_ctref[player].n現在の値].t2D中心基準描画Mirrored(x + 150, y + 156);

                    _tex.t2D拡大率考慮下中心基準描画Mirrored(
                        _x,
                        _y // 312
                        );
                }

                _tex.vc拡大縮小倍率.X = 1f;
                _tex.vc拡大縮小倍率.Y = 1f;

                _tex.Opacity = 255;

            }

        }
    }
}
