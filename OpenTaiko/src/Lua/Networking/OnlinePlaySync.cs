using System;
using FDK;
using Newtonsoft.Json.Linq;
using Color = System.Drawing.Color;

namespace OpenTaiko {
	// ── OnlinePlaySync - runs the remote player spots during an ONLINE song ──────────────────────────────
	// One guarded call from the drum performance screen's Draw(). No-op unless LuaNetworking.Active.PlaySyncActive
	// (i.e. the onlinelobby bracketed this play round with NET:BeginPlaySync), so normal solo/local play is
	// untouched. Each frame it:
	//   • broadcasts the local spot-0 running score + gauge + good/ok/bad counts (~6-7x/sec) on "ps", with its Timed
	//     timer when it plays under that special mod: a remote spot's timer is only shown, never computed here,
	//     and with "f" once it has failed, which fails the remote spot the way a hard gauge at 0 does;
	//   • for each REMOTE spot, snaps its displayed score + gauge to that peer's latest broadcast (snapping = the
	//     score updates with no count-up animation) and lets its combo follow the broadcast one (see FollowCombo);
	//     a frame is parsed once, when it arrives, while the spot auto-hits its own chart with judges sampled
	//     from those broadcast rates (see CStagePlayScreenCommon.AlterJudgement) - remote spots are lanes of the
	//     normal N-player layout, and flying notes follow the offline rule (shown up to 2 players);
	//   • freezes any spot whose remote player has dropped mid-play (it stops updating in real time).
	internal static class OnlinePlaySync {
		private static long _lastSend;
		private static int _epoch = -1;
		private static LuaNetworking? _net;
		private static CCachedFontRenderer _waitFont;
		private static CTexture _waitTex;
		private static string _waitText;
		// per remote spot: the last "ps" string seen and what it held
		private static readonly string[] _frameJson = new string[OpenTaiko.MAX_PLAYERS];
		private static readonly PlayFrame?[] _frames = new PlayFrame?[OpenTaiko.MAX_PLAYERS];

		/// <summary>Centered overlay shown while the gameplay screen holds at the loading/start barrier.</summary>
		public static void DrawWaiting(string text) {
			try {
				if (_waitFont == null) _waitFont = HPrivateFastFont.tInstantiateMainFont(40);
				if (_waitTex == null || _waitText != text) {
					if (_waitTex != null) { var t = _waitTex; OpenTaiko.tTextureRelease(ref t); _waitTex = null; }
					using var bmp = _waitFont.DrawText(text, Color.White, Color.Black, null, 30);
					_waitTex = OpenTaiko.tTextureCreate(bmp, false); _waitText = text;
				}
				if (_waitTex != null)
					_waitTex.t2DDraw(OpenTaiko.Skin.Resolution[0] / 2 - (int)(_waitTex.szTextureSize.Width / 2), OpenTaiko.Skin.Resolution[1] / 2 - 30);
			} catch { }
		}

		// The Timed timer on "ps": "tm" is the owner's timer in ms (below 0 while its digits are hidden) and "ta" the
		// added seconds it shows. Each is left out when it does not apply, as for a player not under Timed.
		internal static void WriteTimer(JObject ps, (int msElapsed, int? addedSeconds)? timer) {
			if (timer == null) return;
			ps["tm"] = timer.Value.msElapsed;
			if (timer.Value.addedSeconds != null) ps["ta"] = timer.Value.addedSeconds.Value;
		}
		internal static (int? msElapsed, int? addedSeconds) ReadTimer(JObject ps)
			=> ((int?)ps["tm"], (int?)ps["ta"]);

		// The owner's failed status on "ps": "f" is written once the player has failed and left out before.
		internal static void WriteFailed(JObject ps, bool failed) {
			if (failed) ps["f"] = 1;
		}
		internal static bool ReadFailed(JObject ps) => (int?)ps["f"] == 1;

		// What a "ps" frame holds for the remote spot's lane; a field the frame leaves out is null.
		internal readonly record struct PlayFrame(double? Score, double? Gauge, int? Combo, int? MsTimer, int? AddedSeconds, bool Failed);

		internal static bool TryParseFrame(string json, out PlayFrame frame) {
			frame = default;
			try {
				JObject o = JObject.Parse(json);
				var (msTimer, addedSeconds) = ReadTimer(o);
				frame = new PlayFrame((double?)o["s"], (double?)o["g"], (int?)o["co"], msTimer, addedSeconds, ReadFailed(o));
				return true;
			} catch { return false; }
		}

		// The combo a remote lane shows once a frame arrived. Only a combo that differs from the previous frame's
		// acts: the lane drops to it when the owner's combo went down, and rises to it when the lane is behind.
		// Otherwise the lane keeps counting its own hits.
		internal static int FollowCombo(int local, int wire, int? lastWire) {
			if (wire == lastWire) return local;
			if (WireDropped(wire, lastWire)) return wire;
			return Math.Max(local, wire);
		}
		internal static bool WireDropped(int wire, int? lastWire) => lastWire != null && wire < lastWire;

		// A new play round: its number changed, or it runs on another connection, whose rounds are numbered from 1 again.
		internal static bool IsNewRound(LuaNetworking? lastNet, int lastEpoch, LuaNetworking net)
			=> !ReferenceEquals(lastNet, net) || lastEpoch != net.PlaySyncEpoch;

		public static void Tick(CStagePlayDrumsScreen screen) {
			var net = LuaNetworking.Active;
			if (net == null || !net.PlaySyncActive) return;
			try {
				if (IsNewRound(_net, _epoch, net)) {
					_net = net; _epoch = net.PlaySyncEpoch; _lastSend = 0;
					Array.Clear(_frameJson); Array.Clear(_frames);
				}

				// remote spots: snap score + gauge from the wire (or freeze on disconnect) - every frame
				int count = Math.Min(net.PlaySpotCount(), _frames.Length);
				for (int spot = 1; spot < count; spot++) {
					if (!net.IsSpotActive(spot)) { screen.OnlineFreezeSpot(spot); continue; }   // dropped mid-play → freeze
					string json = net.GetSpotPlayJson(spot);
					if (string.IsNullOrEmpty(json)) continue;
					try {
						// the same string comes back until the peer's next frame; a frame that does not parse leaves the last one
						if (!ReferenceEquals(json, _frameJson[spot])) {
							_frameJson[spot] = json;
							if (TryParseFrame(json, out PlayFrame parsed)) {
								if (parsed.Combo != null && screen.actCombo != null) {
									int local = screen.actCombo.nCurrentCombo[spot];
									int? lastWire = _frames[spot]?.Combo;
									int followed = FollowCombo(local, parsed.Combo.Value, lastWire);
									bool dropped = WireDropped(parsed.Combo.Value, lastWire);
									if (followed != local || dropped) screen.OnlineSetCombo(spot, followed, dropped);
								}
								_frames[spot] = parsed;
							}
						}
						if (_frames[spot] is not PlayFrame f) continue;
						if (f.Score != null) screen.actScore.Set(f.Score.Value, spot);
						if (f.Gauge != null && screen.actGauge?.dbCurrentGaugeValue != null && spot < screen.actGauge.dbCurrentGaugeValue.Length)
							screen.actGauge.dbCurrentGaugeValue[spot] = f.Gauge.Value;
						screen.actGame.SetRemoteTimer(spot, f.MsTimer, f.AddedSeconds);
						if (f.Failed && !screen.IsStageFailed(spot)) screen.SetStageFailed(spot);
					} catch { }
				}

				// broadcast the local player's (spot 0) running state ~6-7x/sec
				long now = Environment.TickCount64;
				if (now - _lastSend < 150) return;
				_lastSend = now;
				long score = 0; double acc = 0, gauge = 0; int gr = 0, gd = 0, ms = 0, combo = 0;
				try {
					score = screen.actScore.GetDisplayedScore(0);
					var cs = screen.CChartScore[0];
					if (cs != null) { gr = cs.nGreat; gd = cs.nGood; ms = cs.nMiss; acc = cs.GetScore(Exam.Type.Accuracy); }
					if (screen.actCombo != null) combo = screen.actCombo.nCurrentCombo[0];
					gauge = screen.actGauge.dbCurrentGaugeValue[0];
				} catch { }
				var p = new JObject { ["n"] = net.SelfPlayName, ["s"] = score, ["g"] = gauge, ["a"] = Math.Round(acc, 2), ["gr"] = gr, ["gd"] = gd, ["ms"] = ms, ["co"] = combo };
				WriteTimer(p, screen.actGame.OwnTimer());
				WriteFailed(p, screen.IsStageFailed(0));
				net.PushPlayScore(p.ToString(Newtonsoft.Json.Formatting.None));
			} catch { }
		}
	}
}
