using FDK;

namespace OpenTaiko;

/// <summary>
/// CAct演奏Drumsゲージ と CAct演奏Gutiarゲージ のbaseクラス。ダメージ計算やDanger/Failed判断もこのクラスで行う。
///
/// 課題
/// _STAGE FAILED OFF時にゲージ回復を止める
/// _黒→閉店までの差を大きくする。
/// </summary>
internal class CActPlayGaugeCommon : CActivity {
	// Properties
	public CActLVLNFont actLVLNFont { get; protected set; }

	// コンストラクタ
	public CActPlayGaugeCommon() {
		//actLVLNFont = new CActLVLNFont();		// On活性化()に移動
		//actLVLNFont.On活性化();
	}

	// CActivity 実装

	public override void Activate() {
		for (int i = 0; i < 3; i++) {
			for (int n = 0; n < 3; n++) {
				dbGaugeIncreaseAmount_Branch[i, n] = new float[5];
			}
		}
		for (int i = 0; i < this.DTX.Length; ++i)
			this.DTX[i] = OpenTaiko.GetTJA(i)!;
		actLVLNFont = new CActLVLNFont();
		actLVLNFont.Activate();
		base.Activate();
	}
	public override void DeActivate() {
		actLVLNFont.DeActivate();
		actLVLNFont = null;
		base.DeActivate();
	}

	const double GAUGE_MAX = 100.0;
	const double GAUGE_INITIAL = 2.0 / 3;
	const double GAUGE_MIN = -0.1;
	const double GAUGE_ZERO = 0.0;
	const double GAUGE_DANGER = 0.3;

	public bool bRisky                          // Riskyモードか否か
	{
		get;
		private set;
	}
	public int nRiskyTimes_Initial              // Risky初期値
	{
		get;
		private set;
	}
	public int[] nRiskyTimes                    // 残Miss回数
	{
		get;
		private set;
	} = new int [OpenTaiko.MAX_PLAYERS];

	public bool IsRiskyMine(int iPlayer) => this.DTX[iPlayer].boomRule is CTja.EBoomRule.Fatal;
	public int[] timesRiskyMine
	{
		get;
		private set;
	} = new int[OpenTaiko.MAX_PLAYERS];

	public bool IsRiskyFailed() {
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i)
			if (!IsRiskyFailed(i))
				return false;
		return true;
	}
	public bool IsRiskyFailed(int iPlayer) => bRisky && nRiskyTimes[iPlayer] <= 0;   // 閉店状態になったかどうか
	public bool IsRiskyDanger(int iPlayer) => bRisky && nRiskyTimes_Initial switch {  // DANGERかどうか
		1 => false,
		2 or 3 => (nRiskyTimes[iPlayer] <= 1),
		_ => (nRiskyTimes[iPlayer] <= 2),
	};

	public bool IsRiskyMineFailed(int iPlayer) => IsRiskyMine(iPlayer) && timesRiskyMine[iPlayer] <= 0;

	/// <summary>
	/// ゲージの初期化
	/// </summary>
	/// <param name="nRiskyTimes_Initial_">Riskyの初期値(0でRisky未使用)</param>
	public void Init(int nRiskyTimes_InitialVal, int nPlayer)       // ゲージ初期化
	{
		//ダメージ値の計算
		switch (HGaugeMethods.tGetGaugeTypeEnum(nPlayer)) {
			default:
			case HGaugeMethods.EGaugeType.NORMAL:
				this.dbCurrentGaugeValue[nPlayer] = 0;
				break;
			case HGaugeMethods.EGaugeType.HARD:
			case HGaugeMethods.EGaugeType.EXTREME:
				this.dbCurrentGaugeValue[nPlayer] = 100;
				break;
		}

		//ゲージのMAXまでの最低コンボ数を計算
		float[] dbGaugeMaxComboValue_branch = new float[3];


		this.bRisky = (nRiskyTimes_InitialVal > 0);
		if (bRisky) {
			this.nRiskyTimes[nPlayer] = OpenTaiko.ConfigIni.nRisky;
			this.nRiskyTimes_Initial = OpenTaiko.ConfigIni.nRisky;
		}

		if (this.IsRiskyMine(nPlayer))
			this.timesRiskyMine[nPlayer] = Math.Max(1, (int)this.DTX[nPlayer].boomRuleValue);

		float gaugeRate = 0f;
		float dbDamageRate = 2.0f;

		int nanidou = this.DTX[nPlayer].nInstanceDifficulty;

		switch (this.DTX[nPlayer].PlayerSideMetadata.LEVELtaiko) {
			case 0:
			case 1:
			case 2:
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
				gaugeRate = this.fGaugeMaxRate[0];
				dbDamageRate = 0.625f;
				break;


			case 8:
				gaugeRate = this.fGaugeMaxRate[1];
				dbDamageRate = 0.625f;
				break;

			case 9:
			case 10:
			default:
				gaugeRate = this.fGaugeMaxRate[2];
				dbDamageRate = 2.0f;
				break;
		}

		#region [(Unbloated) Gauge max combo values]

		for (int i = 0; i < 3; i++) {
			dbGaugeMaxComboValue_branch[i] = this.DTX[nPlayer].nNotesCount_Branch[i] * (gaugeRate / 100.0f);
		}

		#endregion

		float multiplicationFactor = 1f;
		if (nanidou == (int)Difficulty.Tower)
			multiplicationFactor = 0f;

		double[] nGaugeRankValue_branch = new double[] { 0D, 0D, 0D };

		for (int i = 0; i < 3; i++) {
			nGaugeRankValue_branch[i] = (10000.0f / dbGaugeMaxComboValue_branch[i]) * multiplicationFactor;
		}

		//ゲージ値計算
		//実機に近い計算

		//2015.03.26 kairera0467 計算を初期化時にするよう修正。

		#region [ Handling infinity cases ]
		float fIsDontInfinty = 0.4f;//適当に0.4で
		float[] fAddVolume = new float[] { 1.0f, 0.5f, -Math.Abs(dbDamageRate) };

		for (int ib = 0; ib < 3; ++ib) {
			if (!double.IsInfinity(nGaugeRankValue_branch[ib] / 100.0f)) { //値がInfintyかチェック
				fIsDontInfinty = (float)(nGaugeRankValue_branch[ib] / 100.0f);
				for (int ij = 0; ij < 3; ++ij) {
					this.dbGaugeIncreaseAmount_Branch[ib, ij][nPlayer] = fIsDontInfinty * fAddVolume[ij];
				}
			} else {
				for (int ij = 0; ij < 3; ++ij) {
					// Handling infinity cases
					//Infintyだった場合はInfintyではない値 * 3.0をしてその値を利用する。
					this.dbGaugeIncreaseAmount_Branch[ib, ij][nPlayer] = (fIsDontInfinty * fAddVolume[ij]) * 3f;
				}
			}
		}
		#endregion

		#region [Rounding process]
		Func<float, float>? gaugeRoundFunc = this.DTX[nPlayer].GaugeIncreaseMode switch {
			GaugeIncreaseMode.Normal or GaugeIncreaseMode.Floor => MathF.Truncate, // 切り捨て
			GaugeIncreaseMode.Round => MathF.Round, // 四捨五入
			GaugeIncreaseMode.Ceiling => MathF.Ceiling, // 切り上げ
			GaugeIncreaseMode.NotFix or _ => null, // 丸めない
		};
		if (gaugeRoundFunc != null) {
			for (int ib = 0; ib < 3; ++ib) {
				for (int ij = 0; ij < 3; ++ij)
					dbGaugeIncreaseAmount_Branch[ib, ij][nPlayer] = gaugeRoundFunc(dbGaugeIncreaseAmount_Branch[ib, ij][nPlayer] * 10000.0f) / 10000.0f;
			}
		}

		float gaugeFillRatio = HGaugeMethods.tGetGaugeTypeEnum(nPlayer) switch {
			HGaugeMethods.EGaugeType.HARD => HGaugeMethods.HardGaugeFillRatio,
			HGaugeMethods.EGaugeType.EXTREME => HGaugeMethods.ExtremeGaugeFillRatio,
			HGaugeMethods.EGaugeType.NORMAL or _ => 1.0f,
		};
		if (gaugeFillRatio != 1) {
			for (int ib = 0; ib < 3; ++ib) {
				for (int ij = 0; ij < 3; ++ij)
					dbGaugeIncreaseAmount_Branch[ib, ij][nPlayer] *= gaugeFillRatio;
			}
		}
		#endregion
	}

	#region [ DAMAGE ]
#if true       // DAMAGELEVELTUNING
	#region [ DAMAGELEVELTUNING ]
	// ----------------------------------
	public float[,] fDamageGaugeDelta = {			// #23625 2011.1.10 ickw_284: tuned damage/recover factors
		// drums,   guitar,  bass
		{  0.004f,  0.006f,  0.006f,  0.004f },
		{  0.002f,  0.003f,  0.003f,  0.002f },
		{  0.000f,  0.000f,  0.000f,  0.000f },
		{ -0.020f, -0.030f, -0.030f, -0.020f },
		{ -0.050f, -0.050f, -0.050f, -0.050f }
	};
	public float[] fDamageLevelFactor = {
		0.5f, 1.0f, 1.5f
	};

	//譜面レベル, 判定
	public float[,][] dbGaugeIncreaseAmount_Branch = new float[3, 3][];


	public float[] fGaugeMaxRate =
	{
		70.7f, // 1～7
		70f,   // 8
		75.0f, // 9～10
		78.5f, // 11
		80.5f, // 12
		82f,   // 13+
	};//おおよその値。

	// ----------------------------------
	#endregion
#endif

	public void MineDamage(int nPlayer, CTja.ECourse? chipBranch = null) {
		CTja tja = this.DTX[nPlayer];
		int iBranch = (int)(chipBranch ?? OpenTaiko.stageGameScreen.nCurrentBranch[nPlayer]);

		float fDamage;
		switch (tja.boomRule) {
			default:
			case CTja.EBoomRule.Scal:
				fDamage = -Math.Abs(tja.boomRuleValue);
				break;
			case CTja.EBoomRule.Ratio:
				fDamage = -Math.Abs(tja.boomRuleValue * this.dbGaugeIncreaseAmount_Branch[iBranch, 0][nPlayer]);
				break;
			case CTja.EBoomRule.Fatal:
				fDamage = 0; // or use another default value?
				this.timesRiskyMine[nPlayer]--;
				break;
		}

		if (this.bRisky)
			this.nRiskyTimes[nPlayer]--;

		this.Damage(nPlayer, fDamage);
	}

	public void Damage(EKeyConfigPart screenmode, ENoteJudge eThisTimeJudge, int nPlayer, CTja.ECourse? chipBranch = null) {
		float fDamage;
		int nCourse = (int)(chipBranch ?? OpenTaiko.stageGameScreen.nCurrentBranch[nPlayer]);

		switch (eThisTimeJudge) {
			case ENoteJudge.Perfect:
			case ENoteJudge.Great:
				fDamage = this.dbGaugeIncreaseAmount_Branch[nCourse, 0][nPlayer];
				break;
			case ENoteJudge.Good:
				fDamage = this.dbGaugeIncreaseAmount_Branch[nCourse, 1][nPlayer];
				break;
			case ENoteJudge.Poor:
			case ENoteJudge.Miss: {
					fDamage = this.dbGaugeIncreaseAmount_Branch[nCourse, 2][nPlayer];

					int level = this.DTX[nPlayer].PlayerSideMetadata.LEVELtaiko;
					int nanidou = this.DTX[nPlayer].nInstanceDifficulty;

					switch (HGaugeMethods.tGetGaugeTypeEnum(nPlayer)) {
						case HGaugeMethods.EGaugeType.HARD:
							fDamage = -HGaugeMethods.tHardGaugeGetDamage((Difficulty)nanidou, level);
							break;
						case HGaugeMethods.EGaugeType.EXTREME:
							fDamage = -HGaugeMethods.tExtremeGaugeGetDamage((Difficulty)nanidou, level);
							break;
					}

					if (this.bRisky) {
						this.nRiskyTimes[nPlayer]--;
					}
				}

				break;



			default:
				fDamage = this.dbGaugeIncreaseAmount_Branch[nCourse, 0][nPlayer];
				break;
		}

		this.Damage(nPlayer, fDamage);
	}

	public void Damage(int nPlayer, float fDamage) {
		this.dbCurrentGaugeValue[nPlayer] = Math.Round(this.dbCurrentGaugeValue[nPlayer] + fDamage, 5, MidpointRounding.ToEven);

		if (this.dbCurrentGaugeValue[nPlayer] >= 100.0)
			this.dbCurrentGaugeValue[nPlayer] = 100.0;
		else if (this.dbCurrentGaugeValue[nPlayer] <= 0.0)
			this.dbCurrentGaugeValue[nPlayer] = 0.0;


		//CDTXMania.stage演奏ドラム画面.nGauge = fDamage;

	}

	public virtual void Start(NotesManager.ENoteType nLane, EGameType gameType, ENoteJudge judge, int player) {
	}

	//-----------------
	#endregion

	private CTja[] DTX = new CTja[OpenTaiko.MAX_PLAYERS];
	public double[] dbCurrentGaugeValue = new double[5];
	protected CCounter ctFlame;
	protected CCounter ctRainbowAnime;
	protected CCounter ctRainbowOpacity;
	protected CTexture[] txGaugeRainbow = new CTexture[12];
	protected CTexture[] txGaugeRainbow2P = new CTexture[12];
}
