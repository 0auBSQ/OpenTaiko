using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using FDK;
using FDK.ExtensionMethods;
using TJAPlayer3;
using System.Linq;
using Silk.NET.Core;

namespace TJAPlayer3
{
	/// <summary>
	/// 演奏画面の共通クラス (ドラム演奏画面, ギター演奏画面の継承元)
	/// </summary>
	internal abstract class CStage演奏画面共通 : CStage
	{
		// プロパティ

		// メソッド

		#region [ t演奏結果を格納する_ドラム() ]
		public void t演奏結果を格納する_ドラム( out CScoreIni.C演奏記録 Drums )
		{
			Drums = new CScoreIni.C演奏記録();

			//if (  )
			{
				Drums.nスコア = (long) this.actScore.Get( EInstrumentPad.DRUMS, 0 );
                Drums.dbゲーム型スキル値 = CScoreIni.tゲーム型スキルを計算して返す(TJAPlayer3.DTX.LEVEL.Drums, TJAPlayer3.DTX.n可視チップ数.Drums, this.nヒット数_Auto含まない.Drums.Perfect, this.actCombo.n現在のコンボ数.最高値[0], EInstrumentPad.DRUMS, bIsAutoPlay);
                Drums.db演奏型スキル値 = CScoreIni.t演奏型スキルを計算して返す( TJAPlayer3.DTX.n可視チップ数.Drums, this.nヒット数_Auto含まない.Drums.Perfect, this.nヒット数_Auto含まない.Drums.Great, this.nヒット数_Auto含まない.Drums.Good, this.nヒット数_Auto含まない.Drums.Poor, this.nヒット数_Auto含まない.Drums.Miss, EInstrumentPad.DRUMS, bIsAutoPlay );
				Drums.nPerfect数 = TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[0] ? this.nヒット数_Auto含む.Drums.Perfect : this.nヒット数_Auto含まない.Drums.Perfect;
				Drums.nGreat数 = TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[0] ? this.nヒット数_Auto含む.Drums.Great : this.nヒット数_Auto含まない.Drums.Great;
				Drums.nGood数 = TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[0] ? this.nヒット数_Auto含む.Drums.Good : this.nヒット数_Auto含まない.Drums.Good;
				Drums.nPoor数 = TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[0] ? this.nヒット数_Auto含む.Drums.Poor : this.nヒット数_Auto含まない.Drums.Poor;
				Drums.nMiss数 = TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[0] ? this.nヒット数_Auto含む.Drums.Miss : this.nヒット数_Auto含まない.Drums.Miss;
				Drums.nPerfect数_Auto含まない = this.nヒット数_Auto含まない.Drums.Perfect;
				Drums.nGreat数_Auto含まない = this.nヒット数_Auto含まない.Drums.Great;
				Drums.nGood数_Auto含まない = this.nヒット数_Auto含まない.Drums.Good;
				Drums.nPoor数_Auto含まない = this.nヒット数_Auto含まない.Drums.Poor;
				Drums.nMiss数_Auto含まない = this.nヒット数_Auto含まない.Drums.Miss;
                Drums.n連打数 = this.n合計連打数[ 0 ]; 
                Drums.n最大コンボ数 = this.actCombo.n現在のコンボ数.最高値[0];
                Drums.n全チップ数 = TJAPlayer3.DTX.n可視チップ数.Drums;
				for ( int i = 0; i < (int) Eレーン.MAX;  i++ )
				{
					Drums.bAutoPlay[ i ] = bIsAutoPlay[ i ];
				}
				Drums.bTight = TJAPlayer3.ConfigIni.bTight;
				for ( int i = 0; i < 3; i++ )
				{
					Drums.bSudden[ i ] = TJAPlayer3.ConfigIni.bSudden[ i ];
					Drums.bHidden[ i ] = TJAPlayer3.ConfigIni.bHidden[ i ];
					Drums.eInvisible[ i ] = TJAPlayer3.ConfigIni.eInvisible[ i ];
					Drums.bReverse[ i ] = TJAPlayer3.ConfigIni.bReverse[ i ];
					Drums.eRandom[ i ] = TJAPlayer3.ConfigIni.eRandom[ i ];
					Drums.bLight[ i ] = TJAPlayer3.ConfigIni.bLight[ i ];
					Drums.bLeft[ i ] = TJAPlayer3.ConfigIni.bLeft[ i ];
					Drums.f譜面スクロール速度[ i ] = ( (float) ( TJAPlayer3.ConfigIni.nScrollSpeed[ i ] + 1 ) ) * 0.5f;
				}
				Drums.eDark = TJAPlayer3.ConfigIni.eDark;
				Drums.n演奏速度分子 = TJAPlayer3.ConfigIni.n演奏速度;
				Drums.n演奏速度分母 = 20;
				Drums.bSTAGEFAILED有効 = TJAPlayer3.ConfigIni.bSTAGEFAILED有効;
				Drums.eダメージレベル = TJAPlayer3.ConfigIni.eダメージレベル;
				Drums.b演奏にキーボードを使用した = this.b演奏にキーボードを使った;
				Drums.b演奏にMIDI入力を使用した = this.b演奏にMIDI入力を使った;
				Drums.b演奏にジョイパッドを使用した = this.b演奏にジョイパッドを使った;
				Drums.b演奏にマウスを使用した = this.b演奏にマウスを使った;
				Drums.nPerfectになる範囲ms = TJAPlayer3.nPerfect範囲ms;
				Drums.nGreatになる範囲ms = TJAPlayer3.nGreat範囲ms;
				Drums.nGoodになる範囲ms = TJAPlayer3.nGood範囲ms;
				Drums.nPoorになる範囲ms = TJAPlayer3.nPoor範囲ms;
				Drums.strDTXManiaのバージョン = TJAPlayer3.VERSION;
				Drums.最終更新日時 = DateTime.Now.ToString();
				Drums.Hash = CScoreIni.t演奏セクションのMD5を求めて返す( Drums );
                Drums.fゲージ = (float)this.actGauge.db現在のゲージ値[ 0 ];
                if( !TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[0])
                {
                    Drums.nハイスコア = TJAPlayer3.stageSongSelect.r確定されたスコア.譜面情報.nハイスコア; //2015.06.16 kairera0467 他難易度の上書き防止。
                    if( TJAPlayer3.stageSongSelect.r確定されたスコア.譜面情報.nハイスコア[ TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0]] < (int)this.actScore.Get( EInstrumentPad.DRUMS, 0 ) )
                        Drums.nハイスコア[ TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0]] = (int)this.actScore.Get( EInstrumentPad.DRUMS, 0 );
                }
                var danC = TJAPlayer3.stage演奏ドラム画面.actDan.GetExam();
                for (int i = 0; i < danC.Length; i++)
                {
                    Drums.Dan_C[i] = danC[i];
                }
			}
		}
		#endregion

        // CStage 実装
        
        public int[] nNoteCount = new int[5];
        public int[] nBalloonCount = new int[5];
        public double[] nRollTimeMs = new double[5];
        public double[] nAddScoreNiji = new double[5];

        public override void Activate()
		{
            listChip = new List<CDTX.CChip>[ 5 ];
            List<CDTX.CChip>[] balloonChips = new List<CDTX.CChip>[5];
            for( int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++ )
            {
                nNoteCount[i] = 0;
                nBalloonCount[i] = 0;
                nRollTimeMs[i] = 0;
                nAddScoreNiji[i] = 0;

                switch( i )
                {
                    case 0:
			            listChip[i] = TJAPlayer3.DTX.listChip;
                        break;
                    case 1:
			            listChip[i] = TJAPlayer3.DTX_2P.listChip;
                        break;
                    case 2:
                        listChip[i] = TJAPlayer3.DTX_3P.listChip;
                        break;
                    case 3:
                        listChip[i] = TJAPlayer3.DTX_4P.listChip;
                        break;
                    case 4:
                        listChip[i] = TJAPlayer3.DTX_5P.listChip;
                        break;
                }

                if (TJAPlayer3.ConfigIni.nPlayerCount >= 2)
                {
                    balloonChips[i] = new();
                    for(int j = 0; j < listChip[i].Count; j++)
                    {
                        var chip = listChip[i][j];

                        if (NotesManager.IsGenericBalloon(chip))
                        {
                            balloonChips[i].Add(chip);
                        }
                    }
                }

                int n整数値管理 = 0;
                if (r指定時刻に一番近い未ヒットChipを過去方向優先で検索する(0, i) != null) //2020.07.08 Mr-Ojii 未ヒットチップがないときの例外の発生回避 <-(KabanFriends)コード借りましたごめんなさい(´・ω・`)
                {
                    foreach (CDTX.CChip chip in listChip[i])
                    {
                        chip.nList上の位置 = n整数値管理;
                        //if ((chip.nチャンネル番号 == 0x15 || chip.nチャンネル番号 == 0x16) && (n整数値管理 < this.listChip[i].Count - 1))
                        if ((NotesManager.IsRoll(chip) || NotesManager.IsFuzeRoll(chip)) && (n整数値管理 < this.listChip[i].Count - 1))
                        {
                            if (chip.db発声時刻ms < r指定時刻に一番近い未ヒットChipを過去方向優先で検索する(0, i).db発声時刻ms)
                            {
                                chip.n描画優先度 = 1;
                            }
                        }
                        n整数値管理++;
                    }
                }
            }

            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                CDTX _dtx = TJAPlayer3.DTX;
                switch (i) //2017.08.11 kairera0467
                {
                    case 0:
                        break;
                    case 1:
                        _dtx = TJAPlayer3.DTX_2P;
                        break;
                    case 2:
                        _dtx = TJAPlayer3.DTX_3P;
                        break;
                    case 3:
                        _dtx = TJAPlayer3.DTX_4P;
                        break;
                    case 4:
                        _dtx = TJAPlayer3.DTX_5P;
                        break;
                    default:
                        break;
                }

                if (TJAPlayer3.ConfigIni.nPlayerCount >= 2)
                {
                    for(int j = 0; j < balloonChips[i].Count; j++)
                    {
                        var chip = balloonChips[i][j];
                        if (NotesManager.IsKusudama(chip))
                        {
                            for(int p = 0; p < TJAPlayer3.ConfigIni.nPlayerCount; p++)
                            {
                                if (p == i) continue;
                                var chip2 = balloonChips[p].Find(x => Math.Abs(x.db発声時刻ms - chip.db発声時刻ms) < 100);

                                if (chip2 == null)
                                {
                                    var chip3 = listChip[p].Find(x => Math.Abs(x.db発声時刻ms - chip.db発声時刻ms) < 100);
                                    if (!NotesManager.IsKusudama(chip3))
                                    {
                                        chip.nチャンネル番号 = 0x17;
                                    }
                                }
                                else if (!NotesManager.IsKusudama(chip2)) 
                                {
                                    chip.nチャンネル番号 = 0x17;
                                }
                            }
                        }
                    }
                    /*
                    for(int p = 0; p < TJAPlayer3.ConfigIni.nPlayerCount; p++)
                    {
                        for(int j = 0; j < balloonChips[p].Count; j++)
                        {
                            var chip = balloonChips[i].Find(x => Math.Abs(x.db発声時刻ms - balloonChips[p][j].db発声時刻ms) < 100);
                            if (chip == null)
                            {
                                var chip2 = listChip[i].Find(x => Math.Abs(x.db発声時刻ms - balloonChips[p][j].db発声時刻ms) < 100);
                                if (NotesManager.IsKusudama(chip2))
                                {
                                    chip.nチャンネル番号 = NotesManager.GetNoteValueFromChar("7");
                                }
                            }
                            else if (NotesManager.IsKusudama(chip) && !NotesManager.IsKusudama(balloonChips[p][j]))
                            {
                                chip.nチャンネル番号 = balloonChips[p][j].nチャンネル番号;
                            }
                        }
                    }
                    */
                }



                int _totalNotes = 0;
                int _totalBalloons = 0;
                double _totalRolls = 0;

                /*
                for (int j = 0; j < (_dtx.bチップがある.Branch ? 2 : 1); j++)
                {
                    var _list = (j == 0) ? _dtx.listChip : _dtx.listChip_Branch[2];

                    _totalNotes += _list.Where(num => NotesManager.IsMissableNote(num)).Count();
                    for (int k = 0; k < _list.Count; k++)
                    {
                        var _chip = _list[k];
                        _totalBalloons += _chip.nBalloon;
                        if (NotesManager.IsRoll(_chip))
                            _totalRolls += (_chip.nノーツ終了時刻ms - _chip.n発声時刻ms) / 1000.0;
                    }
                }
                */

                var _list = (_dtx.bチップがある.Branch) ? _dtx.listChip_Branch[2] : _dtx.listChip;

                _totalNotes += _list.Where(num => NotesManager.IsMissableNote(num)).Count();
                for (int k = 0; k < _list.Count; k++)
                {
                    var _chip = _list[k];

                    if (NotesManager.IsGenericBalloon(_chip))
                    {
                        var _duration = (_chip.nノーツ終了時刻ms - _chip.n発声時刻ms) / 1000.0;
                        var _expectedHits = (int)(_duration / 16.6f);
                        _totalBalloons += Math.Min(_chip.nBalloon, _expectedHits);
                    }
                    
                    if (NotesManager.IsRoll(_chip) || NotesManager.IsFuzeRoll(_chip))
                        _totalRolls += (_chip.nノーツ終了時刻ms - _chip.n発声時刻ms) / 1000.0;
                }

                nNoteCount[i] = _totalNotes;
                nBalloonCount[i] = _totalBalloons;
                nRollTimeMs[i] = _totalRolls;
            }

            for (int k = 0; k < TJAPlayer3.ConfigIni.nPlayerCount; k++)
            {
                //nAddScoreNiji = (1000000 - (15 * RollTimems * 100) - (nBalloonCount * 100)) / TJAPlayer3.DTX.listChip.Count;
                if (nNoteCount[k] == 0 && nBalloonCount[k] == 0)
                {
                    nAddScoreNiji[k] = 1000000;
                }
                else
                {
                    nAddScoreNiji[k] = (double)Math.Ceiling((decimal)(1000000 - (nBalloonCount[k] * 100) - (nRollTimeMs[k] * 100 * 16.6)) / nNoteCount[k] / 10) * 10;
                }

            }

            
            for (int index = TJAPlayer3.DTX.listChip.Count - 1; index >= 0; index--)
            {
                if (TJAPlayer3.DTX.listChip[index].nチャンネル番号 == 0x01)
                {
                    this.bgmlength = TJAPlayer3.DTX.listChip[index].GetDuration() + TJAPlayer3.DTX.listChip[index].n発声時刻ms;
                    break;
                }
            }

            _AIBattleState = 0;
            _AIBattleStateBatch = new Queue<float>[] { new Queue<float>(), new Queue<float>() };

            this.AIBattleSections = new List<AIBattleSection>();

            CDTX.CChip endChip = null;
            for (int i = 0; i < listChip[0].Count; i++)
            {
                CDTX.CChip chip = listChip[0][i];
                if (endChip == null || (chip.n発声時刻ms > endChip.n発声時刻ms && chip.nチャンネル番号 == 0x50))
                {
                    endChip = chip;
                }
            }

            int battleSectionCount = 3 + ((endChip.n発声時刻ms * 2) / 100000);
            // Avoid single section
            if (battleSectionCount <= 1)
                battleSectionCount = 3;
            // Avoid ties
            if (battleSectionCount % 2 == 0)
                battleSectionCount -= 1;


            int battleSectionTime = 0;

            int nowBattleSectionCount = 1;

            for (int i = 0; i < listChip[0].Count; i++)
            {
                CDTX.CChip chip = listChip[0][i];

                if (nowBattleSectionCount == battleSectionCount)
                {
                    chip = endChip;
                    i = listChip[0].Count - 1;
                }

                int endtime = endChip.n発声時刻ms / battleSectionCount;

                bool isAddSection = (nowBattleSectionCount != battleSectionCount) ? 
                    chip.n発声時刻ms >= endtime * nowBattleSectionCount : 
                    i == listChip[0].Count - 1;


                if (isAddSection)
                {
                    AIBattleSection aIBattleSection = new AIBattleSection();

                    aIBattleSection.StartTime = battleSectionTime;
                    aIBattleSection.EndTime = chip.n発声時刻ms;
                    aIBattleSection.Length = aIBattleSection.EndTime - aIBattleSection.StartTime;

                    this.AIBattleSections.Add(aIBattleSection);

                    battleSectionTime = aIBattleSection.EndTime;
                    nowBattleSectionCount++;
                }
            }

            NowAIBattleSectionCount = 0;
            bIsAIBattleWin = false;

            ctChipAnime = new CCounter[5];
            ctChipAnimeLag = new CCounter[5];
            for (int i = 0; i < 5; i++)
            {
                ctChipAnime[i] = new CCounter();
                ctChipAnimeLag[i] = new CCounter();
            }

            listWAV = TJAPlayer3.DTX.listWAV;

			this.eフェードアウト完了時の戻り値 = E演奏画面の戻り値.継続;
			this.n現在のトップChip = ( listChip[0].Count > 0 ) ? 0 : -1;
			this.L最後に再生したHHの実WAV番号 = new List<int>( 16 );
			this.n最後に再生したHHのチャンネル番号 = 0;
			this.n最後に再生した実WAV番号.Guitar = -1;
			this.n最後に再生した実WAV番号.Bass = -1;
			for ( int i = 0; i < 50; i++ )
			{
				this.n最後に再生したBGMの実WAV番号[ i ] = -1;
			}

			cInvisibleChip = new CInvisibleChip( TJAPlayer3.ConfigIni.nDisplayTimesMs, TJAPlayer3.ConfigIni.nFadeoutTimeMs );
			this.演奏判定ライン座標 = new C演奏判定ライン座標共通();
			for ( int k = 0; k < 4; k++ )
			{
				//for ( int n = 0; n < 5; n++ )
				//{
					this.nヒット数_Auto含まない[ k ] = new CHITCOUNTOFRANK();
					this.nヒット数_Auto含む[ k ] = new CHITCOUNTOFRANK();
				//}
				this.queWailing[ k ] = new Queue<CDTX.CChip>();
				this.r現在の歓声Chip[ k ] = null;
				cInvisibleChip.eInvisibleMode[ k ] = TJAPlayer3.ConfigIni.eInvisible[ k ];

                /*
				if ( TJAPlayer3.DTXVmode.Enabled )
				{
					TJAPlayer3.ConfigIni.nScrollSpeed[ k ] = TJAPlayer3.ConfigIni.nViewerScrollSpeed[ k ];
				}
                */

				//this.nJudgeLinePosY_delta[ k ] = CDTXMania.ConfigIni.nJudgeLinePosOffset[ k ];		// #31602 2013.6.23 yyagi

				this.演奏判定ライン座標.n判定位置[ k ] = TJAPlayer3.ConfigIni.e判定位置[ k ];
				this.演奏判定ライン座標.nJudgeLinePosY_delta[ k ] = TJAPlayer3.ConfigIni.nJudgeLinePosOffset[ k ];
				this.bReverse[ k ]             = TJAPlayer3.ConfigIni.bReverse[ k ];					//

			}
			actCombo.演奏判定ライン座標 = 演奏判定ライン座標;

            this.b演奏にキーボードを使った = false;
            this.b演奏にジョイパッドを使った = false;
            this.b演奏にMIDI入力を使った = false;
            this.b演奏にマウスを使った = false;

			cInvisibleChip.Reset();
			base.Activate();
			this.tステータスパネルの選択();
			this.tパネル文字列の設定();
            //this.演奏判定ライン座標();
            this.bIsGOGOTIME = new bool[] { false, false, false, false, false };
            this.bIsMiss = new bool[] { false, false, false, false, false };
            this.bUseBranch = new bool[]{ false, false, false, false, false };
            this.n現在のコース = new CDTX.ECourse[5];
            this.n次回のコース = new CDTX.ECourse[5];
            nCurrentKusudamaRollCount = 0;
            nCurrentKusudamaCount = 0;

            for (int i = 0; i < 5; i++)
            {
                this.b強制的に分岐させた[i] = false;

                this.CChartScore[i] = new CBRANCHSCORE();
                this.CSectionScore[i] = new CBRANCHSCORE();

                TJAPlayer3.stage演奏ドラム画面.actMtaiko.After[i] = CDTX.ECourse.eNormal;
                TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.stBranch[i].nAfter = CDTX.ECourse.eNormal;
                TJAPlayer3.stage演奏ドラム画面.actMtaiko.Before[i] = CDTX.ECourse.eNormal;
                TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.stBranch[i].nBefore = CDTX.ECourse.eNormal;
            }

            for (int i = 0; i < CBranchScore.Length; i++)
            {
                this.CBranchScore[i] = new CBRANCHSCORE();

                //大音符分岐時の情報をまとめるため
                this.CBranchScore[i].cBigNotes = new CBRANCHSCORE();
            }


            this.nレーン用表示コース = new CDTX.ECourse[5];
            this.b連打中 = new bool[] { false, false, false, false, false };
            this.n現在の連打数 = new int[]{ 0, 0, 0, 0, 0 };
            this.n合計連打数 = new int[]{ 0, 0, 0, 0, 0 };
            this.n分岐した回数 = new int[ 5 ];
            this.Chara_MissCount = new int[5];
            for (int i = 0; i < 2; i++)
            {
                ShownLyric[i] = 0;
            }
            this.nJPOSSCROLL = new int[ 5 ];
            this.bLEVELHOLD = new bool[]{ false, false, false, false, false };
            this.JPOSCROLLX = new int[5];
            this.JPOSCROLLY = new int[5];
            eFirstGameType = new EGameType[5];
            bSplitLane = new bool[5];


            // Double play set here
            this.bDoublePlay = TJAPlayer3.ConfigIni.nPlayerCount >= 2 ? true : false;

            this.nLoopCount_Clear = 1;

            this.tBranchReset(0);

            this.bIsAutoPlay = TJAPlayer3.ConfigIni.bAutoPlay;                                  // #24239 2011.1.23 yyagi

            //this.bIsAutoPlay.Guitar = CDTXMania.ConfigIni.bギターが全部オートプレイである;
            //this.bIsAutoPlay.Bass = CDTXMania.ConfigIni.bベースが全部オートプレイである;
            //			this.nRisky = CDTXMania.ConfigIni.nRisky;											// #23559 2011.7.28 yyagi

            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                actGauge.Init(TJAPlayer3.ConfigIni.nRisky, i);                                  // #23559 2011.7.28 yyagi
                eFirstGameType[i] = TJAPlayer3.ConfigIni.nGameType[i];
            }
			this.nPolyphonicSounds = TJAPlayer3.ConfigIni.nPoliphonicSounds;
			e判定表示優先度 = TJAPlayer3.ConfigIni.e判定表示優先度;

			TJAPlayer3.Skin.tRemoveMixerAll();	// 効果音のストリームをミキサーから解除しておく

			queueMixerSound = new Queue<stmixer>( 64 );
			bIsDirectSound = ( TJAPlayer3.SoundManager.GetCurrentSoundDeviceType() == "DirectSound" );
			bUseOSTimer = TJAPlayer3.ConfigIni.bUseOSTimer;
			this.bPAUSE = false;
			if ( TJAPlayer3.DTXVmode.Enabled )
			{
				db再生速度 = TJAPlayer3.DTX.dbDTXVPlaySpeed;
				TJAPlayer3.ConfigIni.n演奏速度 = (int) (TJAPlayer3.DTX.dbDTXVPlaySpeed * 20 + 0.5 );
			}
			else
			{
				db再生速度 = TJAPlayer3.ConfigIni.SongPlaybackSpeed;
			}
			bValidScore = ( TJAPlayer3.DTXVmode.Enabled ) ? false : true;

			#region [ 演奏開始前にmixer登録しておくべきサウンド(開幕してすぐに鳴らすことになるチップ音)を登録しておく ]
			foreach ( CDTX.CChip pChip in listChip[0] )
			{
//				Debug.WriteLine( "CH=" + pChip.nチャンネル番号.ToString( "x2" ) + ", 整数値=" + pChip.n整数値 +  ", time=" + pChip.n発声時刻ms );
				if ( pChip.n発声時刻ms <= 0 )
				{
					if ( pChip.nチャンネル番号 == 0xDA )
					{
						pChip.bHit = true;
//						Trace.TraceInformation( "first [DA] BAR=" + pChip.n発声位置 / 384 + " ch=" + pChip.nチャンネル番号.ToString( "x2" ) + ", wav=" + pChip.n整数値 + ", time=" + pChip.n発声時刻ms );
						if ( listWAV.TryGetValue( pChip.n整数値_内部番号, out CDTX.CWAV wc ) )
						{
							for ( int i = 0; i < nPolyphonicSounds; i++ )
							{
								if ( wc.rSound[ i ] != null )
								{
									TJAPlayer3.SoundManager.AddMixer( wc.rSound[ i ], db再生速度, pChip.b演奏終了後も再生が続くチップである );
									//AddMixer( wc.rSound[ i ] );		// 最初はqueueを介さず直接ミキサー登録する
								}
							}
						}
					}
				}
				else
				{
					break;
				}
			}
            #endregion

            // Note
            if(TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
            {
                n良 = new int[TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count];
                nCombo = new int[TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count];
                nHighestCombo = new int[TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count];
                n可 = new int[TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count];
                n不可 = new int[TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count];
                n連打 = new int[TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count];
                nADLIB = new int[TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count];
                nMine = new int[TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count];
            }


            this.sw = new Stopwatch();
			//          this.sw2 = new Stopwatch();
            //			this.gclatencymode = GCSettings.LatencyMode;
            //			GCSettings.LatencyMode = GCLatencyMode.Batch;	// 演奏画面中はGCを抑止する
            this.bIsAlreadyCleared = new bool[5];
            for (int player = 0; player < TJAPlayer3.ConfigIni.nPlayerCount; player++)
            {
                var chara = TJAPlayer3.Tx.Characters[TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data.Character];
                switch (chara.effect.tGetGaugeType())
                {
                    default:
                    case "Normal":
                        bIsAlreadyCleared[player] = false;
                        break;
                    case "Hard":
                    case "Extreme":
                        bIsAlreadyCleared[player] = true;
                        break;
                }
            }
            this.bIsAlreadyMaxed = new bool[5];

            this.ListDan_Number = 0;
            this.IsDanFailed = false;

            this.objHandlers = new Dictionary<CDTX.CChip, CCounter>();
            
			this.t背景テクスチャの生成();
        }


        public void ftDanReSetScoreNiji(int songNotes, int ballons)
        {
            if (songNotes == 0 && ballons == 0)
            {
                nAddScoreNiji[0] = 1000000;
            }
            else
            {
                nAddScoreNiji[0] = (double)Math.Ceiling((decimal)(1000000 - (ballons * 100)) / songNotes / 10) * 10;
            }
        }

        public void ftDanReSetBranches(bool hasBranches)
        {
            this.tBranchReset(0);

            TJAPlayer3.stage演奏ドラム画面.nレーン用表示コース[0] = CDTX.ECourse.eNormal;
            TJAPlayer3.stage演奏ドラム画面.bUseBranch[0] = hasBranches;

            // TJAPlayer3.stage選曲.r確定されたスコア.譜面情報.b譜面分岐[(int)Difficulty.Dan] = hasBranches;
        }


		public override void DeActivate()
		{
            this.bgmlength = 1;
            this.L最後に再生したHHの実WAV番号.Clear();	// #23921 2011.1.4 yyagi
			this.L最後に再生したHHの実WAV番号 = null;	//
			this.ctチップ模様アニメ.Drums = null;
			this.ctチップ模様アニメ.Guitar = null;
			this.ctチップ模様アニメ.Bass = null;
			this.ctチップ模様アニメ.Taiko = null;

            this.ctCamHMove = null;
            this.ctCamVMove = null;
            this.ctCamHScale = null;
            this.ctCamVScale = null;
            this.ctCamRotation = null;
            this.ctCamZoom = null;

            TJAPlayer3.borderColor = new Color4(0f, 0f, 0f, 0f);
            TJAPlayer3.fCamXOffset = 0.0f;
            TJAPlayer3.fCamYOffset = 0.0f;
            TJAPlayer3.fCamXScale = 1.0f;
            TJAPlayer3.fCamYScale = 1.0f;
            TJAPlayer3.fCamRotation = 0.0f;
            TJAPlayer3.fCamZoomFactor = 1.0f;

            for (int i = 0; i < 5; i++)
            {
                ctChipAnime[i] = null;
                ctChipAnimeLag[i] = null;
                TJAPlayer3.ConfigIni.nGameType[i] = eFirstGameType[i];
                bSplitLane[i] = false;
            }

			listWAV.Clear();
			listWAV = null;
            listChip = null;
			queueMixerSound.Clear();
			queueMixerSound = null;
			cInvisibleChip.Dispose();
			cInvisibleChip = null;
//			GCSettings.LatencyMode = this.gclatencymode;

			var meanLag = CLagLogger.LogAndReturnMeanLag();

			if (TJAPlayer3.IsPerformingCalibration && meanLag != null)
			{
			    var oldInputAdjustTimeMs = TJAPlayer3.ConfigIni.nInputAdjustTimeMs;
			    var newInputAdjustTimeMs = oldInputAdjustTimeMs - (int) Math.Round(meanLag.Value);
			    Trace.TraceInformation($"Calibration complete. Updating InputAdjustTime from {oldInputAdjustTimeMs}ms to {newInputAdjustTimeMs}ms.");
			    TJAPlayer3.ConfigIni.nInputAdjustTimeMs = newInputAdjustTimeMs;
			}
            this.actDan.IsAnimating = false;// IsAnimating=trueのときにそのまま選曲画面に戻ると、文字列が描画されない問題修正用。
			TJAPlayer3.tテクスチャの解放( ref this.tx背景 );

            base.DeActivate();
		}
		public override void CreateManagedResource()
		{
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
            Trace.TraceInformation("CStage演奏画面共通 リソースの開放");
            base.ReleaseManagedResource();
		}

		// その他


		//-----------------
		public class CHITCOUNTOFRANK
		{
			// Fields
			public int Good;
			public int Great;
			public int Miss;
			public int Perfect;
			public int Poor;

			// Properties
			public int this[ int index ]
			{
				get
				{
					switch ( index )
					{
						case 0:
							return this.Perfect;

						case 1:
							return this.Great;

						case 2:
							return this.Good;

						case 3:
							return this.Poor;

						case 4:
							return this.Miss;
					}
					throw new IndexOutOfRangeException();
				}
				set
				{
					switch ( index )
					{
						case 0:
							this.Perfect = value;
							return;

						case 1:
							this.Great = value;
							return;

						case 2:
							this.Good = value;
							return;

						case 3:
							this.Poor = value;
							return;

						case 4:
							this.Miss = value;
							return;
					}
					throw new IndexOutOfRangeException();
				}
			}
		}



		protected struct stmixer
		{
			internal bool bIsAdd;
			internal CSound csound;
			internal bool b演奏終了後も再生が続くチップである;
		};

        /// <summary>
        /// 分岐用のスコアをまとめるクラス。
        /// .2020.04.21.akasoko26
        /// </summary>
        public class CBRANCHSCORE
        {
            public CBRANCHSCORE cBigNotes;//大音符分岐時の情報をまとめるため
            public int nRoll;
            public int nGreat;
            public int nGood;
            public int nMiss;
            public int nScore;
            public int nADLIB;
            public int nMine;
        }

        public int[] JPOSCROLLX = new int[5];
        public int GetJPOSCROLLX(int player)
        {
            double screen_ratio = TJAPlayer3.Skin.Resolution[0] / 1280.0;
            return (int)(JPOSCROLLX[player] * screen_ratio);
        }
        public int[] NoteOriginX
        {
            get
            {
                if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
                {
                    return new int[] {
                        TJAPlayer3.Skin.nScrollField_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * 0) + GetJPOSCROLLX(0),
                        TJAPlayer3.Skin.nScrollField_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * 1) + GetJPOSCROLLX(1),
                        TJAPlayer3.Skin.nScrollField_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * 2) + GetJPOSCROLLX(2),
                        TJAPlayer3.Skin.nScrollField_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * 3) + GetJPOSCROLLX(3),
                        TJAPlayer3.Skin.nScrollField_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * 4) + GetJPOSCROLLX(4)
                    };
                }
                else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
                {
                    return new int[] {
                        TJAPlayer3.Skin.nScrollField_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * 0) + GetJPOSCROLLX(0),
                        TJAPlayer3.Skin.nScrollField_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * 1) + GetJPOSCROLLX(1),
                        TJAPlayer3.Skin.nScrollField_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * 2) + GetJPOSCROLLX(2),
                        TJAPlayer3.Skin.nScrollField_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * 3) + GetJPOSCROLLX(3)
                    };
                }
                else
                {
                    return new int[] {
                        TJAPlayer3.Skin.nScrollFieldX[0] + GetJPOSCROLLX(0),
                        TJAPlayer3.Skin.nScrollFieldX[1] + GetJPOSCROLLX(1)
                    };
                }
            }
        }

        public int[] JPOSCROLLY = new int[5];
        public int GetJPOSCROLLY(int player)
        {
            double screen_ratio = TJAPlayer3.Skin.Resolution[1] / 720.0;
            return (int)(JPOSCROLLY[player] * screen_ratio);
        }
        public int[] NoteOriginY
        {
            get
            {
                if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
                {
                    return new int[] {
                        TJAPlayer3.Skin.nScrollField_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * 0) + GetJPOSCROLLY(0),
                        TJAPlayer3.Skin.nScrollField_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * 1) + GetJPOSCROLLY(1),
                        TJAPlayer3.Skin.nScrollField_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * 2) + GetJPOSCROLLY(2),
                        TJAPlayer3.Skin.nScrollField_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * 3) + GetJPOSCROLLY(3),
                        TJAPlayer3.Skin.nScrollField_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * 4) + GetJPOSCROLLY(4)
                    };
                }
                else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
                {
                    return new int[] {
                        TJAPlayer3.Skin.nScrollField_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * 0) + GetJPOSCROLLY(0),
                        TJAPlayer3.Skin.nScrollField_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * 1) + GetJPOSCROLLY(1),
                        TJAPlayer3.Skin.nScrollField_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * 2) + GetJPOSCROLLY(2),
                        TJAPlayer3.Skin.nScrollField_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * 3) + GetJPOSCROLLY(3)
                    };
                }
                else
                {
                    return new int[] {
                        TJAPlayer3.Skin.nScrollFieldY[0] + GetJPOSCROLLY(0),
                        TJAPlayer3.Skin.nScrollFieldY[1] + GetJPOSCROLLY(1)
                    };
                }
            }
        }

        public CAct演奏AVI actAVI;
        public Rainbow Rainbow;
		protected CAct演奏チップファイアGB actChipFireGB;
		public CAct演奏Combo共通 actCombo;
		protected CAct演奏Danger共通 actDANGER;
        //protected CActFIFOBlack actFI;
        public CActFIFOStart actFI;
        protected CActFIFOBlack actFO;
        protected CActFIFOResult actFOClear;
		public    CAct演奏ゲージ共通 actGauge;

        public CAct演奏DrumsDancer actDancer;
		protected CAct演奏Drums判定文字列 actJudgeString;
		public TaikoLaneFlash actTaikoLaneFlash;
		protected CAct演奏レーンフラッシュGB共通 actLaneFlushGB;
		public CAct演奏パネル文字列 actPanel;
		public CAct演奏演奏情報 actPlayInfo;
		public CAct演奏スコア共通 actScore;
		public CAct演奏ステージ失敗 actStageFailed;
		protected CAct演奏ステータスパネル共通 actStatusPanels;
		protected CAct演奏スクロール速度 act譜面スクロール速度;
		public    C演奏判定ライン座標共通 演奏判定ライン座標;
        protected CAct演奏Drums連打 actRoll;
        protected CAct演奏Drums風船 actBalloon;
        public CAct演奏Drumsキャラクター actChara;
        protected CAct演奏Drums連打キャラ actRollChara;
        protected CAct演奏Drumsコンボ吹き出し actComboBalloon;
        protected CAct演奏Combo音声 actComboVoice;
        protected CAct演奏PauseMenu actPauseMenu;
        public CAct演奏Drumsチップエフェクト actChipEffects;
        public CAct演奏DrumsFooter actFooter;
        public CAct演奏DrumsRunner actRunner;
        public CAct演奏DrumsMob actMob;
        public Dan_Cert actDan;
        public AIBattle actAIBattle;
        public CAct演奏DrumsTrainingMode actTokkun;
        public bool bPAUSE;
        public bool[] bIsAlreadyCleared;
        public bool[] bIsAlreadyMaxed;
        protected bool b演奏にMIDI入力を使った;
        protected bool b演奏にキーボードを使った;
        protected bool b演奏にジョイパッドを使った;
        protected bool b演奏にマウスを使った;
        protected STDGBVALUE<CCounter> ctチップ模様アニメ;
        public CCounter[] ctChipAnime;
        public CCounter[] ctChipAnimeLag;
        private int bgmlength = 1;

        protected E演奏画面の戻り値 eフェードアウト完了時の戻り値;
        protected readonly int[] nチャンネル0Atoパッド08 = new int[] { 1, 2, 3, 4, 5, 7, 6, 1, 8, 0, 9, 9 };
        protected readonly int[] nチャンネル0Atoレーン07 = new int[] { 1, 2, 3, 4, 5, 7, 6, 1, 9, 0, 8, 8 };
                                                                    //                         RD LC  LP  RD
		protected readonly int[] nパッド0Atoチャンネル0A = new int[] { 0x11, 0x12, 0x13, 0x14, 0x15, 0x17, 0x16, 0x18, 0x19, 0x1a, 0x1b, 0x1c };
        protected readonly int[] nパッド0Atoパッド08 = new int[] { 1, 2, 3, 4, 5, 6, 7, 1, 8, 0, 9, 9 };// パッド画像のヒット処理用
                                                              //   HH SD BD HT LT FT CY HHO RD LC LP LBD
        protected readonly int[] nパッド0Atoレーン07 = new int[] { 1, 2, 3, 4, 5, 6, 7, 1, 9, 0, 8, 8 };
		public STDGBVALUE<CHITCOUNTOFRANK> nヒット数_Auto含まない;
		public STDGBVALUE<CHITCOUNTOFRANK> nヒット数_Auto含む;
        public bool ShowVideo;
        public int[] n良;
        public int[] nHighestCombo;
        public int[] nCombo;
        public int[] n可;
        public int[] n不可;
        public int[] n連打;
        public int[] nADLIB;
        public int[] nMine;

        public int n現在のトップChip = -1;
        protected int[] n最後に再生したBGMの実WAV番号 = new int[ 50 ];
		protected int n最後に再生したHHのチャンネル番号;
		protected List<int> L最後に再生したHHの実WAV番号;		// #23921 2011.1.4 yyagi: change "int" to "List<int>", for recording multiple wav No.
		protected STLANEVALUE<int> n最後に再生した実WAV番号;	// #26388 2011.11.8 yyagi: change "n最後に再生した実WAV番号.GUITAR" and "n最後に再生した実WAV番号.BASS"
																//							into "n最後に再生した実WAV番号";
//		protected int n最後に再生した実WAV番号.GUITAR;
//		protected int n最後に再生した実WAV番号.BASS;

		protected volatile Queue<stmixer> queueMixerSound;		// #24820 2013.1.21 yyagi まずは単純にAdd/Removeを1個のキューでまとめて管理するやり方で設計する
		protected DateTime dtLastQueueOperation;				//
		protected bool bIsDirectSound;							//
		protected double db再生速度;
		protected bool bValidScore;
//		protected bool bDTXVmode;
		protected STDGBVALUE<bool> bReverse;

		protected STDGBVALUE<Queue<CDTX.CChip>> queWailing;
		protected STDGBVALUE<CDTX.CChip> r現在の歓声Chip;
		protected CTexture txチップ;
		protected CTexture txヒットバー;

		protected CTexture tx背景;

		protected STAUTOPLAY bIsAutoPlay;		// #24239 2011.1.23 yyagi
//		protected int nRisky_InitialVar, nRiskyTime;		// #23559 2011.7.28 yyagi → CAct演奏ゲージ共通クラスに隠蔽
		protected int nPolyphonicSounds;
		protected List<CDTX.CChip>[] listChip = new List<CDTX.CChip>[5];
		protected Dictionary<int, CDTX.CWAV> listWAV;
		protected CInvisibleChip cInvisibleChip;
		protected bool bUseOSTimer;
		protected E判定表示優先度 e判定表示優先度;

        public CBRANCHSCORE[] CBranchScore = new CBRANCHSCORE[6];
        public CBRANCHSCORE[] CChartScore = new CBRANCHSCORE[5];
        public CBRANCHSCORE[] CSectionScore = new CBRANCHSCORE[5];

        public bool[] bIsGOGOTIME = new bool[5];
        public bool[] bIsMiss = new bool[5];
        public bool[] bUseBranch = new bool[ 5 ];
        public CDTX.ECourse[] n現在のコース = new CDTX.ECourse[5]; //0:普通譜面 1:玄人譜面 2:達人譜面
        public CDTX.ECourse[] n次回のコース = new CDTX.ECourse[5];
        public CDTX.ECourse[] nレーン用表示コース = new CDTX.ECourse[5];
        protected bool[] b譜面分岐中 = new bool[] { false, false, false, false, false };
        protected int[] n分岐した回数 = new int[ 5 ];
        protected int[] nJPOSSCROLL = new int[ 5 ];

        public bool[] b強制的に分岐させた = new bool[] { false, false, false, false, false };
        public bool[] bLEVELHOLD = new bool[] { false, false, false, false, false };
        protected int nListCount;

        private readonly int[] ShownLyric = new int[] { 0, 0 };
        public bool[] b連打中 = new bool[]{ false, false, false, false, false }; //奥の手
        private int[] n合計連打数 = new int[ 5 ];
        protected int[] n風船残り = new int[ 5 ];
        protected int[] n現在の連打数 = new int[ 5 ];
        public int[] Chara_MissCount;
        protected E連打State eRollState;
        protected bool[] ifp = { false, false, false, false, false };
        protected bool[] isDeniedPlaying = { false, false, false, false, false };

        protected int nタイマ番号;
        protected int n現在の音符の顔番号;

        protected int nWaitButton;

        protected int[] nStoredHit;
        private EGameType[] eFirstGameType;
        protected bool[] bSplitLane;


        public CDTX.CChip[] chip現在処理中の連打チップ = new CDTX.CChip[ 5 ];

        protected const int NOTE_GAP = 25;        
        public int nLoopCount_Clear;
        protected int[] nScore = new int[11];
        protected int[] nHand = new int[5];
        protected CSound[] soundRed = new CSound[5];
        protected CSound[] soundBlue = new CSound[5];
        protected CSound[] soundAdlib = new CSound[5];
        protected CSound[] soundClap = new CSound[5];
        public bool bDoublePlay; // 2016.08.21 kairera0467 表示だけ。
		protected Stopwatch sw;		// 2011.6.13 最適化検討用のストップウォッチ
        public int ListDan_Number;
        private bool IsDanFailed;
        private bool[] b強制分岐譜面 = new bool[5];
        private CDTX.E分岐種類 eBranch種類;
        public double nBranch条件数値A;
        public double nBranch条件数値B;
        private readonly int[] NowProcessingChip = new int[] { 0, 0, 0, 0, 0 };
        protected int nCurrentKusudamaRollCount;
        protected int nCurrentKusudamaCount;

        private float _AIBattleState;
        private Queue<float>[] _AIBattleStateBatch;
        public int AIBattleState
        {
            get
            {
                return (int)_AIBattleState;
            }
        }
        public bool bIsAIBattleWin
        {
            get;
            private set;
        }

        public class AIBattleSection
        {
            public enum EndType
            {
                None,
                Clear,
                Lose
            }

            public int Length;
            public int StartTime;
            public int EndTime;

            public EndType End;
            public bool IsAnimated;
        }

        public List<AIBattleSection> AIBattleSections;

        public int NowAIBattleSectionCount;
        public int NowAIBattleSectionTime;
        public AIBattleSection NowAIBattleSection
        {
            get
            {
                return AIBattleSections[Math.Min(NowAIBattleSectionCount, AIBattleSections.Count - 1)];
            }
        }

        private void PassAIBattleSection()
        {
            if (AIBattleState >= 0)
            {
                NowAIBattleSection.End = AIBattleSection.EndType.Clear;
                if (TJAPlayer3.ConfigIni.nAILevel < 10)
                    TJAPlayer3.ConfigIni.nAILevel++;
            }
            else
            {
                NowAIBattleSection.End = AIBattleSection.EndType.Lose;
                if (TJAPlayer3.ConfigIni.nAILevel > 1)
                    TJAPlayer3.ConfigIni.nAILevel--;
            }
            actAIBattle.BatchAnimeCounter.CurrentValue = 0;
            _AIBattleState = 0;

            for (int i = 0; i < 5; i++)
            {
                this.CSectionScore[i] = new CBRANCHSCORE();
            }

            int clearCount = 0;
            for (int i = 0; i < TJAPlayer3.stage演奏ドラム画面.AIBattleSections.Count; i++)
            {
                if (TJAPlayer3.stage演奏ドラム画面.AIBattleSections[i].End == CStage演奏画面共通.AIBattleSection.EndType.Clear)
                {
                    clearCount++;
                }
            }
            bIsAIBattleWin = clearCount >= TJAPlayer3.stage演奏ドラム画面.AIBattleSections.Count / 2.0;
        }

        private void AIRegisterInput(int nPlayer, float move)
        {
            if (nPlayer < 2 && nPlayer >= 0)
            {
                _AIBattleStateBatch[nPlayer].Enqueue(move);
                while (_AIBattleStateBatch[0].Count > 0 && _AIBattleStateBatch[1].Count > 0)
                {
                    _AIBattleState += _AIBattleStateBatch[0].Dequeue() - _AIBattleStateBatch[1].Dequeue();
                    _AIBattleState = Math.Max(Math.Min(_AIBattleState, 9), -9);
                }
            }
        }

        private void UpdateCharaCounter(int nPlayer)
        {
            for (int i = 0; i < 5; i++)
            {
                ctChipAnime[i] = new CCounter(0, 3, 60.0 / TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[i] * 1 / 4 / TJAPlayer3.ConfigIni.SongPlaybackSpeed, SoundManager.PlayTimer);
            }

            TJAPlayer3.stage演奏ドラム画面.PuchiChara.ChangeBPM(60.0 / TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[nPlayer] / TJAPlayer3.ConfigIni.SongPlaybackSpeed);
        }

        public void AddMixer( CSound cs, bool _b演奏終了後も再生が続くチップである )
		{
			stmixer stm = new stmixer()
			{
				bIsAdd = true,
				csound = cs,
				b演奏終了後も再生が続くチップである = _b演奏終了後も再生が続くチップである
			};
			queueMixerSound.Enqueue( stm );
//		Debug.WriteLine( "★Queue: add " + Path.GetFileName( stm.csound.strファイル名 ));
		}
		public void RemoveMixer( CSound cs )
		{
			stmixer stm = new stmixer()
			{
				bIsAdd = false,
				csound = cs,
				b演奏終了後も再生が続くチップである = false
			};
			queueMixerSound.Enqueue( stm );
//		Debug.WriteLine( "★Queue: remove " + Path.GetFileName( stm.csound.strファイル名 ));
		}
		public void ManageMixerQueue()
		{
			// もしサウンドの登録/削除が必要なら、実行する
			if ( queueMixerSound.Count > 0 )
			{
				//Debug.WriteLine( "☆queueLength=" + queueMixerSound.Count );
				DateTime dtnow = DateTime.Now;
				TimeSpan ts = dtnow - dtLastQueueOperation;
				if ( ts.Milliseconds > 7 )
				{
					for ( int i = 0; i < 2 && queueMixerSound.Count > 0; i++ )
					{
						dtLastQueueOperation = dtnow;
						stmixer stm = queueMixerSound.Dequeue();
						if ( stm.bIsAdd )
						{
							TJAPlayer3.SoundManager.AddMixer( stm.csound, db再生速度, stm.b演奏終了後も再生が続くチップである );
						}
						else
						{
							TJAPlayer3.SoundManager.RemoveMixer( stm.csound );
						}
					}
				}
			}
		}



	    internal ENoteJudge e指定時刻からChipのJUDGEを返す(long nTime, CDTX.CChip pChip, int player = 0)
	    {
	        var e判定 = e指定時刻からChipのJUDGEを返すImpl(nTime, pChip, player);

	        // When performing calibration, reduce audio distraction from user input.
	        // For users who play primarily by watching notes cross the judgment position,
	        // you might think that we want them to see visual judgment feedback during
	        // calibration, but we do not. Humans are remarkably good at adjusting
	        // the timing of their own physical movement, even without realizing it.
	        // We are calibrating their input timing for the purposes of judgment.
	        // We do not want them subconsciously playing early so as to line up
	        // their hits with the perfect, good, etc. judgment results based on their
	        // current (and soon to be replaced) input adjust time values.
	        // Instead, we want them focused on the sounds of their keyboard, tatacon,
	        // other controller, etc. and the visuals of notes crossing the judgment position.
	        if (TJAPlayer3.IsPerformingCalibration)
	        {
	            return e判定 < ENoteJudge.Good ? ENoteJudge.Good : e判定;
	        }
	        else
	        {
	            return e判定;
	        }
	    }

        private bool tEasyTimeZones(int nPlayer)
        {
            bool _timingzonesAreEasy = false;

            int diff = TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[nPlayer];

            // Diff = Normal or Easy
            if (diff <= (int)Difficulty.Normal)
            {
                _timingzonesAreEasy = true;
            }

            // Diff = Dan and current song is Normal or Easy
            if (diff == (int)Difficulty.Dan)
            {
                int _nb = TJAPlayer3.stage演奏ドラム画面.actDan.NowShowingNumber;
                var _danSongs = TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs;

                if (_nb < _danSongs.Count)
                {
                    var _currentDiff = _danSongs[_nb].Difficulty;
                    if (_currentDiff <= (int)Difficulty.Normal)
                        _timingzonesAreEasy = true;

                }
            }
            
            // Diff = Tower and SIDE is Normal
            if (diff == (int)Difficulty.Tower)
            {
                _timingzonesAreEasy = TJAPlayer3.stageSongSelect.rChoosenSong.nSide == CDTX.ESide.eNormal;
            }

            return _timingzonesAreEasy;
        }

        private void tIncreaseComboDan(int danSong)
        {
            this.nCombo[danSong]++;
            if (this.nCombo[danSong] > this.nHighestCombo[danSong])
                this.nHighestCombo[danSong] = this.nCombo[danSong];
        }

		private ENoteJudge e指定時刻からChipのJUDGEを返すImpl( long nTime, CDTX.CChip pChip, int player = 0 )
		{

			if ( pChip != null )
			{
				pChip.nLag = (int) ( nTime - pChip.n発声時刻ms );		// #23580 2011.1.3 yyagi: add "nInputAdjustTime" to add input timing adjust feature
				int nDeltaTime = Math.Abs( pChip.nLag );
                //Debug.WriteLine("nAbsTime=" + (nTime - pChip.n発声時刻ms) + ", nDeltaTime=" + (nTime + nInputAdjustTime - pChip.n発声時刻ms));
                if(NotesManager.IsRoll(pChip) || NotesManager.IsFuzeRoll(pChip))
                {
                    if ((SoundManager.PlayTimer.NowTimeMs * TJAPlayer3.ConfigIni.SongPlaybackSpeed) > pChip.n発声時刻ms && (SoundManager.PlayTimer.NowTimeMs * TJAPlayer3.ConfigIni.SongPlaybackSpeed) < pChip.nノーツ終了時刻ms)
                    {
                        return ENoteJudge.Perfect;
				    }
                }
                else if(NotesManager.IsGenericBalloon(pChip))
                {
                    if ((SoundManager.PlayTimer.NowTimeMs * TJAPlayer3.ConfigIni.SongPlaybackSpeed) >= pChip.n発声時刻ms - 17 && (SoundManager.PlayTimer.NowTimeMs * TJAPlayer3.ConfigIni.SongPlaybackSpeed) < pChip.nノーツ終了時刻ms)
                    {
                        return ENoteJudge.Perfect;
				    }
                }

                
                
                // To change later to adapt to Tower Ama-kuchi
                //diff = Math.Min(diff, (int)Difficulty.Oni);

                int actual = TJAPlayer3.GetActualPlayer(player);

                int timingShift = TJAPlayer3.ConfigIni.nTimingZones[actual];

                bool _timingzonesAreEasy = tEasyTimeZones(player);

                CConfigIni.CTimingZones tz = (_timingzonesAreEasy == true) ? TJAPlayer3.ConfigIni.tzLevels[timingShift] : TJAPlayer3.ConfigIni.tzLevels[2 + timingShift];

                if (nDeltaTime <= tz.nGoodZone * TJAPlayer3.ConfigIni.SongPlaybackSpeed)
                {
                    return ENoteJudge.Perfect;
				}
                if (nDeltaTime <= tz.nOkZone * TJAPlayer3.ConfigIni.SongPlaybackSpeed)
                {
                    if ( TJAPlayer3.ConfigIni.bJust[actual] == 1 && NotesManager.IsMissableNote(pChip)) // Just
                        return ENoteJudge.Poor;
					return ENoteJudge.Good;
				}

                
                if (nDeltaTime <= tz.nBadZone * TJAPlayer3.ConfigIni.SongPlaybackSpeed)
                {
                    if (TJAPlayer3.ConfigIni.bJust[actual] == 2 || !NotesManager.IsMissableNote(pChip)) // Safe
                        return ENoteJudge.Good;
                    return ENoteJudge.Poor;
                }
                
			}
			return ENoteJudge.Miss;
		}

		protected CDTX.CChip r指定時刻に一番近い連打Chip_ヒット未済問わず不可視考慮( long nTime, int nChannel, int nInputAdjustTime, int nPlayer )
		{
			//sw2.Start();
//Trace.TraceInformation( "NTime={0}, nChannel={1:x2}", nTime, nChannel );
			nTime += nInputAdjustTime;						// #24239 2011.1.23 yyagi InputAdjust

			int nIndex_InitialPositionSearchingToPast;
			if ( this.n現在のトップChip == -1 )				// 演奏データとして1個もチップがない場合は
			{
				//sw2.Stop();
				return null;
			}

            List<CDTX.CChip> playerListChip = listChip[ nPlayer ];
            int count = playerListChip.Count;
			int nIndex_NearestChip_Future = nIndex_InitialPositionSearchingToPast = this.n現在のトップChip;
			if ( this.n現在のトップChip >= count )			// その時点で演奏すべきチップが既に全部無くなっていたら
			{
				nIndex_NearestChip_Future = nIndex_InitialPositionSearchingToPast = count - 1;
			}
			//int nIndex_NearestChip_Future;	// = nIndex_InitialPositionSearchingToFuture;
			//while ( nIndex_NearestChip_Future < count )		// 未来方向への検索
			for ( ; nIndex_NearestChip_Future < count; nIndex_NearestChip_Future++)
			{
				if ( ( ( 0x11 <= nChannel ) && ( nChannel <= 0x17 ) ) || nChannel == 0x19 )
				{
                    CDTX.CChip chip = playerListChip[ nIndex_NearestChip_Future ];

                    if ( chip.nチャンネル番号 == nChannel )
					{
						if ( chip.n発声時刻ms > nTime )
						{
							break;
						}
                        if( chip.nコース != this.n次回のコース[ nPlayer ] )
                        {
                            break;
                        }
						nIndex_InitialPositionSearchingToPast = nIndex_NearestChip_Future;
					}
					continue;	// ほんの僅かながら高速化
				}

				// nIndex_NearestChip_Future++;
			}
			int nIndex_NearestChip_Past = nIndex_InitialPositionSearchingToPast;
			//while ( nIndex_NearestChip_Past >= 0 )			// 過去方向への検索
			for ( ; nIndex_NearestChip_Past >= 0; nIndex_NearestChip_Past-- )
			{
				if ( ((( 0x15 <= nChannel ) && ( nChannel <= 0x17 ) || nChannel == 0x19) || (nChannel == 0x20 || nChannel == 0x21)) )
				{
                    CDTX.CChip chip = playerListChip[ nIndex_NearestChip_Past ];

					if ( ( ( chip.nチャンネル番号 == nChannel ) )  )
					{
						break;
					}
				}
				// nIndex_NearestChip_Past--;
			}

			if ( nIndex_NearestChip_Future >= count )
			{
				if ( nIndex_NearestChip_Past < 0 )	// 検索対象が過去未来どちらにも見つからなかった場合
				{
					return null;
				}
				else 								// 検索対象が未来方向には見つからなかった(しかし過去方向には見つかった)場合
				{
					//sw2.Stop();
					return playerListChip[ nIndex_NearestChip_Past ];
				}
			}
			else if ( nIndex_NearestChip_Past < 0 )	// 検索対象が過去方向には見つからなかった(しかし未来方向には見つかった)場合
			{
				//sw2.Stop();
				return playerListChip[ nIndex_NearestChip_Future ];
			}
													// 検索対象が過去未来の双方に見つかったなら、より近い方を採用する
			CDTX.CChip nearestChip_Future = playerListChip[ nIndex_NearestChip_Future ];
			CDTX.CChip nearestChip_Past   = playerListChip[ nIndex_NearestChip_Past ];
			int nDiffTime_Future = Math.Abs( (int) ( nTime - nearestChip_Future.n発声時刻ms ) );
			int nDiffTime_Past   = Math.Abs( (int) ( nTime - nearestChip_Past.n発声時刻ms ) );
			if ( nDiffTime_Future >= nDiffTime_Past )
			{
				//sw2.Stop();
				return nearestChip_Past;
			}
			//sw2.Stop();
			return nearestChip_Future;
		}
		protected void tサウンド再生( CDTX.CChip pChip, int nPlayer )
		{
            var _gt = TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(nPlayer)];
			int index = pChip.nチャンネル番号;
            
            if (index == 0x11 || index == 0x13 || index == 0x1A || index == 0x101)
            {
                this.soundRed[pChip.nPlayerSide]?.PlayStart();
                if ((index == 0x13 && _gt == EGameType.KONGA) || index == 0x101)
                {
                    this.soundBlue[pChip.nPlayerSide]?.PlayStart();
                }
            }
            else if (index == 0x12 || index == 0x14 || index == 0x1B)
            {
                if (index == 0x14 && _gt == EGameType.KONGA)
                {
                    this.soundClap[pChip.nPlayerSide]?.PlayStart();
                }
                else
                {
                    this.soundBlue[pChip.nPlayerSide]?.PlayStart();
                }

                
            }
            else if (index == 0x1F)
            {
                this.soundAdlib[pChip.nPlayerSide]?.PlayStart();
            }

            if (this.nHand[nPlayer] == 0)
                this.nHand[ nPlayer ]++;
            else
                this.nHand[ nPlayer ] = 0;
		}

		protected void tステータスパネルの選択()
		{
			if ( TJAPlayer3.bコンパクトモード )
			{
				this.actStatusPanels.tラベル名からステータスパネルを決定する( null );
			}
			else if ( TJAPlayer3.stageSongSelect.rChoosenSong != null )
			{
				this.actStatusPanels.tラベル名からステータスパネルを決定する( TJAPlayer3.stageSongSelect.rChoosenSong.ar難易度ラベル[ TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0]] );
			}
		}

        protected bool tRollProcess( CDTX.CChip pChip, double dbProcess_time, int num, int sort, int Input, int nPlayer )
        {
            if (dbProcess_time >= pChip.n発声時刻ms && dbProcess_time < pChip.nノーツ終了時刻ms)
            {
                if( pChip.nRollCount == 0 ) //連打カウントが0の時
                {
                    this.actRoll.b表示[ nPlayer ] = true;
                    this.n現在の連打数[ nPlayer ] = 0;
                    this.actRoll.t枠表示時間延長(nPlayer, true);
                }
                else
                {
                    this.actRoll.t枠表示時間延長(nPlayer, false);
                }
                this.b連打中[ nPlayer ] = true;
                if(this.actRoll.ct連打アニメ[nPlayer].IsUnEnded)
                {
                    this.actRoll.ct連打アニメ[nPlayer] = new CCounter(0, 9, 14, TJAPlayer3.Timer);
                    this.actRoll.ct連打アニメ[nPlayer].CurrentValue = 1;
                }
                else
                {
                    this.actRoll.ct連打アニメ[nPlayer] = new CCounter(0, 9, 14, TJAPlayer3.Timer);
                }


                pChip.RollEffectLevel += 10;
                if(pChip.RollEffectLevel >= 100)
                {
                    pChip.RollEffectLevel = 100;
                    pChip.RollInputTime = new CCounter(0, 1500, 1, TJAPlayer3.Timer);
                    pChip.RollDelay?.Stop();
                } else
                {
                    pChip.RollInputTime = new CCounter(0, 150, 1, TJAPlayer3.Timer);
                    pChip.RollDelay?.Stop(); 
                }

                if ( pChip.nチャンネル番号 == 0x15 )
                    this.eRollState = E連打State.roll;
                else
                    this.eRollState = E連打State.rollB;

                pChip.nRollCount++;

                if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                    this.n連打[actDan.NowShowingNumber]++;

                this.n現在の連打数[ nPlayer ]++;
                
                this.CBranchScore[ nPlayer ].nRoll++;
                this.CChartScore[nPlayer].nRoll++;
                this.CSectionScore[nPlayer].nRoll++;

                this.n合計連打数[ nPlayer ]++;
                if(TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Dan) this.actRollChara.Start(nPlayer);

                //2017.01.28 DD CDTXから直接呼び出す
                if (!TJAPlayer3.ConfigIni.ShinuchiMode) //2018.03.11 kairera0467 チップに埋め込んだフラグから読み取る
                {
                    if (pChip.bGOGOTIME) //non-Shin'uchi / GoGo-Time　／　真打OFF・ゴーゴータイム
                    {
                        // 旧配点・旧筐体配点
                        if (TJAPlayer3.DTX.nScoreModeTmp == 0 || TJAPlayer3.DTX.nScoreModeTmp == 1)
                        {
                            if (pChip.nチャンネル番号 == 0x15)
                                this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, (long)(300 * 1.2f), nPlayer);
                            else
                                this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, (long)(360 * 1.2f), nPlayer);
                        }
                        // 新配点
                        else
                        {
                            if (pChip.nチャンネル番号 == 0x15)
                                this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, (long)(100 * 1.2f), nPlayer);
                            else
                                this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, (long)(200 * 1.2f), nPlayer);
                        }
                    }
                    else //non-Shin'uchi / non-GoGo-Time　／　真打OFF・非ゴーゴータイム
                    {
                        // 旧配点・旧筐体配点
                        if (TJAPlayer3.DTX.nScoreModeTmp == 0 || TJAPlayer3.DTX.nScoreModeTmp == 1)
                        {
                            if (pChip.nチャンネル番号 == 0x15)
                                this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, (long)(300L), nPlayer);
                            else
                                this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, (long)(360L), nPlayer);
                        }
                        // 新配点
                        else
                        {
                            if (pChip.nチャンネル番号 == 0x15)
                                this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, (long)(100L), nPlayer);
                            else
                                this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, (long)(200L), nPlayer);
                        }
                    }
                }
                else  //Shin'uchi　／　真打
                {
                    // 旧配点・旧筐体配点
                    if (TJAPlayer3.DTX.nScoreModeTmp == 0 || TJAPlayer3.DTX.nScoreModeTmp == 1)
                    {
                        if (pChip.nチャンネル番号 == 0x15)
                            this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, 100L, nPlayer);
                        else
                            this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, 100L, nPlayer);
                    }
                    // 新配点
                    else
                    {
                        if (pChip.nチャンネル番号 == 0x15)
                            this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, 100L, nPlayer);
                        else
                            this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, 100L, nPlayer);
                    }
                }

                EGameType _gt = TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(nPlayer)];

                //赤か青かの分岐
                if ( sort == 0|| sort == 2 )
                {
                    this.soundRed[pChip.nPlayerSide]?.PlayStart();

                    if (pChip.nチャンネル番号 == 0x15 || _gt == EGameType.KONGA || (_gt == EGameType.TAIKO && pChip.nチャンネル番号 == 0x21))
                    {
                        //CDTXMania.Skin.soundRed.t再生する();
                        //CDTXMania.stage演奏ドラム画面.actChipFireTaiko.Start( 1, nPlayer );
                        TJAPlayer3.stage演奏ドラム画面.FlyingNotes.Start(1, nPlayer, true);
                    }
                    else
                    {
                        //CDTXMania.Skin.soundRed.t再生する();
                        //CDTXMania.stage演奏ドラム画面.actChipFireTaiko.Start( 3, nPlayer );
                        TJAPlayer3.stage演奏ドラム画面.FlyingNotes.Start(3, nPlayer, true);
                    }
                }
                else if (sort == 1 || sort == 3)
                {
                    this.soundBlue[pChip.nPlayerSide]?.PlayStart();

                    if (pChip.nチャンネル番号 == 0x15 || _gt == EGameType.KONGA || (_gt == EGameType.TAIKO && pChip.nチャンネル番号 == 0x21))
                    {
                        //CDTXMania.Skin.soundBlue.t再生する();
                        //CDTXMania.stage演奏ドラム画面.actChipFireTaiko.Start( 2, nPlayer );
                        TJAPlayer3.stage演奏ドラム画面.FlyingNotes.Start(2, nPlayer, true);
                    }
                    else
                    {
                        //CDTXMania.Skin.soundBlue.t再生する();
                        //CDTXMania.stage演奏ドラム画面.actChipFireTaiko.Start( 4, nPlayer );
                        TJAPlayer3.stage演奏ドラム画面.FlyingNotes.Start(4, nPlayer, true);
                    }
                }
                else if (sort == 4)
                {
                    this.soundClap[pChip.nPlayerSide]?.PlayStart();
                    TJAPlayer3.stage演奏ドラム画面.FlyingNotes.Start(4, nPlayer, true);
                }

                //TJAPlayer3.stage演奏ドラム画面.actTaikoLaneFlash.PlayerLane[nPlayer].Start(PlayerLane.FlashType.Hit);
            }
            else
            {
                this.b連打中[ nPlayer ] = false;
                return true;
            }

            return false;
        }

        protected bool tBalloonProcess( CDTX.CChip pChip, double dbProcess_time, int player )
        {
            //if( dbProcess_time >= pChip.n発声時刻ms && dbProcess_time < pChip.nノーツ終了時刻ms )
            long nowTime = (long)(SoundManager.PlayTimer.NowTimeMs * TJAPlayer3.ConfigIni.SongPlaybackSpeed);
            bool IsKusudama = NotesManager.IsKusudama(pChip);
            bool IsFuze = NotesManager.IsFuzeRoll(pChip);

            int rollCount = pChip.nRollCount;
            int balloon = pChip.nBalloon;
            
            if (IsKusudama)
            {
                nCurrentKusudamaRollCount++;
                rollCount = nCurrentKusudamaRollCount;
                balloon = nCurrentKusudamaCount;
            }

            if ((int)nowTime >= pChip.n発声時刻ms 
                && (int)nowTime <= pChip.nノーツ終了時刻ms)
            {

                if (IsKusudama)
                {
                    if (nCurrentKusudamaCount > 0)
                    {
                        actChara.ChangeAnime(player, CAct演奏Drumsキャラクター.Anime.Kusudama_Breaking, true);
                        for(int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                        {
                            this.b連打中[i] = true;
                            

                            if (this.actBalloon.ct風船アニメ[i].IsUnEnded)
                            {
                                this.actBalloon.ct風船アニメ[i] = new CCounter(0, 9, 14, TJAPlayer3.Timer);
                                this.actBalloon.ct風船アニメ[i].CurrentValue = 1;
                            }
                            else
                            {
                                this.actBalloon.ct風船アニメ[i] = new CCounter(0, 9, 14, TJAPlayer3.Timer);
                            }
                        }
                    }
                }
                else 
                {
                    this.b連打中[player] = true;
                    actChara.ChangeAnime(player, CAct演奏Drumsキャラクター.Anime.Balloon_Breaking, true);
                    

                    if (this.actBalloon.ct風船アニメ[player].IsUnEnded)
                    {
                        this.actBalloon.ct風船アニメ[player] = new CCounter(0, 9, 14, TJAPlayer3.Timer);
                        this.actBalloon.ct風船アニメ[player].CurrentValue = 1;
                    }
                    else
                    {
                        this.actBalloon.ct風船アニメ[player] = new CCounter(0, 9, 14, TJAPlayer3.Timer);
                    }
                }
                
                this.eRollState = E連打State.balloon;


                
                if (IsKusudama)
                {
                    //pChip.nRollCount = nCurrentKusudamaRollCount;
                    for(int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                    {
                        pChip.nRollCount = nCurrentKusudamaRollCount;
                        this.n風船残り[i] = balloon - rollCount;
                    }
                }
                else
                {
                    pChip.nRollCount++;
                    rollCount = pChip.nRollCount;
                    this.n風船残り[player] = balloon - rollCount;
                }

                if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                    this.n連打[actDan.NowShowingNumber]++;
                this.CBranchScore[player].nRoll++;
                this.CChartScore[player].nRoll++;
                this.CSectionScore[player].nRoll++;

                this.n合計連打数[player]++; //  成績発表の連打数に風船を含めるように (AioiLight)

                //分岐のための処理。実装してない。

                //赤か青かの分岐

                if (!TJAPlayer3.ConfigIni.ShinuchiMode)
                {
                    if (pChip.bGOGOTIME) //non-Shin'uchi / GoGo-Time　／　真打OFF・ゴーゴータイム
                    {
                        if (balloon == rollCount)
                            this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, 6000L, player);
                        else
                            this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, 360L, player);
                    }
                    else //non-Shin'uchi / non-GoGo-Time　／　真打OFF・非ゴーゴータイム
                    {
                        if (balloon == rollCount)
                            this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, 5000L, player);
                        else
                            this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, 300L, player);
                    }
                }
                else //Shin'uchi　／　真打
                {
                    this.actScore.Add(EInstrumentPad.TAIKO, this.bIsAutoPlay, 100L, player);
                }
                //CDTXMania.Skin.soundRed.t再生する();
                this.soundRed[pChip.nPlayerSide]?.PlayStart();


                if (this.n風船残り[player] <= 0)
                {
                    if (IsKusudama)
                    {
                        TJAPlayer3.Skin.soundKusudama.tPlay();
                        pChip.bHit = true;
                        pChip.IsHitted = true;
                        chip現在処理中の連打チップ[player].bHit = true;
                        pChip.b可視 = false;
                        nCurrentKusudamaCount = 0;
                        
                        actBalloon.KusuBroke();
                        for(int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                        {
                            actChara.ChangeAnime(i, CAct演奏Drumsキャラクター.Anime.Kusudama_Broke, true);
                            if (actChara.CharaAction_Balloon_Delay[i] != null) actChara.CharaAction_Balloon_Delay[i] = new CCounter(0, TJAPlayer3.Skin.Characters_Balloon_Delay[actChara.iCurrentCharacter[i]] - 1, 1, TJAPlayer3.Timer);
                        }
                    }
                    else
                    {
                        //ﾊﾟｧｰﾝ
                        TJAPlayer3.Skin.soundBalloon.tPlay();
                        //CDTXMania.stage演奏ドラム画面.actChipFireTaiko.Start( 3, player ); //ここで飛ばす。飛ばされるのは大音符のみ。
                        TJAPlayer3.stage演奏ドラム画面.FlyingNotes.Start(3, player);
                        TJAPlayer3.stage演奏ドラム画面.Rainbow.Start(player);
                        //CDTXMania.stage演奏ドラム画面.actChipFireD.Start( 0, player );
                        pChip.bHit = true;
                        pChip.IsHitted = true;
                        chip現在処理中の連打チップ[player].bHit = true;
                        //this.b連打中 = false;
                        //this.actChara.b風船連打中 = false;
                        pChip.b可視 = false;
                        {
                            actChara.ChangeAnime(player, CAct演奏Drumsキャラクター.Anime.Balloon_Broke, true);
                            if (actChara.CharaAction_Balloon_Delay[player] != null) actChara.CharaAction_Balloon_Delay[player] = new CCounter(0, TJAPlayer3.Skin.Characters_Balloon_Delay[actChara.iCurrentCharacter[player]] - 1, 1, TJAPlayer3.Timer);
                        }
                    }
                    this.eRollState = E連打State.none; // Unused variable ?
                }
            }
            else
            {
                if (IsKusudama)
                {
                    for(int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                    {
                        if (chip現在処理中の連打チップ[i] != null)
                            chip現在処理中の連打チップ[i].bHit = true;
                        this.b連打中[i] = false;
                        this.actChara.b風船連打中[i] = false;
                        nCurrentKusudamaCount = 0;
                    }
                }
                else
                {
                    if (chip現在処理中の連打チップ[player] != null)
                        chip現在処理中の連打チップ[player].bHit = true;
                    this.b連打中[player] = false;
                    this.actChara.b風船連打中[player] = false;
                }
                return false;
            }
            return true;
        }

		protected abstract ENoteJudge tチップのヒット処理( long nHitTime, CDTX.CChip pChip, bool bCorrectLane );

		protected ENoteJudge tチップのヒット処理( long nHitTime, CDTX.CChip pChip, EInstrumentPad screenmode, bool bCorrectLane, int nNowInput )
		{
			return tチップのヒット処理( nHitTime, pChip, screenmode, bCorrectLane, nNowInput, 0 );
		}
        protected unsafe ENoteJudge tチップのヒット処理(long nHitTime, CDTX.CChip pChip, EInstrumentPad screenmode, bool bCorrectLane, int nNowInput, int nPlayer, bool rollEffectHit = false)
        {
            //unsafeコードにつき、デバッグ中の変更厳禁!

            bool bAutoPlay = TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[nPlayer];
            bool bBombHit = false;

            switch (nPlayer)
            {
                case 1:
                    bAutoPlay = TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[nPlayer] || TJAPlayer3.ConfigIni.bAIBattleMode;
                    break;
            }

            if (!pChip.b可視)
                return ENoteJudge.Auto;

            if (!NotesManager.IsGenericRoll(pChip))
            {
                if (!pChip.IsMissed)//通り越したチップでなければ判定！
                {
                    pChip.bHit = true;
                    pChip.IsHitted = true;
                }
            }

			ENoteJudge eJudgeResult = ENoteJudge.Auto;
            switch (pChip.e楽器パート)
            {
                case EInstrumentPad.DRUMS:
				case EInstrumentPad.GUITAR:
				case EInstrumentPad.BASS:
					break;
				case EInstrumentPad.TAIKO:
					{
                        //連打が短すぎると発声されない
						eJudgeResult = (bCorrectLane)? this.e指定時刻からChipのJUDGEを返す( nHitTime, pChip, nPlayer ) : ENoteJudge.Miss;

                        // AI judges
                        eJudgeResult = AlterJudgement(nPlayer, eJudgeResult, true);

                        if (!bAutoPlay && eJudgeResult != ENoteJudge.Miss)
					    {
					        CLagLogger.Add(nPlayer, pChip);
                        }

                        var puchichara = TJAPlayer3.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(TJAPlayer3.GetActualPlayer(nPlayer))];

                        if (NotesManager.IsRoll(pChip))
                        {
                            #region[ Drumroll ]
                            //---------------------------
                            this.b連打中[nPlayer] = true;
                            if (bAutoPlay || rollEffectHit)
                            {
                                int rollSpeed = bAutoPlay ? TJAPlayer3.ConfigIni.nRollsPerSec : puchichara.effect.Autoroll;
                                if (TJAPlayer3.ConfigIni.bAIBattleMode && nPlayer == 1)
                                    rollSpeed = TJAPlayer3.ConfigIni.apAIPerformances[TJAPlayer3.ConfigIni.nAILevel - 1].nRollSpeed;

                                if (this.bPAUSE == false && rollSpeed > 0) // && TJAPlayer3.ConfigIni.bAuto先生の連打)
                                {
                                    if (((SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed) 
                                        * TJAPlayer3.ConfigIni.SongPlaybackSpeed) 
                                            > (pChip.n発声時刻ms + (1000.0 / (double)rollSpeed) * pChip.nRollCount))
                                    {
                                        EGameType _gt = TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(nPlayer)];
                                        int nLane = 0;

                                        if (this.nHand[nPlayer] == 0)
                                            this.nHand[nPlayer]++;
                                        else
                                            this.nHand[nPlayer] = 0;

                                        if (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[nPlayer] < 0 && (pChip.eScrollMode == EScrollMode.HBSCROLL))
                                            pChip.fBMSCROLLTime -= TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[nPlayer] * -0.05;

                                        TJAPlayer3.stage演奏ドラム画面.actTaikoLaneFlash.PlayerLane[nPlayer].Start(PlayerLane.FlashType.Red);
                                        //CDTXMania.stage演奏ドラム画面.actChipFireTaiko.Start( pChip.nチャンネル番号 == 0x15 ? 1 : 3, nPlayer );
                                        TJAPlayer3.stage演奏ドラム画面.FlyingNotes.Start(pChip.nチャンネル番号 == 0x15 ? 1 : 3, nPlayer, true);
                                        TJAPlayer3.stage演奏ドラム画面.actMtaiko.tMtaikoEvent(pChip.nチャンネル番号, this.nHand[nPlayer], nPlayer);


                                        if (pChip.nチャンネル番号 == 0x20 && _gt == EGameType.KONGA) nLane = 4;
                                        else if (pChip.nチャンネル番号 == 0x21 && _gt == EGameType.KONGA) nLane = 1;

                                        this.tRollProcess(pChip, (SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed), 1, nLane, 0, nPlayer);
                                    }
                                }
                            }
                            if (!bAutoPlay && !rollEffectHit)
                            {
                                this.eRollState = E連打State.roll;
                                this.tRollProcess(pChip, (SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed), 1, nNowInput, 0, nPlayer);
                            }

                            break;
                            //---------------------------
                            #endregion
                        }
                        else if (NotesManager.IsGenericBalloon(pChip))
                        {
                            #region [ Balloon ]

                            bool IsKusudama = NotesManager.IsKusudama(pChip);

                            if (IsKusudama)
                            {
                                if (nCurrentKusudamaCount > 0)
                                {
                                    /*
                                    if (!this.b連打中[nPlayer] && nPlayer == 0)
                                    {
                                        actBalloon.KusuIn();
                                        actChara.KusuIn();
                                    }
                                    for(int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                                    {
                                        this.b連打中[i] = true;
                                        this.actChara.b風船連打中[i] = true;
                                    }
                                    */
                                }
                            }
                            else
                            {
                                this.b連打中[nPlayer] = true;
                                this.actChara.b風船連打中[nPlayer] = true;
                            }

                            if (bAutoPlay || rollEffectHit)
                            {

                                int rollCount = pChip.nRollCount;
                                int balloon = pChip.nBalloon;
                                if (IsKusudama)
                                {
                                    /*
                                    var ts = pChip.db発声時刻ms;
                                    var km = TJAPlayer3.DTX.kusudaMAP;

                                    if (km.ContainsKey(ts))
                                    {
                                        rollCount = km[ts].nRollCount;
                                        balloon = km[ts].nBalloon;
                                    }
                                    */
                                    rollCount = nCurrentKusudamaRollCount;
                                    balloon = nCurrentKusudamaCount;
                                    
                                }

                                if (balloon != 0 && this.bPAUSE == false)
                                {
                                    int rollSpeed = bAutoPlay ? balloon : puchichara.effect.Autoroll;

                                    int balloonDuration = bAutoPlay ? (pChip.nノーツ終了時刻ms - pChip.n発声時刻ms) : 1000;

                                    if ((SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed) > 
                                        (pChip.n発声時刻ms + (balloonDuration / (double)rollSpeed) * rollCount))
                                    {
                                        if (this.nHand[nPlayer] == 0)
                                            this.nHand[nPlayer]++;
                                        else
                                            this.nHand[nPlayer] = 0;

                                        TJAPlayer3.stage演奏ドラム画面.actTaikoLaneFlash.PlayerLane[nPlayer].Start(PlayerLane.FlashType.Red);
                                        TJAPlayer3.stage演奏ドラム画面.actMtaiko.tMtaikoEvent(pChip.nチャンネル番号, this.nHand[nPlayer], nPlayer);

                                        this.tBalloonProcess(pChip, (SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed), nPlayer);
                                    }
                                }
                            }
                            if (!bAutoPlay && !rollEffectHit)
                            {
                                if (!IsKusudama || nCurrentKusudamaCount > 0)
                                {
                                    this.tBalloonProcess(pChip, (SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed), nPlayer);
                                }
                            }
                            break;
                            #endregion
                        }
                        else if (NotesManager.IsRollEnd(pChip))
                        {
                            if (pChip.nノーツ終了時刻ms <= (SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed))
                            {
                                if (NotesManager.IsKusudama(pChip))
                                {
                                    for(int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                                    {
                                        chip現在処理中の連打チップ[i].bHit = true;
                                        this.b連打中[i] = false;
                                    }
                                }
                                else 
                                {
                                    this.b連打中[nPlayer] = false;
                                }
                              
                                
                                // this.actChara.b風船連打中[nPlayer] = false;

                                pChip.bHit = true;
                                pChip.IsHitted = true;
                                break;
                            }
                        }
                        else if (NotesManager.IsADLIB(pChip))
                        {
                            if (eJudgeResult != ENoteJudge.Auto && eJudgeResult != ENoteJudge.Miss)
                            {
                                this.actJudgeString.Start(nPlayer, eJudgeResult != ENoteJudge.Bad ? ENoteJudge.ADLIB : ENoteJudge.Bad);
                                eJudgeResult = ENoteJudge.Perfect; // Prevent ADLIB notes breaking DFC runs
                                TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.Start(0x11, eJudgeResult, true, nPlayer);
                                TJAPlayer3.stage演奏ドラム画面.actChipFireD.Start(0x11, eJudgeResult, nPlayer);
                                this.CChartScore[nPlayer].nADLIB++;
                                this.CSectionScore[nPlayer].nADLIB++;
                                this.CBranchScore[nPlayer].nADLIB++;
                                if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                                    this.nADLIB[actDan.NowShowingNumber]++;
                            }
                            break;
                        }
                        else if (NotesManager.IsMine(pChip))
                        {
                            if (eJudgeResult != ENoteJudge.Auto && eJudgeResult != ENoteJudge.Miss)
                            {
                                this.actJudgeString.Start(nPlayer, eJudgeResult != ENoteJudge.Bad ? ENoteJudge.Mine : ENoteJudge.Bad);
                                bBombHit = true;
                                eJudgeResult = ENoteJudge.Bad;
                                TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.Start(0x11, eJudgeResult, true, nPlayer);
                                TJAPlayer3.stage演奏ドラム画面.actChipFireD.Start(0x11, ENoteJudge.Mine, nPlayer);
                                TJAPlayer3.Skin.soundBomb?.tPlay();
                                actGauge.MineDamage(nPlayer);
                                this.CChartScore[nPlayer].nMine++;
                                this.CSectionScore[nPlayer].nMine++;
                                this.CBranchScore[nPlayer].nMine++;
                                if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                                    this.nMine[actDan.NowShowingNumber]++;
                            }
                            break;
                        }
                        else
                        {
                            if (eJudgeResult != ENoteJudge.Miss)
                            {
                                pChip.bShow = false;
                            }
                        }

                        if (eJudgeResult != ENoteJudge.Auto && eJudgeResult != ENoteJudge.Miss)
                        {

                            this.actJudgeString.Start(nPlayer, (bAutoPlay && !TJAPlayer3.ConfigIni.bAIBattleMode) ? ENoteJudge.Auto : eJudgeResult);
                            TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.Start(pChip.nチャンネル番号, eJudgeResult, true, nPlayer);
                            TJAPlayer3.stage演奏ドラム画面.actChipFireD.Start(pChip.nチャンネル番号, eJudgeResult, nPlayer);
                        }

                    }
					break;
			}

            if ((pChip.e楽器パート != EInstrumentPad.UNKNOWN))
            {
                if (NotesManager.IsMissableNote(pChip))
                {
                    actGauge.Damage(screenmode, pChip.e楽器パート, eJudgeResult, nPlayer);
                }
            }




            

            var chara = TJAPlayer3.Tx.Characters[TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(nPlayer)].data.Character];
            bool cleared = HGaugeMethods.UNSAFE_FastNormaCheck(nPlayer);

            if (eJudgeResult != ENoteJudge.Poor && eJudgeResult != ENoteJudge.Miss)
            {
                double dbUnit = (((60.0 / (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[nPlayer]))));

                // ランナー(たたけたやつ)
                this.actRunner.Start(nPlayer, false, pChip);

                int Character = this.actChara.iCurrentCharacter[nPlayer];

                if (HGaugeMethods.UNSAFE_IsRainbow(nPlayer) && this.bIsAlreadyMaxed[nPlayer] == false)
                {
                    if(TJAPlayer3.Skin.Characters_Become_Maxed_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded)
                    {
                        this.actChara.ChangeAnime(nPlayer, CAct演奏Drumsキャラクター.Anime.Become_Maxed, true);
                    }
                    this.bIsAlreadyMaxed[nPlayer] = true;
                }
                if (cleared && this.bIsAlreadyCleared[nPlayer] == false)
                {
                    if(TJAPlayer3.Skin.Characters_Become_Cleared_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded)
                    {
                        this.actChara.ChangeAnime(nPlayer, CAct演奏Drumsキャラクター.Anime.Become_Cleared, true);
                    }
                    this.bIsAlreadyCleared[nPlayer] = true;
                    TJAPlayer3.stage演奏ドラム画面.actBackground.ClearIn(nPlayer);
                }
            }

			if ( eJudgeResult == ENoteJudge.Poor || eJudgeResult == ENoteJudge.Miss || eJudgeResult == ENoteJudge.Bad )
            {
                int Character = this.actChara.iCurrentCharacter[nPlayer];

                // ランナー(みすったやつ)
                this.actRunner.Start(nPlayer, true, pChip);
                if (!HGaugeMethods.UNSAFE_IsRainbow(nPlayer) && this.bIsAlreadyMaxed[nPlayer] == true)
                {
                    this.bIsAlreadyMaxed[nPlayer] = false;
                    if(TJAPlayer3.Skin.Characters_SoulOut_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded)
                    {
                        this.actChara.ChangeAnime(nPlayer, CAct演奏Drumsキャラクター.Anime.SoulOut, true);
                    }
                }
                else if (!bIsGOGOTIME[nPlayer])
                {
                    if (Chara_MissCount[nPlayer] == 1 - 1)
                    {
                        if(TJAPlayer3.Skin.Characters_MissIn_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded)
                        {
                            this.actChara.ChangeAnime(nPlayer, CAct演奏Drumsキャラクター.Anime.MissIn, true);
                        }
                    }
                    else if (Chara_MissCount[nPlayer] == 6 - 1)
                    {
                        if(TJAPlayer3.Skin.Characters_MissDownIn_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded)
                        {
                            this.actChara.ChangeAnime(nPlayer, CAct演奏Drumsキャラクター.Anime.MissDownIn, true);
                        }
                    }
                }
                if (!cleared && this.bIsAlreadyCleared[nPlayer] == true)
                {
                    this.bIsAlreadyCleared[nPlayer] = false;
                    if (TJAPlayer3.Skin.Characters_ClearOut_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded)
                    {
                        this.actChara.ChangeAnime(nPlayer, CAct演奏Drumsキャラクター.Anime.ClearOut, true);
                    }
                    TJAPlayer3.stage演奏ドラム画面.actBackground.ClearOut(nPlayer);

                    switch (chara.effect.tGetGaugeType())
                    {
                        case "Hard":
                        case "Extreme":
                            {
                                ifp[nPlayer] = true;
                                isDeniedPlaying[nPlayer] = true; // Prevents the player to ever be able to hit the drum, without freezing the whole game

                                bool allDeniedPlaying = true;
                                for (int p = 0; p < TJAPlayer3.ConfigIni.nPlayerCount; p++)
                                {
                                    if (!isDeniedPlaying[p])
                                    {
                                        allDeniedPlaying = false;
                                        break;
                                    }
                                }
                                if (allDeniedPlaying) TJAPlayer3.DTX.t全チップの再生停止(); // Stop playing song

                                // Stop timer : Pauses the whole game (to remove once is denied playing will work)
                                //CSound管理.rc演奏用タイマ.t一時停止();
                            }
                            break;
                    }
                }
                cInvisibleChip.ShowChipTemporally( pChip.e楽器パート );
			}

            void returnChara()
            {
                int Character = this.actChara.iCurrentCharacter[nPlayer];

                double dbUnit = (((60.0 / (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[nPlayer]))));
                dbUnit = (((60.0 / pChip.dbBPM)));

                if (TJAPlayer3.Skin.Characters_Return_Ptn[Character] != 0 && !bIsGOGOTIME[nPlayer] && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded)
                {
                    {
                        // 魂ゲージMAXではない
                        // ジャンプ_ノーマル
                        this.actChara.ChangeAnime(nPlayer, CAct演奏Drumsキャラクター.Anime.Return, true);
                        //this.actChara.キャラクター_アクション_10コンボ();
                    }
                }
            }

            
			switch ( pChip.e楽器パート )
			{
				case EInstrumentPad.DRUMS:
				case EInstrumentPad.GUITAR:
				case EInstrumentPad.BASS:
					break;
                case EInstrumentPad.TAIKO:
                    if( !bAutoPlay )
                    {
                        if(NotesManager.IsGenericRoll(pChip))
                            break;

					    switch ( eJudgeResult )
					    {
                            case ENoteJudge.Perfect:
                                {
                                    if (NotesManager.IsADLIB(pChip))
                                        break;

                                    this.CBranchScore[nPlayer].nGreat++;
                                    this.CChartScore[nPlayer].nGreat++;
                                    this.CSectionScore[nPlayer].nGreat++;
                                    this.Chara_MissCount[nPlayer] = 0;

                                    if ( nPlayer == 0 ) this.nヒット数_Auto含まない.Drums.Perfect++;
                                    this.actCombo.n現在のコンボ数[nPlayer]++;

                                    if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                                    {
                                        this.n良[actDan.NowShowingNumber]++;
                                        this.tIncreaseComboDan(actDan.NowShowingNumber);
                                    }
                                        

                                    if (this.actCombo.ctコンボ加算[nPlayer].IsUnEnded)
                                    {
                                        this.actCombo.ctコンボ加算[nPlayer].CurrentValue = 1;
                                    }
                                    else
                                    {
                                        this.actCombo.ctコンボ加算[nPlayer].CurrentValue = 0;
                                    }


                                    AIRegisterInput(nPlayer, 1);

                                    TJAPlayer3.stage演奏ドラム画面.actMtaiko.BackSymbolEvent(nPlayer);

                                    
                                    if (this.bIsMiss[nPlayer])
                                    {
                                        returnChara();
                                    }

                                    this.bIsMiss[nPlayer] = false;
                                }
                                break;
                            case ENoteJudge.Great:
                            case ENoteJudge.Good:
                                {
                                    this.CBranchScore[nPlayer].nGood++;
                                    this.CChartScore[nPlayer].nGood++;
                                    this.CSectionScore[nPlayer].nGood++;
                                    this.Chara_MissCount[nPlayer] = 0;

                                    if ( nPlayer == 0 ) this.nヒット数_Auto含まない.Drums.Great++;
                                    this.actCombo.n現在のコンボ数[ nPlayer ]++;

                                    if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                                    {
                                        this.n可[actDan.NowShowingNumber]++;
                                        this.tIncreaseComboDan(actDan.NowShowingNumber);
                                    }

                                    if (this.actCombo.ctコンボ加算[nPlayer].IsUnEnded)
                                    {
                                        this.actCombo.ctコンボ加算[nPlayer].CurrentValue = 1;
                                    }
                                    else
                                    {
                                        this.actCombo.ctコンボ加算[nPlayer].CurrentValue = 0;
                                    }


                                    AIRegisterInput(nPlayer, 0.5f);

                                    TJAPlayer3.stage演奏ドラム画面.actMtaiko.BackSymbolEvent(nPlayer);

                                    if (this.bIsMiss[nPlayer])
                                    {
                                        returnChara();
                                    }

                                    this.bIsMiss[nPlayer] = false;
                                }
                                break;
                            case ENoteJudge.Poor:
		    				case ENoteJudge.Miss:
			    			case ENoteJudge.Bad:
                                {
                                    if(!NotesManager.IsMissableNote(pChip) && !bBombHit)
                                        break;

                                    if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)
                                        CFloorManagement.damage();

                                    if (!bBombHit)
                                    {
                                        if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                                            this.n不可[actDan.NowShowingNumber]++;

                                        this.CBranchScore[nPlayer].nMiss++;
                                        this.CChartScore[nPlayer].nMiss++;
                                        this.CSectionScore[nPlayer].nMiss++;
                                        this.Chara_MissCount[nPlayer]++;

                                        if (nPlayer == 0) this.nヒット数_Auto含まない.Drums.Miss++;
                                    }
                                    
                                    this.actCombo.n現在のコンボ数[ nPlayer ] = 0;
                                    if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                                        this.nCombo[actDan.NowShowingNumber] = 0;
                                    this.actComboVoice.tReset(nPlayer);

                                    AIRegisterInput(nPlayer, 0f);

                                    this.bIsMiss[nPlayer] = true;
                                }
			    				break;
				    		default:
					    		this.nヒット数_Auto含む.Drums[ (int) eJudgeResult ]++;
		    					break;
			    		}
                    }
					else if ( bAutoPlay )
					{
						switch ( eJudgeResult )
						{
                            case ENoteJudge.Perfect:
                                {
                                    if(!NotesManager.IsGenericRoll(pChip))
                                    {
                                        if (NotesManager.IsADLIB(pChip))
                                            break;

                                        if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                                        {
                                            this.n良[actDan.NowShowingNumber]++;
                                            this.tIncreaseComboDan(actDan.NowShowingNumber);
                                        }

                                        this.CBranchScore[nPlayer].nGreat++;
                                        this.CChartScore[nPlayer].nGreat++;
                                        this.CSectionScore[nPlayer].nGreat++;
                                        this.Chara_MissCount[nPlayer] = 0;

                                        if ( nPlayer == 0 ) this.nヒット数_Auto含む.Drums.Perfect++;
                                        this.actCombo.n現在のコンボ数[ nPlayer ]++;
                                        //this.actCombo.ctコンボ加算.t進行();
                                        if (this.actCombo.ctコンボ加算[nPlayer].IsUnEnded)
                                        {
                                            this.actCombo.ctコンボ加算[nPlayer].CurrentValue = 1;
                                        }
                                        else
                                        {
                                            this.actCombo.ctコンボ加算[nPlayer].CurrentValue = 0;
                                        }

                                        AIRegisterInput(nPlayer, 1);

                                        TJAPlayer3.stage演奏ドラム画面.actMtaiko.BackSymbolEvent(nPlayer);

                                        if (this.bIsMiss[nPlayer])
                                        {
                                            returnChara();
                                        }

                                        this.bIsMiss[nPlayer] = false;
                                    }
                                }
                                break;

                            case ENoteJudge.Great:
                            case ENoteJudge.Good:
                                {
                                    if (!NotesManager.IsGenericRoll(pChip))
                                    {
                                        if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                                        { 
                                            this.n可[actDan.NowShowingNumber]++;
                                            this.tIncreaseComboDan(actDan.NowShowingNumber);
                                        }

                                        this.CBranchScore[nPlayer].nGood++;
                                        this.CChartScore[nPlayer].nGood++;
                                        this.CSectionScore[nPlayer].nGood++;
                                        this.Chara_MissCount[nPlayer] = 0;

                                        if (nPlayer == 0) this.nヒット数_Auto含む.Drums.Great++;
                                        this.actCombo.n現在のコンボ数[nPlayer]++;

                                        if (this.actCombo.ctコンボ加算[nPlayer].IsUnEnded)
                                        {
                                            this.actCombo.ctコンボ加算[nPlayer].CurrentValue = 1;
                                        }
                                        else
                                        {
                                            this.actCombo.ctコンボ加算[nPlayer].CurrentValue = 0;
                                        }


                                        AIRegisterInput(nPlayer, 0.5f);

                                        TJAPlayer3.stage演奏ドラム画面.actMtaiko.BackSymbolEvent(nPlayer);

                                        if (this.bIsMiss[nPlayer])
                                        {
                                            returnChara();
                                        }

                                        this.bIsMiss[nPlayer] = false;
                                    }
                                }
                                break;

                            default:
                                {
                                    if(!NotesManager.IsGenericRoll(pChip))
                                    {
                                        if (!NotesManager.IsMissableNote(pChip) && !bBombHit)
                                            break;

                                        if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)
                                            CFloorManagement.damage();

                                        if (!bBombHit)
                                        {
                                            if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                                                this.n不可[actDan.NowShowingNumber]++;
                                                

                                            this.CBranchScore[nPlayer].nMiss++;
                                            this.CChartScore[nPlayer].nMiss++;
                                            this.CSectionScore[nPlayer].nMiss++;
                                            this.Chara_MissCount[nPlayer]++;
                                        }

                                        this.actCombo.n現在のコンボ数[ nPlayer ] = 0;
                                        if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                                            this.nCombo[actDan.NowShowingNumber] = 0;
                                        this.actComboVoice.tReset(nPlayer);

                                        AIRegisterInput(nPlayer, 0f);


                                        this.bIsMiss[nPlayer] = true;
                                    }
                                }
								break;
						}
					}
                    actDan.Update();
                
                    #region[ Combo voice ]

                    if(!NotesManager.IsGenericRoll(pChip))
                    {
                        if((this.actCombo.n現在のコンボ数[ nPlayer ] % 100 == 0 || this.actCombo.n現在のコンボ数[nPlayer] == 50) && this.actCombo.n現在のコンボ数[ nPlayer ] > 0 )
                        {
                            this.actComboBalloon.Start( this.actCombo.n現在のコンボ数[ nPlayer ], nPlayer );
                        }

                        // Combo voice here
                        this.actComboVoice.t再生( this.actCombo.n現在のコンボ数[ nPlayer ], nPlayer );

                        double dbUnit = (((60.0 / (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[nPlayer]))));
                        dbUnit = (((60.0 / pChip.dbBPM)));

                        //CDTXMania.act文字コンソール.tPrint(620, 80, C文字コンソール.Eフォント種別.白, "BPM: " + dbUnit.ToString());

                        for (int i = 0; i < 5; i++)
                        {
                            if (this.actCombo.n現在のコンボ数[i] == 50 || this.actCombo.n現在のコンボ数[i] == 300)
                            {
                                ctChipAnimeLag[i] = new CCounter(0, 664, 1, TJAPlayer3.Timer);
                            }
                        }

                        if (this.actCombo.n現在のコンボ数[nPlayer] % 10 == 0 && this.actCombo.n現在のコンボ数[nPlayer] > 0)
                        {
                            //if (this.actChara.bキャラクターアクション中 == false)
                            //{
                            int Character = this.actChara.iCurrentCharacter[nPlayer];
                                // Edit character values here
                                if (!pChip.bGOGOTIME) //2018.03.11 kairera0467 チップに埋め込んだフラグから読み取る
                                {
                                    if (TJAPlayer3.Skin.Characters_10Combo_Ptn[Character] != 0 && this.actChara.eNowAnime[nPlayer] != CAct演奏Drumsキャラクター.Anime.Combo10 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded)
                                    {
                                        if (!HGaugeMethods.UNSAFE_IsRainbow(nPlayer))
                                        {
                                            // 魂ゲージMAXではない
                                            // ジャンプ_ノーマル
                                        this.actChara.ChangeAnime(nPlayer, CAct演奏Drumsキャラクター.Anime.Combo10, true);
                                        }
                                    }
                                    if (TJAPlayer3.Skin.Characters_10Combo_Maxed_Ptn[Character] != 0 && this.actChara.eNowAnime[nPlayer] != CAct演奏Drumsキャラクター.Anime.Combo10_Max && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded)
                                    {
                                        if (HGaugeMethods.UNSAFE_IsRainbow(nPlayer))
                                        {
                                            // 魂ゲージMAX
                                            // ジャンプ_MAX
                                        this.actChara.ChangeAnime(nPlayer, CAct演奏Drumsキャラクター.Anime.Combo10_Max, true);
                                        }
                                    }
                                }


                        }
                        
                        this.t紙吹雪_開始();
                    }
                    #endregion


					break;


				default:
					break;
			}
			if ( ( ( pChip.e楽器パート != EInstrumentPad.UNKNOWN ) ) && ( eJudgeResult != ENoteJudge.Miss ) && ( eJudgeResult != ENoteJudge.Bad ) && ( eJudgeResult != ENoteJudge.Poor ) && (NotesManager.IsMissableNote(pChip)) )
			{
                int nCombos = this.actCombo.n現在のコンボ数[nPlayer];
                long nInit = TJAPlayer3.DTX.nScoreInit[0, TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[nPlayer]];
                long nDiff = TJAPlayer3.DTX.nScoreDiff[TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[nPlayer]];
                long nAddScore = 0;

                if ( TJAPlayer3.ConfigIni.ShinuchiMode )  //2016.07.04 kairera0467 真打モード。
                {
                    nAddScore = (long)nAddScoreNiji[nPlayer];

                    if (eJudgeResult == ENoteJudge.Great || eJudgeResult == ENoteJudge.Good)
                    {
                        nAddScore = (long)nAddScoreNiji[nPlayer] / 20;
                        nAddScore = (long)nAddScore * 10;
                    }

                    this.actScore.Add( EInstrumentPad.TAIKO, bIsAutoPlay, (long)nAddScore, nPlayer );
                }
                else if( TJAPlayer3.DTX.nScoreModeTmp == 2 )
                {
                    if( nCombos < 10 )
                    {
                        nAddScore = this.nScore[ 0 ];
                    }
                    else if( nCombos >= 10 && nCombos <= 29 )
                    {
                        nAddScore = this.nScore[ 1 ];
                    }
                    else if( nCombos >= 30 && nCombos <= 49 )
                    {
                        nAddScore = this.nScore[ 2 ];
                    }
                    else if( nCombos >= 50 && nCombos <= 99 )
                    {
                        nAddScore = this.nScore[ 3 ];
                    }
                    else if (nCombos >= 100)
                    {
                        nAddScore = this.nScore[ 4 ];
                    }

                    if (eJudgeResult == ENoteJudge.Great || eJudgeResult == ENoteJudge.Good)
                    {
                        nAddScore = nAddScore / 2;
                    }

                    if (pChip.bGOGOTIME) //2018.03.11 kairera0467 チップに埋め込んだフラグから読み取る
                    {
                        nAddScore = (int)(nAddScore * 1.2f);
                    }

                    //100コンボ毎のボーナス
                    if (nCombos % 100 == 0 && nCombos > 99)
                    {
                        if (this.actScore.ctボーナス加算タイマ[nPlayer].IsTicked)
                        {
                            this.actScore.ctボーナス加算タイマ[nPlayer].Stop();
                            this.actScore.BonusAdd(nPlayer);
                        }
                        this.actScore.ctボーナス加算タイマ[nPlayer].CurrentValue = 0;
                        this.actScore.ctボーナス加算タイマ[nPlayer] = new CCounter(0, 2, 1000, TJAPlayer3.Timer);
                    }

                    nAddScore = (int)(nAddScore / 10);
                    nAddScore = (int)(nAddScore * 10);

                    //大音符のボーナス
                    if (pChip.nチャンネル番号 == 0x13 || pChip.nチャンネル番号 == 0x14 || pChip.nチャンネル番号 == 0x1A || pChip.nチャンネル番号 == 0x1B)
                    {
                        nAddScore = nAddScore * 2;
                    }

                    this.actScore.Add(EInstrumentPad.TAIKO, bIsAutoPlay, nAddScore, nPlayer);
                    //this.actScore.Add( E楽器パート.DRUMS, bIsAutoPlay, nAddScore );
                }
                else if (TJAPlayer3.DTX.nScoreModeTmp == 1)
                {
                    if (nCombos < 10)
                    {
                        nAddScore = this.nScore[0];
                    }
                    else if (nCombos >= 10 && nCombos <= 19)
                    {
                        nAddScore = this.nScore[1];
                    }
                    else if (nCombos >= 20 && nCombos <= 29)
                    {
                        nAddScore = this.nScore[2];
                    }
                    else if (nCombos >= 30 && nCombos <= 39)
                    {
                        nAddScore = this.nScore[3];
                    }
                    else if (nCombos >= 40 && nCombos <= 49)
                    {
                        nAddScore = this.nScore[4];
                    }
                    else if (nCombos >= 50 && nCombos <= 59)
                    {
                        nAddScore = this.nScore[5];
                    }
                    else if (nCombos >= 60 && nCombos <= 69)
                    {
                        nAddScore = this.nScore[6];
                    }
                    else if (nCombos >= 70 && nCombos <= 79)
                    {
                        nAddScore = this.nScore[7];
                    }
                    else if (nCombos >= 80 && nCombos <= 89)
                    {
                        nAddScore = this.nScore[8];
                    }
                    else if (nCombos >= 90 && nCombos <= 99)
                    {
                        nAddScore = this.nScore[9];
                    }
                    else if (nCombos >= 100)
                    {
                        nAddScore = this.nScore[10];
                    }

                    if (eJudgeResult == ENoteJudge.Great || eJudgeResult == ENoteJudge.Good)
                    {
                        nAddScore = nAddScore / 2;
                    }

                    if (pChip.bGOGOTIME) //2018.03.11 kairera0467 チップに埋め込んだフラグから読み取る
                        nAddScore = (int)(nAddScore * 1.2f);

                    nAddScore = (int)(nAddScore / 10.0);
                    nAddScore = (int)(nAddScore * 10);

                    //大音符のボーナス
                    if (pChip.nチャンネル番号 == 0x13 || pChip.nチャンネル番号 == 0x14 || pChip.nチャンネル番号 == 0x1A || pChip.nチャンネル番号 == 0x1B)
                    {
                        nAddScore = nAddScore * 2;
                    }

                    this.actScore.Add(EInstrumentPad.TAIKO, bIsAutoPlay, nAddScore, nPlayer);
                }
                else
                {
                    if (eJudgeResult == ENoteJudge.Perfect)
                    {
                        if (nCombos < 200)
                        {
                            nAddScore = 1000;
                        }
                        else
                        {
                            nAddScore = 2000;
                        }
                    }
                    else if (eJudgeResult == ENoteJudge.Great || eJudgeResult == ENoteJudge.Good)
                    {
                        if (nCombos < 200)
                        {
                            nAddScore = 500;
                        }
                        else
                        {
                            nAddScore = 1000;
                        }
                    }

                    if (pChip.bGOGOTIME) //2018.03.11 kairera0467 チップに埋め込んだフラグから読み取る
                        nAddScore = (int)(nAddScore * 1.2f);


                    //大音符のボーナス
                    if (pChip.nチャンネル番号 == 0x13 || pChip.nチャンネル番号 == 0x25)
                    {
                        nAddScore = nAddScore * 2;
                    }

                    this.actScore.Add(EInstrumentPad.TAIKO, bIsAutoPlay, nAddScore, nPlayer);
                    //this.actScore.Add( E楽器パート.DRUMS, bIsAutoPlay, nAddScore );              
                }

                //キーを押したときにスコア情報 + nAddScoreを置き換える様に
                int __score = (int)(this.actScore.GetScore(nPlayer) + nAddScore);
                this.CBranchScore[nPlayer].nScore = __score;
                this.CChartScore[nPlayer].nScore = __score;
                this.CSectionScore[nPlayer].nScore = __score;
            }


            return ENoteJudge.Auto;
        }

        protected abstract void tチップのヒット処理_BadならびにTight時のMiss(CDTX.ECourse eCourse, EInstrumentPad part);
        protected abstract void tチップのヒット処理_BadならびにTight時のMiss(CDTX.ECourse eCourse, EInstrumentPad part, int nLane);

        protected void tチップのヒット処理_BadならびにTight時のMiss(CDTX.ECourse eCourse, EInstrumentPad part, int nLane, EInstrumentPad screenmode)
        {
			cInvisibleChip.StartSemiInvisible( part );
			cInvisibleChip.ShowChipTemporally( part );

            //ChipのCourseをベースにゲージの伸びを調節
            actGauge.Damage(screenmode, part, ENoteJudge.Miss, 0);
            switch ( part )
			{
				case EInstrumentPad.DRUMS:
				case EInstrumentPad.GUITAR:
				case EInstrumentPad.BASS:
					break;

				case EInstrumentPad.TAIKO:
					this.actCombo.n現在のコンボ数.P1 = 0;
                    this.actCombo.n現在のコンボ数.P2 = 0;
                    this.actCombo.n現在のコンボ数.P3 = 0;
                    this.actCombo.n現在のコンボ数.P4 = 0;
                    this.actCombo.n現在のコンボ数.P5 = 0;
                    break;

				default:
					return;
			}
		}

		protected CDTX.CChip r指定時刻に一番近い未ヒットChipを過去方向優先で検索する( long nTime, int nPlayer )
		{
			//sw2.Start();

			int nIndex_InitialPositionSearchingToPast;
			int nTimeDiff;
			int count = listChip[ nPlayer ].Count;
			if ( count <= 0 )			// 演奏データとして1個もチップがない場合は
			{
				//sw2.Stop();
				return null;
			}

			int nIndex_NearestChip_Future = nIndex_InitialPositionSearchingToPast = this.n現在のトップChip;
			if ( this.n現在のトップChip >= count )		// その時点で演奏すべきチップが既に全部無くなっていたら
			{
				nIndex_NearestChip_Future  = nIndex_InitialPositionSearchingToPast = count - 1;
			}


			// int nIndex_NearestChip_Future = nIndex_InitialPositionSearchingToFuture;
//			while ( nIndex_NearestChip_Future < count )	// 未来方向への検索
			for ( ; nIndex_NearestChip_Future < count; nIndex_NearestChip_Future++ )
			{
                
                if( nIndex_NearestChip_Future < 0 )
                    continue;
                

				CDTX.CChip chip = listChip[ nPlayer ][ nIndex_NearestChip_Future ];
				if ( !chip.bHit && chip.b可視 )
				{
					if (NotesManager.IsHittableNote(chip))
					{
						if ( chip.n発声時刻ms > nTime )
						{
							break;
						}
                        nIndex_InitialPositionSearchingToPast = nIndex_NearestChip_Future;
					}
				}
                if( chip.bHit && chip.b可視 ) // 2015.11.5 kairera0467 連打対策
                {
                    if (NotesManager.IsGenericRoll(chip) && !NotesManager.IsRollEnd(chip))
                    {
                        if (chip.nノーツ終了時刻ms > nTime)
                        {
                            nIndex_InitialPositionSearchingToPast = nIndex_NearestChip_Future;
                            break;
                        }
                    }
                }
//				nIndex_NearestChip_Future++;
			}


			int nIndex_NearestChip_Past = nIndex_InitialPositionSearchingToPast;
//			while ( nIndex_NearestChip_Past >= 0 )		// 過去方向への検索
			for ( ; nIndex_NearestChip_Past >= 0; nIndex_NearestChip_Past-- )
			{
				CDTX.CChip chip = listChip[ nPlayer ][ nIndex_NearestChip_Past ];
                //if ( (!chip.bHit && chip.b可視 ) && ( (  0x93 <= chip.nチャンネル番号 ) && ( chip.nチャンネル番号 <= 0x99 ) ) )

                if ( (!chip.bHit && chip.b可視 ) && NotesManager.IsHittableNote(chip) && !NotesManager.IsRollEnd(chip) )
                    {
						break;
					}
                //2015.11.5 kairera0467 連打対策
				else if ( ( chip.b可視 ) && NotesManager.IsGenericRoll(chip) && !NotesManager.IsRollEnd(chip)) 
					{
						break;
					}
                
                //				nIndex_NearestChip_Past--;
            }
			if ( ( nIndex_NearestChip_Future >= count ) && ( nIndex_NearestChip_Past < 0 ) )	// 検索対象が過去未来どちらにも見つからなかった場合
			{
				//sw2.Stop();
				return null;
			}
			CDTX.CChip nearestChip;	// = null;	// 以下のifブロックのいずれかで必ずnearestChipには非nullが代入されるので、null初期化を削除
			if ( nIndex_NearestChip_Future >= count )											// 検索対象が未来方向には見つからなかった(しかし過去方向には見つかった)場合
			{
                nearestChip = listChip[ nPlayer ][ nIndex_NearestChip_Past ];
//				nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
			}
			else if ( nIndex_NearestChip_Past < 0 )												// 検索対象が過去方向には見つからなかった(しかし未来方向には見つかった)場合
			{
                nearestChip = listChip[ nPlayer ][ nIndex_NearestChip_Future ];
//				nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
			}
			else
			{
				int nTimeDiff_Future = Math.Abs( (int) ( nTime - listChip[ nPlayer ][ nIndex_NearestChip_Future ].n発声時刻ms ) );
				int nTimeDiff_Past   = Math.Abs( (int) ( nTime - listChip[ nPlayer ][ nIndex_NearestChip_Past   ].n発声時刻ms ) );

                if ( nTimeDiff_Future < nTimeDiff_Past )
				{
                    if ( !listChip[ nPlayer ][ nIndex_NearestChip_Past ].bHit 
                        && listChip[ nPlayer ][ nIndex_NearestChip_Past ].n発声時刻ms + 108 >= nTime
                        && NotesManager.IsMissableNote(listChip[nPlayer][nIndex_NearestChip_Past])
                        )
                    {
					    nearestChip = listChip[ nPlayer ][ nIndex_NearestChip_Past ];
                    }
                    else
					    nearestChip = listChip[ nPlayer ][ nIndex_NearestChip_Future ];
                    
//					nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
				}
				else
				{
					nearestChip = listChip[ nPlayer ][ nIndex_NearestChip_Past ];
//					nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
				}

                var __tmpchp = listChip[nPlayer][nIndex_NearestChip_Future];

                //2015.11.5 kairera0467　連打音符の判定
                if (NotesManager.IsGenericRoll(__tmpchp) && !NotesManager.IsRollEnd(__tmpchp))
                {
                    if( listChip[ nPlayer ][ nIndex_NearestChip_Future ].n発声時刻ms <= nTime && listChip[ nPlayer ][ nIndex_NearestChip_Future ].nノーツ終了時刻ms >= nTime )
                    {
                        nearestChip = listChip[ nPlayer ][ nIndex_NearestChip_Future ];
                    }
                }
			}
			nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
            int n検索範囲時間ms = 0;
			if ( ( n検索範囲時間ms > 0 ) && ( nTimeDiff > n検索範囲時間ms ) )					// チップは見つかったが、検索範囲時間外だった場合
			{
				//sw2.Stop();
				return null;
			}
			//sw2.Stop();
			return nearestChip;
		}

        /// <summary>
        /// 最も判定枠に近いドンカツを返します。
        /// </summary>
        /// <param name="nowTime">判定時の時間。</param>
        /// <param name="player">プレイヤー。</param>
        /// <param name="don">ドンかどうか。</param>
        /// <returns>最も判定枠に近いノーツ。</returns>
        /*
        protected CDTX.CChip GetChipOfNearest(long nowTime, int player, bool don)
        {
            var nearestChip = new CDTX.CChip();
            var count = listChip[player].Count;
            var chips = listChip[player];
            var startPosision = NowProcessingChip[player];
            CDTX.CChip pastChip; // 判定されるべき過去ノート
            CDTX.CChip futureChip; // 判定されるべき未来ノート
            var pastJudge = E判定.Miss;
            var futureJudge = E判定.Miss;

            bool GetDon(CDTX.CChip note)
            {
                return note.nチャンネル番号 == 0x11 || note.nチャンネル番号 == 0x13 || note.nチャンネル番号 == 0x1A || note.nチャンネル番号 == 0x1F;
            }
            bool GetKatsu(CDTX.CChip note)
            {
                return note.nチャンネル番号 == 0x12 || note.nチャンネル番号 == 0x14 || note.nチャンネル番号 == 0x1B || note.nチャンネル番号 == 0x1F;
            }

            if (count <= 0)
            {
                return null;
            }

            if (startPosision >= count)
            {
                startPosision -= 1;
            }

            #region 過去のノーツで、かつ可判定以上のノーツの決定
            CDTX.CChip afterChip = null;
            for (int pastNote = startPosision - 1; ; pastNote--)
            {
                if (pastNote < 0)
                {
                    pastChip = afterChip != null ? afterChip : null; // afterChipに過去の判定があるかもしれないので
                    break;
                }
                var processingChip = chips[pastNote];
                if (!processingChip.IsHitted && processingChip.nコース == n現在のコース[player]) // まだ判定されてない音符
                {
                    if (don ? GetDon(processingChip) : GetKatsu(processingChip)) // 音符のチャンネルである
                    {
                        var thisChipJudge = pastJudge = e指定時刻からChipのJUDGEを返すImpl(nowTime, processingChip, player);
                        if (thisChipJudge != E判定.Miss)
                        {
                            // 判定が見過ごし不可ではない(=たたいて不可以上)
                            // その前のノートがもしかしたら存在して、可以上の判定かもしれないからまだ処理を続行する。
                            afterChip = processingChip;
                            continue;
                        }
                        else
                        {
                            // 判定が不可だった
                            // その前のノーツを過去で可以上のノート(つまり判定されるべきノート)とする。
                            pastChip = afterChip;
                            break; // 検索終わり
                        }
                    }
                }
                if (processingChip.IsHitted && processingChip.nコース == n現在のコース[player]) // 連打
                {
                    if ((0x15 <= processingChip.nチャンネル番号) && (processingChip.nチャンネル番号 <= 0x17))
                    {
                        if (processingChip.nノーツ終了時刻ms > nowTime)
                        {
                            pastChip = processingChip;
                            break;
                        }
                    }
                }
            }
            #endregion

            #region 未来のノーツで、かつ可判定以上のノーツの決定
            for (int futureNote = startPosision; ; futureNote++)
            {
                if (futureNote >= count)
                {
                    futureChip = null;
                    break;
                }
                var processingChip = chips[futureNote];
                if (!processingChip.IsHitted && processingChip.nコース == n現在のコース[player]) // まだ判定されてない音符
                {
                    if (don ? GetDon(processingChip) : GetKatsu(processingChip)) // 音符のチャンネルである
                    {
                        var thisChipJudge = futureJudge = e指定時刻からChipのJUDGEを返すImpl(nowTime, processingChip, player);
                        if (thisChipJudge != E判定.Miss)
                        {
                            // 判定が見過ごし不可ではない(=たたいて不可以上)
                            // そのノートを処理すべきなので、検索終わり。
                            futureChip = processingChip;
                            break; // 検索終わり
                        }
                        else
                        {
                            // 判定が不可だった
                            // つまり未来に処理すべきノートはないので、検索終わり。
                            futureChip = null; // 今処理中のノート
                            break; // 検索終わり
                        }
                    }
                }
            }
            #endregion

            #region 過去のノーツが見つかったらそれを返却、そうでなければ未来のノーツを返却
            if ((pastJudge == E判定.Miss || pastJudge == E判定.Poor) && (futureJudge != E判定.Miss && futureJudge != E判定.Poor))
            {
                // 過去の判定が不可で、未来の判定が可以上なら未来を返却。
                nearestChip = futureChip;
            }
            else if (futureChip == null && pastChip != null)
            {
                // 未来に処理するべきノートがなかったので、過去の処理すべきノートを返す。
                nearestChip = pastChip;
            }
            else if (pastChip == null && futureChip != null)
            {
                // 過去の検索が該当なしだったので、未来のノートを返す。
                nearestChip = futureChip;
            }
            else
            {
                // 基本的には過去のノートを返す。
                nearestChip = pastChip;
            }
            #endregion

            return nearestChip;
        }
        */


        protected CDTX.CChip r指定時刻に一番近い未ヒットChip( long nTime, int nChannel, int nInputAdjustTime, int n検索範囲時間ms, int nPlayer )
		{
			//sw2.Start();
//Trace.TraceInformation( "nTime={0}, nChannel={1:x2}, 現在のTop={2}", nTime, nChannel,CDTXMania.DTX.listChip[ this.n現在のトップChip ].n発声時刻ms );
			nTime += nInputAdjustTime;

			int nIndex_InitialPositionSearchingToPast;
			int nTimeDiff;
			if ( this.n現在のトップChip == -1 )			// 演奏データとして1個もチップがない場合は
			{
				//sw2.Stop();
				return null;
			}
			int count = listChip[ nPlayer ].Count;
			int nIndex_NearestChip_Future = nIndex_InitialPositionSearchingToPast = this.n現在のトップChip;
			if ( this.n現在のトップChip >= count )		// その時点で演奏すべきチップが既に全部無くなっていたら
			{
				nIndex_NearestChip_Future  = nIndex_InitialPositionSearchingToPast = count - 1;
			}
			// int nIndex_NearestChip_Future = nIndex_InitialPositionSearchingToFuture;
//			while ( nIndex_NearestChip_Future < count )	// 未来方向への検索
			for ( ; nIndex_NearestChip_Future < count; nIndex_NearestChip_Future++ )
			{
				CDTX.CChip chip = listChip[ nPlayer ][ nIndex_NearestChip_Future ];
				if ( !chip.bHit )
				{
                    if( ( 0x11 <= nChannel) && ( nChannel <= 0x1F ) )
                    {
                        if ((chip.nチャンネル番号 == nChannel) || (chip.nチャンネル番号 == (nChannel + 0x20)))
                        {
                            if (chip.n発声時刻ms > nTime)
                            {
                                break;
                            }
                            nIndex_InitialPositionSearchingToPast = nIndex_NearestChip_Future;
                        }
                        continue;
                    }

					//if ( ( ( 0xDE <= nChannel ) && ( nChannel <= 0xDF ) ) )
                    if ( ( ( 0xDF == nChannel ) ) )
					{
                        if( chip.nチャンネル番号 == nChannel )
                        {
						    if ( chip.n発声時刻ms > nTime )
						    {
						    	break;
						    }
						    nIndex_InitialPositionSearchingToPast = nIndex_NearestChip_Future;
                        }
					}

                    if ( ( ( 0x50 == nChannel ) ) )
					{
                        if( chip.nチャンネル番号 == nChannel )
                        {
						    if ( chip.n発声時刻ms > nTime )
						    {
						    	break;
						    }
						    nIndex_InitialPositionSearchingToPast = nIndex_NearestChip_Future;
                        }
					}

				}
//				nIndex_NearestChip_Future++;
			}

            // Channel is always 50, following code is unreachable

			int nIndex_NearestChip_Past = nIndex_InitialPositionSearchingToPast;
//			while ( nIndex_NearestChip_Past >= 0 )		// 過去方向への検索
			for ( ; nIndex_NearestChip_Past >= 0; nIndex_NearestChip_Past-- )
			{
				CDTX.CChip chip = listChip[ nPlayer ][ nIndex_NearestChip_Past ];
				if ( (!chip.bHit) &&
						(
							(
                                ( ( ( ( nChannel >= 0x11 ) && ( nChannel <= 0x14 ) ) || nChannel == 0x1A || nChannel == 0x1B || nChannel == 0x1F ) && ( chip.nチャンネル番号 == nChannel ) )
							)
							||
							(
							//	( ( ( nChannel >= 0xDE ) && ( nChannel <= 0xDF ) ) && ( chip.nチャンネル番号 == nChannel ) )
	                            ( ( ( nChannel == 0xDF ) ) && ( chip.nチャンネル番号 == nChannel ) )
							)
							||
							(
							//	( ( ( nChannel >= 0xDE ) && ( nChannel <= 0xDF ) ) && ( chip.nチャンネル番号 == nChannel ) )
	                            ( ( ( nChannel == 0x50 ) ) && ( chip.nチャンネル番号 == nChannel ) )
							)
						)
					)
					{
						break;
					}
//				nIndex_NearestChip_Past--;
			}
			if ( ( nIndex_NearestChip_Future >= count ) && ( nIndex_NearestChip_Past < 0 ) )	// 検索対象が過去未来どちらにも見つからなかった場合
			{
				//sw2.Stop();
				return null;
			}
			CDTX.CChip nearestChip;	// = null;	// 以下のifブロックのいずれかで必ずnearestChipには非nullが代入されるので、null初期化を削除
			if ( nIndex_NearestChip_Future >= count )											// 検索対象が未来方向には見つからなかった(しかし過去方向には見つかった)場合
			{
				nearestChip = listChip[ nPlayer ][ nIndex_NearestChip_Past ];
//				nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
			}
			else if ( nIndex_NearestChip_Past < 0 )												// 検索対象が過去方向には見つからなかった(しかし未来方向には見つかった)場合
			{
				nearestChip = listChip[ nPlayer ][ nIndex_NearestChip_Future ];
//				nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
			}
			else
			{
				int nTimeDiff_Future = Math.Abs( (int) ( nTime - listChip[ nPlayer ][ nIndex_NearestChip_Future ].n発声時刻ms ) );
				int nTimeDiff_Past   = Math.Abs( (int) ( nTime - listChip[ nPlayer ][ nIndex_NearestChip_Past   ].n発声時刻ms ) );

                if( nChannel == 0xDF ) //0xDFの場合は過去方向への検索をしない
                {
                    return listChip[ nPlayer ][ nIndex_NearestChip_Future ];
                }

				if ( nTimeDiff_Future < nTimeDiff_Past )
				{
					nearestChip = listChip[ nPlayer ][ nIndex_NearestChip_Future ];
//					nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
				}
				else
				{
					nearestChip = listChip[ nPlayer ][ nIndex_NearestChip_Past ];
//					nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
				}
			}
			nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
			if ( ( n検索範囲時間ms > 0 ) && ( nTimeDiff > n検索範囲時間ms ) )					// チップは見つかったが、検索範囲時間外だった場合
			{
				//sw2.Stop();
				return null;
			}
			//sw2.Stop();
			return nearestChip;
		}
		public bool r検索範囲内にチップがあるか調べる( long nTime, int nInputAdjustTime, int n検索範囲時間ms, int nPlayer )
		{
			nTime += nInputAdjustTime;

			for ( int i = 0; i < listChip[ nPlayer ].Count; i++ )
			{
				CDTX.CChip chip = listChip[ nPlayer ][ i ];
				if ( !chip.bHit )
				{
					if (NotesManager.IsMissableNote(chip))
					{
						if ( chip.n発声時刻ms < nTime + n検索範囲時間ms )
						{
                            if( chip.nコース == this.n現在のコース[ nPlayer ] ) //2016.06.14 kairera0467 譜面分岐も考慮するようにしてみる。
						        return true;
						}
					}
				}
			}
			
			return false;
		}

		protected void ChangeInputAdjustTimeInPlaying( IInputDevice keyboard, int plusminus )		// #23580 2011.1.16 yyagi UI for InputAdjustTime in playing screen.
		{
			int offset;
			if (keyboard.KeyPressing((int)SlimDXKeys.Key.LeftControl) ||
				keyboard.KeyPressing((int)SlimDXKeys.Key.RightControl))
			{
				offset = plusminus;
			}
			else
			{
				offset = plusminus * 10;
			}

		    var newInputAdjustTimeMs = (TJAPlayer3.ConfigIni.nInputAdjustTimeMs + offset).Clamp(-99, 99);
			TJAPlayer3.ConfigIni.nInputAdjustTimeMs = newInputAdjustTimeMs;
		}

		protected abstract void t入力処理_ドラム();
		protected abstract void ドラムスクロール速度アップ();
		protected abstract void ドラムスクロール速度ダウン();
		protected void tキー入力()
		{
            // Inputs 

			IInputDevice keyboard = TJAPlayer3.InputManager.Keyboard;

			if ( ( !this.bPAUSE && ( base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED ) ) && ( base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED_FadeOut ) )
			{
				this.t入力処理_ドラム();


                // Individual offset
				if (keyboard.KeyPressed( (int)SlimDXKeys.Key.UpArrow ) && ( keyboard.KeyPressing( (int)SlimDXKeys.Key.RightShift ) || keyboard.KeyPressing( (int)SlimDXKeys.Key.LeftShift ) ) )
				{	// shift (+ctrl) + UpArrow (BGMAdjust)
					TJAPlayer3.DTX.t各自動再生音チップの再生時刻を変更する( ( keyboard.KeyPressing( (int)SlimDXKeys.Key.LeftControl ) || keyboard.KeyPressing( (int)SlimDXKeys.Key.RightControl ) ) ? 1 : 10 );
					TJAPlayer3.DTX.tWave再生位置自動補正();
				}
				else if (keyboard.KeyPressed( (int)SlimDXKeys.Key.DownArrow ) && ( keyboard.KeyPressing( (int)SlimDXKeys.Key.RightShift ) || keyboard.KeyPressing( (int)SlimDXKeys.Key.LeftShift ) ) )
				{	// shift + DownArrow (BGMAdjust)
					TJAPlayer3.DTX.t各自動再生音チップの再生時刻を変更する( ( keyboard.KeyPressing( (int)SlimDXKeys.Key.LeftControl ) || keyboard.KeyPressing( (int)SlimDXKeys.Key.RightControl ) ) ? -1 : -10 );
					TJAPlayer3.DTX.tWave再生位置自動補正();
				}
                // Tokkun only
                else if (TJAPlayer3.ConfigIni.bTokkunMode && 
                    keyboard.KeyPressed( (int)SlimDXKeys.Key.UpArrow ) )
				{	// UpArrow(scrollspeed up)
					ドラムスクロール速度アップ();
				}
				else if (TJAPlayer3.ConfigIni.bTokkunMode && 
                    keyboard.KeyPressed( (int)SlimDXKeys.Key.DownArrow ) )
				{	// DownArrow (scrollspeed down)
					ドラムスクロール速度ダウン();
				}
                // Debug mode
				else if (TJAPlayer3.ConfigIni.KeyAssign.KeyIsPressed(TJAPlayer3.ConfigIni.KeyAssign.System.DisplayDebug) )
				{	// del (debug info)
					TJAPlayer3.ConfigIni.b演奏情報を表示する = !TJAPlayer3.ConfigIni.b演奏情報を表示する;
				}

                
                /*
				else if ( keyboard.bキーが押された( (int)SlimDXKeys.Key.LeftArrow ) )		// #24243 2011.1.16 yyagi UI for InputAdjustTime in playing screen.
				{
					ChangeInputAdjustTimeInPlaying( keyboard, -1 );
				}
				else if ( keyboard.bキーが押された( (int)SlimDXKeys.Key.RightArrow ) )		// #24243 2011.1.16 yyagi UI for InputAdjustTime in playing screen.
				{
					ChangeInputAdjustTimeInPlaying( keyboard, +1 );
				}
                */
                
				else if ( ( base.ePhaseID == CStage.EPhase.Common_NORMAL ) && ( keyboard.KeyPressed( (int)SlimDXKeys.Key.Escape ) || TJAPlayer3.Pad.bPressedGB( EPad.FT ) ) && !this.actPauseMenu.bIsActivePopupMenu )
				{	// escape (exit)
                    if (!this.actPauseMenu.bIsActivePopupMenu && this.bPAUSE == false)
                    {
                        TJAPlayer3.Skin.soundChangeSFX.tPlay();

                        SoundManager.PlayTimer.Pause();
                        TJAPlayer3.Timer.Pause();
                        TJAPlayer3.DTX.t全チップの再生一時停止();
                        this.actAVI.tPauseControl();

                        this.bPAUSE = true;
                        this.actPauseMenu.tActivatePopupMenu(0);
                    }
                    // this.t演奏中止();
				}
                else if ( keyboard.KeyPressed( (int)SlimDXKeys.Key.D1 ) )
                {
                    if (!TJAPlayer3.DTX.bHasBranch[TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0]]) return;

                    //listBRANCHを廃止したため強制分岐の開始値を
                    //rc演奏用タイマ.n現在時刻msから引っ張ることに

                    //判定枠に一番近いチップの情報を元に一小節分の値を計算する. 2020.04.21 akasoko26

                    var p判定枠に最も近いチップ = r指定時刻に一番近い未ヒットChipを過去方向優先で検索する((long)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed), 0);
                    double db一小節後 = 0.0;
                    if (p判定枠に最も近いチップ != null)
                        db一小節後 = ((15000.0 / p判定枠に最も近いチップ.dbBPM * (p判定枠に最も近いチップ.fNow_Measure_s / p判定枠に最も近いチップ.fNow_Measure_m)) * 16.0);

                    this.t分岐処理(CDTX.ECourse.eNormal, 0, (SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed) + db一小節後);

                    TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.t分岐レイヤー_コース変化(TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.stBranch[0].nAfter, CDTX.ECourse.eNormal, 0);
                    TJAPlayer3.stage演奏ドラム画面.actMtaiko.tBranchEvent(TJAPlayer3.stage演奏ドラム画面.actMtaiko.After[0], CDTX.ECourse.eNormal, 0);
                    
                    this.n現在のコース[0] = CDTX.ECourse.eNormal;
                    this.n次回のコース[0] = CDTX.ECourse.eNormal;
                    this.nレーン用表示コース[0] = CDTX.ECourse.eNormal;
                    

                    this.b強制的に分岐させた[0] = true;
                }
                else if ( keyboard.KeyPressed( (int)SlimDXKeys.Key.D2 ) )		// #24243 2011.1.16 yyagi UI for InputAdjustTime in playing screen.
                {
                    if (!TJAPlayer3.DTX.bHasBranch[TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0]]) return;

                    //listBRANCHを廃止したため強制分岐の開始値を
                    //rc演奏用タイマ.n現在時刻msから引っ張ることに

                    //判定枠に一番近いチップの情報を元に一小節分の値を計算する. 2020.04.21 akasoko26
                    var p判定枠に最も近いチップ = r指定時刻に一番近い未ヒットChipを過去方向優先で検索する((long)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed), 0);

                    double db一小節後 = 0.0;
                    if (p判定枠に最も近いチップ != null)
                        db一小節後 = ((15000.0 / p判定枠に最も近いチップ.dbBPM * (p判定枠に最も近いチップ.fNow_Measure_s / p判定枠に最も近いチップ.fNow_Measure_m)) * 16.0);

                    this.t分岐処理(CDTX.ECourse.eExpert, 0, (SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed) + db一小節後);

                    TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.t分岐レイヤー_コース変化(TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.stBranch[0].nAfter, CDTX.ECourse.eExpert, 0);
                    TJAPlayer3.stage演奏ドラム画面.actMtaiko.tBranchEvent(TJAPlayer3.stage演奏ドラム画面.actMtaiko.After[0], CDTX.ECourse.eExpert, 0);


                    this.n現在のコース[0] = CDTX.ECourse.eExpert;
                    this.n次回のコース[0] = CDTX.ECourse.eExpert;
                    this.nレーン用表示コース[0] = CDTX.ECourse.eExpert;

                    this.b強制的に分岐させた[0] = true;
                }
                else if ( keyboard.KeyPressed( (int)SlimDXKeys.Key.D3 ) )		// #24243 2011.1.16 yyagi UI for InputAdjustTime in playing screen.
                {
                    if (!TJAPlayer3.DTX.bHasBranch[TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0]]) return;

                    //listBRANCHを廃止したため強制分岐の開始値を
                    //rc演奏用タイマ.n現在時刻msから引っ張ることに

                    //判定枠に一番近いチップの情報を元に一小節分の値を計算する. 2020.04.21 akasoko26
                    var p判定枠に最も近いチップ = r指定時刻に一番近い未ヒットChipを過去方向優先で検索する((long)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed), 0);

                    double db一小節後 = 0.0;
                    if (p判定枠に最も近いチップ != null)
                        db一小節後 = ((15000.0 / p判定枠に最も近いチップ.dbBPM * (p判定枠に最も近いチップ.fNow_Measure_s / p判定枠に最も近いチップ.fNow_Measure_m)) * 16.0);

                    this.t分岐処理(CDTX.ECourse.eMaster, 0, (SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed) + db一小節後);

                    TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.t分岐レイヤー_コース変化(TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.stBranch[0].nAfter, CDTX.ECourse.eMaster, 0);
                    TJAPlayer3.stage演奏ドラム画面.actMtaiko.tBranchEvent(TJAPlayer3.stage演奏ドラム画面.actMtaiko.After[0], CDTX.ECourse.eMaster, 0);

                    this.n現在のコース[0] = CDTX.ECourse.eMaster;
                    this.n次回のコース[0] = CDTX.ECourse.eMaster;
                    this.nレーン用表示コース[0] = CDTX.ECourse.eMaster;

                    this.b強制的に分岐させた[0] = true;
                }

                if ( TJAPlayer3.ConfigIni.KeyAssign.KeyIsPressed(TJAPlayer3.ConfigIni.KeyAssign.System.DisplayHits) )
                {
                    if( TJAPlayer3.ConfigIni.bJudgeCountDisplay == false )
                        TJAPlayer3.ConfigIni.bJudgeCountDisplay = true;
                    else
                        TJAPlayer3.ConfigIni.bJudgeCountDisplay = false;
                }

				if ( TJAPlayer3.ConfigIni.KeyAssign.KeyIsPressed(TJAPlayer3.ConfigIni.KeyAssign.System.CycleVideoDisplayMode) )
				{
                    switch( TJAPlayer3.ConfigIni.eClipDispType  )
                    {
                        case EClipDispType.OFF:
                            TJAPlayer3.ConfigIni.eClipDispType = EClipDispType.背景のみ;
                            break;
                        case EClipDispType.背景のみ:
                            TJAPlayer3.ConfigIni.eClipDispType = EClipDispType.ウィンドウのみ;
                            break;
                        case EClipDispType.ウィンドウのみ:
                            TJAPlayer3.ConfigIni.eClipDispType = EClipDispType.両方;
                            break;
                        case EClipDispType.両方:
                            TJAPlayer3.ConfigIni.eClipDispType = EClipDispType.OFF;
                            break;
                    }
				}

                if (TJAPlayer3.ConfigIni.bTokkunMode) 
                {
                    if (keyboard.KeyPressed((int)SlimDXKeys.Key.F6))
                    {
                        if (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[0] == false)
                            TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[0] = true;
                        else
                            TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[0] = false;
                    }
                }
            }

#if DEBUG

            if (keyboard.KeyPressed((int)SlimDXKeys.Key.F7))
            {
                if (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[1] == false)
                    TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[1] = true;
                else
                    TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[1] = false;
            }
#endif
            if ( !this.actPauseMenu.bIsActivePopupMenu && this.bPAUSE && ( ( base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED ) ) && ( base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED_FadeOut ) )
			{
				if ( keyboard.KeyPressed( (int)SlimDXKeys.Key.UpArrow ) )
				{	// UpArrow(scrollspeed up)
					ドラムスクロール速度アップ();
				}
				else if ( keyboard.KeyPressed( (int)SlimDXKeys.Key.DownArrow ) )
				{	// DownArrow (scrollspeed down)
					ドラムスクロール速度ダウン();
				}
				else if ( keyboard.KeyPressed( (int)SlimDXKeys.Key.Delete ) )
				{	// del (debug info)
					TJAPlayer3.ConfigIni.b演奏情報を表示する = !TJAPlayer3.ConfigIni.b演奏情報を表示する;
				}
                else if ((keyboard.KeyPressed((int)SlimDXKeys.Key.Escape)))
                {   // escape (exit)
                    SoundManager.PlayTimer.Resume();
                    TJAPlayer3.Timer.Resume();
                    this.t演奏中止();
                }
            }

#region [ Minus & Equals Sound Group Level ]
		    KeyboardSoundGroupLevelControlHandler.Handle(
		        keyboard, TJAPlayer3.SoundGroupLevelController, TJAPlayer3.Skin, false);
#endregion
		}

		protected void t入力メソッド記憶( EInstrumentPad part )
		{
			if ( TJAPlayer3.Pad.st検知したデバイス.Keyboard )
			{
				this.b演奏にキーボードを使った = true;
			}
			if ( TJAPlayer3.Pad.st検知したデバイス.Joypad )
			{
				this.b演奏にジョイパッドを使った = true;
			}
			if ( TJAPlayer3.Pad.st検知したデバイス.MIDIIN )
			{
				this.b演奏にMIDI入力を使った = true;
			}
			if ( TJAPlayer3.Pad.st検知したデバイス.Mouse )
			{
				this.b演奏にマウスを使った = true;
			}
		}


		protected abstract void t進行描画_AVI();
		protected void t進行描画_AVI(int x, int y)
		{
			if ( ( ( base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED ) && ( base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED_FadeOut ) ) && ( !TJAPlayer3.ConfigIni.bストイックモード && TJAPlayer3.ConfigIni.bAVI有効 ) )
			{
				this.actAVI.t進行描画( x, y );
			}
		}
		protected abstract void t進行描画_DANGER();

		protected void t進行描画_STAGEFAILED()
		{
            // Transition for failed games
			if ( ( ( base.ePhaseID == CStage.EPhase.Game_STAGE_FAILED ) 
                || ( base.ePhaseID == CStage.EPhase.Game_STAGE_FAILED_FadeOut ) ) 
                && ( ( this.actStageFailed.Draw() != 0 ) 
                && ( base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED_FadeOut ) ) )
			{
                if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)
                {
                    this.eフェードアウト完了時の戻り値 = E演奏画面の戻り値.ステージクリア;
                }
                else
                {
                    this.eフェードアウト完了時の戻り値 = E演奏画面の戻り値.ステージ失敗;
                    
                }
                base.ePhaseID = CStage.EPhase.Game_STAGE_FAILED_FadeOut;
                this.actFO.tフェードアウト開始();
            }
		}

		protected abstract void t進行描画_パネル文字列();
		protected void t進行描画_パネル文字列( int x, int y )
		{
			if ( ( base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED ) && ( base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED_FadeOut ) )
			{
				this.actPanel.t進行描画( x, y );
			}
		}
		protected void tパネル文字列の設定()
		{
		    // When performing calibration, inform the player that
		    // calibration is taking place, rather than
		    // displaying the panel title or song title as usual.

		    var panelString = TJAPlayer3.IsPerformingCalibration
		        ? "Calibrating input..."
		        : string.IsNullOrEmpty( TJAPlayer3.DTX.PANEL ) ? TJAPlayer3.DTX.TITLE: TJAPlayer3.DTX.PANEL;

		    this.actPanel.SetPanelString( panelString, 
                TJAPlayer3.stageSongSelect.rChoosenSong.str本当のジャンル, 
                TJAPlayer3.Skin.Game_StageText, 
                songNode: TJAPlayer3.stageSongSelect.rChoosenSong);
		}


		protected void t進行描画_ゲージ()
		{
			if ( ( ( ( base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED ) && ( base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED_FadeOut ) ) ) )
			{
				this.actGauge.Draw();
			}
		}
		protected void t進行描画_コンボ()
		{
			this.actCombo.Draw();
		}
		protected void t進行描画_スコア()
		{
			this.actScore.Draw();
		}

		protected bool t進行描画_チップ( EInstrumentPad ePlayMode, int nPlayer )
		{
			if ( ( base.ePhaseID == CStage.EPhase.Game_STAGE_FAILED ) || ( base.ePhaseID == CStage.EPhase.Game_STAGE_FAILED_FadeOut ) )
			{
				return true;
			}
			if ( ( this.n現在のトップChip == -1 ) || ( this.n現在のトップChip >= listChip[ nPlayer ].Count ) )
			{
				return true;
			}
            if (IsDanFailed)
            {
                return true;
            }

            var n現在時刻ms = (long)(SoundManager.PlayTimer.NowTimeMs * TJAPlayer3.ConfigIni.SongPlaybackSpeed);

            NowAIBattleSectionTime = (int)n現在時刻ms - NowAIBattleSection.StartTime;

            if ( this.r指定時刻に一番近い未ヒットChip( (long)n現在時刻ms, 0x50, 0, 1000000, nPlayer ) == null )
            {
                this.actChara.b演奏中[nPlayer] = false;
            }

			var db現在の譜面スクロール速度 = this.act譜面スクロール速度.db現在の譜面スクロール速度;

			//double speed = 264.0;	// BPM150の時の1小節の長さ[dot]
			const double speed = 324.0;	// BPM150の時の1小節の長さ[dot]

			double ScrollSpeedDrums = (( db現在の譜面スクロール速度[nPlayer] + 1.0 ) * speed ) * 0.5 * 37.5 / 60000.0;
			double ScrollSpeedGuitar = ( db現在の譜面スクロール速度[nPlayer] + 1.0 ) * 0.5 * 0.5 * 37.5 * speed / 60000.0;
			double ScrollSpeedBass = ( db現在の譜面スクロール速度[nPlayer] + 1.0 ) * 0.5 * 0.5 * 37.5 * speed / 60000.0;
            double ScrollSpeedTaiko = (( db現在の譜面スクロール速度[nPlayer] + 1.0 ) * speed ) * 0.5 * 37.5 / 60000.0;


			CConfigIni configIni = TJAPlayer3.ConfigIni;

			CDTX dTX = TJAPlayer3.DTX;
            bool bAutoPlay = configIni.b太鼓パートAutoPlay[nPlayer];
            switch ( nPlayer ) //2017.08.11 kairera0467
            {
                case 1:
                    bAutoPlay = configIni.b太鼓パートAutoPlay[nPlayer] || TJAPlayer3.ConfigIni.bAIBattleMode;
                    dTX = TJAPlayer3.DTX_2P;
                    break;
                case 2:
                    dTX = TJAPlayer3.DTX_3P;
                    break;
                case 3:
                    dTX = TJAPlayer3.DTX_4P;
                    break;
                case 4:
                    dTX = TJAPlayer3.DTX_5P;
                    break;
                default:
                    break;
            }

            if( this.n分岐した回数[ nPlayer ] == 0 )
            {
                this.bUseBranch[ nPlayer ] = dTX.bHIDDENBRANCH ? false : dTX.bチップがある.Branch;
            }


            //CDTXMania.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.灰, this.nLoopCount_Clear.ToString()  );

            float play_bpm_time = this.GetNowPBMTime(dTX, 0);

            //for ( int nCurrentTopChip = this.n現在のトップChip; nCurrentTopChip < dTX.listChip.Count; nCurrentTopChip++ )
            for ( int nCurrentTopChip = dTX.listChip.Count - 1; nCurrentTopChip > 0; nCurrentTopChip-- )
			{
				CDTX.CChip pChip = dTX.listChip[ nCurrentTopChip ];
                //Debug.WriteLine( "nCurrentTopChip=" + nCurrentTopChip + ", ch=" + pChip.nチャンネル番号.ToString("x2") + ", 発音位置=" + pChip.n発声位置 + ", 発声時刻ms=" + pChip.n発声時刻ms );
                long time = pChip.n発声時刻ms - n現在時刻ms;
				pChip.nバーからの距離dot.Drums = (int) ( time * ScrollSpeedDrums );
				pChip.nバーからの距離dot.Guitar = (int) ( time * ScrollSpeedGuitar );
				pChip.nバーからの距離dot.Bass = (int) ( time * ScrollSpeedBass );

                double _scrollSpeed = pChip.dbSCROLL * (db現在の譜面スクロール速度[nPlayer] + 1.0) / 10.0;
                double _scrollSpeed_Y = pChip.dbSCROLL_Y * (db現在の譜面スクロール速度[nPlayer] + 1.0) / 10.0;
                pChip.nバーからの距離dot.Taiko = NotesManager.GetNoteX(pChip, time * pChip.dbBPM, _scrollSpeed, TJAPlayer3.Skin.Game_Notes_Interval, play_bpm_time, pChip.eScrollMode, false);
                if ( pChip.nノーツ終了時刻ms != 0 )
                {
                    pChip.nバーからのノーツ末端距離dot = NotesManager.GetNoteX(pChip, (pChip.nノーツ終了時刻ms - n現在時刻ms) * pChip.dbBPM, _scrollSpeed, TJAPlayer3.Skin.Game_Notes_Interval, play_bpm_time, pChip.eScrollMode, true);
                    pChip.nバーからのノーツ末端距離dot_Y = NotesManager.GetNoteY(pChip, (pChip.nノーツ終了時刻ms - n現在時刻ms) * pChip.dbBPM, _scrollSpeed_Y, TJAPlayer3.Skin.Game_Notes_Interval, play_bpm_time, pChip.eScrollMode, true);
                }
                    

                if ( pChip.eScrollMode == EScrollMode.BMSCROLL || pChip.eScrollMode == EScrollMode.HBSCROLL )
                {

                    /*
                    pChip.nバーからの距離dot.Taiko = (int)(3 * 0.8335 * ((pChip.fBMSCROLLTime * NOTE_GAP) - (play_bpm_time * NOTE_GAP)) * dbSCROLL * (db現在の譜面スクロール速度[nPlayer] + 1) / 2 / 5.0);
                    if ( pChip.nノーツ終了時刻ms != 0 )
                        pChip.nバーからのノーツ末端距離dot = (int)(3 * 0.8335 * ((pChip.fBMSCROLLTime_end * NOTE_GAP) - (play_bpm_time * NOTE_GAP)) * pChip.dbSCROLL * (db現在の譜面スクロール速度[nPlayer] + 1.0) / 2 / 5.0);
                    */
                }

				int instIndex = (int) pChip.e楽器パート;

                if (!pChip.IsMissed && !pChip.bHit)
                {
                    if (NotesManager.IsMissableNote(pChip))//|| pChip.nチャンネル番号 == 0x9A )
                    {
                        //こっちのほうが適格と考えたためフラグを変更.2020.04.20 Akasoko26
                        if (time <= 0)
                        {
                            if (this.e指定時刻からChipのJUDGEを返す(n現在時刻ms, pChip, nPlayer) == ENoteJudge.Miss)
                            {
                                pChip.IsMissed = true;
                                pChip.eNoteState = ENoteState.bad;
                                this.tチップのヒット処理(n現在時刻ms, pChip, EInstrumentPad.TAIKO, false, 0, nPlayer);
                            }
                        }
                    }
                }

                if( pChip.nバーからの距離dot[ instIndex ] < -150 )
                {
                    if( !(NotesManager.IsMissableNote(pChip)))
                    {
                        //2016.02.11 kairera0467
                        //太鼓の単音符の場合は座標による判定を行わない。
                        //(ここで判定をすると高スピードでスクロールしている時に見逃し不可判定が行われない。)
                        pChip.bHit = true;
                    }
                }

                var cChipCurrentlyInProcess = chip現在処理中の連打チップ[ nPlayer ];
                if( cChipCurrentlyInProcess != null && !cChipCurrentlyInProcess.bHit )
                {

                    //if( cChipCurrentlyInProcess.nチャンネル番号 >= 0x13 && cChipCurrentlyInProcess.nチャンネル番号 <= 0x15 )//|| pChip.nチャンネル番号 == 0x9A )
                    if (NotesManager.IsBigNote(cChipCurrentlyInProcess))
                    {
				        if ( ( ( cChipCurrentlyInProcess.nバーからの距離dot.Taiko < -500 ) && ( cChipCurrentlyInProcess.n発声時刻ms <= n現在時刻ms && cChipCurrentlyInProcess.nノーツ終了時刻ms >= n現在時刻ms ) ) )
                           //( ( chip現在処理中の連打チップ.nバーからのノーツ末端距離dot.Taiko < -500 ) && ( chip現在処理中の連打チップ.n発声時刻ms <= CSound管理.rc演奏用タイマ.n現在時刻ms && chip現在処理中の連打チップ.nノーツ終了時刻ms >= CSound管理.rc演奏用タイマ.n現在時刻ms ) ) )
                           //( ( pChip.n発声時刻ms <= CSound管理.rc演奏用タイマ.n現在時刻ms && pChip.nノーツ終了時刻ms >= CSound管理.rc演奏用タイマ.n現在時刻ms ) ) )
		    		    {
                            if( bAutoPlay )
    		    		        this.tチップのヒット処理( n現在時刻ms, cChipCurrentlyInProcess, EInstrumentPad.TAIKO, false, 0, nPlayer );
	    		    	}
                    }
                }


                if(pChip.nPlayerSide == nPlayer && pChip.n発声時刻ms >= n現在時刻ms)
                {
                    NowProcessingChip[pChip.nPlayerSide] = nCurrentTopChip;
                }
                
				switch ( pChip.nチャンネル番号 )
				{
#region [ 01: BGM ]
					case 0x01:	// BGM
						if ( !pChip.bHit && time < 0)
						{
							pChip.bHit = true;
							if ( configIni.bBGM音を発声する )
							{
                                dTX.tチップの再生(pChip, SoundManager.PlayTimer.PrevResetTime + (long)(pChip.n発声時刻ms / TJAPlayer3.ConfigIni.SongPlaybackSpeed));
                            }
                        }
						break;
#endregion
#region [ 03: BPM変更 ]
					case 0x03:	// BPM変更
						if ( !pChip.bHit && time < 0)
						{
							pChip.bHit = true;
                            this.actPlayInfo.dbBPM[nPlayer] = dTX.BASEBPM; //2016.07.10 kairera0467 太鼓の仕様にあわせて修正。(そもそもの仕様が不明&コードミス疑惑)
                        }
                        break;
#endregion
#region [ 08: BPM変更(拡張) ]
					case 0x08:	// BPM変更(拡張)
                        //CDTXMania.act文字コンソール.tPrint( 414 + pChip.nバーからの距離dot.Drums + 4, 192, C文字コンソール.Eフォント種別.白, "BRANCH START" + "  " + pChip.n整数値.ToString() );
						if ( !pChip.bHit && time < 0)
						{
							pChip.bHit = true;
                            //if( pChip.nコース == this.n現在のコース[ nPlayer ] )
                            //{
                                //double bpm = ( dTX.listBPM[ pChip.n整数値_内部番号 ].dbBPM値 * ( ( (double) configIni.n演奏速度 ) / 20.0 ) );
                                //int nUnit = (int)((60.0 / ( bpm ) / this.actChara.nキャラクター通常モーション枚数 ) * 1000 );
                                //int nUnit_gogo = (int)((60.0 / ( bpm ) / this.actChara.nキャラクターゴーゴーモーション枚数 ) * 1000 );
                                //this.actChara.ct通常モーション = new CCounter( 0, this.actChara.nキャラクター通常モーション枚数 - 1, nUnit, CDTXMania.Timer );
                                //this.actChara.ctゴーゴーモーション = new CCounter(0, this.actChara.nキャラクターゴーゴーモーション枚数 - 1, nUnit_gogo * 2, CDTXMania.Timer);

                            //}
						}
						break;
#endregion

#region [ 11-1f & 101-: Taiko ]
					case 0x11:
					case 0x12:
					case 0x13:
					case 0x14:
                    case 0x1C:
                    case 0x101:
                        {
                            this.t進行描画_チップ_Taiko( configIni, ref dTX, ref pChip, nPlayer );
                        }
                        break;

					case 0x15:
					case 0x16:
					case 0x17:
                    case 0x19:
                    case 0x1D:
                        {
                            //2015.03.28 kairera0467
                            //描画順序を変えるため、メイン処理だけをこちらに残して描画処理は分離。

                            //this.t進行描画_チップ_Taiko連打(configIni, ref dTX, ref pChip);
                            //2015.04.13 kairera0467 ここを外さないと恋文2000の連打に対応できず、ここをつけないと他のコースと重なっている連打をどうにもできない。
                            //常時実行メソッドに渡したら対応できた!?
                            //if ((!pChip.bHit && (pChip.nバーからの距離dot.Drums < 0)))
                            {
                                if( ( pChip.n発声時刻ms <= (int)n現在時刻ms && pChip.nノーツ終了時刻ms >= (int)n現在時刻ms ) )
                                {
                                    //if( this.n現在のコース == pChip.nコース )
                                    if( pChip.b可視 == true )
                                        this.chip現在処理中の連打チップ[ nPlayer ] = pChip;
                                }
                            }
                            if ( !pChip.bProcessed && time < 0)
						    {
                                if (NotesManager.IsKusudama(pChip))
                                {
                                    if (!this.b連打中[nPlayer] && nPlayer == 0)
                                    {
                                        actBalloon.KusuIn();
                                        actChara.KusuIn();
                                        for(int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                                        {
                                            this.b連打中[i] = true;
                                            this.actChara.b風船連打中[i] = true;
                                        }
                                    }

                                    nCurrentKusudamaRollCount = 0;
                                    nCurrentKusudamaCount += pChip.nBalloon;
                                    for(int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                                    {
                                        n風船残り[i] = nCurrentKusudamaCount;
                                    }
                                    pChip.bProcessed = true;
                                }
                            }
                            if (pChip.n描画優先度 <= 0)
                                this.t進行描画_チップ_Taiko連打(configIni, ref dTX, ref pChip, nPlayer);
                        }

                        break;
                    case 0x18:
                        {
                            if( ( !pChip.bProcessed && time < 0) )
                            {
                                this.b連打中[ nPlayer ] = false;
                                this.actRoll.b表示[ nPlayer ] = false;
                                this.actChara.b風船連打中[nPlayer] = false;
                                pChip.bProcessed = true;
                                if( chip現在処理中の連打チップ[ nPlayer ] != null )
                                {
                                    chip現在処理中の連打チップ[ nPlayer ].bHit = true;
                                    if (NotesManager.IsKusudama(chip現在処理中の連打チップ[nPlayer]))
                                    {
                                        if (nCurrentKusudamaCount > nCurrentKusudamaRollCount)
                                        {
                                            if ( nPlayer == 0) 
                                            {
                                                actBalloon.KusuMiss();
                                                TJAPlayer3.Skin.soundKusudamaMiss.tPlay();
                                                for (int p = 0; p < TJAPlayer3.ConfigIni.nPlayerCount; p++)
                                                {
                                                    {
                                                        this.actChara.ChangeAnime(p, CAct演奏Drumsキャラクター.Anime.Kusudama_Miss, true);

                                                        if (actChara.CharaAction_Balloon_Delay[p] != null) actChara.CharaAction_Balloon_Delay[p] = new CCounter(0, 
                                                            TJAPlayer3.Skin.Characters_Balloon_Delay[actChara.iCurrentCharacter[p]] - 1, 
                                                            1, 
                                                            TJAPlayer3.Timer);
                                                    }
                                                }
                                                nCurrentKusudamaRollCount = 0;
                                                nCurrentKusudamaCount = 0;
                                            }
                                            
                                        }
                                    }
                                    else 
                                    {
                                        if (chip現在処理中の連打チップ[nPlayer].nBalloon > chip現在処理中の連打チップ[nPlayer].nRollCount 
                                            && chip現在処理中の連打チップ[nPlayer].nRollCount > 0)
                                        {
                                            {
                                                this.actChara.ChangeAnime(nPlayer, CAct演奏Drumsキャラクター.Anime.Balloon_Miss, true);

                                                if (actChara.CharaAction_Balloon_Delay[nPlayer] != null) actChara.CharaAction_Balloon_Delay[nPlayer] = new CCounter(0, 
                                                    TJAPlayer3.Skin.Characters_Balloon_Delay[actChara.iCurrentCharacter[nPlayer]] - 1, 
                                                    1, 
                                                    TJAPlayer3.Timer);
                                            }
                                         }
                                    }
                                    if (chip現在処理中の連打チップ[nPlayer].nBalloon > chip現在処理中の連打チップ[nPlayer].nRollCount)
                                    {
                                        if (pChip.n連打音符State == 13)
                                        {
                                            this.actJudgeString.Start(nPlayer, ENoteJudge.Mine);
                                            TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.Start(0x11, ENoteJudge.Bad, true, nPlayer);
                                            TJAPlayer3.stage演奏ドラム画面.actChipFireD.Start(0x11, ENoteJudge.Mine, nPlayer);
                                            actGauge.MineDamage(nPlayer);
                                            TJAPlayer3.Skin.soundBomb?.tPlay();
                                            this.CChartScore[nPlayer].nMine++;
                                            this.CSectionScore[nPlayer].nMine++;
                                            this.CBranchScore[nPlayer].nMine++;
                                            if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)
                                                CFloorManagement.damage();
                                            if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                                                this.nMine[actDan.NowShowingNumber]++;
                                            this.actCombo.n現在のコンボ数[nPlayer] = 0;
                                            if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                                                this.nCombo[actDan.NowShowingNumber] = 0;
                                            this.actComboVoice.tReset(nPlayer);
                                            this.bIsMiss[nPlayer] = true;
                                        }
                                    }
                                    chip現在処理中の連打チップ[nPlayer] = null;

                                }
                                this.eRollState = E連打State.none;
                            }
                            if( pChip.n描画優先度 <= 0 )
                                this.t進行描画_チップ_Taiko連打(configIni, ref dTX, ref pChip, nPlayer);
                        }

                        break;

                    case 0x1e:
                        break;

					case 0x1a:
                    case 0x1b:
					case 0x1f:
                        {
                            this.t進行描画_チップ_Taiko( configIni, ref dTX, ref pChip, nPlayer );
                        }
						break;
#endregion
#region [ 20-2F: EmptySlot ]
					case 0x20:
					case 0x21:
                        {
                            if ((pChip.n発声時刻ms <= (int)n現在時刻ms && pChip.nノーツ終了時刻ms >= (int)n現在時刻ms))
                            {
                                //if( this.n現在のコース == pChip.nコース )
                                if (pChip.b可視 == true)
                                    this.chip現在処理中の連打チップ[nPlayer] = pChip;
                            }
                            if (pChip.n描画優先度 <= 0)
                                this.t進行描画_チップ_Taiko連打(configIni, ref dTX, ref pChip, nPlayer);
                        }
                        break;
                    case 0x22:
					case 0x23:
					case 0x24:
					case 0x25:
					case 0x26:
					case 0x27:
					case 0x28:
                    case 0x29:
                    case 0x2a:
                    case 0x2b:
                    case 0x2c:
                    case 0x2d:
                    case 0x2e:
                    case 0x2f:
						break;
#endregion
#region [ 31-3f: EmptySlot ]
					case 0x31:
					case 0x32:
					case 0x33:
					case 0x34:
					case 0x35:
					case 0x36:
					case 0x37:
					case 0x38:
					case 0x39:
					case 0x3a:
                    case 0x3b:
                    case 0x3c:
                    case 0x3d:
                    case 0x3e:
                    case 0x3f:
						break;
#endregion

#region [ 50: 小節線 ]
					case 0x50:	// 小節線
						{

                            if ( !pChip.bHit && time < 0)
                            {
                                //if (nPlayer == 0) TJAPlayer3.BeatScaling = new CCounter(0, 1000, 120.0 / pChip.dbBPM / 2.0, TJAPlayer3.Timer);
                                if (NowAIBattleSectionTime >= NowAIBattleSection.Length && NowAIBattleSection.End == AIBattleSection.EndType.None && nPlayer == 0)
                                {
                                    PassAIBattleSection();

                                    NowAIBattleSectionCount++;

                                    if (AIBattleSections.Count > NowAIBattleSectionCount)
                                    {
                                        NowAIBattleSectionTime = 0;
                                    }
                                    NowAIBattleSectionTime = (int)n現在時刻ms - NowAIBattleSection.StartTime;
                                }


                                this.actChara.b演奏中[nPlayer] = true;
                                if( this.actPlayInfo.NowMeasure[nPlayer] == 0 )
                                {
                                    UpdateCharaCounter(nPlayer);
                                }
                                if (!bPAUSE)//2020.07.08 Mr-Ojii KabanFriends氏のコードを参考に
                                {
                                    actPlayInfo.NowMeasure[nPlayer] = pChip.n整数値_内部番号;
                                }
                                pChip.bHit = true;
                            }
                            this.t進行描画_チップ_小節線( configIni, ref dTX, ref pChip, nPlayer );
							break;
						}
#endregion
#region [ 51: 拍線 ]
					case 0x51:	// 拍線
						if ( !pChip.bHit && time < 0)
						{
							pChip.bHit = true;
						}
						break;
#endregion
#region [ 54: 動画再生 ]
					case 0x54:	// 動画再生
						if ( !pChip.bHit && time < 0)
						{
							pChip.bHit = true;
							if ( configIni.bAVI有効 )
							{
                                if ((dTX.listVD.TryGetValue(pChip.n整数値_内部番号, out CVideoDecoder vd)))
                                {
                                    ShowVideo = true;
                                    if (TJAPlayer3.ConfigIni.bAVI有効 && vd != null)
                                    {
                                        this.actAVI.Start(pChip.nチャンネル番号, vd);
                                        this.actAVI.Seek(pChip.VideoStartTimeMs);
                                    }
                                }
							}
						}
						break;
					case 0x55:
						if ( !pChip.bHit && time < 0)
						{
							pChip.bHit = true;
							if ( configIni.bAVI有効 )
							{
                                if ((dTX.listVD.TryGetValue(pChip.n整数値_内部番号, out CVideoDecoder vd)))
                                {
                                    ShowVideo = false;
                                    if (TJAPlayer3.ConfigIni.bAVI有効 && vd != null)
                                    {
                                        this.actAVI.Stop();
                                    }
                                }

                                if ((dTX.listVD.TryGetValue(1, out CVideoDecoder vd2)))
                                {
                                    ShowVideo = true;
                                    if (TJAPlayer3.ConfigIni.bAVI有効 && vd != null)
                                    {
                                        this.actAVI.Start(pChip.nチャンネル番号, vd);
                                    }
                                }
							}
						}
						break;
#endregion
#region[ 55-60: EmptySlot ]
                    case 0x56:
                    case 0x57:
                    case 0x58:
                    case 0x59:
                        break;
#endregion
#region [ 61-89: EmptySlot ]
                    case 0x60:
                    case 0x61:
					case 0x62:
					case 0x63:
					case 0x64:
					case 0x65:
					case 0x66:
					case 0x67:
					case 0x68:
					case 0x69:
					case 0x70:
					case 0x71:
					case 0x72:
					case 0x73:
					case 0x74:
					case 0x75:
					case 0x76:
					case 0x77:
					case 0x78:
					case 0x79:
					case 0x80:
					case 0x81:
					case 0x82:
					case 0x83:
                    case 0x84:
                    case 0x85:
                    case 0x86:
                    case 0x87:
                    case 0x88:
                    case 0x89:
                        break;
#endregion

#region[ 90-9A: EmptySlot ]
                    case 0x90:
					case 0x91:
					case 0x92:
                    case 0x93:
                    case 0x94:
                    case 0x95:
                    case 0x96:
                    case 0x97:
                    case 0x98:
                    case 0x99:
                    case 0x9A:
						break;
#endregion

#region[ 9B-9F: 太鼓 ]
                    case 0x9B:
                        // 段位認定モードの幕アニメーション
                        if ( !pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            this.actPanel.t歌詞テクスチャを削除する();
                            if (pChip.nコース == this.n現在のコース[nPlayer])
                            {
                                this.actDan.Update();
                                if (ListDan_Number != 0 && actDan.FirstSectionAnime)
                                {
                                    if (this.actDan.GetFailedAllChallenges())
                                    {
                                        this.n現在のトップChip = TJAPlayer3.DTX.listChip.Count - 1;   // 終端にシーク
                                        IsDanFailed = true;
                                        return true;
                                    }

                                    // Play next song here
                                    this.actDan.Start(this.ListDan_Number);
                                    ListDan_Number++;
                                }
                                else
                                {
                                    actDan.FirstSectionAnime = true;
                                }
                            }
                        }
                        break;
                    //0x9C BPM変化(アニメーション用)
                    case 0x9C:
                        //CDTXMania.act文字コンソール.tPrint( 414 + pChip.nバーからの距離dot.Taiko + 8, 192, C文字コンソール.Eフォント種別.白, "BPMCHANGE" );
						if ( !pChip.bHit && time < 0)
						{
							pChip.bHit = true;
                            if( pChip.nコース == this.n現在のコース[ nPlayer ] )
                            {
							    if ( dTX.listBPM.TryGetValue( pChip.n整数値_内部番号, out CDTX.CBPM cBPM ) )
							    {
                                    this.actPlayInfo.dbBPM[nPlayer] = cBPM.dbBPM値;// + dTX.BASEBPM;
                                }


                                for (int i = 0; i < 5; i++)
                                {
                                    ctChipAnime[i] = new CCounter(0, 3, 60.0 / TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[nPlayer] * 1 / 4, SoundManager.PlayTimer);
                                }

                                UpdateCharaCounter(nPlayer);
                                //this.actDancer.ct踊り子モーション = new CCounter(0, this.actDancer.ar踊り子モーション番号.Length - 1, (dbUnit * CDTXMania.Skin.Game_Dancer_Beat) / this.actDancer.ar踊り子モーション番号.Length, CSound管理.rc演奏用タイマ);
                                //this.actChara.ctモブモーション = new CCounter(0, this.actChara.arモブモーション番号.Length - 1, (dbUnit) / this.actChara.arモブモーション番号.Length, CSound管理.rc演奏用タイマ);
                                //#if C_82D982F182AF82CD82A282AF82A2
                                /*
                                 * for( int dancer = 0; dancer < 5; dancer++ )
                                    this.actDancer.st投げ上げ[ dancer ].ct進行 = new CCounter( 0, this.actDancer.arモーション番号_登場.Length - 1, dbUnit / this.actDancer.arモーション番号_登場.Length, CSound管理.rc演奏用タイマ );

                                this.actDancer.ct通常モーション = new CCounter( 0, this.actDancer.arモーション番号_通常.Length - 1, ( dbUnit * 4 ) / this.actDancer.arモーション番号_通常.Length, CSound管理.rc演奏用タイマ );
                                this.actDancer.ctモブ = new CCounter( 1.0, 16.0, (int)((60.0 / bpm / 16.0 ) * 1000 ), CSound管理.rc演奏用タイマ );
//#endif
                               */
        }

    }
                        break;

                    case 0x9D: //SCROLL
						if ( !pChip.bHit && time < 0)
						{
							pChip.bHit = true;
							//if ( dTX.listSCROLL.ContainsKey( pChip.n整数値_内部番号 ) )
							//{
								//this.actPlayInfo.dbBPM = ( dTX.listBPM[ pChip.n整数値_内部番号 ].dbBPM値 * ( ( (double) configIni.n演奏速度 ) / 20.0 ) );// + dTX.BASEBPM;
							//}
						}
                        break;

                    case 0x9E: //ゴーゴータイム
                        if( !pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            this.bIsGOGOTIME[ nPlayer ] = true;
                            //double dbUnit = (((60.0 / (CDTXMania.stage演奏ドラム画面.actPlayInfo.dbBPM))));
                            double dbUnit = (((60.0 / pChip.dbBPM)));

                            int Character = this.actChara.iCurrentCharacter[nPlayer];

                            {
                                if (TJAPlayer3.Skin.Characters_GoGoStart_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded)
                                {
                                    if (!HGaugeMethods.UNSAFE_IsRainbow(nPlayer) && (!HGaugeMethods.UNSAFE_FastNormaCheck(nPlayer) || TJAPlayer3.Skin.Characters_GoGoStart_Clear_Ptn[Character] == 0))
                                    {
                                        // 魂ゲージMAXではない
                                        // ゴーゴースタート_ノーマル
                                        this.actChara.ChangeAnime(nPlayer, CAct演奏Drumsキャラクター.Anime.GoGoStart, true);
                                        //this.actChara.キャラクター_アクション_10コンボ();
                                    }
                                }
                                if (TJAPlayer3.Skin.Characters_GoGoStart_Clear_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded)
                                {
                                    if (!HGaugeMethods.UNSAFE_IsRainbow(nPlayer) && HGaugeMethods.UNSAFE_FastNormaCheck(nPlayer))
                                    {
                                        this.actChara.ChangeAnime(nPlayer, CAct演奏Drumsキャラクター.Anime.GoGoStart_Clear, true);
                                    }
                                }
                                if (TJAPlayer3.Skin.Characters_GoGoStart_Maxed_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded)
                                {
                                    if (HGaugeMethods.UNSAFE_IsRainbow(nPlayer))
                                    {
                                        // 魂ゲージMAX
                                        // ゴーゴースタート_MAX
                                        this.actChara.ChangeAnime(nPlayer, CAct演奏Drumsキャラクター.Anime.GoGoStart_Max, true);
                                    }
                                }

                            }
                            TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.GOGOSTART();
                        }
                        break;
                    case 0x9F: //ゴーゴータイム
                        if( !pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            this.bIsGOGOTIME[ nPlayer ] = false;
                        }
                        break;
                    #endregion

                    #region [ EXTENDED COMMANDS ]
                    case 0xa0: //camera vertical move start
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            this.currentCamVMoveChip = pChip;
                            this.ctCamVMove = new CCounter(0, pChip.fCamTimeMs, 1, TJAPlayer3.Timer);
                        }
                        break;
                    case 0xa1: //camera vertical move end
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                        }
                        break;
                    case 0xa2: //camera horizontal move start
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            this.currentCamHMoveChip = pChip;
                            this.ctCamHMove = new CCounter(0, pChip.fCamTimeMs, 1, TJAPlayer3.Timer);
                        }
                        break;
                    case 0xa3: //camera horizontal move end
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                        }
                        break;
                    case 0xa4: //camera zoom start
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            this.currentCamZoomChip = pChip;
                            this.ctCamZoom = new CCounter(0, pChip.fCamTimeMs, 1, TJAPlayer3.Timer);
                        }
                        break;
                    case 0xa5: //camera zoom end
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                        }
                        break;
                    case 0xa6: //camera rotation start
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            this.currentCamRotateChip = pChip;
                            this.ctCamRotation = new CCounter(0, pChip.fCamTimeMs, 1, TJAPlayer3.Timer);
                        }
                        break;
                    case 0xa7: //camera rotation end
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                        }
                        break;
                    case 0xa8: //camera vertical scaling start
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            this.currentCamVScaleChip = pChip;
                            this.ctCamVScale = new CCounter(0, pChip.fCamTimeMs, 1, TJAPlayer3.Timer);
                        }
                        break;
                    case 0xa9: //camera vertical scaling end
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                        }
                        break;
                    case 0xb0: //camera horizontal scaling start
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            this.currentCamHScaleChip = pChip;
                            this.ctCamHScale = new CCounter(0, pChip.fCamTimeMs, 1, TJAPlayer3.Timer);
                        }
                        break;
                    case 0xb1: //camera horizontal scaling end
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                        }
                        break;
                    case 0xb2: //change border color
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            TJAPlayer3.borderColor = pChip.borderColor;
                        }
                        break;
                    case 0xb3: //set camera x offset
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;

                            this.currentCamHMoveChip = pChip;
                            this.ctCamHMove = new CCounter(0, 0, 1, TJAPlayer3.Timer);
                        }
                        break;
                    case 0xb4: //set camera y offset
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;

                            this.currentCamVMoveChip = pChip;
                            this.ctCamVMove = new CCounter(0, 0, 1, TJAPlayer3.Timer);
                        }
                        break;
                    case 0xb5: //set camera zoom factor
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;

                            this.currentCamZoomChip = pChip;
                            this.ctCamZoom = new CCounter(0, 0, 1, TJAPlayer3.Timer);
                        }
                        break;
                    case 0xb6: //set camera rotation
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;

                            this.currentCamRotateChip = pChip;
                            this.ctCamRotation = new CCounter(0, 0, 1, TJAPlayer3.Timer);
                        }
                        break;
                    case 0xb7: //set camera x scale
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;

                            this.currentCamHScaleChip = pChip;
                            this.ctCamHScale = new CCounter(0, 0, 1, TJAPlayer3.Timer);
                        }
                        break;
                    case 0xb8: //set camera y scale
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;

                            this.currentCamVScaleChip = pChip;
                            this.ctCamVScale = new CCounter(0, 0, 1, TJAPlayer3.Timer);
                        }
                        break;
                    case 0xb9: //reset camera
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;

                            TJAPlayer3.borderColor = new Color4(0f, 0f, 0f, 0f);

                            this.currentCamVMoveChip = pChip;
                            this.currentCamHMoveChip = pChip;

                            this.currentCamZoomChip = pChip;
                            this.currentCamRotateChip = pChip;

                            this.currentCamVScaleChip = pChip;
                            this.currentCamHScaleChip = pChip;

                            this.ctCamVMove = new CCounter(0, 0, 1, TJAPlayer3.Timer);
                            this.ctCamHMove = new CCounter(0, 0, 1, TJAPlayer3.Timer);

                            this.ctCamZoom = new CCounter(0, 0, 1, TJAPlayer3.Timer);
                            this.ctCamRotation = new CCounter(0, 0, 1, TJAPlayer3.Timer);

                            this.ctCamVScale = new CCounter(0, 0, 1, TJAPlayer3.Timer);
                            this.ctCamHScale = new CCounter(0, 0, 1, TJAPlayer3.Timer);
                        }
                        break;
                    case 0xba: //enable doron
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            bCustomDoron = true;
                        }
                        break;
                    case 0xbb: //disable doron
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            bCustomDoron = false;
                        }
                        break;
                    case 0xbc: //add object
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;

                            dTX.listObj.TryGetValue(pChip.strObjName, out CSongObject obj);
                            obj.x = pChip.fObjX;
                            obj.y = pChip.fObjY;
                            obj.isVisible = true;
                        }
                        break;
                    case 0xbd: //remove object
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;

                            dTX.listObj.TryGetValue(pChip.strObjName, out CSongObject obj);
                            obj.isVisible = false;
                        }
                        break;
                    case 0xbe: //object animation start
                    case 0xc0:
                    case 0xc2:
                    case 0xc4:
                    case 0xc6:
                    case 0xc8:
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;

                            dTX.listObj.TryGetValue(pChip.strObjName, out pChip.obj);
                            objHandlers.Add(pChip, new CCounter(0, pChip.fObjTimeMs, 1, TJAPlayer3.Timer));
                        }
                        break;
                    case 0xbf: //object animation end
                    case 0xc1:
                    case 0xc3:
                    case 0xc5:
                    case 0xc7:
                    case 0xc9:
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                        }
                        break;
                    case 0xca: //set object color
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;

                            dTX.listObj.TryGetValue(pChip.strObjName, out CSongObject obj);
                            obj.color = pChip.borderColor;
                        }
                        break;
                    case 0xcb: //set object y
                    case 0xcc: //set object x
                    case 0xcd: //set object vertical scale
                    case 0xce: //set object horizontal scale
                    case 0xcf: //set object rotation
                    case 0xd0: //set object opacity
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;

                            dTX.listObj.TryGetValue(pChip.strObjName, out pChip.obj);
                            objHandlers.Add(pChip, new CCounter(0, 0, 1, TJAPlayer3.Timer));
                        }
                        break;
                    case 0xd1: //change texture
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;

                            if (TJAPlayer3.Tx.trackedTextures.ContainsKey(pChip.strTargetTxName))
                            {
                                TJAPlayer3.Tx.trackedTextures.TryGetValue(pChip.strTargetTxName, out CTexture oldTx);
                                dTX.listTextures.TryGetValue(pChip.strNewPath, out CTexture newTx);

                                newTx.Opacity = oldTx.Opacity;
                                newTx.fZ軸中心回転 = oldTx.fZ軸中心回転;
                                newTx.vcScaleRatio = oldTx.vcScaleRatio;

                                oldTx.UpdateTexture(newTx, newTx.sz画像サイズ.Width, newTx.sz画像サイズ.Height);
                            }
                        }
                        break;
                    case 0xd2: //reset texture
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;

                            if (TJAPlayer3.Tx.trackedTextures.ContainsKey(pChip.strTargetTxName))
                            {
                                TJAPlayer3.Tx.trackedTextures.TryGetValue(pChip.strTargetTxName, out CTexture oldTx);
                                dTX.listOriginalTextures.TryGetValue(pChip.strTargetTxName, out CTexture originalTx);

                                originalTx.Opacity = oldTx.Opacity;
                                originalTx.fZ軸中心回転 = oldTx.fZ軸中心回転;
                                originalTx.vcScaleRatio = oldTx.vcScaleRatio;

                                oldTx.UpdateTexture(originalTx, originalTx.sz画像サイズ.Width, originalTx.sz画像サイズ.Height);
                            }
                        }
                        break;
                    case 0xd3: //set config
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            string[] split = pChip.strConfigValue.Split('=');

                            //TJAPlayer3.Skin.t文字列から読み込み(pChip.strConfigValue, split[0]);
                            bConfigUpdated = true;
                        }
                        break;
                    case 0xd4: //start object animation
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            dTX.listObj.TryGetValue(pChip.strObjName, out CSongObject obj);

                            obj.tStartAnimation(pChip.dbAnimInterval, false);
                        }
                        break;
                    case 0xd5: //start object animation (looping)
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            dTX.listObj.TryGetValue(pChip.strObjName, out CSongObject obj);

                            obj.tStartAnimation(pChip.dbAnimInterval, true);
                        }
                        break;
                    case 0xd6: //end object animation
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            dTX.listObj.TryGetValue(pChip.strObjName, out CSongObject obj);

                            obj.tStopAnimation();
                        }
                        break;
                    case 0xd7: //set object frame
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                            dTX.listObj.TryGetValue(pChip.strObjName, out CSongObject obj);

                            obj.frame = pChip.intFrame;
                        }
                        break;
                    #endregion

#region [ d8-d9: EXTENDED2 ]
					case 0xd8:
                        if (!pChip.bHit && time < 0)
                        {
                            TJAPlayer3.ConfigIni.nGameType[nPlayer] = pChip.eGameType;
                            pChip.bHit = true;
                        }
                        break;
                    case 0xd9:
                        if (!pChip.bHit && time < 0)
                        {
                            bSplitLane[nPlayer] = true;
                            pChip.bHit = true;
                        }
                        break;
#endregion

#region [ da: ミキサーへチップ音追加 ]
					case 0xDA:
						if ( !pChip.bHit && time < 0)
						{
//Debug.WriteLine( "[DA(AddMixer)] BAR=" + pChip.n発声位置 / 384 + " ch=" + pChip.nチャンネル番号.ToString( "x2" ) + ", wav=" + pChip.n整数値.ToString( "x2" ) + ", time=" + pChip.n発声時刻ms );
							pChip.bHit = true;
							if ( listWAV.TryGetValue( pChip.n整数値_内部番号, out CDTX.CWAV wc ) )	// 参照が遠いので後日最適化する
							{
								for ( int i = 0; i < nPolyphonicSounds; i++ )
								{
									if ( wc.rSound[ i ] != null )
									{
										//CDTXMania.Sound管理.AddMixer( wc.rSound[ i ] );
										AddMixer( wc.rSound[ i ], pChip.b演奏終了後も再生が続くチップである );
									}
								}
							}
						}
						break;
#endregion
#region [ db: ミキサーからチップ音削除 ]
					case 0xDB:
						if ( !pChip.bHit && time < 0)
						{
//Debug.WriteLine( "[DB(RemoveMixer)] BAR=" + pChip.n発声位置 / 384 + " ch=" + pChip.nチャンネル番号.ToString( "x2" ) + ", wav=" + pChip.n整数値.ToString( "x2" ) + ", time=" + pChip.n発声時刻ms );
							pChip.bHit = true;
							if ( listWAV.TryGetValue( pChip.n整数値_内部番号, out CDTX.CWAV wc ) )	// 参照が遠いので後日最適化する
							{
							    for ( int i = 0; i < nPolyphonicSounds; i++ )
							    {
									if ( wc.rSound[ i ] != null )
									{
										//CDTXMania.Sound管理.RemoveMixer( wc.rSound[ i ] );
										if ( !wc.rSound[ i ].b演奏終了後も再生が続くチップである )	// #32248 2013.10.16 yyagi
										{															// DTX終了後も再生が続くチップの0xDB登録をなくすことはできず。
											RemoveMixer( wc.rSound[ i ] );							// (ミキサー解除のタイミングが遅延する場合の対応が面倒なので。)
										}															// そこで、代わりにフラグをチェックしてミキサー削除ロジックへの遷移をカットする。
									}
							    }
							}
						}
						break;
#endregion

#region[ dc-df:太鼓(特殊命令) ]
                    case 0xDC: //DELAY
						if ( !pChip.bHit && time < 0)
						{
							pChip.bHit = true;
							//if ( dTX.listDELAY.ContainsKey( pChip.n整数値_内部番号 ) )
							//{
								//this.actPlayInfo.dbBPM = ( dTX.listBPM[ pChip.n整数値_内部番号 ].dbBPM値 * ( ( (double) configIni.n演奏速度 ) / 20.0 ) );// + dTX.BASEBPM;
							//}
						}
                        break;
                    case 0xDD: //SECTION
                        if (!pChip.bHit && time < 0)
                        {
                            // 分岐毎にリセットしていたのでSECTIONの命令が来たらリセットする。
                            this.tBranchReset(nPlayer);
                            pChip.bHit = true;
                        }
                        break;

                    case 0xDE: //Judgeに応じたCourseを取得
                        if (!pChip.bHit && time < 0)
                        {
                            this.b強制分岐譜面[nPlayer] = false;
                            //分岐の種類はプレイヤー関係ないと思う
                            this.eBranch種類 = pChip.e分岐の種類;
                            this.nBranch条件数値A = pChip.n条件数値A;
                            this.nBranch条件数値B = pChip.n条件数値B;
                            if (!this.bLEVELHOLD[nPlayer])
                            {
                                //成仏2000にある-2,-1だったら達人に強制分岐みたいな。
                                this.t強制用条件かを判断する(pChip.n条件数値A, pChip.n条件数値B, nPlayer);

                                TJAPlayer3.stage演奏ドラム画面.bUseBranch[nPlayer] = true;

                                CBRANCHSCORE branchScore;
                                if (TJAPlayer3.ConfigIni.bAIBattleMode)
                                {
                                    branchScore = this.CBranchScore[0];
                                }
                                else
                                {
                                    branchScore = this.CBranchScore[nPlayer];
                                }
                                this.tBranchJudge(pChip, branchScore.cBigNotes, branchScore.nScore, branchScore.nRoll, branchScore.nGreat, branchScore.nGood, branchScore.nMiss, nPlayer);

                                if (this.b強制分岐譜面[nPlayer])//強制分岐譜面だったら次回コースをそのコースにセット
                                    this.n次回のコース[nPlayer] = this.E強制コース[nPlayer];

                                this.t分岐処理(this.n次回のコース[nPlayer], nPlayer, pChip.n分岐時刻ms, pChip.e分岐の種類);

                                TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.t分岐レイヤー_コース変化(TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.stBranch[nPlayer].nAfter, this.n次回のコース[nPlayer], nPlayer);
                                TJAPlayer3.stage演奏ドラム画面.actMtaiko.tBranchEvent(TJAPlayer3.stage演奏ドラム画面.actMtaiko.After[nPlayer], this.n次回のコース[nPlayer], nPlayer);
                                this.n現在のコース[nPlayer] = this.n次回のコース[nPlayer];
                            }
                            this.n分岐した回数[nPlayer]++;
                            pChip.bHit = true;
                        }
                        break;
                    case 0x52://End処理
                        if (!pChip.bHit && time < 0)
                        {

                            pChip.bHit = true;
                        }

                        break;
                    case 0xE0:
                        //if( !pChip.bHit && time < 0 )
                        //{
                        //#BARLINEONと#BARLINEOFF
                        //演奏中は使用しません。
                        //}
                        break;
                    case 0xE1:
                        if (!pChip.bHit && time < 0)
                        {
                            //LEVELHOLD
                            this.bLEVELHOLD[nPlayer] = true;
                        }
                        break;
                    case 0xE2:
                        if( !pChip.bHit && time < 0)
                        {
                            TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.t判定枠移動(dTX.listJPOSSCROLL[nJPOSSCROLL[nPlayer]].db移動時間, dTX.listJPOSSCROLL[nJPOSSCROLL[nPlayer]].n移動距離px, dTX.listJPOSSCROLL[nJPOSSCROLL[nPlayer]].n移動方向, nPlayer, dTX.listJPOSSCROLL[nJPOSSCROLL[nPlayer]].nVerticalMove);
                            this.nJPOSSCROLL[ nPlayer ]++;
                            pChip.bHit = true;
                        }
                        break;
#endregion
#region[ f1: 歌詞 ]
                    case 0xF1:
                        if (!pChip.bHit && time < 0)
                        {
                            if (TJAPlayer3.ConfigIni.nPlayerCount == 1)
                            {
                                if (dTX.listLyric.Count > ShownLyric[nPlayer] && dTX.nPlayerSide == nPlayer)
                                {
                                    this.actPanel.t歌詞テクスチャを生成する(dTX.listLyric[ShownLyric[nPlayer]]);
                                    ShownLyric[nPlayer]++;
                                }
                            }
                            pChip.bHit = true;
                        }
                        break;
#endregion
#region[ ff: 譜面の強制終了 ]
                    //バグで譜面がとてつもないことになっているため、#ENDがきたらこれを差し込む。
                    case 0xFF:
                        if ( !pChip.bHit && time < 0)
						{
                            if (TJAPlayer3.ConfigIni.bTokkunMode)
                            {
                                foreach (CDTX.CWAV cwav in TJAPlayer3.DTX.listWAV.Values)
                                {
                                    for (int i = 0; i < nPolyphonicSounds; i++)
                                    {
                                        if ((cwav.rSound[i] != null) && cwav.rSound[i].IsPlaying)
                                        {
                                            return false;
                                        }
                                    }
                                }
                            }
                            pChip.bHit = true;
                            return true;
                        }
                        break;
                    #endregion

                    #region [ d8-d9: EXTENDED2 ]
                    case 0xe3:
                        if (!pChip.bHit && time < 0)
                        {
                            bSplitLane[nPlayer] = false;
                            pChip.bHit = true;
                        }
                        break;
                    case 0xe4:
                        if (!pChip.bHit && time < 0)
                        {
                            pChip.bHit = true;
                        }
                        this.t進行描画_チップ_小節線(configIni, ref dTX, ref pChip, nPlayer);
                        break;
                    case 0x09:
                        if (!pChip.bHit && time < 0)
                        {

                            pChip.bHit = true;
                        }
                        break;
                    case 0x0A:
                        if (!pChip.bHit && time < 0)
                        {

                            pChip.bHit = true;
                        }
                        break;
                    case 0x0B:
                        if (!pChip.bHit && time < 0)
                        {

                            pChip.bHit = true;
                        }
                        break;
                    #endregion

                    #region [ その他(未定義) ]
                    default:
						if ( !pChip.bHit && time < 0)
						{
							pChip.bHit = true;
						}
						break;
#endregion
                }

            }


            #region [ EXTENDED CONTROLS ]
            if (ctCamVMove != null) //vertical camera move
            {
                ctCamVMove.Tick();
                float value = 0.0f;
                if (currentCamVMoveChip.strCamEaseType.Equals("IN")) value = easing.EaseIn(ctCamVMove, currentCamVMoveChip.fCamScrollStartY, currentCamVMoveChip.fCamScrollEndY, currentCamVMoveChip.fCamMoveType);
                if (currentCamVMoveChip.strCamEaseType.Equals("OUT")) value = easing.EaseOut(ctCamVMove, currentCamVMoveChip.fCamScrollStartY, currentCamVMoveChip.fCamScrollEndY, currentCamVMoveChip.fCamMoveType);
                if (currentCamVMoveChip.strCamEaseType.Equals("IN_OUT")) value = easing.EaseInOut(ctCamVMove, currentCamVMoveChip.fCamScrollStartY, currentCamVMoveChip.fCamScrollEndY, currentCamVMoveChip.fCamMoveType);
                TJAPlayer3.fCamYOffset = float.IsNaN(value) ? currentCamVMoveChip.fCamScrollStartY : value;

                if (ctCamVMove.IsEnded)
                {
                    ctCamVMove = null;
                    TJAPlayer3.fCamYOffset = currentCamVMoveChip.fCamScrollEndY;
                }
            }

            if (ctCamHMove != null) //horizontal camera move
            {
                ctCamHMove.Tick();
                float value = 0.0f;
                if (currentCamHMoveChip.strCamEaseType.Equals("IN")) value = easing.EaseIn(ctCamHMove, currentCamHMoveChip.fCamScrollStartX, currentCamHMoveChip.fCamScrollEndX, currentCamHMoveChip.fCamMoveType);
                if (currentCamHMoveChip.strCamEaseType.Equals("OUT")) value = easing.EaseOut(ctCamHMove, currentCamHMoveChip.fCamScrollStartX, currentCamHMoveChip.fCamScrollEndX, currentCamHMoveChip.fCamMoveType);
                if (currentCamHMoveChip.strCamEaseType.Equals("IN_OUT")) value = easing.EaseInOut(ctCamHMove, currentCamHMoveChip.fCamScrollStartX, currentCamHMoveChip.fCamScrollEndX, currentCamHMoveChip.fCamMoveType);
                TJAPlayer3.fCamXOffset = float.IsNaN(value) ? currentCamHMoveChip.fCamScrollStartX : value;

                if (ctCamHMove.IsEnded)
                {
                    ctCamHMove = null;
                    TJAPlayer3.fCamXOffset = currentCamHMoveChip.fCamScrollEndX;
                }
            }

            if (ctCamZoom != null) //camera zoom
            {
                ctCamZoom.Tick();
                float value = 0.0f;
                if (currentCamZoomChip.strCamEaseType.Equals("IN")) value = easing.EaseIn(ctCamZoom, currentCamZoomChip.fCamZoomStart, currentCamZoomChip.fCamZoomEnd, currentCamZoomChip.fCamMoveType);
                if (currentCamZoomChip.strCamEaseType.Equals("OUT")) value = easing.EaseOut(ctCamZoom, currentCamZoomChip.fCamZoomStart, currentCamZoomChip.fCamZoomEnd, currentCamZoomChip.fCamMoveType);
                if (currentCamZoomChip.strCamEaseType.Equals("IN_OUT")) value = easing.EaseInOut(ctCamZoom, currentCamZoomChip.fCamZoomStart, currentCamZoomChip.fCamZoomEnd, currentCamZoomChip.fCamMoveType);
                TJAPlayer3.fCamZoomFactor = float.IsNaN(value) ? currentCamZoomChip.fCamZoomStart : value;

                if (ctCamZoom.IsEnded)
                {
                    ctCamZoom = null;
                    TJAPlayer3.fCamZoomFactor = currentCamZoomChip.fCamZoomEnd;
                }
            }

            if (ctCamRotation != null) //camera rotation
            {
                ctCamRotation.Tick();
                float value = 0.0f;
                if (currentCamRotateChip.strCamEaseType.Equals("IN")) value = easing.EaseIn(ctCamRotation, currentCamRotateChip.fCamRotationStart, currentCamRotateChip.fCamRotationEnd, currentCamRotateChip.fCamMoveType);
                if (currentCamRotateChip.strCamEaseType.Equals("OUT")) value = easing.EaseOut(ctCamRotation, currentCamRotateChip.fCamRotationStart, currentCamRotateChip.fCamRotationEnd, currentCamRotateChip.fCamMoveType);
                if (currentCamRotateChip.strCamEaseType.Equals("IN_OUT")) value = easing.EaseInOut(ctCamRotation, currentCamRotateChip.fCamRotationStart, currentCamRotateChip.fCamRotationEnd, currentCamRotateChip.fCamMoveType);
                TJAPlayer3.fCamRotation = float.IsNaN(value) ? currentCamRotateChip.fCamRotationStart : value;

                if (ctCamRotation.IsEnded)
                {
                    ctCamRotation = null;
                    TJAPlayer3.fCamRotation = currentCamRotateChip.fCamRotationEnd;
                }
            }

            if (ctCamVScale != null) //vertical camera scaling
            {
                ctCamVScale.Tick();
                float value = 0.0f;
                if (currentCamVScaleChip.strCamEaseType.Equals("IN")) value = easing.EaseIn(ctCamVScale, currentCamVScaleChip.fCamScaleStartY, currentCamVScaleChip.fCamScaleEndY, currentCamVScaleChip.fCamMoveType);
                if (currentCamVScaleChip.strCamEaseType.Equals("OUT")) value = easing.EaseOut(ctCamVScale, currentCamVScaleChip.fCamScaleStartY, currentCamVScaleChip.fCamScaleEndY, currentCamVScaleChip.fCamMoveType);
                if (currentCamVScaleChip.strCamEaseType.Equals("IN_OUT")) value = easing.EaseInOut(ctCamVScale, currentCamVScaleChip.fCamScaleStartY, currentCamVScaleChip.fCamScaleEndY, currentCamVScaleChip.fCamMoveType);
                TJAPlayer3.fCamYScale = float.IsNaN(value) ? currentCamVScaleChip.fCamScaleStartY : value;

                if (ctCamVScale.IsEnded)
                {
                    ctCamVScale = null;
                    TJAPlayer3.fCamYScale = currentCamVScaleChip.fCamScaleEndY;
                }
            }

            if (ctCamHScale != null) //horizontal camera scaling
            {
                ctCamHScale.Tick();
                float value = 0.0f;
                if (currentCamHScaleChip.strCamEaseType.Equals("IN")) value = easing.EaseIn(ctCamHScale, currentCamHScaleChip.fCamScaleStartX, currentCamHScaleChip.fCamScaleEndX, currentCamHScaleChip.fCamMoveType);
                if (currentCamHScaleChip.strCamEaseType.Equals("OUT")) value = easing.EaseOut(ctCamHScale, currentCamHScaleChip.fCamScaleStartX, currentCamHScaleChip.fCamScaleEndX, currentCamHScaleChip.fCamMoveType);
                if (currentCamHScaleChip.strCamEaseType.Equals("IN_OUT")) value = easing.EaseInOut(ctCamHScale, currentCamHScaleChip.fCamScaleStartX, currentCamHScaleChip.fCamScaleEndX, currentCamHScaleChip.fCamMoveType);
                TJAPlayer3.fCamXScale = float.IsNaN(value) ? currentCamHScaleChip.fCamScaleStartX : value;

                if (ctCamHScale.IsEnded)
                {
                    ctCamHScale = null;
                    TJAPlayer3.fCamXScale = currentCamHScaleChip.fCamScaleEndX;
                }
            }

            foreach (KeyValuePair<CDTX.CChip, CCounter> pair in objHandlers)
            {
                CDTX.CChip chip = pair.Key;
                CCounter counter = pair.Value;

                if (counter != null)
                {
                    counter.Tick();

                    float value = 0.0f;
                    if (counter.IsEnded)
                    {
                        value = chip.fObjEnd;
                        counter = null;
                    }
                    else
                    {
                        if (chip.strObjEaseType.Equals("IN")) value = easing.EaseIn(counter, chip.fObjStart, chip.fObjEnd, chip.objCalcType);
                        if (chip.strObjEaseType.Equals("OUT")) value = easing.EaseOut(counter, chip.fObjStart, chip.fObjEnd, chip.objCalcType);
                        if (chip.strObjEaseType.Equals("IN_OUT")) value = easing.EaseInOut(counter, chip.fObjStart, chip.fObjEnd, chip.objCalcType);
                        value = float.IsNaN(value) ? chip.fObjStart : value;
                    }

                    if (chip.nチャンネル番号 == 0xBE) chip.obj.y = value;
                    if (chip.nチャンネル番号 == 0xC0) chip.obj.x = value;
                    if (chip.nチャンネル番号 == 0xC2) chip.obj.yScale = value;
                    if (chip.nチャンネル番号 == 0xC4) chip.obj.xScale = value;
                    if (chip.nチャンネル番号 == 0xC6) chip.obj.rotation = value;
                    if (chip.nチャンネル番号 == 0xC8) chip.obj.opacity = (int)value;

                    if (chip.nチャンネル番号 == 0xCB) chip.obj.y = value;
                    if (chip.nチャンネル番号 == 0xCC) chip.obj.x = value;
                    if (chip.nチャンネル番号 == 0xCD) chip.obj.yScale = value;
                    if (chip.nチャンネル番号 == 0xCE) chip.obj.xScale = value;
                    if (chip.nチャンネル番号 == 0xCF) chip.obj.rotation = value;
                    if (chip.nチャンネル番号 == 0xD0) chip.obj.opacity = (int)value;
                }
            }
            #endregion

            return false;
		}

        protected bool t進行描画_チップ_連打( EInstrumentPad ePlayMode, int nPlayer )
		{
			if ( ( base.ePhaseID == CStage.EPhase.Game_STAGE_FAILED ) || ( base.ePhaseID == CStage.EPhase.Game_STAGE_FAILED_FadeOut ) )
			{
				return true;
			}
			if ( ( this.n現在のトップChip == -1 ) || ( this.n現在のトップChip >= listChip[ nPlayer ].Count ) )
			{
				return true;
			}

			CConfigIni configIni = TJAPlayer3.ConfigIni;

			CDTX dTX = TJAPlayer3.DTX;
            bool bAutoPlay = configIni.b太鼓パートAutoPlay[nPlayer];
            switch ( nPlayer ) //2017.08.11 kairera0467
            {
                case 1:
                    bAutoPlay = configIni.b太鼓パートAutoPlay[nPlayer] || TJAPlayer3.ConfigIni.bAIBattleMode;
                    dTX = TJAPlayer3.DTX_2P;
                    break;
                case 2:
                    dTX = TJAPlayer3.DTX_3P;
                    break;
                case 3:
                    dTX = TJAPlayer3.DTX_4P;
                    break;
                case 4:
                    dTX = TJAPlayer3.DTX_5P;
                    break;
                default:
                    break;
            }

            var n現在時刻ms = (long)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed);

            //for ( int nCurrentTopChip = this.n現在のトップChip; nCurrentTopChip < dTX.listChip.Count; nCurrentTopChip++ )
            for ( int nCurrentTopChip = dTX.listChip.Count - 1; nCurrentTopChip > 0; nCurrentTopChip-- )
			{
				CDTX.CChip pChip = dTX.listChip[ nCurrentTopChip ];

                if ( !pChip.bHit )
                {
                    bool bRollChip = NotesManager.IsGenericRoll(pChip);// pChip.nチャンネル番号 >= 0x15 && pChip.nチャンネル番号 <= 0x19;
                    if( bRollChip && ( ( pChip.e楽器パート != EInstrumentPad.UNKNOWN ) ) )
                    {
                        int instIndex = (int) pChip.e楽器パート;
                        if( pChip.nバーからの距離dot[instIndex] < -40 )
                        {
                            if ( this.e指定時刻からChipのJUDGEを返す( n現在時刻ms, pChip, nPlayer ) == ENoteJudge.Miss )
                            {
                                this.tチップのヒット処理( n現在時刻ms, pChip, EInstrumentPad.TAIKO, false, 0, nPlayer );
                            }
                        }
                    }
                }

				switch ( pChip.nチャンネル番号 )
				{
#region[ 15-19: Rolls ]
                    case 0x15: //連打
                    case 0x16: //連打(大)
                    case 0x17: //風船
                    case 0x18: //連打終了
                    case 0x19:
                    case 0x1D:
                    case 0x20:
                    case 0x21:
                        {
                            if( pChip.n描画優先度 >= 1 )
                                this.t進行描画_チップ_Taiko連打( configIni, ref dTX, ref pChip, nPlayer );
                        }
                        break;
#endregion
                }

            }
			return false;
		}

        public void tBranchReset(int player)
        {
            if (player != -1)
            {
                this.CBranchScore[player].cBigNotes.nGreat = 0;
                this.CBranchScore[player].cBigNotes.nGood = 0;
                this.CBranchScore[player].cBigNotes.nMiss = 0;
                this.CBranchScore[player].cBigNotes.nRoll = 0;

                this.CBranchScore[player].nGreat = 0;
                this.CBranchScore[player].nGood = 0;
                this.CBranchScore[player].nMiss = 0;
                this.CBranchScore[player].nRoll = 0;
            }
            else
            {
                for (int i = 0; i < CBranchScore.Length; i++)
                {
                    this.CBranchScore[i].cBigNotes.nGreat = 0;
                    this.CBranchScore[i].cBigNotes.nGood = 0;
                    this.CBranchScore[i].cBigNotes.nMiss = 0;
                    this.CBranchScore[i].cBigNotes.nRoll = 0;

                    this.CBranchScore[i].nGreat = 0;
                    this.CBranchScore[i].nGood = 0;
                    this.CBranchScore[i].nMiss = 0;
                    this.CBranchScore[i].nRoll = 0;
                }
            }
        }

        public void tBranchJudge(CDTX.CChip pChip, CBRANCHSCORE cBRANCHSCORE, int nスコア, int n連打数, int n良, int n可, int n不可, int nPlayer)
        {
            // Branch check score here 

            if (this.b強制的に分岐させた[nPlayer]) return;

            var e種類 = pChip.e分岐の種類;

            //分岐の仕方が同じなので一緒にしていいと思う。
            var b分岐種類が一致 = e種類 == CDTX.E分岐種類.e精度分岐 || e種類 == CDTX.E分岐種類.eスコア分岐;


            double dbRate = 0;

            if (e種類 == CDTX.E分岐種類.e精度分岐)
            {
                if ((n良 + n可 + n不可) != 0)
                {
                    dbRate = (((double)n良 + (double)n可 * 0.5)  / (double)(n良 + n可 + n不可)) * 100.0;
                }
            }
            else if (e種類 == CDTX.E分岐種類.eスコア分岐)
            {
                dbRate = nスコア; 
            }
            else if (e種類 == CDTX.E分岐種類.e連打分岐)
            {
                dbRate = n連打数;
            }
            else if (e種類 == CDTX.E分岐種類.e大音符のみ精度分岐)
            {
                dbRate = cBRANCHSCORE.nGreat;
            }


            if (b分岐種類が一致)
            {
                if (dbRate < pChip.n条件数値A)
                {
                    this.nレーン用表示コース[nPlayer] = CDTX.ECourse.eNormal;
                    this.n次回のコース[nPlayer] = CDTX.ECourse.eNormal;
                }
                else if (dbRate >= pChip.n条件数値A && dbRate < pChip.n条件数値B)
                {
                    this.nレーン用表示コース[nPlayer] = CDTX.ECourse.eExpert;
                    this.n次回のコース[nPlayer] = CDTX.ECourse.eExpert;
                }
                else if (dbRate >= pChip.n条件数値B)
                {
                    this.nレーン用表示コース[nPlayer] = CDTX.ECourse.eMaster;
                    this.n次回のコース[nPlayer] = CDTX.ECourse.eMaster;
                }

            }
            else if (e種類 == CDTX.E分岐種類.e連打分岐)
            {
                if (!(pChip.n条件数値A == 0 && pChip.n条件数値B == 0))
                {
                    if (dbRate < pChip.n条件数値A)
                    {
                        this.nレーン用表示コース[nPlayer] = CDTX.ECourse.eNormal;
                        this.n次回のコース[nPlayer] = CDTX.ECourse.eNormal;
                    }
                    else if (dbRate >= pChip.n条件数値A && dbRate < pChip.n条件数値B)
                    {
                        this.nレーン用表示コース[nPlayer] = CDTX.ECourse.eExpert;
                        this.n次回のコース[nPlayer] = CDTX.ECourse.eExpert;
                    }
                    else if (dbRate >= pChip.n条件数値B)
                    {
                        this.nレーン用表示コース[nPlayer] = CDTX.ECourse.eMaster;
                        this.n次回のコース[nPlayer] = CDTX.ECourse.eMaster;
                    }
                }
            }
            else if (e種類 == CDTX.E分岐種類.e大音符のみ精度分岐)
            {
                if (!(pChip.n条件数値A == 0 && pChip.n条件数値B == 0))
                {
                    if (dbRate < pChip.n条件数値A)
                    {
                        this.nレーン用表示コース[nPlayer] = CDTX.ECourse.eNormal;
                        this.n次回のコース[nPlayer] = CDTX.ECourse.eNormal;
                    }
                    else if (dbRate >= pChip.n条件数値A && dbRate < pChip.n条件数値B)
                    {
                        this.nレーン用表示コース[nPlayer] = CDTX.ECourse.eExpert;
                        this.n次回のコース[nPlayer] = CDTX.ECourse.eExpert;
                    }
                    else if (dbRate >= pChip.n条件数値B)
                    {
                        this.nレーン用表示コース[nPlayer] = CDTX.ECourse.eMaster;
                        this.n次回のコース[nPlayer] = CDTX.ECourse.eMaster;
                    }
                }
            }
        }

        private CDTX.ECourse[] E強制コース = new CDTX.ECourse[5];
        private void t強制用条件かを判断する(double db条件A, double db条件B, int nPlayer)
        {
            //Wiki参考
            //成仏

            if (db条件A == 101 && db条件B == 102) //強制普通譜面
            {
                E強制コース[nPlayer] = CDTX.ECourse.eNormal;
                this.b強制分岐譜面[nPlayer] = true;
            }
            else if (db条件A == -1 && db条件B == 101)  //強制玄人譜面
            {
                E強制コース[nPlayer] = CDTX.ECourse.eExpert;
                this.b強制分岐譜面[nPlayer] = true;
            }
            else if (db条件A == -2 && db条件B == -1)   //強制達人譜面
            {
                E強制コース[nPlayer] = CDTX.ECourse.eMaster;
                this.b強制分岐譜面[nPlayer] = true;
            }
        }

        public void t分岐処理(CDTX.ECourse n分岐先, int nPlayer, double n発声位置, CDTX.E分岐種類 e分岐種類 = CDTX.E分岐種類.e精度分岐)
        {

            CDTX dTX = TJAPlayer3.DTX;
            switch ( nPlayer )
            {
                case 1:
                    dTX = TJAPlayer3.DTX_2P;
                    break;
                case 2:
                    dTX = TJAPlayer3.DTX_3P;
                    break;
                case 3:
                    dTX = TJAPlayer3.DTX_4P;
                    break;
                case 4:
                    dTX = TJAPlayer3.DTX_5P;
                    break;
                default:
                    break;
            }


            for (int A = 0; A < dTX.listChip.Count; A++)
            {
                var Chip = dTX.listChip[A].nチャンネル番号;
                var _chip = dTX.listChip[A];

                var bDontDeleteFlag = NotesManager.IsHittableNote(_chip);// Chip >= 0x11 && Chip <= 0x19;
                var bRollAllFlag = NotesManager.IsGenericRoll(_chip);//Chip >= 0x15 && Chip <= 0x19;
                var bBalloonOnlyFlag = NotesManager.IsGenericBalloon(_chip);//Chip == 0x17;
                var bRollOnlyFlag = NotesManager.IsRoll(_chip);//Chip >= 0x15 && Chip <= 0x16;

                if (bDontDeleteFlag)
                {
                    if (dTX.listChip[A].n発声時刻ms > n発声位置)
                    {
                        if (dTX.listChip[A].nコース == n分岐先)
                        {
                            dTX.listChip[A].b可視 = true;

                            if (dTX.listChip[A].IsEndedBranching)
                            {
                                if (bRollAllFlag)//共通譜面時かつ、連打譜面だったら非可視化
                                {
                                    dTX.listChip[A].bHit = true;
                                    dTX.listChip[A].bShow = false;
                                    dTX.listChip[A].b可視 = false;
                                }
                            }
                        }
                        else
                        {
                            if (!dTX.listChip[A].IsEndedBranching)
                                dTX.listChip[A].b可視 = false;
                        }
                        //共通なため分岐させない.
                        dTX.listChip[A].eNoteState = ENoteState.none;

                        if (dTX.listChip[A].IsEndedBranching && (dTX.listChip[A].nコース == CDTX.ECourse.eNormal))
                        {
                            if (bRollOnlyFlag)//共通譜面時かつ、連打譜面だったら可視化
                            {
                                dTX.listChip[A].bHit = false;
                                dTX.listChip[A].bShow = true;
                                dTX.listChip[A].b可視 = true;
                            }
                            else
                            {
                                if (bBalloonOnlyFlag)//共通譜面時かつ、風船譜面だったら可視化
                                {
                                    dTX.listChip[A].bShow = true;
                                    dTX.listChip[A].b可視 = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        public int GetRoll(int player)
        {
            return n合計連打数[player];
        }

        protected float GetNowPBMTime( CDTX tja, float play_time )
        {
            float bpm_time = 0;
            int last_input = 0;
            float last_bpm_change_time;
            play_time = SoundManager.PlayTimer.NowTimeMs * (float)TJAPlayer3.ConfigIni.SongPlaybackSpeed - tja.nOFFSET;

            for (int i = 1; ; i++)
            {
                //BPMCHANGEの数越えた
                if( i >= tja.listBPM.Count )
                {
                    CDTX.CBPM cBPM = tja.listBPM[ last_input ];
                    bpm_time = (float)cBPM.bpm_change_bmscroll_time + ( ( play_time - (float)cBPM.bpm_change_time ) * (float)cBPM.dbBPM値 / 15000.0f );
                    last_bpm_change_time = (float)cBPM.bpm_change_time;
                    break;
                }
                for( ; i < tja.listBPM.Count; i++ )
                {
                    CDTX.CBPM cBPM = tja.listBPM[ i ];
                    if (cBPM.bpm_change_time == 0 || cBPM.bpm_change_course == this.n現在のコース[ 0 ] )
                    {
                        break;
                    }
                }
                if( i == tja.listBPM.Count )
                {
                    i = tja.listBPM.Count - 1;
                    continue;
                }

                if( play_time < tja.listBPM[ i ].bpm_change_time )
                {
                    CDTX.CBPM cBPM = tja.listBPM[ last_input ];
                    bpm_time = (float)cBPM.bpm_change_bmscroll_time + ( ( play_time - (float)cBPM.bpm_change_time ) * (float)cBPM.dbBPM値 / 15000.0f );
                    last_bpm_change_time = (float)cBPM.bpm_change_time;
                    break;
                }
                else
                {
                    last_input = i;
                }
            }

            return bpm_time;
        }

		public void t再読込()
		{
			TJAPlayer3.DTX.t全チップの再生停止とミキサーからの削除();
			this.eフェードアウト完了時の戻り値 = E演奏画面の戻り値.再読込_再演奏;
			base.ePhaseID = CStage.EPhase.Game_Reload;
			this.bPAUSE = false;
		}

        public void t演奏やりなおし()
        {
            _AIBattleState = 0;
            _AIBattleStateBatch = new Queue<float>[] { new Queue<float>(), new Queue<float>() };

            NowAIBattleSectionCount = 0;
            NowAIBattleSectionTime = 0;

            CFloorManagement.reload();

            for (int i = 0; i < AIBattleSections.Count; i++)
            {
                AIBattleSections[i].End = AIBattleSection.EndType.None;
                AIBattleSections[i].IsAnimated = false;
            }

            TJAPlayer3.fCamXOffset = 0;

            TJAPlayer3.fCamYOffset = 0;

            TJAPlayer3.fCamZoomFactor = 1.0f;
            TJAPlayer3.fCamRotation = 0;

            TJAPlayer3.fCamXScale = 1.0f;
            TJAPlayer3.fCamYScale = 1.0f;

            TJAPlayer3.borderColor = new Color4(1f, 0f, 0f, 0f);

            foreach (var chip in TJAPlayer3.DTX.listChip)
            {
                if (chip.obj == null) continue;
                chip.obj.isVisible = false;
                chip.obj.yScale = 1.0f;
                chip.obj.xScale = 1.0f;
                chip.obj.rotation = 0.0f;
                chip.obj.opacity = 255;
                chip.obj.frame = 0;
            }

            TJAPlayer3.DTX.t全チップの再生停止とミキサーからの削除();
            this.t数値の初期化( true, true );
			//this.actAVI.Stop();
            foreach(var vd in TJAPlayer3.DTX.listVD)
            {
                vd.Value.Stop();
            }
			this.actAVI.Stop();
            this.actPanel.t歌詞テクスチャを削除する();
            bool[] cleared = new bool[5];
            for (int i = 0; i < 5; i++)
            {
                cleared[i] = bIsAlreadyCleared[i];
                this.t演奏位置の変更(0, i);
                this.actPlayInfo.NowMeasure[i] = 0;
                JPOSCROLLX[i] = 0;
                JPOSCROLLY[i] = 0;
                ifp[i] = false;
                isDeniedPlaying[i] = false;

                TJAPlayer3.ConfigIni.nGameType[i] = eFirstGameType[i];
                bSplitLane[i] = false;
            }
            TJAPlayer3.stage演奏ドラム画面.Activate();
            for( int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++ )
            {
                if (!bIsAlreadyCleared[i] && cleared[i])
                {
                    TJAPlayer3.stage演奏ドラム画面.actBackground.ClearOut(i);
                }
                
                if (NotesManager.IsKusudama(this.chip現在処理中の連打チップ[ i ]) && this.actChara.b風船連打中[i]) actBalloon.KusuMiss();
                this.chip現在処理中の連打チップ[ i ] = null;
                this.actChara.b風船連打中[i] = false;
                this.actChara.ReturnDefaultAnime(i, true);
            }
            this.bPAUSE = false;
        }

		public void t停止()
		{
			TJAPlayer3.DTX.t全チップの再生停止とミキサーからの削除();
            foreach(var vd in TJAPlayer3.DTX.listVD)
            {
                vd.Value.Stop();
            }
			this.actAVI.Stop();
			this.actPanel.Stop();				// PANEL表示停止
			TJAPlayer3.Timer.Pause();		// 再生時刻カウンタ停止

			this.n現在のトップChip = TJAPlayer3.DTX.listChip.Count - 1;	// 終端にシーク

			// 自分自身のOn活性化()相当の処理もすべき。
		}

        public void t数値の初期化( bool b演奏記録, bool b演奏状態 )
        {
            if( b演奏記録 )
            {
                this.nヒット数_Auto含む.Taiko.Perfect = 0;
                this.nヒット数_Auto含む.Taiko.Great = 0;
                this.nヒット数_Auto含む.Taiko.Good = 0;
                this.nヒット数_Auto含む.Taiko.Poor = 0;
                this.nヒット数_Auto含む.Taiko.Miss = 0;

                this.nヒット数_Auto含まない.Taiko.Perfect = 0;
                this.nヒット数_Auto含まない.Taiko.Great = 0;
                this.nヒット数_Auto含まない.Taiko.Good = 0;
                this.nヒット数_Auto含まない.Taiko.Poor = 0;
                this.nヒット数_Auto含まない.Taiko.Miss = 0;

                this.actCombo.Activate();
                this.actScore.Activate();
                for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
                    this.actGauge.Init(TJAPlayer3.ConfigIni.nRisky, i);
                }
            }
            if( b演奏状態 )
            {
                for( int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++ )
                {
                    this.bIsGOGOTIME[ i ] = false;
                    this.bIsMiss[i] = false;
                    this.bLEVELHOLD[ i ] = false;
                    this.b強制的に分岐させた[ i ] = false;
                    this.b譜面分岐中[ i ] = false;
                    this.b連打中[ i ] = false;
                    this.n現在のコース[ i ] = 0;
                    this.n次回のコース[ i] = 0;
                    this.n現在の連打数[ i ] = 0;
                    this.n合計連打数[ i ] = 0;
                    this.n分岐した回数[ i ] = 0;
                }
                for (int i = 0; i < 5; i++)
                {
                    this.actComboVoice.tReset(i);
                    NowProcessingChip[i] = 0;
                }
            }
            nCurrentKusudamaCount = 0;
            nCurrentKusudamaRollCount = 0;

            this.ReSetScore(TJAPlayer3.DTX.nScoreInit[0, TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0]], TJAPlayer3.DTX.nScoreDiff[TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0]]);
            this.nHand = new int[]{ 0, 0, 0, 0, 0 };
        }

		public void t演奏位置の変更( int nStartBar, int nPlayer )
		{
			// まず全サウンドオフにする
			TJAPlayer3.DTX.t全チップの再生停止();
			this.actAVI.Stop();
            CDTX dTX = TJAPlayer3.DTX;
            switch (nPlayer)
            {
                case 1:
                    dTX = TJAPlayer3.DTX_2P;
                    break;
                case 2:
                    dTX = TJAPlayer3.DTX_3P;
                    break;
                case 3:
                    dTX = TJAPlayer3.DTX_4P;
                    break;
                case 4:
                    dTX = TJAPlayer3.DTX_5P;
                    break;
                default:
                    break;
            }

            if (dTX == null) return; //CDTXがnullの場合はプレイヤーが居ないのでその場で処理終了


#region [ 再生開始小節の変更 ]
            //nStartBar++;									// +1が必要

#region [ 演奏済みフラグのついたChipをリセットする ]
            for ( int i = 0; i < dTX.listChip.Count; i++ )
			{
                //if(dTX.listChip[i].bHit) フラグが付いてなくてもすべてのチップをリセットする。(必要がある).2020.04.23.akasoko26

                dTX.listChip[i].bHit = false;
                dTX.listChip[i].bShow = true;
                dTX.listChip[i].bProcessed = false;
                dTX.listChip[i].b可視 = true;
                dTX.listChip[i].IsHitted = false;
                dTX.listChip[i].IsMissed = false;
                dTX.listChip[i].eNoteState = ENoteState.none;
                dTX.listChip[i].nProcessTime = 0;
                dTX.listChip[i].nRollCount = 0;
                dTX.listChip[i].nRollCount = 0;
            }
#endregion

#region [ 処理を開始するチップの特定 ]
			//for ( int i = this.n現在のトップChip; i < CDTXMania.DTX.listChip.Count; i++ )
			bool bSuccessSeek = false;
            for (int i = 0; i < dTX.listChip.Count; i++)
            {
                CDTX.CChip pChip = dTX.listChip[i];
                if (nStartBar == 0)
                {
                    if (pChip.n発声位置 < 384 * nStartBar)
                    {
                        continue;
                    }
                    else
                    {
                        bSuccessSeek = true;
                        this.n現在のトップChip = i;
                        break;
                    }
                }
				else
                {
                    if (pChip.nチャンネル番号 == 0x50 && pChip.n整数値_内部番号 > nStartBar - 1)
                    {
                        bSuccessSeek = true;
                        this.n現在のトップChip = i;
                        break;
                    }
                }
			}
            if (!bSuccessSeek)
            {
                // this.n現在のトップChip = CDTXMania.DTX.listChip.Count - 1;
                this.n現在のトップChip = 0;		// 対象小節が存在しないなら、最初から再生
			}
            else
            {
                while (this.n現在のトップChip != 0 && dTX.listChip[this.n現在のトップChip].n発声時刻ms == dTX.listChip[TJAPlayer3.stage演奏ドラム画面.n現在のトップChip - 1].n発声時刻ms)
                    TJAPlayer3.stage演奏ドラム画面.n現在のトップChip--;
            }
#endregion
#region [ 演奏開始の発声時刻msを取得し、タイマに設定 ]
            int nStartTime = (int)(dTX.listChip[this.n現在のトップChip].n発声時刻ms / TJAPlayer3.ConfigIni.SongPlaybackSpeed);

            SoundManager.PlayTimer.Reset();	// これでPAUSE解除されるので、次のPAUSEチェックは不要
			//if ( !this.bPAUSE )
			//{
				SoundManager.PlayTimer.Pause();
			//}
			SoundManager.PlayTimer.NowTime = nStartTime;
#endregion

			List<CSound> pausedCSound = new List<CSound>();

#region [ BGMやギターなど、演奏開始のタイミングで再生がかかっているサウンドのの途中再生開始 ] // (CDTXのt入力_行解析_チップ配置()で小節番号が+1されているのを削っておくこと)
			for ( int i = this.n現在のトップChip; i >= 0; i-- )
			{
				CDTX.CChip pChip = dTX.listChip[ i ];
				int nDuration = pChip.GetDuration();
                long n発声時刻ms = (long)(pChip.n発声時刻ms / TJAPlayer3.ConfigIni.SongPlaybackSpeed);

                if ((n発声時刻ms + nDuration > 0) && (n発声時刻ms <= nStartTime) && (nStartTime <= n発声時刻ms + nDuration))
                {
                    if (pChip.nチャンネル番号 == 0x01 && (pChip.nチャンネル番号 >> 4) != 0xB) // wav系チャンネル、且つ、空打ちチップではない
                    {
                        CDTX.CWAV wc;
						bool b = dTX.listWAV.TryGetValue( pChip.n整数値_内部番号, out wc );
						if ( !b ) continue;

						if ( ( wc.bIsBGMSound && TJAPlayer3.ConfigIni.bBGM音を発声する ) || ( !wc.bIsBGMSound ) )
						{
                            TJAPlayer3.DTX.tチップの再生(pChip, (long)(SoundManager.PlayTimer.PrevResetTime) + (long)(pChip.n発声時刻ms / TJAPlayer3.ConfigIni.SongPlaybackSpeed));
#region [ PAUSEする ]
                            int j = wc.n現在再生中のサウンド番号;
							if ( wc.rSound[ j ] != null )
							{
							    wc.rSound[ j ].Pause();
                                wc.rSound[j].tSetPositonToBegin(nStartTime - n発声時刻ms);
                                pausedCSound.Add( wc.rSound[ j ] );
							}
#endregion
						}
					}
				}
			}
#endregion
#region [ 演奏開始時点で既に表示されているBGAとAVIの、シークと再生 ]
			if (dTX.listVD.Count > 0)
            {
                for (int i = 0; i < dTX.listChip.Count; i++)
                {
                    if (dTX.listChip[i].nチャンネル番号 == 0x54)
                    {
                        if (dTX.listChip[i].n発声時刻ms <= nStartTime)
                        {
                            this.actAVI.Seek(nStartTime - dTX.listChip[i].n発声時刻ms);
                            this.actAVI.Start(0x54, this.actAVI.rVD);
                            break;
                        }
                        else
                        {
                            this.actAVI.Seek(0);
                        }
                        break;
                    } 
                }
            }
#endregion
#region [ PAUSEしていたサウンドを一斉に再生再開する(ただしタイマを止めているので、ここではまだ再生開始しない) ]

            if (!(TJAPlayer3.ConfigIni.b演奏速度が一倍速であるとき以外音声を再生しない && TJAPlayer3.ConfigIni.n演奏速度 != 20))
                foreach (CSound cs in pausedCSound)
                {
                    cs.tPlaySound();
                }
#endregion
            pausedCSound.Clear();
#region [ タイマを再開して、PAUSEから復帰する ]
            SoundManager.PlayTimer.NowTime = nStartTime;
			TJAPlayer3.Timer.Reset();						// これでPAUSE解除されるので、3行先の再開()は不要
			TJAPlayer3.Timer.NowTime = nStartTime;				// Debug表示のTime: 表記を正しくするために必要
			SoundManager.PlayTimer.Resume();
			//CDTXMania.Timer.t再開();
			this.bPAUSE = false;								// システムがPAUSE状態だったら、強制解除
			this.actPanel.Start();
#endregion
#endregion
		}

        public void t演奏中止()
        {
            this.actFO.tフェードアウト開始();
            base.ePhaseID = CStage.EPhase.Common_FADEOUT;
            this.eフェードアウト完了時の戻り値 = E演奏画面の戻り値.演奏中断;
        }

		/// <summary>
		/// DTXV用の設定をする。(全AUTOなど)
		/// 元の設定のバックアップなどはしないので、あとでConfig.iniを上書き保存しないこと。
		/// </summary>
		protected void tDTXV用の設定()
		{
			TJAPlayer3.ConfigIni.bAutoPlay.HH = true;
			TJAPlayer3.ConfigIni.bAutoPlay.SD = true;
			TJAPlayer3.ConfigIni.bAutoPlay.BD = true;
			TJAPlayer3.ConfigIni.bAutoPlay.HT = true;
			TJAPlayer3.ConfigIni.bAutoPlay.LT = true;
			TJAPlayer3.ConfigIni.bAutoPlay.CY = true;
			TJAPlayer3.ConfigIni.bAutoPlay.FT = true;
			TJAPlayer3.ConfigIni.bAutoPlay.RD = true;
			TJAPlayer3.ConfigIni.bAutoPlay.LC = true;
            TJAPlayer3.ConfigIni.bAutoPlay.LP = true;
            TJAPlayer3.ConfigIni.bAutoPlay.LBD = true;
			TJAPlayer3.ConfigIni.bAutoPlay.GtR = true;
			TJAPlayer3.ConfigIni.bAutoPlay.GtB = true;
			TJAPlayer3.ConfigIni.bAutoPlay.GtB = true;
			TJAPlayer3.ConfigIni.bAutoPlay.GtPick = true;
			TJAPlayer3.ConfigIni.bAutoPlay.GtW = true;
			TJAPlayer3.ConfigIni.bAutoPlay.BsR = true;
			TJAPlayer3.ConfigIni.bAutoPlay.BsB = true;
			TJAPlayer3.ConfigIni.bAutoPlay.BsB = true;
			TJAPlayer3.ConfigIni.bAutoPlay.BsPick = true;
			TJAPlayer3.ConfigIni.bAutoPlay.BsW = true;

			this.bIsAutoPlay = TJAPlayer3.ConfigIni.bAutoPlay;

			TJAPlayer3.ConfigIni.bAVI有効 = true;
			TJAPlayer3.ConfigIni.bBGA有効 = true;
			for ( int i = 0; i < 3; i++ )
			{
				TJAPlayer3.ConfigIni.bGraph[ i ] = false;
				TJAPlayer3.ConfigIni.bHidden[ i ] = false;
				TJAPlayer3.ConfigIni.bLeft[ i ] = false;
				TJAPlayer3.ConfigIni.bLight[ i ] = false;
				TJAPlayer3.ConfigIni.bReverse[ i ] = false;
				TJAPlayer3.ConfigIni.bSudden[ i ] = false;
				TJAPlayer3.ConfigIni.eInvisible[ i ] = EInvisible.OFF;
				TJAPlayer3.ConfigIni.eRandom[ i ] = Eランダムモード.OFF;
				TJAPlayer3.ConfigIni.n表示可能な最小コンボ数[ i ] = 65535;
				TJAPlayer3.ConfigIni.判定文字表示位置[ i ] = E判定文字表示位置.表示OFF;
				// CDTXMania.ConfigIni.n譜面スクロール速度[ i ] = CDTXMania.ConfigIni.nViewerScrollSpeed[ i ];	// これだけはOn活性化()で行うこと。
																												// そうしないと、演奏開始直後にスクロール速度が変化して見苦しい。
			}

			TJAPlayer3.ConfigIni.eDark = Eダークモード.OFF;

			TJAPlayer3.ConfigIni.b演奏情報を表示する = TJAPlayer3.ConfigIni.bViewerShowDebugStatus;
			TJAPlayer3.ConfigIni.bScoreIniを出力する = false;
			TJAPlayer3.ConfigIni.bSTAGEFAILED有効 = false;
			TJAPlayer3.ConfigIni.bTight = false;
			TJAPlayer3.ConfigIni.bストイックモード = false;

			TJAPlayer3.ConfigIni.nRisky = 0;
		}

		protected abstract void t進行描画_チップ_ドラムス( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip );
		protected abstract void t進行描画_チップ本体_ドラムス( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip );
		protected abstract void t進行描画_チップ_Taiko( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip, int nPlayer );
		protected abstract void t進行描画_チップ_Taiko連打( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip, int nPlayer );

		protected abstract void t進行描画_チップ_フィルイン( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip );
		protected abstract void t進行描画_チップ_小節線( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip, int nPlayer );
		protected void t進行描画_チップアニメ()
		{
            for (int i = 0; i < 5; i++)
            {
                ctChipAnime[i].TickLoopDB();
                ctChipAnimeLag[i].Tick();
            }
        }

		protected bool t進行描画_フェードイン_アウト()
		{
			switch ( base.ePhaseID )
			{
				case CStage.EPhase.Common_FADEIN:
					if ( this.actFI.Draw() != 0)
					{
						base.ePhaseID = CStage.EPhase.Common_NORMAL;
					}
					break;

				case CStage.EPhase.Common_FADEOUT:
				case CStage.EPhase.Game_STAGE_FAILED_FadeOut:
					if ( this.actFO.Draw() != 0 )
					{
						return true;
					}
					break;

				case CStage.EPhase.Game_STAGE_CLEAR_FadeOut:
					if ( this.actFOClear.Draw() == 0 )
					{
						break;
					}
					return true;
		
			}
			return false;
		}

		protected abstract void t進行描画_演奏情報();
		protected void t進行描画_演奏情報(int x, int y)
		{
			if ( !TJAPlayer3.ConfigIni.b演奏情報を表示しない )
			{
				this.actPlayInfo.t進行描画( x, y );
			}
		}
		protected void t進行描画_背景()
		{
			if ( this.tx背景 != null )
			{
				this.tx背景.t2D描画( 0, 0 );
			}
		}

		protected void t進行描画_判定文字列1_通常位置指定の場合()
		{
			if ( ( (E判定文字表示位置) TJAPlayer3.ConfigIni.判定文字表示位置.Drums ) != E判定文字表示位置.コンボ下 )    // 判定ライン上または横
            {
                this.actJudgeString.Draw();
            }
		}

		protected void t進行描画_譜面スクロール速度()
		{
			this.act譜面スクロール速度.Draw();
		}
        protected abstract void t紙吹雪_開始();
		protected abstract void t背景テクスチャの生成();
		protected void t背景テクスチャの生成( string DefaultBgFilename, Rectangle bgrect, string bgfilename )
		{
            try
            {
                if( !String.IsNullOrEmpty( bgfilename ) )
                    this.tx背景 = TJAPlayer3.tテクスチャの生成( TJAPlayer3.stageSongSelect.r確定されたスコア.ファイル情報.フォルダの絶対パス + bgfilename );
                else
                    this.tx背景 = TJAPlayer3.tテクスチャの生成( CSkin.Path( DefaultBgFilename ) );
            }
            catch (Exception e)
            {
                Trace.TraceError( e.ToString() );
                Trace.TraceError( "例外が発生しましたが処理を継続します。 (a80767e1-4de7-4fec-b072-d078b3659e62)" );
                this.tx背景 = null;
            }
		}

        private int nDice = 0;

        public ENoteJudge AlterJudgement(int player, ENoteJudge judgement, bool reroll)
        {
            int AILevel = TJAPlayer3.ConfigIni.nAILevel;
            if (TJAPlayer3.ConfigIni.bAIBattleMode && player == 1)
            {
                if (reroll)
                    nDice = TJAPlayer3.Random.Next(1000);

                if (nDice < TJAPlayer3.ConfigIni.apAIPerformances[AILevel - 1].nBadOdds)
                    return ENoteJudge.Poor;
                else if (nDice - TJAPlayer3.ConfigIni.apAIPerformances[AILevel - 1].nBadOdds
                    < TJAPlayer3.ConfigIni.apAIPerformances[AILevel - 1].nGoodOdds)
                    return ENoteJudge.Good;
            }
            return judgement;
        }

        public void ReSetScore(int scoreInit, int scoreDiff)
        {
            //一打目の処理落ちがひどいので、あらかじめここで点数の計算をしておく。
            // -1だった場合、その前を引き継ぐ。
            int nInit = scoreInit != -1 ? scoreInit : this.nScore[0];
            int nDiff = scoreDiff != -1 ? scoreDiff : this.nScore[1] - this.nScore[0];
            int nAddScore = 0;
            int[] n倍率 = { 0, 1, 2, 4, 8 };

            if( TJAPlayer3.DTX.nScoreModeTmp == 1 )
            {
                for( int i = 0; i < 11; i++ )
                {
                    this.nScore[ i ] = (int)( nInit + ( nDiff * ( i ) ) );
                }
            }
            else if( TJAPlayer3.DTX.nScoreModeTmp == 2 )
            {
                for( int i = 0; i < 5; i++ )
                {
                    this.nScore[ i ] = (int)( nInit + ( nDiff * n倍率[ i ] ) );

                    this.nScore[ i ] = (int)( this.nScore[ i ] / 10.0 );
                    this.nScore[ i ] = this.nScore[ i ] * 10;

                }
            }
        }


        #region [EXTENDED COMMANDS]
        private CCounter ctCamVMove;
        private CCounter ctCamHMove;
        private CCounter ctCamZoom;
        private CCounter ctCamRotation;
        private CCounter ctCamVScale;
        private CCounter ctCamHScale;

        private CDTX.CChip currentCamVMoveChip;
        private CDTX.CChip currentCamHMoveChip;
        private CDTX.CChip currentCamZoomChip;
        private CDTX.CChip currentCamRotateChip;
        private CDTX.CChip currentCamVScaleChip;
        private CDTX.CChip currentCamHScaleChip;

        private Dictionary<CDTX.CChip, CCounter> camHandlers;
        private Dictionary<CDTX.CChip, CCounter> objHandlers;

        private Easing easing = new Easing();

        public bool bCustomDoron = false;
        private bool bConfigUpdated = false;
        #endregion
    }
}
