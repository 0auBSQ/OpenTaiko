﻿using System;
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

        public override void On活性化()
        {
            for(int i = 0; i < 5; i++)
            {
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

                CharaAction_Balloon_Breaking[i] = new CCounter();
                CharaAction_Balloon_Broke[i] = new CCounter();
                CharaAction_Balloon_Miss[i] = new CCounter();
                CharaAction_Balloon_Delay[i] = new CCounter();

                // Currently used character
                int p = TJAPlayer3.GetActualPlayer(i);

                this.iCurrentCharacter[i] = Math.Max(0, Math.Min(TJAPlayer3.SaveFileInstances[p].data.Character, TJAPlayer3.Skin.Characters_Ptn - 1));


                this.b風船連打中[i] = false;
                this.b演奏中[i] = false;

                // CharaAction_Balloon_FadeOut[i] = new Animations.FadeOut(TJAPlayer3.Skin.Game_Chara_Balloon_FadeOut);
                CharaAction_Balloon_FadeOut[i] = new Animations.FadeOut(TJAPlayer3.Skin.Characters_Balloon_FadeOut[this.iCurrentCharacter[i]]);

                this.bマイどんアクション中[i] = false;

                var tick = TJAPlayer3.Skin.Characters_Balloon_Timer[this.iCurrentCharacter[i]];

                var balloonBrokePtn = TJAPlayer3.Skin.Characters_Balloon_Broke_Ptn[this.iCurrentCharacter[i]];
                var balloonMissPtn = TJAPlayer3.Skin.Characters_Balloon_Miss_Ptn[this.iCurrentCharacter[i]];

                CharaAction_Balloon_FadeOut_StartMs[i] = new int[2];

                CharaAction_Balloon_FadeOut_StartMs[i][0] = (balloonBrokePtn * tick) - TJAPlayer3.Skin.Characters_Balloon_FadeOut[this.iCurrentCharacter[i]];
                CharaAction_Balloon_FadeOut_StartMs[i][1] = (balloonMissPtn * tick) - TJAPlayer3.Skin.Characters_Balloon_FadeOut[this.iCurrentCharacter[i]];

                if (balloonBrokePtn > 1) CharaAction_Balloon_FadeOut_StartMs[i][0] /= balloonBrokePtn - 1;
                if (balloonMissPtn > 1) CharaAction_Balloon_FadeOut_StartMs[i][1] /= balloonMissPtn - 1; // - 1はタイマー用
            }

            base.On活性化();
        }

        public override void On非活性化()
        {
            for (int i = 0; i < 5; i++)
            {
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

                CharaAction_Balloon_Breaking[i] = null;
                CharaAction_Balloon_Broke[i] = null;
                CharaAction_Balloon_Miss[i] = null;
                CharaAction_Balloon_Delay[i] = null;

                CharaAction_Balloon_FadeOut[i] = null;
            }

            base.On非活性化();
        }

        public override void OnManagedリソースの作成()
        {
            for (int i = 0; i < 5; i++)
            {
                this.arモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す(TJAPlayer3.Skin.Characters_Motion_Normal[this.iCurrentCharacter[i]]);
                this.arMissモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す(TJAPlayer3.Skin.Characters_Motion_Miss[this.iCurrentCharacter[i]]);
                this.arMissDownモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す(TJAPlayer3.Skin.Characters_Motion_MissDown[this.iCurrentCharacter[i]]);
                this.arゴーゴーモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す(TJAPlayer3.Skin.Characters_Motion_GoGo[this.iCurrentCharacter[i]]);
                this.arクリアモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す(TJAPlayer3.Skin.Characters_Motion_Clear[this.iCurrentCharacter[i]]);

                if (arモーション番号[i] == null) this.arモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す("0,0");
                if (arMissモーション番号[i] == null) this.arMissモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す("0,0");
                if (arMissDownモーション番号[i] == null) this.arMissDownモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す("0,0");
                if (arゴーゴーモーション番号[i] == null) this.arゴーゴーモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す("0,0");
                if (arクリアモーション番号[i] == null) this.arクリアモーション番号[i] = C変換.ar配列形式のstringをint配列に変換して返す("0,0");

                ctChara_Normal[i] = new CCounter(0, arモーション番号[i].Length - 1, 10, CSound管理.rc演奏用タイマ);
                ctChara_Miss[i] = new CCounter(0, arMissモーション番号[i].Length - 1, 10, CSound管理.rc演奏用タイマ);
                ctChara_MissDown[i] = new CCounter(0, arMissDownモーション番号[i].Length - 1, 10, CSound管理.rc演奏用タイマ);
                ctChara_GoGo[i] = new CCounter(0, arゴーゴーモーション番号[i].Length - 1, 10, CSound管理.rc演奏用タイマ);
                ctChara_Clear[i] = new CCounter(0, arクリアモーション番号[i].Length - 1, 10, CSound管理.rc演奏用タイマ);
                if (CharaAction_Balloon_Delay[i] != null) CharaAction_Balloon_Delay[i].n現在の値 = (int)CharaAction_Balloon_Delay[i].n終了値;
            }
            base.OnManagedリソースの作成();
        }

        public override void OnManagedリソースの解放()
        {
            base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {
            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                int Character = this.iCurrentCharacter[i];

                if (TJAPlayer3.Skin.Characters_Ptn == 0)
                    break;

                if (ctChara_Normal != null || TJAPlayer3.Skin.Characters_Normal_Ptn[Character] != 0) ctChara_Normal[i].t進行LoopDb();
                if (ctChara_Miss != null || TJAPlayer3.Skin.Characters_Normal_Missed_Ptn[Character] != 0) ctChara_Miss[i].t進行LoopDb();
                if (ctChara_MissDown != null || TJAPlayer3.Skin.Characters_Normal_MissedDown_Ptn[Character] != 0) ctChara_MissDown[i].t進行LoopDb();
                if (ctChara_GoGo != null || TJAPlayer3.Skin.Characters_GoGoTime_Ptn[Character] != 0) ctChara_GoGo[i].t進行LoopDb();
                if (ctChara_Clear != null || TJAPlayer3.Skin.Characters_Normal_Cleared_Ptn[Character] != 0) ctChara_Clear[i].t進行LoopDb();

                if (this.ctキャラクターアクション_10コンボ != null || TJAPlayer3.Skin.Characters_10Combo_Ptn[Character] != 0) this.ctキャラクターアクション_10コンボ[i].t進行db();
                if (this.ctキャラクターアクション_10コンボMAX != null || TJAPlayer3.Skin.Characters_10Combo_Maxed_Ptn[Character] != 0) this.ctキャラクターアクション_10コンボMAX[i].t進行db();
                if (this.ctキャラクターアクション_ゴーゴースタート != null || TJAPlayer3.Skin.Characters_GoGoStart_Ptn[Character] != 0) this.ctキャラクターアクション_ゴーゴースタート[i].t進行db();
                if (this.ctキャラクターアクション_ゴーゴースタートMAX != null || TJAPlayer3.Skin.Characters_GoGoStart_Maxed_Ptn[Character] != 0) this.ctキャラクターアクション_ゴーゴースタートMAX[i].t進行db();
                if (this.ctキャラクターアクション_ノルマ != null || TJAPlayer3.Skin.Characters_Become_Cleared_Ptn[Character] != 0) this.ctキャラクターアクション_ノルマ[i].t進行db();
                if (this.ctキャラクターアクション_魂MAX != null || TJAPlayer3.Skin.Characters_Become_Maxed_Ptn[Character] != 0) this.ctキャラクターアクション_魂MAX[i].t進行db();
                if (this.ctキャラクターアクション_Return != null || TJAPlayer3.Skin.Characters_10Combo_Ptn[Character] != 0) this.ctキャラクターアクション_Return[i].t進行db();

                // Blinking animation during invincibility frames
                if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
                {
                    if (CFloorManagement.isBlinking() == true)
                        break;
                }

                CTexture nowChara = null;

                if (this.b風船連打中[i] != true && this.bマイどんアクション中[i] != true && CharaAction_Balloon_Delay[i].b終了値に達した)
                {
                    if (!TJAPlayer3.stage演奏ドラム画面.bIsGOGOTIME[i])
                    {
                        if (!TJAPlayer3.stage演奏ドラム画面.bIsMiss[i] || TJAPlayer3.Skin.Characters_Normal_Missed_Ptn[Character] == 0)
                        {
                            if (TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[i] >= 100.0 && TJAPlayer3.Skin.Characters_Normal_Cleared_Ptn[Character] != 0)
                            {
                                if (TJAPlayer3.Skin.Characters_Normal_Maxed_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_Normal_Maxed[Character][this.arクリアモーション番号[i][(int)this.ctChara_Clear[i].n現在の値]];
                                }
                            }
                            else if (TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[i] >= 80.0 && TJAPlayer3.Skin.Characters_Normal_Cleared_Ptn[Character] != 0)
                            {
                                if (TJAPlayer3.Skin.Characters_Normal_Cleared_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_Normal_Cleared[Character][this.arクリアモーション番号[i][(int)this.ctChara_Clear[i].n現在の値]];
                                }
                            }
                            else
                            {
                                if (TJAPlayer3.Skin.Characters_Normal_Ptn[Character] != 0)
                                {
                                    nowChara = TJAPlayer3.Tx.Characters_Normal[Character][this.arモーション番号[i][(int)this.ctChara_Normal[i].n現在の値]];
                                }
                            }
                        }
                        else
                        {
                            if (TJAPlayer3.stage演奏ドラム画面.Chara_MissCount[i] < 6 || TJAPlayer3.Skin.Characters_Normal_MissedDown_Ptn[Character] == 0)
                            {
                                nowChara = TJAPlayer3.Tx.Characters_Normal_Missed[Character][this.arMissモーション番号[i][(int)this.ctChara_Miss[i].n現在の値]];
                            }
                            else
                            {
                                nowChara = TJAPlayer3.Tx.Characters_Normal_MissedDown[Character][this.arMissDownモーション番号[i][(int)this.ctChara_MissDown[i].n現在の値]];
                            }
                        }
                    }
                    else
                    {
                        if (TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[i] >= 100.0 && TJAPlayer3.Skin.Characters_GoGoTime_Maxed_Ptn[Character] != 0)
                        {
                            if (TJAPlayer3.Skin.Characters_GoGoTime_Maxed_Ptn[Character] != 0)
                                nowChara = TJAPlayer3.Tx.Characters_GoGoTime_Maxed[Character][this.arゴーゴーモーション番号[i][(int)this.ctChara_GoGo[i].n現在の値]];
                        }
                        else
                        {
                            if (TJAPlayer3.Skin.Characters_GoGoTime_Ptn[Character] != 0)
                                nowChara = TJAPlayer3.Tx.Characters_GoGoTime[Character][this.arゴーゴーモーション番号[i][(int)this.ctChara_GoGo[i].n現在の値]];
                        }
                    }
                }

                if (this.b風船連打中[i] != true && bマイどんアクション中[i] == true && CharaAction_Balloon_Delay[i].b終了値に達した)
                {
                    if (this.ctキャラクターアクション_10コンボ[i].b進行中)
                    {
                        if (TJAPlayer3.Tx.Characters_10Combo[Character] != null && TJAPlayer3.Skin.Characters_10Combo_Ptn[Character] != 0)
                        {
                            nowChara = TJAPlayer3.Tx.Characters_10Combo[Character][(int)this.ctキャラクターアクション_10コンボ[i].n現在の値];
                        }
                        if (this.ctキャラクターアクション_10コンボ[i].b終了値に達した)
                        {
                            this.bマイどんアクション中[i] = false;
                            this.ctキャラクターアクション_10コンボ[i].t停止();
                            this.ctキャラクターアクション_10コンボ[i].n現在の値 = 0;
                        }
                    }


                    if (this.ctキャラクターアクション_10コンボMAX[i].b進行中)
                    {
                        if (TJAPlayer3.Tx.Characters_10Combo_Maxed[Character] != null && TJAPlayer3.Skin.Characters_10Combo_Maxed_Ptn[Character] != 0)
                        {
                            nowChara = TJAPlayer3.Tx.Characters_10Combo_Maxed[Character][(int)this.ctキャラクターアクション_10コンボMAX[i].n現在の値];
                        }
                        if (this.ctキャラクターアクション_10コンボMAX[i].b終了値に達した)
                        {
                            this.bマイどんアクション中[i] = false;
                            this.ctキャラクターアクション_10コンボMAX[i].t停止();
                            this.ctキャラクターアクション_10コンボMAX[i].n現在の値 = 0;
                        }

                    }

                    if (this.ctキャラクターアクション_ゴーゴースタート[i].b進行中)
                    {
                        if (TJAPlayer3.Tx.Characters_GoGoStart[Character] != null && TJAPlayer3.Skin.Characters_GoGoStart_Ptn[Character] != 0)
                        {
                            nowChara = TJAPlayer3.Tx.Characters_GoGoStart[Character][(int)this.ctキャラクターアクション_ゴーゴースタート[i].n現在の値];
                        }
                        if (this.ctキャラクターアクション_ゴーゴースタート[i].b終了値に達した)
                        {
                            this.bマイどんアクション中[i] = false;
                            this.ctキャラクターアクション_ゴーゴースタート[i].t停止();
                            this.ctキャラクターアクション_ゴーゴースタート[i].n現在の値 = 0;
                            this.ctChara_GoGo[i].n現在の値 = TJAPlayer3.Skin.Characters_GoGoTime_Ptn[Character] / 2;
                        }
                    }

                    if (this.ctキャラクターアクション_ゴーゴースタートMAX[i].b進行中)
                    {
                        if (TJAPlayer3.Tx.Characters_GoGoStart_Maxed[Character] != null && TJAPlayer3.Skin.Characters_GoGoStart_Maxed_Ptn[Character] != 0)
                        {
                            nowChara = TJAPlayer3.Tx.Characters_GoGoStart_Maxed[Character][(int)this.ctキャラクターアクション_ゴーゴースタートMAX[i].n現在の値];
                        }
                        if (this.ctキャラクターアクション_ゴーゴースタートMAX[i].b終了値に達した)
                        {
                            this.bマイどんアクション中[i] = false;
                            this.ctキャラクターアクション_ゴーゴースタートMAX[i].t停止();
                            this.ctキャラクターアクション_ゴーゴースタートMAX[i].n現在の値 = 0;
                            this.ctChara_GoGo[i].n現在の値 = TJAPlayer3.Skin.Characters_GoGoTime_Ptn[Character] / 2;
                        }
                    }

                    if (this.ctキャラクターアクション_ノルマ[i].b進行中)
                    {
                        if (TJAPlayer3.Tx.Characters_Become_Cleared[Character] != null && TJAPlayer3.Skin.Characters_Become_Cleared_Ptn[Character] != 0)
                        {
                            nowChara = TJAPlayer3.Tx.Characters_Become_Cleared[Character][(int)this.ctキャラクターアクション_ノルマ[i].n現在の値];
                        }
                        if (this.ctキャラクターアクション_ノルマ[i].b終了値に達した)
                        {
                            this.bマイどんアクション中[i] = false;
                            this.ctキャラクターアクション_ノルマ[i].t停止();
                            this.ctキャラクターアクション_ノルマ[i].n現在の値 = 0;
                        }
                    }

                    if (this.ctキャラクターアクション_魂MAX[i].b進行中)
                    {
                        if (TJAPlayer3.Tx.Characters_Become_Maxed[Character] != null && TJAPlayer3.Skin.Characters_Become_Maxed_Ptn[Character] != 0)
                        {
                            nowChara = TJAPlayer3.Tx.Characters_Become_Maxed[Character][(int)this.ctキャラクターアクション_魂MAX[i].n現在の値];
                        }
                        if (this.ctキャラクターアクション_魂MAX[i].b終了値に達した)
                        {
                            this.bマイどんアクション中[i] = false;
                            this.ctキャラクターアクション_魂MAX[i].t停止();
                            this.ctキャラクターアクション_魂MAX[i].n現在の値 = 0;
                        }
                    }

                    if (this.ctキャラクターアクション_Return[i].b進行中)
                    {
                        if (TJAPlayer3.Tx.Characters_Return[Character] != null && TJAPlayer3.Skin.Characters_Return_Ptn[Character] != 0)
                        {
                            nowChara = TJAPlayer3.Tx.Characters_Return[Character][(int)this.ctキャラクターアクション_Return[i].n現在の値];
                        }
                        if (this.ctキャラクターアクション_Return[i].b終了値に達した)
                        {
                            this.bマイどんアクション中[i] = false;
                            this.ctキャラクターアクション_Return[i].t停止();
                            this.ctキャラクターアクション_Return[i].n現在の値 = 0;
                        }
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
                        nowChara.t2D左右反転描画(TJAPlayer3.app.Device, chara_x, chara_y);
                    }
                    else
                    {
                        nowChara.t2D描画(TJAPlayer3.app.Device, chara_x, chara_y);
                    }

                    nowChara.vc拡大縮小倍率.X = 1.0f;
                    nowChara.vc拡大縮小倍率.Y = 1.0f;
                }

                if ((this.b風船連打中[i] != true && CharaAction_Balloon_Delay[i].b終了値に達した) || TJAPlayer3.ConfigIni.nPlayerCount > 2)
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
            return base.On進行描画();
        }

        public void OnDraw_Balloon()
        {
            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                if (TJAPlayer3.Skin.Characters_Balloon_Breaking_Ptn[iCurrentCharacter[i]] != 0) CharaAction_Balloon_Breaking[i]?.t進行();
                if (TJAPlayer3.Skin.Characters_Balloon_Broke_Ptn[iCurrentCharacter[i]] != 0) CharaAction_Balloon_Broke[i]?.t進行();
                CharaAction_Balloon_Delay[i]?.t進行();
                if (TJAPlayer3.Skin.Characters_Balloon_Miss_Ptn[iCurrentCharacter[i]] != 0) CharaAction_Balloon_Miss[i]?.t進行();
                CharaAction_Balloon_FadeOut[i].Tick();

                if (bマイどんアクション中[i])
                {
                    var nowOpacity = CharaAction_Balloon_FadeOut[i].Counter.b進行中 ? (int)CharaAction_Balloon_FadeOut[i].GetAnimation() : 255;

                    float resolutionScaleX = TJAPlayer3.Skin.Resolution[0] / (float)TJAPlayer3.Skin.Characters_Resolution[this.iCurrentCharacter[i]][0];
                    float resolutionScaleY = TJAPlayer3.Skin.Resolution[1] / (float)TJAPlayer3.Skin.Characters_Resolution[this.iCurrentCharacter[i]][1];

                    float chara_x = 0;
                    float chara_y = 0;

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


                    if (CharaAction_Balloon_Broke[i]?.b進行中 == true && TJAPlayer3.Skin.Characters_Balloon_Broke_Ptn[this.iCurrentCharacter[i]] != 0)
                    {
                        if (CharaAction_Balloon_FadeOut[i].Counter.b停止中 && CharaAction_Balloon_Broke[i].n現在の値 > CharaAction_Balloon_FadeOut_StartMs[i][0])
                        {
                            CharaAction_Balloon_FadeOut[i].Start();
                        }
                        
                        if (TJAPlayer3.Tx.Characters_Balloon_Broke[this.iCurrentCharacter[i]][CharaAction_Balloon_Broke[i].n現在の値] != null)
                        {
                            TJAPlayer3.Tx.Characters_Balloon_Broke[this.iCurrentCharacter[i]][CharaAction_Balloon_Broke[i].n現在の値].Opacity = nowOpacity;
                            TJAPlayer3.Tx.Characters_Balloon_Broke[this.iCurrentCharacter[i]][CharaAction_Balloon_Broke[i].n現在の値].vc拡大縮小倍率.X = charaScale;
                            TJAPlayer3.Tx.Characters_Balloon_Broke[this.iCurrentCharacter[i]][CharaAction_Balloon_Broke[i].n現在の値].vc拡大縮小倍率.Y = charaScale;
                            TJAPlayer3.Tx.Characters_Balloon_Broke[this.iCurrentCharacter[i]][CharaAction_Balloon_Broke[i].n現在の値].t2D描画(TJAPlayer3.app.Device, 
                                (TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.nDefaultJudgePos[0, 0]) 
                                + chara_x,
                                chara_y);
                        }

                        if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
                            TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画((TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.nDefaultJudgePos[0, 0]) + TJAPlayer3.Skin.Game_PuchiChara_BalloonX[0], TJAPlayer3.Skin.Game_PuchiChara_BalloonY[i], false, nowOpacity, true, player : i);
                        
                        if (CharaAction_Balloon_Broke[i].b終了値に達した)
                        {
                            CharaAction_Balloon_Broke[i].t停止();
                            CharaAction_Balloon_Broke[i].n現在の値 = 0;
                            bマイどんアクション中[i] = false;
                        }
                    }
                    else if (CharaAction_Balloon_Miss[i]?.b進行中 == true && TJAPlayer3.Skin.Characters_Balloon_Miss_Ptn[this.iCurrentCharacter[i]] != 0)
                    {
                        if (CharaAction_Balloon_FadeOut[i].Counter.b停止中 && CharaAction_Balloon_Miss[i].n現在の値 > CharaAction_Balloon_FadeOut_StartMs[i][1])
                        {
                            CharaAction_Balloon_FadeOut[i].Start();
                        }

                        if (TJAPlayer3.Tx.Characters_Balloon_Miss[this.iCurrentCharacter[i]][CharaAction_Balloon_Miss[i].n現在の値] != null)
                        {
                            TJAPlayer3.Tx.Characters_Balloon_Miss[this.iCurrentCharacter[i]][CharaAction_Balloon_Miss[i].n現在の値].Opacity = nowOpacity;
                            TJAPlayer3.Tx.Characters_Balloon_Miss[this.iCurrentCharacter[i]][CharaAction_Balloon_Miss[i].n現在の値].vc拡大縮小倍率.X = charaScale;
                            TJAPlayer3.Tx.Characters_Balloon_Miss[this.iCurrentCharacter[i]][CharaAction_Balloon_Miss[i].n現在の値].vc拡大縮小倍率.Y = charaScale;
                            TJAPlayer3.Tx.Characters_Balloon_Miss[this.iCurrentCharacter[i]][CharaAction_Balloon_Miss[i].n現在の値].t2D描画(TJAPlayer3.app.Device, 
                                (TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.nDefaultJudgePos[0, 0]) 
                                + chara_x,
                                chara_y);
                        }

                        if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
                            TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画((TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.nDefaultJudgePos[0, 0]) + TJAPlayer3.Skin.Game_PuchiChara_BalloonX[0], TJAPlayer3.Skin.Game_PuchiChara_BalloonY[i], false, nowOpacity, true, player : i);
                        
                        if (CharaAction_Balloon_Miss[i].b終了値に達した)
                        {
                            CharaAction_Balloon_Miss[i].t停止();
                            CharaAction_Balloon_Miss[i].n現在の値 = 0;
                            bマイどんアクション中[i] = false;
                        }
                    }
                    else if (CharaAction_Balloon_Breaking[i]?.b進行中 == true && TJAPlayer3.Skin.Characters_Balloon_Breaking_Ptn[this.iCurrentCharacter[i]] != 0)
                    {
                        if (TJAPlayer3.Tx.Characters_Balloon_Breaking[this.iCurrentCharacter[i]][CharaAction_Balloon_Breaking[i].n現在の値] != null)
                        {
                            TJAPlayer3.Tx.Characters_Balloon_Breaking[this.iCurrentCharacter[i]][CharaAction_Balloon_Breaking[i].n現在の値].vc拡大縮小倍率.X = charaScale;
                            TJAPlayer3.Tx.Characters_Balloon_Breaking[this.iCurrentCharacter[i]][CharaAction_Balloon_Breaking[i].n現在の値].vc拡大縮小倍率.Y = charaScale;
                            TJAPlayer3.Tx.Characters_Balloon_Breaking[this.iCurrentCharacter[i]][CharaAction_Balloon_Breaking[i].n現在の値].t2D描画(TJAPlayer3.app.Device,
                                (TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.nDefaultJudgePos[0, 0]) +
                                chara_x,
                                chara_y);
                        }

                        if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
                            TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画((TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.nDefaultJudgePos[0, 0]) + TJAPlayer3.Skin.Game_PuchiChara_BalloonX[0], TJAPlayer3.Skin.Game_PuchiChara_BalloonY[i], false, 255, true, player : i);
                    }
                }
            }
        }

        public void アクションタイマーリセット(int player)
        { 
                ctキャラクターアクション_10コンボ[player].t停止();
                ctキャラクターアクション_10コンボMAX[player].t停止();
                ctキャラクターアクション_ゴーゴースタート[player].t停止();
                ctキャラクターアクション_ゴーゴースタートMAX[player].t停止();
                ctキャラクターアクション_ノルマ[player].t停止();
                ctキャラクターアクション_魂MAX[player].t停止();
                ctキャラクターアクション_Return[player].t停止();
                ctキャラクターアクション_10コンボ[player].n現在の値 = 0;
                ctキャラクターアクション_10コンボMAX[player].n現在の値 = 0;
                ctキャラクターアクション_ゴーゴースタート[player].n現在の値 = 0;
                ctキャラクターアクション_ゴーゴースタートMAX[player].n現在の値 = 0;
                ctキャラクターアクション_ノルマ[player].n現在の値 = 0;
                ctキャラクターアクション_魂MAX[player].n現在の値 = 0;
                ctキャラクターアクション_Return[player].n現在の値 = 0;
                CharaAction_Balloon_Breaking[player]?.t停止();
                CharaAction_Balloon_Broke[player]?.t停止();
                CharaAction_Balloon_Miss[player]?.t停止();
                //CharaAction_Balloon_Delay?.t停止();
                CharaAction_Balloon_Breaking[player].n現在の値 = 0;
                CharaAction_Balloon_Broke[player].n現在の値 = 0;
                CharaAction_Balloon_Miss[player].n現在の値 = 0;
                //CharaAction_Balloon_Delay.n現在の値 = 0;
        }

        public int[][] arモーション番号 = new int[5][];
        public int[][] arMissモーション番号 = new int[5][];
        public int[][] arMissDownモーション番号 = new int[5][];
        public int[][] arゴーゴーモーション番号 = new int[5][];
        public int[][] arクリアモーション番号 = new int[5][];

        public CCounter[] ctキャラクターアクション_10コンボ = new CCounter[5];
        public CCounter[] ctキャラクターアクション_10コンボMAX = new CCounter[5];
        public CCounter[] ctキャラクターアクション_ゴーゴースタート = new CCounter[5];
        public CCounter[] ctキャラクターアクション_ゴーゴースタートMAX = new CCounter[5];
        public CCounter[] ctキャラクターアクション_ノルマ = new CCounter[5];
        public CCounter[] ctキャラクターアクション_魂MAX = new CCounter[5];
        public CCounter[] ctキャラクターアクション_Return = new CCounter[5];
        public CCounter[] CharaAction_Balloon_Breaking = new CCounter[5];
        public CCounter[] CharaAction_Balloon_Broke = new CCounter[5];
        public CCounter[] CharaAction_Balloon_Miss = new CCounter[5];
        public CCounter[] CharaAction_Balloon_Delay = new CCounter[5];

        public CCounter[] ctChara_Normal = new CCounter[5];
        public CCounter[] ctChara_Miss = new CCounter[5];
        public CCounter[] ctChara_MissDown = new CCounter[5];
        public CCounter[] ctChara_GoGo = new CCounter[5];
        public CCounter[] ctChara_Clear = new CCounter[5];

        public Animations.FadeOut[] CharaAction_Balloon_FadeOut = new Animations.FadeOut[5];
        //private readonly int[] CharaAction_Balloon_FadeOut_StartMs = new int[5];
        private readonly int[][] CharaAction_Balloon_FadeOut_StartMs = new int[5][];

        public bool[] bマイどんアクション中 = new bool[5];

        public bool[] b風船連打中 = new bool[5];
        public bool[] b演奏中 = new bool[5];

        public int[] iCurrentCharacter = new int[5] { 0, 0, 0, 0, 0 };
    }
}
