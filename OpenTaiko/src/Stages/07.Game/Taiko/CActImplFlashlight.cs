using FDK;

namespace OpenTaiko;

internal class CActImplFlashlight : CActivity {
	// the radius of the light as a share of the lane height, shrinking as the combo grows
	private const float RADIUS = 1.05f;
	private const float RADIUS_100 = 0.82f;
	private const float RADIUS_200 = 0.64f;
	// the light is full up to this share of the radius, then falls off to the rim
	private const float FULL_SHARE = 0.84f;
	private const int DOWNSCALE = 2;
	private const int FADE_MS = 600;

	private LuaCanvas? canvas;
	private readonly List<long> shape = new();    // the bands and circles the canvas currently holds
	private readonly List<long> wanted = new();
	private long fadeStartMs = -1;
	private bool active;

	public bool Active => this.active;
	public bool IsFlashlit(int player) => OpenTaiko.ConfigIni.eSTEALTH[player] == EStealthMode.Flashlight;
	// a player without the mod draws over the canvas while anyone has it, so nothing of theirs is darkened
	public bool DrawsOverLight(int player) => this.active && !this.IsFlashlit(player);

	public override void Activate() {
		if (this.IsActivated) return;
		this.Reset();
		base.Activate();
	}

	// the canvas is per play: freed on deactivation (the stage is deactivated before its resources are
	// released, and they may not be released at all with pre-loaded assets)
	public override void DeActivate() {
		if (this.IsDeActivated) return;
		this.canvas?.Dispose();
		this.canvas = null;
		base.DeActivate();
	}

	public override void ReleaseManagedResource() {
		this.canvas?.Dispose();
		this.canvas = null;
		base.ReleaseManagedResource();
	}

	public void Reset() {
		this.active = false;
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) this.active |= this.IsFlashlit(i);
		this.fadeStartMs = -1;
		this.shape.Clear();
		this.canvas?.ClearTransparent();
		this.canvas?.Upload();
	}

	// the status (clear or fail) is appearing: the darkness lifts
	public void BeginFade() {
		if (this.fadeStartMs < 0) this.fadeStartMs = SoundManager.PlayTimer.NowTimeMs;
	}

	// 1 while the mod is dark, down to 0 at the end of the fade
	private float Darkness() {
		if (this.fadeStartMs < 0) return 1f;
		return 1f - Math.Clamp((SoundManager.PlayTimer.NowTimeMs - this.fadeStartMs) / (float)FADE_MS, 0f, 1f);
	}

	// the opacity the light gives a flashlit player's element whose note slot's top-left is (x, y); 1 for the others
	public float NoteOpacity(int player, int x, int y) {
		if (!this.IsFlashlit(player)) return 1f;
		var (cx, cy, r) = this.Light(player);
		float dx = x + OpenTaiko.Skin.Game_Notes_Size[0] / 2 - cx;
		float dy = y + OpenTaiko.Skin.Game_Notes_Size[1] / 2 - cy;
		return this.Lift(Lit(MathF.Sqrt(dx * dx + dy * dy), r));
	}

	// the same for a roll, judged by the point of its head-to-tail segment closest to the light
	public float RollOpacity(int player, int x, int y, int xEnd, int yEnd) {
		if (!this.IsFlashlit(player)) return 1f;
		var (cx, cy, r) = this.Light(player);
		float half = OpenTaiko.Skin.Game_Notes_Size[0] / 2f;
		float ax = x + half - cx, ay = y + half - cy;
		float bx = xEnd + half - cx, by = yEnd + half - cy;
		float vx = bx - ax, vy = by - ay;
		float len2 = vx * vx + vy * vy;
		float t = len2 <= 0f ? 0f : Math.Clamp(-(ax * vx + ay * vy) / len2, 0f, 1f);
		float px = ax + vx * t, py = ay + vy * t;
		return this.Lift(Lit(MathF.Sqrt(px * px + py * py), r));
	}

	// the darkness lifting also lifts the elements it was hiding
	private float Lift(float lit) => lit + (1f - lit) * (1f - this.Darkness());

	// 1 inside the full light, falling off smoothly to 0 at the rim
	private static float Lit(float d, float r) {
		float full = r * FULL_SHARE;
		if (d <= full) return 1f;
		if (d >= r) return 0f;
		float k = (d - full) / (r - full);
		return 1f - k * k * (3f - 2f * k);
	}

	private static int LaneHeight => OpenTaiko.Tx.Lane_Background_Main?.szTextureSize.Height ?? 195;

	// a flashlit player's light: centred on the judge zone wherever it has scrolled, radius by combo
	private (int cx, int cy, int r) Light(int player) {
		var screen = OpenTaiko.stageGameScreen;
		int combo = screen.actCombo.nCurrentCombo[player];
		float share = combo >= 200 ? RADIUS_200 : combo >= 100 ? RADIUS_100 : RADIUS;
		int cx = screen.GetNoteOriginX(player) + OpenTaiko.Skin.Game_Notes_Size[0] / 2;
		int cy = screen.GetNoteOriginY(player) + OpenTaiko.Skin.Game_Notes_Size[1] / 2;
		return (cx, cy, Math.Max(8, (int)(LaneHeight * share)));
	}

	// the lane background's rectangle, which is all the mod darkens
	private static (int left, int top, int right, int bottom) Lane(int player) {
		int x, y;
		if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
			x = OpenTaiko.Skin.Game_Lane_5P[0] + OpenTaiko.Skin.Game_UIMove_5P[0] * player;
			y = OpenTaiko.Skin.Game_Lane_5P[1] + OpenTaiko.Skin.Game_UIMove_5P[1] * player;
		} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
			x = OpenTaiko.Skin.Game_Lane_4P[0] + OpenTaiko.Skin.Game_UIMove_4P[0] * player;
			y = OpenTaiko.Skin.Game_Lane_4P[1] + OpenTaiko.Skin.Game_UIMove_4P[1] * player;
		} else {
			x = OpenTaiko.Skin.Game_Lane_X[player];
			y = OpenTaiko.Skin.Game_Lane_Y[player];
		}
		int w = OpenTaiko.Tx.Lane_Background_Main?.szTextureSize.Width ?? OpenTaiko.Skin.Resolution[0];
		return (x, y, x + w, y + LaneHeight);
	}

	private static long Pack(int a, int b, int c, int d) => ((long)(a & 0xFFFF) << 48) | ((long)(b & 0xFFFF) << 32) | ((long)(c & 0xFFFF) << 16) | (long)(d & 0xFFFF);

	public override int Draw() {
		if (this.IsDeActivated || !this.active) return 0;
		float darkness = this.Darkness();
		if (darkness <= 0f) return 0;

		int W = OpenTaiko.Skin.Resolution[0], H = OpenTaiko.Skin.Resolution[1];
		this.wanted.Clear();
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			if (!this.IsFlashlit(i)) continue;
			var (left, top, right, bottom) = Lane(i);
			var (cx, cy, r) = this.Light(i);
			this.wanted.Add(Pack(left, top, right, bottom));
			this.wanted.Add(Pack(cx, cy, r, 0));
		}

		this.canvas ??= new LuaCanvas(Math.Max(1, W / DOWNSCALE), Math.Max(1, H / DOWNSCALE), W, H);

		bool same = this.wanted.Count == this.shape.Count;
		for (int i = 0; same && i < this.wanted.Count; i++) same = this.wanted[i] == this.shape[i];
		if (!same) {
			this.Raster();
			this.shape.Clear();
			this.shape.AddRange(this.wanted);
		}

		this.canvas.SetOpacity(darkness);
		this.canvas.Draw(0, 0);
		this.canvas.SetOpacity(1f);
		return 0;
	}

	// black over every band, then every circle lowers the alpha inside its radius with a soft rim,
	// wherever it falls, so any light reaches any band
	private void Raster() {
		var cv = this.canvas!;
		int w = cv._w, h = cv._h;
		var buf = cv._buf;
		Array.Clear(buf, 0, buf.Length);
		int ds = DOWNSCALE;
		int minY = h, maxY = -1;
		for (int i = 0; i + 1 < this.wanted.Count; i += 2) {
			long band = this.wanted[i];
			int left = (int)((band >> 48) & 0xFFFF) / ds, top = (int)((band >> 32) & 0xFFFF) / ds;
			int right = ((int)((band >> 16) & 0xFFFF) + ds - 1) / ds, bottom = ((int)(band & 0xFFFF) + ds - 1) / ds;
			left = Math.Max(0, left); top = Math.Max(0, top); right = Math.Min(w, right); bottom = Math.Min(h, bottom);
			for (int y = top; y < bottom; y++) {
				int o = (y * w + left) * 4;
				for (int x = left; x < right; x++, o += 4) { buf[o] = 0; buf[o + 1] = 0; buf[o + 2] = 0; buf[o + 3] = 255; }
			}
			if (bottom > top) { minY = Math.Min(minY, top); maxY = Math.Max(maxY, bottom - 1); }
		}
		for (int i = 0; i + 1 < this.wanted.Count; i += 2) {
			long circle = this.wanted[i + 1];
			float cx = (float)((circle >> 48) & 0xFFFF) / ds, cy = (float)((circle >> 32) & 0xFFFF) / ds, r = (float)((circle >> 16) & 0xFFFF) / ds;
			int x0 = Math.Max(0, (int)(cx - r) - 1), x1 = Math.Min(w - 1, (int)(cx + r) + 1);
			int y0 = Math.Max(0, (int)(cy - r) - 1), y1 = Math.Min(h - 1, (int)(cy + r) + 1);
			for (int y = y0; y <= y1; y++) {
				float dy = y + 0.5f - cy;
				int o = (y * w + x0) * 4 + 3;
				for (int x = x0; x <= x1; x++, o += 4) {
					if (buf[o] == 0) continue;
					float dx = x + 0.5f - cx;
					byte a = (byte)(255f * (1f - Lit(MathF.Sqrt(dx * dx + dy * dy), r)));
					if (a < buf[o]) buf[o] = a;
				}
			}
		}
		if (maxY >= minY) cv.MarkDirty(0, minY, w - 1, maxY);
		cv.Upload();
	}
}
