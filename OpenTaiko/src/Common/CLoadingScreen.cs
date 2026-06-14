using FDK;

namespace OpenTaiko;

/// <summary>
/// The engine-drawn loading bar overlay. Rendered on top of whatever the loading stage already draws
/// (boot background, song-loading background + characters), so EVERY skin gets a consistent, robust
/// loading bar with zero skinner input. It reads <see cref="CLoadingProgress"/>'s smoothed
/// <see cref="CLoadingProgress.DisplayProgress"/> and advances the smoothing itself (one Tick per Draw),
/// so the bar always eases and shows an "alive" shimmer instead of snapping/sticking.
///
/// Self-contained: uses only <see cref="CTexture.FillBox"/> + the console font, no skin assets. Layout is
/// in the skin's logical resolution (same space the stages draw in).
/// </summary>
public static class CLoadingScreen {
	private static long _lastDrawTicks = 0;

	public static void Draw() {
		// Advance the eased display value using our own frame delta (clamped inside Tick).
		long now = DateTime.Now.Ticks;
		double deltaMs = _lastDrawTicks == 0 ? 0.0 : (now - _lastDrawTicks) / (double)TimeSpan.TicksPerMillisecond;
		_lastDrawTicks = now;
		CLoadingProgress.Tick(deltaMs);

		if (!CLoadingProgress.ShouldDraw) {
			_lastDrawTicks = 0;   // idle: reset so the next load's first frame has a 0 delta
			return;
		}

		// Layout in the skin's logical resolution (the coordinate space CTexture draws in).
		int resW = 1920, resH = 1080;
		try {
			var res = OpenTaiko.Skin?.Resolution;
			if (res != null && res.Length >= 2 && res[0] > 0 && res[1] > 0) { resW = res[0]; resH = res[1]; }
		} catch { /* skin not ready yet (very early boot) — fall back to 1920x1080 */ }

		int bw = (int)(resW * 0.46f);
		int bh = Math.Max(12, resH / 45);
		int bx = (resW - bw) / 2;
		int by = resH - bh - (resH / 10);

		float p = Math.Clamp(CLoadingProgress.DisplayProgress, 0f, 1f);
		int fillW = (int)(bw * p);

		// Backing (soft dark frame) + track + fill.
		CTexture.FillBox(bx - 5, by - 5, bw + 10, bh + 10, 0, 0, 0, 150);
		CTexture.FillBox(bx, by, bw, bh, 38, 38, 42, 225);
		if (fillW > 0)
			CTexture.FillBox(bx, by, fillW, bh, 255, 178, 64, 255);   // warm gold fill

		// Indeterminate shimmer: a soft highlight band sweeping across the filled portion ("alive" motion).
		if (fillW > 2) {
			int sw = Math.Max(28, bw / 9);
			int sx = bx - sw + (int)((fillW + sw) * CLoadingProgress.AnimPhase);
			int x0 = Math.Max(bx, sx);
			int x1 = Math.Min(bx + fillW, sx + sw);
			if (x1 > x0)
				CTexture.FillBox(x0, by, x1 - x0, bh, 255, 255, 255, 70);
		}

		// Percentage, using the console font (matches the boot screen). Guarded: actTextConsole may not be
		// initialized during the earliest boot frames — the bar still shows without it.
		try {
			OpenTaiko.actTextConsole?.Print(bx + bw + 18, by + (bh / 2) - 8,
				CTextConsole.EFontType.White, CLoadingProgress.DisplayPercent + "%");
		} catch { /* font not ready yet — skip the % this frame */ }
	}
}
