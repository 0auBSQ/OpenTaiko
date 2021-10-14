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

        public override void On活性化()
        {
            for(int i = 0; i < 2; i++)
            {
                ctChara_Normal[i] = new CCounter();
                ctChara_GoGo[i] = new CCounter();
                ctChara_Clear[i] = new CCounter();

                this.ctキャラクターアクション_10コンボ[i] = new CCounter();
                this.ctキャラクターアクション_10コンボMAX[i] = new CCounter();
                this.ctキャラクターアクション_ゴーゴースタート[i] = new CCounter();
                this.ctキャラクターアクション_ゴーゴースタートMAX[i] = new CCounter();
                this.ctキャラクターアクション_ノルマ[i] = new CCounter();
                this.ctキャラクターアクション_魂MAX[i] = new CCounter();

                CharaAction_Balloon_Breaking[i] = new CCounter();
                CharaAction_Balloon_Broke[i] = new CCounter();
                CharaAction_Balloon_Miss[i] = new CCounter();
                CharaAction_Balloon_Delay[i] = new CCounter();

                this.b風船連打中[i] = false;
                this.b演奏中[i] = false;

                CharaAction_Balloon_FadeOut[i] = new Animations.FadeOut(TJAPlayer3.Skin.Game_Chara_Balloon_FadeOut);

                this.bマイどんアクション中[i] = false;
            }

            // ふうせん系アニメーションの総再生時間は画像枚数 x Tick間隔なので、
            // フェードアウトの開始タイミングは、総再生時間 - フェードアウト時間。
            var tick = TJAPlayer3.Skin.Game_Chara_Balloon_Timer;
            var balloonBrokePtn = TJAPlayer3.Skin.Game_Chara_Ptn_Balloon_Broke;
            var balloonMissPtn = TJAPlayer3.Skin.Game_Chara_Ptn_Balloon_Miss;
            CharaAction_Balloon_FadeOut_StartMs[0] = (balloonBrokePtn * tick) - TJAPlayer3.Skin.Game_Chara_Balloon_FadeOut;
            CharaAction_Balloon_FadeOut_StartMs[1] = (balloonMissPtn * tick) - TJAPlayer3.Skin.Game_Chara_Balloon_FadeOut;
            if (balloonBrokePtn > 1) CharaAction_Balloon_FadeOut_StartMs[0] /= balloonBrokePtn - 1;
            if (balloonMissPtn > 1) CharaAction_Balloon_FadeOut_StartMs[1] /= balloonMissPtn - 1; // - 1はタイマー用

            base.On活性化();
        }

        public override void On非活性化()
        {
            for (int i = 0; i < 2; i++)
            {
                ctChara_Normal[i] = null;
                ctChara_GoGo[i] = null;
                ctChara_Clear[i] = null;
                this.ctキャラクターアクション_10コンボ[i] = null;
                this.ctキャラクターアクション_10コンボMAX[i] = null;
                this.ctキャラクターアクション_ゴーゴースタート[i] = null;
                this.ctキャラクターアクション_ゴーゴースタートMAX[i] = null;
                this.ctキャラクターアクション_ノルマ[i] = null;
                this.ctキャラクターアクション_魂MAX[i] = null;

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
            this.arモーション番号 = C変換.ar配列形式のstringをint配列に変換して返す( TJAPlayer3.Skin.Game_Chara_Motion_Normal);
            this.arゴーゴーモーション番号 = C変換.ar配列形式のstringをint配列に変換して返す(TJAPlayer3.Skin.Game_Chara_Motion_GoGo);
            this.arクリアモーション番号 = C変換.ar配列形式のstringをint配列に変換して返す(TJAPlayer3.Skin.Game_Chara_Motion_Clear);
            if (arモーション番号 == null) this.arモーション番号 = C変換.ar配列形式のstringをint配列に変換して返す("0,0");
            if (arゴーゴーモーション番号 == null) this.arゴーゴーモーション番号 = C変換.ar配列形式のstringをint配列に変換して返す("0,0");
            if (arクリアモーション番号 == null) this.arクリアモーション番号 = C変換.ar配列形式のstringをint配列に変換して返す("0,0");
            for (int i = 0; i < 2; i++)
            {
                ctChara_Normal[i] = new CCounter(0, arモーション番号.Length - 1, 10, CSound管理.rc演奏用タイマ);
                ctChara_GoGo[i] = new CCounter(0, arゴーゴーモーション番号.Length - 1, 10, CSound管理.rc演奏用タイマ);
                ctChara_Clear[i] = new CCounter(0, arクリアモーション番号.Length - 1, 10, CSound管理.rc演奏用タイマ);
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
                if (ctChara_Normal != null || TJAPlayer3.Skin.Game_Chara_Ptn_Normal != 0) ctChara_Normal[i].t進行LoopDb();
                if (ctChara_GoGo != null || TJAPlayer3.Skin.Game_Chara_Ptn_GoGo != 0) ctChara_GoGo[i].t進行LoopDb();
                if (ctChara_Clear != null || TJAPlayer3.Skin.Game_Chara_Ptn_Clear != 0) ctChara_Clear[i].t進行LoopDb();
                if (this.ctキャラクターアクション_10コンボ != null || TJAPlayer3.Skin.Game_Chara_Ptn_10combo != 0) this.ctキャラクターアクション_10コンボ[i].t進行db();
                if (this.ctキャラクターアクション_10コンボMAX != null || TJAPlayer3.Skin.Game_Chara_Ptn_10combo_Max != 0) this.ctキャラクターアクション_10コンボMAX[i].t進行db();
                if (this.ctキャラクターアクション_ゴーゴースタート != null || TJAPlayer3.Skin.Game_Chara_Ptn_GoGoStart != 0) this.ctキャラクターアクション_ゴーゴースタート[i].t進行db();
                if (this.ctキャラクターアクション_ゴーゴースタートMAX != null || TJAPlayer3.Skin.Game_Chara_Ptn_GoGoStart_Max != 0) this.ctキャラクターアクション_ゴーゴースタートMAX[i].t進行db();
                if (this.ctキャラクターアクション_ノルマ != null || TJAPlayer3.Skin.Game_Chara_Ptn_ClearIn != 0) this.ctキャラクターアクション_ノルマ[i].t進行db();
                if (this.ctキャラクターアクション_魂MAX != null || TJAPlayer3.Skin.Game_Chara_Ptn_SoulIn != 0) this.ctキャラクターアクション_魂MAX[i].t進行db();

                // Blinking animation during invincibility frames
                if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
                {
                    if (CFloorManagement.isBlinking() == true)
                        break;
                }

                if (this.b風船連打中[i] != true && this.bマイどんアクション中[i] != true && CharaAction_Balloon_Delay[i].b終了値に達した)
                {
                    if (!TJAPlayer3.stage演奏ドラム画面.bIsGOGOTIME[i])
                    {
                        if (TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[i] >= 100.0 && TJAPlayer3.Skin.Game_Chara_Ptn_Clear != 0)
                        {
                            if (TJAPlayer3.Skin.Game_Chara_Ptn_Clear != 0)
                                TJAPlayer3.Tx.Chara_Normal_Maxed[this.arクリアモーション番号[(int)this.ctChara_Clear[i].n現在の値]].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Chara_X[i], TJAPlayer3.Skin.Game_Chara_Y[i]);
                        }
                        else if (TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[i] >= 80.0 && TJAPlayer3.Skin.Game_Chara_Ptn_Clear != 0)
                        {
                            if (TJAPlayer3.Skin.Game_Chara_Ptn_Clear != 0)
                            {
                                TJAPlayer3.Tx.Chara_Normal_Cleared[this.arクリアモーション番号[(int)this.ctChara_Clear[i].n現在の値]].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Chara_X[i], TJAPlayer3.Skin.Game_Chara_Y[i]);
                            }
                        }
                        else
                        {
                            if (TJAPlayer3.Skin.Game_Chara_Ptn_Normal != 0)
                            {
                                TJAPlayer3.Tx.Chara_Normal[this.arモーション番号[(int)this.ctChara_Normal[i].n現在の値]].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Chara_X[i], TJAPlayer3.Skin.Game_Chara_Y[i]);
                            }
                        }
                    }
                    else
                    {
                        if (TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[i] >= 100.0 && TJAPlayer3.Skin.Game_Chara_Ptn_GoGo != 0)
                        {
                            if (TJAPlayer3.Skin.Game_Chara_Ptn_GoGo != 0)
                                TJAPlayer3.Tx.Chara_GoGoTime_Maxed[this.arゴーゴーモーション番号[(int)this.ctChara_GoGo[i].n現在の値]].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Chara_X[i], TJAPlayer3.Skin.Game_Chara_Y[i]);
                        }
                        else
                        {
                            if (TJAPlayer3.Skin.Game_Chara_Ptn_GoGo != 0)
                                TJAPlayer3.Tx.Chara_GoGoTime[this.arゴーゴーモーション番号[(int)this.ctChara_GoGo[i].n現在の値]].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Chara_X[i], TJAPlayer3.Skin.Game_Chara_Y[i]);
                        }
                    }
                }

                if (this.b風船連打中[i] != true && bマイどんアクション中[i] == true && CharaAction_Balloon_Delay[i].b終了値に達した)
                {
                    if (this.ctキャラクターアクション_10コンボ[i].b進行中)
                    {
                        if (TJAPlayer3.Tx.Chara_10Combo[i] != null && TJAPlayer3.Skin.Game_Chara_Ptn_10combo != 0)
                        {
                            TJAPlayer3.Tx.Chara_10Combo[(int)this.ctキャラクターアクション_10コンボ[i].n現在の値].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Chara_X[i], TJAPlayer3.Skin.Game_Chara_Y[i]);
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
                        if (TJAPlayer3.Tx.Chara_10Combo_Maxed[i] != null && TJAPlayer3.Skin.Game_Chara_Ptn_10combo_Max != 0)
                        {
                            TJAPlayer3.Tx.Chara_10Combo_Maxed[(int)this.ctキャラクターアクション_10コンボMAX[i].n現在の値].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Chara_X[i], TJAPlayer3.Skin.Game_Chara_Y[i]);
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
                        if (TJAPlayer3.Tx.Chara_GoGoStart[i] != null && TJAPlayer3.Skin.Game_Chara_Ptn_GoGoStart != 0)
                        {
                            TJAPlayer3.Tx.Chara_GoGoStart[(int)this.ctキャラクターアクション_ゴーゴースタート[i].n現在の値].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Chara_X[i], TJAPlayer3.Skin.Game_Chara_Y[i]);
                        }
                        if (this.ctキャラクターアクション_ゴーゴースタート[i].b終了値に達した)
                        {
                            this.bマイどんアクション中[i] = false;
                            this.ctキャラクターアクション_ゴーゴースタート[i].t停止();
                            this.ctキャラクターアクション_ゴーゴースタート[i].n現在の値 = 0;
                            this.ctChara_GoGo[i].n現在の値 = TJAPlayer3.Skin.Game_Chara_Ptn_GoGo / 2;
                        }
                    }

                    if (this.ctキャラクターアクション_ゴーゴースタートMAX[i].b進行中)
                    {
                        if (TJAPlayer3.Tx.Chara_GoGoStart_Maxed[i] != null && TJAPlayer3.Skin.Game_Chara_Ptn_GoGoStart_Max != 0)
                        {
                            TJAPlayer3.Tx.Chara_GoGoStart_Maxed[(int)this.ctキャラクターアクション_ゴーゴースタートMAX[i].n現在の値].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Chara_X[i], TJAPlayer3.Skin.Game_Chara_Y[i]);
                        }
                        if (this.ctキャラクターアクション_ゴーゴースタートMAX[i].b終了値に達した)
                        {
                            this.bマイどんアクション中[i] = false;
                            this.ctキャラクターアクション_ゴーゴースタートMAX[i].t停止();
                            this.ctキャラクターアクション_ゴーゴースタートMAX[i].n現在の値 = 0;
                            this.ctChara_GoGo[i].n現在の値 = TJAPlayer3.Skin.Game_Chara_Ptn_GoGo / 2;
                        }
                    }

                    if (this.ctキャラクターアクション_ノルマ[i].b進行中)
                    {
                        if (TJAPlayer3.Tx.Chara_Become_Cleared[i] != null && TJAPlayer3.Skin.Game_Chara_Ptn_ClearIn != 0)
                        {
                            TJAPlayer3.Tx.Chara_Become_Cleared[(int)this.ctキャラクターアクション_ノルマ[i].n現在の値].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Chara_X[i], TJAPlayer3.Skin.Game_Chara_Y[i]);
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
                        if (TJAPlayer3.Tx.Chara_Become_Maxed[i] != null && TJAPlayer3.Skin.Game_Chara_Ptn_SoulIn != 0)
                        {
                            TJAPlayer3.Tx.Chara_Become_Maxed[(int)this.ctキャラクターアクション_魂MAX[i].n現在の値].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Chara_X[i], TJAPlayer3.Skin.Game_Chara_Y[i]);
                        }
                        if (this.ctキャラクターアクション_魂MAX[i].b終了値に達した)
                        {
                            this.bマイどんアクション中[i] = false;
                            this.ctキャラクターアクション_魂MAX[i].t停止();
                            this.ctキャラクターアクション_魂MAX[i].n現在の値 = 0;
                        }
                    }
                }
                if (this.b風船連打中[i] != true && CharaAction_Balloon_Delay[i].b終了値に達した)
                {
                    TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画(TJAPlayer3.Skin.Game_PuchiChara_X[i], TJAPlayer3.Skin.Game_PuchiChara_Y[i], TJAPlayer3.stage演奏ドラム画面.bIsAlreadyMaxed[i], player : i);
                }
            }
            return base.On進行描画();
        }

        public void OnDraw_Balloon()
        {
            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                if (TJAPlayer3.Skin.Game_Chara_Ptn_Balloon_Breaking != 0) CharaAction_Balloon_Breaking[i]?.t進行();
                if (TJAPlayer3.Skin.Game_Chara_Ptn_Balloon_Broke != 0) CharaAction_Balloon_Broke[i]?.t進行();
                CharaAction_Balloon_Delay[i]?.t進行();
                if (TJAPlayer3.Skin.Game_Chara_Ptn_Balloon_Miss != 0) CharaAction_Balloon_Miss[i]?.t進行();
                CharaAction_Balloon_FadeOut[i].Tick();

                //CharaAction_Balloon_Delay?.t進行();
                //CDTXMania.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, CharaAction_Balloon_Broke?.b進行中.ToString());
                //CDTXMania.act文字コンソール.tPrint(0, 20, C文字コンソール.Eフォント種別.白, CharaAction_Balloon_Miss?.b進行中.ToString());
                //CDTXMania.act文字コンソール.tPrint(0, 40, C文字コンソール.Eフォント種別.白, CharaAction_Balloon_Breaking?.b進行中.ToString());

                if (bマイどんアクション中[i])
                {
                    var nowOpacity = CharaAction_Balloon_FadeOut[i].Counter.b進行中 ? (int)CharaAction_Balloon_FadeOut[i].GetAnimation() : 255;
                    if (CharaAction_Balloon_Broke[i]?.b進行中 == true && TJAPlayer3.Skin.Game_Chara_Ptn_Balloon_Broke != 0)
                    {
                        if (CharaAction_Balloon_FadeOut[i].Counter.b停止中 && CharaAction_Balloon_Broke[i].n現在の値 > CharaAction_Balloon_FadeOut_StartMs[i])
                        {
                            CharaAction_Balloon_FadeOut[i].Start();
                        }
                        if (TJAPlayer3.Tx.Chara_Balloon_Broke[CharaAction_Balloon_Broke[i].n現在の値] != null)
                        {
                            TJAPlayer3.Tx.Chara_Balloon_Broke[CharaAction_Balloon_Broke[i].n現在の値].Opacity = nowOpacity;
                            TJAPlayer3.Tx.Chara_Balloon_Broke[CharaAction_Balloon_Broke[i].n現在の値].t2D描画(TJAPlayer3.app.Device, (TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.nDefaultJudgePos[0, 0]) + TJAPlayer3.Skin.Game_Chara_Balloon_X[0], TJAPlayer3.Skin.Game_Chara_Balloon_Y[i]);
                        }
                        TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画((TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.nDefaultJudgePos[0, 0]) + TJAPlayer3.Skin.Game_PuchiChara_BalloonX[0], TJAPlayer3.Skin.Game_PuchiChara_BalloonY[i], false, nowOpacity, true, player : i);
                        if (CharaAction_Balloon_Broke[i].b終了値に達した)
                        {
                            CharaAction_Balloon_Broke[i].t停止();
                            CharaAction_Balloon_Broke[i].n現在の値 = 0;
                            bマイどんアクション中[i] = false;
                        }
                    }
                    else if (CharaAction_Balloon_Miss[i]?.b進行中 == true && TJAPlayer3.Skin.Game_Chara_Ptn_Balloon_Miss != 0)
                    {
                        if (CharaAction_Balloon_FadeOut[i].Counter.b停止中 && CharaAction_Balloon_Miss[i].n現在の値 > CharaAction_Balloon_FadeOut_StartMs[1])
                        {
                            CharaAction_Balloon_FadeOut[i].Start();
                        }
                        if (TJAPlayer3.Tx.Chara_Balloon_Miss[CharaAction_Balloon_Miss[i].n現在の値] != null)
                        {
                            TJAPlayer3.Tx.Chara_Balloon_Miss[CharaAction_Balloon_Miss[i].n現在の値].Opacity = nowOpacity;
                            TJAPlayer3.Tx.Chara_Balloon_Miss[CharaAction_Balloon_Miss[i].n現在の値].t2D描画(TJAPlayer3.app.Device, (TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.nDefaultJudgePos[0, 0]) + TJAPlayer3.Skin.Game_Chara_Balloon_X[0], TJAPlayer3.Skin.Game_Chara_Balloon_Y[i]);
                        }
                        TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画((TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.nDefaultJudgePos[0, 0]) + TJAPlayer3.Skin.Game_PuchiChara_BalloonX[0], TJAPlayer3.Skin.Game_PuchiChara_BalloonY[i], false, nowOpacity, true, player : i);
                        if (CharaAction_Balloon_Miss[i].b終了値に達した)
                        {
                            CharaAction_Balloon_Miss[i].t停止();
                            CharaAction_Balloon_Miss[i].n現在の値 = 0;
                            bマイどんアクション中[i] = false;
                        }
                    }
                    else if (CharaAction_Balloon_Breaking[i]?.b進行中 == true && TJAPlayer3.Skin.Game_Chara_Ptn_Balloon_Breaking != 0)
                    {
                        TJAPlayer3.Tx.Chara_Balloon_Breaking[CharaAction_Balloon_Breaking[i].n現在の値]?.t2D描画(TJAPlayer3.app.Device, (TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.nDefaultJudgePos[0, 0]) + TJAPlayer3.Skin.Game_Chara_Balloon_X[0], TJAPlayer3.Skin.Game_Chara_Balloon_Y[i]);
                        TJAPlayer3.stage演奏ドラム画面.PuchiChara.On進行描画((TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.nDefaultJudgePos[0, 0]) + TJAPlayer3.Skin.Game_PuchiChara_BalloonX[0], TJAPlayer3.Skin.Game_PuchiChara_BalloonY[i], false, 255, true, player : i);
                    }

                    //if (CDTXMania.stage演奏ドラム画面.actChara.CharaAction_Balloon_Breaking?.b終了値に達した == true)
                    //{
                    //    CDTXMania.stage演奏ドラム画面.actChara.bマイどんアクション中 = false;
                    //    CDTXMania.stage演奏ドラム画面.actChara.CharaAction_Balloon_Breaking.t停止();
                    //    CDTXMania.stage演奏ドラム画面.actChara.CharaAction_Balloon_Breaking.n現在の値 = 0;
                    //}

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
                ctキャラクターアクション_10コンボ[player].n現在の値 = 0;
                ctキャラクターアクション_10コンボMAX[player].n現在の値 = 0;
                ctキャラクターアクション_ゴーゴースタート[player].n現在の値 = 0;
                ctキャラクターアクション_ゴーゴースタートMAX[player].n現在の値 = 0;
                ctキャラクターアクション_ノルマ[player].n現在の値 = 0;
                ctキャラクターアクション_魂MAX[player].n現在の値 = 0;
                CharaAction_Balloon_Breaking[player]?.t停止();
                CharaAction_Balloon_Broke[player]?.t停止();
                CharaAction_Balloon_Miss[player]?.t停止();
                //CharaAction_Balloon_Delay?.t停止();
                CharaAction_Balloon_Breaking[player].n現在の値 = 0;
                CharaAction_Balloon_Broke[player].n現在の値 = 0;
                CharaAction_Balloon_Miss[player].n現在の値 = 0;
                //CharaAction_Balloon_Delay.n現在の値 = 0;
        }

        public int[] arモーション番号;
        public int[] arゴーゴーモーション番号;
        public int[] arクリアモーション番号;

        public CCounter[] ctキャラクターアクション_10コンボ = new CCounter[2];
        public CCounter[] ctキャラクターアクション_10コンボMAX = new CCounter[2];
        public CCounter[] ctキャラクターアクション_ゴーゴースタート = new CCounter[2];
        public CCounter[] ctキャラクターアクション_ゴーゴースタートMAX = new CCounter[2];
        public CCounter[] ctキャラクターアクション_ノルマ = new CCounter[2];
        public CCounter[] ctキャラクターアクション_魂MAX = new CCounter[2];
        public CCounter[] CharaAction_Balloon_Breaking = new CCounter[2];
        public CCounter[] CharaAction_Balloon_Broke = new CCounter[2];
        public CCounter[] CharaAction_Balloon_Miss = new CCounter[2];
        public CCounter[] CharaAction_Balloon_Delay = new CCounter[2];

        public CCounter[] ctChara_Normal = new CCounter[2];
        public CCounter[] ctChara_GoGo = new CCounter[2];
        public CCounter[] ctChara_Clear = new CCounter[2];

        public Animations.FadeOut[] CharaAction_Balloon_FadeOut = new Animations.FadeOut[2];
        private readonly int[] CharaAction_Balloon_FadeOut_StartMs = new int[2];

        public bool[] bマイどんアクション中 = new bool[2];

        public bool[] b風船連打中 = new bool[2];
        public bool[] b演奏中 = new bool[2];
    }
}
