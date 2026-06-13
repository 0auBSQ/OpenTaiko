using System;
using FDK;
using Newtonsoft.Json.Linq;
using Color = System.Drawing.Color;

namespace OpenTaiko {
	// ── OnlinePlaySync - runs the remote player spots during an ONLINE song ──────────────────────────────
	// One guarded call from the drum performance screen's Draw(). No-op unless LuaNetworking.Active.PlaySyncActive
	// (i.e. the onlinelobby bracketed this play round with NET:BeginPlaySync), so normal solo/local play is
	// untouched. Each frame it:
	//   • broadcasts the local spot-0 running score + gauge + good/ok/bad counts (~6-7x/sec) on "ps";
	//   • for each REMOTE spot, snaps its displayed score + gauge to that peer's latest broadcast (snapping = the
	//     score updates with no count-up animation), while the spot auto-hits its own chart with judges sampled
	//     from those broadcast rates (see CStage演奏画面共通.AlterJudgement) - flying notes are hidden in the chip
	//     draw, so you see judge indicators only;
	//   • freezes any spot whose remote player has dropped mid-play (it stops updating in real time).
	internal static class OnlinePlaySync {
		private static long _lastSend;
		private static int _epoch = -1;
		private static CCachedFontRenderer _waitFont;
		private static CTexture _waitTex;
		private static string _waitText;

		/// <summary>Centered overlay shown while the gameplay screen holds at the loading/start barrier.</summary>
		public static void DrawWaiting(string text) {
			try {
				if (_waitFont == null) _waitFont = HPrivateFastFont.tInstantiateMainFont(40);
				if (_waitTex == null || _waitText != text) {
					if (_waitTex != null) { var t = _waitTex; OpenTaiko.tテクスチャの解放(ref t); _waitTex = null; }
					using var bmp = _waitFont.DrawText(text, Color.White, Color.Black, null, 30);
					_waitTex = OpenTaiko.tテクスチャの生成(bmp, false); _waitText = text;
				}
				if (_waitTex != null)
					_waitTex.t2D描画(OpenTaiko.Skin.Resolution[0] / 2 - (int)(_waitTex.szTextureSize.Width / 2), OpenTaiko.Skin.Resolution[1] / 2 - 30);
			} catch { }
		}

		public static void Tick(CStage演奏画面共通 screen) {
			var net = LuaNetworking.Active;
			if (net == null || !net.PlaySyncActive) return;
			try {
				if (_epoch != net.PlaySyncEpoch) { _epoch = net.PlaySyncEpoch; _lastSend = 0; }

				// remote spots: snap score + gauge from the wire (or freeze on disconnect) - every frame
				int count = net.PlaySpotCount();
				for (int spot = 1; spot < count; spot++) {
					if (!net.IsSpotActive(spot)) { screen.OnlineFreezeSpot(spot); continue; }   // dropped mid-play → freeze
					string json = net.GetSpotPlayJson(spot);
					if (string.IsNullOrEmpty(json)) continue;
					JObject o; try { o = JObject.Parse(json); } catch { continue; }
					try {
						if (o["s"] != null) screen.actScore.Set((double)o["s"], spot);
						if (o["g"] != null && screen.actGauge?.db現在のゲージ値 != null && spot < screen.actGauge.db現在のゲージ値.Length)
							screen.actGauge.db現在のゲージ値[spot] = (double)o["g"];
						// snap the combo counter too, so a remote spot's combo tracks the wire like its score
						if (o["co"] != null && screen.actCombo != null)
							screen.actCombo.nCurrentCombo[spot] = (int)o["co"];
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
					if (cs != null) { gr = cs.nGreat; gd = cs.nGood; ms = cs.nMiss; combo = cs.nCombo; acc = cs.GetScore(Exam.Type.Accuracy); }
					gauge = screen.actGauge.db現在のゲージ値[0];
				} catch { }
				var p = new JObject { ["n"] = net.SelfPlayName, ["s"] = score, ["g"] = gauge, ["a"] = Math.Round(acc, 2), ["gr"] = gr, ["gd"] = gd, ["ms"] = ms, ["co"] = combo };
				net.PushPlayScore(p.ToString(Newtonsoft.Json.Formatting.None));
			} catch { }
		}
	}
}
