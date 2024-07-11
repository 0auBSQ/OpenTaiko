using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Text.RegularExpressions;
using FDK;
using FDK.ExtensionMethods;
using System.Linq;
using TJAPlayer3;
using SkiaSharp;

using Point = System.Drawing.Point;
using Color = System.Drawing.Color;

namespace TJAPlayer3
{
    internal class CDTX : CActivity
    {
        // 定数

        public enum E種別 { DTX, GDA, G2D, BMS, BME, SMF }

        public List<string> listErrors = new List<string>();
        private int nNowReadLine;
        // クラス

        public class CBPM
        {
            public double dbBPM値;
            public double bpm_change_time;
            public double bpm_change_bmscroll_time;
            public ECourse bpm_change_course = ECourse.eNormal;
            public int n内部番号;
            public int n表記上の番号;

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder(0x80);
                if (this.n内部番号 != this.n表記上の番号)
                {
                    builder.Append(string.Format("CBPM{0}(内部{1})", CDTX.tZZ(this.n表記上の番号), this.n内部番号));
                }
                else
                {
                    builder.Append(string.Format("CBPM{0}", CDTX.tZZ(this.n表記上の番号)));
                }
                builder.Append(string.Format(", BPM:{0}", this.dbBPM値));
                return builder.ToString();
            }
        }
        public class CSCROLL
        {
            public double dbSCROLL値;
            public double dbSCROLL値Y;
            public int n内部番号;
            public int n表記上の番号;

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder(0x80);
                if (this.n内部番号 != this.n表記上の番号)
                {
                    builder.Append(string.Format("CSCROLL{0}(内部{1})", CDTX.tZZ(this.n表記上の番号), this.n内部番号));
                }
                else
                {
                    builder.Append(string.Format("CSCROLL{0}", CDTX.tZZ(this.n表記上の番号)));
                }
                builder.Append(string.Format(", SCROLL:{0}", this.dbSCROLL値));
                return builder.ToString();
            }
        }
        /// <summary>
        /// 判定ライン移動命令
        /// </summary>
		public class CJPOSSCROLL
        {
            public double db移動時間;
            public int n移動距離px;
            public int n移動方向; //移動方向は0(左)、1(右)の2つだけ。
            public int n内部番号;
            public int n表記上の番号;
            public int nVerticalMove;

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder(0x80);
                if (this.n内部番号 != this.n表記上の番号)
                {
                    builder.Append(string.Format("CJPOSSCROLL{0}(内部{1})", CDTX.tZZ(this.n表記上の番号), this.n内部番号));
                }
                else
                {
                    builder.Append(string.Format("CJPOSSCROLL{0}", CDTX.tZZ(this.n表記上の番号)));
                }
                builder.Append(string.Format(", JPOSSCROLL:{0}", this.db移動時間));
                return builder.ToString();
            }
        }

        public class CDELAY
        {
            public int nDELAY値; //格納時にはmsになっているため、doubleにはしない。
            public int n内部番号;
            public int n表記上の番号;
            public double delay_time;
            public double delay_bmscroll_time;
            public double delay_bpm;
            public ECourse delay_course = ECourse.eNormal;

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder(0x80);
                if (this.n内部番号 != this.n表記上の番号)
                {
                    builder.Append(string.Format("CDELAY{0}(内部{1})", CDTX.tZZ(this.n表記上の番号), this.n内部番号));
                }
                else
                {
                    builder.Append(string.Format("CDELAY{0}", CDTX.tZZ(this.n表記上の番号)));
                }
                builder.Append(string.Format(", DELAY:{0}", this.nDELAY値));
                return builder.ToString();
            }
        }
        public enum E分岐種類
        {
            e精度分岐,
            e連打分岐,
            eスコア分岐,
            e大音符のみ精度分岐
        }
        public class CBRANCH
        {
            public E分岐種類 e分岐の種類; //0:精度分岐 1:連打分岐 2:スコア分岐 3:大音符のみの精度分岐
            public double n条件数値A;
            public double n条件数値B;
            public double db分岐時間;
            public double db分岐時間ms;
            public double db判定時間;
            public double dbBMScrollTime;
            public double dbBPM;
            public double dbSCROLL;
            public int n現在の小節;
            public int n命令時のChipList番号;

            public int n表記上の番号;
            public int n内部番号;

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder(0x80);
                if (this.n内部番号 != this.n表記上の番号)
                {
                    builder.Append(string.Format("CBRANCH{0}(内部{1})", CDTX.tZZ(this.n表記上の番号), this.n内部番号));
                }
                else
                {
                    builder.Append(string.Format("CBRANCH{0}", CDTX.tZZ(this.n表記上の番号)));
                }
                builder.Append(string.Format(", BRANCH:{0}", this.e分岐の種類));
                return builder.ToString();
            }
        }


        public class CChip : IComparable<CDTX.CChip>, ICloneable
        {
            public EScrollMode eScrollMode;
            public bool bHit;
            public bool b可視 = true;
            public bool bHideBarLine = true;
            public bool bProcessed = false;
            public bool bShow;
            public bool bShowRoll;
            public bool bBranch = false;
            public double dbチップサイズ倍率 = 1.0;
            public double db実数値;
            public double dbBPM;
            public float fNow_Measure_s = 4.0f;//強制分岐のために追加.2020.04.21.akasoko26
            public float fNow_Measure_m = 4.0f;//強制分岐のために追加.2020.04.21.akasoko26
            public bool IsEndedBranching = false;//分岐が終わった時の連打譜面が非可視化になってしまうためフラグを追加.2020.04.21.akasoko26
            public double dbSCROLL;
            public double dbSCROLL_Y;
            public ECourse nコース;
            public int nSenote;
            public int nState;
            public int nRollCount;
            public int nBalloon;
            public int nProcessTime;
            public int nスクロール方向;
            public int n描画優先度; //(特殊)現状連打との判断目的で使用
            public ENoteState eNoteState;
            public EInstrumentPad e楽器パート = EInstrumentPad.UNKNOWN;
            public int nチャンネル番号;
            public int VideoStartTimeMs;
            public STDGBVALUE<int> nバーからの距離dot;
            public int nバーからのノーツ末端距離dot;
            public int nバーからのノーツ末端距離dot_Y;
            public int n整数値;
            public int n文字数 = 16;

            public int n整数値_内部番号;
            public int n総移動時間;
            public int n透明度 = 0xff;
            public int n発声位置;
            public double n条件数値A;
            public double n条件数値B;
            public double db分岐時間のズレ;
            public E分岐種類 e分岐の種類;

            public double db発声位置;  // 発声時刻を格納していた変数のうちの１つをfloat型からdouble型に変更。(kairera0467)
            public double fBMSCROLLTime;
            public double fBMSCROLLTime_end;
            public int n発声時刻ms;
            public double n分岐時刻ms;


            public double db発声時刻ms;
            public int nノーツ終了位置;
            public int nノーツ終了時刻ms;
            public int nノーツ出現時刻ms;
            public int nノーツ移動開始時刻ms;
            public int n分岐回数;
            public int n連打音符State;
            public int nLag;                // 2011.2.1 yyagi
            public double db発声時刻;
            public double db判定終了時刻;//連打系音符で使用
            public double dbProcess_Time;
            public int nPlayerSide;
            public bool bGOGOTIME = false; //2018.03.11 k1airera0467 ゴーゴータイム内のチップであるか
            public int nList上の位置;
            public bool IsFixedSENote;
            public bool IsHitted = false;
            public bool IsMissed = false;



            //EXTENDED COMMANDS
            public int fCamTimeMs;
            public string strCamEaseType;
            public Easing.CalcType fCamMoveType;

            public float fCamScrollStartX;
            public float fCamScrollStartY;
            public float fCamScrollEndX;
            public float fCamScrollEndY;

            public float fCamRotationStart;
            public float fCamRotationEnd;

            public float fCamZoomStart;
            public float fCamZoomEnd;

            public float fCamScaleStartX;
            public float fCamScaleStartY;
            public float fCamScaleEndX;
            public float fCamScaleEndY;

            public Color4 borderColor;

            public int fObjTimeMs;
            public string strObjName;
            public string strObjEaseType;
            public Easing.CalcType objCalcType;

            public float fObjX;
            public float fObjY;

            public float fObjStart;
            public float fObjEnd;

            public CSongObject obj;

            public string strTargetTxName;
            public string strNewPath;

            public string strConfigValue;

            public double dbAnimInterval;

            public int intFrame;

            public EGameType eGameType;
            //


            public bool bBPMチップである
            {
                get
                {
                    if (this.nチャンネル番号 == 3 || this.nチャンネル番号 == 8)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            public bool b自動再生音チャンネルである
            {
                get
                {
                    int num = this.nチャンネル番号;
                    if ((((num != 1) && ((0x61 > num) || (num > 0x69))) && ((0x70 > num) || (num > 0x79))) && ((0x80 > num) || (num > 0x89)))
                    {
                        return ((0x90 <= num) && (num <= 0x92));
                    }
                    return true;
                }
            }



            public bool b演奏終了後も再生が続くチップである;	// #32248 2013.10.14 yyagi
            public CCounter RollDelay; // 18.9.22 AioiLight Add 連打時に赤くなるやつのタイマー
            public CCounter RollInputTime; // 18.9.22 AioiLight Add  連打入力後、RollDelayが作動するまでのタイマー
            public int RollEffectLevel; // 18.9.22 AioiLight Add 連打時に赤くなるやつの度合い

            public CChip()
            {
                this.nバーからの距離dot = new STDGBVALUE<int>()
                {
                    Drums = 0,
                    Guitar = 0,
                    Bass = 0,
                    Taiko = 0
                };
            }
            public void t初期化()
            {
                this.bBranch = false;
                this.nチャンネル番号 = 0;
                this.n整数値 = 0; //整数値をList上の番号として用いる。
                this.n整数値_内部番号 = 0;
                this.db実数値 = 0.0;
                this.n発声位置 = 0;
                this.db発声位置 = 0.0D;
                this.n発声時刻ms = 0;
                this.db発声時刻ms = 0.0D;
                this.fBMSCROLLTime = 0;
                this.nノーツ終了位置 = 0;
                this.nノーツ終了時刻ms = 0;
                this.n描画優先度 = 0;
                this.nLag = -999;
                this.b演奏終了後も再生が続くチップである = false;
                this.nList上の位置 = 0;
                this.dbチップサイズ倍率 = 1.0;
                this.bHit = false;
                this.IsMissed = false;
                this.b可視 = true;
                this.e楽器パート = EInstrumentPad.UNKNOWN;
                this.n透明度 = 0xff;
                this.nバーからの距離dot.Drums = 0;
                this.nバーからの距離dot.Guitar = 0;
                this.nバーからの距離dot.Bass = 0;
                this.nバーからの距離dot.Taiko = 0;
                this.nバーからのノーツ末端距離dot = 0;
                this.nバーからのノーツ末端距離dot_Y = 0;
                this.n総移動時間 = 0;
                this.dbBPM = 120.0;
                this.fNow_Measure_m = 4.0f;
                this.fNow_Measure_s = 4.0f;
                this.nスクロール方向 = 0;
                this.dbSCROLL = 1.0;
                this.dbSCROLL_Y = 0.0f;
            }
            public override string ToString()
            {

                //2016.10.07 kairera0467 近日中に再編成予定
                string[] chToStr =
                {
                    //システム
					"??", "バックコーラス", "小節長変更", "BPM変更", "??", "??", "??", "??",
                    "BPM変更(拡張)", "??", "??", "??", "??", "??", "??", "??",

                    //太鼓1P(移動予定)
					"??", "ドン", "カツ", "ドン(大)", "カツ(大)", "連打", "連打(大)", "ふうせん連打",
                    "連打終点", "芋", "ドン(手)", "カッ(手)", "Mine", "??", "??", "AD-LIB",

                    //太鼓予備
					"??", "??", "??", "??", "??", "??", "??", "??",
                    "??", "??", "??", "??", "??", "??", "??", "??",

                    //太鼓予備
					"??", "??", "??", "??", "??", "??", "??", "??",
                    "??", "??", "??", "??", "??", "??", "??", "??",

                    //太鼓予備
					"??", "??", "??", "??", "??", "??", "??", "??",
                    "??", "??", "??", "??", "??", "??", "??", "??", 

                    //システム
					"小節線", "拍線", "??", "??", "AVI", "??", "??", "??",
                    "??", "??", "??", "??", "??", "??", "??", "??", 

                    //システム(移動予定)
					"SCROLL", "DELAY", "ゴーゴータイム開始", "ゴーゴータイム終了", "カメラ移動開始(縦)", "カメラ移動終了(縦)", "カメラ移動開始(横)", "カメラ移動終了(横)",
                    "カメラズーム開始", "カメラズーム終了", "カメラ回転開始", "カメラ回転終了", "カメラスケーリング開始(横)", "カメラスケーリング終了(横)", "カメラスケーリング開始(縦)", "カメラスケーリング終了(縦)",

                    "ボーダーカラー変更", "??", "??", "??", "??", "??", "??", "??",
                    "??", "??", "??", "??", "??", "??", "??", "??",

                    "??", "??", "??", "??", "??", "??", "??", "??",
                    "??", "??", "??", "??", "??", "??", "??", "??", 

                    //太鼓1P、システム(現行)
					"??", "??", "??", "太鼓_赤", "太鼓_青", "太鼓_赤(大)", "太鼓_青(大)", "太鼓_黄",
                    "太鼓_黄(大)", "太鼓_風船", "太鼓_連打末端", "太鼓_芋", "??", "SCROLL", "ゴーゴータイム開始", "ゴーゴータイム終了",

                    "??", "??", "??", "??", "??", "??", "??", "??",
                    "??", "??", "??", "??", "??", "??", "??", "太鼓 AD-LIB",

                    "??", "??", "??", "??", "??", "??", "??", "??",
                    "??", "??", "??", "??", "??", "??", "??", "??",

                    "??", "??", "??", "??", "0xC4", "0xC5", "0xC6", "??",
                    "??", "??", "0xCA", "??", "??", "??", "??", "0xCF", 

                    //システム(現行)
					"0xD0", "??", "??", "??", "??", "??", "??", "??",
                    "??", "??", "ミキサー追加", "ミキサー削除", "DELAY", "譜面分岐リセット", "譜面分岐アニメ", "譜面分岐内部処理", 

                    //システム(現行)
					"小節線ON/OFF", "分岐固定", "判定枠移動", "", "", "", "", "",
                    "", "", "", "", "", "", "", "",

                    "0xF0", "歌詞", "??", "SUDDEN", "??", "??", "??", "??",
                    "??", "??", "??", "??", "??", "??", "??", "??", "譜面終了",

                    // Extra notes

                    "KaDon", "??", "??", "??", "??", "??", "??", "??",
                    "??", "??", "??", "??", "??", "??", "??", "??",
                };
                return string.Format("CChip: 位置:{0:D4}.{1:D3}, 時刻{2:D6}, Ch:{3:X2}({4}), Pn:{5}({11})(内部{6}), Pd:{7}, Sz:{8}, BMScroll:{9}, Auto:{10}, コース:{11}",
                    this.n発声位置 / 384, this.n発声位置 % 384,
                    this.n発声時刻ms,
                    this.nチャンネル番号, chToStr[this.nチャンネル番号],
                    this.n整数値, this.n整数値_内部番号,
                    this.db実数値,
                    this.dbチップサイズ倍率,
                    this.fBMSCROLLTime,
                    this.b自動再生音チャンネルである,
                    this.nコース,
                    CDTX.tZZ(this.n整数値));
            }
            /// <summary>
            /// チップの再生長を取得する。現状、WAVチップとBGAチップでのみ使用可能。
            /// </summary>
            /// <returns>再生長(ms)</returns>
            public int GetDuration()
            {
                int nDuration = 0;

                if (this.nチャンネル番号 == 0x01)       // WAV
                {
                    CDTX.CWAV wc;
                    TJAPlayer3.DTX.listWAV.TryGetValue(this.n整数値_内部番号, out wc);
                    if (wc == null)
                    {
                        nDuration = 0;
                    }
                    else
                    {
                        nDuration = (wc.rSound[0] == null) ? 0 : wc.rSound[0].TotalPlayTime;
                    }
                }
                else if (this.nチャンネル番号 == 0x54) // AVI
                {
					CVideoDecoder wc;
					TJAPlayer3.DTX.listVD.TryGetValue(this.n整数値_内部番号, out wc);
					if (wc == null)
					{
						nDuration = 0;
					}
					else
					{
						nDuration = (int)(wc.Duration * 1000);
					}
                }

                double _db再生速度 = (TJAPlayer3.DTXVmode.Enabled) ? TJAPlayer3.DTX.dbDTXVPlaySpeed : TJAPlayer3.DTX.db再生速度;
                return (int)(nDuration / _db再生速度);
            }

            #region [ IComparable 実装 ]
            //-----------------

            private static readonly byte[] n優先度 = new byte[] {
                5, 5, 3, 7, 5, 5, 5, 5, 3, 5, 5, 5, 5, 5, 5, 5, //0x00
		        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x10
		        7, 7, 7, 7, 7, 7, 7, 7, 5, 5, 5, 5, 5, 5, 5, 5, //0x20
		        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x30
		        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x40
		        9, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x50
		        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x60
		        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x70
		        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x80
		        5, 5, 5, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 9, 9, 9, //0x90
		        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0xA0
		        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0xB0
		        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0xC0
		        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 3, 4, 4, //0xD0
		        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0xE0
		        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0xF0
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, //0x100
		    };

            public int CompareTo(CDTX.CChip other)
            {
                // まずは位置で比較。

                //BGMチップだけ発声位置
                //if( this.nチャンネル番号 == 0x01 || this.nチャンネル番号 == 0x02 )
                //{
                //    if( this.n発声位置 < other.n発声位置 )
                //        return -1;

                //    if( this.n発声位置 > other.n発声位置 )
                //        return 1;
                //}

                //if( this.n発声位置 < other.n発声位置 )
                //    return -1;

                //if( this.n発声位置 > other.n発声位置 )
                //    return 1;

                //譜面解析メソッドV4では発声時刻msで比較する。
                var n発声時刻msCompareToResult = 0;
                n発声時刻msCompareToResult = this.n発声時刻ms.CompareTo(other.n発声時刻ms);
                if (n発声時刻msCompareToResult != 0)
                {
                    return n発声時刻msCompareToResult;
                }

                n発声時刻msCompareToResult = this.db発声時刻ms.CompareTo(other.db発声時刻ms);
                if (n発声時刻msCompareToResult != 0)
                {
                    return n発声時刻msCompareToResult;
                }

                // 位置が同じなら優先度で比較。
                return n優先度[this.nチャンネル番号].CompareTo(n優先度[other.nチャンネル番号]);
            }
            //-----------------
            #endregion
            /// <summary>
            /// shallow copyです。
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                return MemberwiseClone();
            }
        }
        public class CWAV : IDisposable
        {
            public bool bBGMとして使う;
            public List<int> listこのWAVを使用するチャンネル番号の集合 = new List<int>(16);
            public int nチップサイズ = 100;
            public int n位置;
            public long[] n一時停止時刻 = new long[TJAPlayer3.ConfigIni.nPoliphonicSounds];    // 4
            public int SongVol = CSound.DefaultSongVol;
            public LoudnessMetadata? SongLoudnessMetadata = null;
            public int n現在再生中のサウンド番号;
            public long[] n再生開始時刻 = new long[TJAPlayer3.ConfigIni.nPoliphonicSounds];    // 4
            public int n内部番号;
            public int n表記上の番号;
            public CSound[] rSound = new CSound[TJAPlayer3.ConfigIni.nPoliphonicSounds];     // 4
            public string strコメント文 = "";
            public string strファイル名 = "";
            public bool bBGMとして使わない
            {
                get
                {
                    return !this.bBGMとして使う;
                }
                set
                {
                    this.bBGMとして使う = !value;
                }
            }
            public bool bIsBassSound = false;
            public bool bIsGuitarSound = false;
            public bool bIsDrumsSound = false;
            public bool bIsSESound = false;
            public bool bIsBGMSound = false;

            public override string ToString()
            {
                var sb = new StringBuilder(128);

                if (this.n表記上の番号 == this.n内部番号)
                {
                    sb.Append(string.Format("CWAV{0}: ", CDTX.tZZ(this.n表記上の番号)));
                }
                else
                {
                    sb.Append(string.Format("CWAV{0}(内部{1}): ", CDTX.tZZ(this.n表記上の番号), this.n内部番号));
                }
                sb.Append(
                    $"{nameof(SongVol)}:{this.SongVol}, {nameof(LoudnessMetadata.Integrated)}:{this.SongLoudnessMetadata?.Integrated}, {nameof(LoudnessMetadata.TruePeak)}:{this.SongLoudnessMetadata?.TruePeak}, 位置:{this.n位置}, サイズ:{this.nチップサイズ}, BGM:{(this.bBGMとして使う ? 'Y' : 'N')}, File:{this.strファイル名}, Comment:{this.strコメント文}");

                return sb.ToString();
            }

            #region [ Dispose-Finalize パターン実装 ]
            //-----------------
            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }
            public void Dispose(bool bManagedリソースの解放も行う)
            {
                if (this.bDisposed済み)
                    return;

                if (bManagedリソースの解放も行う)
                {
                    for (int i = 0; i < TJAPlayer3.ConfigIni.nPoliphonicSounds; i++) // 4
                    {
                        if (this.rSound[i] != null)
                            TJAPlayer3.SoundManager.tDisposeSound(this.rSound[i]);
                        this.rSound[i] = null;

                        if ((i == 0) && TJAPlayer3.ConfigIni.bLog作成解放ログ出力)
                            Trace.TraceInformation("サウンドを解放しました。({0})({1})", this.strコメント文, this.strファイル名);
                    }
                }

                this.bDisposed済み = true;
            }
            ~CWAV()
            {
                this.Dispose(false);
            }
            //-----------------
            #endregion

            #region [ private ]
            //-----------------
            private bool bDisposed済み;
            //-----------------
            #endregion
        }

	    [Serializable]
        public class DanSongs
        {
            [NonSerialized]
            public CTexture TitleTex;
            [NonSerialized]
            public CTexture SubTitleTex;
            public string Title;
            public string SubTitle;
            public string FileName;
            public string Genre;
            public int ScoreInit;
            public int ScoreDiff;
            public int Level;
            public int Difficulty;
            public static int Number = 0;
            public bool bTitleShow;
            public Dan_C[] Dan_C = new Dan_C[CExamInfo.cMaxExam];

            [NonSerialized]
            public CWAV Wave;

            public DanSongs()
            {
                Number++;
            }
        }
        
        public struct STLYRIC
        {
            public long Time;
            public SKBitmap TextTex;
            public string Text;
            public int index;
        }


        // 構造体

        public struct STLANEINT
        {
            public int HH;
            public int SD;
            public int BD;
            public int HT;
            public int LT;
            public int CY;
            public int FT;
            public int HHO;
            public int RD;
            public int LC;
            public int LP;
            public int LBD;

            public int Drums
            {
                get
                {
                    return this.HH + this.SD + this.BD + this.HT + this.LT + this.CY + this.FT + this.HHO + this.RD + this.LC + this.LP + this.LBD;
                }
            }
            public int Guitar;
            public int Bass;
            public int Taiko_Red;
            public int Taiko_Blue;

            public int this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return this.HH;

                        case 1:
                            return this.SD;

                        case 2:
                            return this.BD;

                        case 3:
                            return this.HT;

                        case 4:
                            return this.LT;

                        case 5:
                            return this.CY;

                        case 6:
                            return this.FT;

                        case 7:
                            return this.HHO;

                        case 8:
                            return this.RD;

                        case 9:
                            return this.LC;

                        case 10:
                            return this.LP;

                        case 11:
                            return this.LBD;

                        case 12:
                            return this.Guitar;

                        case 13:
                            return this.Bass;

                        case 14:
                            return this.Taiko_Red;

                        case 15:
                            return this.Taiko_Blue;
                    }
                    throw new IndexOutOfRangeException();
                }
                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    switch (index)
                    {
                        case 0:
                            this.HH = value;
                            return;

                        case 1:
                            this.SD = value;
                            return;

                        case 2:
                            this.BD = value;
                            return;

                        case 3:
                            this.HT = value;
                            return;

                        case 4:
                            this.LT = value;
                            return;

                        case 5:
                            this.CY = value;
                            return;

                        case 6:
                            this.FT = value;
                            return;

                        case 7:
                            this.HHO = value;
                            return;

                        case 8:
                            this.RD = value;
                            return;

                        case 9:
                            this.LC = value;
                            return;

                        case 10:
                            this.LP = value;
                            return;

                        case 11:
                            this.LBD = value;
                            return;

                        case 12:
                            this.Guitar = value;
                            return;

                        case 13:
                            this.Bass = value;
                            return;

                        case 14:
                            this.Taiko_Red = value;
                            return;

                        case 15:
                            this.Taiko_Blue = value;
                            return;
                    }
                    throw new IndexOutOfRangeException();
                }
            }
        }
        public struct STRESULT
        {
            public string SS;
            public string S;
            public string A;
            public string B;
            public string C;
            public string D;
            public string E;

            public string this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return this.SS;

                        case 1:
                            return this.S;

                        case 2:
                            return this.A;

                        case 3:
                            return this.B;

                        case 4:
                            return this.C;

                        case 5:
                            return this.D;

                        case 6:
                            return this.E;
                    }
                    throw new IndexOutOfRangeException();
                }
                set
                {
                    switch (index)
                    {
                        case 0:
                            this.SS = value;
                            return;

                        case 1:
                            this.S = value;
                            return;

                        case 2:
                            this.A = value;
                            return;

                        case 3:
                            this.B = value;
                            return;

                        case 4:
                            this.C = value;
                            return;

                        case 5:
                            this.D = value;
                            return;

                        case 6:
                            this.E = value;
                            return;
                    }
                    throw new IndexOutOfRangeException();
                }
            }
        }
        public struct STチップがある
        {
            public bool Drums;
            public bool Guitar;
            public bool Bass;

            public bool HHOpen;
            public bool Ride;
            public bool LeftCymbal;
            public bool OpenGuitar;
            public bool OpenBass;

            public bool Branch;

            public bool this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return this.Drums;

                        case 1:
                            return this.Guitar;

                        case 2:
                            return this.Bass;

                        case 3:
                            return this.HHOpen;

                        case 4:
                            return this.Ride;

                        case 5:
                            return this.LeftCymbal;

                        case 6:
                            return this.OpenGuitar;

                        case 7:
                            return this.OpenBass;

                        case 8:
                            return this.Branch;
                    }
                    throw new IndexOutOfRangeException();
                }
                set
                {
                    switch (index)
                    {
                        case 0:
                            this.Drums = value;
                            return;

                        case 1:
                            this.Guitar = value;
                            return;

                        case 2:
                            this.Bass = value;
                            return;

                        case 3:
                            this.HHOpen = value;
                            return;

                        case 4:
                            this.Ride = value;
                            return;

                        case 5:
                            this.LeftCymbal = value;
                            return;

                        case 6:
                            this.OpenGuitar = value;
                            return;

                        case 7:
                            this.OpenBass = value;
                            return;

                        case 8:
                            this.Branch = value;
                            return;
                    }
                    throw new IndexOutOfRangeException();
                }
            }
        }
        public enum ECourse
        {
            eNormal,
            eExpert,
            eMaster
        }

        public enum ELevelIcon
        {
            eMinus,
            eNone,
            ePlus
        }

        public enum ESide
        {
            eNormal,
            eEx
        }
        public class CLine
        {
            public int n小節番号;
            public int n文字数;
            public double db発声時刻;
            public double dbBMS時刻;
            public ECourse nコース = ECourse.eNormal;
            public int nタイプ;
        }

        // プロパティ


        public class CBranchStartInfo
        {
            public int nMeasureCount;
            public double dbTime;
            public double dbBPM;
            public double dbSCROLL;
            public double dbSCROLLY;
            public double dbBMScollTime;
            public double db移動待機時刻;
            public double db出現時刻;
            public double db再生速度;
            public float fMeasure_s;
            public float fMeasure_m;
        }

        /// <summary>
        /// 分岐開始時の情報を記録するためのあれ 2020.04.21
        /// </summary>
        public CBranchStartInfo cBranchStart = new CBranchStartInfo();

        public int nBGMAdjust
        {
            get;
            private set;
        }
        public bool b分岐を一回でも開始した = false; //2020.04.22 akasoko26 分岐譜面のみ値を代入するように。

        public int nPlayerSide; //2017.08.14 kairera0467 引数で指定する
        public bool bSession譜面を読み込む;
        public string ARTIST;
        public string BACKGROUND;
        public string BACKGROUND_GR;
        public double BASEBPM;
        public double BPM;
        public double MinBPM;
        public double MaxBPM;
        public STチップがある bチップがある;
        public string COMMENT;
        public double db再生速度;
        public string GENRE;
        public string MAKER;
        public string[] NOTESDESIGNER = new string[(int)Difficulty.Total] { "", "", "", "", "", "", "" };
        public bool EXPLICIT;
        public string SELECTBG;
        public bool HIDDENLEVEL;
        public STDGBVALUE<int> LEVEL;
        public bool bLyrics;
        public int[] LEVELtaiko = new int[(int)Difficulty.Total] { -1, -1, -1, -1, -1, -1, -1 };
        public ELevelIcon[] LEVELtaikoIcon = new ELevelIcon[(int)Difficulty.Total] { ELevelIcon.eNone, ELevelIcon.eNone, ELevelIcon.eNone, ELevelIcon.eNone, ELevelIcon.eNone, ELevelIcon.eNone, ELevelIcon.eNone };
        public ESide SIDE;
        public CSongUniqueID uniqueID;
        
        // Tower lifes
        public int LIFE;

        public string TOWERTYPE;

        public int DANTICK = 0;
        public Color DANTICKCOLOR = Color.White;
        
		public Dictionary<int, CVideoDecoder> listVD;
        public Dictionary<int, CBPM> listBPM;
        public List<CChip> listChip;
        public List<CChip>[] listChip_Branch;
        public Dictionary<int, CWAV> listWAV;
        public Dictionary<int, CSCROLL> listSCROLL;
        public Dictionary<int, CSCROLL> listSCROLL_Normal;
        public Dictionary<int, CSCROLL> listSCROLL_Expert;
        public Dictionary<int, CSCROLL> listSCROLL_Master;
        public Dictionary<int, CJPOSSCROLL> listJPOSSCROLL;
        public List<DanSongs> List_DanSongs;
        private EScrollMode eScrollMode;



        private double[] dbNowSCROLL_Normal;
        private double[] dbNowSCROLL_Expert;
        private double[] dbNowSCROLL_Master;

        private int nNextSongOffset;

        public Dictionary<int, CDELAY> listDELAY;
        public Dictionary<int, CBRANCH> listBRANCH;
        public STLANEINT n可視チップ数;
        public const int n最大音数 = 4;
        public const int n小節の解像度 = 384;
        public string PANEL;
        public string PATH_WAV;
        public string PREIMAGE;
        public string PREVIEW;
        public string strハッシュofDTXファイル;
        public string strファイル名;
        public string strファイル名の絶対パス;
        public string strフォルダ名;
        public CLocalizationData SUBTITLE = new CLocalizationData();
        public CLocalizationData TITLE = new CLocalizationData();
        public double dbDTXVPlaySpeed;
        public double dbScrollSpeed;
        public int nデモBGMオフセット;

        private int n現在の小節数 = 1;

        private int nNowRoll = 0;
        private int nNowRollCount = 0;
        private int[] nNowRollCountBranch = new int[3];

        private int[] n連打チップ_temp = new int[3];
        public int nOFFSET = 0;
        private bool bOFFSETの値がマイナスである = false;
        private int nMOVIEOFFSET = 0;
        private bool bMOVIEOFFSETの値がマイナスである = false;
        private double dbNowBPM = 120.0;
        private int nDELAY = 0;

        public bool[] bHasBranch = new bool[(int)Difficulty.Total] { false, false, false, false, false, false, false };

        public bool[] bHasBranchDan = new bool[1] { false };

        //分岐関連
        private ECourse n現在のコース = ECourse.eNormal;

        private bool b最初の分岐である;
        public int[] nノーツ数 = new int[4]; //3:共通
        
        public int[] nDan_NotesCount = new int[1];
        public int[] nDan_BalloonCount = new int[1];
        // public int[] nDan_BallonCount = new int[1];

        public int[] nノーツ数_Branch = new int[4]; //
        public CChip[] pDan_LastChip;
        public int[] n風船数 = new int[4]; //0～2:各コース 3:共通

        private List<CLine> listLine;
        private int nLineCountTemp; //分岐開始時の小節数を記録。
        private ECourse nLineCountCourseTemp = ECourse.eNormal; //現在カウント中のコースを記録。

        public int n参照中の難易度 = 3;
        public int nScoreModeTmp = 99; //2017.01.28 DD
        public int[,] nScoreInit = new int[2, (int)Difficulty.Total]; //[ x, y ] x=通常or真打 y=コース
        public int[] nScoreDiff = new int[(int)Difficulty.Total]; //[y]
        public bool[,] b配点が指定されている = new bool[3, (int)Difficulty.Total]; //2017.06.04 kairera0467 [ x, y ] x=通常(Init)or真打orDiff y=コース

        private double dbBarLength;
        public float fNow_Measure_s = 4.0f;
        public float fNow_Measure_m = 4.0f;
        public double dbNowTime = 0.0;
        public double dbNowBMScollTime = 0.0;
        public double dbNowScroll = 1.0;
        public double dbNowScrollY = 0.0; //2016.08.13 kairera0467 複素数スクロール
        public double dbLastTime = 0.0; //直前の小節の開始時間
        public double dbLastBMScrollTime = 0.0;

        public int[] bBARLINECUE = new int[2]; //命令を入れた次の小節の操作を実現するためのフラグ。0 = mainflag, 1 = cuetype
        public bool b小節線を挿入している = false;

        //Normal Regular Masterにしたいけどここは我慢。
        private List<int> listBalloon_Normal;
        private List<int> listBalloon_Expert;
        private List<int> listBalloon_Master;
        private List<int> listBalloon; //旧構文用

        public List<SKBitmap> listLyric; //歌詞を格納していくリスト。スペル忘れた(ぉい
        public List<STLYRIC> listLyric2;

        //public Dictionary<double, CChip> kusudaMAP = new Dictionary<double, CChip>();

        public bool usingLyricsFile; //If lyric file is used (VTT/LRC), ignore #LYRIC tags & do not parse other lyric file tags

        private int listBalloon_Normal_数値管理;
        private int listBalloon_Expert_数値管理;
        private int listBalloon_Master_数値管理;

        public string scenePreset;

        public bool[] b譜面が存在する = new bool[(int)Difficulty.Total];

        private string[] dlmtSpace = { " " };
        private string[] dlmtEnter = { "\n" };
        private string[] dlmtCOURSE = { "COURSE:" };

        private int nスクロール方向 = 0;
        //2015.09.18 kairera0467
        //バタフライスライドみたいなアレをやりたいがために実装。
        //次郎2みたいな複素数とかは意味不明なので、方向を指定してスクロールさせることにした。
        //0:通常
        //1:上
        //2:下
        //3:右上
        //4:右下
        //5:左
        //6:左上
        //7:左下

        public string strBGIMAGE_PATH;
        public string strBGVIDEO_PATH;

        public double db出現時刻;
        public double db移動待機時刻;

        public string strBGM_PATH;
        public int SongVol;
        public LoudnessMetadata? SongLoudnessMetadata;

        public bool bHIDDENBRANCH; //2016.04.01 kairera0467 選曲画面上、譜面分岐開始前まで譜面分岐の表示を隠す
        public bool bGOGOTIME; //2018.03.11 kairera0467

        public bool[] IsBranchBarDraw = new bool[4]; // 仕様変更により、黄色lineの表示法を変更.2020.04.21.akasoko26
        public bool IsEndedBranching = true; // BRANCHENDが呼び出されたかどうか
        public Dan_C[] Dan_C;

        public bool IsEnabledFixSENote;
        public int FixSENote;
        public GaugeIncreaseMode GaugeIncreaseMode;

        #region [ EXTENDED VARiABLES ]
        public Dictionary<string, CSongObject> listObj;
        public Dictionary<string, CTexture> listTextures;
        public Dictionary<string, CTexture> listOriginalTextures;
        #endregion



#if TEST_NOTEOFFMODE
		public STLANEVALUE<bool> b演奏で直前の音を消音する;
//		public bool bHH演奏で直前のHHを消音する;
//		public bool bGUITAR演奏で直前のGUITARを消音する;
//		public bool bBASS演奏で直前のBASSを消音する;
#endif
        // コンストラクタ

        public CDTX()
        {
            this.nPlayerSide = 0;
            this.TITLE.SetString("default", "");
            this.SUBTITLE.SetString("default", "");
            this.ARTIST = "";
            this.COMMENT = "";
            this.SIDE = ESide.eEx;
            this.PANEL = "";
            this.GENRE = "";
            this.MAKER = "";
            this.EXPLICIT = false;
            this.SELECTBG = "";
            this.bLyrics = false;
            this.usingLyricsFile = false;
            this.PREVIEW = "";
            this.PREIMAGE = "";
            this.BACKGROUND = "";
            this.BACKGROUND_GR = "";
            this.PATH_WAV = "";
            this.BPM = 120.0;
            this.nOFFSET = TJAPlayer3.ConfigIni.nGlobalOffsetMs; // When OFFSET isn't called (typically in Dans), it should default to the game's Global Offset to avoid desync.
            this.bOFFSETの値がマイナスである = nOFFSET < 0;
            STDGBVALUE<int> stdgbvalue = new STDGBVALUE<int>();
            stdgbvalue.Drums = 0;
            stdgbvalue.Guitar = 0;
            stdgbvalue.Bass = 0;
            this.LEVEL = stdgbvalue;
            this.bHIDDENBRANCH = false;
            this.db再生速度 = 1.0;
            this.bチップがある = new STチップがある();
            this.bチップがある.Drums = false;
            this.bチップがある.Guitar = false;
            this.bチップがある.Bass = false;
            this.bチップがある.HHOpen = false;
            this.bチップがある.Ride = false;
            this.bチップがある.LeftCymbal = false;
            this.bチップがある.OpenGuitar = false;
            this.bチップがある.OpenBass = false;
            this.strファイル名 = "";
            this.strフォルダ名 = "";
            this.strファイル名の絶対パス = "";
            this.n無限管理WAV = new int[36 * 36];
            this.n無限管理BPM = new int[36 * 36];
            this.n無限管理PAN = new int[36 * 36];
            this.n無限管理SIZE = new int[36 * 36];
            this.listBalloon_Normal_数値管理 = 0;
            this.listBalloon_Expert_数値管理 = 0;
            this.listBalloon_Master_数値管理 = 0;
            this.nRESULTIMAGE用優先順位 = new int[7];
            this.nRESULTMOVIE用優先順位 = new int[7];
            this.nRESULTSOUND用優先順位 = new int[7];

            #region [ 2011.1.1 yyagi GDA->DTX変換テーブル リファクタ後 ]
            STGDAPARAM[] stgdaparamArray = new STGDAPARAM[] {		// GDA->DTX conversion table
				new STGDAPARAM("TC", 0x03), new STGDAPARAM("BL", 0x02), new STGDAPARAM("GS", 0x29),
                new STGDAPARAM("DS", 0x30), new STGDAPARAM("FI", 0x53), new STGDAPARAM("HH", 0x11),
                new STGDAPARAM("SD", 0x12), new STGDAPARAM("BD", 0x13), new STGDAPARAM("HT", 0x14),
                new STGDAPARAM("LT", 0x15), new STGDAPARAM("CY", 0x16), new STGDAPARAM("G1", 0x21),
                new STGDAPARAM("G2", 0x22), new STGDAPARAM("G3", 0x23), new STGDAPARAM("G4", 0x24),
                new STGDAPARAM("G5", 0x25), new STGDAPARAM("G6", 0x26), new STGDAPARAM("G7", 0x27),
                new STGDAPARAM("GW", 0x28), new STGDAPARAM("01", 0x61), new STGDAPARAM("02", 0x62),
                new STGDAPARAM("03", 0x63), new STGDAPARAM("04", 0x64), new STGDAPARAM("05", 0x65),
                new STGDAPARAM("06", 0x66), new STGDAPARAM("07", 0x67), new STGDAPARAM("08", 0x68),
                new STGDAPARAM("09", 0x69), new STGDAPARAM("0A", 0x70), new STGDAPARAM("0B", 0x71),
                new STGDAPARAM("0C", 0x72), new STGDAPARAM("0D", 0x73), new STGDAPARAM("0E", 0x74),
                new STGDAPARAM("0F", 0x75), new STGDAPARAM("10", 0x76), new STGDAPARAM("11", 0x77),
                new STGDAPARAM("12", 0x78), new STGDAPARAM("13", 0x79), new STGDAPARAM("14", 0x80),
                new STGDAPARAM("15", 0x81), new STGDAPARAM("16", 0x82), new STGDAPARAM("17", 0x83),
                new STGDAPARAM("18", 0x84), new STGDAPARAM("19", 0x85), new STGDAPARAM("1A", 0x86),
                new STGDAPARAM("1B", 0x87), new STGDAPARAM("1C", 0x88), new STGDAPARAM("1D", 0x89),
                new STGDAPARAM("1E", 0x90), new STGDAPARAM("1F", 0x91), new STGDAPARAM("20", 0x92),
                new STGDAPARAM("B1", 0xA1), new STGDAPARAM("B2", 0xA2), new STGDAPARAM("B3", 0xA3),
                new STGDAPARAM("B4", 0xA4), new STGDAPARAM("B5", 0xA5), new STGDAPARAM("B6", 0xA6),
                new STGDAPARAM("B7", 0xA7), new STGDAPARAM("BW", 0xA8), new STGDAPARAM("G0", 0x20),
                new STGDAPARAM("B0", 0xA0)
            };
            this.stGDAParam = stgdaparamArray;
            #endregion
            this.nBGMAdjust = 0;
            this.nPolyphonicSounds = TJAPlayer3.ConfigIni.nPoliphonicSounds;
            this.dbDTXVPlaySpeed = 1.0f;

            //this.nScoreModeTmp = 1;
            for (int y = 0; y < (int)Difficulty.Total; y++)
            {
                this.nScoreInit[0, y] = 300;
                this.nScoreInit[1, y] = 1000;
                this.nScoreDiff[y] = 120;
                this.b配点が指定されている[0, y] = false;
                this.b配点が指定されている[1, y] = false;
                this.b配点が指定されている[2, y] = false;
            }

            this.dbBarLength = 1.0;

            this.b最初の分岐である = true;

            this.SongVol = CSound.DefaultSongVol;
            this.SongLoudnessMetadata = null;

            GaugeIncreaseMode = GaugeIncreaseMode.Normal;

#if TEST_NOTEOFFMODE
			this.bHH演奏で直前のHHを消音する = true;
			this.bGUITAR演奏で直前のGUITARを消音する = true;
			this.bBASS演奏で直前のBASSを消音する = true;
#endif

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; // Change default culture to invariant, fixes (Purota)
            Dan_C = new Dan_C[CExamInfo.cMaxExam];
            pDan_LastChip = new CChip[1];
            DanSongs.Number = 0;

        }
        /*
        public CDTX(string str全入力文字列, int difficulty)
            : this()
        {
            this.On活性化();
            this.t入力_全入力文字列から(str全入力文字列, difficulty);
        }
        public CDTX(string strファイル名, bool bヘッダのみ, int difficulty)
            : this()
        {
            this.On活性化();
            this.t入力(strファイル名, bヘッダのみ, difficulty);
        }
        public CDTX(string str全入力文字列, double db再生速度, int nBGMAdjust, int difficulty)
            : this()
        {
            this.On活性化();
            this.t入力_全入力文字列から(str全入力文字列, str全入力文字列, db再生速度, nBGMAdjust, difficulty);
        }
        */
        public CDTX(string strファイル名, bool bヘッダのみ, double db再生速度, int nBGMAdjust, int difficulty)
            : this()
        {
            this.Activate();
            this.t入力(strファイル名, bヘッダのみ, db再生速度, nBGMAdjust, 0, 0, false, difficulty);
        }
        /*
        public CDTX(string strファイル名, bool bヘッダのみ, double db再生速度, int nBGMAdjust, int nReadVersion, int difficulty)
            : this()
        {
            this.On活性化();
            this.t入力(strファイル名, bヘッダのみ, db再生速度, nBGMAdjust, nReadVersion, 0, false, difficulty);
        }
        */
        public CDTX(string strファイル名, bool bヘッダのみ, double db再生速度, int nBGMAdjust, int nReadVersion, int nPlayerSide, bool bSession, int difficulty)
            : this()
        {
            this.Activate();
            this.t入力(strファイル名, bヘッダのみ, db再生速度, nBGMAdjust, nReadVersion, nPlayerSide, bSession, difficulty);
        }


        // メソッド

        public void tAVIの読み込み()
        {
            if (!this.bヘッダのみ)
            {
                if (this.listVD != null)
                {
                    foreach (CVideoDecoder cvd in this.listVD.Values)
                    {
                        cvd.InitRead();
                        cvd.dbPlaySpeed = TJAPlayer3.ConfigIni.SongPlaybackSpeed;
                    }
                }
            }
        }

        public void tWave再生位置自動補正()
        {
            foreach (CWAV cwav in this.listWAV.Values)
            {
                this.tWave再生位置自動補正(cwav);
            }
        }
        public void tWave再生位置自動補正(CWAV wc)
        {
            if (wc.rSound[0] != null && wc.rSound[0].TotalPlayTime >= 5000)
            {
                for (int i = 0; i < nPolyphonicSounds; i++)
                {
                    if ((wc.rSound[i] != null) && (wc.rSound[i].IsPlaying))
                    {
                        long nCurrentTime = SoundManager.PlayTimer.SystemTimeMs;
                        if (nCurrentTime > wc.n再生開始時刻[i])
                        {
                            long nAbsTimeFromStartPlaying = nCurrentTime - wc.n再生開始時刻[i];
                            //Trace.TraceInformation( "再生位置自動補正: {0}, seek先={1}ms, 全音長={2}ms",
                            //    Path.GetFileName( wc.rSound[ 0 ].strファイル名 ),
                            //    nAbsTimeFromStartPlaying,
                            //    wc.rSound[ 0 ].n総演奏時間ms
                            //);
                            // wc.rSound[ i ].t再生位置を変更する( wc.rSound[ i ].t時刻から位置を返す( nAbsTimeFromStartPlaying ) );
                            // WASAPI/ASIO用↓
                            if (!TJAPlayer3.stage演奏ドラム画面.bPAUSE)
                            {
                                if (wc.rSound[i].IsPaused) wc.rSound[i].Resume(nAbsTimeFromStartPlaying);
                                else wc.rSound[i].tSetPositonToBegin(nAbsTimeFromStartPlaying);
                            }
                            else
                            {
                                wc.rSound[i].Pause();
                            }
                        }
                    }
                }
            }
        }
        public void tWavの再生停止(int nWaveの内部番号)
        {
            tWavの再生停止(nWaveの内部番号, false);
        }
        public void tWavの再生停止(int nWaveの内部番号, bool bミキサーからも削除する)
        {
            if (this.listWAV.TryGetValue(nWaveの内部番号, out CWAV cwav))
            {
                for (int i = 0; i < nPolyphonicSounds; i++)
                {
                    if (cwav.rSound[i] != null && cwav.rSound[i].IsPlaying)
                    {
                        if (bミキサーからも削除する)
                        {
                            cwav.rSound[i].tStopSoundAndRemoveSoundFromMixer();
                        }
                        else
                        {
                            cwav.rSound[i].Stop();
                        }
                    }
                }
            }
        }
        public void tWAVの読み込み(CWAV cwav)
        {
            string str = string.IsNullOrEmpty(this.PATH_WAV) ? this.strフォルダ名 : this.PATH_WAV;
            str = str + cwav.strファイル名;

            try
            {
                #region [ 同時発音数を、チャンネルによって変える ]

                int nPoly = nPolyphonicSounds;
                if (TJAPlayer3.SoundManager.GetCurrentSoundDeviceType() != "DirectSound") // DShowでの再生の場合はミキシング負荷が高くないため、
                {
                    // チップのライフタイム管理を行わない
                    if (cwav.bIsBassSound) nPoly = (nPolyphonicSounds >= 2) ? 2 : 1;
                    else if (cwav.bIsGuitarSound) nPoly = (nPolyphonicSounds >= 2) ? 2 : 1;
                    else if (cwav.bIsSESound) nPoly = 1;
                    else if (cwav.bIsBGMSound) nPoly = 1;
                }

                if (cwav.bIsBGMSound) nPoly = 1;

                #endregion

                for (int i = 0; i < nPoly; i++)
                {
                    try
                    {
                        cwav.rSound[i] = TJAPlayer3.SoundManager.tCreateSound(str, ESoundGroup.SongPlayback);

                        if (!TJAPlayer3.ConfigIni.bDynamicBassMixerManagement)
                        {
                            cwav.rSound[i].AddBassSoundFromMixer();
                        }

                        if (TJAPlayer3.ConfigIni.bLog作成解放ログ出力)
                        {
                            Trace.TraceInformation("サウンドを作成しました。({3})({0})({1})({2}bytes)", cwav.strコメント文, str,
                                cwav.rSound[0].SoundBufferSize, cwav.rSound[0].IsStreamPlay ? "Stream" : "OnMemory");
                        }
                    }
                    catch (Exception e)
                    {
                        cwav.rSound[i] = null;
                        Trace.TraceError("サウンドの作成に失敗しました。({0})({1})", cwav.strコメント文, str);
                        Trace.TraceError(e.ToString());
                    }
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError("サウンドの生成に失敗しました。({0})({1})", cwav.strコメント文, str);
                Trace.TraceError(exception.ToString());

                for (int j = 0; j < nPolyphonicSounds; j++)
                {
                    cwav.rSound[j] = null;
                }

                //continue;
            }
        }

        public static string tZZ(int n)
        {
            if (n < 0 || n >= 36 * 36)
                return "!!";    // オーバー／アンダーフロー。

            // n を36進数2桁の文字列にして返す。

            string str = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(new char[] { str[n / 36], str[n % 36] });
        }

        public static void tManageKusudama(CDTX[] dtxarr)
        {
            if (TJAPlayer3.ConfigIni.nPlayerCount == 1) return; 

			// Replace non-shared kusudamas by balloons
			#region [Sync check]
            /*
			for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                CDTX dtx = dtxarr[i];
                if (dtx == null) continue;
                foreach (KeyValuePair<double, CChip> kvp in dtx.kusudaMAP)
                {
                    for (int j = 0; j < TJAPlayer3.ConfigIni.nPlayerCount; j++)
                    {
                        if (j == i) continue;

                        CDTX dtxp = dtxarr[j];
                        if (dtxp == null) continue;
                        if (!dtxp.kusudaMAP.ContainsKey(kvp.Key))
                        {
                            kvp.Value.nチャンネル番号 = 0x17;
                            break;
                        }
                    }
                }
            }
            */
            #endregion

            // Stack balloon values to all remining (= existing) kusudamas to player 1
            #region [Accumulation]
            /*
            CDTX dtx1 = dtxarr[0];
            if (dtx1 == null) return;
            foreach (KeyValuePair<double, CChip> kvp in dtx1.kusudaMAP)
            {
                if (!NotesManager.IsKusudama(kvp.Value)) continue;
                for (int j = 1; j < TJAPlayer3.ConfigIni.nPlayerCount; j++)
                {
                    CDTX dtxp = dtxarr[j];
                    if (dtxp == null) continue;
                    if (dtxp.kusudaMAP.ContainsKey(kvp.Key)
                        && NotesManager.IsKusudama(dtxp.kusudaMAP[kvp.Key]))
                    {
                        kvp.Value.nBalloon += dtxp.kusudaMAP[kvp.Key].nBalloon;
                    }
                }
                // For score normalization
                
                for (int j = 1; j < TJAPlayer3.ConfigIni.nPlayerCount; j++)
                {
                    CDTX dtxp = dtxarr[j];
                    if (dtxp == null) continue;
                    if (dtxp.kusudaMAP.ContainsKey(kvp.Key)
                        && NotesManager.IsKusudama(dtxp.kusudaMAP[kvp.Key]))
                    {
                        dtxp.kusudaMAP[kvp.Key].nBalloon = kvp.Value.nBalloon;
                    }
                }
                
            }
            */
            #endregion
        }

		public void tApplyFunMods(int player = 0)
        {
            Random rnd = new System.Random();

            var eFun = TJAPlayer3.ConfigIni.nFunMods[TJAPlayer3.GetActualPlayer(player)];
            var chara = TJAPlayer3.Tx.Characters[TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data.Character];

            var bombFactor = Math.Max(1, Math.Min(100, chara.effect.BombFactor));
            var fuseRollFactor = Math.Max(0, Math.Min(100, chara.effect.FuseRollFactor));

            switch (eFun)
            {
                case EFunMods.MINESWEEPER:
                    foreach (var chip in this.listChip)
                    {
                        if (NotesManager.IsMissableNote(chip))
                        {
                            int n = rnd.Next(100);

                            if (n < bombFactor) chip.nチャンネル番号 = 0x1C;
                        }

                        if (NotesManager.IsBalloon(chip))
                        {
                            int n = rnd.Next(100);

                            if (n < fuseRollFactor) chip.nチャンネル番号 = 0x1D;
                        }

                    }
                    break;
                case EFunMods.AVALANCHE:
                    foreach (var chip in this.listChip)
                    {
                        int n = rnd.Next(100);


                        chip.dbSCROLL *= (n + 50) / (double)100;
                    }
                    break;
                case EFunMods.NONE:
                default:
                    break;
            }
        }

        public void tRandomizeTaikoChips(int player = 0)
        {
            //2016.02.11 kairera0467

            Random rnd = new System.Random();

            var eRandom = TJAPlayer3.ConfigIni.eRandom[TJAPlayer3.GetActualPlayer(player)];

            switch (eRandom)
            {
                case ERandomMode.MIRROR:
                    foreach (var chip in this.listChip)
                    {
                        switch (chip.nチャンネル番号)
                        {
                            case 0x11:
                                chip.nチャンネル番号 = 0x12;
                                break;
                            case 0x12:
                                chip.nチャンネル番号 = 0x11;
                                break;
                            case 0x13:
                                chip.nチャンネル番号 = 0x14;
                                chip.nSenote = 6;
                                break;
                            case 0x14:
                                chip.nチャンネル番号 = 0x13;
                                chip.nSenote = 5;
                                break;
                        }
                    }
                    break;
                case ERandomMode.RANDOM:
                    foreach (var chip in this.listChip)
                    {
                        int n = rnd.Next(100);

                        if (n >= 0 && n <= 20)
                        {
                            switch (chip.nチャンネル番号)
                            {
                                case 0x11:
                                    chip.nチャンネル番号 = 0x12;
                                    break;
                                case 0x12:
                                    chip.nチャンネル番号 = 0x11;
                                    break;
                                case 0x13:
                                    chip.nチャンネル番号 = 0x14;
                                    chip.nSenote = 6;
                                    break;
                                case 0x14:
                                    chip.nチャンネル番号 = 0x13;
                                    chip.nSenote = 5;
                                    break;
                            }
                        }
                    }
                    break;
                case ERandomMode.SUPERRANDOM:
                    foreach (var chip in this.listChip)
                    {
                        int n = rnd.Next(100);

                        if (n >= 0 && n <= 50)
                        {
                            switch (chip.nチャンネル番号)
                            {
                                case 0x11:
                                    chip.nチャンネル番号 = 0x12;
                                    break;
                                case 0x12:
                                    chip.nチャンネル番号 = 0x11;
                                    break;
                                case 0x13:
                                    chip.nチャンネル番号 = 0x14;
                                    chip.nSenote = 6;
                                    break;
                                case 0x14:
                                    chip.nチャンネル番号 = 0x13;
                                    chip.nSenote = 5;
                                    break;
                            }
                        }
                    }
                    break;
                case ERandomMode.MIRRORRANDOM:
                    foreach (var chip in this.listChip)
                    {
                        int n = rnd.Next(100);

                        if (n >= 0 && n <= 80)
                        {
                            switch (chip.nチャンネル番号)
                            {
                                case 0x11:
                                    chip.nチャンネル番号 = 0x12;
                                    break;
                                case 0x12:
                                    chip.nチャンネル番号 = 0x11;
                                    break;
                                case 0x13:
                                    chip.nチャンネル番号 = 0x14;
                                    chip.nSenote = 6;
                                    break;
                                case 0x14:
                                    chip.nチャンネル番号 = 0x13;
                                    chip.nSenote = 5;
                                    break;
                            }
                        }
                    }
                    break;
                case ERandomMode.OFF:
                default:
                    break;
            }

            if (TJAPlayer3.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(TJAPlayer3.GetActualPlayer(nPlayerSide))].effect.AllPurple)
            {
                foreach (var chip in this.listChip)
                {
                    switch (chip.nチャンネル番号)
                    {
                        case 0x13:
                        case 0x1A:
                            chip.nチャンネル番号 = 0x101;
                            break;
                        case 0x14:
                        case 0x1B:
                            chip.nチャンネル番号 = 0x101;
                            break;
                    }
                }
            }

            if (eRandom != ERandomMode.OFF)
            {
                #region[ list作成 ]
                //ひとまずチップだけのリストを作成しておく。
                List<CDTX.CChip> list音符のみのリスト;
                list音符のみのリスト = new List<CChip>();
                int nCount = 0;
                int dkdkCount = 0;

                foreach (CChip chip in this.listChip)
                {
                    if (chip.nチャンネル番号 >= 0x11 && chip.nチャンネル番号 < 0x18)
                    {
                        list音符のみのリスト.Add(chip);
                    }
                }
                #endregion

                this.tSenotes_Core_V2(list音符のみのリスト);

            }


        }

        #region [ チップの再生と停止 ]
        public void tチップの再生(CChip pChip, long n再生開始システム時刻ms)
        {
            if (TJAPlayer3.ConfigIni.b演奏速度が一倍速であるとき以外音声を再生しない && TJAPlayer3.ConfigIni.nSongSpeed != 20)
                return;

            if (pChip.n整数値_内部番号 >= 0)
            {
                if (this.listWAV.TryGetValue(pChip.n整数値_内部番号, out CWAV wc))
                {
                    int index = wc.n現在再生中のサウンド番号 = (wc.n現在再生中のサウンド番号 + 1) % nPolyphonicSounds;
                    if ((wc.rSound[0] != null) &&
                        (wc.rSound[0].IsStreamPlay || wc.rSound[index] == null))
                    {
                        index = wc.n現在再生中のサウンド番号 = 0;
                    }
                    CSound sound = wc.rSound[index];
                    if (sound != null)
                    {
                        sound.PlaySpeed = TJAPlayer3.ConfigIni.SongPlaybackSpeed;
                        // 再生速度によって、WASAPI/ASIOで使う使用mixerが決まるため、付随情報の設定(音量/PAN)は、再生速度の設定後に行う

                        // 2018-08-27 twopointzero - DON'T attempt to load (or queue scanning) loudness metadata here.
                        //                           This code is called right after loading the .tja, and that code
                        //                           will have just made such an attempt.
                        TJAPlayer3.SongGainController.Set(wc.SongVol, wc.SongLoudnessMetadata, sound);

                        sound.SoundPosition = wc.n位置;
                        sound.PlayStart();
                    }
                    wc.n再生開始時刻[wc.n現在再生中のサウンド番号] = n再生開始システム時刻ms;
                    this.tWave再生位置自動補正(wc);
                }
            }
        }
        public void t各自動再生音チップの再生時刻を変更する(int nBGMAdjustの増減値)
        {
            this.nBGMAdjust += nBGMAdjustの増減値;

            for (int i = 0; i < this.listChip.Count; i++)
            {
                int nChannelNumber = this.listChip[i].nチャンネル番号;
                if (((
                        (nChannelNumber == 1) ||
                        ((0x61 <= nChannelNumber) && (nChannelNumber <= 0x69))
                      ) ||
                        ((0x70 <= nChannelNumber) && (nChannelNumber <= 0x79))
                    ) ||
                    (((0x80 <= nChannelNumber) && (nChannelNumber <= 0x89)) || ((0x90 <= nChannelNumber) && (nChannelNumber <= 0x92)))
                  )
                {
                    this.listChip[i].n発声時刻ms += nBGMAdjustの増減値;
                }
            }
            foreach (CWAV cwav in this.listWAV.Values)
            {
                for (int j = 0; j < nPolyphonicSounds; j++)
                {
                    if ((cwav.rSound[j] != null) && cwav.rSound[j].IsPlaying)
                    {
                        cwav.n再生開始時刻[j] += nBGMAdjustの増減値;
                    }
                }
            }
        }
        public void t全チップの再生一時停止()
        {
            foreach (CWAV cwav in this.listWAV.Values)
            {
                for (int i = 0; i < nPolyphonicSounds; i++)
                {
                    if ((cwav.rSound[i] != null) && cwav.rSound[i].IsPlaying)
                    {
                        cwav.rSound[i].Pause();
                        cwav.n一時停止時刻[i] = SoundManager.PlayTimer.SystemTimeMs;
                    }
                }
            }
        }
        public void t全チップの再生再開()
        {
            foreach (CWAV cwav in this.listWAV.Values)
            {
                for (int i = 0; i < nPolyphonicSounds; i++)
                {
                    if ((cwav.rSound[i] != null) && cwav.rSound[i].IsPaused)
                    {
                        //long num1 = cwav.n一時停止時刻[ i ];
                        //long num2 = cwav.n再生開始時刻[ i ];
                        cwav.rSound[i].Resume(cwav.n一時停止時刻[i] - cwav.n再生開始時刻[i]);
                        cwav.n再生開始時刻[i] += SoundManager.PlayTimer.SystemTimeMs - cwav.n一時停止時刻[i];
                    }
                }
            }
        }
        public void t全チップの再生停止()
        {
            foreach (CWAV cwav in this.listWAV.Values)
            {
                this.tWavの再生停止(cwav.n内部番号);
            }
        }
        public void t全チップの再生停止とミキサーからの削除()
        {
            foreach (CWAV cwav in this.listWAV.Values)
            {
                this.tWavの再生停止(cwav.n内部番号, true);
            }
        }
        #endregion

        public void t入力(string strファイル名, bool bヘッダのみ, int difficulty)
        {
            this.t入力(strファイル名, bヘッダのみ, 1.0, 0, 0, 0, false, difficulty);
        }
        public void t入力(string strファイル名, bool bヘッダのみ, double db再生速度, int nBGMAdjust, int nReadVersion, int nPlayerSide, bool bSession, int difficulty)
        {
            this.bヘッダのみ = bヘッダのみ;
            this.strファイル名の絶対パス = Path.GetFullPath(strファイル名);
            this.strファイル名 = Path.GetFileName(this.strファイル名の絶対パス);
            this.strフォルダ名 = Path.GetDirectoryName(this.strファイル名の絶対パス) + Path.DirectorySeparatorChar;

            // Unique ID parsing/generation
            this.uniqueID = new CSongUniqueID(this.strフォルダ名 + @$"{Path.DirectorySeparatorChar}uniqueID.json");

            //if ( this.e種別 != E種別.SMF )
            {
                try
                {
                    this.nPlayerSide = nPlayerSide;
                    this.bSession譜面を読み込む = bSession;
                    if (nReadVersion != 0)
                    {
                        //DTX方式

                        //DateTime timeBeginLoad = DateTime.Now;
                        //TimeSpan span;
                        string[] files = Directory.GetFiles(this.strフォルダ名, "*.tja");

                        StreamReader reader = new StreamReader(strファイル名, Encoding.GetEncoding(TJAPlayer3.sEncType));
                        string str2 = reader.ReadToEnd();
                        reader.Close();

                        //StreamReader reader2 = new StreamReader( this.strフォルダ名 + "test.tja", Encoding.GetEncoding( "Shift_JIS" ) );
                        StreamReader reader2 = new StreamReader(files[0], Encoding.GetEncoding(TJAPlayer3.sEncType));
                        string str3 = reader2.ReadToEnd();
                        reader2.Close();

                        //span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
                        //Trace.TraceInformation( "DTXfileload時間:          {0}", span.ToString() );

                        this.t入力_全入力文字列から(str2, str3, db再生速度, nBGMAdjust, difficulty);
                    }
                    else
                    {
                        //次郎方式

                        //DateTime timeBeginLoad = DateTime.Now;
                        //TimeSpan span;

                        StreamReader reader = new StreamReader(strファイル名, Encoding.GetEncoding(TJAPlayer3.sEncType));
                        string str2 = reader.ReadToEnd();
                        reader.Close();

                        //StreamReader reader2 = new StreamReader( this.strフォルダ名 + "test.tja", Encoding.GetEncoding( "Shift_JIS" ) );
                        //StreamReader reader2 = new StreamReader( strファイル名, Encoding.GetEncoding( "Shift_JIS" ) );
                        //string str3 = reader2.ReadToEnd();
                        //reader2.Close();
                        string str3 = str2;

                        //span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
                        //Trace.TraceInformation( "DTXfileload時間:          {0}", span.ToString() );

                        this.t入力_全入力文字列から(str2, str3, db再生速度, nBGMAdjust, difficulty);
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show( "おや?エラーが出たようです。お兄様。" );
                    Trace.TraceError("おや?エラーが出たようです。お兄様。");
                    Trace.TraceError(ex.ToString());
                    Trace.TraceError("例外が発生しましたが処理を継続します。 (79ff8639-9b3c-477f-bc4a-f2eea9784860)");
                }
            }
        }
        public void t入力_全入力文字列から(string str全入力文字列, int difficulty)
        {
            this.t入力_全入力文字列から(str全入力文字列, str全入力文字列, 1.0, 0, difficulty);
        }
        public void t入力_全入力文字列から(string str全入力文字列, string str1, double db再生速度, int nBGMAdjust, int Difficulty)
        {
            //DateTime timeBeginLoad = DateTime.Now;
            //TimeSpan span;

            if (!string.IsNullOrEmpty(str全入力文字列))
            {
                #region [ 改行カット ]
                this.db再生速度 = db再生速度;
                str全入力文字列 = str全入力文字列.Replace(Environment.NewLine, "\n");
                str全入力文字列 = str全入力文字列.Replace('\t', ' ');
                str全入力文字列 = str全入力文字列 + "\n";
                #endregion
                //span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
                //Trace.TraceInformation( "改行カット時間:           {0}", span.ToString() );
                //timeBeginLoad = DateTime.Now;
                #region [ 初期化 ]
                for (int j = 0; j < 36 * 36; j++)
                {
                    this.n無限管理WAV[j] = -j;
                    this.n無限管理BPM[j] = -j;
                    this.n無限管理PAN[j] = -10000 - j;
                    this.n無限管理SIZE[j] = -j;
                }
                this.n内部番号WAV1to = 1;
                this.n内部番号BPM1to = 1;
                this.bstackIFからENDIFをスキップする = new Stack<bool>();
                this.bstackIFからENDIFをスキップする.Push(false);
                this.n現在の乱数 = 0;
                for (int k = 0; k < 7; k++)
                {
                    this.nRESULTIMAGE用優先順位[k] = 0;
                    this.nRESULTMOVIE用優先順位[k] = 0;
                    this.nRESULTSOUND用優先順位[k] = 0;
                }
                #endregion
                #region [ 入力/行解析 ]
                #region[初期化]
                this.dbNowScroll = 1.0;
                this.dbNowSCROLL_Normal = new double[] { 1.0, 0.0 };
                this.dbNowSCROLL_Expert = new double[] { 1.0, 0.0 };
                this.dbNowSCROLL_Master = new double[] { 1.0, 0.0 };
                this.n現在のコース = ECourse.eNormal;
                #endregion
                CharEnumerator ce = str全入力文字列.GetEnumerator();
                if (ce.MoveNext())
                {
                    this.n現在の行数 = 1;
                    do
                    {
                        if (!this.t入力_空白と改行をスキップする(ref ce))
                        {
                            break;
                        }
                        if (this.listChip.Count == 0)
                        {
                            //this.t入力(str1);
                            //this.t入力_V3( str1, 3 );
                            this.t入力_V4(str1, Difficulty);
                        }
                        if (ce.Current == '#')
                        {
                            if (ce.MoveNext())
                            {
                                StringBuilder builder = new StringBuilder(0x20);
                                if (this.t入力_コマンド文字列を抜き出す(ref ce, ref builder))
                                {
                                    StringBuilder builder2 = new StringBuilder(0x400);
                                    if (this.t入力_パラメータ文字列を抜き出す(ref ce, ref builder2))
                                    {
                                        StringBuilder builder3 = new StringBuilder(0x400);
                                        if (this.t入力_コメント文字列を抜き出す(ref ce, ref builder3))
                                        {
                                            this.t入力_行解析(ref builder, ref builder2, ref builder3);

                                            this.n現在の行数++;
                                            continue;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                        //this.t入力(str1);
                    }
                    while (this.t入力_コメントをスキップする(ref ce));

                    #endregion
                    //span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
                    //Trace.TraceInformation( "抜き出し時間:             {0}", span.ToString() );
                    //timeBeginLoad = DateTime.Now;
                    this.n無限管理WAV = null;
                    this.n無限管理BPM = null;
                    this.n無限管理PAN = null;
                    this.n無限管理SIZE = null;
                    //this.t入力_行解析ヘッダ( str1 );
                    if (!this.bヘッダのみ)
                    {
                        #region [ BPM/BMP初期化 ]
                        int ch;
                        CBPM cbpm = null;
                        foreach (CBPM cbpm2 in this.listBPM.Values)
                        {
                            if (cbpm2.n表記上の番号 == 0)
                            {
                                cbpm = cbpm2;
                                break;
                            }
                        }
                        if (cbpm == null)
                        {
                            cbpm = new CBPM();
                            cbpm.n内部番号 = this.n内部番号BPM1to++;
                            cbpm.n表記上の番号 = 0;
                            cbpm.dbBPM値 = 120.0;
                            this.listBPM.Add(cbpm.n内部番号, cbpm);
                            CChip chip = new CChip();
                            chip.n発声位置 = 0;
                            chip.nチャンネル番号 = 8;      // 拡張BPM
                            chip.n整数値 = 0;
                            chip.n整数値_内部番号 = cbpm.n内部番号;
                            this.listChip.Insert(0, chip);
                        }
                        else
                        {
                            CChip chip = new CChip();
                            chip.n発声位置 = 0;
                            chip.nチャンネル番号 = 8;      // 拡張BPM
                            chip.n整数値 = 0;
                            chip.n整数値_内部番号 = cbpm.n内部番号;
                            this.listChip.Insert(0, chip);
                        }
                        #endregion
                        //span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
                        //Trace.TraceInformation( "前準備完了時間:           {0}", span.ToString() );
                        //timeBeginLoad = DateTime.Now;
                        #region [ CWAV初期化 ]
                        foreach (CWAV cwav in this.listWAV.Values)
                        {
                            if (cwav.nチップサイズ < 0)
                            {
                                cwav.nチップサイズ = 100;
                            }
                            if (cwav.n位置 <= -10000)
                            {
                                cwav.n位置 = 0;
                            }
                        }
                        #endregion
                        //span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
                        //Trace.TraceInformation( "CWAV前準備時間:           {0}", span.ToString() );
                        //timeBeginLoad = DateTime.Now;
                        #region [ チップ倍率設定 ]						// #28145 2012.4.22 yyagi 二重ループを1重ループに変更して高速化)
                        //foreach ( CWAV cwav in this.listWAV.Values )
                        //{
                        //    foreach( CChip chip in this.listChip )
                        //    {
                        //        if( chip.n整数値_内部番号 == cwav.n内部番号 )
                        //        {
                        //            chip.dbチップサイズ倍率 = ( (double) cwav.nチップサイズ ) / 100.0;
                        //            if (chip.nチャンネル番号 == 0x01 )	// BGMだったら
                        //            {
                        //                cwav.bIsOnBGMLane = true;
                        //            }
                        //        }
                        //    }
                        //}
                        foreach (CChip chip in this.listChip)
                        {
                            if (this.listWAV.TryGetValue(chip.n整数値_内部番号, out CWAV cwav))
                            //foreach ( CWAV cwav in this.listWAV.Values )
                            {
                                //	if ( chip.n整数値_内部番号 == cwav.n内部番号 )
                                //	{
                                chip.dbチップサイズ倍率 = ((double)cwav.nチップサイズ) / 100.0;
                                //if ( chip.nチャンネル番号 == 0x01 )	// BGMだったら
                                //{
                                //	cwav.bIsOnBGMLane = true;
                                //}
                                //	}
                            }
                        }
                        #endregion
                        //span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
                        //Trace.TraceInformation( "CWAV全準備時間:           {0}", span.ToString() );
                        //timeBeginLoad = DateTime.Now;
                        #region [ 必要に応じて空打ち音を0小節に定義する ]
                        //for ( int m = 0xb1; m <= 0xbc; m++ )			// #28146 2012.4.21 yyagi; bb -> bc
                        //{
                        //    foreach ( CChip chip in this.listChip )
                        //    {
                        //        if ( chip.nチャンネル番号 == m )
                        //        {
                        //            CChip c = new CChip();
                        //            c.n発声位置 = 0;
                        //            c.nチャンネル番号 = chip.nチャンネル番号;
                        //            c.n整数値 = chip.n整数値;
                        //            c.n整数値_内部番号 = chip.n整数値_内部番号;
                        //            this.listChip.Insert( 0, c );
                        //            break;
                        //        }
                        //    }
                        //}
                        #endregion
                        //span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
                        //Trace.TraceInformation( "空打確認時間:             {0}", span.ToString() );
                        //timeBeginLoad = DateTime.Now;
                        #region [ 拍子_拍線の挿入 ]
                        if (this.listChip.Count > 0)
                        {
                            this.listChip.Sort();       // 高速化のためにはこれを削りたいが、listChipの最後がn発声位置の終端である必要があるので、
                                                        // 保守性確保を優先してここでのソートは残しておく
                                                        // なお、093時点では、このソートを削除しても動作するようにはしてある。
                                                        // (ここまでの一部チップ登録を、listChip.Add(c)から同Insert(0,c)に変更してある)
                                                        // これにより、数ms程度ながらここでのソートも高速化されている。

                            //double barlength = 1.0;
                            //int nEndOfSong = ( this.listChip[ this.listChip.Count - 1 ].n発声位置 + 384 ) - ( this.listChip[ this.listChip.Count - 1 ].n発声位置 % 384 );
                            //for ( int tick384 = 0; tick384 <= nEndOfSong; tick384 += 384 )	// 小節線の挿入　(後に出てくる拍子線とループをまとめようとするなら、forループの終了条件の微妙な違いに注意が必要)
                            //{
                            //    CChip chip = new CChip();
                            //    chip.n発声位置 = tick384;
                            //    chip.nチャンネル番号 = 0x50;	// 小節線
                            //    chip.n整数値 = 36 * 36 - 1;
                            //    chip.dbSCROLL = 1.0;
                            //    this.listChip.Add( chip );
                            //}
                            ////this.listChip.Sort();				// ここでのソートは不要。ただし最後にソートすること
                            //int nChipNo_BarLength = 0;
                            //int nChipNo_C1 = 0;

                            //this.listChip.Sort();
                        }
                        #endregion
                        //span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
                        //Trace.TraceInformation( "拍子_拍線挿入時間:       {0}", span.ToString() );
                        //timeBeginLoad = DateTime.Now;
                        #region [ C2 [拍線_小節線表示指定] の処理 ]		// #28145 2012.4.21 yyagi; 2重ループをほぼ1重にして高速化
                        bool bShowBeatBarLine = true;
                        for (int i = 0; i < this.listChip.Count; i++)
                        {
                            bool bChangedBeatBarStatus = false;
                            if ((this.listChip[i].nチャンネル番号 == 0xc2))
                            {
                                if (this.listChip[i].n整数値 == 1)             // BAR/BEAT LINE = ON
                                {
                                    bShowBeatBarLine = true;
                                    bChangedBeatBarStatus = true;
                                }
                                else if (this.listChip[i].n整数値 == 2)            // BAR/BEAT LINE = OFF
                                {
                                    bShowBeatBarLine = false;
                                    bChangedBeatBarStatus = true;
                                }
                            }
                            int startIndex = i;
                            if (bChangedBeatBarStatus)                          // C2チップの前に50/51チップが来ている可能性に配慮
                            {
                                while (startIndex > 0 && this.listChip[startIndex].n発声位置 == this.listChip[i].n発声位置)
                                {
                                    startIndex--;
                                }
                                startIndex++;   // 1つ小さく過ぎているので、戻す
                            }
                            for (int j = startIndex; j <= i; j++)
                            {
                                if (((this.listChip[j].nチャンネル番号 == 0x50) || (this.listChip[j].nチャンネル番号 == 0x51)) &&
                                    (this.listChip[j].n整数値 == (36 * 36 - 1)))
                                {
                                    this.listChip[j].b可視 = bShowBeatBarLine;
                                }
                            }
                        }
                        #endregion
                        //span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
                        //Trace.TraceInformation( "C2 [拍線_小節線表示指定]:  {0}", span.ToString() );
                        //timeBeginLoad = DateTime.Now;
                        this.n内部番号BRANCH1to = 0;
                        this.n内部番号JSCROLL1to = 0;
                        #region [ 発声時刻の計算 ]
                        double bpm = 120.0;
                        //double dbBarLength = 1.0;
                        int n発声位置 = 0;
                        int ms = 0;
                        int nBar = 0;
                        int nCount = 0;
                        this.nNowRollCount = 0;
                        for (int i = 0; i < this.nNowRollCountBranch.Length; i++)
                            this.nNowRollCountBranch[i] = 0;

                        List<STLYRIC> tmplistlyric = new List<STLYRIC>();
                        int BGM番号 = 0;

                        foreach (CChip chip in this.listChip)
                        {
                            if (chip.nチャンネル番号 == 0x02) { }
                            //else if( chip.nチャンネル番号 == 0x03 ){}
                            else if (chip.nチャンネル番号 == 0x01) { }
                            else if (chip.nチャンネル番号 == 0x08) { }
                            else if (chip.nチャンネル番号 >= 0x11 && chip.nチャンネル番号 <= 0x1F) { }
                            else if (chip.nチャンネル番号 == 0x50) { }
                            else if (chip.nチャンネル番号 == 0x51) { }
                            else if (chip.nチャンネル番号 == 0x54) { }
                            else if (chip.nチャンネル番号 == 0x08) { }
                            else if (chip.nチャンネル番号 == 0xF1) { }
                            else if (chip.nチャンネル番号 == 0xF2) { }
                            else if (chip.nチャンネル番号 == 0xFF) { }
                            else if (chip.nチャンネル番号 == 0xDD) { chip.n発声時刻ms = ms + ((int)(((625 * (chip.n発声位置 - n発声位置)) * this.dbBarLength) / bpm)); }
                            else if (chip.nチャンネル番号 == 0xDF) { chip.n発声時刻ms = ms + ((int)(((625 * (chip.n発声位置 - n発声位置)) * this.dbBarLength) / bpm)); }
                            else if (chip.nチャンネル番号 < 0x93)
                                chip.n発声時刻ms = ms + ((int)(((625 * (chip.n発声位置 - n発声位置)) * this.dbBarLength) / bpm));
                            else if ((chip.nチャンネル番号 > 0x9F && chip.nチャンネル番号 < 0xA0) || (chip.nチャンネル番号 >= 0xF0 && chip.nチャンネル番号 < 0xFE))
                                chip.n発声時刻ms = ms + ((int)(((625 * (chip.n発声位置 - n発声位置)) * this.dbBarLength) / bpm));
                            nBar = chip.n発声位置 / 384;
                            ch = chip.nチャンネル番号;

                            nCount++;
                            this.nNowRollCount++;
                            for (int i = 0; i < this.nNowRollCountBranch.Length; i++)
                                this.nNowRollCountBranch[i]++;

                            switch (ch)
                            {
                                case 0x01:
                                    {
                                        n発声位置 = chip.n発声位置;

                                        if (this.bOFFSETの値がマイナスである == false)
                                            chip.n発声時刻ms += this.nOFFSET;
                                        ms = chip.n発声時刻ms;

                                        #region[listlyric2の時間合わせ]
                                        for (int ind = 0; ind < listLyric2.Count; ind++)
                                        {
                                            if (listLyric2[ind].index == BGM番号)
                                            {
                                                STLYRIC lyrictmp = this.listLyric2[ind];

                                                lyrictmp.Time += chip.n発声時刻ms;

                                                tmplistlyric.Add(lyrictmp);
                                            }
                                        }


                                        BGM番号++;
                                        #endregion
                                        continue;
                                    }
                                case 0x02:  // BarLength
                                    {
                                        n発声位置 = chip.n発声位置;
                                        if (this.bOFFSETの値がマイナスである == false)
                                            chip.n発声時刻ms += this.nOFFSET;
                                        ms = chip.n発声時刻ms;
                                        dbBarLength = chip.db実数値;
                                        continue;
                                    }
                                case 0x03:  // BPM
                                    {
                                        n発声位置 = chip.n発声位置;
                                        if (this.bOFFSETの値がマイナスである == false)
                                            chip.n発声時刻ms += this.nOFFSET;
                                        ms = chip.n発声時刻ms;
                                        bpm = this.BASEBPM + chip.n整数値;
                                        this.dbNowBPM = bpm;
                                        continue;
                                    }
                                case 0x04:  // BGA (レイヤBGA1)
                                case 0x07:	// レイヤBGA2
                                    break;

                                case 0x15:
                                case 0x16:
                                case 0x17:
                                case 0x19:
                                case 0x1D:
                                case 0x20:
                                case 0x21:
                                    {
                                        if (this.bOFFSETの値がマイナスである)
                                        {
                                            chip.n発声時刻ms += this.nOFFSET;
                                            chip.nノーツ終了時刻ms += this.nOFFSET;
                                        }

                                        this.nNowRoll = this.nNowRollCount - 1;
                                        continue;
                                    }
                                case 0x18:
                                    {
                                        if (this.bOFFSETの値がマイナスである)
                                        {
                                            chip.n発声時刻ms += this.nOFFSET;
                                        }
                                        continue;
                                    }

                                case 0x55:
                                case 0x56:
                                case 0x57:
                                case 0x58:
                                case 0x59:
                                case 0x60:
                                    break;

                                case 0x50:
                                    {
                                        if (this.bOFFSETの値がマイナスである)
                                            chip.n発声時刻ms += this.nOFFSET;
                                        //chip.n発声時刻ms += this.nDELAY;
                                        //chip.dbBPM = this.dbNowBPM;
                                        //chip.dbSCROLL = this.dbNowSCROLL;

                                        if (this.n内部番号BRANCH1to + 1 > this.listBRANCH.Count)
                                            continue;

                                        if (this.listBRANCH[this.n内部番号BRANCH1to].n現在の小節 == nBar)
                                        {
                                            chip.bBranch = true;
                                            this.n内部番号BRANCH1to++;
                                        }

                                        //switch (this.n現在のコース)
                                        //{
                                        //    case 0:
                                        //        chip.dbSCROLL = this.dbNowSCROLL_Normal;
                                        //        break;
                                        //    case 1:
                                        //        chip.dbSCROLL = this.dbNowSCROLL_Expert;
                                        //        break;
                                        //    case 2:
                                        //        chip.dbSCROLL = this.dbNowSCROLL_Master;
                                        //        break;
                                        //}

                                        //if( this.bBarLine == true )
                                        //    chip.b可視 = true;
                                        //else
                                        //    chip.b可視 = false;

                                        //if( this.b次の小節が分岐である )
                                        //{
                                        //    chip.bBranch = true;
                                        //    this.b次の小節が分岐である = false;
                                        //}
                                        continue;
                                    }

                                case 0x05:  // Extended Object (非対応)
                                case 0x06:  // Missアニメ (非対応)
                                case 0x5A:  // 未定義
                                case 0x5b:  // 未定義
                                case 0x5c:  // 未定義
                                case 0x5d:  // 未定義
                                case 0x5e:  // 未定義
                                case 0x5f:  // 未定義
                                    {
                                        continue;
                                    }
                                case 0x08:  // 拡張BPM
                                    {
                                        n発声位置 = chip.n発声位置;
                                        if (this.bOFFSETの値がマイナスである == false)
                                            chip.n発声時刻ms += this.nOFFSET;
                                        ms = chip.n発声時刻ms;
                                        if (this.listBPM.TryGetValue(chip.n整数値_内部番号, out CBPM cBPM))
                                        {
                                            bpm = (cBPM.n表記上の番号 == 0 ? 0.0 : this.BASEBPM) + cBPM.dbBPM値;
                                            this.dbNowBPM = bpm;
                                        }
                                        continue;
                                    }
                                case 0x54:  // 動画再生
                                    {
                                        if (this.bOFFSETの値がマイナスである == false)
                                            chip.n発声時刻ms += this.nOFFSET;
                                        if (this.bMOVIEOFFSETの値がマイナスである == false)
                                            chip.n発声時刻ms += this.nMOVIEOFFSET;
                                        else
                                            chip.n発声時刻ms -= this.nMOVIEOFFSET;
                                        continue;
                                    }
                                case 0x97:
                                case 0x98:
                                case 0x99:
                                    {
                                        if (this.bOFFSETの値がマイナスである)
                                        {
                                            chip.n発声時刻ms += this.nOFFSET;
                                            chip.nノーツ終了時刻ms += this.nOFFSET;
                                        }

                                        //chip.dbBPM = this.dbNowBPM;
                                        //chip.dbSCROLL = this.dbNowSCROLL;
                                        this.nNowRoll = this.nNowRollCount - 1;

                                        //chip.nノーツ終了時刻ms = ms + ( (int) ( ( ( 0x271 * ( chip.nノーツ終了位置 - n発声位置 ) ) * dbBarLength ) / bpm ) );

                                        #region[チップ番号を記録]
                                        //switch(chip.nコース)
                                        //{
                                        //    case 0:
                                        //        this.n連打チップ_temp[0] = this.nNowRoll;
                                        //        this.dbSCROLL_temp[0] = this.dbNowSCROLL;
                                        //        break;
                                        //    case 1:
                                        //        this.n連打チップ_temp[1] = this.nNowRoll;
                                        //        this.dbSCROLL_temp[1] = this.dbNowSCROLL;
                                        //        break;
                                        //    case 2:
                                        //        this.n連打チップ_temp[2] = this.nNowRoll;
                                        //        this.dbSCROLL_temp[2] = this.dbNowSCROLL;
                                        //        break;
                                        //}

                                        #endregion

                                        continue;
                                    }
                                case 0x9A:
                                    {

                                        if (this.bOFFSETの値がマイナスである)
                                        {
                                            chip.n発声時刻ms += this.nOFFSET;
                                        }
                                        //chip.n発声時刻ms += this.nDELAY;
                                        //chip.dbBPM = this.dbNowBPM;
                                        //chip.dbSCROLL = this.dbNowSCROLL;

                                        #region[チップ番号を記録]
                                        //風船は現時点では未実装のため処理しない。


                                        //switch (chip.nコース)
                                        //{
                                        //    case 0:
                                        //        if (this.listChip[this.n連打チップ_temp[0]].nチャンネル番号 == 0x99) break;
                                        //        this.listChip[this.n連打チップ_temp[0]].nノーツ終了時刻ms = chip.n発声時刻ms;
                                        //        this.listChip[this.n連打チップ_temp[0]].dbSCROLL = this.dbSCROLL_temp[0];
                                        //        break;
                                        //    case 1:
                                        //        if (this.listChip[this.n連打チップ_temp[1]].nチャンネル番号 == 0x99) break;
                                        //        this.listChip[this.n連打チップ_temp[1]].nノーツ終了時刻ms = chip.n発声時刻ms;
                                        //        this.listChip[this.n連打チップ_temp[1]].dbSCROLL = this.dbSCROLL_temp[1];
                                        //        break;
                                        //    case 2:
                                        //        if (this.listChip[this.n連打チップ_temp[2]].nチャンネル番号 == 0x99) break;
                                        //        this.listChip[this.n連打チップ_temp[2]].nノーツ終了時刻ms = chip.n発声時刻ms;
                                        //        this.listChip[this.n連打チップ_temp[2]].dbSCROLL = this.dbSCROLL_temp[2];
                                        //        break;
                                        //}

                                        #endregion

                                        //this.listChip[this.nNowRoll].nノーツ終了時刻ms = chip.n発声時刻ms;
                                        //this.listChip[this.nNowRoll].dbSCROLL = this.dbNowSCROLL;
                                        //this.listChip[this.nNowRoll].dbBPM = this.dbNowBPM;
                                        continue;
                                    }
                                case 0x9D:
                                    {
                                        //if ( this.listSCROLL.ContainsKey( chip.n整数値_内部番号 ) )
                                        //{
                                        //this.dbNowSCROLL = ( ( this.listSCROLL[ chip.n整数値_内部番号 ].n表記上の番号 == 0 ) ? 0.0 : 1.0 ) + this.listSCROLL[ chip.n整数値_内部番号 ].dbSCROLL値;
                                        //}

                                        //switch (chip.nコース)
                                        //{
                                        //    case 0:
                                        //        this.dbNowSCROLL_Normal = this.dbNowSCROLL;
                                        //        this.n現在のコース = 0;
                                        //        break;
                                        //    case 1:
                                        //        this.dbNowSCROLL_Expert = this.dbNowSCROLL;
                                        //        this.n現在のコース = 1;
                                        //        break;
                                        //    case 2:
                                        //        this.dbNowSCROLL_Master = this.dbNowSCROLL;
                                        //        this.n現在のコース = 2;
                                        //        break;
                                        //}

                                        continue;
                                    }
                                case 0xDC:
                                    {
                                        if (this.bOFFSETの値がマイナスである)
                                            chip.n発声時刻ms += this.nOFFSET;
                                        //if ( this.listDELAY.ContainsKey( chip.n整数値_内部番号 ) )
                                        //{
                                        //    this.nDELAY = ( ( this.listDELAY[ chip.n整数値_内部番号 ].n表記上の番号 == 0 ) ? 0 : 0 ) + this.listDELAY[ chip.n整数値_内部番号 ].nDELAY値;
                                        //}
                                        continue;
                                    }
                                case 0xDE:
                                    {
                                        if (this.bOFFSETの値がマイナスである)
                                        {
                                            chip.n発声時刻ms += this.nOFFSET;
                                            chip.n分岐時刻ms += this.nOFFSET;
                                        }
                                        this.n現在のコース = chip.nコース;
                                        continue;
                                    }
                                case 0x52:
                                    {
                                        if (this.bOFFSETの値がマイナスである)
                                        {
                                            chip.n発声時刻ms += this.nOFFSET;
                                            chip.n分岐時刻ms += this.nOFFSET;
                                        }
                                        this.n現在のコース = chip.nコース;
                                        continue;
                                    }
                                case 0xDF:
                                    {
                                        if (this.bOFFSETの値がマイナスである)
                                            chip.n発声時刻ms += this.nOFFSET;
                                        //if ( this.listBRANCH.ContainsKey( chip.n整数値_内部番号 ) )
                                        //{
                                        //this.listBRANCH[chip.n整数値_内部番号].db分岐時間ms = chip.n発声時刻ms + ( this.bOFFSETの値がマイナスである ? this.nOFFSET : 0 );
                                        //}

                                        continue;
                                    }
                                case 0xE0:
                                    {
                                        //if (this.bOFFSETの値がマイナスである)
                                        //    chip.n発声時刻ms += this.nOFFSET;

                                        //chip.dbBPM = this.dbNowBPM;
                                        //chip.dbSCROLL = this.dbNowSCROLL;
                                        //if( chip.n整数値_内部番号 == 1 )
                                        //    this.bBarLine = false;
                                        //else
                                        //    this.bBarLine = true;
                                        continue;
                                    }
                                default:
                                    {
                                        if (this.bOFFSETの値がマイナスである)
                                            chip.n発声時刻ms += this.nOFFSET;
                                        //chip.n発声時刻ms += this.nDELAY;
                                        chip.dbBPM = this.dbNowBPM;
                                        //chip.dbSCROLL = this.dbNowSCROLL;
                                        continue;
                                    }
                            }
                        }
                        if (this.db再生速度 > 0.0)
                        {
                            double _db再生速度 = (TJAPlayer3.DTXVmode.Enabled) ? this.dbDTXVPlaySpeed : this.db再生速度;
                            foreach (CChip chip in this.listChip)
                            {
                                chip.n発声時刻ms = (int)(((double)chip.n発声時刻ms) / _db再生速度);
                                chip.db発声時刻ms = (((double)chip.n発声時刻ms) / _db再生速度);
                                chip.nノーツ終了時刻ms = (int)(((double)chip.nノーツ終了時刻ms) / _db再生速度);
                            }
                        }
                        #endregion
                        //span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
                        //Trace.TraceInformation( "発声時刻計算:             {0}", span.ToString() );
                        //timeBeginLoad = DateTime.Now;

                        #region[listlyricを時間順に並び替え。]
                        this.listLyric2 = tmplistlyric;
                        this.listLyric2.Sort((a, b) => a.Time.CompareTo(b.Time));
                        #endregion

                        this.nBGMAdjust = 0;
                        this.t各自動再生音チップの再生時刻を変更する(nBGMAdjust);
                        //span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
                        //Trace.TraceInformation( "再生時刻変更:             {0}", span.ToString() );
                        //timeBeginLoad = DateTime.Now;

                        #region [ 可視チップ数カウント ]
                        for (int n = 0; n < 14; n++)
                        {
                            this.n可視チップ数[n] = 0;
                        }
                        foreach (CChip chip in this.listChip)
                        {
                            int c = chip.nチャンネル番号;
                            if ((0x11 <= c) && (c <= 0x14))
                            {
                                if (c == 0x11 || c == 0x13)
                                    this.n可視チップ数.Taiko_Red++;
                                else if (c == 0x12 || c == 0x14)
                                    this.n可視チップ数.Taiko_Blue++;
                            }
                        }
                        #endregion
                        //span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
                        //Trace.TraceInformation( "可視チップ数カウント      {0}", span.ToString() );
                        //timeBeginLoad = DateTime.Now;
                        #region [ チップの種類を分類し、対応するフラグを立てる ]
                        foreach (CChip chip in this.listChip)
                        {
                            if ((chip.nチャンネル番号 == 0x01 && this.listWAV.TryGetValue(chip.n整数値_内部番号, out CWAV cwav)) && !cwav.listこのWAVを使用するチャンネル番号の集合.Contains(chip.nチャンネル番号))
                            {
                                cwav.listこのWAVを使用するチャンネル番号の集合.Add(chip.nチャンネル番号);

                                int c = chip.nチャンネル番号 >> 4;
                                switch (c)
                                {
                                    case 0x01:
                                        cwav.bIsDrumsSound = true; break;
                                    case 0x02:
                                        cwav.bIsGuitarSound = true; break;
                                    case 0x0A:
                                        cwav.bIsBassSound = true; break;
                                    case 0x06:
                                    case 0x07:
                                    case 0x08:
                                    case 0x09:
                                        cwav.bIsSESound = true; break;
                                    case 0x00:
                                        if (chip.nチャンネル番号 == 0x01)
                                        {
                                            cwav.bIsBGMSound = true; break;
                                        }
                                        break;
                                }
                            }
                        }
                        #endregion
                        //span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
                        //Trace.TraceInformation( "ch番号集合確認:           {0}", span.ToString() );
                        //timeBeginLoad = DateTime.Now;
                        #region[ seNotes計算 ]
                        if (this.listBRANCH.Count != 0)
                            this.tSetSenotes_branch();
                        else
                            this.tSetSenotes();

                        #endregion
                        #region [ bLogDTX詳細ログ出力 ]
                        if (TJAPlayer3.ConfigIni.bLogDTX詳細ログ出力)
                        {
                            foreach (CWAV cwav in this.listWAV.Values)
                            {
                                Trace.TraceInformation(cwav.ToString());
                            }
                            foreach (CBPM cbpm3 in this.listBPM.Values)
                            {
                                Trace.TraceInformation(cbpm3.ToString());
                            }
                            foreach (CChip chip in this.listChip)
                            {
                                Trace.TraceInformation(chip.ToString());
                            }
                        }
                        #endregion

                        //ソートっぽい
                        //this.listChip.Sort(delegate(CChip pchipA, CChip pchipB) { return pchipA.n発声時刻ms - pchipB.n発声時刻ms; } );
                        //Random ran1 = new Random();
                        //for (int n = 0; n < this.listChip.Count; n++ )
                        //{

                        //    if (CDTXMania.ConfigIni.bHispeedRandom)
                        //    {

                        //        int nRan = ran1.Next(5, 40);
                        //        this.listChip[n].dbSCROLL = nRan / 10.0;
                        //    }
                        //}
                        int n整数値管理 = 0;
                        foreach (CChip chip in this.listChip)
                        {
                            if (chip.nチャンネル番号 != 0x54)
                                chip.n整数値 = n整数値管理;
                            n整数値管理++;
                        }

                    }
                }
            }
        }

        private string tコメントを削除する(string input)
        {
            string strOutput = Regex.Replace(input, @" *//.*", ""); //2017.01.28 DD コメント前のスペースも削除するように修正

            return strOutput;
        }

        private string[] tコマンド行を削除したTJAを返す(string[] input)
        {
            return this.tコマンド行を削除したTJAを返す(input, 0);
        }

        private string[] tコマンド行を削除したTJAを返す(string[] input, int nMode)
        {
            var sb = new StringBuilder();
            
            // 18/11/11 AioiLight 譜面にSpace、スペース、Tab等が入っているとおかしくなるので修正。
            // 多分コマンドもスペースが抜かれちゃっているが、コマンド行を除く譜面を返すので大丈夫(たぶん)。
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = input[i].Trim();
            }

            for (int n = 0; n < input.Length; n++)
            {
                if (nMode == 0)
                {
                    if (!string.IsNullOrEmpty(input[n]) && NotesManager.FastFlankedParsing(input[n]))//this.CharConvertNote(input[n].Substring(0, 1)) != -1)
                    {
                        sb.Append(input[n] + "\n");
                    }
                }
                else if (nMode == 1)
                {
                    if (!string.IsNullOrEmpty(input[n]) && 
                        (input[n].Substring(0, 1) == "#" 
                        || input[n].StartsWith("EXAM")
                        || NotesManager.FastFlankedParsing(input[n])))//this.CharConvertNote(input[n].Substring(0, 1)) != -1))
                    {
                        if (input[n].StartsWith("BALLOON") || input[n].StartsWith("BPM"))
                        {
                            //A～Fで始まる命令が削除されない不具合の対策
                        }
                        else
                        {
                            sb.Append(input[n] + "\n");
                        }
                    }
                }
                else if (nMode == 2)
                {
                    if (!string.IsNullOrEmpty(input[n]) && NotesManager.FastFlankedParsing(input[n]))//this.CharConvertNote(input[n].Substring(0, 1)) != -1)
                    {
                        if (input[n].StartsWith("BALLOON") || input[n].StartsWith("BPM"))
                        {
                            //A～Fで始まる命令が削除されない不具合の対策
                        }
                        else
                        {
                            sb.Append(input[n] + "\n");
                        }
                    }
                    else
                    {
                        if (input[n].StartsWith("#BRANCHSTART") || input[n] == "#N" || input[n] == "#E" || input[n] == "#M")
                        {
                            sb.Append(input[n] + "\n");
                        }

                    }
                }
            }

            string[] strOutput = sb.ToString().Split(this.dlmtEnter, StringSplitOptions.None);

            return strOutput;
        }

        private string[] t空のstring配列を詰めたstring配列を返す(string[] input)
        {
            var sb = new StringBuilder();

            for (int n = 0; n < input.Length; n++)
            {
                if (!string.IsNullOrEmpty(input[n]))
                {
                    sb.Append(input[n] + "\n");
                }
            }

            string[] strOutput = sb.ToString().Split(this.dlmtEnter, StringSplitOptions.None);

            return strOutput;
        }

        private string StringArrayToString(string[] input)
        {
            return this.StringArrayToString(input, "");
        }
        private string StringArrayToString(string[] input, string strデリミタ文字)
        {
            var sb = new StringBuilder();

            for (int n = 0; n < input.Length; n++)
            {
                sb.Append(input[n] + strデリミタ文字);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="InputText"></param>
        /// <returns>1小節内の文字数</returns>
        private int t1小節の文字数をカウントする(string InputText)
        {
            return InputText.Length - 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="InputText"></param>
        /// <returns>1小節内の文字数</returns>
        private void t1小節の文字数をカウントしてリストに追加する(string InputText)
        {
            int nCount = 0;

            if (InputText.StartsWith("#BRANCHSTART"))
            {
                this.nLineCountTemp = this.n現在の小節数;
                return;
            }
            else if (InputText.StartsWith("#N"))
            {
                this.nLineCountCourseTemp = ECourse.eNormal;
                this.n現在の小節数 = this.nLineCountTemp;
                return;
            }
            else if (InputText.StartsWith("#E"))
            {
                this.nLineCountCourseTemp = ECourse.eExpert;
                this.n現在の小節数 = this.nLineCountTemp;
                return;
            }
            else if (InputText.StartsWith("#M"))
            {
                this.nLineCountCourseTemp = ECourse.eMaster;
                this.n現在の小節数 = this.nLineCountTemp;
                return;
            }


            var line = new CLine();
            line.nコース = this.nLineCountCourseTemp;
            line.n文字数 = InputText.Length - 1;
            line.n小節番号 = this.n現在の小節数;

            this.listLine.Add(line);

            this.n現在の小節数++;

        }

        /// <summary>
        /// 0:改行文字を削除して、デリミタとしてスペースを入れる。(返り値:string)
        /// 1:改行文字を削除、さらにSplitして返す(返り値:string[n])
        /// </summary>
        /// <param name="strInput"></param>
        /// <param name="nMode"></param>
        /// <returns></returns>
        private object str改行文字を削除する(string strInput, int nMode)
        {
            string str = "";
            str = strInput;
            // str = strInput.Replace(Environment.NewLine, "\n");
            // str = str.Replace('\t', ' ');

            unsafe
            {
                fixed (char *s = str)
                {
                    for (int i = 0; i < str.Length; i++)
                    {
                        if (s[i] == '\t')
                            s[i] = ' ';
                        else if (s[i] == '\r')
                            s[i] = '\n';
                    }
                }
            }

            if (nMode == 0)
            {
                str = str.Replace("\n", " ");
            }
            else if (nMode == 1)
            {
                str = str + "\n";

                string[] strArray;
                strArray = str.Split(this.dlmtEnter, StringSplitOptions.RemoveEmptyEntries);

                return strArray;
            }

            return str;
        }

        /// <summary>
        /// コースごとに譜面を分割する。
        /// </summary>
        /// <param name="strTJA"></param>
        /// <returns>各コースの譜面(string[5])</returns>
        private string[] tコースで譜面を分割する(string strTJA)
        {
            string[] strCourseTJA = new string[(int)Difficulty.Total];

            if (strTJA.IndexOf("COURSE", 0) != -1)
            {
                //tja内に「COURSE」があればここを使う。
                string[] strTemp = strTJA.Split(this.dlmtCOURSE, StringSplitOptions.RemoveEmptyEntries);

                for (int n = 1; n < strTemp.Length; n++)
                {
                    int nCourse = 0;
                    string nNC = "";
                    while (strTemp[n].Substring(0, 1) != "\n") //2017.01.29 DD COURSE単語表記に対応
                    {
                        nNC += strTemp[n].Substring(0, 1);
                        strTemp[n] = strTemp[n].Remove(0, 1);
                    }

                    if (this.strConvertCourse(nNC) != -1)
                    {
                        nCourse = this.strConvertCourse(nNC);
                        strCourseTJA[nCourse] = strTemp[n];
                    }
                    else
                    {

                    }
                    //strCourseTJA[ ];

                }
            }
            else
            {
                strCourseTJA[3] = strTJA;
            }

            return strCourseTJA;
        }

        // Regexes
        private static readonly Regex regexForPrefixingCommaStartingLinesWithZero = new Regex(@"^,", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex regexForStrippingHeadingLines = new Regex(
             @"^(?!(TITLE|LEVEL|BPM|WAVE|OFFSET|BALLOON|EXAM1|EXAM2|EXAM3|EXAM4|EXAM5|EXAM6|EXAM7|DANTICK|DANTICKCOLOR|RENREN22|RENREN23|RENREN32|RENREN33|RENREN42|RENREN43|BALLOONNOR|BALLOONEXP|BALLOONMAS|SONGVOL|SEVOL|SCOREINIT|SCOREDIFF|COURSE|STYLE|TOWERTYPE|GAME|LIFE|DEMOSTART|SIDE|SUBTITLE|SCOREMODE|GENRE|MAKER|SELECTBG|MOVIEOFFSET|BGIMAGE|BGMOVIE|HIDDENBRANCH|GAUGEINCR|LYRICFILE|#HBSCROLL|#BMSCROLL)).+\n",
            RegexOptions.Multiline | RegexOptions.Compiled);

        // private static readonly HashSet<string> valableTokens = new HashSet<string>(@"TIT|LEV|BPM|WAV|OFF|BAL|EXA|DAN|REN|BAL|SON|SEV|SCO|COU|STY|TOW|GAM|LIF|DEM|SID|SUB|GEN|MOV|BGI|BGM|HID|GAU|LYR|#HB|#BM".Split('|'));

        private int nDifficulty;

        /// <summary>
        /// 新型。
        /// ○未実装
        /// _「COURSE」定義が無い譜面は未対応
        /// 　→ver2015082200で対応完了。
        /// 
        /// </summary>
        /// <param name="strInput">譜面のデータ</param>
        private void t入力_V4(string strInput, int difficulty)
        {
            nDifficulty = difficulty;
            if (!String.IsNullOrEmpty(strInput)) //空なら通さない
            {

                //2017.01.31 DD カンマのみの行を0,に置き換え
                strInput = regexForPrefixingCommaStartingLinesWithZero.Replace(strInput, "0,");

                //2017.02.03 DD ヘッダ内にある命令以外の文字列を削除
                var startIndex = strInput.IndexOf("#START");
                if (startIndex < 0)
                {
                    Trace.TraceWarning($"#START命令が少なくとも1つは必要です。 ({strファイル名の絶対パス})");
                }
                string strInputHeader = strInput.Remove(startIndex);
                strInput = strInput.Remove(0, startIndex);

                // Regex called here
                // strInputHeader = regexForStrippingHeadingLines.Replace(strInputHeader, "");
                

                strInput = strInputHeader + "\n" + strInput;

                //どうせ使わないので先にSplitしてコメントを削除。
                var strSplitした譜面 = (string[])this.str改行文字を削除する(strInput, 1);

                for (int i = 0; strSplitした譜面.Length > i; i++)
                {
                    // strSplitした譜面[i] = this.tコメントを削除する(strSplitした譜面[i]);

                    int idx = strSplitした譜面[i].IndexOf("//");
                    if (idx >= 0)
                        strSplitした譜面[i] = strSplitした譜面[i].Substring(0, idx);
                }
                //空のstring配列を詰める
                strSplitした譜面 = this.t空のstring配列を詰めたstring配列を返す(strSplitした譜面);

                #region[ヘッダ]

                //2015.05.21 kairera0467
                //ヘッダの読み込みは譜面全体から該当する命令を探す。
                //少し処理が遅くなる可能性はあるが、ここは正確性を重視する。
                //点数などの指定は後から各コースで行うので問題は無いだろう。

                //SplitしたヘッダのLengthの回数だけ、forで回して各種情報を読み取っていく。
                for (int i = 0; strSplitした譜面.Length > i; i++)
                {
                    this.t入力_行解析ヘッダ(strSplitした譜面[i]);
                }
                #endregion

                #region[譜面]

                int n読み込むコース = 3;
                int n譜面数 = 0; //2017.07.22 kairera0467 tjaに含まれる譜面の数


                bool b新処理 = false;

                //まずはコースごとに譜面を分割。
                strSplitした譜面 = this.tコースで譜面を分割する(this.StringArrayToString(strSplitした譜面, "\n"));
                string strTest = "";
                //存在するかのフラグ作成。
                for (int i = 0; i < strSplitした譜面.Length; i++)
                {
                    if (!String.IsNullOrEmpty(strSplitした譜面[i]))
                    {
                        this.b譜面が存在する[i] = true;
                        n譜面数++;
                    }
                    else
                        this.b譜面が存在する[i] = false;
                }

                #region[ 読み込ませるコースを決定 ]
                if (this.b譜面が存在する[difficulty] == false)
                {
                    n読み込むコース = difficulty;
                    n読み込むコース++;
                    for (int n = 1; n < (int)Difficulty.Total; n++)
                    {
                        if (this.b譜面が存在する[n読み込むコース] == false)
                        {
                            n読み込むコース++;
                            if (n読み込むコース > (int)Difficulty.Total - 1)
                                n読み込むコース = 0;
                        }
                        else
                            break;
                    }
                }
                else
                    n読み込むコース = difficulty;
                #endregion

                //指定したコースの譜面の命令を消去する。
                strSplitした譜面[n読み込むコース] = CDTXStyleExtractor.tセッション譜面がある(
                    strSplitした譜面[n読み込むコース],
                    TJAPlayer3.ConfigIni.nPlayerCount > 1 ? (this.nPlayerSide + 1) : 0,
                    this.strファイル名の絶対パス);

                //命令をすべて消去した譜面
                var str命令消去譜面 = strSplitした譜面[n読み込むコース].Split(this.dlmtEnter, StringSplitOptions.RemoveEmptyEntries);


                str命令消去譜面 = this.tコマンド行を削除したTJAを返す(str命令消去譜面, 2);

                //ここで1行の文字数をカウント。配列にして返す。
                var strSplit読み込むコース = strSplitした譜面[n読み込むコース].Split(this.dlmtEnter, StringSplitOptions.RemoveEmptyEntries);
                string str = "";
                try
                {
                    if (n譜面数 > 0)
                    {
                        //2017.07.22 kairera0467 譜面が2つ以上ある場合はCOURSE以下のBALLOON命令を使う
                        this.listBalloon.Clear();
                        this.listBalloon_Normal.Clear();
                        this.listBalloon_Expert.Clear();
                        this.listBalloon_Master.Clear();
                        this.listBalloon_Normal_数値管理 = 0;
                        this.listBalloon_Expert_数値管理 = 0;
                        this.listBalloon_Master_数値管理 = 0;
                    }

                    for (int i = 0; i < strSplit読み込むコース.Length; i++)
                    {
                        if (!String.IsNullOrEmpty(strSplit読み込むコース[i]))
                        {
                            this.t難易度別ヘッダ(strSplit読み込むコース[i]);
                        }
                    }
                    for (int i = 0; i < str命令消去譜面.Length; i++)
                    {
                        if (str命令消去譜面[i].IndexOf(',', 0) == -1 && !String.IsNullOrEmpty(str命令消去譜面[i]))
                        {
                            if (str命令消去譜面[i].Substring(0, 1) == "#")
                            {
                                this.t1小節の文字数をカウントしてリストに追加する(str + str命令消去譜面[i]);
                            }

                            if (NotesManager.FastFlankedParsing(str命令消去譜面[i]))//this.CharConvertNote(str命令消去譜面[i].Substring(0, 1)) != -1)
                                str += str命令消去譜面[i];
                        }
                        else
                        {
                            this.t1小節の文字数をカウントしてリストに追加する(str + str命令消去譜面[i]);
                            str = "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                    Trace.TraceError("例外が発生しましたが処理を継続します。 (9e401212-0b78-4073-88d0-f7e791f36a91)");
                }

                //読み込み部分本体に渡す譜面を作成。
                //0:ヘッダー情報 1:#START以降 となる。個数の定義は後からされるため、ここでは省略。
                var strSplitした後の譜面 = strSplit読み込むコース; //strSplitした譜面[ n読み込むコース ].Split( this.dlmtEnter, StringSplitOptions.RemoveEmptyEntries );
                strSplitした後の譜面 = this.tコマンド行を削除したTJAを返す(strSplitした後の譜面, 1);
                //string str命令消去譜面temp = this.StringArrayToString( this.str命令消去譜面 );
                //string[] strDelimiter = { "," };
                //this.str命令消去譜面 = str命令消去譜面temp.Split( strDelimiter, StringSplitOptions.RemoveEmptyEntries );

                this.n現在の小節数 = 1;
                try
                {
                    #region[ 最初の処理 ]
                    //1小節の時間を挿入して開始時間を調節。
                    this.dbNowTime += ((15000.0 / 120.0 * (4.0 / 4.0)) * 16.0);
                    //this.dbNowBMScollTime += (( this.dbBarLength ) * 16.0 );
                    #endregion
                    //string strWrite = "";
                    for (int i = 0; strSplitした後の譜面.Length > i; i++)
                    {
                        nNowReadLine++;
                        str = strSplitした後の譜面[i];
                        //strWrite += str;
                        //if( !str.StartsWith( "#" ) && !string.IsNullOrEmpty( this.strTemp ) )
                        //{
                        //    str = this.strTemp + str;
                        //}

                        // Check line

                        this.t入力_行解析譜面_V4(str);

                    }

                    // Retrieve all the global exams (non individual) at the end
                    if (DanSongs.Number > 0)
                    {
                        for (int i = 0; i < CExamInfo.cMaxExam; i++)
                        {
                            if (Dan_C[i] != null && List_DanSongs[0].Dan_C[i] == null)
                            {
                                List_DanSongs[0].Dan_C[i] = Dan_C[i];
                            }
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                    Trace.TraceError("例外が発生しましたが処理を継続します。 (2da1e880-6b63-4e82-b018-bf18c3568335)");
                }
                //if( stream != null )
                //{
                //    stream.Flush();
                //    stream.Close();
                //}
                #endregion
            }
        }

        private CChip t発声位置から過去方向で一番近くにある指定チャンネルのチップを返す(int n発声時刻, int nチャンネル番号)
        {
            //過去方向への検索
            for (int i = this.listChip.Count - 1; i >= 0; i--)
            {
                if (this.listChip[i].nチャンネル番号 == nチャンネル番号)
                {
                    return this.listChip[i];
                }
            }

            return null;
        }

        //現在、以下のような行には対応できていません。
        //_パラメータを持つ命令がある
        //_行の途中に命令がある
        private int t文字数解析(string InputText)
        {
            int n文字数 = 0;

            for (int i = 0; i < InputText.Length; i++)
            {
                if (this.CharConvertNote(InputText.Substring(i, 1)) != -1)
                {
                    n文字数++;
                }
            }


            return n文字数;
        }

        private static readonly Regex CommandAndArgumentRegex =
            new Regex(@"^(#[A-Z]+)(?:\s?)(.+?)?$", RegexOptions.Compiled);

        private static readonly Regex BranchStartArgumentRegex =
            new Regex(@"^([^,\s]+)\s*,\s*([^,\s]+)\s*,\s*([^,\s]+)$", RegexOptions.Compiled);

        private void AddError(string command, string argument)
        {
            listErrors.Add($"コメントアウトを除く{(Difficulty)nDifficulty}の{nNowReadLine}行目の{command}が正しくありません。値が{argument}になっています");
        }
        private void AddError_Single(string str)
        {
            listErrors.Add($"コメントアウトを除く{(Difficulty)nDifficulty}の{nNowReadLine}行目の{str}");
        }
        private void AddError(string str)
        {
            listErrors.Add(str);
        }

        private string[] SplitComma(string input)
        {
            var result = new List<string>();
            var workingIndex = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i].Equals(',')) // カンマにぶち当たった
                {
                    if (i - 1 >= 0)// &&演算子でも、例外が起きるので...
                    {
                        if (input[i - 1].Equals('\\')) // 1文字前がバックスラッシュ
                        {
                            input = input.Remove(i - 1, 1);
                        }
                        else
                        {
                            // workingIndexから今の位置までをリストにブチ込む
                            result.Add(input.Substring(workingIndex, i - workingIndex));
                            // workingIndexに今の位置+1を代入
                            workingIndex = i + 1;
                        }
                    }
                    else
                    {
                        // workingIndexから今の位置までをリストにブチ込む
                        result.Add(input.Substring(workingIndex, i - workingIndex));
                        // workingIndexに今の位置+1を代入
                        workingIndex = i + 1;
                    }
                }
                if (i + 1 == input.Length) // 最後に
                {
                    result.Add(input.Substring(workingIndex, input.Length - workingIndex));
                }
            }
            return result.ToArray();
        }


        




        /// <summary>
        /// 譜面読み込みメソッドV4で使用。
        /// </summary>
        /// <param name="InputText"></param>
        private void t命令を挿入する(string InputText)
        {
            #region [Split comma and arguments values]

            string[] SplitComma(string input)
            {
                var result = new List<string>();
                var workingIndex = 0;
                for (int i = 0; i < input.Length; i++)
                {
                    if (input[i] == ',') // カンマにぶち当たった
                    {
                        if (input[i - 1] == '\\') // 1文字前がバックスラッシュ
                        {
                            input = input.Remove(i - 1, 1);
                        }
                        else
                        {
                            // workingIndexから今の位置までをリストにブチ込む
                            result.Add(input.Substring(workingIndex, i - workingIndex));
                            // workingIndexに今の位置+1を代入
                            workingIndex = i + 1;
                        }
                    }
                    if (i + 1 == input.Length) // 最後に
                    {
                        result.Add(input.Substring(workingIndex, input.Length - workingIndex));
                    }
                }
                return result.ToArray();
            }


            var match = CommandAndArgumentRegex.Match(InputText);
            if (!match.Success)
            {
                return;
            }

            var command = match.Groups[1].Value;
            var argumentMatchGroup = match.Groups[2];
            var argument = argumentMatchGroup.Success ? argumentMatchGroup.Value : null;

            while (true)
            {//命令の最後に,が残ってしまっているときの対応
                if (argument != null && argument[argument.Length - 1] == ',')
                    argument = argument.Substring(0, argument.Length - 1);
                else
                    break;
            }

            char[] chDelimiter = new char[] { ' ' };
            string[] strArray = null;

            #endregion

            if (command == "#START")
            {
                //#STARTと同時に鳴らすのはどうかと思うけどしゃーなしだな。
                AddMusicPreTimeMs(); // 音源を鳴らす前に遅延。
                var chip = new CChip();

                chip.nチャンネル番号 = 0x01;
                chip.n発声位置 = 384;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.fBMSCROLLTime = this.dbNowBMScollTime;
                chip.n整数値 = 0x01;
                chip.n整数値_内部番号 = 1;

                // チップを配置。
                this.listChip.Add(chip);

                var chip1 = new CChip();
                chip1.nチャンネル番号 = 0x54;
                //chip1.n発声位置 = 384;
                //chip1.n発声時刻ms = (int)this.dbNowTime;
                if (this.nMOVIEOFFSET == 0)
                    chip1.n発声時刻ms = (int)this.dbNowTime;
                else
                    chip1.n発声時刻ms = (int)this.nMOVIEOFFSET;
                chip1.dbBPM = this.dbNowBPM;
                chip1.fNow_Measure_m = this.fNow_Measure_m;
                chip1.fNow_Measure_s = this.fNow_Measure_s;
                chip1.dbSCROLL = this.dbNowScroll;
                chip1.n整数値 = 0x01;
                chip1.n整数値_内部番号 = 1;

                // チップを配置。

                this.listChip.Add(chip1);
            }
            else if (command == "#END")
            {
                //ためしに割り込む。
                var chip = new CChip();

                chip.nチャンネル番号 = 0xFF;
                chip.n発声位置 = ((this.n現在の小節数 + 2) * 384);
                //chip.n発声時刻ms = (int)( this.dbNowTime + ((15000.0 / this.dbNowBPM * ( 4.0 / 4.0 )) * 16.0) * 2  );
                chip.n発声時刻ms = (int)(this.dbNowTime + 1000); //2016.07.16 kairera0467 終了時から1秒後に設置するよう変更。
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値 = 0xFF;
                chip.n整数値_内部番号 = 1;
                // チップを配置。

                if(n参照中の難易度 == (int)Difficulty.Dan)
                {
                    for (int i = listChip.Count - 1; i >= 0; i--)
                    {
                        //if (listChip[i].nチャンネル番号 >= 0x11 && listChip[i].nチャンネル番号 <= 0x18)
                        if (NotesManager.IsHittableNote(listChip[i]))
                        {
                            if (DanSongs.Number != 0)
                            {
                                Array.Resize(ref this.pDan_LastChip, this.pDan_LastChip.Length + 1);
                                this.pDan_LastChip[DanSongs.Number - 1] = listChip[i];
                                break;
                            }
                        }
                    }
                }

                this.listChip.Add(chip);
            }

            else if (command == "#BPMCHANGE")
            {
                double dbBPM;
                if (!double.TryParse(argument, out dbBPM))
                {
                    AddError(command, argument);
                    dbBPM = 150;
                }
                this.dbNowBPM = dbBPM;

                if (dbBPM > MaxBPM)
                {
                    MaxBPM = dbBPM;
                }
                else if (dbBPM < MinBPM)
                {
                    MinBPM = dbBPM;
                }

                this.listBPM.Add(this.n内部番号BPM1to - 1, new CBPM() { n内部番号 = this.n内部番号BPM1to - 1, n表記上の番号 = 0, dbBPM値 = dbBPM, bpm_change_time = this.dbNowTime - nNextSongOffset, bpm_change_bmscroll_time = this.dbNowBMScollTime, bpm_change_course = this.n現在のコース });


                //チップ追加して割り込んでみる。
                var chip = new CChip();

                chip.nチャンネル番号 = 0x08;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.fBMSCROLLTime = (float)this.dbNowBMScollTime;
                chip.dbBPM = dbBPM;
                chip.n整数値_内部番号 = this.n内部番号BPM1to - 1;

                // チップを配置。

                this.listChip.Add(chip);

                var chip1 = new CChip();
                chip1.nチャンネル番号 = 0x9C;
                chip1.n発声位置 = ((this.n現在の小節数) * 384);
                chip1.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip1.fBMSCROLLTime = (float)this.dbNowBMScollTime;
                chip1.dbBPM = dbBPM;
                chip1.dbSCROLL = this.dbNowScroll;
                chip1.n整数値_内部番号 = this.n内部番号BPM1to - 1;

                // チップを配置。

                this.listChip.Add(chip1);

                this.n内部番号BPM1to++;
            }
            else if (command == "#SCROLL")
            {
                //2016.08.13 kairera0467 複素数スクロールもどきのテスト
                if (argument.IndexOf('i') != -1)
                {
                    //iが入っていた場合、複素数スクロールとみなす。

                    double[] dbComplexNum = new double[2];
                    try
                    {
                        this.tParsedComplexNumber(argument, ref dbComplexNum);
                    }
                    catch(Exception ex)
                    {
                        AddError(command, argument);
                        dbComplexNum[0] = 1.0;
                        dbComplexNum[1] = 0.0;
                    }

                    this.dbNowScroll = dbComplexNum[0];
                    this.dbNowScrollY = dbComplexNum[1];

                    this.listSCROLL.Add(this.n内部番号SCROLL1to, new CSCROLL() { n内部番号 = this.n内部番号SCROLL1to, n表記上の番号 = 0, dbSCROLL値 = dbComplexNum[0], dbSCROLL値Y = dbComplexNum[1] });

                    switch (this.n現在のコース)
                    {
                        case ECourse.eNormal:
                            this.dbNowSCROLL_Normal[0] = dbComplexNum[0];
                            this.dbNowSCROLL_Normal[1] = dbComplexNum[1];
                            break;
                        case ECourse.eExpert:
                            this.dbNowSCROLL_Expert[0] = dbComplexNum[0];
                            this.dbNowSCROLL_Expert[1] = dbComplexNum[1];
                            break;
                        case ECourse.eMaster:
                            this.dbNowSCROLL_Master[0] = dbComplexNum[0];
                            this.dbNowSCROLL_Master[1] = dbComplexNum[1];
                            break;
                        default:
                            this.dbNowSCROLL_Normal[0] = dbComplexNum[0];
                            this.dbNowSCROLL_Normal[1] = dbComplexNum[1];
                            break;
                    }

                    //チップ追加して割り込んでみる。
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0x9D;
                    chip.n発声位置 = ((this.n現在の小節数) * 384) - 1;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = this.n内部番号SCROLL1to;
                    chip.dbSCROLL = dbComplexNum[0];
                    chip.dbSCROLL_Y = dbComplexNum[1];
                    chip.nコース = this.n現在のコース;

                    // チップを配置。

                    this.listChip.Add(chip);
                }
                else
                {
                    double dbSCROLL = 1.0;
                    if (!double.TryParse(argument, out dbSCROLL))
                    {
                        AddError(command, argument);
                        dbSCROLL = 1;
                    }

                    this.dbNowScroll = dbSCROLL;
                    this.dbNowScrollY = 0.0;

                    this.listSCROLL.Add(this.n内部番号SCROLL1to, new CSCROLL() { n内部番号 = this.n内部番号SCROLL1to, n表記上の番号 = 0, dbSCROLL値 = dbSCROLL, dbSCROLL値Y = 0.0 });

                    switch (this.n現在のコース)
                    {
                        case ECourse.eNormal:
                            this.dbNowSCROLL_Normal[0] = dbSCROLL;
                            break;
                        case ECourse.eExpert:
                            this.dbNowSCROLL_Expert[0] = dbSCROLL;
                            break;
                        case ECourse.eMaster:
                            this.dbNowSCROLL_Master[0] = dbSCROLL;
                            break;
                    }

                    //チップ追加して割り込んでみる。
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0x9D;
                    chip.n発声位置 = ((this.n現在の小節数) * 384) - 1;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = this.n内部番号SCROLL1to;
                    chip.dbSCROLL = dbSCROLL;
                    chip.dbSCROLL_Y = 0.0;
                    chip.nコース = this.n現在のコース;

                    // チップを配置。

                    this.listChip.Add(chip);
                }




                this.n内部番号SCROLL1to++;
            }
            else if (command == "#MEASURE")
            {
                strArray = argument.Split(new char[] { '/' });
                WarnSplitLength("#MEASURE subsplit", strArray, 2);

                double[] dbLength = new double[2];
                try
                {
                    dbLength[0] = Convert.ToDouble(strArray[0]);
                    dbLength[1] = Convert.ToDouble(strArray[1]);
                }
                catch(Exception ex)
                {
                    AddError(command, argument);
                }

                double db小節長倍率 = dbLength[0] / dbLength[1];
                this.dbBarLength = db小節長倍率;
                this.fNow_Measure_m = (float)dbLength[1];
                this.fNow_Measure_s = (float)dbLength[0];

                var chip = new CChip();

                chip.nチャンネル番号 = 0x02;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.dbSCROLL = this.dbNowScroll;
                chip.db実数値 = db小節長倍率;
                chip.n整数値_内部番号 = 1;
                // チップを配置。

                this.listChip.Add(chip);

                //lbMaster.Items.Add( ";拍子変更 " + strArray[0] + "/" + strArray[1] );
            }
            else if (command == "#DELAY")
            {
                double nDELAY = 0;
                if (!double.TryParse(argument, out nDELAY))
                {
                    AddError(command, argument);
                    nDELAY = 0;
                }
                nDELAY *= 1000;


                this.listDELAY.Add(this.n内部番号DELAY1to, new CDELAY() { n内部番号 = this.n内部番号DELAY1to, n表記上の番号 = 0, nDELAY値 = (int)nDELAY, delay_bmscroll_time = this.dbLastBMScrollTime, delay_bpm = this.dbNowBPM, delay_course = this.n現在のコース, delay_time = this.dbLastTime });


                //チップ追加して割り込んでみる。
                var chip = new CChip();

                chip.nチャンネル番号 = 0xDC;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.db発声時刻ms = this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.nコース = this.n現在のコース;
                chip.n整数値_内部番号 = this.n内部番号DELAY1to;
                chip.fBMSCROLLTime = this.dbNowBMScollTime;
                // チップを配置。

                this.dbNowTime += nDELAY;
                this.dbNowBMScollTime += nDELAY * this.dbNowBPM / 15000;

                this.listChip.Add(chip);
                this.n内部番号DELAY1to++;
            }

            else if (command == "#GOGOSTART")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0x9E;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;
                this.bGOGOTIME = true;

                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#GOGOEND")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0x9F;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.dbBPM = this.dbNowBPM;
                chip.n整数値_内部番号 = 1;
                this.bGOGOTIME = false;

                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#BGAON")
            {
                try
                {
                    var commandData = argument.Split(' ');
                    string listvdIndex = commandData[0];
                    var bgaStartTime = commandData[1];
                    int index = (10 * int.Parse(listvdIndex[0].ToString())) + int.Parse(listvdIndex[1].ToString()) + 2;
                    
                    var chip = new CChip();
                    chip.nチャンネル番号 = 0x54;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = index;
                    chip.n整数値 = index;

                    chip.VideoStartTimeMs = (int)(float.Parse(bgaStartTime) * 1000);

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                catch (Exception ex)
                {
                        AddError(command, argument);
                }
            }
            else if (command == "#BGAOFF")
            {
                int index = (10 * int.Parse(argument[0].ToString())) + int.Parse(argument[1].ToString()) + 2;
                var chip = new CChip();
                chip.nチャンネル番号 = 0x55;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = index;
                chip.n整数値 = index;

                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#CAMVMOVESTART")
            {
                if (currentCamVMoveChip == null)
                {
                    //starts vertical camera moving
                    //arguments: <start y>,<end y>,<easing type>,<calc type>
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xA0;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 0;

                    try
                    {
                        string[] args = argument.Split(',');
                        chip.fCamScrollStartY = float.Parse(args[0]);
                        chip.fCamScrollEndY = float.Parse(args[1]);
                        chip.strCamEaseType = args[2];

                        var type = args[3];
                        var eType = Easing.CalcType.Quadratic;
                        switch (type)
                        {
                            case "CUBIC":
                                eType = Easing.CalcType.Cubic;
                                break;
                            case "QUARTIC":
                                eType = Easing.CalcType.Quartic;
                                break;
                            case "QUINTIC":
                                eType = Easing.CalcType.Quintic;
                                break;
                            case "SINUSOIDAL":
                                eType = Easing.CalcType.Sinusoidal;
                                break;
                            case "EXPONENTIAL":
                                eType = Easing.CalcType.Exponential;
                                break;
                            case "CIRCULAR":
                                eType = Easing.CalcType.Circular;
                                break;
                            case "LINEAR":
                                eType = Easing.CalcType.Linear;
                                break;
                            default:
                                break;
                        }

                        chip.fCamMoveType = eType;

                        currentCamVMoveChip = chip;

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    catch (Exception ex)
                    {
                        AddError(command, argument);
                    }
                }
                else
                {
                    AddError_Single("Missing #CAMVMOVEEND");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMVMOVEEND");
                }
            }
            else if (command == "#CAMVMOVEEND")
            {
                if (currentCamVMoveChip != null)
                {
                    //ends vertical camera moving
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xA1;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    var index = this.listChip.IndexOf(currentCamVMoveChip);
                    var msDiff = chip.n発声時刻ms - currentCamVMoveChip.n発声時刻ms;

                    currentCamVMoveChip.fCamTimeMs = msDiff;
                    this.listChip[index] = currentCamVMoveChip;

                    currentCamVMoveChip = null;

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #CAMVMOVESTART");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMVMOVESTART");
                }
            }
            else if (command == "#CAMHMOVESTART")
            {
                if (currentCamHMoveChip == null)
                {
                    //starts horizontal camera moving
                    //arguments: <start x>,<end x>,<easing type>,<calc type>
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xA2;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    try
                    {
                        string[] args = argument.Split(',');
                        chip.fCamScrollStartX = float.Parse(args[0]);
                        chip.fCamScrollEndX = float.Parse(args[1]);
                        chip.strCamEaseType = args[2];

                        var type = args[3];
                        var eType = Easing.CalcType.Quadratic;
                        switch (type)
                        {
                            case "CUBIC":
                                eType = Easing.CalcType.Cubic;
                                break;
                            case "QUARTIC":
                                eType = Easing.CalcType.Quartic;
                                break;
                            case "QUINTIC":
                                eType = Easing.CalcType.Quintic;
                                break;
                            case "SINUSOIDAL":
                                eType = Easing.CalcType.Sinusoidal;
                                break;
                            case "EXPONENTIAL":
                                eType = Easing.CalcType.Exponential;
                                break;
                            case "CIRCULAR":
                                eType = Easing.CalcType.Circular;
                                break;
                            case "LINEAR":
                                eType = Easing.CalcType.Linear;
                                break;
                            default:
                                break;
                        }

                        chip.fCamMoveType = eType;

                        currentCamHMoveChip = chip;

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    catch (Exception ex)
                    {
                        AddError(command, argument);
                    }
                }
                else
                {
                    AddError_Single("Missing #CAMHMOVEEND");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMHMOVEEND");
                }
            }
            else if (command == "#CAMHMOVEEND")
            {
                if (currentCamHMoveChip != null)
                {
                    //ends horizontal camera moving
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xA3;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    var index = this.listChip.IndexOf(currentCamHMoveChip);
                    var msDiff = chip.n発声時刻ms - currentCamHMoveChip.n発声時刻ms;

                    currentCamHMoveChip.fCamTimeMs = msDiff;
                    this.listChip[index] = currentCamHMoveChip;

                    currentCamHMoveChip = null;

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #CAMHMOVESTART");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMHMOVESTART");
                }
            }
            else if (command == "#CAMZOOMSTART")
            {
                if (currentCamZoomChip == null)
                {
                    //starts zooming in/out the screen
                    //arguments: <start value>,<end value>,<easing type>,<calc type>
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xA4;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    try
                    {
                        string[] args = argument.Split(',');
                        chip.fCamZoomStart = float.Parse(args[0]);
                        chip.fCamZoomEnd = float.Parse(args[1]);
                        chip.strCamEaseType = args[2];

                        var type = args[3];
                        var eType = Easing.CalcType.Quadratic;
                        switch (type)
                        {
                            case "CUBIC":
                                eType = Easing.CalcType.Cubic;
                                break;
                            case "QUARTIC":
                                eType = Easing.CalcType.Quartic;
                                break;
                            case "QUINTIC":
                                eType = Easing.CalcType.Quintic;
                                break;
                            case "SINUSOIDAL":
                                eType = Easing.CalcType.Sinusoidal;
                                break;
                            case "EXPONENTIAL":
                                eType = Easing.CalcType.Exponential;
                                break;
                            case "CIRCULAR":
                                eType = Easing.CalcType.Circular;
                                break;
                            case "LINEAR":
                                eType = Easing.CalcType.Linear;
                                break;
                            default:
                                break;
                        }

                        chip.fCamMoveType = eType;

                        currentCamZoomChip = chip;

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    catch (Exception ex)
                    {
                        AddError(command, argument);
                    }
                }
                else
                {
                    AddError_Single("Missing #CAMZOOMEND");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMZOOMEND");
                }
            }
            else if (command == "#CAMZOOMEND")
            {
                if (currentCamZoomChip != null)
                {
                    //stops zooming
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xA5;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    var index = this.listChip.IndexOf(currentCamZoomChip);
                    var msDiff = chip.n発声時刻ms - currentCamZoomChip.n発声時刻ms;

                    currentCamZoomChip.fCamTimeMs = msDiff;
                    this.listChip[index] = currentCamZoomChip;

                    currentCamZoomChip = null;

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #CAMZOOMSTART");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMZOOMSTART");
                }
            }
            else if (command == "#CAMROTATIONSTART")
            {
                if (currentCamRotateChip == null)
                {
                    //starts rotating the screen
                    //arguments: <start degrees>,<end degrees>,<easing type>,<calc type>
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xA6;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    try
                    {
                        string[] args = argument.Split(',');
                        chip.fCamRotationStart = float.Parse(args[0]);
                        chip.fCamRotationEnd = float.Parse(args[1]);
                        chip.strCamEaseType = args[2];

                        var type = args[3];
                        var eType = Easing.CalcType.Quadratic;
                        switch (type)
                        {
                            case "CUBIC":
                                eType = Easing.CalcType.Cubic;
                                break;
                            case "QUARTIC":
                                eType = Easing.CalcType.Quartic;
                                break;
                            case "QUINTIC":
                                eType = Easing.CalcType.Quintic;
                                break;
                            case "SINUSOIDAL":
                                eType = Easing.CalcType.Sinusoidal;
                                break;
                            case "EXPONENTIAL":
                                eType = Easing.CalcType.Exponential;
                                break;
                            case "CIRCULAR":
                                eType = Easing.CalcType.Circular;
                                break;
                            case "LINEAR":
                                eType = Easing.CalcType.Linear;
                                break;
                            default:
                                break;
                        }

                        chip.fCamMoveType = eType;

                        currentCamRotateChip = chip;

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    catch (Exception ex)
                    {
                        AddError(command, argument);
                    }
                }
                else
                {
                    AddError_Single("Missing #CAMROTATIONEND");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMROTATIONEND");
                }
            }
            else if (command == "#CAMROTATIONEND")
            {
                if (currentCamRotateChip != null)
                {
                    //stops screen rotation
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xA7;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    var index = this.listChip.IndexOf(currentCamRotateChip);
                    var msDiff = chip.n発声時刻ms - currentCamRotateChip.n発声時刻ms;

                    currentCamRotateChip.fCamTimeMs = msDiff;
                    this.listChip[index] = currentCamRotateChip;

                    currentCamRotateChip = null;

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #CAMROTATIONSTART");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMROTATIONSTART");
                }
            }
            else if (command == "#CAMVSCALESTART")
            {
                if (currentCamVScaleChip == null)
                {
                    //starts vertical camera scale changing
                    //arguments: <start scale>,<end scale>,<easing type>,<calc type>
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xA8;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    try
                    {
                        string[] args = argument.Split(',');
                        chip.fCamScaleStartY = float.Parse(args[0]);
                        chip.fCamScaleEndY = float.Parse(args[1]);
                        chip.strCamEaseType = args[2];

                        var type = args[3];
                        var eType = Easing.CalcType.Quadratic;
                        switch (type)
                        {
                            case "CUBIC":
                                eType = Easing.CalcType.Cubic;
                                break;
                            case "QUARTIC":
                                eType = Easing.CalcType.Quartic;
                                break;
                            case "QUINTIC":
                                eType = Easing.CalcType.Quintic;
                                break;
                            case "SINUSOIDAL":
                                eType = Easing.CalcType.Sinusoidal;
                                break;
                            case "EXPONENTIAL":
                                eType = Easing.CalcType.Exponential;
                                break;
                            case "CIRCULAR":
                                eType = Easing.CalcType.Circular;
                                break;
                            case "LINEAR":
                                eType = Easing.CalcType.Linear;
                                break;
                            default:
                                break;
                        }

                        chip.fCamMoveType = eType;

                        currentCamVScaleChip = chip;

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    catch (Exception ex)
                    {
                        AddError(command, argument);
                    }
                }
                else
                {
                    AddError_Single("Missing #CAMVSCALEEND");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMVSCALEEND");
                }
            }
            else if (command == "#CAMVSCALEEND")
            {
                if (currentCamVScaleChip != null)
                {
                    //ends vertical camera scaling
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xA9;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    var index = this.listChip.IndexOf(currentCamVScaleChip);
                    var msDiff = chip.n発声時刻ms - currentCamVScaleChip.n発声時刻ms;

                    currentCamVScaleChip.fCamTimeMs = msDiff;
                    this.listChip[index] = currentCamVScaleChip;

                    currentCamVScaleChip = null;

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #CAMVSCALESTART");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMVSCALESTART");
                }
            }
            else if (command == "#CAMHSCALESTART")
            {
                if (currentCamHScaleChip == null)
                {
                    //starts horizontal camera scale changing
                    //arguments: <start scale>,<end scale>,<easing type>,<calc type>
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xB0;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    try
                    {
                        string[] args = argument.Split(',');
                        chip.fCamScaleStartX = float.Parse(args[0]);
                        chip.fCamScaleEndX = float.Parse(args[1]);
                        chip.strCamEaseType = args[2];

                        var type = args[3];
                        var eType = Easing.CalcType.Quadratic;
                        switch (type)
                        {
                            case "CUBIC":
                                eType = Easing.CalcType.Cubic;
                                break;
                            case "QUARTIC":
                                eType = Easing.CalcType.Quartic;
                                break;
                            case "QUINTIC":
                                eType = Easing.CalcType.Quintic;
                                break;
                            case "SINUSOIDAL":
                                eType = Easing.CalcType.Sinusoidal;
                                break;
                            case "EXPONENTIAL":
                                eType = Easing.CalcType.Exponential;
                                break;
                            case "CIRCULAR":
                                eType = Easing.CalcType.Circular;
                                break;
                            case "LINEAR":
                                eType = Easing.CalcType.Linear;
                                break;
                            default:
                                break;
                        }

                        chip.fCamMoveType = eType;

                        currentCamHScaleChip = chip;

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    catch (Exception ex)
                    {
                        AddError(command, argument);
                    }
                }
                else
                {
                    AddError_Single("Missing #CAMHSCALEEND");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMHSCALEEND");
                }
            }
            else if (command == "#CAMHSCALEEND")
            {
                if (currentCamHScaleChip != null)
                {
                    //ends horizontal camera scaling
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xB1;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    var index = this.listChip.IndexOf(currentCamHScaleChip);
                    var msDiff = chip.n発声時刻ms - currentCamHScaleChip.n発声時刻ms;

                    currentCamHScaleChip.fCamTimeMs = msDiff;
                    this.listChip[index] = currentCamHScaleChip;

                    currentCamHScaleChip = null;

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #CAMHSCALESTART");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMHSCALESTART");
                }
            }
            else if (command == "#BORDERCOLOR")
            {
                //sets border color
                //arguments: <r>,<g>,<b>
                var chip = new CChip();

                chip.nチャンネル番号 = 0xB2;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                string[] args = argument.Split(',');
                chip.borderColor = new Color4(1f, float.Parse(args[0]) / 255, float.Parse(args[1]) / 255, float.Parse(args[2]) / 255);

                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#CAMHOFFSET")
            {
                if (currentCamHMoveChip == null)
                {
                    //sets camera x offset
                    //argument: <offset>
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xB3;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    if (float.TryParse(argument, out float value))
                    {
                        chip.fCamScrollStartX = value;
                        chip.fCamScrollEndX = value;
                    }
                    else
                    {
                        AddError(command, argument);
                    }
                    chip.strCamEaseType = "IN_OUT";

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #CAMHMOVEEND");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMHMOVEEND");
                }
            }
            else if (command == "#CAMVOFFSET")
            {
                if (currentCamVMoveChip == null)
                {
                    //sets camera y offset
                    //argument: <offset>
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xB4;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    if (float.TryParse(argument, out float value))
                    {
                        chip.fCamScrollStartY = float.Parse(argument);
                        chip.fCamScrollEndY = float.Parse(argument);
                    }
                    else
                    {
                        AddError(command, argument);
                    }
                    chip.strCamEaseType = "IN_OUT";

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #CAMVMOVEEND");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMVMOVEEND");
                }
            }
            else if (command == "#CAMZOOM")
            {
                if (currentCamZoomChip == null)
                {
                    //sets camera zoom factor
                    //argument: <zoom factor>
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xB5;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    if (float.TryParse(argument, out float value))
                    {
                        chip.fCamZoomStart = float.Parse(argument);
                        chip.fCamZoomEnd = float.Parse(argument);
                    }
                    else
                    {
                        AddError(command, argument);
                    }
                    chip.strCamEaseType = "IN_OUT";

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #CAMZOOMEND");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMZOOMEND");
                }
            }
            else if (command == "#CAMROTATION")
            {
                if (currentCamRotateChip == null)
                {
                    //sets camera rotation
                    //argument: <degrees>
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xB6;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    if (float.TryParse(argument, out float value))
                    {
                        chip.fCamRotationStart = float.Parse(argument);
                        chip.fCamRotationEnd = float.Parse(argument);
                    }
                    else
                    {
                        AddError(command, argument);
                    }
                    chip.strCamEaseType = "IN_OUT";

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #CAMROTATIONEND");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMROTATIONEND");
                }
            }
            else if (command == "#CAMHSCALE")
            {
                if (currentCamHScaleChip == null)
                {
                    //sets camera x scale
                    //argument: <scale>
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xB7;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    if (float.TryParse(argument, out float value))
                    {
                        chip.fCamScaleStartX = float.Parse(argument);
                        chip.fCamScaleEndX = float.Parse(argument);
                    }
                    else
                    {
                        AddError(command, argument);
                    }
                    chip.strCamEaseType = "IN_OUT";

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #CAMHSCALEEND");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMHSCALEEND");
                }
            }
            else if (command == "#CAMVSCALE")
            {
                if (currentCamVScaleChip == null)
                {
                    //sets camera y scale
                    //argument: <scale>
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xB8;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;

                    if (float.TryParse(argument, out float value))
                    {
                        chip.fCamScaleStartY = float.Parse(argument);
                        chip.fCamScaleEndY = float.Parse(argument);
                    }
                    else
                    {
                        AddError(command, argument);
                    }
                    chip.strCamEaseType = "IN_OUT";

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #CAMVSCALEEND");
                    Trace.TraceInformation("TJA ERROR: Missing #CAMVSCALEEND");
                }
            }
            else if (command == "#CAMRESET")
            {
                //resets camera properties
                var chip = new CChip();

                chip.nチャンネル番号 = 0xB9;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                chip.fCamScrollStartX = 0.0f;
                chip.fCamScrollEndX = 0.0f;
                chip.fCamScrollStartY = 0.0f;
                chip.fCamScrollEndY = 0.0f;

                chip.fCamZoomStart = 1.0f;
                chip.fCamZoomEnd = 1.0f;
                chip.fCamRotationStart = 0.0f;
                chip.fCamRotationEnd = 0.0f;

                chip.fCamScaleStartX = 1.0f;
                chip.fCamScaleEndX = 1.0f;
                chip.fCamScaleStartY = 1.0f;
                chip.fCamScaleEndY = 1.0f;

                chip.strCamEaseType = "IN_OUT";

                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#ENABLEDORON")
            {
                //resets camera properties
                var chip = new CChip();

                chip.nチャンネル番号 = 0xBA;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#DISABLEDORON")
            {
                //resets camera properties
                var chip = new CChip();

                chip.nチャンネル番号 = 0xBB;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#ADDOBJECT")
            {
                //adds object
                var chip = new CChip();

                chip.nチャンネル番号 = 0xBC;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                try
                {
                    string[] args = argument.Split(',');

                    chip.strObjName = args[0];
                    chip.fObjX = float.Parse(args[1]);
                    chip.fObjY = float.Parse(args[2]);
                    var txPath = this.strフォルダ名 + args[3];
                    Trace.TraceInformation("" + this.bSession譜面を読み込む);
                    if (this.bSession譜面を読み込む)
                    {
                        var obj = new CSongObject(chip.strObjName, chip.fObjX, chip.fObjY, txPath);
                        this.listObj.Add(args[0], obj);
                    }

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#REMOVEOBJECT")
            {
                //removes object
                var chip = new CChip();

                chip.nチャンネル番号 = 0xBD;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                chip.strObjName = argument;

                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#OBJVMOVESTART")
            {
                string[] args = argument.Split(',');

                try
                {
                    string name = args[0];

                    if (!currentObjAnimations.ContainsKey("vmove_" + name))
                    {
                        //starts vertical object movement
                        //arguments: <start y>,<end y>,<easing type>,<calc type>
                        var chip = new CChip();

                        chip.nチャンネル番号 = 0xBE;
                        chip.n発声位置 = ((this.n現在の小節数) * 384);
                        chip.dbBPM = this.dbNowBPM;
                        chip.n発声時刻ms = (int)this.dbNowTime;
                        chip.fNow_Measure_m = this.fNow_Measure_m;
                        chip.fNow_Measure_s = this.fNow_Measure_s;
                        chip.n整数値_内部番号 = 0;

                        chip.strObjName = args[0];
                        chip.fObjStart = float.Parse(args[1]);
                        chip.fObjEnd = float.Parse(args[2]);
                        chip.strObjEaseType = args[3];

                        var type = args[4];
                        var eType = Easing.CalcType.Quadratic;
                        switch (type)
                        {
                            case "CUBIC":
                                eType = Easing.CalcType.Cubic;
                                break;
                            case "QUARTIC":
                                eType = Easing.CalcType.Quartic;
                                break;
                            case "QUINTIC":
                                eType = Easing.CalcType.Quintic;
                                break;
                            case "SINUSOIDAL":
                                eType = Easing.CalcType.Sinusoidal;
                                break;
                            case "EXPONENTIAL":
                                eType = Easing.CalcType.Exponential;
                                break;
                            case "CIRCULAR":
                                eType = Easing.CalcType.Circular;
                                break;
                            case "LINEAR":
                                eType = Easing.CalcType.Linear;
                                break;
                            default:
                                break;
                        }

                        chip.objCalcType = eType;

                        currentObjAnimations.Add("vmove_" + name, chip);

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    else
                    {
                        AddError_Single("Missing #OBJVMOVEEND");
                        Trace.TraceInformation("TJA ERROR: Missing #OBJVMOVEEND");
                    }
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#OBJVMOVEEND")
            {
                string name = argument;

                if (currentObjAnimations.ContainsKey("vmove_" + name))
                {
                    //ends vertical camera moving
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xBF;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;
                    chip.strObjName = argument;

                    currentObjAnimations.TryGetValue("vmove_" + name, out CChip startChip);

                    var index = this.listChip.IndexOf(startChip);
                    var msDiff = chip.n発声時刻ms - startChip.n発声時刻ms;

                    startChip.fObjTimeMs = msDiff;
                    this.listChip[index] = startChip;

                    currentObjAnimations.Remove("vmove_" + name);

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #OBJVMOVESTART");
                    Trace.TraceInformation("TJA ERROR: Missing #OBJVMOVESTART");
                }
            }
            else if (command == "#OBJHMOVESTART")
            {
                string[] args = argument.Split(',');
                try
                {
                    string name = args[0];

                    if (!currentObjAnimations.ContainsKey("hmove_" + name))
                    {
                        //starts horizontal object movement
                        //arguments: <start x>,<end x>,<easing type>,<calc type>
                        var chip = new CChip();

                        chip.nチャンネル番号 = 0xC0;
                        chip.n発声位置 = ((this.n現在の小節数) * 384);
                        chip.dbBPM = this.dbNowBPM;
                        chip.n発声時刻ms = (int)this.dbNowTime;
                        chip.fNow_Measure_m = this.fNow_Measure_m;
                        chip.fNow_Measure_s = this.fNow_Measure_s;
                        chip.n整数値_内部番号 = 0;

                        chip.strObjName = args[0];
                        chip.fObjStart = float.Parse(args[1]);
                        chip.fObjEnd = float.Parse(args[2]);
                        chip.strObjEaseType = args[3];

                        var type = args[4];
                        var eType = Easing.CalcType.Quadratic;
                        switch (type)
                        {
                            case "CUBIC":
                                eType = Easing.CalcType.Cubic;
                                break;
                            case "QUARTIC":
                                eType = Easing.CalcType.Quartic;
                                break;
                            case "QUINTIC":
                                eType = Easing.CalcType.Quintic;
                                break;
                            case "SINUSOIDAL":
                                eType = Easing.CalcType.Sinusoidal;
                                break;
                            case "EXPONENTIAL":
                                eType = Easing.CalcType.Exponential;
                                break;
                            case "CIRCULAR":
                                eType = Easing.CalcType.Circular;
                                break;
                            case "LINEAR":
                                eType = Easing.CalcType.Linear;
                                break;
                            default:
                                break;
                        }

                        chip.objCalcType = eType;

                        currentObjAnimations.Add("hmove_" + name, chip);

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    else
                    {
                        AddError_Single("Missing #OBJHMOVEEND");
                        Trace.TraceInformation("TJA ERROR: Missing #OBJHMOVEEND");
                    }
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#OBJHMOVEEND")
            {
                string name = argument;

                if (currentObjAnimations.ContainsKey("hmove_" + name))
                {
                    //ends horizontal camera moving
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xC1;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;
                    chip.strObjName = argument;

                    currentObjAnimations.TryGetValue("hmove_" + name, out CChip startChip);

                    var index = this.listChip.IndexOf(startChip);
                    var msDiff = chip.n発声時刻ms - startChip.n発声時刻ms;

                    startChip.fObjTimeMs = msDiff;
                    this.listChip[index] = startChip;

                    currentObjAnimations.Remove("hmove_" + name);

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #OBJHMOVESTART");
                    Trace.TraceInformation("TJA ERROR: Missing #OBJHMOVESTART");
                }
            }
            else if (command == "#OBJVSCALESTART")
            {
                string[] args = argument.Split(',');
                try
                {
                    string name = args[0];

                    if (!currentObjAnimations.ContainsKey("vscale_" + name))
                    {
                        var chip = new CChip();

                        chip.nチャンネル番号 = 0xC2;
                        chip.n発声位置 = ((this.n現在の小節数) * 384);
                        chip.dbBPM = this.dbNowBPM;
                        chip.n発声時刻ms = (int)this.dbNowTime;
                        chip.fNow_Measure_m = this.fNow_Measure_m;
                        chip.fNow_Measure_s = this.fNow_Measure_s;
                        chip.n整数値_内部番号 = 0;

                        chip.strObjName = args[0];
                        chip.fObjStart = float.Parse(args[1]);
                        chip.fObjEnd = float.Parse(args[2]);
                        chip.strObjEaseType = args[3];

                        var type = args[4];
                        var eType = Easing.CalcType.Quadratic;
                        switch (type)
                        {
                            case "CUBIC":
                                eType = Easing.CalcType.Cubic;
                                break;
                            case "QUARTIC":
                                eType = Easing.CalcType.Quartic;
                                break;
                            case "QUINTIC":
                                eType = Easing.CalcType.Quintic;
                                break;
                            case "SINUSOIDAL":
                                eType = Easing.CalcType.Sinusoidal;
                                break;
                            case "EXPONENTIAL":
                                eType = Easing.CalcType.Exponential;
                                break;
                            case "CIRCULAR":
                                eType = Easing.CalcType.Circular;
                                break;
                            case "LINEAR":
                                eType = Easing.CalcType.Linear;
                                break;
                            default:
                                break;
                        }

                        chip.objCalcType = eType;

                        currentObjAnimations.Add("vscale_" + name, chip);

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    else
                    {
                        AddError_Single("Missing #OBJVSCALEEND");
                        Trace.TraceInformation("TJA ERROR: Missing #OBJVSCALEEND");
                    }
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#OBJVSCALEEND")
            {
                string name = argument;

                if (currentObjAnimations.ContainsKey("vscale_" + name))
                {
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xC3;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;
                    chip.strObjName = argument;

                    currentObjAnimations.TryGetValue("vscale_" + name, out CChip startChip);

                    var index = this.listChip.IndexOf(startChip);
                    var msDiff = chip.n発声時刻ms - startChip.n発声時刻ms;

                    startChip.fObjTimeMs = msDiff;
                    this.listChip[index] = startChip;

                    currentObjAnimations.Remove("vscale_" + name);

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #OBJVSCALESTART");
                    Trace.TraceInformation("TJA ERROR: Missing #OBJVSCALESTART");
                }
            }
            else if (command == "#OBJHSCALESTART")
            {
                string[] args = argument.Split(',');
                try
                {
                    string name = args[0];

                    if (!currentObjAnimations.ContainsKey("hscale_" + name))
                    {
                        var chip = new CChip();

                        chip.nチャンネル番号 = 0xC4;
                        chip.n発声位置 = ((this.n現在の小節数) * 384);
                        chip.dbBPM = this.dbNowBPM;
                        chip.n発声時刻ms = (int)this.dbNowTime;
                        chip.fNow_Measure_m = this.fNow_Measure_m;
                        chip.fNow_Measure_s = this.fNow_Measure_s;
                        chip.n整数値_内部番号 = 0;

                        chip.strObjName = args[0];
                        chip.fObjStart = float.Parse(args[1]);
                        chip.fObjEnd = float.Parse(args[2]);
                        chip.strObjEaseType = args[3];

                        var type = args[4];
                        var eType = Easing.CalcType.Quadratic;
                        switch (type)
                        {
                            case "CUBIC":
                                eType = Easing.CalcType.Cubic;
                                break;
                            case "QUARTIC":
                                eType = Easing.CalcType.Quartic;
                                break;
                            case "QUINTIC":
                                eType = Easing.CalcType.Quintic;
                                break;
                            case "SINUSOIDAL":
                                eType = Easing.CalcType.Sinusoidal;
                                break;
                            case "EXPONENTIAL":
                                eType = Easing.CalcType.Exponential;
                                break;
                            case "CIRCULAR":
                                eType = Easing.CalcType.Circular;
                                break;
                            case "LINEAR":
                                eType = Easing.CalcType.Linear;
                                break;
                            default:
                                break;
                        }

                        chip.objCalcType = eType;

                        currentObjAnimations.Add("hscale_" + name, chip);

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    else
                    {
                        AddError_Single("Missing #OBJHSCALEEND");
                        Trace.TraceInformation("TJA ERROR: Missing #OBJHSCALEEND");
                    }
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#OBJHSCALEEND")
            {
                string name = argument;

                if (currentObjAnimations.ContainsKey("hscale_" + name))
                {
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xC5;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;
                    chip.strObjName = argument;

                    currentObjAnimations.TryGetValue("hscale_" + name, out CChip startChip);

                    var index = this.listChip.IndexOf(startChip);
                    var msDiff = chip.n発声時刻ms - startChip.n発声時刻ms;

                    startChip.fObjTimeMs = msDiff;
                    this.listChip[index] = startChip;

                    currentObjAnimations.Remove("hscale_" + name);

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #OBJHSCALESTART");
                    Trace.TraceInformation("TJA ERROR: Missing #OBJHSCALESTART");
                }
            }
            else if (command == "#OBJROTATIONSTART")
            {
                string[] args = argument.Split(',');
                try
                {
                    string name = args[0];

                    if (!currentObjAnimations.ContainsKey("rotation_" + name))
                    {
                        var chip = new CChip();

                        chip.nチャンネル番号 = 0xC6;
                        chip.n発声位置 = ((this.n現在の小節数) * 384);
                        chip.dbBPM = this.dbNowBPM;
                        chip.n発声時刻ms = (int)this.dbNowTime;
                        chip.fNow_Measure_m = this.fNow_Measure_m;
                        chip.fNow_Measure_s = this.fNow_Measure_s;
                        chip.n整数値_内部番号 = 0;

                        chip.strObjName = args[0];
                        chip.fObjStart = float.Parse(args[1]);
                        chip.fObjEnd = float.Parse(args[2]);
                        chip.strObjEaseType = args[3];

                        var type = args[4];
                        var eType = Easing.CalcType.Quadratic;
                        switch (type)
                        {
                            case "CUBIC":
                                eType = Easing.CalcType.Cubic;
                                break;
                            case "QUARTIC":
                                eType = Easing.CalcType.Quartic;
                                break;
                            case "QUINTIC":
                                eType = Easing.CalcType.Quintic;
                                break;
                            case "SINUSOIDAL":
                                eType = Easing.CalcType.Sinusoidal;
                                break;
                            case "EXPONENTIAL":
                                eType = Easing.CalcType.Exponential;
                                break;
                            case "CIRCULAR":
                                eType = Easing.CalcType.Circular;
                                break;
                            case "LINEAR":
                                eType = Easing.CalcType.Linear;
                                break;
                            default:
                                break;
                        }

                        chip.objCalcType = eType;

                        currentObjAnimations.Add("rotation_" + name, chip);

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    else
                    {
                        AddError_Single("Missing #OBJROTATIONEND");
                        Trace.TraceInformation("TJA ERROR: Missing #OBJROTATIONEND");
                    }
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#OBJROTATIONEND")
            {
                string name = argument;

                if (currentObjAnimations.ContainsKey("rotation_" + name))
                {
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xC7;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;
                    chip.strObjName = argument;

                    currentObjAnimations.TryGetValue("rotation_" + name, out CChip startChip);

                    var index = this.listChip.IndexOf(startChip);
                    var msDiff = chip.n発声時刻ms - startChip.n発声時刻ms;

                    startChip.fObjTimeMs = msDiff;
                    this.listChip[index] = startChip;

                    currentObjAnimations.Remove("rotation_" + name);

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #OBJROTATIONSTART");
                    Trace.TraceInformation("TJA ERROR: Missing #OBJROTATIONSTART");
                }
            }
            else if (command == "#OBJOPACITYSTART")
            {
                string[] args = argument.Split(',');
                try
                {
                    string name = args[0];

                    if (!currentObjAnimations.ContainsKey("opacity_" + name))
                    {
                        var chip = new CChip();

                        chip.nチャンネル番号 = 0xC8;
                        chip.n発声位置 = ((this.n現在の小節数) * 384);
                        chip.dbBPM = this.dbNowBPM;
                        chip.n発声時刻ms = (int)this.dbNowTime;
                        chip.fNow_Measure_m = this.fNow_Measure_m;
                        chip.fNow_Measure_s = this.fNow_Measure_s;
                        chip.n整数値_内部番号 = 0;

                        chip.strObjName = args[0];
                        chip.fObjStart = float.Parse(args[1]);
                        chip.fObjEnd = float.Parse(args[2]);
                        chip.strObjEaseType = args[3];

                        var type = args[4];
                        var eType = Easing.CalcType.Quadratic;
                        switch (type)
                        {
                            case "CUBIC":
                                eType = Easing.CalcType.Cubic;
                                break;
                            case "QUARTIC":
                                eType = Easing.CalcType.Quartic;
                                break;
                            case "QUINTIC":
                                eType = Easing.CalcType.Quintic;
                                break;
                            case "SINUSOIDAL":
                                eType = Easing.CalcType.Sinusoidal;
                                break;
                            case "EXPONENTIAL":
                                eType = Easing.CalcType.Exponential;
                                break;
                            case "CIRCULAR":
                                eType = Easing.CalcType.Circular;
                                break;
                            case "LINEAR":
                                eType = Easing.CalcType.Linear;
                                break;
                            default:
                                break;
                        }

                        chip.objCalcType = eType;

                        currentObjAnimations.Add("opacity_" + name, chip);

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    else
                    {
                        AddError_Single("Missing #OBJOPACITYEND");
                        Trace.TraceInformation("TJA ERROR: Missing #OBJOPACITYEND");
                    }
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#OBJOPACITYEND")
            {
                string name = argument;

                if (currentObjAnimations.ContainsKey("opacity_" + name))
                {
                    var chip = new CChip();

                    chip.nチャンネル番号 = 0xC9;
                    chip.n発声位置 = ((this.n現在の小節数) * 384);
                    chip.dbBPM = this.dbNowBPM;
                    chip.n発声時刻ms = (int)this.dbNowTime;
                    chip.fNow_Measure_m = this.fNow_Measure_m;
                    chip.fNow_Measure_s = this.fNow_Measure_s;
                    chip.n整数値_内部番号 = 1;
                    chip.strObjName = argument;

                    currentObjAnimations.TryGetValue("opacity_" + name, out CChip startChip);

                    var index = this.listChip.IndexOf(startChip);
                    var msDiff = chip.n発声時刻ms - startChip.n発声時刻ms;

                    startChip.fObjTimeMs = msDiff;
                    this.listChip[index] = startChip;

                    currentObjAnimations.Remove("opacity_" + name);

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                else
                {
                    AddError_Single("Missing #OBJOPACITYSTART");
                    Trace.TraceInformation("TJA ERROR: Missing #OBJOPACITYSTART");
                }
            }
            else if (command == "#OBJCOLOR")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0xCA;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                try
                {
                    string[] args = argument.Split(',');
                    chip.strObjName = args[0];
                    chip.borderColor = new Color4(1f, float.Parse(args[1]) / 255, float.Parse(args[2]) / 255, float.Parse(args[3]) / 255);

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#OBJY")
            {
                string[] args = argument.Split(',');
                try
                {
                    string name = args[0];

                    if (!currentObjAnimations.ContainsKey("vmove_" + name))
                    {
                        var chip = new CChip();

                        chip.nチャンネル番号 = 0xCB;
                        chip.n発声位置 = ((this.n現在の小節数) * 384);
                        chip.dbBPM = this.dbNowBPM;
                        chip.n発声時刻ms = (int)this.dbNowTime;
                        chip.fNow_Measure_m = this.fNow_Measure_m;
                        chip.fNow_Measure_s = this.fNow_Measure_s;
                        chip.n整数値_内部番号 = 0;

                        chip.strObjName = args[0];
                        chip.fObjStart = float.Parse(args[1]);
                        chip.fObjEnd = float.Parse(args[1]);
                        chip.strObjEaseType = "IN_OUT";

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    else
                    {
                        AddError_Single("Missing #OBJVMOVEEND");
                        Trace.TraceInformation("TJA ERROR: Missing #OBJVMOVEEND");
                    }
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#OBJX")
            {
                string[] args = argument.Split(',');
                try
                {
                    string name = args[0];

                    if (!currentObjAnimations.ContainsKey("hmove_" + name))
                    {
                        var chip = new CChip();

                        chip.nチャンネル番号 = 0xCC;
                        chip.n発声位置 = ((this.n現在の小節数) * 384);
                        chip.dbBPM = this.dbNowBPM;
                        chip.n発声時刻ms = (int)this.dbNowTime;
                        chip.fNow_Measure_m = this.fNow_Measure_m;
                        chip.fNow_Measure_s = this.fNow_Measure_s;
                        chip.n整数値_内部番号 = 0;

                        chip.strObjName = args[0];
                        chip.fObjStart = float.Parse(args[1]);
                        chip.fObjEnd = float.Parse(args[1]);
                        chip.strObjEaseType = "IN_OUT";

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    else
                    {
                        AddError_Single("Missing #OBJHMOVEEND");
                        Trace.TraceInformation("TJA ERROR: Missing #OBJHMOVEEND");
                    }
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#OBJVSCALE")
            {
                string[] args = argument.Split(',');
                try
                {
                    string name = args[0];

                    if (!currentObjAnimations.ContainsKey("vscale_" + name))
                    {
                        var chip = new CChip();

                        chip.nチャンネル番号 = 0xCD;
                        chip.n発声位置 = ((this.n現在の小節数) * 384);
                        chip.dbBPM = this.dbNowBPM;
                        chip.n発声時刻ms = (int)this.dbNowTime;
                        chip.fNow_Measure_m = this.fNow_Measure_m;
                        chip.fNow_Measure_s = this.fNow_Measure_s;
                        chip.n整数値_内部番号 = 0;

                        chip.strObjName = args[0];
                        chip.fObjStart = float.Parse(args[1]);
                        chip.fObjEnd = float.Parse(args[1]);
                        chip.strObjEaseType = "IN_OUT";

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    else
                    {
                        AddError_Single("Missing #OBJVSCALEEND");
                        Trace.TraceInformation("TJA ERROR: Missing #OBJVSCALEEND");
                    }
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#OBJHSCALE")
            {
                string[] args = argument.Split(',');
                try
                {
                    string name = args[0];

                    if (!currentObjAnimations.ContainsKey("hscale_" + name))
                    {
                        var chip = new CChip();

                        chip.nチャンネル番号 = 0xCE;
                        chip.n発声位置 = ((this.n現在の小節数) * 384);
                        chip.dbBPM = this.dbNowBPM;
                        chip.n発声時刻ms = (int)this.dbNowTime;
                        chip.fNow_Measure_m = this.fNow_Measure_m;
                        chip.fNow_Measure_s = this.fNow_Measure_s;
                        chip.n整数値_内部番号 = 0;

                        chip.strObjName = args[0];
                        chip.fObjStart = float.Parse(args[1]);
                        chip.fObjEnd = float.Parse(args[1]);
                        chip.strObjEaseType = "IN_OUT";

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    else
                    {
                        AddError_Single("Missing #OBJHSCALEEND");
                        Trace.TraceInformation("TJA ERROR: Missing #OBJHSCALEEND");
                    }
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#OBJROTATION")
            {
                string[] args = argument.Split(',');
                try
                {
                    string name = args[0];

                    if (!currentObjAnimations.ContainsKey("rotation_" + name))
                    {
                        var chip = new CChip();

                        chip.nチャンネル番号 = 0xCF;
                        chip.n発声位置 = ((this.n現在の小節数) * 384);
                        chip.dbBPM = this.dbNowBPM;
                        chip.n発声時刻ms = (int)this.dbNowTime;
                        chip.fNow_Measure_m = this.fNow_Measure_m;
                        chip.fNow_Measure_s = this.fNow_Measure_s;
                        chip.n整数値_内部番号 = 0;

                        chip.strObjName = args[0];
                        chip.fObjStart = float.Parse(args[1]);
                        chip.fObjEnd = float.Parse(args[1]);
                        chip.strObjEaseType = "IN_OUT";

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    else
                    {
                        AddError_Single("Missing #OBJROTATIONEND");
                        Trace.TraceInformation("TJA ERROR: Missing #OBJROTATIONEND");
                    }
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#OBJOPACITY")
            {
                string[] args = argument.Split(',');
                try
                {
                    string name = args[0];

                    if (!currentObjAnimations.ContainsKey("opacity_" + name))
                    {
                        var chip = new CChip();

                        chip.nチャンネル番号 = 0xD0;
                        chip.n発声位置 = ((this.n現在の小節数) * 384);
                        chip.dbBPM = this.dbNowBPM;
                        chip.n発声時刻ms = (int)this.dbNowTime;
                        chip.fNow_Measure_m = this.fNow_Measure_m;
                        chip.fNow_Measure_s = this.fNow_Measure_s;
                        chip.n整数値_内部番号 = 0;

                        chip.strObjName = args[0];
                        chip.fObjStart = float.Parse(args[1]);
                        chip.fObjEnd = float.Parse(args[1]);
                        chip.strObjEaseType = "IN_OUT";

                        // チップを配置。
                        this.listChip.Add(chip);
                    }
                    else
                    {
                        AddError_Single("Missing #OBJOPACITYEND");
                        Trace.TraceInformation("TJA ERROR: Missing #OBJOPACITYEND");
                    }
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#CHANGETEXTURE")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0xD1;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                string[] args = argument.Split(',');
                try
                {
                    chip.strTargetTxName = args[0].Replace("/", "\\");
                    chip.strNewPath = this.strフォルダ名 + args[1];

                    if (this.bSession譜面を読み込む)
                    {
                        if (!this.listOriginalTextures.ContainsKey(chip.strTargetTxName))
                        {
                            TJAPlayer3.Tx.trackedTextures.TryGetValue(chip.strTargetTxName, out CTexture oldTx);
                            this.listOriginalTextures.Add(chip.strTargetTxName, new CTexture(oldTx));
                        }
                        if (!this.listTextures.ContainsKey(chip.strNewPath))
                        {
                            CTexture tx = TJAPlayer3.Tx.TxCSong(chip.strNewPath);
                            this.listTextures.Add(chip.strNewPath, tx);
                        }
                    }

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#RESETTEXTURE")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0xD2;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                chip.strTargetTxName = argument.Replace("/", "\\");

                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#SETCONFIG")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0xD3;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                chip.strConfigValue = argument;

                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#OBJANIMSTART")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0xD4;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                string[] args = argument.Split(',');
                try
                {
                    chip.strObjName = args[0];
                    chip.dbAnimInterval = double.Parse(args[1]);

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#OBJANIMSTARTLOOP")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0xD5;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                string[] args = argument.Split(',');
                try
                {
                    chip.strObjName = args[0];
                    chip.dbAnimInterval = double.Parse(args[1]);

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#OBJANIMEND")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0xD6;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                chip.strObjName = argument;

                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#OBJFRAME")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0xD7;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                string[] args = argument.Split(',');
                try
                {
                    chip.strObjName = args[0];
                    chip.intFrame = int.Parse(args[1]);

                    // チップを配置。
                    this.listChip.Add(chip);
                }
                catch (Exception ex)
                {
                    AddError(command, argument);
                }
            }
            else if (command == "#GAMETYPE")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0xD8;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;
                switch(argument)
                {
                    case "Taiko":
                        chip.eGameType = EGameType.TAIKO;
                        break;
                    case "Bongo":
                    case "Konga":
                        chip.eGameType = EGameType.KONGA;
                        break;
                }

                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#SPLITLANE")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0xD9;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#MERGELANE")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0xE3;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#BARLINE")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0xE4;
                chip.n発声位置 = ((this.n現在の小節数) * 384);
                chip.dbBPM = this.dbNowBPM;
                chip.dbSCROLL = this.dbNowScroll;
                chip.dbSCROLL_Y = this.dbNowScrollY;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;
                chip.bHideBarLine = false;

                // チップを配置。
                this.listChip.Add(chip);
            }

            else if (command == "#SECTION")
            {
                //分岐:条件リセット
                var chip = new CChip();

                chip.nチャンネル番号 = 0xDD;
                chip.n発声位置 = ((this.n現在の小節数 - 1) * 384);
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;
                chip.db発声時刻ms = this.dbNowTime;
                // チップを配置。
                this.listChip.Add(chip);
            }
            else if (command == "#BRANCHSTART")
            {
                #region [ 譜面分岐のパース方法を作り直し ]   
                this.bチップがある.Branch = true;
                this.b最初の分岐である = false;
                this.b分岐を一回でも開始した = true;

                //分岐:分岐スタート
                E分岐種類 e条件;

                //条件数値。
                double[] nNum = new double[2];

                //名前と条件Aの間に,が無いと正常に動作しなくなる.2020.04.23.akasoko26
                #region [ 名前と条件Aの間に,が無いと正常に動作しなくなる ]
                //空白を削除する。
                argument = Regex.Replace(argument, @"\s", "");
                //2文字目が,か数値かをチェック
                var IsNumber = bIsNumber(argument[1]);
                //IsNumber == true であったら,が無いということなので,を2文字目にぶち込む・・・
                if (IsNumber)
                    argument = argument.Insert(1, ",");
                #endregion

                var branchStartArgumentMatch = BranchStartArgumentRegex.Match(argument);
                nNum[0] = Convert.ToDouble(branchStartArgumentMatch.Groups[2].Value);
                nNum[1] = Convert.ToDouble(branchStartArgumentMatch.Groups[3].Value);

                switch (branchStartArgumentMatch.Groups[1].Value)
                {
                    case "p":
                        e条件 = E分岐種類.e精度分岐;
                        break;
                    case "r":
                        e条件 = E分岐種類.e連打分岐;
                        break;
                    case "s":
                        e条件 = E分岐種類.eスコア分岐;
                        break;
                    case "d":
                        e条件 = E分岐種類.e大音符のみ精度分岐;
                        break;
                    default:
                        e条件 = E分岐種類.e精度分岐;
                        break;
                }

                #region [ 分岐開始時のチップ情報を記録 ]
                //現在のチップ情報を記録する必要がある。
                this.t現在のチップ情報を記録する(true);
                #endregion

                #region [ 一小節前の分岐開始Chip ]
                //16分前に戻す計算なんか当てにしちゃだめよ。。(by Akasoko)
                var c小節前の小節線情報 = c一小節前の小節線情報を返す(listChip, e条件);
                CChip c小節前の連打開始位置 = null;

                var chip = new CChip();

                if (e条件 == E分岐種類.e連打分岐)
                {
                    /*
					c小節前の連打開始位置 = c一小節前の小節線情報を返す(listChip, e条件, true);
					//連打分岐の位置を再現
					//この計算式はあてにならないと思うが、まあどうしようもないんでこれで
					//なるべく連打のケツの部分に
					var f連打の長さの半分 = (c小節前の小節線情報.n発声時刻ms - c小節前の連打開始位置.n発声時刻ms) / 2.0f;
					*/

                    chip.n発声時刻ms = c小節前の小節線情報.n発声時刻ms;
                }
                else chip.n発声時刻ms = c小節前の小節線情報.n発声時刻ms;

                chip.nチャンネル番号 = 0xDE;
                chip.fNow_Measure_m = c小節前の小節線情報.fNow_Measure_m;
                chip.fNow_Measure_s = c小節前の小節線情報.fNow_Measure_s;

                //ノーツ * 0.5分後ろにして、ノーツが残らないようにする
                chip.n分岐時刻ms = this.dbNowTime - ((15000.0 / this.dbNowBPM * (this.fNow_Measure_s / this.fNow_Measure_m)) * 0.5);
                chip.e分岐の種類 = e条件;
                chip.n条件数値A = nNum[0];// listに追加していたが仕様を変更。
                chip.n条件数値B = nNum[1];// ""
                chip.dbSCROLL = c小節前の小節線情報.dbSCROLL;
                chip.dbBPM = c小節前の小節線情報.dbBPM;
                this.listChip.Add(chip);
                #endregion

                for (int i = 0; i < 3; i++)
                    IsBranchBarDraw[i] = true;//3コース分の黄色小説線表示㋫ラブ

                IsEndedBranching = false;
                #endregion
            }
            else if (command == "#N" || command == "#E" || command == "#M")//これCourseを全部集めてあとから分岐させればいい件
            {
                //開始時の情報にセット
                t現在のチップ情報を記録する(false);

                if (command == "#N")
                    this.n現在のコース = ECourse.eNormal;//分岐:普通譜面
                else if (command == "#E")
                    this.n現在のコース = ECourse.eExpert;//分岐:玄人譜面
                else if (command == "#M")
                    this.n現在のコース = ECourse.eMaster;//分岐:達人譜面
            }
            else if (command == "#LEVELHOLD")
            {
                var chip = new CChip();
                chip.nチャンネル番号 = 0xE1;
                chip.n発声位置 = ((this.n現在の小節数) * 384) - 1;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 1;

                this.listChip.Add(chip);
            }
            else if (command == "#BRANCHEND")
            {
                var GoBranch = new CChip();

                //End用チャンネルをEmptyから引っ張ってきた。
                GoBranch.nチャンネル番号 = 0x52;
                GoBranch.n発声位置 = ((this.n現在の小節数) * 384) - 1;
                GoBranch.n発声時刻ms = (int)this.dbNowTime;
                GoBranch.fNow_Measure_m = this.fNow_Measure_m;
                GoBranch.fNow_Measure_s = this.fNow_Measure_s;
                GoBranch.dbSCROLL = this.dbNowScroll;
                GoBranch.dbBPM = this.dbNowBPM;
                GoBranch.n整数値_内部番号 = 1;

                this.listChip.Add(GoBranch);

                //End時にも黄色い小節線あったべ？
                for (int i = 0; i < 3; i++)
                    IsBranchBarDraw[i] = true;//3コース分の黄色小説線表示㋫ラブ

                IsEndedBranching = true;
            }
            else if (command == "#BARLINEOFF")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0xE0;
                chip.n発声位置 = ((this.n現在の小節数) * 384) - 1;
                chip.n発声時刻ms = (int)this.dbNowTime + 1;
                chip.n整数値_内部番号 = 1;
                chip.nコース = this.n現在のコース;
                this.bBARLINECUE[0] = 1;

                this.listChip.Add(chip);
            }
            else if (command == "#BARLINEON")
            {
                var chip = new CChip();

                chip.nチャンネル番号 = 0xE0;
                chip.n発声位置 = ((this.n現在の小節数) * 384) - 1;
                chip.n発声時刻ms = (int)this.dbNowTime + 1;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 2;
                chip.nコース = this.n現在のコース;
                this.bBARLINECUE[0] = 0;

                this.listChip.Add(chip);
            }
            else if (command == "#LYRIC" && !usingLyricsFile && TJAPlayer3.ConfigIni.nPlayerCount < 4) // Do not parse LYRIC tags if a lyric file is already loaded
            {
                if (TJAPlayer3.r現在のステージ.eStageID == CStage.EStage.SongLoading)//起動時に重たくなってしまう問題の修正用
                    this.listLyric.Add(this.pf歌詞フォント.DrawText(argument, TJAPlayer3.Skin.Game_Lyric_ForeColor, TJAPlayer3.Skin.Game_Lyric_BackColor, null, 30));

                var chip = new CChip();

                chip.nチャンネル番号 = 0xF1;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 0;
                chip.nコース = this.n現在のコース;

                // チップを配置。

                this.listChip.Add(chip);
                this.bLyrics = true;
            }
            else if (command == "#DIRECTION")
            {
                double dbSCROLL = Convert.ToDouble(argument);
                this.nスクロール方向 = (int)dbSCROLL;

                //チップ追加して割り込んでみる。
                var chip = new CChip();

                chip.nチャンネル番号 = 0xF2;
                chip.n発声位置 = ((this.n現在の小節数) * 384) - 1;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 0;
                chip.nスクロール方向 = (int)dbSCROLL;
                chip.nコース = this.n現在のコース;

                // チップを配置。

                this.listChip.Add(chip);
            }
            else if (command == "#SUDDEN")
            {
                strArray = argument.Split(chDelimiter);
                WarnSplitLength("#SUDDEN", strArray, 2);
                double db出現時刻 = Convert.ToDouble(strArray[0]);
                double db移動待機時刻 = Convert.ToDouble(strArray[1]);
                this.db出現時刻 = db出現時刻;
                this.db移動待機時刻 = db移動待機時刻;

                //チップ追加して割り込んでみる。
                var chip = new CChip();

                chip.nチャンネル番号 = 0xF3;
                chip.n発声位置 = ((this.n現在の小節数) * 384) - 1;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 0;
                chip.nノーツ出現時刻ms = (int)this.db出現時刻;
                chip.nノーツ移動開始時刻ms = (int)this.db移動待機時刻;
                chip.nコース = this.n現在のコース;

                // チップを配置。

                this.listChip.Add(chip);
            }
            else if (command == "#JPOSSCROLL")
            {
                strArray = argument.Split(chDelimiter);
                WarnSplitLength("#JPOSSCROLL", strArray, 2);
                double db移動時刻 = Convert.ToDouble(strArray[0]);
                int n移動px = 0;
                int nComplexMove = 0;
                if (strArray[1].IndexOf('i') != -1)
                {
                    double[] dbComplexNum = new double[2];
                    this.tParsedComplexNumber(strArray[1], ref dbComplexNum);
                    n移動px = Convert.ToInt32(dbComplexNum[0]);
                    nComplexMove = Convert.ToInt32(dbComplexNum[1]);
                }
                else
                    n移動px = Convert.ToInt32(strArray[1]);


                int n移動方向 = (strArray.Length >= 3) ? Convert.ToInt32(strArray[2]) : 0;

                //チップ追加して割り込んでみる。
                var chip = new CChip();

                chip.nチャンネル番号 = 0xE2;
                chip.n発声位置 = ((this.n現在の小節数) * 384) - 1;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = 0;
                chip.nコース = this.n現在のコース;

                // チップを配置。

                this.listJPOSSCROLL.Add(this.n内部番号JSCROLL1to, new CJPOSSCROLL() { n内部番号 = this.n内部番号JSCROLL1to, n表記上の番号 = 0, db移動時間 = db移動時刻, n移動距離px = n移動px, n移動方向 = n移動方向, nVerticalMove = nComplexMove });
                this.listChip.Add(chip);
                this.n内部番号JSCROLL1to++;
            }
            else if (command == "#SENOTECHANGE")
            {
                FixSENote = int.Parse(argument);
                IsEnabledFixSENote = true;
            }
            else if (command == "#NEXTSONG")
            {
                nNextSongOffset += nOFFSET;
                var delayTime = 6200.0 + nOFFSET; // 6.2秒ディレイ
                //チップ追加して割り込んでみる。
                var chip = new CChip();

                chip.nチャンネル番号 = 0x9B;
                chip.n発声位置 = ((this.n現在の小節数) * 384) - 1;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                this.dbNowTime += delayTime;
                this.dbNowBMScollTime += (delayTime - nOFFSET) * this.dbNowBPM / 15000;
                chip.n整数値_内部番号 = 0;
                chip.nコース = this.n現在のコース;

                // チップを配置。
                this.listChip.Add(chip);

                AddMusicPreTimeMs(); // 段位の幕が開いてからの遅延。

                strArray = SplitComma(argument); // \,をエスケープ処理するメソッドだぞっ

                for (int i = listChip.Count - 1; i >= 0; i--)
                {
                    //if (listChip[i].nチャンネル番号 >= 0x11 && listChip[i].nチャンネル番号 <= 0x18)
                    if (NotesManager.IsHittableNote(listChip[i]))
                    {
                        if (DanSongs.Number != 0)
                        {
                            Array.Resize(ref this.pDan_LastChip, this.pDan_LastChip.Length + 1);
                            this.pDan_LastChip[DanSongs.Number - 1] = listChip[i];
                            break;
                        }
                    }
                }

                WarnSplitLength("#NEXTSONG", strArray, 8);
                var dansongs = new DanSongs();
                dansongs.Title = strArray[0];
                dansongs.SubTitle = strArray[1];
                dansongs.Genre = strArray[2];
                dansongs.FileName = strArray[3];
                dansongs.ScoreInit = int.Parse(strArray[4]);
                dansongs.ScoreDiff = int.Parse(strArray[5]);

                if (strArray.Length >= 7 && strArray[6] != "" && strArray[6] != null)
                    dansongs.Level = int.Parse(strArray[6]);
                else if (strArray.Length < 7)
                    dansongs.Level = 10;

                if (strArray.Length >= 8 && strArray[7] != "" && strArray[7] != null)
                    dansongs.Difficulty = strConvertCourse(strArray[7]);
                else if (strArray.Length < 8)
                    dansongs.Difficulty = 3;

                if (strArray.Length == 9 && strArray[8] != "" && strArray[8] != null)
                    dansongs.bTitleShow = bool.Parse(strArray[8]);
                else if (strArray.Length < 9)
                    dansongs.bTitleShow = false;

                dansongs.Wave = new CWAV
                {
                    n内部番号 = this.n内部番号WAV1to,
                    n表記上の番号 = this.n内部番号WAV1to,
                    nチップサイズ = this.n無限管理SIZE[this.n内部番号WAV1to],
                    n位置 = this.n無限管理PAN[this.n内部番号WAV1to],
                    SongVol = this.SongVol,
                    SongLoudnessMetadata = this.SongLoudnessMetadata,
                    strファイル名 = CDTXCompanionFileFinder.FindFileName(this.strフォルダ名, strファイル名, dansongs.FileName),
                    strコメント文 = "TJA BGM"
                };
                dansongs.Wave.SongLoudnessMetadata = LoudnessMetadataScanner.LoadForAudioPath(dansongs.Wave.strファイル名);
                List_DanSongs.Add(dansongs);
                this.listWAV.Add(this.n内部番号WAV1to, dansongs.Wave);
                this.n内部番号WAV1to++;

                var nextSongnextSongChip = new CChip();

                nextSongnextSongChip.nチャンネル番号 = 0x01;
                nextSongnextSongChip.n発声位置 = 384;
                //nextSongnextSongChip.n発声時刻ms = (int)this.dbNowTime - (bOFFSETの値がマイナスである ? -nOFFSET : nOFFSET);
                nextSongnextSongChip.n発声時刻ms = (int)this.dbNowTime;
                nextSongnextSongChip.fNow_Measure_m = this.fNow_Measure_m;
                nextSongnextSongChip.fNow_Measure_s = this.fNow_Measure_s;
                nextSongnextSongChip.n整数値 = 0x01;
                nextSongnextSongChip.n整数値_内部番号 = 1 + List_DanSongs.Count;

                this.listWAV[1].strファイル名 = "";

                Array.Resize(ref bHasBranchDan, List_DanSongs.Count);
                bHasBranchDan[bHasBranchDan.Length - 1] = false; 

                // チップを配置。
                this.listChip.Add(nextSongnextSongChip);
            }
            else if (command == "#NMSCROLL")
            {
                //チップ追加して割り込んでみる。
                var chip = new CChip();

                chip.nチャンネル番号 = 0x09;
                chip.n発声位置 = ((this.n現在の小節数) * 384) - 1;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = this.n内部番号SCROLL1to;
                chip.nコース = this.n現在のコース;

                // チップを配置。
                eScrollMode = EScrollMode.Normal;

                this.listChip.Add(chip);
            }
            else if (command == "#BMSCROLL")
            {
                //チップ追加して割り込んでみる。
                var chip = new CChip();

                chip.nチャンネル番号 = 0x0A;
                chip.n発声位置 = ((this.n現在の小節数) * 384) - 1;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = this.n内部番号SCROLL1to;
                chip.nコース = this.n現在のコース;

                // チップを配置。
                eScrollMode = EScrollMode.BMSCROLL;

                this.listChip.Add(chip);
            }
            else if (command == "#HBSCROLL")
            {
                //チップ追加して割り込んでみる。
                var chip = new CChip();

                chip.nチャンネル番号 = 0x0B;
                chip.n発声位置 = ((this.n現在の小節数) * 384) - 1;
                chip.n発声時刻ms = (int)this.dbNowTime;
                chip.fNow_Measure_m = this.fNow_Measure_m;
                chip.fNow_Measure_s = this.fNow_Measure_s;
                chip.n整数値_内部番号 = this.n内部番号SCROLL1to;
                chip.nコース = this.n現在のコース;

                // チップを配置。
                eScrollMode = EScrollMode.HBSCROLL;

                this.listChip.Add(chip);
            }
        }
        void t現在のチップ情報を記録する(bool bInPut)
        {
            //2020.04.21 こうなってしまったのは仕方がないな。。 
            if (bInPut)
            {
                #region [ 記録する ]
                cBranchStart.dbTime = this.dbNowTime;
                cBranchStart.dbSCROLL = this.dbNowScroll;
                cBranchStart.dbSCROLLY = this.dbNowScrollY;
                cBranchStart.dbBMScollTime = this.dbNowBMScollTime;
                cBranchStart.dbBPM = this.dbNowBPM;
                cBranchStart.fMeasure_s = this.fNow_Measure_s;
                cBranchStart.fMeasure_m = this.fNow_Measure_m;
                cBranchStart.nMeasureCount = this.n現在の小節数;
                cBranchStart.db移動待機時刻 = this.db移動待機時刻;
                cBranchStart.db再生速度 = this.db再生速度;
                cBranchStart.db出現時刻 = this.db出現時刻;
                #endregion
            }
            else
            {
                #region [ 記録した情報をNow~に適応 ]
                this.dbNowTime = cBranchStart.dbTime;
                this.dbNowScroll = cBranchStart.dbSCROLL;
                this.dbNowScrollY = cBranchStart.dbSCROLLY;
                this.dbNowBMScollTime = cBranchStart.dbBMScollTime;
                this.dbNowBPM = cBranchStart.dbBPM;
                this.fNow_Measure_s = cBranchStart.fMeasure_s;
                this.fNow_Measure_m = cBranchStart.fMeasure_m;
                this.n現在の小節数 = cBranchStart.nMeasureCount;
                this.db移動待機時刻 = cBranchStart.db移動待機時刻;
                this.db再生速度 = cBranchStart.db再生速度;
                this.db出現時刻 = cBranchStart.db出現時刻;
                #endregion
            }
        }

        /// <summary>
        /// 一小節前の小節線情報を返すMethod 2020.04.21.akasoko26
        /// </summary>
        /// <param name="listChips"></param>
        /// <returns></returns>
        private CChip c一小節前の小節線情報を返す(List<CChip> listChips, E分岐種類 e分岐種類, bool b分岐前の連打開始 = false)
        {
            //2020.04.20 c一小節前の小節線情報を返すMethodを追加
            //連打分岐時は現在の小節以降の連打の終わり部分の時刻を取得する

            int? nReturnChip = null;

            //--して取得しないとだめよ～ダメダメ💛
            //:damedane:
            for (int i = listChips.Count - 1; i >= 0; i--)
            {
                if (b分岐前の連打開始)
                {
                    //if (listChips[i].nチャンネル番号 == 0x15 || listChips[i].nチャンネル番号 == 0x16)
                    if (NotesManager.IsRoll(listChips[i]) || NotesManager.IsFuzeRoll(listChips[i]))
                    {
                        if (nReturnChip == null)
                            nReturnChip = i;

                        //ReturnChipがnullであったら適応
                    }
                }
                else
                {
                    var Flag = e分岐種類 == E分岐種類.e連打分岐 ? 0x18 : 0x50;

                    if (listChips[i].nチャンネル番号 == Flag)
                    {
                        if (nReturnChip == null)
                            nReturnChip = i;
                        //ReturnChipがnullであったら適応
                    }
                }
            }

            //もし、nReturnChipがnullだったらlistChipのCount - 1にセットする。
            return listChips[nReturnChip == null ? listChips.Count - 1 : (int)nReturnChip];
        }

        private void WarnSplitLength(string name, string[] strArray, int minimumLength)
        {
            if (strArray.Length < minimumLength)
            {
                Trace.TraceWarning(
                    $"命令 {name} のパラメータが足りません。少なくとも {minimumLength} つのパラメータが必要です。 (現在のパラメータ数: {strArray.Length}). ({strファイル名の絶対パス})");
            }
        }

        private void t入力_行解析譜面_V4(string InputText)
        {
            if (!String.IsNullOrEmpty(InputText))
            {
                int n文字数 = 16;

                //現在のコース、小節に当てはまるものをリストから探して文字数を返す。
                for (int i = 0; i < this.listLine.Count; i++)
                {
                    if (this.listLine[i].n小節番号 == this.n現在の小節数 && this.listLine[i].nコース == this.n現在のコース)
                    {
                        n文字数 = this.listLine[i].n文字数;
                    }

                }

                if (InputText.StartsWith("#"))
                {
                    // Call orders here
                    this.t命令を挿入する(InputText);
                    return;
                }
                else if (InputText.StartsWith("EXAM"))
                {
                    this.tDanExamLoad(InputText);
                    return;
                }
                else
                {
                    if (this.b小節線を挿入している == false)
                    {
                        // 小節線にもやってあげないと
                        // IsEndedBranchingがfalseで1回
                        // trueで3回だよ3回
                        for (int i = 0; i < (IsEndedBranching == true ? 3 : 1); i++)
                        {
                            CChip chip = new CChip();
                            chip.n発声位置 = ((this.n現在の小節数) * 384);
                            chip.nチャンネル番号 = 0x50;
                            chip.n発声時刻ms = (int)this.dbNowTime;
                            chip.n整数値 = this.n現在の小節数;
                            chip.n文字数 = n文字数;
                            chip.n整数値_内部番号 = this.n現在の小節数;
                            chip.dbBPM = this.dbNowBPM;
                            chip.fNow_Measure_m = this.fNow_Measure_m;
                            chip.fNow_Measure_s = this.fNow_Measure_s;
                            chip.IsEndedBranching = IsEndedBranching;
                            chip.dbSCROLL = this.dbNowScroll;
                            chip.dbSCROLL_Y = this.dbNowScrollY;
                            chip.fBMSCROLLTime = (float)this.dbNowBMScollTime;
                            chip.eScrollMode = eScrollMode;

                            if (IsEndedBranching)
                                chip.nコース = (ECourse)i;
                            else
                                chip.nコース = n現在のコース;

                            chip.b可視 = true;
                            chip.bHideBarLine = this.bBARLINECUE[0] == 1;
                            #region [ 作り直し ]  
                            if (IsEndedBranching)
                            {
                                if (this.IsBranchBarDraw[i])
                                    chip.bBranch = true;
                            }
                            else
                            {
                                if (this.IsBranchBarDraw[(int)n現在のコース])
                                    chip.bBranch = true;
                            }
                            #endregion

                            this.listChip.Add(chip);

                            #region [ 作り直し ]  
                            if (IsEndedBranching)
                                this.IsBranchBarDraw[i] = false;
                            else this.IsBranchBarDraw[(int)n現在のコース] = false;
                            #endregion
                        }


                        this.dbLastTime = this.dbNowTime;
                        this.b小節線を挿入している = true;

                        #region[ 拍線チップテスト ]
                        //1拍の時間を計算
                        double db1拍 = (60.0 / this.dbNowBPM) / 4.0;
                        //forループ(拍数)
                        for (int measure = 1; measure < this.fNow_Measure_s; measure++)
                        {
                            CChip hakusen = new CChip();
                            hakusen.n発声位置 = ((this.n現在の小節数) * 384);
                            hakusen.n発声時刻ms = (int)(this.dbNowTime + (((db1拍 * 4.0)) * measure) * 1000.0);
                            hakusen.nチャンネル番号 = 0x51;
                            //hakusen.n発声時刻ms = (int)this.dbNowTime;
                            hakusen.fBMSCROLLTime = this.dbNowBMScollTime;
                            hakusen.n整数値_内部番号 = this.n現在の小節数;
                            hakusen.n整数値 = 0;
                            hakusen.dbBPM = this.dbNowBPM;
                            hakusen.dbSCROLL = this.dbNowScroll;
                            hakusen.fNow_Measure_m = this.fNow_Measure_m;
                            hakusen.fNow_Measure_s = this.fNow_Measure_s;
                            hakusen.dbSCROLL_Y = this.dbNowScrollY;
                            hakusen.nコース = n現在のコース;
                            hakusen.eScrollMode = eScrollMode;

                            this.listChip.Add(hakusen);
                            //--全ての拍線の時間を出力する--
                            //Trace.WriteLine( string.Format( "|| {0,3:##0} Time:{1} Beat:{2}", this.n現在の小節数, hakusen.n発声時刻ms, measure ) );
                            //--------------------------------
                        }

                        #endregion

                    }

                    for (int n = 0; n < InputText.Length; n++)
                    {
                        if (InputText.Substring(n, 1) == ",")
                        {
                            this.n現在の小節数++;
                            this.b小節線を挿入している = false;
                            return;
                        }

                        if (InputText.Substring(0, 1) == "F")
                        {
                            bool bTest = true;
                        }


                        int nObjectNum = this.CharConvertNote(InputText.Substring(n, 1));

                        if (nObjectNum != 0)
                        {
                            if ((nObjectNum >= 5 && nObjectNum <= 7) || nObjectNum == 9 || nObjectNum == 13 || nObjectNum == 16 || nObjectNum == 17)
                            {
                                if (nNowRoll != 0)
                                {
                                    this.dbNowTime += (15000.0 / this.dbNowBPM * (this.fNow_Measure_s / this.fNow_Measure_m) * (16.0 / n文字数));
                                    this.dbNowBMScollTime += (double)((this.dbBarLength) * (16.0 / n文字数));
                                    continue;
                                }
                                else
                                {
                                    this.nNowRollCount = listChip.Count;
                                    for (int i = 0; i < this.nNowRollCountBranch.Length; i++)
                                        this.nNowRollCountBranch[i] = listChip_Branch[i].Count;
                                    nNowRoll = nObjectNum;
                                }
                            }

                            // IsEndedBranchingがfalseで1回
                            // trueで3回だよ3回
                            for (int i = 0; i < (IsEndedBranching == true ? 3 : 1); i++)
                            {
                                var chip = new CChip();
                                chip.IsMissed = false;
                                chip.bHit = false;
                                chip.b可視 = true;
                                chip.bShow = true;
                                chip.bShowRoll = true;
                                chip.nチャンネル番号 = 0x10 + nObjectNum;
                                //chip.n発声位置 = (this.n現在の小節数 * 384) + ((384 * n) / n文字数);
                                chip.n発声位置 = (int)((this.n現在の小節数 * 384.0) + ((384.0 * n) / n文字数));
                                chip.db発声位置 = this.dbNowTime;
                                chip.n発声時刻ms = (int)this.dbNowTime;
                                //chip.fBMSCROLLTime = (float)(( this.dbBarLength ) * (16.0f / this.n各小節の文字数[this.n現在の小節数]));
                                chip.fBMSCROLLTime = (float)this.dbNowBMScollTime;
                                chip.n整数値 = nObjectNum;
                                chip.n整数値_内部番号 = 1;
                                chip.IsEndedBranching = IsEndedBranching;
                                chip.fNow_Measure_m = this.fNow_Measure_m;
                                chip.fNow_Measure_s = this.fNow_Measure_s;
                                chip.dbBPM = this.dbNowBPM;
                                chip.dbSCROLL = this.dbNowScroll;
                                chip.dbSCROLL_Y = this.dbNowScrollY;
                                chip.nスクロール方向 = this.nスクロール方向;
                                chip.eScrollMode = eScrollMode;

                                if (IsEndedBranching)
                                    chip.nコース = (ECourse)i;
                                else
                                    chip.nコース = n現在のコース;

                                chip.n分岐回数 = this.n内部番号BRANCH1to;
                                chip.e楽器パート = EInstrumentPad.TAIKO;
                                chip.nノーツ出現時刻ms = (int)(this.db出現時刻 * 1000.0);
                                chip.nノーツ移動開始時刻ms = (int)(this.db移動待機時刻 * 1000.0);
                                chip.nPlayerSide = this.nPlayerSide;
                                chip.bGOGOTIME = this.bGOGOTIME;

                                if (NotesManager.IsKusudama(chip))
                                {
                                    if (IsEndedBranching)
                                    {
                                    }
                                    else
                                    {
                                        // Balloon in branches
                                        chip.nチャンネル番号 = 0x19;
                                    }
                                }

                                if (NotesManager.IsGenericBalloon(chip))
                                {
                                    //this.n現在のコースをswitchで分岐していたため風船の値がうまく割り当てられていない 2020.04.21 akasoko26

                                    #region [Balloons]

                                    switch (chip.nコース)
                                    {
                                        case ECourse.eNormal:
                                            if (this.listBalloon_Normal.Count == 0)
                                            {
                                                chip.nBalloon = 5;
                                                break;
                                            }

                                            if (this.listBalloon_Normal.Count > this.listBalloon_Normal_数値管理)
                                            {
                                                chip.nBalloon = this.listBalloon_Normal[this.listBalloon_Normal_数値管理];
                                                this.listBalloon_Normal_数値管理++;
                                                break;
                                            }
                                            break;
                                        case ECourse.eExpert:
                                            if (this.listBalloon_Expert.Count == 0)
                                            {
                                                chip.nBalloon = 5;
                                                break;
                                            }

                                            if (this.listBalloon_Expert.Count > this.listBalloon_Expert_数値管理)
                                            {
                                                chip.nBalloon = this.listBalloon_Expert[this.listBalloon_Expert_数値管理];
                                                this.listBalloon_Expert_数値管理++;
                                                break;
                                            }
                                            break;
                                        case ECourse.eMaster:
                                            if (this.listBalloon_Master.Count == 0)
                                            {
                                                chip.nBalloon = 5;
                                                break;
                                            }

                                            if (this.listBalloon_Master.Count > this.listBalloon_Master_数値管理)
                                            {
                                                chip.nBalloon = this.listBalloon_Master[this.listBalloon_Master_数値管理];
                                                this.listBalloon_Master_数値管理++;
                                                break;
                                            }
                                            break;
                                    }

                                    #endregion

                                }
                                if (NotesManager.IsRollEnd(chip))
                                {
                                    chip.nノーツ終了位置 = (this.n現在の小節数 * 384) + ((384 * n) / n文字数);
                                    chip.nノーツ終了時刻ms = (int)this.dbNowTime;
                                    chip.fBMSCROLLTime_end = (float)this.dbNowBMScollTime;

                                    chip.nノーツ出現時刻ms = listChip[nNowRollCount].nノーツ出現時刻ms;
                                    chip.nノーツ移動開始時刻ms = listChip[nNowRollCount].nノーツ移動開始時刻ms;

                                    chip.n連打音符State = nNowRoll;

                                    if (!IsEndedBranching || i == 0)
                                    {
                                        listChip[nNowRollCount].nノーツ終了位置 = (this.n現在の小節数 * 384) + ((384 * n) / n文字数);
                                        listChip[nNowRollCount].nノーツ終了時刻ms = (int)this.dbNowTime;
                                        listChip[nNowRollCount].fBMSCROLLTime_end = (int)this.dbNowBMScollTime;
                                    }
                                    else if (!IsEndedBranching)
                                    {
                                        listChip_Branch[(int)chip.nコース][nNowRollCountBranch[(int)chip.nコース]].nノーツ終了位置 = (this.n現在の小節数 * 384) + ((384 * n) / n文字数);
                                        listChip_Branch[(int)chip.nコース][nNowRollCountBranch[(int)chip.nコース]].nノーツ終了時刻ms = (int)this.dbNowTime;
                                        listChip_Branch[(int)chip.nコース][nNowRollCountBranch[(int)chip.nコース]].fBMSCROLLTime_end = (int)this.dbNowBMScollTime;
                                    }
                                    else
                                    {
                                        listChip_Branch[i][nNowRollCountBranch[i]].nノーツ終了位置 = (this.n現在の小節数 * 384) + ((384 * n) / n文字数);
                                        listChip_Branch[i][nNowRollCountBranch[i]].nノーツ終了時刻ms = (int)this.dbNowTime;
                                        listChip_Branch[i][nNowRollCountBranch[i]].fBMSCROLLTime_end = (int)this.dbNowBMScollTime;
                                    }
                                    
                                    //listChip[ nNowRollCount ].dbBPM = this.dbNowBPM;
                                    //listChip[ nNowRollCount ].dbSCROLL = this.dbNowSCROLL;
                                    if (!IsEndedBranching || i == 2)
                                        nNowRoll = 0;
                                    //continue;
                                }

                                if (IsEnabledFixSENote)
                                {
                                    chip.IsFixedSENote = true;
                                    chip.nSenote = FixSENote - 1;
                                }

                                #region[ 固定される種類のsenotesはここで設定しておく。 ]
                                switch (nObjectNum)
                                {
                                    case 3:
                                        chip.nSenote = 5;
                                        break;
                                    case 4:
                                        chip.nSenote = 6;
                                        break;
                                    case 5:
                                        chip.nSenote = 7;
                                        break;
                                    case 6:
                                        chip.nSenote = 0xA;
                                        break;
                                    case 7:
                                        chip.nSenote = 0xB;
                                        break;
                                    case 8:
                                        chip.nSenote = 0xC;
                                        break;
                                    case 9:
                                        chip.nSenote = 0xB;
                                        break;
                                    case 0xA:
                                        chip.nSenote = 5;
                                        break;
                                    case 0xB:
                                        chip.nSenote = 6;
                                        break;
                                    case 0xD:
                                        chip.nSenote = 0xB;
                                        break;
                                    case 0xF1:
                                        chip.nSenote = 5;
                                        break;
                                }
                                #endregion

                                
                                if (NotesManager.IsMissableNote(chip))
                                {
                                    #region [ 作り直し ]
                                    //譜面分岐がない譜面でも値は加算されてしまうがしゃあない
                                    //分岐を開始しない間は共通譜面としてみなす。
                                    if (IsEndedBranching)
                                    {
                                        this.nノーツ数_Branch[i]++;

                                        if (i == 0)
                                        {
                                            if (this.n参照中の難易度 == (int)Difficulty.Dan)
                                            {
                                                this.nDan_NotesCount[DanSongs.Number - 1]++;
                                            }
                                            this.nノーツ数[3]++;
                                        }
                                    }
                                    else
                                    {
                                        this.nノーツ数_Branch[(int)chip.nコース]++;
                                        if (this.n参照中の難易度 == (int)Difficulty.Dan && chip.nコース == ECourse.eMaster)
                                        {
                                            this.nDan_NotesCount[DanSongs.Number - 1]++;
                                        }

                                        if (!this.b分岐を一回でも開始した)
                                        {
                                            //IsEndedBranching==false = forloopが行われていないときのみ
                                            for (int l = 0; l < 3; l++)
                                                this.nノーツ数_Branch[l]++;
                                        }
                                    }
                                    
                                    #endregion
                                }
                                else if (NotesManager.IsGenericBalloon(chip))
                                {
                                    //風船はこのままでも機能しているので何もしない.
                                    if (IsEndedBranching)
                                    {
                                        if (this.n参照中の難易度 == (int)Difficulty.Dan)
                                        {
                                            this.nDan_BalloonCount[DanSongs.Number - 1]++;
                                        }
                                    }
                                    else
                                    {
                                        if (this.n参照中の難易度 == (int)Difficulty.Dan && chip.nコース == ECourse.eMaster)
                                        {
                                            this.nDan_BalloonCount[DanSongs.Number - 1]++;
                                        }
                                    }

                                    if (this.b最初の分岐である == false)
                                    {
                                        this.n風船数[(int)this.n現在のコース]++;
                                    }
                                    else
                                    {
                                        this.n風船数[3]++;
                                    }
                                        
                                }
                                
                                Array.Resize(ref nDan_NotesCount, nDan_NotesCount.Length + 1);
                                Array.Resize(ref nDan_BalloonCount, nDan_BalloonCount.Length + 1);
                                // Array.Resize(ref nDan_BallonCount, nDan_BallonCount.Length + 1);

                                if (IsEndedBranching)
                                {
                                    this.listChip_Branch[i].Add(chip);
                                    if (i == 0)
                                        this.listChip.Add(chip);
                                }
                                else
                                {
                                    this.listChip_Branch[(int)chip.nコース].Add(chip);
                                    this.listChip.Add(chip);
                                }
                                    
                            }
                        }

                        if (IsEnabledFixSENote) IsEnabledFixSENote = false;

                        this.dbLastTime = this.dbNowTime;
                        this.dbLastBMScrollTime = this.dbNowBMScollTime;
                        this.dbNowTime += (15000.0 / this.dbNowBPM * (this.fNow_Measure_s / this.fNow_Measure_m) * (16.0 / n文字数));
                        this.dbNowBMScollTime += (((this.fNow_Measure_s / this.fNow_Measure_m)) * (16.0 / (double)n文字数));
                    }
                }
            }
        }

        /// <summary>
        /// 難易度ごとによって変わるヘッダ値を読み込む。
        /// (BALLOONなど。)
        /// </summary>
        /// <param name="InputText"></param>
        private void t難易度別ヘッダ(string InputText)
        {
            if (TJAPlayer3.actEnumSongs != null && TJAPlayer3.actEnumSongs.IsDeActivated)
            {
                if (InputText.Equals("#NMSCROLL"))
                {
                    eScrollMode = EScrollMode.Normal;
                }
                else if (InputText.Equals("#HBSCROLL"))
                {
                    eScrollMode = EScrollMode.HBSCROLL;
                }
                if (InputText.Equals("#BMSCROLL"))
                {
                    eScrollMode = EScrollMode.BMSCROLL;
                }
            }

            string[] strArray = InputText.Split(new char[] { ':' });
            string strCommandName = "";
            string strCommandParam = "";

            if (strArray.Length == 2)
            {
                strCommandName = strArray[0].Trim();
                strCommandParam = strArray[1].Trim();
            }

            void ParseOptionalInt16(Action<short> setValue)
            {
                this.ParseOptionalInt16(strCommandName, strCommandParam, setValue);
            }

            if (strCommandName.Equals("BALLOON") || strCommandName.Equals("BALLOONNOR"))
            {
                ParseBalloon(strCommandParam, this.listBalloon_Normal);
            }
            else if (strCommandName.Equals("BALLOONEXP"))
            {
                ParseBalloon(strCommandParam, this.listBalloon_Expert);
                //tbBALLOON.Text = strCommandParam;
            }
            else if (strCommandName.Equals("BALLOONMAS"))
            {
                ParseBalloon(strCommandParam, this.listBalloon_Master);
                //tbBALLOON.Text = strCommandParam;
            }
            else if (strCommandName.Equals("SCOREMODE"))
            {
                ParseOptionalInt16(value => this.nScoreModeTmp = value);
            }
            else if (strCommandName.Equals("SCOREINIT"))
            {
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    string[] scoreinit = strCommandParam.Split(',');

                    this.ParseOptionalInt16("SCOREINIT first value", scoreinit[0], value =>
                    {
                        this.nScoreInit[0, this.n参照中の難易度] = value;
                        this.b配点が指定されている[0, this.n参照中の難易度] = true;
                    });

                    if (scoreinit.Length == 2)
                    {
                        this.ParseOptionalInt16("SCOREINIT second value", scoreinit[1], value =>
                        {
                            this.nScoreInit[1, this.n参照中の難易度] = value;
                            this.b配点が指定されている[2, this.n参照中の難易度] = true;
                        });
                    }
                }
            }
            else if (strCommandName.Equals("SCOREDIFF"))
            {
                ParseOptionalInt16(value =>
                {
                    this.nScoreDiff[this.n参照中の難易度] = value;
                    this.b配点が指定されている[1, this.n参照中の難易度] = true;
                });
            }

            //if( this.nScoreModeTmp == 99 ) //2017.01.28 DD SCOREMODEを入力していない場合のみConfigで設定したモードにする
            //{
            //    this.nScoreModeTmp = CDTXMania.ConfigIni.nScoreMode;
            //}
            //if( CDTXMania.ConfigIni.nScoreMode == 3 && !this.b配点が指定されている[ 2, this.n参照中の難易度 ] ){ //2017.06.04 kairera0467
            //    this.nScoreModeTmp = 3;
            //}



            else if (strCommandName.Equals("SCOREMODE"))
            {
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    this.nScoreModeTmp = Convert.ToInt16(strCommandParam);
                }
            }
            else if (strCommandName.Equals("SCOREINIT"))
            {
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    string[] scoreinit = strCommandParam.Split(',');

                    this.nScoreInit[0, this.n参照中の難易度] = Convert.ToInt16(scoreinit[0]);
                    this.b配点が指定されている[0, this.n参照中の難易度] = true;
                    if (scoreinit.Length == 2)
                    {
                        this.nScoreInit[1, this.n参照中の難易度] = Convert.ToInt16(scoreinit[1]);
                        this.b配点が指定されている[2, this.n参照中の難易度] = true;
                    }
                }
            }
            else if (strCommandName.Equals("SCOREDIFF"))
            {
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    this.nScoreDiff[this.n参照中の難易度] = Convert.ToInt16(strCommandParam);
                    this.b配点が指定されている[1, this.n参照中の難易度] = true;
                }
            }
            if (this.nScoreModeTmp == 99) //2017.01.28 DD SCOREMODEを入力していない場合のみConfigで設定したモードにする
            {
                this.nScoreModeTmp = TJAPlayer3.ConfigIni.nScoreMode;
            }
            if (TJAPlayer3.ConfigIni.nScoreMode == 3 && !this.b配点が指定されている[2, this.n参照中の難易度])
            { //2017.06.04 kairera0467
                this.nScoreModeTmp = 3;
            }
        }

        private void tDanExamLoad(string input)
        {
            string[] strArray = input.Split(new char[] { ':' });
            string strCommandName = "";
            string strCommandParam = "";

            if (strArray.Length == 2)
            {
                strCommandName = strArray[0].Trim();
                strCommandParam = strArray[1].Trim();
            }

            // Adapt to EXAM until 7, optimise condition

            if (strCommandName.StartsWith("EXAM"))
            {
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    Exam.Type examType;
                    int[] examValue;
                    Exam.Range examRange;
                    var splitExam = strCommandParam.Split(',');
                    int examNumber = int.Parse(strCommandName.Substring(4)) - 1;

                    if (examNumber > CExamInfo.cMaxExam)
                        return;

                    switch (splitExam[0])
                    {
                        case "g":
                            examType = Exam.Type.Gauge;
                            break;
                        case "jp":
                            examType = Exam.Type.JudgePerfect;
                            break;
                        case "jg":
                            examType = Exam.Type.JudgeGood;
                            break;
                        case "jb":
                            examType = Exam.Type.JudgeBad;
                            break;
                        case "s":
                            examType = Exam.Type.Score;
                            break;
                        case "r":
                            examType = Exam.Type.Roll;
                            break;
                        case "h":
                            examType = Exam.Type.Hit;
                            break;
                        case "c":
                            examType = Exam.Type.Combo;
                            break;
                        case "a":
                            examType = Exam.Type.Accuracy;
                            break;
                        case "ja":
                            examType = Exam.Type.JudgeADLIB;
                            break;
                        case "jm":
                            examType = Exam.Type.JudgeMine;
                            break;
                        default:
                            examType = Exam.Type.Gauge;
                            break;
                    }
                    try
                    {
                        examValue = new int[] { int.Parse(splitExam[1]), int.Parse(splitExam[2]) };
                    }
                    catch (Exception)
                    {
                        examValue = new int[] { 100, 100 };
                    }
                    switch (splitExam[3])
                    {
                        case "m":
                            examRange = Exam.Range.More;
                            break;
                        case "l":
                            examRange = Exam.Range.Less;
                            break;
                        default:
                            examRange = Exam.Range.More;
                            break;
                    }

                    if(Dan_C[examNumber] == null)
                        Dan_C[examNumber] = new Dan_C(examType, examValue, examRange);

                    if (DanSongs.Number > 0)
                        List_DanSongs[DanSongs.Number - 1].Dan_C[examNumber] = new Dan_C(examType, examValue, examRange);
                }
            }
        }

        private void ParseOptionalInt16(string name, string unparsedValue, Action<short> setValue)
        {
            if (string.IsNullOrEmpty(unparsedValue))
            {
                return;
            }

            if (short.TryParse(unparsedValue, out var value))
            {
                setValue(value);
            }
            else
            {
                Trace.TraceWarning($"命令名: {name} のパラメータの値が正しくないことを検知しました。値: {unparsedValue} ({strファイル名の絶対パス})");
            }
        }


        private void ParseBalloon(string strCommandParam, List<int> listBalloon)
        {
            string[] strParam = strCommandParam.Split(',');
            for (int n = 0; n < strParam.Length; n++)
            {
                int n打数;
                try
                {
                    if (strParam[n] == null || strParam[n] == "")
                        break;

                    n打数 = Convert.ToInt32(strParam[n]);
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"おや?エラーが出たようです。お兄様。 ({strファイル名の絶対パス})");
                    Trace.TraceError(ex.ToString());
                    Trace.TraceError("例外が発生しましたが処理を継続します。 (95327158-4e83-4fa9-b5e9-ad3c3d4c2a22)");
                    break;
                }

                listBalloon.Add(n打数);
            }
        }
        private void t入力_行解析ヘッダ(string InputText)
        {
            //やべー。先頭にコメント行あったらやばいやん。
            string[] strArray = InputText.Split(new char[] { ':' }, 2);
            string strCommandName = "";
            string strCommandParam = "";

            if (InputText.StartsWith("#BRANCHSTART"))
            {
                //2015.08.18 kairera0467
                //本来はヘッダ命令ではありませんが、難易度ごとに違う項目なのでここで読み込ませます。
                //Lengthのチェックをされる前ににif文を入れています。
                this.bHasBranch[this.n参照中の難易度] = true;

                if (this.n参照中の難易度 == (int)Difficulty.Dan)
                {
                    this.bHasBranchDan[this.bHasBranchDan.Length - 1] = true;
                }
            }

            //まずは「:」でSplitして割り当てる。
            if (strArray.Length == 2)
            {
                strCommandName = strArray[0].Trim();
                strCommandParam = strArray[1].Trim();
            }
            else if (strArray.Length > 2)
            {
                //strArrayが2じゃない場合、ヘッダのSplitを通していない可能性がある。
                //この処理自体は「t入力」を改造したもの。STARTでSplitしていない等、一部の処理が異なる。

                #region [Header]
                InputText = InputText.Replace(Environment.NewLine, "\n"); //改行文字を別の文字列に差し替え。
                InputText = InputText.Replace('\t', ' '); //何の文字か知らないけどスペースに差し替え。
                InputText = InputText + "\n";

                string[] strDelimiter2 = { "\n" };
                strArray = InputText.Split(strDelimiter2, StringSplitOptions.RemoveEmptyEntries);


                strArray = strArray[0].Split(new char[] { ':' });
                WarnSplitLength("Header Name & Value", strArray, 2);

                strCommandName = strArray[0].Trim();
                strCommandParam = strArray[1].Trim();

                #endregion
                //lblMessage.Text = "おや?strArrayのLengthが2じゃないようですね。お兄様。";
            }

            void ParseOptionalInt16(Action<short> setValue)
            {
                this.ParseOptionalInt16(strCommandName, strCommandParam, setValue);
            }

            //パラメータを分別、そこから割り当てていきます。
            if (strCommandName.Equals("TITLE"))
            {
                this.TITLE.SetString("default", strCommandParam);
            }
            else if (strCommandName.StartsWith("TITLE"))
            {
                string _lang = strCommandName.Substring(5).ToLowerInvariant();
                this.TITLE.SetString(_lang, strCommandParam);
            }
            else if (strCommandName.Equals("SUBTITLE"))
            {
                if (strCommandParam.StartsWith("--") || strCommandParam.StartsWith("++"))
                    this.SUBTITLE.SetString("default", strCommandParam.Substring(2));
                else
                    this.SUBTITLE.SetString("default", strCommandParam);
            }
            else if (strCommandName.StartsWith("SUBTITLE"))
            {
                string _lang = strCommandName.Substring(8).ToLowerInvariant();
                this.SUBTITLE.SetString(_lang, strCommandParam);
            }
            else if (strCommandName.Equals("LEVEL"))
            {
                var level_dec = Convert.ToDouble(strCommandParam);
                var level = (int)level_dec;
                if (strCommandParam != level.ToString())
                {
                    int frac_part = Int32.Parse(level_dec.ToString("0.0", CultureInfo.InvariantCulture).Split('.')[1]);
                    this.LEVELtaikoIcon[this.n参照中の難易度] = (frac_part >= 5) ? ELevelIcon.ePlus : ELevelIcon.eMinus;
                }
                this.LEVEL.Drums = (int)level;
                this.LEVEL.Taiko = (int)level;
                this.LEVELtaiko[this.n参照中の難易度] = (int)level;
            }
            else if (strCommandName.StartsWith("NOTESDESIGNER"))
            {
                this.NOTESDESIGNER[this.n参照中の難易度] = strCommandParam;
            }
            else if (strCommandName.Equals("LIFE"))
            {
                // LIFE here
                var life = (int)Convert.ToDouble(strCommandParam);
                this.LIFE = life;
            }
            else if (strCommandName.Equals("PREIMAGE"))
            {
                this.PREIMAGE = strCommandParam;
            }
            else if (strCommandName.Equals("TOWERTYPE"))
            {
                this.TOWERTYPE = strCommandParam;
            }
            else if (strCommandName.Equals("DANTICK"))
            {
                var tick = (int)Convert.ToDouble(strCommandParam);
                this.DANTICK = tick;
            }
            else if (strCommandName.Equals("DANTICKCOLOR"))
            {
                var tickcolor = ColorTranslator.FromHtml(strCommandParam);
                this.DANTICKCOLOR = tickcolor;
            }
            else if (strCommandName.Equals("BPM"))
            {
                if (strCommandParam.IndexOf(",") != -1)
                    strCommandParam = strCommandParam.Replace(',', '.');

                double dbBPM = Convert.ToDouble(strCommandParam);
                this.BPM = dbBPM;
                this.BASEBPM = dbBPM;
                this.MinBPM = dbBPM;
                this.MaxBPM = dbBPM;
                this.dbNowBPM = dbBPM;

                this.listBPM.Add(this.n内部番号BPM1to - 1, new CBPM() { n内部番号 = this.n内部番号BPM1to - 1, n表記上の番号 = this.n内部番号BPM1to - 1, dbBPM値 = dbBPM, });
                this.n内部番号BPM1to++;


                //チップ追加して割り込んでみる。
                var chip = new CChip();

                chip.nチャンネル番号 = 0x03;
                chip.n発声位置 = ((this.n現在の小節数 - 1) * 384);
                chip.n整数値 = 0x00;
                chip.n整数値_内部番号 = 1;

                this.listChip.Add(chip);
                //tbBPM.Text = strCommandParam;
            }
            else if (strCommandName.Equals("WAVE"))
            {
                if (strBGM_PATH != null)
                {
                    Trace.TraceWarning($"{nameof(CDTX)} is ignoring an extra WAVE header in {this.strファイル名の絶対パス}");
                }
                else
                {
                    this.strBGM_PATH = CDTXCompanionFileFinder.FindFileName(this.strフォルダ名, strファイル名, strCommandParam);
                    //tbWave.Text = strCommandParam;
                    if (this.listWAV != null)
                    {
                        // 2018-08-27 twopointzero - DO attempt to load (or queue scanning) loudness metadata here.
                        //                           TJAP3 is either launching, enumerating songs, or is about to
                        //                           begin playing a song. If metadata is available, we want it now.
                        //                           If is not yet available then we wish to queue scanning.
                        var absoluteBgmPath = Path.Combine(this.strフォルダ名, this.strBGM_PATH);
                        this.SongLoudnessMetadata = LoudnessMetadataScanner.LoadForAudioPath(absoluteBgmPath);

                        var wav = new CWAV()
                        {
                            n内部番号 = this.n内部番号WAV1to,
                            n表記上の番号 = 1,
                            nチップサイズ = this.n無限管理SIZE[this.n内部番号WAV1to],
                            n位置 = this.n無限管理PAN[this.n内部番号WAV1to],
                            SongVol = this.SongVol,
                            SongLoudnessMetadata = this.SongLoudnessMetadata,
                            strファイル名 = this.strBGM_PATH,
                            strコメント文 = "TJA BGM",
                        };

                        this.listWAV.Add(this.n内部番号WAV1to, wav);
                        this.n内部番号WAV1to++;
                    }
                }
            }
            else if (strCommandName.Equals("OFFSET") && !string.IsNullOrEmpty(strCommandParam))
            {
                this.nOFFSET = (int)(Convert.ToDouble(strCommandParam) * 1000);

                this.bOFFSETの値がマイナスである = this.nOFFSET < 0 ? true : false;

                this.listBPM[0].bpm_change_bmscroll_time = -2000 * this.dbNowBPM / 15000;
                if (this.bOFFSETの値がマイナスである == true)
                    this.nOFFSET = this.nOFFSET * -1; //OFFSETは秒を加算するので、必ず正の数にすること。
                //tbOFFSET.Text = strCommandParam;

                // Substract global offset
                this.nOFFSET += ((this.bOFFSETの値がマイナスである == true) ? -TJAPlayer3.ConfigIni.nGlobalOffsetMs : TJAPlayer3.ConfigIni.nGlobalOffsetMs);
            }
            else if (strCommandName.Equals("MOVIEOFFSET"))
            {
                this.nMOVIEOFFSET = (int)(Convert.ToDouble(strCommandParam) * 1000);
                this.bMOVIEOFFSETの値がマイナスである = this.nMOVIEOFFSET < 0 ? true : false;

                if (this.bMOVIEOFFSETの値がマイナスである == true)
                    this.nMOVIEOFFSET = this.nMOVIEOFFSET * -1; //OFFSETは秒を加算するので、必ず正の数にすること。
                //tbOFFSET.Text = strCommandParam;
            }
            #region[移動→不具合が起こるのでここも一応復活させておく]
            else if (strCommandName.Equals("BALLOON") || strCommandName.Equals("BALLOONNOR"))
            {
                ParseBalloon(strCommandParam, this.listBalloon_Normal);
            }
            else if (strCommandName.Equals("BALLOONEXP"))
            {
                ParseBalloon(strCommandParam, this.listBalloon_Expert);
                //tbBALLOON.Text = strCommandParam;
            }
            else if (strCommandName.Equals("BALLOONMAS"))
            {
                ParseBalloon(strCommandParam, this.listBalloon_Master);
                //tbBALLOON.Text = strCommandParam;
            }
            else if (strCommandName.Equals("SCOREMODE"))
            {
                ParseOptionalInt16(value => this.nScoreModeTmp = value);
            }
            else if (strCommandName.Equals("SCOREINIT"))
            {
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    string[] scoreinit = strCommandParam.Split(',');

                    this.ParseOptionalInt16("SCOREINIT first value", scoreinit[0], value =>
                    {
                        this.nScoreInit[0, this.n参照中の難易度] = value;
                    });

                    if (scoreinit.Length == 2)
                    {
                        this.ParseOptionalInt16("SCOREINIT second value", scoreinit[1], value =>
                        {
                            this.nScoreInit[1, this.n参照中の難易度] = value;
                        });
                    }
                }
            }
            else if (strCommandName.Equals("GAUGEINCR"))
            {
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    switch (strCommandParam.ToLower())
                    {
                        case "normal":
                            GaugeIncreaseMode = GaugeIncreaseMode.Normal;
                            break;
                        case "floor":
                            GaugeIncreaseMode = GaugeIncreaseMode.Floor;
                            break;
                        case "round":
                            GaugeIncreaseMode = GaugeIncreaseMode.Round;
                            break;
                        case "ceiling":
                            GaugeIncreaseMode = GaugeIncreaseMode.Ceiling;
                            break;
                        case "notfix":
                            GaugeIncreaseMode = GaugeIncreaseMode.NotFix;
                            break;
                        default:
                            GaugeIncreaseMode = GaugeIncreaseMode.Normal;
                            break;
                    }
                }
            }
            else if (strCommandName.Equals("SCOREDIFF"))
            {
                ParseOptionalInt16(value => this.nScoreDiff[this.n参照中の難易度] = value);
            }
            #endregion
            else if (strCommandName.Equals("SONGVOL") && !string.IsNullOrEmpty(strCommandParam))
            {
                this.SongVol = Convert.ToInt32(strCommandParam).Clamp(CSound.MinimumSongVol, CSound.MaximumSongVol);

                foreach (var kvp in this.listWAV)
                {
                    kvp.Value.SongVol = this.SongVol;
                }
            }
            else if (strCommandName.Equals("SEVOL"))
            {
                //tbSeVol.Text = strCommandParam;
            }
            else if (strCommandName.Equals("COURSE"))
            {
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    //this.n参照中の難易度 = Convert.ToInt16( strCommandParam );
                    this.n参照中の難易度 = this.strConvertCourse(strCommandParam);
                }
            }

            else if (strCommandName.Equals("HEADSCROLL"))
            {
                //新定義:初期スクロール速度設定(というよりこのシステムに合わせるには必須。)
                //どうしても一番最初に1小節挿入されるから、こうするしかなかったんだ___

                this.dbScrollSpeed = Convert.ToDouble(strCommandParam);

                this.listSCROLL.Add(this.n内部番号SCROLL1to, new CSCROLL() { n内部番号 = this.n内部番号SCROLL1to, n表記上の番号 = 0, dbSCROLL値 = this.dbScrollSpeed, });


                //チップ追加して割り込んでみる。
                var chip = new CChip();

                chip.nチャンネル番号 = 0x9D;
                chip.n発声位置 = ((this.n現在の小節数 - 2) * 384);
                chip.n整数値 = 0x00;
                chip.n整数値_内部番号 = this.n内部番号SCROLL1to;
                chip.dbSCROLL = this.dbScrollSpeed;

                // チップを配置。

                this.listChip.Add(chip);
                this.n内部番号SCROLL1to++;

                //this.nScoreDiff = Convert.ToInt16( strCommandParam );
                //tbScoreDiff.Text = strCommandParam;
            }
            else if (strCommandName.Equals("GENRE"))
            {
                //2015.03.28 kairera0467
                //ジャンルの定義。DTXから入力もできるが、tjaからも入力できるようにする。
                //日本語名だと選曲画面でバグが出るので、そこもどうにかしていく予定。

                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    this.GENRE = strCommandParam;
                }
            }
            else if (strCommandName.Equals("MAKER"))
            {
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    this.MAKER = strCommandParam;
                }
            }
            else if (strCommandName.Equals("SIDE"))
            {
                if (!string.IsNullOrEmpty(strCommandParam) && strCommandParam.Equals("Normal"))
                    this.SIDE = ESide.eNormal;
            }
            else if (strCommandName.Equals("EXPLICIT"))
            {
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    this.EXPLICIT = CConversion.bONorOFF(strCommandParam[0]);
                }
            }
            else if (strCommandName.Equals("SELECTBG"))
            {
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    this.SELECTBG = strCommandParam;
                }
            }
            else if (strCommandName.Equals("SCENEPRESET"))
            {
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    this.scenePreset = strCommandParam;
                }
            }
            else if (strCommandName.Equals("DEMOSTART"))
            {
                //2015.04.10 kairera0467

                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    int nOFFSETms;
                    try
                    {
                        nOFFSETms = (int)(Convert.ToDouble(strCommandParam) * 1000.0);
                    }
                    catch
                    {
                        nOFFSETms = 0;
                    }


                    this.nデモBGMオフセット = nOFFSETms;
                }
            }
            else if (strCommandName.Equals("BGMOVIE"))
            {
                //2016.02.02 kairera0467
                //背景動画の定義。DTXから入力もできるが、tjaからも入力できるようにする。

                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    this.strBGVIDEO_PATH =
                        CDTXCompanionFileFinder.FindFileName(this.strフォルダ名, strファイル名, strCommandParam);
                }

				string strVideoFilename;
				if (!string.IsNullOrEmpty(this.PATH_WAV))
					strVideoFilename = this.PATH_WAV + this.strBGVIDEO_PATH;
				else
					strVideoFilename = this.strフォルダ名 + this.strBGVIDEO_PATH;

				try
				{
					CVideoDecoder vd = new CVideoDecoder(strVideoFilename);

					if (this.listVD.ContainsKey(1))
						this.listVD.Remove(1);

					this.listVD.Add(1, vd);
				}
				catch (Exception e)
				{
					Trace.TraceWarning(e.ToString()+"\n"+
						"動画のデコーダー生成で例外が発生しましたが、処理を継続します。");
					if (this.listVD.ContainsKey(1))
						this.listVD.Remove(1);
				}
            }
            else if (strCommandName.Contains("BGA"))
            {
                //2016.02.02 kairera0467
                //背景動画の定義。DTXから入力もできるが、tjaからも入力できるようにする。

                string videoPath = "";
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    videoPath =
                        CDTXCompanionFileFinder.FindFileName(this.strフォルダ名, strファイル名, strCommandParam);
                }

				string strVideoFilename;
				if (!string.IsNullOrEmpty(this.PATH_WAV))
					strVideoFilename = this.PATH_WAV + videoPath;
				else
					strVideoFilename = this.strフォルダ名 + videoPath;

				try
				{
					CVideoDecoder vd = new CVideoDecoder(strVideoFilename);

                    var indexText = strCommandName.Remove(0, 3);

					this.listVD.Add((10 * int.Parse(indexText[0].ToString())) + int.Parse(indexText[1].ToString()) + 2, vd);
				}
				catch (Exception e)
				{
					Trace.TraceWarning(e.ToString()+"\n"+
						"動画のデコーダー生成で例外が発生しましたが、処理を継続します。");
					if (this.listVD.ContainsKey(1))
						this.listVD.Remove(1);
				}
            }
            else if (strCommandName.Equals("BGIMAGE"))
            {
                //2016.02.02 kairera0467
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    this.strBGIMAGE_PATH = strCommandParam;
                }
            }
            else if (strCommandName.Equals("HIDDENBRANCH"))
            {
                //2016.04.01 kairera0467 パラメーターは
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    this.bHIDDENBRANCH = true;
                }
            }
            else if (strCommandName.Equals("LYRICS") && !usingLyricsFile && TJAPlayer3.ConfigIni.nPlayerCount < 4)
            {
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    string[] files = SplitComma(strCommandParam);
                    string[] filePaths = new string[files.Length];
                    for (int i = 0; i < files.Length; i++)
                    {
                        filePaths[i] = this.strフォルダ名 + files[i];

                        if (File.Exists(filePaths[i]))
                        {
                            try
                            {
                                if (TJAPlayer3.r現在のステージ.eStageID == CStage.EStage.SongLoading)
                                {
                                    if (filePaths[i].EndsWith(".vtt"))
                                    {
                                        using (VTTParser parser = new VTTParser())
                                        {
                                            this.listLyric2.AddRange(parser.ParseVTTFile(filePaths[i], 0, 0));
                                        }
                                        this.bLyrics = true;
                                        this.usingLyricsFile = true;
                                    }
                                    else if (filePaths[i].EndsWith(".lrc"))
                                    {
                                        this.LyricFileParser(filePaths[i], i);
                                        this.bLyrics = true;
                                        this.usingLyricsFile = true;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Trace.TraceError("Something went wrong while parsing a lyric file at {0}. More details : {1}", filePaths[i], e);
                            }
                        }
                    }
                }
            }
            else if (strCommandName.Equals("LYRICFILE") && !usingLyricsFile && TJAPlayer3.ConfigIni.nPlayerCount < 4)
            {
                if (!string.IsNullOrEmpty(strCommandParam))
                {
                    string[] strFiles = SplitComma(strCommandParam);
                    string[] strFilePath = new string[strFiles.Length];
                    for (int index = 0; index < strFiles.Length; index++)
                    {
                        strFilePath[index] = this.strフォルダ名 + strFiles[index];
                        if (File.Exists(strFilePath[index]))
                        {
                            try
                            {
                                if (TJAPlayer3.r現在のステージ.eStageID == CStage.EStage.SongLoading)//起動時に重たくなってしまう問題の修正用
                                    this.LyricFileParser(strFilePath[index], index);
                                this.bLyrics = true;
                                this.usingLyricsFile = true;
                            }
                            catch
                            {
                                Console.WriteLine("lrcファイルNo.{0}の読み込みに失敗しましたが、", index);
                                Console.WriteLine("処理を続行します。");
                            }
                        }
                    }
                }
            }
            if (this.nScoreModeTmp == 99)
            {
                //2017.01.28 DD 
                this.nScoreModeTmp = TJAPlayer3.ConfigIni.nScoreMode;
            }
        }
        /// <summary>
        /// 指定した文字が数値かを返すメソッド
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool bIsNumber(char Char)
        {
            if ((Char >= '0') && (Char <= '9'))
                return true;
            else
                return false;
        }

        /// <summary>
        /// string型からint型に変換する。
        /// TJAP2から持ってきた。
        /// </summary>
        private int CharConvertNote(string str)
        {
            return (NotesManager.GetNoteValueFromChar(str));            
        }

        private int strConvertCourse(string str)
        {
            //2016.08.24 kairera0467
            //正規表現を使っているため、easyでもEASYでもOK。

            // 小文字大文字区別しない正規表現で仮対応。 (AioiLight)
            // 相変わらず原始的なやり方だが、正常に動作した。
            string[] Matchptn = new string[7] { "easy", "normal", "hard", "oni", "edit", "tower", "dan" };
            for (int i = 0; i < Matchptn.Length; i++)
            {
                if (Regex.IsMatch(str, Matchptn[i], RegexOptions.IgnoreCase))
                {
                    return i;
                }
            }

            switch (str)
            {
                case "0":
                    return 0;
                case "1":
                    return 1;
                case "2":
                    return 2;
                case "3":
                    return 3;
                case "4":
                    return 4;
                case "5":
                    return 5;
                case "6":
                    return 6;
                default:
                    return 3;
            }
        }

        /// <summary>
        /// Lyricファイルのパースもどき
        /// 自力で作ったので、うまくパースしてくれないかも
        /// </summary>
        /// <param name="strFilePath">lrcファイルのパス</param>
		private void LyricFileParser(string strFilePath, int ordnumber)//lrcファイルのパース用
        {
            string str = CJudgeTextEncoding.ReadTextFile(strFilePath);
            var strSplit後 = str.Split(this.dlmtEnter, StringSplitOptions.RemoveEmptyEntries);
            Regex timeRegex = new Regex(@"^(\[)(\d{2})(:)(\d{2})([:.])(\d{2})(\])", RegexOptions.Multiline | RegexOptions.Compiled);
            Regex timeRegexO = new Regex(@"^(\[)(\d{2})(:)(\d{2})(\])", RegexOptions.Multiline | RegexOptions.Compiled);
            List<long> list;
            for (int i = 0; i < strSplit後.Length; i++)
            {
                list = new List<long>();
                if (!String.IsNullOrEmpty(strSplit後[i]))
                {
                    if (strSplit後[i].StartsWith("["))
                    {
                        Match timestring = timeRegex.Match(strSplit後[i]), timestringO = timeRegexO.Match(strSplit後[i]);
                        while (timestringO.Success || timestring.Success)
                        {
                            long time;
                            if (timestring.Success)
                            {
                                time = Int32.Parse(timestring.Groups[2].Value) * 60000 + Int32.Parse(timestring.Groups[4].Value) * 1000 + Int32.Parse(timestring.Groups[6].Value) * 10;
                                strSplit後[i] = strSplit後[i].Remove(0, 10);
                            }
                            else if (timestringO.Success)
                            {
                                time = Int32.Parse(timestringO.Groups[2].Value) * 60000 + Int32.Parse(timestringO.Groups[4].Value) * 1000;
                                strSplit後[i] = strSplit後[i].Remove(0, 7);
                            }
                            else
                                break;
                            list.Add(time);
                            timestring = timeRegex.Match(strSplit後[i]);
                            timestringO = timeRegexO.Match(strSplit後[i]);
                        }
                        strSplit後[i] = strSplit後[i].Replace("\r", "").Replace("\n", "");

                        for (int listindex = 0; listindex < list.Count; listindex++)
                        {
                            STLYRIC stlrc;
                            stlrc.Text = strSplit後[i];
                            stlrc.TextTex = this.pf歌詞フォント.DrawText(strSplit後[i], TJAPlayer3.Skin.Game_Lyric_ForeColor, TJAPlayer3.Skin.Game_Lyric_BackColor, null, 30);
                            stlrc.Time = list[listindex];
                            stlrc.index = ordnumber;
                            this.listLyric2.Add(stlrc);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 複素数のパースもどき
        /// </summary>
        private void tParsedComplexNumber(string strScroll, ref double[] dbScroll)
        {
            /*
            bool bFirst = true; //最初の数値か
            bool bUse = false; //数値扱い中
            string[] arScroll = new string[2];
            char[] c = strScroll.ToCharArray();
            //1.0-1.0i
            for (int i = 0; i < strScroll.Length; i++)
            {
                if (bFirst)
                    arScroll[0] += c[i];
                else
                    arScroll[1] += c[i];

                //次の文字が'i'なら脱出。
                if (c[i + 1] == 'i')
                    break;
                else if (c[i + 1] == '-' || c[i + 1] == '+')
                    bFirst = false;

            }
            dbScroll[0] = Convert.ToDouble(arScroll[0]);
            dbScroll[1] = Convert.ToDouble(arScroll[1]);
            */

            var cpx = strScroll.ParseComplex();
            dbScroll[0] = cpx[0];
            dbScroll[1] = cpx[1];
            return;
        }

        private void tSetSenotes()
        {
            #region[ list作成 ]
            //ひとまずチップだけのリストを作成しておく。
            List<CDTX.CChip> list音符のみのリスト;
            list音符のみのリスト = new List<CChip>();
            int nCount = 0;
            int dkdkCount = 0;

            foreach (CChip chip in this.listChip)
            {
                if (NotesManager.IsCommonNote(chip))
                {
                    list音符のみのリスト.Add(chip);
                }
            }
            #endregion

            //時間判定は、「次のチップの発声時刻」から「現在(過去)のチップの発声時刻」で引く必要がある。
            //逆にしてしまうと計算がとてつもないことになるので注意。

            try
            {
                //this.tSenotes_Core( list音符のみのリスト );
                this.tSenotes_Core_V2(list音符のみのリスト, true);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                Trace.TraceError("例外が発生しましたが処理を継続します。 (b67473e4-1930-44f1-b320-4ead5786e74c)");
            }

        }

        /// <summary>
        /// 譜面分岐がある場合はこちらを使う
        /// </summary>
        private void tSetSenotes_branch()
        {
            #region[ list作成 ]
            //ひとまずチップだけのリストを作成しておく。
            List<CDTX.CChip> list音符のみのリスト;
            List<CDTX.CChip> list普通譜面のみのリスト;
            List<CDTX.CChip> list玄人譜面のみのリスト;
            List<CDTX.CChip> list達人譜面のみのリスト;
            list音符のみのリスト = new List<CChip>();
            list普通譜面のみのリスト = new List<CChip>();
            list玄人譜面のみのリスト = new List<CChip>();
            list達人譜面のみのリスト = new List<CChip>();
            int nCount = 0;
            int dkdkCount = 0;

            foreach (CChip chip in this.listChip)
            {
                if (NotesManager.IsCommonNote(chip))
                {
                    list音符のみのリスト.Add(chip);

                    switch (chip.nコース)
                    {
                        case ECourse.eNormal:
                            list普通譜面のみのリスト.Add(chip);
                            break;
                        case ECourse.eExpert:
                            list玄人譜面のみのリスト.Add(chip);
                            break;
                        case ECourse.eMaster:
                            list達人譜面のみのリスト.Add(chip);
                            break;
                    }
                }
            }
            #endregion

            //forで処理。
            for (int n = 0; n < 3; n++)
            {
                switch (n)
                {
                    case 0:
                        list音符のみのリスト = list普通譜面のみのリスト;
                        break;
                    case 1:
                        list音符のみのリスト = list玄人譜面のみのリスト;
                        break;
                    case 2:
                        list音符のみのリスト = list達人譜面のみのリスト;
                        break;
                }

                //this.tSenotes_Core( list音符のみのリスト );
                this.tSenotes_Core_V2(list音符のみのリスト, true);
            }

        }

        /// <summary>
        /// コア部分Ver2。TJAP2から移植しただけ。
        /// </summary>
        /// <param name="list音符のみのリスト"></param>
        private void tSenotes_Core_V2(List<CChip> list音符のみのリスト, bool ignoreSENote = false)
        {
            const int DATA = 3;
            int doco_count = 0;
            int[] sort = new int[7];
            double[] time = new double[7];
            double[] scroll = new double[7];
            double time_tmp;

            for (int i = 0; i < list音符のみのリスト.Count; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (i + (j - 3) < 0)
                    {
                        sort[j] = -1;
                        time[j] = -1000000000;
                        scroll[j] = 1.0;
                    }
                    else if (i + (j - 3) >= list音符のみのリスト.Count)
                    {
                        sort[j] = -1;
                        time[j] = 1000000000;
                        scroll[j] = 1.0;
                    }
                    else
                    {
                        sort[j] = list音符のみのリスト[i + (j - 3)].nチャンネル番号;
                        time[j] = list音符のみのリスト[i + (j - 3)].fBMSCROLLTime;
                        scroll[j] = list音符のみのリスト[i + (j - 3)].dbSCROLL;
                    }
                }
                time_tmp = time[DATA];
                for (int j = 0; j < 7; j++)
                {
                    time[j] = (time[j] - time_tmp) * scroll[j];
                    if (time[j] < 0) time[j] *= -1;
                }

                if (ignoreSENote && list音符のみのリスト[i].IsFixedSENote) continue;

                switch (list音符のみのリスト[i].nチャンネル番号)
                {
                    case 0x11:

                        //（左2より離れている｜）_右2_右ドン_右右4_右右ドン…
                        if ((time[DATA - 1] > 2/* || (sort[DATA-1] != 1 && time[DATA-1] >= 2 && time[DATA-2] >= 4 && time[DATA-3] <= 5)*/) && time[DATA + 1] == 2 && sort[DATA + 1] == 1 && time[DATA + 2] == 4 && sort[DATA + 2] == 0x11 && time[DATA + 3] == 6 && sort[DATA + 3] == 0x11)
                        {
                            list音符のみのリスト[i].nSenote = 1;
                            doco_count = 1;
                            break;
                        }
                        //ドコドコ中_左2_右2_右ドン
                        else if (doco_count != 0 && time[DATA - 1] == 2 && time[DATA + 1] == 2 && (sort[DATA + 1] == 0x11 || sort[DATA + 1] == 0x11))
                        {
                            if (doco_count % 2 == 0)
                                list音符のみのリスト[i].nSenote = 1;
                            else
                                list音符のみのリスト[i].nSenote = 2;
                            doco_count++;
                            break;
                        }
                        else
                        {
                            doco_count = 0;
                        }

                        //8分ドコドン
                        if ((time[DATA - 2] >= 4.1 && time[DATA - 1] == 2 && time[DATA + 1] == 2 && time[DATA + 2] >= 4.1) && (sort[DATA - 1] == 0x11 && sort[DATA + 1] == 0x11))
                        {
                            if (list音符のみのリスト[i].dbBPM >= 120.0)
                            {
                                list音符のみのリスト[i - 1].nSenote = 1;
                                list音符のみのリスト[i].nSenote = 2;
                                list音符のみのリスト[i + 1].nSenote = 0;
                                break;
                            }
                            else if (list音符のみのリスト[i].dbBPM < 120.0)
                            {
                                list音符のみのリスト[i - 1].nSenote = 0;
                                list音符のみのリスト[i].nSenote = 0;
                                list音符のみのリスト[i + 1].nSenote = 0;
                                break;
                            }
                        }

                        //BPM120以下のみ
                        //8分間隔の「ドドド」→「ドンドンドン」

                        if (time[DATA - 1] >= 2 && time[DATA + 1] >= 2)
                        {
                            if (list音符のみのリスト[i].dbBPM < 120.0)
                            {
                                list音符のみのリスト[i].nSenote = 0;
                                break;
                            }
                        }

                        //ドコドコドン
                        if (time[DATA - 3] >= 3.4 && time[DATA - 2] == 2 && time[DATA - 1] == 1 && time[DATA + 1] == 1 && time[DATA + 2] == 2 && time[DATA + 3] >= 3.4 && sort[DATA - 2] == 0x11 && sort[DATA - 1] == 0x11 && sort[DATA + 1] == 0x11 && sort[DATA + 2] == 0x11)
                        {
                            list音符のみのリスト[i - 2].nSenote = 1;
                            list音符のみのリスト[i - 1].nSenote = 2;
                            list音符のみのリスト[i + 0].nSenote = 1;
                            list音符のみのリスト[i + 1].nSenote = 2;
                            list音符のみのリスト[i + 2].nSenote = 0;
                            i += 2;
                            //break;
                        }
                        //ドコドン
                        else if (time[DATA - 2] >= 2.4 && time[DATA - 1] == 1 && time[DATA + 1] == 1 && time[DATA + 2] >= 2.4 && sort[DATA - 1] == 0x11 && sort[DATA + 1] == 0x11)
                        {
                            list音符のみのリスト[i].nSenote = 2;
                        }
                        //右の音符が2以上離れている
                        else if (time[DATA + 1] > 2)
                        {
                            list音符のみのリスト[i].nSenote = 0;
                        }
                        //右の音符が1.4以上_左の音符が1.4以内
                        else if (time[DATA + 1] >= 1.4 && time[DATA - 1] <= 1.4)
                        {
                            list音符のみのリスト[i].nSenote = 0;
                        }
                        //右の音符が2以上_右右の音符が3以内
                        else if (time[DATA + 1] >= 2 && time[DATA + 2] <= 3)
                        {
                            list音符のみのリスト[i].nSenote = 0;
                        }
                        //右の音符が2以上_大音符
                        else if (time[DATA + 1] >= 2 && (sort[DATA + 1] == 0x13 || sort[DATA + 1] == 0x14))
                        {
                            list音符のみのリスト[i].nSenote = 0;
                        }
                        else
                        {
                            list音符のみのリスト[i].nSenote = 1;
                        }
                        break;
                    case 0x12:
                        doco_count = 0;

                        //BPM120以下のみ
                        //8分間隔の「ドドド」→「ドンドンドン」
                        if (time[DATA - 1] == 2 && time[DATA + 1] == 2)
                        {
                            if (list音符のみのリスト[i - 1].dbBPM < 120.0 && list音符のみのリスト[i].dbBPM < 120.0 && list音符のみのリスト[i + 1].dbBPM < 120.0)
                            {
                                list音符のみのリスト[i].nSenote = 3;
                                break;
                            }
                        }

                        //右の音符が2以上離れている
                        if (time[DATA + 1] > 2)
                        {
                            list音符のみのリスト[i].nSenote = 3;
                        }
                        //右の音符が1.4以上_左の音符が1.4以内
                        else if (time[DATA + 1] >= 1.4 && time[DATA - 1] <= 1.4)
                        {
                            list音符のみのリスト[i].nSenote = 3;
                        }
                        //右の音符が2以上_右右の音符が3以内
                        else if (time[DATA + 1] >= 2 && time[DATA + 2] <= 3)
                        {
                            list音符のみのリスト[i].nSenote = 3;
                        }
                        //右の音符が2以上_大音符
                        else if (time[DATA + 1] >= 2 && (sort[DATA + 1] == 0x13 || sort[DATA + 1] == 0x14))
                        {
                            list音符のみのリスト[i].nSenote = 3;
                        }
                        else
                        {
                            list音符のみのリスト[i].nSenote = 4;
                        }
                        break;
                    default:
                        doco_count = 0;
                        break;
                }
            }
        }

        /// <summary>
        /// サウンドミキサーにサウンドを登録_削除する時刻を事前に算出する
        /// </summary>
        public void PlanToAddMixerChannel()
        {
            if (TJAPlayer3.SoundManager.GetCurrentSoundDeviceType() == "DirectSound") // DShowでの再生の場合はミキシング負荷が高くないため、
            {                                                                       // チップのライフタイム管理を行わない
                return;
            }

            List<CChip> listAddMixerChannel = new List<CChip>(128); ;
            List<CChip> listRemoveMixerChannel = new List<CChip>(128);
            List<CChip> listRemoveTiming = new List<CChip>(128);

            foreach (CChip pChip in listChip)
            {
                switch (pChip.nチャンネル番号)
                {
                    // BGM, 演奏チャネル, 不可視サウンド, フィルインサウンド, 空打ち音はミキサー管理の対象
                    // BGM:
                    case 0x01:
                        // Dr演奏チャネル
                        //case 0x11:	case 0x12:	case 0x13:	case 0x14:	case 0x15:	case 0x16:	case 0x17:	case 0x18:	case 0x19:	case 0x1A:  case 0x1B:  case 0x1C:
                        // Gt演奏チャネル
                        //case 0x20:	case 0x21:	case 0x22:	case 0x23:	case 0x24:	case 0x25:	case 0x26:	case 0x27:	case 0x28:
                        // Bs演奏チャネル
                        //case 0xA0:	case 0xA1:	case 0xA2:	case 0xA3:	case 0xA4:	case 0xA5:	case 0xA6:	case 0xA7:	case 0xA8:
                        // Dr不可視チップ
                        //case 0x31:	case 0x32:	case 0x33:	case 0x34:	case 0x35:	case 0x36:	case 0x37:
                        //case 0x38:	case 0x39:	case 0x3A:
                        // Dr/Gt/Bs空打ち
                        //case 0xB1:	case 0xB2:	case 0xB3:	case 0xB4:	case 0xB5:	case 0xB6:	case 0xB7:	case 0xB8:
                        //case 0xB9:	case 0xBA:	case 0xBB:	case 0xBC:
                        // フィルインサウンド
                        //case 0x1F:	case 0x2F:	case 0xAF:
                        // 自動演奏チップ
                        //case 0x61:	case 0x62:	case 0x63:	case 0x64:	case 0x65:	case 0x66:	case 0x67:	case 0x68:	case 0x69:
                        //case 0x70:	case 0x71:	case 0x72:	case 0x73:	case 0x74:	case 0x75:	case 0x76:	case 0x77:	case 0x78:	case 0x79:
                        //case 0x80:	case 0x81:	case 0x82:	case 0x83:	case 0x84:	case 0x85:	case 0x86:	case 0x87:	case 0x88:	case 0x89:
                        //case 0x90:	case 0x91:	case 0x92:

                        #region [ 発音1秒前のタイミングを算出 ]
                        int n発音前余裕ms = 1000, n発音後余裕ms = 800;
                        {
                            int ch = pChip.nチャンネル番号 >> 4;
                            if (ch == 0x02 || ch == 0x0A)
                            {
                                n発音前余裕ms = 800;
                                n発音前余裕ms = 500;
                            }
                            if (ch == 0x06 || ch == 0x07 || ch == 0x08 || ch == 0x09)
                            {
                                n発音前余裕ms = 200;
                                n発音前余裕ms = 500;
                            }
                        }
                        #endregion
                        #region [ BGMチップならば即ミキサーに追加 ]
                        //if ( pChip.nチャンネル番号 == 0x01 )	// BGMチップは即ミキサーに追加
                        //{
                        //    if ( listWAV.ContainsKey( pChip.n整数値_内部番号 ) )
                        //    {
                        //        CDTX.CWAV wc = CDTXMania.DTX.listWAV[ pChip.n整数値_内部番号 ];
                        //        if ( wc.rSound[ 0 ] != null )
                        //        {
                        //            CDTXMania.Sound管理.AddMixer( wc.rSound[ 0 ] );	// BGMは多重再生しない仕様としているので、1個目だけミキサーに登録すればよい
                        //        }
                        //    }
                        //}
                        #endregion
                        #region [ 発音1秒前のタイミングを算出 ]
                        int nAddMixer時刻ms, nAddMixer位置 = 0;
                        //Debug.WriteLine("==================================================================");
                        //Debug.WriteLine( "Start: ch=" + pChip.nチャンネル番号.ToString("x2") + ", nWAV番号=" + pChip.n整数値 + ", time=" + pChip.n発声時刻ms + ", lasttime=" + listChip[ listChip.Count - 1 ].n発声時刻ms );
                        t発声時刻msと発声位置を取得する(pChip.n発声時刻ms - n発音前余裕ms, out nAddMixer時刻ms, out nAddMixer位置);
                        //Debug.WriteLine( "nAddMixer時刻ms=" + nAddMixer時刻ms + ",nAddMixer位置=" + nAddMixer位置 );

                        CChip c_AddMixer = new CChip()
                        {
                            nチャンネル番号 = 0xDA,
                            n整数値 = pChip.n整数値,
                            n整数値_内部番号 = pChip.n整数値_内部番号,
                            n発声時刻ms = nAddMixer時刻ms,
                            n発声位置 = nAddMixer位置,
                            b演奏終了後も再生が続くチップである = false
                        };
                        listAddMixerChannel.Add(c_AddMixer);
                        //Debug.WriteLine("listAddMixerChannel:" );
                        //DebugOut_CChipList( listAddMixerChannel );
                        #endregion

                        int duration = 0;
                        if (listWAV.TryGetValue(pChip.n整数値_内部番号, out CDTX.CWAV wc))
                        {
                            double _db再生速度 = (TJAPlayer3.DTXVmode.Enabled) ? this.dbDTXVPlaySpeed : this.db再生速度;
                            duration = (wc.rSound[0] == null) ? 0 : (int)(wc.rSound[0].TotalPlayTime / _db再生速度); // #23664 durationに再生速度が加味されておらず、低速再生でBGMが途切れる問題を修正 (発声時刻msは、DTX読み込み時に再生速度加味済)
                        }
                        //Debug.WriteLine("duration=" + duration );
                        int n新RemoveMixer時刻ms, n新RemoveMixer位置;
                        t発声時刻msと発声位置を取得する(pChip.n発声時刻ms + duration + n発音後余裕ms, out n新RemoveMixer時刻ms, out n新RemoveMixer位置);
                        //Debug.WriteLine( "n新RemoveMixer時刻ms=" + n新RemoveMixer時刻ms + ",n新RemoveMixer位置=" + n新RemoveMixer位置 );
                        if (n新RemoveMixer時刻ms < pChip.n発声時刻ms + duration)   // 曲の最後でサウンドが切れるような場合は
                        {
                            CChip c_AddMixer_noremove = c_AddMixer;
                            c_AddMixer_noremove.b演奏終了後も再生が続くチップである = true;
                            listAddMixerChannel[listAddMixerChannel.Count - 1] = c_AddMixer_noremove;
                            //continue;												// 発声位置の計算ができないので、Mixer削除をあきらめる___のではなく
                            // #32248 2013.10.15 yyagi 演奏終了後も再生を続けるチップであるというフラグをpChip内に立てる
                            break;
                        }
                        #region [ 未使用コード ]
                        //if ( n新RemoveMixer時刻ms < pChip.n発声時刻ms + duration )	// 曲の最後でサウンドが切れるような場合
                        //{
                        //    n新RemoveMixer時刻ms = pChip.n発声時刻ms + duration;
                        //    // 「位置」は比例計算で求めてお茶を濁す...このやり方だと誤動作したため対応中止
                        //    n新RemoveMixer位置 = listChip[ listChip.Count - 1 ].n発声位置 * n新RemoveMixer時刻ms / listChip[ listChip.Count - 1 ].n発声時刻ms;
                        //}
                        #endregion

                        #region [ 発音終了2秒後にmixerから削除するが、その前に再発音することになるのかを確認(再発音ならmixer削除タイミングを延期) ]
                        int n整数値 = pChip.n整数値;
                        int index = listRemoveTiming.FindIndex(
                            delegate (CChip cchip) { return cchip.n整数値 == n整数値; }
                        );
                        //Debug.WriteLine( "index=" + index );
                        if (index >= 0)                                                 // 過去に同じチップで発音中のものが見つかった場合
                        {                                                                   // 過去の発音のmixer削除を確定させるか、延期するかの2択。
                            int n旧RemoveMixer時刻ms = listRemoveTiming[index].n発声時刻ms;
                            int n旧RemoveMixer位置 = listRemoveTiming[index].n発声位置;

                            //Debug.WriteLine( "n旧RemoveMixer時刻ms=" + n旧RemoveMixer時刻ms + ",n旧RemoveMixer位置=" + n旧RemoveMixer位置 );
                            if (pChip.n発声時刻ms - n発音前余裕ms <= n旧RemoveMixer時刻ms)  // mixer削除前に、同じ音の再発音がある場合は、
                            {                                                                   // mixer削除時刻を遅延させる(if-else後に行う)
                                                                                                //Debug.WriteLine( "remove TAIL of listAddMixerChannel. TAIL INDEX=" + listAddMixerChannel.Count );
                                                                                                //DebugOut_CChipList( listAddMixerChannel );
                                listAddMixerChannel.RemoveAt(listAddMixerChannel.Count - 1);    // また、同じチップ音の「mixerへの再追加」は削除する
                                                                                                //Debug.WriteLine( "removed result:" );
                                                                                                //DebugOut_CChipList( listAddMixerChannel );
                            }
                            else                                                            // 逆に、時間軸上、mixer削除後に再発音するような流れの場合は
                            {
                                //Debug.WriteLine( "Publish the value(listRemoveTiming[index] to listRemoveMixerChannel." );
                                listRemoveMixerChannel.Add(listRemoveTiming[index]);    // mixer削除を確定させる
                                                                                        //Debug.WriteLine( "listRemoveMixerChannel:" );
                                                                                        //DebugOut_CChipList( listRemoveMixerChannel );
                                                                                        //listRemoveTiming.RemoveAt( index );
                            }
                            CChip c = new CChip()                                           // mixer削除時刻を更新(遅延)する
                            {
                                nチャンネル番号 = 0xDB,
                                n整数値 = listRemoveTiming[index].n整数値,
                                n整数値_内部番号 = listRemoveTiming[index].n整数値_内部番号,
                                n発声時刻ms = n新RemoveMixer時刻ms,
                                n発声位置 = n新RemoveMixer位置
                            };
                            listRemoveTiming[index] = c;
                            //listRemoveTiming[ index ].n発声時刻ms = n新RemoveMixer時刻ms;	// mixer削除時刻を更新(遅延)する
                            //listRemoveTiming[ index ].n発声位置 = n新RemoveMixer位置;
                            //Debug.WriteLine( "listRemoveTiming: modified" );
                            //DebugOut_CChipList( listRemoveTiming );
                        }
                        else                                                                // 過去に同じチップを発音していないor
                        {                                                                   // 発音していたが既にmixer削除確定していたなら
                            CChip c = new CChip()                                           // 新しくmixer削除候補として追加する
                            {
                                nチャンネル番号 = 0xDB,
                                n整数値 = pChip.n整数値,
                                n整数値_内部番号 = pChip.n整数値_内部番号,
                                n発声時刻ms = n新RemoveMixer時刻ms,
                                n発声位置 = n新RemoveMixer位置
                            };
                            //Debug.WriteLine( "Add new chip to listRemoveMixerTiming: " );
                            //Debug.WriteLine( "ch=" + c.nチャンネル番号.ToString( "x2" ) + ", nWAV番号=" + c.n整数値 + ", time=" + c.n発声時刻ms + ", lasttime=" + listChip[ listChip.Count - 1 ].n発声時刻ms );
                            listRemoveTiming.Add(c);
                            //Debug.WriteLine( "listRemoveTiming:" );
                            //DebugOut_CChipList( listRemoveTiming );
                        }
                        #endregion
                        break;
                }
            }
            //Debug.WriteLine("==================================================================");
            //Debug.WriteLine( "Result:" );
            //Debug.WriteLine( "listAddMixerChannel:" );
            //DebugOut_CChipList( listAddMixerChannel );
            //Debug.WriteLine( "listRemoveMixerChannel:" );
            //DebugOut_CChipList( listRemoveMixerChannel );
            //Debug.WriteLine( "listRemoveTiming:" );
            //DebugOut_CChipList( listRemoveTiming );
            //Debug.WriteLine( "==================================================================" );

            listChip.AddRange(listAddMixerChannel);
            listChip.AddRange(listRemoveMixerChannel);
            listChip.AddRange(listRemoveTiming);
            listChip.Sort();
        }
        private void DebugOut_CChipList(List<CChip> c)
        {
            //Debug.WriteLine( "Count=" + c.Count );
            for (int i = 0; i < c.Count; i++)
            {
                Debug.WriteLine(i + ": ch=" + c[i].nチャンネル番号.ToString("x2") + ", WAV番号=" + c[i].n整数値 + ", time=" + c[i].n発声時刻ms);
            }
        }
        private bool t発声時刻msと発声位置を取得する(int n希望発声時刻ms, out int n新発声時刻ms, out int n新発声位置)
        {
            // 発声時刻msから発声位置を逆算することはできないため、近似計算する。
            // 具体的には、希望発声位置前後の2つのチップの発声位置の中間を取る。

            if (n希望発声時刻ms < 0)
            {
                n希望発声時刻ms = 0;
            }
            //else if ( n希望発声時刻ms > listChip[ listChip.Count - 1 ].n発声時刻ms )		// BGMの最後の余韻を殺してしまうので、この条件は外す
            //{
            //    n希望発声時刻ms = listChip[ listChip.Count - 1 ].n発声時刻ms;
            //}

            int index_min = -1, index_max = -1;
            for (int i = 0; i < listChip.Count; i++)        // 希望発声位置前後の「前」の方のチップを検索
            {
                if (listChip[i].n発声時刻ms >= n希望発声時刻ms)
                {
                    index_min = i;
                    break;
                }
            }
            if (index_min < 0)  // 希望発声時刻に至らずに曲が終了してしまう場合
            {
                // listの最終項目の時刻をそのまま使用する
                //___のではダメ。BGMが尻切れになる。
                // そこで、listの最終項目の発声時刻msと発生位置から、希望発声時刻に相当する希望発声位置を比例計算して求める。
                //n新発声時刻ms = n希望発声時刻ms;
                //n新発声位置 = listChip[ listChip.Count - 1 ].n発声位置 * n希望発声時刻ms / listChip[ listChip.Count - 1 ].n発声時刻ms;
                n新発声時刻ms = listChip[listChip.Count - 1].n発声時刻ms;
                n新発声位置 = listChip[listChip.Count - 1].n発声位置;
                return false;
            }
            index_max = index_min + 1;
            if (index_max >= listChip.Count)
            {
                index_max = index_min;
            }
            n新発声時刻ms = (listChip[index_max].n発声時刻ms + listChip[index_min].n発声時刻ms) / 2;
            n新発声位置 = (listChip[index_max].n発声位置 + listChip[index_min].n発声位置) / 2;

            return true;
        }

        public void SwapGuitarBassInfos()
        {
        }

        // SwapGuitarBassInfos_AutoFlags()は、CDTXからCConfigIniに移動。

        // CActivity 実装
        private CCachedFontRenderer pf歌詞フォント;
        public override void Activate()
        {
            if (TJAPlayer3.r現在のステージ.eStageID == CStage.EStage.SongLoading)
            {
                //まさかこれが原因で曲の読み込みが停止するとは思わなかった...
                //どういうことかというとスキンを読み込むときに...いや厳密には
                //RefleshSkinを呼び出した後一回Disposeしてnullにして解放(その後にまたインスタンスを作成する)するんだけど
                //その時にここでTJAPlayer3.Skinを参照して例外が出ていたんだ....
                //いやいや! なんでTJAPlayer3.Skinをnullにした瞬間に参照されるんだ!と思った方もいるかもしれないですが
                //実は曲の読み込みはマルチスレッドで実行されているのでnullにした瞬間に参照される可能性も十分にある
                //それならアプリが終了するんじゃないかと思ったのだけどtryを使ってい曲の読み込みを続行していた...
                //いやーマルチスレッドって難しいね!
                if (!string.IsNullOrEmpty(TJAPlayer3.Skin.Game_Lyric_FontName))
                {
                    this.pf歌詞フォント = new CCachedFontRenderer(TJAPlayer3.Skin.Game_Lyric_FontName, TJAPlayer3.Skin.Game_Lyric_FontSize);
                }
                else
                {
                    this.pf歌詞フォント = new CCachedFontRenderer(CFontRenderer.DefaultFontName, TJAPlayer3.Skin.Game_Lyric_FontSize);
                }
            }
            this.listWAV = new Dictionary<int, CWAV>();
            this.listBPM = new Dictionary<int, CBPM>();
            this.listSCROLL = new Dictionary<int, CSCROLL>();
            this.listSCROLL_Normal = new Dictionary<int, CSCROLL>();
            this.listSCROLL_Expert = new Dictionary<int, CSCROLL>();
            this.listSCROLL_Master = new Dictionary<int, CSCROLL>();
            this.listJPOSSCROLL = new Dictionary<int, CJPOSSCROLL>();
            this.listDELAY = new Dictionary<int, CDELAY>();
            this.listBRANCH = new Dictionary<int, CBRANCH>();
			this.listVD = new Dictionary<int, CVideoDecoder>();
            this.listChip = new List<CChip>();
            this.listChip_Branch = new List<CChip>[3];
            this.listChip_Branch[0] = new List<CChip>();
            this.listChip_Branch[1] = new List<CChip>();
            this.listChip_Branch[2] = new List<CChip>();
            this.listBalloon = new List<int>();
            this.listBalloon_Normal = new List<int>();
            this.listBalloon_Expert = new List<int>();
            this.listBalloon_Master = new List<int>();
            this.listLine = new List<CLine>(); 
            this.listLyric = new List<SKBitmap>();
            this.listLyric2 = new List<STLYRIC>();
            this.List_DanSongs = new List<DanSongs>();
            this.listObj = new Dictionary<string, CSongObject>();
            this.listTextures = new Dictionary<string, CTexture>();
            this.listOriginalTextures = new Dictionary<string, CTexture>();
            this.currentObjAnimations = new Dictionary<string, CChip>();
            base.Activate();
        }
        public override void DeActivate()
        {
            if (this.listWAV != null)
            {
                foreach (CWAV cwav in this.listWAV.Values)
                {
                    cwav.Dispose();
                }
                this.listWAV = null;
            }
			if (this.listVD != null)
			{
				foreach (CVideoDecoder cvd in this.listVD.Values)
				{
					cvd.Dispose();
				}
				this.listVD = null;
			}
            if (this.listBPM != null)
            {
                this.listBPM.Clear();
                this.listBPM = null;
            }
            if (this.listDELAY != null)
            {
                this.listDELAY.Clear();
                this.listDELAY = null;
            }
            if (this.listBRANCH != null)
            {
                this.listBRANCH.Clear();
                this.listBRANCH = null;
            }
            if (this.listSCROLL != null)
            {
                this.listSCROLL.Clear();
                this.listSCROLL = null;
            }

            if (this.listSCROLL_Normal != null)
            {
                this.listSCROLL_Normal.Clear();
                this.listSCROLL_Normal = null;
            }
            if (this.listSCROLL_Expert != null)
            {
                this.listSCROLL_Expert.Clear();
                this.listSCROLL_Expert = null;
            }
            if (this.listSCROLL_Master != null)
            {
                this.listSCROLL_Master.Clear();
                this.listSCROLL_Master = null;
            }
            if (this.listJPOSSCROLL != null)
            {
                this.listJPOSSCROLL.Clear();
                this.listJPOSSCROLL = null;
            }
            if (this.List_DanSongs != null)
            {
                this.List_DanSongs.Clear();
                this.List_DanSongs = null;
            }

            if (this.listChip != null)
            {
                this.listChip.Clear();
            }

            if (this.listBalloon != null)
            {
                this.listBalloon.Clear();
            }
            if (this.listBalloon_Normal != null)
            {
                this.listBalloon_Normal.Clear();
            }
            if (this.listBalloon_Expert != null)
            {
                this.listBalloon_Expert.Clear();
            }
            if (this.listBalloon_Master != null)
            {
                this.listBalloon_Master.Clear();
            }
            if (this.listLyric != null)
            {
                this.listLyric.Clear();
            }
            if (this.listLyric2 != null)
            {
                this.listLyric2.Clear();
            }



            if (this.listObj != null)
            {
                foreach (KeyValuePair<string, CSongObject> pair in this.listObj)
                {
                    pair.Value.tDispose();
                }
                this.listObj.Clear();
            }

            if (this.listOriginalTextures != null)
            {
                foreach (KeyValuePair<string, CTexture> pair in this.listOriginalTextures)
                {
                    string txPath = pair.Key;
                    CTexture originalTx = pair.Value;
                    TJAPlayer3.Tx.trackedTextures.TryGetValue(txPath, out CTexture oldTx);

                    if (oldTx != originalTx)
                    {
                        oldTx.UpdateTexture(originalTx, originalTx.sz画像サイズ.Width, originalTx.sz画像サイズ.Height);
                    }
                }
                this.listOriginalTextures.Clear();
            }

            if (this.listTextures != null)
            {
                foreach (KeyValuePair<string, CTexture> pair in this.listTextures)
                {
                    pair.Value.Dispose();
                }
                this.listTextures.Clear();
            }

            base.DeActivate();
        }
        public override void CreateManagedResource()
        {
            if (!base.IsDeActivated)
            {
                this.tAVIの読み込み();
                base.CreateManagedResource();
            }
        }
        public override void ReleaseManagedResource()
        {
            if (!base.IsDeActivated)
            {
                if (this.listVD != null)
                {
                    foreach (CVideoDecoder cvd in this.listVD.Values)
                    {
                        cvd.Dispose();
                    }
                    this.listVD = null;
                }
                TJAPlayer3.tDisposeSafely(ref this.pf歌詞フォント);
                base.ReleaseManagedResource();
            }
        }

        // その他

        #region [ private ]
        //-----------------
        /// <summary>
        /// <para>GDAチャンネル番号に対応するDTXチャンネル番号。</para>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct STGDAPARAM
        {
            public string strGDAのチャンネル文字列;
            public int nDTXのチャンネル番号;

            public STGDAPARAM(string strGDAのチャンネル文字列, int nDTXのチャンネル番号)     // 2011.1.1 yyagi 構造体のコンストラクタ追加(初期化簡易化のため)
            {
                this.strGDAのチャンネル文字列 = strGDAのチャンネル文字列;
                this.nDTXのチャンネル番号 = nDTXのチャンネル番号;
            }
        }

        private readonly STGDAPARAM[] stGDAParam;
        private bool bヘッダのみ;
        private Stack<bool> bstackIFからENDIFをスキップする;

        private int n現在の行数;
        private int n現在の乱数;

        private int nPolyphonicSounds = 4;                          // #28228 2012.5.1 yyagi

        private int n内部番号BPM1to;
        private int n内部番号SCROLL1to;
        private int n内部番号JSCROLL1to;
        private int n内部番号DELAY1to;
        private int n内部番号BRANCH1to;
        private int n内部番号WAV1to;
        private int[] n無限管理BPM;
        private int[] n無限管理PAN;
        private int[] n無限管理SIZE;
        private int[] n無限管理WAV;
        private int[] nRESULTIMAGE用優先順位;
        private int[] nRESULTMOVIE用優先順位;
        private int[] nRESULTSOUND用優先順位;

        private CChip currentCamVMoveChip;
        private CChip currentCamHMoveChip;
        private CChip currentCamRotateChip;
        private CChip currentCamZoomChip;
        private CChip currentCamVScaleChip;
        private CChip currentCamHScaleChip;

        private Dictionary<string, CChip> currentObjAnimations;

        private void t行のコメント処理(ref string strText)
        {
            int nCommentPos = strText.IndexOf("//");
            if (nCommentPos != -1)
                strText = strText.Remove(nCommentPos);
        }

        private bool t入力_コマンド文字列を抜き出す(ref CharEnumerator ce, ref StringBuilder sb文字列)
        {
            if (!this.t入力_空白をスキップする(ref ce))
                return false;   // 文字が尽きた

            #region [ コマンド終端文字(':')、半角空白、コメント開始文字(';')、改行のいずれかが出現するまでをコマンド文字列と見なし、sb文字列 にコピーする。]
            //-----------------
            while (ce.Current != ':' && ce.Current != ' ' && ce.Current != ';' && ce.Current != '\n')
            {
                sb文字列.Append(ce.Current);

                if (!ce.MoveNext())
                    return false;   // 文字が尽きた
            }
            //-----------------
            #endregion

            #region [ コマンド終端文字(':')で終端したなら、その次から空白をスキップしておく。]
            //-----------------
            if (ce.Current == ':')
            {
                if (!ce.MoveNext())
                    return false;   // 文字が尽きた

                if (!this.t入力_空白をスキップする(ref ce))
                    return false;   // 文字が尽きた
            }
            //-----------------
            #endregion

            return true;
        }
        private bool t入力_コメントをスキップする(ref CharEnumerator ce)
        {
            // 改行が現れるまでをコメントと見なしてスキップする。

            while (ce.Current != '\n')
            {
                if (!ce.MoveNext())
                    return false;   // 文字が尽きた
            }

            // 改行の次の文字へ移動した結果を返す。

            return ce.MoveNext();
        }
        private bool t入力_コメント文字列を抜き出す(ref CharEnumerator ce, ref StringBuilder sb文字列)
        {
            if (ce.Current != ';')      // コメント開始文字(';')じゃなければ正常帰還。
                return true;

            if (!ce.MoveNext())     // ';' の次で文字列が終わってたら終了帰還。
                return false;

            #region [ ';' の次の文字から '\n' の１つ前までをコメント文字列と見なし、sb文字列にコピーする。]
            //-----------------
            while (ce.Current != '\n')
            {
                sb文字列.Append(ce.Current);

                if (!ce.MoveNext())
                    return false;
            }
            //-----------------
            #endregion

            return true;
        }
        private void t入力_パラメータ食い込みチェック(string strコマンド名, ref string strコマンド, ref string strパラメータ)
        {
            if ((strコマンド.Length > strコマンド名.Length) && strコマンド.StartsWith(strコマンド名, StringComparison.OrdinalIgnoreCase))
            {
                strパラメータ = strコマンド.Substring(strコマンド名.Length).Trim();
                strコマンド = strコマンド.Substring(0, strコマンド名.Length);
            }
        }
        private bool t入力_パラメータ文字列を抜き出す(ref CharEnumerator ce, ref StringBuilder sb文字列)
        {
            if (!this.t入力_空白をスキップする(ref ce))
                return false;   // 文字が尽きた

            #region [ 改行またはコメント開始文字(';')が出現するまでをパラメータ文字列と見なし、sb文字列 にコピーする。]
            //-----------------
            while (ce.Current != '\n' && ce.Current != ';')
            {
                sb文字列.Append(ce.Current);

                if (!ce.MoveNext())
                    return false;
            }
            //-----------------
            #endregion

            return true;
        }
        private bool t入力_空白と改行をスキップする(ref CharEnumerator ce)
        {
            // 空白と改行が続く間はこれらをスキップする。

            while (ce.Current == ' ' || ce.Current == '\n')
            {
                if (ce.Current == '\n')
                    this.n現在の行数++;      // 改行文字では行番号が増える。

                if (!ce.MoveNext())
                    return false;   // 文字が尽きた
            }

            return true;
        }
        private bool t入力_空白をスキップする(ref CharEnumerator ce)
        {
            // 空白が続く間はこれをスキップする。

            while (ce.Current == ' ')
            {
                if (!ce.MoveNext())
                    return false;   // 文字が尽きた
            }

            return true;
        }
        private void t入力_行解析(ref StringBuilder sbコマンド, ref StringBuilder sbパラメータ, ref StringBuilder sbコメント)
        {
            string strコマンド = sbコマンド.ToString();
            string strパラメータ = sbパラメータ.ToString().Trim();
            string strコメント = sbコメント.ToString();

            // 行頭コマンドの処理

            #region [ IF ]
            //-----------------
            if (strコマンド.StartsWith("IF", StringComparison.OrdinalIgnoreCase))
            {
                this.t入力_パラメータ食い込みチェック("IF", ref strコマンド, ref strパラメータ);

                if (this.bstackIFからENDIFをスキップする.Count == 255)
                {
                    Trace.TraceWarning("#IF の入れ子の数が 255 を超えました。この #IF を無視します。[{0}: {1}行]", this.strファイル名の絶対パス, this.n現在の行数);
                }
                else if (this.bstackIFからENDIFをスキップする.Peek())
                {
                    this.bstackIFからENDIFをスキップする.Push(true); // 親が true ならその入れ子も問答無用で true 。
                }
                else                                                    // 親が false なら入れ子はパラメータと乱数を比較して結果を判断する。
                {
                    int n数値 = 0;

                    if (!int.TryParse(strパラメータ, out n数値))
                        n数値 = 1;

                    this.bstackIFからENDIFをスキップする.Push(n数値 != this.n現在の乱数);       // 乱数と数値が一致したら true 。
                }
            }
            //-----------------
            #endregion
            #region [ ENDIF ]
            //-----------------
            else if (strコマンド.StartsWith("ENDIF", StringComparison.OrdinalIgnoreCase))
            {
                this.t入力_パラメータ食い込みチェック("ENDIF", ref strコマンド, ref strパラメータ);

                if (this.bstackIFからENDIFをスキップする.Count > 1)
                {
                    this.bstackIFからENDIFをスキップする.Pop();      // 入れ子を１つ脱出。
                }
                else
                {
                    Trace.TraceWarning("#ENDIF に対応する #IF がありません。この #ENDIF を無視します。[{0}: {1}行]", this.strファイル名の絶対パス, this.n現在の行数);
                }
            }
            //-----------------
            #endregion

            else if (!this.bstackIFからENDIFをスキップする.Peek())       // IF～ENDIF をスキップするなら以下はすべて無視。
            {
                #region [ PATH_WAV ]
                //-----------------
                if (strコマンド.StartsWith("PATH_WAV", StringComparison.OrdinalIgnoreCase))
                {
                    this.t入力_パラメータ食い込みチェック("PATH_WAV", ref strコマンド, ref strパラメータ);
                    this.PATH_WAV = strパラメータ;
                }
                //-----------------
                #endregion
                #region [ TITLE ]
                //-----------------
                else if (strコマンド.StartsWith("TITLE", StringComparison.OrdinalIgnoreCase))
                {
                    //this.t入力_パラメータ食い込みチェック( "TITLE", ref strコマンド, ref strパラメータ );
                    //this.TITLE = strパラメータ;
                }
                //-----------------
                #endregion
                #region [ ARTIST ]
                //-----------------
                else if (strコマンド.StartsWith("ARTIST", StringComparison.OrdinalIgnoreCase))
                {
                    this.t入力_パラメータ食い込みチェック("ARTIST", ref strコマンド, ref strパラメータ);
                    this.ARTIST = strパラメータ;
                }
                //-----------------
                #endregion
                #region [ COMMENT ]
                //-----------------
                else if (strコマンド.StartsWith("COMMENT", StringComparison.OrdinalIgnoreCase))
                {
                    this.t入力_パラメータ食い込みチェック("COMMENT", ref strコマンド, ref strパラメータ);
                    this.COMMENT = strパラメータ;
                }
                //-----------------
                #endregion
                #region [ GENRE ]
                //-----------------
                else if (strコマンド.StartsWith("GENRE", StringComparison.OrdinalIgnoreCase))
                {
                    this.t入力_パラメータ食い込みチェック("GENRE", ref strコマンド, ref strパラメータ);
                    this.GENRE = strパラメータ;
                }
                //-----------------
                #endregion
                #region [ MAKER ]
                //-----------------
                else if (strコマンド.StartsWith("MAKER", StringComparison.OrdinalIgnoreCase))
                {
                    this.t入力_パラメータ食い込みチェック("MAKER", ref strコマンド, ref strパラメータ);
                    this.MAKER = strパラメータ;
                }
                //-----------------
                #endregion
                #region [ SELECTBG ]
                //-----------------
                else if (strコマンド.StartsWith("SELECTBG", StringComparison.OrdinalIgnoreCase))
                {
                    this.t入力_パラメータ食い込みチェック("SELECTBG", ref strコマンド, ref strパラメータ);
                    this.SELECTBG = strパラメータ;
                }
                //-----------------
                #endregion
                #region [ HIDDENLEVEL ]
                //-----------------
                else if (strコマンド.StartsWith("HIDDENLEVEL", StringComparison.OrdinalIgnoreCase))
                {
                    this.t入力_パラメータ食い込みチェック("HIDDENLEVEL", ref strコマンド, ref strパラメータ);
                    this.HIDDENLEVEL = strパラメータ.ToLower().Equals("on");
                }
                //-----------------
                #endregion
                #region [ PREVIEW ]
                //-----------------
                else if (strコマンド.StartsWith("PREVIEW", StringComparison.OrdinalIgnoreCase))
                {
                    this.t入力_パラメータ食い込みチェック("PREVIEW", ref strコマンド, ref strパラメータ);
                    this.PREVIEW = strパラメータ;
                }
                //-----------------
                #endregion
                #region [ PREIMAGE ]
                //-----------------
                else if (strコマンド.StartsWith("PREIMAGE", StringComparison.OrdinalIgnoreCase))
                {
                    this.t入力_パラメータ食い込みチェック("PREIMAGE", ref strコマンド, ref strパラメータ);
                    this.PREIMAGE = strパラメータ;
                }
                //-----------------
                #endregion
                #region [ RANDOM ]
                //-----------------
                else if (strコマンド.StartsWith("RANDOM", StringComparison.OrdinalIgnoreCase))
                {
                    this.t入力_パラメータ食い込みチェック("RANDOM", ref strコマンド, ref strパラメータ);

                    int n数値 = 1;
                    if (!int.TryParse(strパラメータ, out n数値))
                        n数値 = 1;

                    this.n現在の乱数 = TJAPlayer3.Random.Next(n数値) + 1;       // 1～数値 までの乱数を生成。
                }
                //-----------------
                #endregion
                #region [ BPM ]
                //-----------------
                else if (strコマンド.StartsWith("BPM", StringComparison.OrdinalIgnoreCase))
                {
                    //this.t入力_行解析_BPM_BPMzz( strコマンド, strパラメータ, strコメント );
                }
                //-----------------
                #endregion
                #region [ DTXVPLAYSPEED ]
                //-----------------
                else if (strコマンド.StartsWith("DTXVPLAYSPEED", StringComparison.OrdinalIgnoreCase))
                {
                    this.t入力_パラメータ食い込みチェック("DTXVPLAYSPEED", ref strコマンド, ref strパラメータ);

                    double dtxvplayspeed = 0.0;
                    if (TryParse(strパラメータ, out dtxvplayspeed) && dtxvplayspeed > 0.0)
                    {
                        this.dbDTXVPlaySpeed = dtxvplayspeed;
                    }
                }
                //-----------------
                #endregion
                else if (!this.bヘッダのみ)      // ヘッダのみの解析の場合、以下は無視。
                {
                    #region [ PANEL ]
                    //-----------------
                    if (strコマンド.StartsWith("PANEL", StringComparison.OrdinalIgnoreCase))
                    {
                        this.t入力_パラメータ食い込みチェック("PANEL", ref strコマンド, ref strパラメータ);

                        int dummyResult;                                // #23885 2010.12.12 yyagi: not to confuse "#PANEL strings (panel)" and "#PANEL int (panpot of EL)"
                        if (!int.TryParse(strパラメータ, out dummyResult))
                        {       // 数値じゃないならPANELとみなす
                            this.PANEL = strパラメータ;                          //
                            goto EOL;                                   //
                        }                                               // 数値ならPAN ELとみなす

                    }
                    //-----------------
                    #endregion
                    #region [ BASEBPM ]
                    //-----------------
                    else if (strコマンド.StartsWith("BASEBPM", StringComparison.OrdinalIgnoreCase))
                    {
                        this.t入力_パラメータ食い込みチェック("BASEBPM", ref strコマンド, ref strパラメータ);

                        double basebpm = 0.0;
                        //if( double.TryParse( str2, out num6 ) && ( num6 > 0.0 ) )
                        if (TryParse(strパラメータ, out basebpm) && basebpm > 0.0)   // #23880 2010.12.30 yyagi: alternative TryParse to permit both '.' and ',' for decimal point
                        {                                                   // #24204 2011.01.21 yyagi: Fix the condition correctly
                            this.BASEBPM = basebpm;
                        }
                    }
                    //-----------------
                    #endregion

                    // オブジェクト記述コマンドの処理。

                    else if (
                        !this.t入力_行解析_WAVPAN_PAN(strコマンド, strパラメータ, strコメント) &&
                        //	!this.t入力_行解析_BPM_BPMzz( strコマンド, strパラメータ, strコメント ) &&	// bヘッダのみ==trueの場合でもチェックするよう変更
                        !this.t入力_行解析_SIZE(strコマンド, strパラメータ, strコメント))
                    {
                        this.t入力_行解析_チップ配置(strコマンド, strパラメータ, strコメント);
                    }
                    EOL:
                    Debug.Assert(true);     // #23885 2010.12.12 yyagi: dummy line to exit parsing the line
                                            // 2011.8.17 from: "int xx=0;" から変更。毎回警告が出るので。
                }
                //else
                //{	// Duration測定のため、bヘッダのみ==trueでも、チップ配置は行う
                //	this.t入力_行解析_チップ配置( strコマンド, strパラメータ, strコメント );
                //}
            }
        }
        private bool t入力_行解析_BPM_BPMzz(string strコマンド, string strパラメータ, string strコメント)
        {
            // (1) コマンドを処理。

            #region [ "BPM" で始まらないコマンドは無効。]
            //-----------------
            if (!strコマンド.StartsWith("BPM", StringComparison.OrdinalIgnoreCase))
                return false;

            strコマンド = strコマンド.Substring(3); // strコマンド から先頭の"BPM"文字を除去。
                                            //-----------------
            #endregion

            // (2) パラメータを処理。

            int zz = 0;

            #region [ BPM番号 zz を取得する。]
            //-----------------
            if (strコマンド.Length < 2)
            {
                #region [ (A) "#BPM:" の場合 → zz = 00 ]
                //-----------------
                zz = 0;
                //-----------------
                #endregion
            }
            else
            {
                #region [ (B) "#BPMzz:" の場合 → zz = 00 ～ ZZ ]
                //-----------------
                zz = CConversion.n36進数2桁の文字列を数値に変換して返す(strコマンド.Substring(0, 2));
                if (zz < 0 || zz >= 36 * 36)
                {
                    Trace.TraceError("BPM番号に 00～ZZ 以外の値または不正な文字列が指定されました。[{0}: {1}行]", this.strファイル名の絶対パス, this.n現在の行数);
                    return false;
                }
                //-----------------
                #endregion
            }
            //-----------------
            #endregion

            double dbBPM = 0.0;

            #region [ BPM値を取得する。]
            //-----------------
            //if( !double.TryParse( strパラメータ, out result ) )
            if (!TryParse(strパラメータ, out dbBPM))         // #23880 2010.12.30 yyagi: alternative TryParse to permit both '.' and ',' for decimal point
                return false;

            if (dbBPM <= 0.0)
                return false;
            //-----------------
            #endregion

            if (zz == 0)            // "#BPM00:" と "#BPM:" は等価。
                this.BPM = dbBPM;   // この曲の代表 BPM に格納する。

            #region [ BPMリストに {内部番号, zz, dbBPM} の組を登録。]
            //-----------------
            this.listBPM.Add(
                this.n内部番号BPM1to,
                new CBPM()
                {
                    n内部番号 = this.n内部番号BPM1to,
                    n表記上の番号 = zz,
                    dbBPM値 = dbBPM,
                });
            //-----------------
            #endregion

            #region [ BPM番号が zz であるBPM未設定のBPMチップがあれば、そのサイズを変更する。無限管理に対応。]
            //-----------------
            if (this.n無限管理BPM[zz] == -zz)   // 初期状態では n無限管理BPM[zz] = -zz である。この場合、#BPMzz がまだ出現していないことを意味する。
            {
                for (int i = 0; i < this.listChip.Count; i++)   // これまでに出てきたチップのうち、該当する（BPM値が未設定の）BPMチップの値を変更する（仕組み上、必ず後方参照となる）。
                {
                    var chip = this.listChip[i];

                    if (chip.bBPMチップである && chip.n整数値_内部番号 == -zz)   // #BPMzz 行より前の行に出現した #BPMzz では、整数値_内部番号は -zz に初期化されている。
                        chip.n整数値_内部番号 = this.n内部番号BPM1to;
                }
            }
            this.n無限管理BPM[zz] = this.n内部番号BPM1to;           // 次にこの BPM番号 zz を使うBPMチップが現れたら、このBPM値が格納されることになる。
            this.n内部番号BPM1to++;     // 内部番号は単純増加連番。
                                    //-----------------
            #endregion

            return true;
        }

        private bool t入力_行解析_SIZE(string strコマンド, string strパラメータ, string strコメント)
        {
            // (1) コマンドを処理。

            #region [ "SIZE" で始まらないコマンドや、その後ろに2文字（番号）が付随してないコマンドは無効。]
            //-----------------
            if (!strコマンド.StartsWith("SIZE", StringComparison.OrdinalIgnoreCase))
                return false;

            strコマンド = strコマンド.Substring(4); // strコマンド から先頭の"SIZE"文字を除去。

            if (strコマンド.Length < 2) // サイズ番号の指定がない場合は無効。
                return false;
            //-----------------
            #endregion

            #region [ nWAV番号（36進数2桁）を取得。]
            //-----------------
            int nWAV番号 = CConversion.n36進数2桁の文字列を数値に変換して返す(strコマンド.Substring(0, 2));

            if (nWAV番号 < 0 || nWAV番号 >= 36 * 36)
            {
                Trace.TraceError("SIZEのWAV番号に 00～ZZ 以外の値または不正な文字列が指定されました。[{0}: {1}行]", this.strファイル名の絶対パス, this.n現在の行数);
                return false;
            }
            //-----------------
            #endregion


            // (2) パラメータを処理。

            #region [ nサイズ値 を取得する。値は 0～100 に収める。]
            //-----------------
            int nサイズ値;

            if (!int.TryParse(strパラメータ, out nサイズ値))
                return true;    // int変換に失敗しても、この行自体の処理は終えたのでtrueを返す。

            nサイズ値 = Math.Min(Math.Max(nサイズ値, 0), 100);  // 0未満は0、100超えは100に強制変換。
                                                        //-----------------
            #endregion

            #region [ nWAV番号で示されるサイズ未設定のWAVチップがあれば、そのサイズを変更する。無限管理に対応。]
            //-----------------
            if (this.n無限管理SIZE[nWAV番号] == -nWAV番号)  // 初期状態では n無限管理SIZE[xx] = -xx である。この場合、#SIZExx がまだ出現していないことを意味する。
            {
                foreach (CWAV wav in this.listWAV.Values)       // これまでに出てきたWAVチップのうち、該当する（サイズが未設定の）チップのサイズを変更する（仕組み上、必ず後方参照となる）。
                {
                    if (wav.nチップサイズ == -nWAV番号)     // #SIZExx 行より前の行に出現した #WAVxx では、チップサイズは -xx に初期化されている。
                        wav.nチップサイズ = nサイズ値;
                }
            }
            this.n無限管理SIZE[nWAV番号] = nサイズ値;         // 次にこの nWAV番号を使うWAVチップが現れたら、負数の代わりに、このサイズ値が格納されることになる。
                                                    //-----------------
            #endregion

            return true;
        }
        private bool t入力_行解析_WAVPAN_PAN(string strコマンド, string strパラメータ, string strコメント)
        {
            // (1) コマンドを処理。

            #region [ "WAVPAN" or "PAN" で始まらないコマンドは無効。]
            //-----------------
            if (strコマンド.StartsWith("WAVPAN", StringComparison.OrdinalIgnoreCase))
                strコマンド = strコマンド.Substring(6);     // strコマンド から先頭の"WAVPAN"文字を除去。

            else if (strコマンド.StartsWith("PAN", StringComparison.OrdinalIgnoreCase))
                strコマンド = strコマンド.Substring(3);     // strコマンド から先頭の"PAN"文字を除去。

            else
                return false;
            //-----------------
            #endregion

            // (2) パラメータを処理。

            if (strコマンド.Length < 2)
                return false;   // WAV番号 zz がないなら無効。

            #region [ WAV番号 zz を取得する。]
            //-----------------
            int zz = CConversion.n36進数2桁の文字列を数値に変換して返す(strコマンド.Substring(0, 2));
            if (zz < 0 || zz >= 36 * 36)
            {
                Trace.TraceError("WAVPAN(PAN)のWAV番号に 00～ZZ 以外の値または不正な文字列が指定されました。[{0}: {1}行]", this.strファイル名の絶対パス, this.n現在の行数);
                return false;
            }
            //-----------------
            #endregion

            #region [ WAV番号 zz を持つWAVチップの位置を変更する。無限定義対応。]
            //-----------------
            int n位置;
            if (int.TryParse(strパラメータ, out n位置))
            {
                n位置 = Math.Min(Math.Max(n位置, -100), 100);   // -100～+100 に丸める

                if (this.n無限管理PAN[zz] == (-10000 - zz)) // 初期状態では n無限管理PAN[zz] = -10000 - zz である。この場合、#WAVPANzz, #PANzz がまだ出現していないことを意味する。
                {
                    foreach (CWAV wav in this.listWAV.Values)   // これまでに出てきたチップのうち、該当する（位置が未設定の）WAVチップの値を変更する（仕組み上、必ず後方参照となる）。
                    {
                        if (wav.n位置 == (-10000 - zz))   // #WAVPANzz, #PANzz 行より前の行に出現した #WAVzz では、位置は -10000-zz に初期化されている。
                            wav.n位置 = n位置;
                    }
                }
                this.n無限管理PAN[zz] = n位置;            // 次にこの WAV番号 zz を使うWAVチップが現れたら、この位置が格納されることになる。
            }
            //-----------------
            #endregion

            return true;
        }
        private bool t入力_行解析_チップ配置(string strコマンド, string strパラメータ, string strコメント)
        {
            // (1) コマンドを処理。

            if (strコマンド.Length != 5)    // コマンドは必ず5文字であること。
                return false;

            #region [ n小節番号 を取得する。]
            //-----------------
            int n小節番号 = CConversion.n小節番号の文字列3桁を数値に変換して返す(strコマンド.Substring(0, 3));
            if (n小節番号 < 0)
                return false;

            n小節番号++;    // 先頭に空の1小節を設ける。
                        //-----------------
            #endregion

            #region [ nチャンネル番号 を取得する。]
            //-----------------
            int nチャンネル番号 = -1;

            // ファイルフォーマットによって処理が異なる。
            #region [ (B) その他の場合：チャンネル番号は16進数2桁。]
            //-----------------
            nチャンネル番号 = CConversion.n16進数2桁の文字列を数値に変換して返す(strコマンド.Substring(3, 2));

                if (nチャンネル番号 < 0)
                    return false;
                //-----------------
                #endregion
            //-----------------
            #endregion
            #region [ 取得したチャンネル番号で、this.bチップがある に該当があれば設定する。]
            //-----------------
            if ((nチャンネル番号 >= 0x11) && (nチャンネル番号 <= 0x1a))
            {
                this.bチップがある.Drums = true;
            }
            else if ((nチャンネル番号 >= 0x20) && (nチャンネル番号 <= 0x27))
            {
                this.bチップがある.Guitar = true;
            }
            else if ((nチャンネル番号 >= 0xA0) && (nチャンネル番号 <= 0xa7))
            {
                this.bチップがある.Bass = true;
            }
            switch (nチャンネル番号)
            {
                case 0x18:
                    this.bチップがある.HHOpen = true;
                    break;

                case 0x19:
                    this.bチップがある.Ride = true;
                    break;

                case 0x1a:
                    this.bチップがある.LeftCymbal = true;
                    break;

                case 0x20:
                    this.bチップがある.OpenGuitar = true;
                    break;

                case 0xA0:
                    this.bチップがある.OpenBass = true;
                    break;
            }
            //-----------------
            #endregion


            // (2) Ch.02を処理。

            #region [ 小節長変更(Ch.02)は他のチャンネルとはパラメータが特殊なので、先にとっとと終わらせる。 ]
            //-----------------
            if (nチャンネル番号 == 0x02)
            {
                // 小節長倍率を取得する。

                double db小節長倍率 = 1.0;
                //if( !double.TryParse( strパラメータ, out result ) )
                if (!this.TryParse(strパラメータ, out db小節長倍率))          // #23880 2010.12.30 yyagi: alternative TryParse to permit both '.' and ',' for decimal point
                {
                    Trace.TraceError("小節長倍率に不正な値を指定しました。[{0}: {1}行]", this.strファイル名の絶対パス, this.n現在の行数);
                    return false;
                }

                // 小節長倍率チップを配置する。

                this.listChip.Insert(
                    0,
                    new CChip()
                    {
                        nチャンネル番号 = nチャンネル番号,
                        db実数値 = db小節長倍率,
                        n発声位置 = n小節番号 * 384,
                    });

                return true;    // 配置終了。
            }
            //-----------------
            #endregion


            // (3) パラメータを処理。

            if (string.IsNullOrEmpty(strパラメータ))     // パラメータはnullまたは空文字列ではないこと。
                return false;

            #region [ strパラメータ にオブジェクト記述を格納し、その n文字数 をカウントする。]
            //-----------------
            int n文字数 = 0;

            var sb = new StringBuilder(strパラメータ.Length);

            // strパラメータを先頭から1文字ずつ見ながら正規化（無効文字('_')を飛ばしたり不正な文字でエラーを出したり）し、sb へ格納する。

            CharEnumerator ce = strパラメータ.GetEnumerator();
            while (ce.MoveNext())
            {
                if (ce.Current == '_')      // '_' は無視。
                    continue;

                if (CConversion.str36進数文字.IndexOf(ce.Current) < 0)  // オブジェクト記述は36進数文字であること。
                {
                    Trace.TraceError("不正なオブジェクト指定があります。[{0}: {1}行]", this.strファイル名の絶対パス, this.n現在の行数);
                    return false;
                }

                sb.Append(ce.Current);
                n文字数++;
            }

            strパラメータ = sb.ToString();   // 正規化された文字列になりました。

            if ((n文字数 % 2) != 0)        // パラメータの文字数が奇数の場合、最後の1文字を無視する。
                n文字数--;
            //-----------------
            #endregion


            // (4) パラメータをオブジェクト数値に分解して配置する。

            for (int i = 0; i < (n文字数 / 2); i++)    // 2文字で1オブジェクト数値
            {
                #region [ nオブジェクト数値 を１つ取得する。'00' なら無視。]
                //-----------------
                int nオブジェクト数値 = 0;

                if (nチャンネル番号 == 0x03)
                {
                    // Ch.03 のみ 16進数2桁。
                    nオブジェクト数値 = CConversion.n16進数2桁の文字列を数値に変換して返す(strパラメータ.Substring(i * 2, 2));
                }
                else
                {
                    // その他のチャンネルは36進数2桁。
                    nオブジェクト数値 = CConversion.n36進数2桁の文字列を数値に変換して返す(strパラメータ.Substring(i * 2, 2));
                }

                if (nオブジェクト数値 == 0x00)
                    continue;
                //-----------------
                #endregion

                // オブジェクト数値に対応するチップを生成。

                var chip = new CChip();

                chip.nチャンネル番号 = nチャンネル番号;
                chip.n発声位置 = (n小節番号 * 384) + ((384 * i) / (n文字数 / 2));
                chip.n整数値 = nオブジェクト数値;
                chip.n整数値_内部番号 = nオブジェクト数値;

                #region [ chip.e楽器パート = ... ]
                //-----------------
                if ((nチャンネル番号 >= 0x11) && (nチャンネル番号 <= 0x1C))
                {
                    chip.e楽器パート = EInstrumentPad.DRUMS;
                }
                if ((nチャンネル番号 >= 0x20) && (nチャンネル番号 <= 0x27))
                {
                    chip.e楽器パート = EInstrumentPad.GUITAR;
                }
                if ((nチャンネル番号 >= 160) && (nチャンネル番号 <= 0xA7))
                {
                    chip.e楽器パート = EInstrumentPad.BASS;
                }
                //-----------------
                #endregion

                #region [ 無限定義への対応 → 内部番号の取得。]
                //-----------------
                if (chip.nチャンネル番号 == 0x01)
                {
                    chip.n整数値_内部番号 = this.n無限管理WAV[nオブジェクト数値];  // これが本当に一意なWAV番号となる。（無限定義の場合、chip.n整数値 は一意である保証がない。）
                }
                else if (chip.bBPMチップである)
                {
                    chip.n整数値_内部番号 = this.n無限管理BPM[nオブジェクト数値];  // これが本当に一意なBPM番号となる。（同上。）
                }
                //-----------------
                #endregion

                #region [ フィルインON/OFFチャンネル(Ch.53)の場合、発声位置を少し前後にずらす。]
                //-----------------
                if (nチャンネル番号 == 0x53)
                {
                    // ずらすのは、フィルインONチップと同じ位置にいるチップでも確実にフィルインが発動し、
                    // 同様に、フィルインOFFチップと同じ位置にいるチップでも確実にフィルインが終了するようにするため。

                    if ((nオブジェクト数値 > 0) && (nオブジェクト数値 != 2))
                    {
                        chip.n発声位置 -= 32;   // 384÷32＝12 ということで、フィルインONチップは12分音符ほど前へ移動。
                    }
                    else if (nオブジェクト数値 == 2)
                    {
                        chip.n発声位置 += 32;   // 同じく、フィルインOFFチップは12分音符ほど後ろへ移動。
                    }
                }
                //-----------------
                #endregion

                // チップを配置。

                this.listChip.Add(chip);
            }
            return true;
        }
        #region [#23880 2010.12.30 yyagi: コンマとスペースの両方を小数点として扱うTryParse]
        /// <summary>
        /// 小数点としてコンマとピリオドの両方を受け付けるTryParse()
        /// </summary>
        /// <param name="s">strings convert to double</param>
        /// <param name="result">parsed double value</param>
        /// <returns>s が正常に変換された場合は true。それ以外の場合は false。</returns>
        /// <exception cref="ArgumentException">style が NumberStyles 値でないか、style に NumberStyles.AllowHexSpecifier 値が含まれている</exception>
        private bool TryParse(string s, out double result)
        {   // #23880 2010.12.30 yyagi: alternative TryParse to permit both '.' and ',' for decimal point
            // EU諸国での #BPM 123,45 のような記述に対応するため、
            // 小数点の最終位置を検出して、それをlocaleにあった
            // 文字に置き換えてからTryParse()する
            // 桁区切りの文字はスキップする

            const string DecimalSeparators = ".,";              // 小数点文字
            const string GroupSeparators = ".,' ";              // 桁区切り文字
            const string NumberSymbols = "0123456789";          // 数値文字

            int len = s.Length;                                 // 文字列長
            int decimalPosition = len;                          // 真の小数点の位置 最初は文字列終端位置に仮置きする

            for (int i = 0; i < len; i++)
            {                           // まず、真の小数点(一番最後に現れる小数点)の位置を求める
                char c = s[i];
                if (NumberSymbols.IndexOf(c) >= 0)
                {               // 数値だったらスキップ
                    continue;
                }
                else if (DecimalSeparators.IndexOf(c) >= 0)
                {       // 小数点文字だったら、その都度位置を上書き記憶
                    decimalPosition = i;
                }
                else if (GroupSeparators.IndexOf(c) >= 0)
                {       // 桁区切り文字の場合もスキップ
                    continue;
                }
                else
                {                                           // 数値_小数点_区切り文字以外がきたらループ終了
                    break;
                }
            }

            StringBuilder decimalStr = new StringBuilder(16);
            for (int i = 0; i < len; i++)
            {                           // 次に、localeにあった数値文字列を生成する
                char c = s[i];
                if (NumberSymbols.IndexOf(c) >= 0)
                {               // 数値だったら
                    decimalStr.Append(c);                           // そのままコピー
                }
                else if (DecimalSeparators.IndexOf(c) >= 0)
                {       // 小数点文字だったら
                    if (i == decimalPosition)
                    {                       // 最後に出現した小数点文字なら、localeに合った小数点を出力する
                        decimalStr.Append(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                    }
                }
                else if (GroupSeparators.IndexOf(c) >= 0)
                {       // 桁区切り文字だったら
                    continue;                                       // 何もしない(スキップ)
                }
                else
                {
                    break;
                }
            }
            return double.TryParse(decimalStr.ToString(), out result);  // 最後に、自分のlocale向けの文字列に対してTryParse実行
        }
        #endregion
        /// <summary>
        /// 音源再生前の空白を追加するメソッド。
        /// </summary>
        private void AddMusicPreTimeMs()
        {
            this.dbNowTime += TJAPlayer3.ConfigIni.MusicPreTimeMs;
            this.dbNowBMScollTime += TJAPlayer3.ConfigIni.MusicPreTimeMs * this.dbNowBPM / 15000;
        }
        //-----------------
        #endregion
    }
}
