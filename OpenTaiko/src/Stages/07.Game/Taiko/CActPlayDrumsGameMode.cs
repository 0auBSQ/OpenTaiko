using FDK;
using Rectangle = System.Drawing.Rectangle;

namespace OpenTaiko;

// The Timed special mod in play: one timer per player judged under it, drawn at the player's lane. An online remote
// player's timer is not computed here: the value its owner sends is drawn as it comes.
internal class CActPlayDrumsGameMode : CActivity {
	public CActPlayDrumsGameMode() {
		this.IsDeActivated = true;
	}

	// what a remote spot's owner last sent: the timer in ms, below 0 while its digits are hidden
	private readonly record struct RemoteTimer(int MsElapsed, int? AddedSeconds);

	private readonly TimedMod?[] timers = new TimedMod?[OpenTaiko.MAX_PLAYERS];
	private readonly RemoteTimer?[] remoteTimers = new RemoteTimer?[OpenTaiko.MAX_PLAYERS];

	private static CStagePlayDrumsScreen Stage => OpenTaiko.stageGameScreen;

	// a new play or a retry: a fresh timer for each player judged under Timed
	public void Initialize() {
		for (int i = 0; i < OpenTaiko.MAX_PLAYERS; ++i) {
			this.remoteTimers[i] = null;
			ESpecialMod mod = (i < OpenTaiko.ConfigIni.nPlayerCount) ? Stage.JudgedSpecialMod(i) : ESpecialMod.None;
			if (!SpecialMods.IsTimed(mod)) {
				this.timers[i] = null;
				continue;
			}
			int player = i;
			int level = OpenTaiko.GetTJA(player)?.PlayerSideMetadata.LEVELtaiko ?? 0;
			this.timers[i] = new TimedMod(TimedMod.TablesFor(mod == ESpecialMod.TimedHard, level), () => {
				var score = Stage.CChartScore[player];
				return new TimedMod.Totals(score.nGreat + score.nGood, score.nMiss, Stage.actCombo.nCurrentCombo.MaxValue[player]);
			});
		}
	}

	// the player's timer while this machine judges them under Timed
	private TimedMod? TimerOf(int player)
		=> SpecialMods.IsTimed(Stage.JudgedSpecialMod(player)) ? this.timers[player] : null;

	public bool IsTimeUp(int player) => this.TimerOf(player)?.IsTimeUp == true;

	// a note of the player reached the judge line
	public void OnNoteReached(int player) => this.TimerOf(player)?.Start();

	// a hit, or a note the player let pass (a miss)
	public void OnJudge(int player, ENoteJudge judge, int msLag, long msChartTime)
		=> this.TimerOf(player)?.Judge(judge, msLag, msChartTime);

	// the local player's timer for the online play sync, null when not under Timed
	public (int msElapsed, int? addedSeconds)? OwnTimer() {
		TimedMod? timer = this.TimerOf(0);
		if (timer == null)
			return null;
		return (timer.IsJudging ? -1 : Math.Max(0, timer.MsElapsed), timer.AddedSeconds);
	}

	// the timer a remote spot's owner sent, null when that player is not under Timed
	public void SetRemoteTimer(int spot, int? msElapsed, int? addedSeconds)
		=> this.remoteTimers[spot] = (msElapsed is int ms) ? new RemoteTimer(ms, addedSeconds) : null;

	// moves the timers, and in a 1-player play darkens the screen over the last seconds
	public override int Draw() {
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i) {
			TimedMod? timer = this.TimerOf(i);
			if (timer == null || Stage.IsStageFailed(i))
				continue;
			long msNow = Stage.GetChartTimeNow(i);
			timer.Advance(msNow);
			timer.SetNotes(Stage.rIsChipInSearchRange(msNow, TimedMod.MsNotesNear, i),
				Stage.rIsChipInSearchRange(msNow, TimedMod.MsNotesAhead, i));
		}

		if (OpenTaiko.ConfigIni.nPlayerCount == 1 && this.TimerOf(0) is TimedMod solo) {
			int opacity = solo.MsElapsed switch {
				>= 24000 => 192,
				>= 23000 => 128,
				>= 22000 => 64,
				_ => 0,
			};
			if (opacity > 0)
				HBlackBackdrop.Draw(opacity);
		}
		return 0;
	}

	// each player's timer frame, digits and added seconds
	public void DrawTimers() {
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i) {
			int msElapsed;
			bool showDigits;
			int? addedSeconds;
			int digitOpacity = 255;
			if (this.TimerOf(i) is TimedMod timer) {
				msElapsed = timer.MsElapsed;
				showDigits = !timer.IsJudging;
				addedSeconds = timer.AddedSeconds;
				if (timer.IsStarted && !timer.IsRunning)
					digitOpacity = 128;
			} else if (this.remoteTimers[i] is RemoteTimer remote && LuaNetworking.Active?.IsRemoteSpot(i) == true) {
				msElapsed = remote.MsElapsed;
				showDigits = remote.MsElapsed >= 0;
				addedSeconds = remote.AddedSeconds;
			} else {
				continue;
			}

			CTexture? frame = OpenTaiko.Tx.GameMode_Timer_Frame;
			int width = frame?.szTextureSize.Width ?? 100;
			int height = frame?.szTextureSize.Height ?? 100;
			var (x, y) = FramePosition(i, width, height);
			frame?.t2DDraw(x, y);
			if (showDigits)
				DrawDigits(x + width / 2, y + height / 2, width, TimedMod.DisplayedSeconds(msElapsed), digitOpacity);
			if (addedSeconds is int added)
				DrawAddedSeconds(x + width / 2, y + height * 2 / 3, added);
		}
	}

	// 1 player: the spot of the 1280x720 layout, scaled to the skin. Several players: the right end of the player's lane.
	private static (int x, int y) FramePosition(int player, int width, int height) {
		int[] resolution = OpenTaiko.Skin.Resolution;
		if (OpenTaiko.ConfigIni.nPlayerCount == 1)
			return (230 * resolution[0] / 1280, 84 * resolution[1] / 720);
		int laneY = Stage.GetNoteOriginY(player) - Stage.GetJPOSCROLLY(player);
		return (resolution[0] - width - 24 * resolution[0] / 1280, laneY + (OpenTaiko.Skin.Game_Notes_Size[1] - height) / 2);
	}

	// the remaining seconds in combo digits, centred in the frame and shrunk to fit its width
	private static void DrawDigits(int centerX, int centerY, int frameWidth, int seconds, int opacity) {
		CTexture? digits = OpenTaiko.Tx.Taiko_Combo[0];
		if (digits == null)
			return;
		int digitWidth = OpenTaiko.Skin.Game_Taiko_Combo_Size[0];
		int digitHeight = OpenTaiko.Skin.Game_Taiko_Combo_Size[1];
		int step = OpenTaiko.Skin.Game_Taiko_Combo_Padding[0];
		string text = seconds.ToString();
		float scale = Math.Min(1f, frameWidth * 0.9f / (step * (text.Length - 1) + digitWidth));

		digits.Opacity = opacity;
		digits.vcScaleRatio.X = scale;
		digits.vcScaleRatio.Y = scale;
		float left = centerX - step * scale * (text.Length - 1) / 2f;
		for (int i = 0; i < text.Length; ++i) {
			var rect = new Rectangle(digitWidth * (text[i] - '0'), 0, digitWidth, digitHeight);
			digits.t2DCenterBasedDraw(left + step * scale * i, centerY, rect);
		}
		digits.Opacity = 255;
		digits.vcScaleRatio.X = 1f;
		digits.vcScaleRatio.Y = 1f;
	}

	// the seconds an extension added, in score digits
	private static void DrawAddedSeconds(int centerX, int y, int seconds) {
		CTexture? digits = OpenTaiko.Tx.Taiko_Score[0];
		if (digits == null)
			return;
		int digitWidth = OpenTaiko.Skin.Game_Score_Size[0];
		int digitHeight = OpenTaiko.Skin.Game_Score_Size[1];
		string text = seconds.ToString();
		int left = centerX - digitWidth * text.Length / 2;

		digits.Opacity = 255;
		digits.vcScaleRatio.X = 1f;
		digits.vcScaleRatio.Y = 1f;
		for (int i = 0; i < text.Length; ++i)
			digits.t2DDraw(left + digitWidth * i, y, new Rectangle(digitWidth * (text[i] - '0'), 0, digitWidth, digitHeight));
	}
}
