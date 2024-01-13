using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using FDK;


namespace TJAPlayer3
{
    class HGaugeMethods
    {
        public enum EGaugeType
        {
            NORMAL = 0,
            HARD,
            EXTREME
        }

        public static float BombDamage = 4f;
        public static float FuserollDamage = 4f;
        public static float HardGaugeFillRatio = 1f;
        public static float ExtremeGaugeFillRatio = 1f;

        private static Dictionary<Difficulty, float> DifficultyToHardGaugeDamage = new Dictionary<Difficulty, float>
        {
            [Difficulty.Easy] = 4.5f,
            [Difficulty.Normal] = 5f,
            [Difficulty.Hard] = 6f,
            [Difficulty.Oni] = 6.5f,
            [Difficulty.Edit] = 6.5f
        };

        private static Dictionary<Difficulty, float> DifficultyToExtremeGaugeDamage = new Dictionary<Difficulty, float>
        {
            [Difficulty.Easy] = 4.5f,
            [Difficulty.Normal] = 5f,
            [Difficulty.Hard] = 6f,
            [Difficulty.Oni] = 6.5f,
            [Difficulty.Edit] = 6.5f
        };

        private static Dictionary<int, float> LevelExtraToHardGaugeDamage = new Dictionary<int, float>
        {
            [11] = 7.5f,
            [12] = 8f,
            [13] = 8.5f
        };

        private static Dictionary<int, float> LevelExtraToExtremeGaugeDamage = new Dictionary<int, float>
        {
            [11] = 7.5f,
            [12] = 8f,
            [13] = 8.5f
        };

        private static Dictionary<string, EGaugeType> GaugeTypeStringToEnum = new Dictionary<string, EGaugeType>
        {
            ["Normal"] = EGaugeType.NORMAL,
            ["Hard"] = EGaugeType.HARD,
            ["Extreme"] = EGaugeType.EXTREME
        };

        private static Dictionary<Difficulty, float> DifficultyToNorma = new Dictionary<Difficulty, float>
        {
            [Difficulty.Easy] = 60f,
            [Difficulty.Normal] = 70f,
            [Difficulty.Hard] = 70f,
            [Difficulty.Oni] = 80f,
            [Difficulty.Edit] = 80f
        };

        private static Dictionary<int, float> LevelExtraToNorma = new Dictionary<int, float>
        {
            [11] = 88f,
            [12] = 92f,
            [13] = 96f
        };

        #region [General calculation]

        public static float tHardGaugeGetDamage(Difficulty diff, int level)
        {
            float damage = 6.5f;

            if (DifficultyToHardGaugeDamage.ContainsKey(diff))
                damage = DifficultyToHardGaugeDamage[diff];

            int levelCaped = Math.Min(13, level);
            if (LevelExtraToHardGaugeDamage.ContainsKey(levelCaped))
                damage = LevelExtraToHardGaugeDamage[levelCaped];

            return damage;
        }

        public static float tExtremeGaugeGetDamage(Difficulty diff, int level)
        {
            float damage = 6.5f;

            if (DifficultyToExtremeGaugeDamage.ContainsKey(diff))
                damage = DifficultyToExtremeGaugeDamage[diff];

            int levelCaped = Math.Min(13, level);
            if (LevelExtraToExtremeGaugeDamage.ContainsKey(levelCaped))
                damage = LevelExtraToExtremeGaugeDamage[levelCaped];

            return damage;
        }

        public static float tHardGaugeGetKillscreenRatio(Difficulty diff, int level, EGaugeType gaugeType, int perfectHits, int totalNotes)
        {
            if (gaugeType != EGaugeType.EXTREME) return 0f;

            float norma = tGetCurrentGaugeNorma(diff, level);
            float ratio = Math.Min(1f, Math.Max(0f, perfectHits / (float)totalNotes));

            return ratio * norma;
        }

        public static bool tIsDangerHardGauge(Difficulty diff, int level, EGaugeType gaugeType, float percentObtained, int perfectHits, int totalNotes)
        {
            if (gaugeType == EGaugeType.NORMAL || diff > Difficulty.Edit) return false;
            float percent = Math.Min(100f, Math.Max(0f, percentObtained));
            return percent < 100f - ((100f - tHardGaugeGetKillscreenRatio(diff, level, gaugeType, perfectHits, totalNotes)) * 0.7f);
        }

        public static float tGetCurrentGaugeNorma(Difficulty diff, int level)
        {
            float norma = 80f;

            if (DifficultyToNorma.ContainsKey(diff))
                norma = DifficultyToNorma[diff];

            int levelCaped = Math.Min(13, level);
            if (LevelExtraToNorma.ContainsKey(levelCaped))
                norma = LevelExtraToNorma[levelCaped];

            return norma;
        }

        public static EGaugeType tGetGaugeTypeEnum(string gaugeType)
        {
            EGaugeType gt = EGaugeType.NORMAL;

            if (GaugeTypeStringToEnum.ContainsKey(gaugeType))
                gt = GaugeTypeStringToEnum[gaugeType];

            return gt;
        }

        public static bool tNormaCheck(Difficulty diff, int level, EGaugeType gaugeType, float percentObtained, float killZonePercent)
        {
            float percent = Math.Min(100f, Math.Max(0f, percentObtained));
            float norma = tGetCurrentGaugeNorma(diff, level);

            if ((gaugeType == EGaugeType.HARD || gaugeType == EGaugeType.EXTREME) && percent > killZonePercent) return true;
            if (gaugeType == EGaugeType.NORMAL && percent >= norma) return true;

            return false;
        }

        #endregion

        #region [Displayables]

        public static void tDrawGaugeBase(CTexture baseTexture, int x, int y, float scale_x, float scale_y)
        {
            if (baseTexture != null)
            {
                baseTexture.vc拡大縮小倍率.X = scale_x;
                baseTexture.vc拡大縮小倍率.Y = scale_y;

                baseTexture.t2D描画(x, y,
                new Rectangle(
                    GaugeBox[0],
                    GaugeBox[1],
                    GaugeBox[2],
                    GaugeBox[3])
                );
            }
        }

        public static void tDrawGaugeBaseClear(CTexture baseTexture, int x, int y, Difficulty diff, int level, EGaugeType gaugeType, float scale_x, float scale_y)
        {
            if (gaugeType != EGaugeType.NORMAL || diff > Difficulty.Edit) return;
            float norma = tGetCurrentGaugeNorma(diff, level) - 2f; // A segment flashes earlier
            float revnorma = 100f - norma;

            if (baseTexture != null)
            {
                baseTexture.vc拡大縮小倍率.X = scale_x;
                baseTexture.vc拡大縮小倍率.Y = scale_y;

                int gaugeTexLength = GaugeBox[2];
                int clearPartLength = (int)(gaugeTexLength * (revnorma / 100f));
                int texStartPoint = (int)(gaugeTexLength * (norma / 100f));
                int xOff = (int)(scale_x * texStartPoint);

                baseTexture.t2D描画(x + xOff, y,
                new Rectangle(
                    GaugeBox[0] + texStartPoint,
                    GaugeBox[1],
                    clearPartLength,
                    GaugeBox[3])
                );
            }
        }

        public static void tDrawGaugeFill(
            CTexture fillTexture, 
            CTexture yellowTexture, 
            CTexture rainbowTexture, 
            int x, 
            int y,
            int rainbow_x,
            int rainbow_y,
            Difficulty diff, 
            int level, 
            float currentPercent, 
            EGaugeType gaugeType,
            float scale_x, 
            float scale_y,
            int flashOpacity,
            int perfectHits, 
            int totalNotes
            )
        {
            if (diff > Difficulty.Edit) return;
            float norma = tGetCurrentGaugeNorma(diff, level) - 2f; // A segment flashes earlier
            float percent = Math.Min(100f, Math.Max(0f, currentPercent));

            int gaugeTexLength = GaugeBox[2];
            float nWidth = (gaugeTexLength / 50f);
            int closestTwo = (int)((int)(percent / 2) * nWidth);
            int closestNorma = (gaugeType != EGaugeType.NORMAL) ? gaugeTexLength : (int)(gaugeTexLength * (norma / 100f));

            bool normaCheck = tNormaCheck(diff, level, gaugeType, percent, 0);
            bool isRainbow = (rainbowTexture != null && percent >= 100f && gaugeType == EGaugeType.NORMAL);

            // Fill
            if (fillTexture != null && !isRainbow)
            {
                fillTexture.vc拡大縮小倍率.X = scale_x;
                fillTexture.vc拡大縮小倍率.Y = scale_y;

                fillTexture.Opacity = 255;
                if (gaugeType != EGaugeType.NORMAL && tIsDangerHardGauge(diff, level, gaugeType, percent, perfectHits, totalNotes)) fillTexture.Opacity = 255 - flashOpacity;
                fillTexture.t2D描画(x, y,
                    new Rectangle(
                    GaugeBox[0],
                    GaugeBox[1],
                    Math.Min(closestTwo, closestNorma),
                    GaugeBox[3])
                );
            }

            if (gaugeType != EGaugeType.NORMAL) return;
            if (!normaCheck) return;

            // Yellow
            if (yellowTexture != null && !isRainbow)
            {
                int texStartPoint = (int)(gaugeTexLength * (norma / 100f));
                int differencial = closestTwo - closestNorma;
                int xOff = (int)(scale_x * texStartPoint);

                yellowTexture.vc拡大縮小倍率.X = scale_x;
                yellowTexture.vc拡大縮小倍率.Y = scale_y;

                yellowTexture.Opacity = 255;
                yellowTexture.t2D描画(x + xOff, y,
                    new Rectangle(
                    GaugeBox[0] + texStartPoint,
                    GaugeBox[1],
                    differencial,
                    GaugeBox[3])
                );
            }

            // Rainbow
            if (rainbowTexture != null && percent >= 100f)
            {
                rainbowTexture.vc拡大縮小倍率.X = scale_x;
                rainbowTexture.vc拡大縮小倍率.Y = scale_y;

                rainbowTexture.t2D描画(rainbow_x, rainbow_y);
            }

        }

        public static void tDrawGaugeFlash(CTexture flashTexture, int x, int y, int Opacity, Difficulty diff, int level, float currentPercent, EGaugeType gaugeType, float scale_x, float scale_y)
        {
            if (gaugeType != EGaugeType.NORMAL || diff > Difficulty.Edit) return ;
            float norma = tGetCurrentGaugeNorma(diff, level) - 2f; // A segment flashes earlier
            float percent = Math.Min(100f, Math.Max(0f, currentPercent));
            if (tNormaCheck(diff, level, gaugeType, percent, 0) && percent < 100.0)
            {
                if (flashTexture != null)
                {
                    flashTexture.vc拡大縮小倍率.X = scale_x;
                    flashTexture.vc拡大縮小倍率.Y = scale_y;

                    flashTexture.Opacity = Opacity;
                    flashTexture.t2D描画(x, y,
                    new Rectangle(
                    GaugeBox[0],
                    GaugeBox[1], 
                    (int)(GaugeBox[2] * (norma / 100f)),
                    GaugeBox[3])
                    );
                }

            }
        }

        public static void tDrawKillZone(CTexture killzoneTexture, int x, int y, Difficulty diff, int level, EGaugeType gaugeType, float scale_x, float scale_y, int perfectHits, int totalNotes)
        {
            if (gaugeType != EGaugeType.EXTREME || diff > Difficulty.Edit) return;
            float currentFill = tHardGaugeGetKillscreenRatio(diff, level, gaugeType, perfectHits, totalNotes);
            if (killzoneTexture != null)
            {
                killzoneTexture.vc拡大縮小倍率.X = scale_x;
                killzoneTexture.vc拡大縮小倍率.Y = scale_y;

                killzoneTexture.t2D描画(x, y,
                    new Rectangle(
                    GaugeBox[0],
                    GaugeBox[1],
                    (int)(GaugeBox[2] * (currentFill / 100f)),
                    GaugeBox[3])
                );
            }
        }

        public static void tDrawClearIcon(CTexture clearIcon, Difficulty diff, int level, float currentPercent, int text_x, int text_y, EGaugeType gaugeType, int perfectHits, int totalNotes, Rectangle clearRect, Rectangle clearRectHighlight)
        {
            if (clearIcon == null) return;
            if (diff > Difficulty.Edit) return;

            float percent = Math.Min(100f, Math.Max(0f, currentPercent));
            bool highlight = (gaugeType != EGaugeType.NORMAL)
                ? tIsDangerHardGauge(diff, level, gaugeType, currentPercent, perfectHits, totalNotes)
                : tNormaCheck(diff, level, gaugeType, percent, 0);

            clearIcon.Opacity = 255;
            if (highlight)
            {
                clearIcon.t2D描画(text_x, text_y, clearRectHighlight);
            }
            else
            {
                clearIcon.t2D描画(text_x, text_y, clearRect);
            }
        }

        public static void tDrawSoulFire(CTexture soulFire, Difficulty diff, int level, float currentPercent, EGaugeType gaugeType, float scale_x, float scale_y, int fire_x, int fire_y, int fireFrame)
        {
            if (soulFire == null) return;
            if (gaugeType != EGaugeType.NORMAL || diff > Difficulty.Edit) return;
            float percent = Math.Min(100f, Math.Max(0f, currentPercent));

            int soulfire_width = soulFire.szテクスチャサイズ.Width / 8;
            int soulfire_height = soulFire.szテクスチャサイズ.Height;

            if (percent >= 100.0)
            {
                soulFire.vc拡大縮小倍率.X = scale_x;
                soulFire.vc拡大縮小倍率.Y = scale_y;

                soulFire.t2D描画(fire_x, fire_y, new Rectangle(soulfire_width * fireFrame, 0, soulfire_width, soulfire_height));
            }
        }

        public static void tDrawSoulLetter(CTexture soulLetter, Difficulty diff, int level, float currentPercent, EGaugeType gaugeType, float scale_x, float scale_y, int soul_x, int soul_y)
        {
            if (soulLetter == null) return;
            if (gaugeType != EGaugeType.NORMAL || diff > Difficulty.Edit) return;
            float norma = tGetCurrentGaugeNorma(diff, level);
            float percent = Math.Min(100f, Math.Max(0f, currentPercent));

            soulLetter.vc拡大縮小倍率.X = scale_x;
            soulLetter.vc拡大縮小倍率.Y = scale_y;

            int soul_height = soulLetter.szテクスチャサイズ.Height / 2;
            if (tNormaCheck(diff, level, gaugeType, percent, 0))
            {
                soulLetter.t2D描画(soul_x, soul_y, new Rectangle(0, 0, soulLetter.szテクスチャサイズ.Width, soul_height));
            }
            else
            {
                soulLetter.t2D描画(soul_x, soul_y, new Rectangle(0, soul_height, soulLetter.szテクスチャサイズ.Width, soul_height));
            }
        }

        public static void tDrawCompleteGauge(
            CTexture baseTexture,
            CTexture baseNormaTexture,
            CTexture flashTexture, 
            CTexture fillTexture,
            CTexture yellowTexture,
            CTexture[] rainbowTextureArr,
            CTexture killzoneTexture,
            CTexture clearIcon,
            CTexture soulLetter,
            CTexture soulFire,
            int x,
            int y,
            int rainbow_x,
            int rainbow_y,
            int Opacity,
            int RainbowTextureIndex,
            int SoulFireIndex,
            Difficulty diff, 
            int level, 
            float currentPercent, 
            EGaugeType gaugeType,
            float scale_x, 
            float scale_y,
            int text_x,
            int text_y,
            int perfectHits,
            int totalNotes,
            int soul_x,
            int soul_y,
            int fire_x,
            int fire_y, 
            Rectangle clearRect, 
            Rectangle clearRectHighlight
            )
        {
            // Layers : Base - Base clear - Fill - Flash - Killzone - Clear logo - Soul fire - Soul text
            tDrawGaugeBase(baseTexture, x, y, scale_x, scale_y);
            tDrawGaugeBaseClear(baseNormaTexture, x, y, diff, level, gaugeType, scale_x, scale_y);
            tDrawGaugeFill(fillTexture, yellowTexture, (rainbowTextureArr != null && RainbowTextureIndex < rainbowTextureArr.Length) ? rainbowTextureArr[RainbowTextureIndex] : null, x, y, rainbow_x, rainbow_y, diff, level, currentPercent, gaugeType, scale_x, scale_y, Opacity, perfectHits, totalNotes);
            if (!TJAPlayer3.ConfigIni.SimpleMode) tDrawGaugeFlash(flashTexture, x, y, Opacity, diff, level, currentPercent, gaugeType, scale_x, scale_y);
            tDrawKillZone(killzoneTexture, x, y, diff, level, gaugeType, scale_x, scale_y, perfectHits, totalNotes);
            tDrawClearIcon(clearIcon, diff, level, currentPercent, text_x, text_y, gaugeType, perfectHits, totalNotes, clearRect, clearRectHighlight);
            tDrawSoulFire(soulFire, diff, level, currentPercent, gaugeType, scale_x, scale_y, fire_x, fire_y, SoulFireIndex);
            tDrawSoulLetter(soulLetter, diff, level, currentPercent, gaugeType, scale_x, scale_y, soul_x, soul_y);
        }

        #endregion

        // Use with caution
        #region [Unsafe methods]

        public static bool UNSAFE_FastNormaCheck(int player)
        {
            var chara = TJAPlayer3.Tx.Characters[TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data.Character];
            var _dif = TJAPlayer3.stage選曲.n確定された曲の難易度[player];
            return tNormaCheck(
                (Difficulty)_dif,
                TJAPlayer3.stage選曲.r確定された曲.arスコア[_dif].譜面情報.nレベル[_dif],
                tGetGaugeTypeEnum(chara.effect.tGetGaugeType()),
                (float)TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[player],
                UNSAFE_KillZonePercent(player)
                );
        }

        public static bool UNSAFE_IsRainbow(int player)
        {
            var chara = TJAPlayer3.Tx.Characters[TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data.Character];
            if (tGetGaugeTypeEnum(chara.effect.tGetGaugeType()) != EGaugeType.NORMAL) return false;
            return (float)TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[player] >= 100f;
        }

        public static float UNSAFE_KillZonePercent(int player)
        {
            var chara = TJAPlayer3.Tx.Characters[TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data.Character];
            CDTX[] dtxs =
            {
                TJAPlayer3.DTX,
                TJAPlayer3.DTX_2P,
                TJAPlayer3.DTX_3P,
                TJAPlayer3.DTX_4P,
                TJAPlayer3.DTX_5P
            };

            // Total hits and perfect hits
            int perfectHits = TJAPlayer3.stage演奏ドラム画面.CChartScore[player].nGreat;
            int totalHits = dtxs[player].nノーツ数[3];

            // Difficulty
            int _dif = TJAPlayer3.stage選曲.n確定された曲の難易度[player];
            Difficulty difficulty = (Difficulty)_dif;
            int level = TJAPlayer3.stage選曲.r確定された曲.arスコア[_dif].譜面情報.nレベル[_dif];

            return tHardGaugeGetKillscreenRatio(
                difficulty,
                level,
                tGetGaugeTypeEnum(chara.effect.tGetGaugeType()),
                perfectHits,
                totalHits);
        }

        public static void UNSAFE_DrawGaugeFast(int player, int opacity, int rainbowTextureIndex, int soulFlameIndex)
        {
            var chara = TJAPlayer3.Tx.Characters[TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data.Character];
            CDTX[] dtxs =
            {
                TJAPlayer3.DTX,
                TJAPlayer3.DTX_2P,
                TJAPlayer3.DTX_3P,
                TJAPlayer3.DTX_4P,
                TJAPlayer3.DTX_5P
            };

            // Set box
            GaugeBox = new int[]{ TJAPlayer3.Skin.Game_Gauge_Rect[0], TJAPlayer3.Skin.Game_Gauge_Rect[1], TJAPlayer3.Skin.Game_Gauge_Rect[2], TJAPlayer3.Skin.Game_Gauge_Rect[3] };

            // Gauge pos
            int gauge_x = 0;
            int gauge_y = 0;

            if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
            {
                gauge_x = TJAPlayer3.Skin.Game_Gauge_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * player);
                gauge_y = TJAPlayer3.Skin.Game_Gauge_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * player);
            }
            else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
            {
                gauge_x = TJAPlayer3.Skin.Game_Gauge_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * player);
                gauge_y = TJAPlayer3.Skin.Game_Gauge_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * player);
            }
            else if (TJAPlayer3.ConfigIni.bAIBattleMode)
            {
                gauge_x = TJAPlayer3.Skin.Game_Gauge_X_AI;
                gauge_y = TJAPlayer3.Skin.Game_Gauge_Y_AI;
            }
            else
            {
                gauge_x = TJAPlayer3.Skin.Game_Gauge_X[player];
                gauge_y = TJAPlayer3.Skin.Game_Gauge_Y[player];
            }

            // Text pos
            int text_x = 0;
            int text_y = 0;
            if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
            {
                if (TJAPlayer3.ConfigIni.bAIBattleMode)
                {
                    text_x = TJAPlayer3.Skin.Game_Gauge_ClearText_X_AI;
                    text_y = TJAPlayer3.Skin.Game_Gauge_ClearText_Y_AI;
                }
                else
                {
                    text_x = TJAPlayer3.Skin.Game_Gauge_ClearText_X[player];
                    text_y = TJAPlayer3.Skin.Game_Gauge_ClearText_Y[player];
                }
            }

            // Soul pos
            int soul_x = 0;
            int soul_y = 0;
            if (TJAPlayer3.ConfigIni.bAIBattleMode)
            {
                soul_x = TJAPlayer3.Skin.Gauge_Soul_X_AI;
                soul_y = TJAPlayer3.Skin.Gauge_Soul_Y_AI;
            }
            else
            {
                if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
                {
                    soul_x = TJAPlayer3.Skin.Gauge_Soul_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * player);
                    soul_y = TJAPlayer3.Skin.Gauge_Soul_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * player);
                }
                else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
                {
                    soul_x = TJAPlayer3.Skin.Gauge_Soul_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * player);
                    soul_y = TJAPlayer3.Skin.Gauge_Soul_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * player);
                }
                else
                {
                    soul_x = TJAPlayer3.Skin.Gauge_Soul_X[player];
                    soul_y = TJAPlayer3.Skin.Gauge_Soul_Y[player];
                }
            }

            // Fire pos
            int fire_x = 0;
            int fire_y = 0;
            if (TJAPlayer3.ConfigIni.bAIBattleMode)
            {
                fire_x = TJAPlayer3.Skin.Gauge_Soul_Fire_X_AI;
                fire_y = TJAPlayer3.Skin.Gauge_Soul_Fire_Y_AI;
            }
            else
            {
                if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
                {
                    fire_x = TJAPlayer3.Skin.Gauge_Soul_Fire_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * player);
                    fire_y = TJAPlayer3.Skin.Gauge_Soul_Fire_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * player);
                }
                else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
                {
                    fire_x = TJAPlayer3.Skin.Gauge_Soul_Fire_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * player);
                    fire_y = TJAPlayer3.Skin.Gauge_Soul_Fire_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * player);
                }
                else
                {
                    fire_x = TJAPlayer3.Skin.Gauge_Soul_Fire_X[player];
                    fire_y = TJAPlayer3.Skin.Gauge_Soul_Fire_Y[player];
                }
            }

            // Total hits and perfect hits
            int perfectHits = TJAPlayer3.stage演奏ドラム画面.CChartScore[player].nGreat;
            int totalHits = dtxs[player].nノーツ数[3];

            // Scale 
            float scale = 1.0f;
            if (TJAPlayer3.ConfigIni.bAIBattleMode)
            {
                scale = 0.8f;
            }

            // Difficulty
            int _dif = TJAPlayer3.stage選曲.n確定された曲の難易度[player];
            Difficulty difficulty = (Difficulty)_dif;
            int level = TJAPlayer3.stage選曲.r確定された曲.arスコア[_dif].譜面情報.nレベル[_dif];

            // Current percent
            float currentPercent = (float)TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[player];

            // Gauge type
            EGaugeType gaugeType = tGetGaugeTypeEnum(chara.effect.tGetGaugeType());

            // Textures
            int _4pGaugeIDX = (TJAPlayer3.ConfigIni.nPlayerCount >= 3) ? 1 : 0;
            int _usedGauge = player + 3 * _4pGaugeIDX;
            if (TJAPlayer3.P1IsBlue()) _usedGauge = 2;
            _4pGaugeIDX = (TJAPlayer3.ConfigIni.nPlayerCount >= 3) ? 2
                    : (player == 1) ? 1
                    : 0;

            CTexture baseTexture = TJAPlayer3.Tx.Gauge_Base[_usedGauge];
            CTexture fillTexture = TJAPlayer3.Tx.Gauge[_usedGauge];

            CTexture[] rainbowTextureArr = (new CTexture[][]{ TJAPlayer3.Tx.Gauge_Rainbow , TJAPlayer3.Tx.Gauge_Rainbow_2PGauge, TJAPlayer3.Tx.Gauge_Rainbow_Flat })[_4pGaugeIDX];
            CTexture yellowTexture = TJAPlayer3.Tx.Gauge_Clear[_4pGaugeIDX];
            CTexture baseNormaTexture = TJAPlayer3.Tx.Gauge_Base_Norma[_4pGaugeIDX];
            CTexture killzoneTexture = TJAPlayer3.Tx.Gauge_Killzone[_4pGaugeIDX];
            
            CTexture flashTexture = yellowTexture;
            CTexture clearIcon = (_4pGaugeIDX == 2) 
                ? null 
                : (gaugeType != EGaugeType.NORMAL)
                    ? TJAPlayer3.Tx.Gauge_Killzone[0]
                    : TJAPlayer3.Tx.Gauge[0];
            CTexture soulLetter = TJAPlayer3.Tx.Gauge_Soul;
            CTexture soulFlame = TJAPlayer3.Tx.Gauge_Soul_Fire;

            // Rectangles
            Rectangle clearRectHighlight = new Rectangle(
                        TJAPlayer3.Skin.Game_Gauge_ClearText_Rect[0],
                        TJAPlayer3.Skin.Game_Gauge_ClearText_Rect[1],
                        TJAPlayer3.Skin.Game_Gauge_ClearText_Rect[2],
                        TJAPlayer3.Skin.Game_Gauge_ClearText_Rect[3]
                        );

            Rectangle clearRect = new Rectangle(
                        TJAPlayer3.Skin.Game_Gauge_ClearText_Clear_Rect[0],
                        TJAPlayer3.Skin.Game_Gauge_ClearText_Clear_Rect[1],
                        TJAPlayer3.Skin.Game_Gauge_ClearText_Clear_Rect[2],
                        TJAPlayer3.Skin.Game_Gauge_ClearText_Clear_Rect[3]
                        );

            tDrawCompleteGauge(baseTexture, baseNormaTexture, flashTexture, fillTexture, yellowTexture, rainbowTextureArr, killzoneTexture, clearIcon, soulLetter, soulFlame, gauge_x, gauge_y, gauge_x, gauge_y, opacity, rainbowTextureIndex, soulFlameIndex, difficulty, level, currentPercent, gaugeType, scale, scale, text_x, text_y, perfectHits, totalHits, soul_x, soul_y, fire_x, fire_y, clearRect, clearRectHighlight);
        }

        public static void UNSAFE_DrawResultGaugeFast(int player, int shiftPos, int pos, int segmentsDisplayed, int rainbowTextureIndex, int soulFlameIndex, int uioffset_x)
        {
            var chara = TJAPlayer3.Tx.Characters[TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data.Character];
            CDTX[] dtxs =
            {
                TJAPlayer3.DTX,
                TJAPlayer3.DTX_2P,
                TJAPlayer3.DTX_3P,
                TJAPlayer3.DTX_4P,
                TJAPlayer3.DTX_5P
            };

            // Set box
            GaugeBox = new int[] { TJAPlayer3.Skin.Result_Gauge_Rect[0], TJAPlayer3.Skin.Result_Gauge_Rect[1], TJAPlayer3.Skin.Result_Gauge_Rect[2], TJAPlayer3.Skin.Result_Gauge_Rect[3] };

            // Total hits and perfect hits
            int perfectHits = TJAPlayer3.stage演奏ドラム画面.CChartScore[player].nGreat;
            int totalHits = dtxs[player].nノーツ数[3];

            // Gauge type
            EGaugeType gaugeType = tGetGaugeTypeEnum(chara.effect.tGetGaugeType());

            // Current percent
            float currentPercent = segmentsDisplayed * 2f;

            // Scale x
            float scale_x = 1.0f;
            if (TJAPlayer3.ConfigIni.nPlayerCount >= 3)
            {
                scale_x = 0.5f;
            }

            // Difficulty
            int _dif = TJAPlayer3.stage選曲.n確定された曲の難易度[player];
            Difficulty difficulty = (Difficulty)_dif;
            int level = TJAPlayer3.stage選曲.r確定された曲.arスコア[_dif].譜面情報.nレベル[_dif];

            int gauge_x;
            int gauge_y;
            if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
            {
                gauge_x = TJAPlayer3.Skin.Result_Gauge_5P[0] + TJAPlayer3.Skin.Result_UIMove_5P_X[pos];
                gauge_y = TJAPlayer3.Skin.Result_Gauge_5P[1] + TJAPlayer3.Skin.Result_UIMove_5P_Y[pos];
            }
            else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
            {
                gauge_x = TJAPlayer3.Skin.Result_Gauge_4P[0] + TJAPlayer3.Skin.Result_UIMove_4P_X[pos];
                gauge_y = TJAPlayer3.Skin.Result_Gauge_4P[1] + TJAPlayer3.Skin.Result_UIMove_4P_Y[pos];
            }
            else
            {
                gauge_x = TJAPlayer3.Skin.Result_Gauge_X[pos] + uioffset_x;
                gauge_y = TJAPlayer3.Skin.Result_Gauge_Y[pos];
            }

            int gauge_rainbow_x;
            int gauge_rainbow_y;
            if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
            {
                gauge_rainbow_x = TJAPlayer3.Skin.Result_Gauge_Rainbow_5P[0] + TJAPlayer3.Skin.Result_UIMove_5P_X[pos];
                gauge_rainbow_y = TJAPlayer3.Skin.Result_Gauge_Rainbow_5P[1] + TJAPlayer3.Skin.Result_UIMove_5P_Y[pos];
            }
            else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
            {
                gauge_rainbow_x = TJAPlayer3.Skin.Result_Gauge_Rainbow_4P[0] + TJAPlayer3.Skin.Result_UIMove_4P_X[pos];
                gauge_rainbow_y = TJAPlayer3.Skin.Result_Gauge_Rainbow_4P[1] + TJAPlayer3.Skin.Result_UIMove_4P_Y[pos];
            }
            else
            {
                gauge_rainbow_x = TJAPlayer3.Skin.Result_Gauge_Rainbow_X[pos] + uioffset_x;
                gauge_rainbow_y = TJAPlayer3.Skin.Result_Gauge_Rainbow_Y[pos];
            }

            // Flame and soul
            int soulText_x;
            int soulText_y;
            int soulFire_x;
            int soulFire_y;
            if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
            {
                soulText_x = TJAPlayer3.Skin.Result_Soul_Text_5P[0] + TJAPlayer3.Skin.Result_UIMove_5P_X[pos];
                soulText_y = TJAPlayer3.Skin.Result_Soul_Text_5P[1] + TJAPlayer3.Skin.Result_UIMove_5P_Y[pos];
                soulFire_x = TJAPlayer3.Skin.Result_Soul_Fire_5P[0] + TJAPlayer3.Skin.Result_UIMove_5P_X[pos];
                soulFire_y = TJAPlayer3.Skin.Result_Soul_Fire_5P[1] + TJAPlayer3.Skin.Result_UIMove_5P_Y[pos];
            }
            else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
            {
                soulText_x = TJAPlayer3.Skin.Result_Soul_Text_4P[0] + TJAPlayer3.Skin.Result_UIMove_4P_X[0];
                soulText_y = TJAPlayer3.Skin.Result_Soul_Text_4P[1] + TJAPlayer3.Skin.Result_UIMove_4P_Y[1];
                soulFire_x = TJAPlayer3.Skin.Result_Soul_Fire_4P[0] + TJAPlayer3.Skin.Result_UIMove_4P_X[0];
                soulFire_y = TJAPlayer3.Skin.Result_Soul_Fire_4P[1] + TJAPlayer3.Skin.Result_UIMove_4P_Y[1];
            }
            else
            {
                soulText_x = TJAPlayer3.Skin.Result_Soul_Text_X[pos] + uioffset_x;
                soulText_y = TJAPlayer3.Skin.Result_Soul_Text_Y[pos];
                soulFire_x = TJAPlayer3.Skin.Result_Soul_Fire_X[pos] + uioffset_x;
                soulFire_y = TJAPlayer3.Skin.Result_Soul_Fire_Y[pos];
            }

            // Clear text
            int clearText_x;
            int clearText_y;
            if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
            {
                clearText_x = TJAPlayer3.Skin.Result_Gauge_ClearText_5P[0] + TJAPlayer3.Skin.Result_UIMove_5P_X[pos];
                clearText_y = TJAPlayer3.Skin.Result_Gauge_ClearText_5P[1] + TJAPlayer3.Skin.Result_UIMove_5P_Y[pos];
            }
            else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
            {
                clearText_x = TJAPlayer3.Skin.Result_Gauge_ClearText_4P[0] + TJAPlayer3.Skin.Result_UIMove_4P_X[pos];
                clearText_y = TJAPlayer3.Skin.Result_Gauge_ClearText_4P[1] + TJAPlayer3.Skin.Result_UIMove_4P_Y[pos];
            }
            else
            {
                clearText_x = TJAPlayer3.Skin.Result_Gauge_ClearText_X[pos] + uioffset_x;
                clearText_y = TJAPlayer3.Skin.Result_Gauge_ClearText_Y[pos];
            }

            // Textures
            int _usedGauge = shiftPos;
            CTexture baseTexture = TJAPlayer3.Tx.Result_Gauge_Base[_usedGauge];
            CTexture fillTexture = TJAPlayer3.Tx.Result_Gauge[_usedGauge];

            CTexture[] rainbowTextureArr = TJAPlayer3.Tx.Result_Rainbow;
            CTexture yellowTexture = TJAPlayer3.Tx.Result_Gauge_Clear;
            CTexture baseNormaTexture = TJAPlayer3.Tx.Result_Gauge_Clear_Base;
            CTexture killzoneTexture = TJAPlayer3.Tx.Result_Gauge_Killzone;

            CTexture flashTexture = null;
            CTexture clearIcon = (gaugeType != EGaugeType.NORMAL)
                    ? TJAPlayer3.Tx.Result_Gauge_Killzone
                    : TJAPlayer3.Tx.Result_Gauge[0];

            CTexture soulLetter = TJAPlayer3.Tx.Result_Soul_Text;
            CTexture soulFlame = TJAPlayer3.Tx.Result_Soul_Fire;

            // Rectangles
            Rectangle clearRectHighlight = new Rectangle(
                        TJAPlayer3.Skin.Result_Gauge_ClearText_Clear_Rect[0], 
                        TJAPlayer3.Skin.Result_Gauge_ClearText_Clear_Rect[1], 
                        TJAPlayer3.Skin.Result_Gauge_ClearText_Clear_Rect[2], 
                        TJAPlayer3.Skin.Result_Gauge_ClearText_Clear_Rect[3]
                        );

            Rectangle clearRect = new Rectangle(
                        TJAPlayer3.Skin.Result_Gauge_ClearText_Rect[0], 
                        TJAPlayer3.Skin.Result_Gauge_ClearText_Rect[1], 
                        TJAPlayer3.Skin.Result_Gauge_ClearText_Rect[2], 
                        TJAPlayer3.Skin.Result_Gauge_ClearText_Rect[3]
                        );

            // Positionnings 
            if (soulLetter != null)
            {
                soulText_x -= (int)((soulLetter.szテクスチャサイズ.Width / 2));
                soulText_y -= (soulLetter.szテクスチャサイズ.Height / 4);
            }
            if (soulFlame != null)
            {
                soulFire_y -= (soulFlame.szテクスチャサイズ.Height / 2);
                soulFire_x -= (int)((soulFlame.szテクスチャサイズ.Width / 16));
            }
            
            tDrawCompleteGauge(baseTexture, baseNormaTexture, flashTexture, fillTexture, yellowTexture, rainbowTextureArr, killzoneTexture, clearIcon, null, null, gauge_x, gauge_y, gauge_rainbow_x, gauge_rainbow_y, 0, rainbowTextureIndex, soulFlameIndex, difficulty, level, currentPercent, gaugeType, scale_x, 1f, clearText_x, clearText_y, perfectHits, totalHits, soulText_x, soulText_y, soulFire_x, soulFire_y, clearRect, clearRectHighlight);
            tDrawSoulFire(soulFlame, difficulty, level, currentPercent, gaugeType, 1f, 1f, soulFire_x, soulFire_y, soulFlameIndex);
            tDrawSoulLetter(soulLetter, difficulty, level, currentPercent, gaugeType, 1f, 1f, soulText_x, soulText_y);
        }

        #endregion

        private static int[] GaugeBox = { TJAPlayer3.Skin.Game_Gauge_Rect[0], TJAPlayer3.Skin.Game_Gauge_Rect[1], TJAPlayer3.Skin.Game_Gauge_Rect[2], TJAPlayer3.Skin.Game_Gauge_Rect[3] };

    }
}
