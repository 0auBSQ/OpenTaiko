using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using FDK;


namespace TJAPlayer3
{
    //クラスの設置位置は必ず演奏画面共通に置くこと。
    //そうしなければBPM変化に対応できません。

    //完成している部分は以下のとおり。(画像完成+動作確認完了で完成とする)
    //_通常モーション
    //_ゴーゴータイムモーション
    //_クリア時モーション
    //
    internal class CAct演奏Drumsキャラクター : CActivity
    {
        public CAct演奏Drumsキャラクター()
        {

        }

        public override void Activate()
        {
            for(int i = 0; i < 5; i++)
            {

                /*
                ctChara_Normal[i] = new CCounter();
                ctChara_Miss[i] = new CCounter();
                ctChara_MissDown[i] = new CCounter();
                ctChara_GoGo[i] = new CCounter();
                ctChara_Clear[i] = new CCounter();

                this.ctキャラクターアクション_10コンボ[i] = new CCounter();
                this.ctキャラクターアクション_10コンボMAX[i] = new CCounter();
                this.ctキャラクターアクション_ゴーゴースタート[i] = new CCounter();
                this.ctキャラクターアクション_ゴーゴースタートMAX[i] = new CCounter();
                this.ctキャラクターアクション_ノルマ[i] = new CCounter();
                this.ctキャラクターアクション_魂MAX[i] = new CCounter();
                this.ctキャラクターアクション_Return[i] = new CCounter();
                */

                //CharaAction_Balloon_Breaking[i] = new CCounter();
                //CharaAction_Balloon_Broke[i] = new CCounter();
                //CharaAction_Balloon_Miss[i] = new CCounter();
                CharaAction_Balloon_Delay[i] = new CCounter();
                ctKusuIn[i] = new();

                // Currently used character
                int p = TJAPlayer3.GetActualPlayer(i);

                this.iCurrentCharacter[i] = Math.Max(0, Math.Min(TJAPlayer3.SaveFileInstances[p].data.Character, TJAPlayer3.Skin.Characters_Ptn - 1));

                if (TJAPlayer3.Skin.Characters_Normal_Ptn[this.iCurrentCharacter[i]] != 0) ChangeAnime(i, Anime.Normal, true);
                else ChangeAnime(i, Anime.None, true);

                this.b風船連打中[i] = false;
                this.b演奏中[i] = false;

                // CharaAction_Balloon_FadeOut[i] = new Animations.FadeOut(TJAPlayer3.Skin.Game_Chara_Balloon_FadeOut);
                CharaAction_Balloon_FadeOut[i] = new Animations.FadeOut(TJAPlayer3.Skin.Characters_Balloon_FadeOut[this.iCurrentCharacter[i]]);

                var tick = TJAPlayer3.Skin.Characters_Balloon_Timer[this.iCurrentCharacter[i]];

                var balloonBrokePtn = TJAPlayer3.Skin.Characters_Balloon_Broke_Ptn[this.iCurrentCharacter[i]];
                var balloonMissPtn = TJAPlayer3.Skin.Characters_Balloon_Miss_Ptn[this.iCurrentCharacter[i]];

                CharaAction_Balloon_FadeOut_StartMs[i] = new int[2];

                CharaAction_Balloon_FadeOut_StartMs[i][0] = (balloonBrokePtn * tick) - TJAPlayer3.Skin.Characters_Balloon_FadeOut[this.iCurrentCharacter[i]];
                CharaAction_Balloon_FadeOut_StartMs[i][1] = (balloonMissPtn * tick) - TJAPlayer3.Skin.Characters_Balloon_FadeOut[this.iCurrentCharacter[i]];

                if (balloonBrokePtn > 1) CharaAction_Balloon_FadeOut_StartMs[i][0] /= balloonBrokePtn - 1;
                if (balloonMissPtn > 1) CharaAction_Balloon_FadeOut_StartMs[i][1] /= balloonMissPtn - 1; // - 1はタイマー用

                if (CharaAction_Balloon_Delay[i] != null) CharaAction_Balloon_Delay[i].CurrentValue = (int)CharaAction_Balloon_Delay[i].EndValue;
            }

            base.Activate();
        }

        public override void DeActivate()
        {
            for (int i = 0; i < 5; i++)
            {
                /*
                ctChara_Normal[i] = null;
                ctChara_Miss[i] = null;
                ctChara_MissDown[i] = null;
                ctChara_GoGo[i] = null;
                ctChara_Clear[i] = null;
                this.ctキャラクターアクション_10コンボ[i] = null;
                this.ctキャラクターアクション_10コンボMAX[i] = null;
                this.ctキャラクターアクション_ゴーゴースタート[i] = null;
                this.ctキャラクターアクション_ゴーゴースタートMAX[i] = null;
                this.ctキャラクターアクション_ノルマ[i] = null;
                this.ctキャラクターアクション_魂MAX[i] = null;
                this.ctキャラクターアクション_Return[i] = null;
                */

                //CharaAction_Balloon_Breaking[i] = null;
                //CharaAction_Balloon_Broke[i] = null;
                //CharaAction_Balloon_Miss[i] = null;
                CharaAction_Balloon_Delay[i] = null;

                CharaAction_Balloon_FadeOut[i] = null;
            }

            base.DeActivate();
        }

        public override void CreateManagedResource()
        {
            for (int i = 0; i < 5; i++)
            {
                //this.arモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す(TJAPlayer3.Skin.Characters_Motion_Normal[this.iCurrentCharacter[i]]);
                //this.arMissモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す(TJAPlayer3.Skin.Characters_Motion_Miss[this.iCurrentCharacter[i]]);
                //this.arMissDownモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す(TJAPlayer3.Skin.Characters_Motion_MissDown[this.iCurrentCharacter[i]]);
                //this.arゴーゴーモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す(TJAPlayer3.Skin.Characters_Motion_GoGo[this.iCurrentCharacter[i]]);
                //this.arクリアモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す(TJAPlayer3.Skin.Characters_Motion_Clear[this.iCurrentCharacter[i]]);

                //if (arモーション番号[i] == null) this.arモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す("0,0");
                //if (arMissモーション番号[i] == null) this.arMissモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す("0,0");
                //if (arMissDownモーション番号[i] == null) this.arMissDownモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す("0,0");
                //if (arゴーゴーモーション番号[i] == null) this.arゴーゴーモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す("0,0");
                //if (arクリアモーション番号[i] == null) this.arクリアモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す("0,0");
            }
            base.CreateManagedResource();
        }

        public override void ReleaseManagedResource()
        {
            base.ReleaseManagedResource();
        }

        public override int Draw()
        {
            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                int Character = this.iCurrentCharacter[i];

                if (TJAPlayer3.Skin.Characters_Ptn == 0)
                    break;

                // Blinking animation during invincibility frames
                if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
                {
                    if (CFloorManagement.isBlinking() == true)
                        break;
                }

                CTexture nowChara = null;

                void updateNormal()
                {
                    if (!TJAPlayer3.stage演奏ドラム画面.bPAUSE)
                    {
                        nNowCharaCounter[i] += ((Math.Abs((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[i]) / 60.0f) * (float)TJAPlayer3.FPS.DeltaTime) / nCharaBeat[i];
                    }
                }
                void updateBalloon()
                {
                    if (!TJAPlayer3.stage演奏ドラム画面.bPAUSE)
                    {
                        nNowCharaCounter[i] += (float)TJAPlayer3.FPS.DeltaTime / nCharaBeat[i];
                    }
                }

                ctKusuIn[i].Tick();

                bool endAnime = nNowCharaCounter[i] >= 1;
                nNowCharaFrame[i] = (int)(nNowCharaCounter[i] * (nCharaFrameCount[i] + 1));
                nNowCharaFrame[i] = Math.Min(nNowCharaFrame[i], nCharaFrameCount[i]);

                if (eNowAnime[i] != Anime.None)
                {
                    switch (eNowAnime[i])
                    {
                        case Anime.None:
                            {
                                ReturnDefaultAnime(i, false);
                            }
                            break;
                        case Anime.Normal:
                            {
                                updateNormal();
                                ReturnDefaultAnime(i, false);
                                nowChara = TJAPlayer3.Tx.Characters_Normal[Character][TJAPlayer3.Skin.Characters_Motion_Normal[Character][nNowCharaFrame[i]]];
                                if (endAnime)
                                {
                                    nNowCharaCounter[i] = 0;
                                    nNowCharaFrame[i] = 0;
                                }
                            }
                            break;
                        case Anime.Miss:
                            {
                                updateNormal();
                                ReturnDefaultAnime(i, false);
                                nowChara = TJAPlayer3.Tx.Characters_Normal_Missed[Character][TJAPlayer3.Skin.Characters_Motion_Miss[Character][nNowCharaFrame[i]]];
                                if (endAnime)
                                {
                                    nNowCharaCounter[i] = 0;
                                    nNowCharaFrame[i] = 0;
                                }
                            }
                            break;
                        case Anime.MissDown:
                            {
                                updateNormal();
                                ReturnDefaultAnime(i, false);
                                nowChara = TJAPlayer3.Tx.Characters_Normal_MissedDown[Character][TJAPlayer3.Skin.Characters_Motion_MissDown[Character][nNowCharaFrame[i]]];
                                if (endAnime)
                                {
                                    nNowCharaCounter[i] = 0;
                                    nNowCharaFrame[i] = 0;
                                }
                            }
                            break;
                        case Anime.Cleared:
                            {
                                updateNormal();
                                ReturnDefaultAnime(i, false);
                                nowChara = TJAPlayer3.Tx.Characters_Normal_Cleared[Character][TJAPlayer3.Skin.Characters_Motion_Clear[Character][nNowCharaFrame[i]]];
                                if (endAnime)
                                {
                                    nNowCharaCounter[i] = 0;
                                    nNowCharaFrame[i] = 0;
                                }
                            }
                            break;
                        case Anime.Maxed:
                            {
                                updateNormal();
                                ReturnDefaultAnime(i, false);
                                nowChara = TJAPlayer3.Tx.Characters_Normal_Maxed[Character][TJAPlayer3.Skin.Characters_Motion_ClearMax[Character][nNowCharaFrame[i]]];
                                if (endAnime)
                                {
                                    nNowCharaCounter[i] = 0;
                                    nNowCharaFrame[i] = 0;
                                }
                            }
                            break;
                        case Anime.MissIn:
                            {
                                updateNormal();
                                if (TJAPlayer3.Tx.Characters_MissIn[Character] != null && TJAPlayer3.Skin.Characters_MissIn_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_MissIn[Character][TJAPlayer3.Skin.Characters_Motion_MissIn[Character][nNowCharaFrame[i]]];
                                }
                                if (endAnime)
                                {
                                    ReturnDefaultAnime(i, true);
                                }
                            }
                            break;
                        case Anime.MissDownIn:
                            {
                                updateNormal();
                                if (TJAPlayer3.Tx.Characters_MissDownIn[Character] != null && TJAPlayer3.Skin.Characters_MissDownIn_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_MissDownIn[Character][TJAPlayer3.Skin.Characters_Motion_MissDownIn[Character][nNowCharaFrame[i]]];
                                }
                                if (endAnime)
                                {
                                    ReturnDefaultAnime(i, true);
                                }
                            }
                            break;
                        case Anime.GoGoTime:
                            {
                                updateNormal();
                                ReturnDefaultAnime(i, false);
                                nowChara = TJAPlayer3.Tx.Characters_GoGoTime[Character][TJAPlayer3.Skin.Characters_Motion_GoGo[Character][nNowCharaFrame[i]]];
                                if (endAnime)
                                {
                                    nNowCharaCounter[i] = 0;
                                    nNowCharaFrame[i] = 0;
                                }
                            }
                            break;
                        case Anime.GoGoTime_Maxed:
                            {
                                updateNormal();
                                ReturnDefaultAnime(i, false);
                                nowChara = TJAPlayer3.Tx.Characters_GoGoTime_Maxed[Character][TJAPlayer3.Skin.Characters_Motion_GoGoMax[Character][nNowCharaFrame[i]]];
                                if (endAnime)
                                {
                                    nNowCharaCounter[i] = 0;
                                    nNowCharaFrame[i] = 0;
                                }
                            }
                            break;
                        case Anime.Combo10:
                            {
                                updateNormal();
                                if (TJAPlayer3.Tx.Characters_10Combo[Character] != null && TJAPlayer3.Skin.Characters_10Combo_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_10Combo[Character][TJAPlayer3.Skin.Characters_Motion_10Combo[Character][nNowCharaFrame[i]]];
                                }
                                if (endAnime)
                                {
                                    ReturnDefaultAnime(i, true);
                                }
                            }
                            break;
                        case Anime.Combo10_Clear:
                            {
                                updateNormal();
                                if (TJAPlayer3.Tx.Characters_10Combo_Clear[Character] != null && TJAPlayer3.Skin.Characters_10Combo_Clear_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_10Combo_Clear[Character][TJAPlayer3.Skin.Characters_Motion_10Combo_Clear[Character][nNowCharaFrame[i]]];
                                }
                                if (endAnime)
                                {
                                    ReturnDefaultAnime(i, true);
                                }
                            }
                            break;
                        case Anime.Combo10_Max:
                            {
                                updateNormal();
                                if (TJAPlayer3.Tx.Characters_10Combo_Maxed[Character] != null && TJAPlayer3.Skin.Characters_10Combo_Maxed_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_10Combo_Maxed[Character][TJAPlayer3.Skin.Characters_Motion_10ComboMax[Character][nNowCharaFrame[i]]];
                                }
                                if (endAnime)
                                {
                                    ReturnDefaultAnime(i, true);
                                }
                            }
                            break;
                        case Anime.GoGoStart:
                            {
                                updateNormal();
                                if (TJAPlayer3.Tx.Characters_GoGoStart[Character] != null && TJAPlayer3.Skin.Characters_GoGoStart_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_GoGoStart[Character][TJAPlayer3.Skin.Characters_Motion_GoGoStart[Character][nNowCharaFrame[i]]];
                                }
                                if (endAnime)
                                {
                                    ReturnDefaultAnime(i, true);
                                }
                            }
                            break;
                        case Anime.GoGoStart_Clear:
                            {
                                updateNormal();
                                if (TJAPlayer3.Tx.Characters_GoGoStart_Clear[Character] != null && TJAPlayer3.Skin.Characters_GoGoStart_Clear_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_GoGoStart_Clear[Character][TJAPlayer3.Skin.Characters_Motion_GoGoStart_Clear[Character][nNowCharaFrame[i]]];
                                }
                                if (endAnime)
                                {
                                    ReturnDefaultAnime(i, true);
                                }
                            }
                            break;
                        case Anime.GoGoStart_Max:
                            {
                                updateNormal();
                                if (TJAPlayer3.Tx.Characters_GoGoStart_Maxed[Character] != null && TJAPlayer3.Skin.Characters_GoGoStart_Maxed_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_GoGoStart_Maxed[Character][TJAPlayer3.Skin.Characters_Motion_GoGoStartMax[Character][nNowCharaFrame[i]]];
                                }
                                if (endAnime)
                                {
                                    ReturnDefaultAnime(i, true);
                                }
                            }
                            break;
                        case Anime.Become_Cleared:
                            {
                                updateNormal();
                                if (TJAPlayer3.Tx.Characters_Become_Cleared[Character] != null && TJAPlayer3.Skin.Characters_Become_Cleared_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_Become_Cleared[Character][TJAPlayer3.Skin.Characters_Motion_ClearIn[Character][nNowCharaFrame[i]]];
                                }
                                if (endAnime)
                                {
                                    ReturnDefaultAnime(i, true);
                                }
                            }
                            break;
                        case Anime.Become_Maxed:
                            {
                                updateNormal();
                                if (TJAPlayer3.Tx.Characters_Become_Maxed[Character] != null && TJAPlayer3.Skin.Characters_Become_Maxed_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_Become_Maxed[Character][TJAPlayer3.Skin.Characters_Motion_SoulIn[Character][nNowCharaFrame[i]]];
                                }
                                if (endAnime)
                                {
                                    ReturnDefaultAnime(i, true);
                                }
                            }
                            break;
                        case Anime.SoulOut:
                            {
                                updateNormal();
                                if (TJAPlayer3.Tx.Characters_SoulOut[Character] != null && TJAPlayer3.Skin.Characters_SoulOut_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_SoulOut[Character][TJAPlayer3.Skin.Characters_Motion_SoulOut[Character][nNowCharaFrame[i]]];
                                }
                                if (endAnime)
                                {
                                    ReturnDefaultAnime(i, true);
                                }
                            }
                            break;
                        case Anime.ClearOut:
                            {
                                updateNormal();
                                if (TJAPlayer3.Tx.Characters_ClearOut[Character] != null && TJAPlayer3.Skin.Characters_ClearOut_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_ClearOut[Character][TJAPlayer3.Skin.Characters_Motion_ClearOut[Character][nNowCharaFrame[i]]];
                                }
                                if (endAnime)
                                {
                                    ReturnDefaultAnime(i, true);
                                }
                            }
                            break;
                        case Anime.Return:
                            {
                                updateNormal();
                                if (TJAPlayer3.Tx.Characters_Return[Character] != null && TJAPlayer3.Skin.Characters_Return_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_Return[Character][TJAPlayer3.Skin.Characters_Motion_Return[Character][nNowCharaFrame[i]]];
                                }
                                if (endAnime)
                                {
                                    ReturnDefaultAnime(i, true);
                                }
                            }
                            break;
                        case Anime.Balloon_Breaking:
                        case Anime.Balloon_Broke:
                        case Anime.Balloon_Miss:
                        case Anime.Kusudama_Idle:
                        case Anime.Kusudama_Breaking:
                        case Anime.Kusudama_Broke:
                            {
                                updateBalloon();
                            }
                            break;
                        case Anime.Kusudama_Miss:
                            {
                                nNowCharaFrame[i] = (int)(nNowCharaCounter[i] * 2 * (nCharaFrameCount[i] + 1));
                                nNowCharaFrame[i] = Math.Min(nNowCharaFrame[i], nCharaFrameCount[i]);
                                updateBalloon();
                            }
                            break;
                    }
                }

                float chara_x;
                float chara_y;

                float charaScale = 1.0f;

                if (nowChara != null)
                {
                    bool flipX = TJAPlayer3.ConfigIni.bAIBattleMode ? (i == 1) : false;

                    float resolutionScaleX = TJAPlayer3.Skin.Resolution[0] / (float)TJAPlayer3.Skin.Characters_Resolution[Character][0];
                    float resolutionScaleY = TJAPlayer3.Skin.Resolution[1] / (float)TJAPlayer3.Skin.Characters_Resolution[Character][1];

                    if (TJAPlayer3.ConfigIni.bAIBattleMode)
                    {
                        chara_x = (TJAPlayer3.Skin.Characters_X_AI[Character][i] * resolutionScaleX);
                        chara_y = (TJAPlayer3.Skin.Characters_Y_AI[Character][i] * resolutionScaleY);

                        if (nowChara != null)
                        {
                            charaScale = 0.58f;
                        }
                    }
                    else if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
                    {
                        chara_x = (TJAPlayer3.Skin.Characters_X[Character][i] * resolutionScaleX);
                        chara_y = (TJAPlayer3.Skin.Characters_Y[Character][i] * resolutionScaleY);

                        if (nowChara != null)
                        {
                            charaScale = 1.0f;
                        }
                    }
                    else if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
                    {
                        chara_x = (TJAPlayer3.Skin.Characters_5P[Character][0] * resolutionScaleX) + (TJAPlayer3.Skin.Game_UIMove_5P[0] * i);
                        chara_y = (TJAPlayer3.Skin.Characters_5P[Character][1] * resolutionScaleY) + (TJAPlayer3.Skin.Game_UIMove_5P[1] * i);

                        if (nowChara != null)
                        {
                            charaScale = 0.58f;
                        }
                    }
                    else
                    {
                        chara_x = (TJAPlayer3.Skin.Characters_4P[Character][0] * resolutionScaleX) + (TJAPlayer3.Skin.Game_UIMove_4P[0] * i);
                        chara_y = (TJAPlayer3.Skin.Characters_4P[Character][1] * resolutionScaleY) + (TJAPlayer3.Skin.Game_UIMove_4P[1] * i);

                        if (nowChara != null)
                        {
                            charaScale = 0.58f;
                        }
                    }

                    charaScale *= resolutionScaleY;
                    //chara_x *= resolutionScaleX;
                    //chara_y *= resolutionScaleY;

                    if (TJAPlayer3.ConfigIni.bAIBattleMode)
                    {
                        chara_x += TJAPlayer3.Skin.Game_AIBattle_CharaMove * TJAPlayer3.stage演奏ドラム画面.AIBattleState;
                        chara_y -= nowChara.szテクスチャサイズ.Height * charaScale; // Center down
                    }

                    nowChara.vc拡大縮小倍率.X = charaScale;
                    nowChara.vc拡大縮小倍率.Y = charaScale;

                    if (flipX)
                    {
                        nowChara.t2D左右反転描画(chara_x, chara_y);
                    }
                    else
                    {
                        nowChara.t2D描画(chara_x, chara_y);
                    }

                    nowChara.vc拡大縮小倍率.X = 1.0f;
                    nowChara.vc拡大縮小倍率.Y = 1.0f;
                }

                if ((this.b風船連打中[i] != true && CharaAction_Balloon_Delay[i].IsEnded) || TJAPlayer3.ConfigIni.nPlayerCount > 2)
                {
                    if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
                    {
                        TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画(TJAPlayer3.Skin.Game_PuchiChara_X[i], TJAPlayer3.Skin.Game_PuchiChara_Y[i], TJAPlayer3.stage演奏ドラム画面.bIsAlreadyMaxed[i], player: i);
                    }
                    else if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
                    {
                        TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画(TJAPlayer3.Skin.Game_PuchiChara_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * i), TJAPlayer3.Skin.Game_PuchiChara_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * i), TJAPlayer3.stage演奏ドラム画面.bIsAlreadyMaxed[i], player: i, scale: 0.5f);
                    }
                    else
                    {
                        TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画(TJAPlayer3.Skin.Game_PuchiChara_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * i), TJAPlayer3.Skin.Game_PuchiChara_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * i), TJAPlayer3.stage演奏ドラム画面.bIsAlreadyMaxed[i], player: i, scale: 0.5f);
                    }
                }
            }
            return base.Draw();
        }

        public void OnDraw_Balloon()
        {
            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                //if (TJAPlayer3.Skin.Characters_Balloon_Breaking_Ptn[iCurrentCharacter[i]] != 0) CharaAction_Balloon_Breaking[i]?.t進行();
                //if (TJAPlayer3.Skin.Characters_Balloon_Broke_Ptn[iCurrentCharacter[i]] != 0) CharaAction_Balloon_Broke[i]?.t進行();
                CharaAction_Balloon_Delay[i]?.Tick();
                //if (TJAPlayer3.Skin.Characters_Balloon_Miss_Ptn[iCurrentCharacter[i]] != 0) CharaAction_Balloon_Miss[i]?.t進行();
                //CharaAction_Balloon_FadeOut[i].Tick();

                {
                    bool endAnime = nNowCharaCounter[i] >= 1;
                    var nowOpacity = 255;

                    float resolutionScaleX = TJAPlayer3.Skin.Resolution[0] / (float)TJAPlayer3.Skin.Characters_Resolution[this.iCurrentCharacter[i]][0];
                    float resolutionScaleY = TJAPlayer3.Skin.Resolution[1] / (float)TJAPlayer3.Skin.Characters_Resolution[this.iCurrentCharacter[i]][1];

                    float chara_x = 0;
                    float chara_y = 0;
                    float kusu_chara_x = TJAPlayer3.Skin.Characters_Kusudama_X[this.iCurrentCharacter[i]][i] * resolutionScaleX;
                    float kusu_chara_y = TJAPlayer3.Skin.Characters_Kusudama_Y[this.iCurrentCharacter[i]][i] * resolutionScaleY;

                    if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
                    {
                        chara_x = TJAPlayer3.Skin.Characters_Balloon_X[this.iCurrentCharacter[i]][i];
                        chara_y = TJAPlayer3.Skin.Characters_Balloon_Y[this.iCurrentCharacter[i]][i];
                    }
                    else
                    {
                        if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
                        {
                            chara_x = TJAPlayer3.Skin.Characters_Balloon_5P[this.iCurrentCharacter[i]][0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * i);
                            chara_y = TJAPlayer3.Skin.Characters_Balloon_5P[this.iCurrentCharacter[i]][1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * i);
                        }
                        else
                        {
                            chara_x = TJAPlayer3.Skin.Characters_Balloon_4P[this.iCurrentCharacter[i]][0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * i);
                            chara_y = TJAPlayer3.Skin.Characters_Balloon_4P[this.iCurrentCharacter[i]][1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * i);
                        }
                    }

                    chara_x *= resolutionScaleX;
                    chara_y *= resolutionScaleY;

                    float charaScale = resolutionScaleY;


                    if (eNowAnime[i] == Anime.Balloon_Broke)
                    {
                        if (CharaAction_Balloon_FadeOut[i].Counter.IsStoped && nNowCharaFrame[i] > CharaAction_Balloon_FadeOut_StartMs[i][0])
                        {
                            CharaAction_Balloon_FadeOut[i].Start();
                        }
                        
                        if (TJAPlayer3.Skin.Characters_Balloon_Broke_Ptn[this.iCurrentCharacter[i]] != 0 && TJAPlayer3.Tx.Characters_Balloon_Broke[this.iCurrentCharacter[i]][nNowCharaFrame[i]] != null)
                        {
                            TJAPlayer3.Tx.Characters_Balloon_Broke[this.iCurrentCharacter[i]][nNowCharaFrame[i]].Opacity = nowOpacity;
                            TJAPlayer3.Tx.Characters_Balloon_Broke[this.iCurrentCharacter[i]][nNowCharaFrame[i]].vc拡大縮小倍率.X = charaScale;
                            TJAPlayer3.Tx.Characters_Balloon_Broke[this.iCurrentCharacter[i]][nNowCharaFrame[i]].vc拡大縮小倍率.Y = charaScale;
                            TJAPlayer3.Tx.Characters_Balloon_Broke[this.iCurrentCharacter[i]][nNowCharaFrame[i]].t2D描画(
                                TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLX(i) + chara_x,
                                TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLY(i) + chara_y);
                        }

                        if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
                            TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画(
                                TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLX(i) + TJAPlayer3.Skin.Game_PuchiChara_BalloonX[i],
                                TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLY(i) + TJAPlayer3.Skin.Game_PuchiChara_BalloonY[i], false, nowOpacity, true, player : i);
                        
                        if (endAnime)
                        {
                            ReturnDefaultAnime(i, true);
                        }
                    }
                    else if (eNowAnime[i] == Anime.Balloon_Miss)
                    {
                        if (CharaAction_Balloon_FadeOut[i].Counter.IsStoped && nNowCharaFrame[i] > CharaAction_Balloon_FadeOut_StartMs[i][1])
                        {
                            CharaAction_Balloon_FadeOut[i].Start();
                        }

                        if (TJAPlayer3.Skin.Characters_Balloon_Miss_Ptn[this.iCurrentCharacter[i]] != 0 && TJAPlayer3.Tx.Characters_Balloon_Miss[this.iCurrentCharacter[i]][nNowCharaFrame[i]] != null)
                        {
                            TJAPlayer3.Tx.Characters_Balloon_Miss[this.iCurrentCharacter[i]][nNowCharaFrame[i]].Opacity = nowOpacity;
                            TJAPlayer3.Tx.Characters_Balloon_Miss[this.iCurrentCharacter[i]][nNowCharaFrame[i]].vc拡大縮小倍率.X = charaScale;
                            TJAPlayer3.Tx.Characters_Balloon_Miss[this.iCurrentCharacter[i]][nNowCharaFrame[i]].vc拡大縮小倍率.Y = charaScale;
                            TJAPlayer3.Tx.Characters_Balloon_Miss[this.iCurrentCharacter[i]][nNowCharaFrame[i]].t2D描画(
                                TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLX(i) + chara_x,
                                TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLY(i) + chara_y);
                        }

                        if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
                            TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画(
                                TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLX(i) + TJAPlayer3.Skin.Game_PuchiChara_BalloonX[i],
                                TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLY(i) + TJAPlayer3.Skin.Game_PuchiChara_BalloonY[i], false, nowOpacity, true, player : i);
                        
                        if (endAnime)
                        {
                            ReturnDefaultAnime(i, true);
                        }
                    }
                    else if (eNowAnime[i] == Anime.Balloon_Breaking)
                    {
                        if (TJAPlayer3.Skin.Characters_Balloon_Breaking_Ptn[this.iCurrentCharacter[i]] != 0 && TJAPlayer3.Tx.Characters_Balloon_Breaking[this.iCurrentCharacter[i]][nNowCharaFrame[i]] != null)
                        {
                            TJAPlayer3.Tx.Characters_Balloon_Breaking[this.iCurrentCharacter[i]][nNowCharaFrame[i]].vc拡大縮小倍率.X = charaScale;
                            TJAPlayer3.Tx.Characters_Balloon_Breaking[this.iCurrentCharacter[i]][nNowCharaFrame[i]].vc拡大縮小倍率.Y = charaScale;
                            TJAPlayer3.Tx.Characters_Balloon_Breaking[this.iCurrentCharacter[i]][nNowCharaFrame[i]].t2D描画(
                                TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLX(i) + chara_x,
                                TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLY(i) + chara_y);
                        }

                        if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
                            TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画(
                                TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLX(i) + TJAPlayer3.Skin.Game_PuchiChara_BalloonX[i],
                                TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLY(i) + TJAPlayer3.Skin.Game_PuchiChara_BalloonY[i], false, 255, true, player : i);
                    }
                    else if (eNowAnime[i] == Anime.Kusudama_Broke)
                    {
                        if (CharaAction_Balloon_FadeOut[i].Counter.IsStoped && nNowCharaFrame[i] > CharaAction_Balloon_FadeOut_StartMs[i][0])
                        {
                            CharaAction_Balloon_FadeOut[i].Start();
                        }
                        float kusuOutX = ((1.0f - MathF.Cos(nNowCharaCounter[i] * MathF.PI)) * TJAPlayer3.Skin.Resolution[0] / 2.0f) * resolutionScaleX;
                        float kusuOutY = (MathF.Sin(nNowCharaCounter[i] * MathF.PI / 2) * TJAPlayer3.Skin.Resolution[1] / 2.0f) * resolutionScaleY;
                        
                        if (TJAPlayer3.Skin.Characters_Kusudama_Broke_Ptn[this.iCurrentCharacter[i]] != 0 && TJAPlayer3.Tx.Characters_Kusudama_Broke[this.iCurrentCharacter[i]][nNowCharaFrame[i]] != null)
                        {
                            TJAPlayer3.Tx.Characters_Kusudama_Broke[this.iCurrentCharacter[i]][nNowCharaFrame[i]].Opacity = nowOpacity;
                            TJAPlayer3.Tx.Characters_Kusudama_Broke[this.iCurrentCharacter[i]][nNowCharaFrame[i]].vc拡大縮小倍率.X = charaScale;
                            TJAPlayer3.Tx.Characters_Kusudama_Broke[this.iCurrentCharacter[i]][nNowCharaFrame[i]].vc拡大縮小倍率.Y = charaScale;
                            if (i % 2 == 0)
                            {
                                TJAPlayer3.Tx.Characters_Kusudama_Broke[this.iCurrentCharacter[i]][nNowCharaFrame[i]].t2D描画(kusu_chara_x - kusuOutX, kusu_chara_y - kusuOutY);
                            }
                            else
                            {
                                TJAPlayer3.Tx.Characters_Kusudama_Broke[this.iCurrentCharacter[i]][nNowCharaFrame[i]].t2D左右反転描画(kusu_chara_x + kusuOutX, kusu_chara_y - kusuOutY);
                            }
                        }
                        if (i % 2 == 0)
                        {
                            TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画(
                                TJAPlayer3.Skin.Game_PuchiChara_KusudamaX[i] - (int)kusuOutX,
                                TJAPlayer3.Skin.Game_PuchiChara_KusudamaY[i] - (int)kusuOutY, false, nowOpacity, true, player : i);
                        }
                        else
                        {
                            TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画(
                                TJAPlayer3.Skin.Game_PuchiChara_KusudamaX[i] + (int)kusuOutX,
                                TJAPlayer3.Skin.Game_PuchiChara_KusudamaY[i] - (int)kusuOutY, false, nowOpacity, true, player : i);
                        }
                        
                        if (endAnime)
                        {
                            ReturnDefaultAnime(i, true);
                        }
                    }
                    else if (eNowAnime[i] == Anime.Kusudama_Miss)
                    {
                        if (CharaAction_Balloon_FadeOut[i].Counter.IsStoped && nNowCharaFrame[i] > CharaAction_Balloon_FadeOut_StartMs[i][1])
                        {
                            CharaAction_Balloon_FadeOut[i].Start();
                        }

                        float kusuOutY = (Math.Max(nNowCharaCounter[i] - 0.5f, 0) * TJAPlayer3.Skin.Resolution[1] * 2) * resolutionScaleY;

                        if (TJAPlayer3.Skin.Characters_Kusudama_Miss_Ptn[this.iCurrentCharacter[i]] != 0 && TJAPlayer3.Tx.Characters_Kusudama_Miss[this.iCurrentCharacter[i]][nNowCharaFrame[i]] != null)
                        {
                            TJAPlayer3.Tx.Characters_Kusudama_Miss[this.iCurrentCharacter[i]][nNowCharaFrame[i]].Opacity = nowOpacity;
                            TJAPlayer3.Tx.Characters_Kusudama_Miss[this.iCurrentCharacter[i]][nNowCharaFrame[i]].vc拡大縮小倍率.X = charaScale;
                            TJAPlayer3.Tx.Characters_Kusudama_Miss[this.iCurrentCharacter[i]][nNowCharaFrame[i]].vc拡大縮小倍率.Y = charaScale;


                            if (i % 2 == 0)
                            {
                                TJAPlayer3.Tx.Characters_Kusudama_Miss[this.iCurrentCharacter[i]][nNowCharaFrame[i]].t2D描画(kusu_chara_x, kusu_chara_y + kusuOutY);
                            }
                            else
                            {
                                TJAPlayer3.Tx.Characters_Kusudama_Miss[this.iCurrentCharacter[i]][nNowCharaFrame[i]].t2D左右反転描画(kusu_chara_x, kusu_chara_y + kusuOutY);
                            }
                        }

                        TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画(
                            TJAPlayer3.Skin.Game_PuchiChara_KusudamaX[i],
                            TJAPlayer3.Skin.Game_PuchiChara_KusudamaY[i] + (int)kusuOutY, false, nowOpacity, true, player : i);
                        
                        if (endAnime)
                        {
                            ReturnDefaultAnime(i, true);
                        }
                    }
                    else if (eNowAnime[i] == Anime.Kusudama_Breaking)
                    {
                        float kusuInX = ((1.0f - MathF.Sin(ctKusuIn[i].CurrentValue / 2000.0f * MathF.PI)) * TJAPlayer3.Skin.Resolution[0] / 2.0f) * resolutionScaleX;
                        float kusuInY = -((MathF.Cos(ctKusuIn[i].CurrentValue / 1000.0f * MathF.PI / 2)) * TJAPlayer3.Skin.Resolution[1] / 2.0f) * resolutionScaleY;
                        
                        
                        if (TJAPlayer3.Skin.Characters_Kusudama_Breaking_Ptn[this.iCurrentCharacter[i]] != 0 && TJAPlayer3.Tx.Characters_Kusudama_Breaking[this.iCurrentCharacter[i]][nNowCharaFrame[i]] != null)
                        {
                            TJAPlayer3.Tx.Characters_Kusudama_Breaking[this.iCurrentCharacter[i]][nNowCharaFrame[i]].vc拡大縮小倍率.X = charaScale;
                            TJAPlayer3.Tx.Characters_Kusudama_Breaking[this.iCurrentCharacter[i]][nNowCharaFrame[i]].vc拡大縮小倍率.Y = charaScale;
                            if (i % 2 == 0)
                            {
                                TJAPlayer3.Tx.Characters_Kusudama_Breaking[this.iCurrentCharacter[i]][nNowCharaFrame[i]].t2D描画(kusu_chara_x - kusuInX, kusu_chara_y + kusuInY);
                            }
                            else
                            {
                                TJAPlayer3.Tx.Characters_Kusudama_Breaking[this.iCurrentCharacter[i]][nNowCharaFrame[i]].t2D左右反転描画(kusu_chara_x + kusuInX, kusu_chara_y + kusuInY);
                            }
                        }

                        if (i % 2 == 0)
                        {
                            TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画(
                                TJAPlayer3.Skin.Game_PuchiChara_KusudamaX[i] - (int)kusuInX,
                                TJAPlayer3.Skin.Game_PuchiChara_KusudamaY[i] + (int)kusuInY, false, 255, true, player : i);
                        }
                        else
                        {
                            TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画(
                                TJAPlayer3.Skin.Game_PuchiChara_KusudamaX[i] + (int)kusuInX,
                                TJAPlayer3.Skin.Game_PuchiChara_KusudamaY[i] + (int)kusuInY, false, 255, true, player : i);
                        }
                        
                        if (endAnime)
                        {
                            ChangeAnime(i, Anime.Kusudama_Idle, true);
                        }
                    }
                    else if (eNowAnime[i] == Anime.Kusudama_Idle)
                    {
                        float kusuInX = ((1.0f - MathF.Sin(ctKusuIn[i].CurrentValue / 2000.0f * MathF.PI)) * TJAPlayer3.Skin.Resolution[0] / 2.0f) * resolutionScaleX;
                        float kusuInY = -((MathF.Cos(ctKusuIn[i].CurrentValue / 1000.0f * MathF.PI / 2)) * TJAPlayer3.Skin.Resolution[1] / 2.0f) * resolutionScaleY;

                        if (TJAPlayer3.Skin.Characters_Kusudama_Idle_Ptn[this.iCurrentCharacter[i]] != 0 && TJAPlayer3.Tx.Characters_Kusudama_Idle[this.iCurrentCharacter[i]][nNowCharaFrame[i]] != null)
                        {
                            TJAPlayer3.Tx.Characters_Kusudama_Idle[this.iCurrentCharacter[i]][nNowCharaFrame[i]].vc拡大縮小倍率.X = charaScale;
                            TJAPlayer3.Tx.Characters_Kusudama_Idle[this.iCurrentCharacter[i]][nNowCharaFrame[i]].vc拡大縮小倍率.Y = charaScale;
                            if (i % 2 == 0)
                            {
                                TJAPlayer3.Tx.Characters_Kusudama_Idle[this.iCurrentCharacter[i]][nNowCharaFrame[i]].t2D描画(kusu_chara_x - kusuInX, kusu_chara_y + kusuInY);
                            }
                            else
                            {
                                TJAPlayer3.Tx.Characters_Kusudama_Idle[this.iCurrentCharacter[i]][nNowCharaFrame[i]].t2D左右反転描画(kusu_chara_x + kusuInX, kusu_chara_y + kusuInY);
                            }
                        }

                        if (i % 2 == 0)
                        {
                            TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画(
                                TJAPlayer3.Skin.Game_PuchiChara_KusudamaX[i] - (int)kusuInX,
                                TJAPlayer3.Skin.Game_PuchiChara_KusudamaY[i] + (int)kusuInY, false, 255, true, player : i);
                        }
                        else
                        {
                            TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画(
                                TJAPlayer3.Skin.Game_PuchiChara_KusudamaX[i] + (int)kusuInX,
                                TJAPlayer3.Skin.Game_PuchiChara_KusudamaY[i] + (int)kusuInY, false, 255, true, player : i);
                        }
                        
                        if (endAnime)
                        {
                            ChangeAnime(i, Anime.Kusudama_Idle, true);
                        }
                    }
                }
            }
        }


        public void ReturnDefaultAnime(int player, bool resetCounter)
        {
            if (TJAPlayer3.stage演奏ドラム画面.bIsGOGOTIME[player] && TJAPlayer3.Skin.Characters_GoGoTime_Ptn[this.iCurrentCharacter[player]] != 0)
            {
                if (TJAPlayer3.stage演奏ドラム画面.bIsAlreadyMaxed[player] && TJAPlayer3.Skin.Characters_GoGoTime_Maxed_Ptn[this.iCurrentCharacter[player]] != 0)
                {
                    ChangeAnime(player, Anime.GoGoTime_Maxed, resetCounter);
                }
                else
                {
                    ChangeAnime(player, Anime.GoGoTime, resetCounter);
                }
            }
            else
            {
                if (TJAPlayer3.stage演奏ドラム画面.bIsMiss[player] && TJAPlayer3.Skin.Characters_Normal_Missed_Ptn[this.iCurrentCharacter[player]] != 0)
                {
                    if (TJAPlayer3.stage演奏ドラム画面.Chara_MissCount[player] >= 6 && TJAPlayer3.Skin.Characters_Normal_MissedDown_Ptn[this.iCurrentCharacter[player]] != 0)
                    {
                        ChangeAnime(player, Anime.MissDown, resetCounter);
                    }
                    else
                    {
                        ChangeAnime(player, Anime.Miss, resetCounter);
                    }
                }
                else
                {
                    if (TJAPlayer3.stage演奏ドラム画面.bIsAlreadyMaxed[player] && TJAPlayer3.Skin.Characters_Normal_Maxed_Ptn[this.iCurrentCharacter[player]] != 0)
                    {
                        ChangeAnime(player, Anime.Maxed, resetCounter);
                    }
                    else if (TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared[player] && TJAPlayer3.Skin.Characters_Normal_Cleared_Ptn[this.iCurrentCharacter[player]] != 0)
                    {
                        ChangeAnime(player, Anime.Cleared, resetCounter);
                    }
                    else if (TJAPlayer3.Skin.Characters_Normal_Ptn[this.iCurrentCharacter[player]] != 0)
                    {
                        ChangeAnime(player, Anime.Normal, resetCounter);
                    }
                    else
                    {
                        ChangeAnime(player, Anime.None, resetCounter);
                    }
                }
            }
        }

        public int[][] arモーション番号 = new int[5][];
        public int[][] arMissモーション番号 = new int[5][];
        public int[][] arMissDownモーション番号 = new int[5][];
        public int[][] arゴーゴーモーション番号 = new int[5][];
        public int[][] arクリアモーション番号 = new int[5][];

        private float[] nNowCharaCounter = new float[5];
        private int[] nNowCharaFrame = new int[5];
        private int[] nCharaFrameCount = new int[5];
        private float[] nCharaBeat = new float[5];

        public enum Anime
        {
            None,
            Normal,
            Miss,
            MissDown,
            Cleared,
            Maxed,
            MissIn,
            MissDownIn,
            GoGoTime,
            GoGoTime_Maxed,
            Combo10,
            Combo10_Clear,
            Combo10_Max,
            GoGoStart,
            GoGoStart_Clear,
            GoGoStart_Max,
            Become_Cleared,
            Become_Maxed,
            SoulOut,
            ClearOut,
            Return,
            Balloon_Breaking,
            Balloon_Broke,
            Balloon_Miss,
            Kusudama_Idle,
            Kusudama_Breaking,
            Kusudama_Broke,
            Kusudama_Miss
        }


        public Anime[] eNowAnime = new Anime[5];

        public CCounter[] ctKusuIn = new CCounter[5];

        public void KusuIn()
        {
            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                ChangeAnime(i, Anime.Kusudama_Idle, true);
                ctKusuIn[i] = new CCounter(0, 1000, 0.4f, TJAPlayer3.Timer);
            }
        }

        public void ChangeAnime(int player, Anime anime, bool resetCounter)
        {
            eNowAnime[player] = anime;

            if (resetCounter)
            {
                nNowCharaCounter[player] = 0;
                nNowCharaFrame[player] = 0;
            }

            switch (anime)
            {
                case Anime.None:
                    break;
                case Anime.Normal:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_Normal[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_Normal[iCurrentCharacter[player]];
                    break;
                case Anime.Miss:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_Miss[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_Miss[iCurrentCharacter[player]];
                    break;
                case Anime.MissDown:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_MissDown[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_MissDown[iCurrentCharacter[player]];
                    break;
                case Anime.Cleared:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_Clear[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_Clear[iCurrentCharacter[player]];
                    break;
                case Anime.Maxed:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_ClearMax[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_ClearMax[iCurrentCharacter[player]];
                    break;
                case Anime.MissIn:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_MissIn[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_MissIn[iCurrentCharacter[player]];
                    break;
                case Anime.MissDownIn:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_MissDownIn[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_MissDownIn[iCurrentCharacter[player]];
                    break;
                case Anime.GoGoTime:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_GoGo[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_GoGo[iCurrentCharacter[player]];
                    break;
                case Anime.GoGoTime_Maxed:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_GoGoMax[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_GoGoMax[iCurrentCharacter[player]];
                    break;
                case Anime.Combo10:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_10Combo[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_10Combo[iCurrentCharacter[player]];
                    break;
                case Anime.Combo10_Clear:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_10Combo_Clear[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_10Combo_Clear[iCurrentCharacter[player]];
                    break;
                case Anime.Combo10_Max:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_10ComboMax[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_10ComboMax[iCurrentCharacter[player]];
                    break;
                case Anime.GoGoStart:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_GoGoStart[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_GoGoStart[iCurrentCharacter[player]];
                    break;
                case Anime.GoGoStart_Clear:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_GoGoStart_Clear[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_GoGoStart_Clear[iCurrentCharacter[player]];
                    break;
                case Anime.GoGoStart_Max:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_GoGoStartMax[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_GoGoStartMax[iCurrentCharacter[player]];
                    break;
                case Anime.Become_Cleared:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_ClearIn[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_ClearIn[iCurrentCharacter[player]];
                    break;
                case Anime.Become_Maxed:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_SoulIn[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_SoulIn[iCurrentCharacter[player]];
                    break;
                case Anime.SoulOut:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_SoulOut[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_SoulOut[iCurrentCharacter[player]];
                    break;
                case Anime.ClearOut:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_ClearOut[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_ClearOut[iCurrentCharacter[player]];
                    break;
                case Anime.Return:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Motion_Return[iCurrentCharacter[player]].Length - 1;
                    nCharaBeat[player] = TJAPlayer3.Skin.Characters_Beat_Return[iCurrentCharacter[player]];
                    break;
                case Anime.Balloon_Breaking:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Balloon_Breaking_Ptn[iCurrentCharacter[player]] - 1;
                    nCharaBeat[player] = 0.2f;
                    break;
                case Anime.Balloon_Broke:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Balloon_Broke_Ptn[iCurrentCharacter[player]] - 1;
                    nCharaBeat[player] = 0.2f;
                    break;
                case Anime.Balloon_Miss:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Balloon_Miss_Ptn[iCurrentCharacter[player]] - 1;
                    nCharaBeat[player] = 0.2f;
                    break;
                case Anime.Kusudama_Idle:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Kusudama_Idle_Ptn[iCurrentCharacter[player]] - 1;
                    nCharaBeat[player] = 0.4f;
                    break;
                case Anime.Kusudama_Breaking:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Kusudama_Breaking_Ptn[iCurrentCharacter[player]] - 1;
                    nCharaBeat[player] = 0.2f;
                    break;
                case Anime.Kusudama_Broke:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Kusudama_Broke_Ptn[iCurrentCharacter[player]] - 1;
                    nCharaBeat[player] = 1f;
                    break;
                case Anime.Kusudama_Miss:
                    nCharaFrameCount[player] = TJAPlayer3.Skin.Characters_Kusudama_Miss_Ptn[iCurrentCharacter[player]] - 1;
                    nCharaBeat[player] = 0.5f;
                    break;
            }
        }

        public CCounter[] CharaAction_Balloon_Delay = new CCounter[5];

        public Animations.FadeOut[] CharaAction_Balloon_FadeOut = new Animations.FadeOut[5];
        //private readonly int[] CharaAction_Balloon_FadeOut_StartMs = new int[5];
        private readonly int[][] CharaAction_Balloon_FadeOut_StartMs = new int[5][];

        //public bool[] bマイどんアクション中 = new bool[5];

        public bool[] b風船連打中 = new bool[5];
        public bool[] b演奏中 = new bool[5];

        public int[] iCurrentCharacter = new int[5] { 0, 0, 0, 0, 0 };
    }
}
