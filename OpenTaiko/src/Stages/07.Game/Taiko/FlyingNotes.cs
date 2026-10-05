using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

internal class FlyingNotes : CActivity {
	// Constructor

	public FlyingNotes() {
		base.IsDeActivated = true;
	}


	// メソッド
	public virtual void Start(NotesManager.ENoteType nLane, EGameType gameType, int nPlayer, bool? forceFirework = null) {
		if (!Flies(OpenTaiko.ConfigIni, nLane) || nPlayer < 0 || nPlayer >= StartPointX.Length)
			return;

		if (OpenTaiko.Tx.Notes[(int)gameType] != null) {
			ref var flying = ref Flying[FlyingTail];
			if (!flying.IsUsing) {
				FlyingTail = (FlyingTail + 1) % FLYING_COUNT;
				// 初期化
				flying.IsUsing = true;
				flying.Lane = nLane;
				flying.GameType = gameType;
				flying.Player = nPlayer;
				flying.X = -100; //StartPointX[nPlayer];
				flying.Y = -100; //TJAPlayer3.Skin.Game_Effect_FlyingNotes_StartPoint_Y[nPlayer];
				flying.StartPointX = StartPointX[nPlayer];
				flying.StartPointY = OpenTaiko.Skin.Game_Effect_FlyingNotes_StartPoint_Y[nPlayer];
				flying.OldValue = 0;
				flying.ForceFirework = forceFirework; // for balloons; no firework for big roll in some style (not followed)
				// 角度の決定
				flying.Height = Math.Abs(OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y[nPlayer] - OpenTaiko.Skin.Game_Effect_FlyingNotes_StartPoint_Y[nPlayer]);
				flying.Width = (Math.Abs((OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X[nPlayer] - StartPointX[nPlayer])) / 2);
				//Console.WriteLine("{0}, {1}", width2P, height2P);
				flying.Theta = ((Math.Atan2(flying.Height, flying.Width) * 180.0) / Math.PI);
				flying.Counter = new CCounter(0, 140, OpenTaiko.Skin.Game_Effect_FlyingNotes_Timer, OpenTaiko.Timer);
				//flying.Counter = new CCounter(0, 200000, CDTXMania.Skin.Game_Effect_FlyingNotes_Timer, CDTXMania.Timer);

				flying.IncreaseX = (1.00 * Math.Abs((OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X[nPlayer] - StartPointX[nPlayer]))) / (180);
				flying.IncreaseY = (1.00 * Math.Abs((OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y[nPlayer] - OpenTaiko.Skin.Game_Effect_FlyingNotes_StartPoint_Y[nPlayer]))) / (180);
			}
		}
	}

	// the skin has fly coordinates for the 1 and 2 player layouts only, so 3 players or more drop them
	internal static bool Flies(CConfigIni config, NotesManager.ENoteType nLane)
		=> config.nPlayerCount <= 2 && !config.SimpleMode
			&& nLane is not (NotesManager.ENoteType.Empty or NotesManager.ENoteType.Unknown);

	// CActivity 実装

	public override void Activate() {
		for (int i = 0; i < FLYING_COUNT; i++) {
			ref var flying = ref Flying[i];
			flying = new Status();
			flying.IsUsing = false;
			flying.Counter = new CCounter();
		}
		FlyingHead = FlyingTail = 0;
		for (int i = 0; i < 2; i++) {
			StartPointX[i] = OpenTaiko.Skin.Game_Effect_FlyingNotes_StartPoint_X[i];
		}
		base.Activate();
	}
	public override void DeActivate() {
		for (int i = 0; i < FLYING_COUNT; i++) {
			Flying[i].Counter = null;
		}
		base.DeActivate();
	}
	public override void CreateManagedResource() {
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		base.ReleaseManagedResource();
	}
	public override int Draw() {
		if (!base.IsDeActivated && !OpenTaiko.ConfigIni.SimpleMode) {
			var prevHead = FlyingHead;
			for (int i = 0; i < FLYING_COUNT; i++) {
				var iState = (prevHead + i) % FLYING_COUNT;
				ref var state = ref Flying[iState];
				if (!state.IsUsing) {
					if (iState == FlyingTail)
						break;
				} else {
					// the shown gameplay background can make the notes land on its own art
					bool toBackground = OpenTaiko.stageGameScreen.actBackground.FlyTarget(state.Player, out double bgX, out double bgY);
					state.OldValue = state.Counter.CurrentValue;
					state.Counter.Tick();
					if (state.Counter.IsEnded) {
						state.Counter.Stop();
						state.IsUsing = false;
						if (iState == FlyingHead)
							FlyingHead = (FlyingHead + 1) % FLYING_COUNT;
						OpenTaiko.stageGameScreen.actGauge.Start(state.Lane, state.GameType, ENoteJudge.Perfect, state.Player);
						if (!toBackground)
							OpenTaiko.stageGameScreen.actChipEffects.Start(state.Player, state.Lane, state.GameType);
						OpenTaiko.stageGameScreen.actBackground.NoteLanded(state.Player);
					}

					int endX;
					int endY;

					if (toBackground) {
						endX = (int)bgX;
						endY = (int)bgY;
					} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
						endX = OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X_AI[state.Player];
						endY = OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y_AI[state.Player];
					} else {
						endX = OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X[state.Player];
						endY = OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y[state.Player];
					}

					int movingDistanceX = endX - StartPointX[state.Player];
					int movingDistanceY = endY - OpenTaiko.Skin.Game_Effect_FlyingNotes_StartPoint_Y[state.Player];

					static IEnumerable<(int mid, bool onStep)> midValues(int old, int current, int step, bool needMid) {
						if (needMid) {
							int mid = (int)Math.Ceiling((double)old / step) * step;
							for (; mid <= current; mid += step)
								yield return (mid, true);
							if (mid == current)
								yield break;
						}
						yield return (current, false);
					}

					bool needFireWork = state.ForceFirework ?? NotesManager.IsBigNoteTaiko(state.Lane, state.GameType);
					foreach ((int mid, bool onStep) in midValues(state.OldValue, state.Counter.CurrentValue, OpenTaiko.Skin.Game_Effect_FireWorks_Timing, needFireWork)) {
						/*
                        if (TJAPlayer3.Skin.Game_Effect_FlyingNotes_IsUsingEasing)
                        {
                            flying.X = (flying.StartPointX + movingDistanceX + ((-Math.Cos(flying.Counter.n現在の値 * (Math.PI / 180)) * movingDistanceX))) - 85;
                            //flying.X += (Math.Cos(flying.Counter.n現在の値 * (Math.PI / 180))) * flying.Increase;
                        }
                        else
                        {
                            flying.X += flying.IncreaseX;
                        }
                        */

						double movingRate = (mid / 140.0);

						state.X = StartPointX[state.Player] + OpenTaiko.stageGameScreen.GetJPOSCROLLX(state.Player) + (movingDistanceX * movingRate);
						state.Y = OpenTaiko.Skin.Game_Effect_FlyingNotes_StartPoint_Y[state.Player] + OpenTaiko.stageGameScreen.GetJPOSCROLLY(state.Player) + (int)(movingDistanceY * movingRate);

						if (OpenTaiko.ConfigIni.bAIBattleMode) {
							state.Y += Math.Sin(movingRate * Math.PI) * ((state.Player == 0 ? -OpenTaiko.Skin.Game_Effect_FlyingNotes_Sine : OpenTaiko.Skin.Game_Effect_FlyingNotes_Sine) / 3.0);
						} else {
							state.Y += Math.Sin(movingRate * Math.PI) * (state.Player == 0 ? -OpenTaiko.Skin.Game_Effect_FlyingNotes_Sine : OpenTaiko.Skin.Game_Effect_FlyingNotes_Sine);
						}

						if (OpenTaiko.Skin.Game_Effect_FlyingNotes_IsUsingEasing) {
						} else {
						}

						if (needFireWork && onStep && mid > 8) {
							OpenTaiko.stageGameScreen.FireWorks.Start(state.Lane, state.GameType, state.Player, state.X, state.Y);
						}

						/*
                        if (flying.Player == 0)
                        {
                            flying.Y = ((TJAPlayer3.Skin.Game_Effect_FlyingNotes_StartPoint_Y[flying.Player]) + -Math.Sin(flying.Counter.n現在の値 * (Math.PI / 180)) * 559) + 329;
                            flying.Y -= flying.IncreaseY * flying.Counter.n現在の値;
                        }
                        else
                        {
                            flying.Y = ((TJAPlayer3.Skin.Game_Effect_FlyingNotes_StartPoint_Y[flying.Player]) + Math.Sin(flying.Counter.n現在の値 * (Math.PI / 180)) * 559) - 329;
                            flying.Y += flying.IncreaseY * flying.Counter.n現在の値;
                        }
                        */
					}
					//flying.OldValue = flying.Counter.n現在の値;

					NotesManager.DisplayNote(state.Player, (int)state.X, (int)state.Y, state.Lane, state.GameType);
				}
			}
		}
		return base.Draw();
	}


	#region [ private ]
	//-----------------

	[StructLayout(LayoutKind.Sequential)]
	private struct Status {
		public NotesManager.ENoteType Lane;
		public EGameType GameType;
		public int Player;
		public bool IsUsing;
		public CCounter Counter;
		public int OldValue;
		public double X;
		public double Y;
		public int Height;
		public int Width;
		public double IncreaseX;
		public double IncreaseY;
		public bool? ForceFirework;
		public int StartPointX;
		public int StartPointY;
		public double Theta;
	}

	private const int FLYING_COUNT = 128; // circular queue
	private readonly Status[] Flying = new Status[FLYING_COUNT];
	private int FlyingHead = 0;
	private int FlyingTail = 0;

	public readonly int[] StartPointX = new int[2];

	//-----------------
	#endregion
}
