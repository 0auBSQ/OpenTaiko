using System;
using System.Collections.Generic;
using System.Numerics;

namespace OpenTaiko.AviUtl2 {
	// The moving and timed effects: the ways an object comes in and goes out, shakes, swings, blinks and breaks.
	internal static class Aup2FiltersMotion {
		public static void Register(Dictionary<string, Aup2FilterFn> map) {
			map["ワイプ"] = Wipe;
			map["画面外から登場"] = FromOutside;
			map["ランダム方向から登場"] = FromRandomSide;
			map["拡大縮小して登場"] = ScaleIn;
			map["ランダム間隔で落ちながら登場"] = FallIn;
			map["弾んで登場"] = BounceIn;
			map["広がって登場"] = SpreadIn;
			map["起き上がって登場"] = RiseIn;
			map["何処からともなく登場"] = FromNowhere;
			map["点滅して登場"] = BlinkIn;
			map["振動"] = Vibrate;
			map["震える"] = Tremble;
			map["振り子"] = Pendulum;
			map["弾む"] = Bounce;
			map["反復移動"] = BackAndForth;
			map["砕け散る"] = Shatter;
			map["点滅"] = Blink;
			map["リール回転"] = Reel;
			map["粒子化"] = Particlize;
			map["パーティクル出力"] = Particles;
			map["パーティクル出力(カメラ制御)"] = ParticlesInSpace;
		}

		#region Helpers
		// How far an entrance is: 0 at its start, 1 once the object is in place. A positive time plays at
		// the object's start; a negative one plays backwards at its end. `delay` holds a piece back.
		private static double Progress(Aup2Context c, double time, double delay = 0) {
			if (time > 0) return Math.Clamp((c.Time - delay) / time, 0, 1);
			if (time < 0) return Math.Clamp((c.TimeLeft - delay) / -time, 0, 1);
			return 1;
		}

		private static double EaseOut(double p) => 1 - (1 - p) * (1 - p);

		// the pieces an effect acts on: the object's parts, or the object as one piece (index 0 of 1)
		private delegate void PieceFn(int index, int count, ref double x, ref double y, ref double z,
			ref double sx, ref double sy, ref double rx, ref double ry, ref double rz, ref double opacity);

		private static void EachPiece(Aup2Context c, PieceFn fn) {
			var item = c.Item;
			if (item.Parts == null) {
				double sx = 1, sy = 1, x = 0, y = 0, z = 0, rx = 0, ry = 0, rz = 0, op = 1;
				fn(0, 1, ref x, ref y, ref z, ref sx, ref sy, ref rx, ref ry, ref rz, ref op);
				item.X += x; item.Y += y; item.Z += z;
				item.ScaleX *= sx; item.ScaleY *= sy;
				item.RotX += rx; item.RotY += ry; item.RotZ += rz;
				item.Opacity *= op;
				return;
			}
			int n = item.Parts.Count;
			for (int i = 0; i < n; i++) {
				var p = item.Parts[i];
				double sx = 1, sy = 1, x = 0, y = 0, z = 0, rx = 0, ry = 0, rz = 0, op = 1;
				fn(i, n, ref x, ref y, ref z, ref sx, ref sy, ref rx, ref ry, ref rz, ref op);
				p.X += x; p.Y += y; p.Z += z;
				p.ScaleX *= sx; p.ScaleY *= sy;
				p.RotX += rx; p.RotY += ry; p.RotZ += rz;
				p.Opacity *= op;
			}
		}

		// a smooth wobble in -1..1 from a clock and a name
		private static double Wobble(Aup2Context c, string name, double clock, int index = 0) {
			double f = Math.Floor(clock), t = clock - f;
			double a = c.Random(name, (long)f * 1009 + index) * 2 - 1, b = c.Random(name, ((long)f + 1) * 1009 + index) * 2 - 1;
			t = t * t * (3 - 2 * t);
			return a + (b - a) * t;
		}
		#endregion

		#region Coming in and going out
		private static void FromOutside(Aup2Context c) {
			double time = c.V("時間", 0.5), angle = c.V("角度", 90);
			bool random = c.Flag("ランダム方向");
			double far = Math.Sqrt((double)c.FrameW * c.FrameW + (double)c.FrameH * c.FrameH);
			EachPiece(c, (int i, int n, ref double x, ref double y, ref double z, ref double sx, ref double sy, ref double rx, ref double ry, ref double rz, ref double op) => {
				double p = EaseOut(Progress(c, time));
				// 0 degrees comes from the top, clockwise
				double a = (random ? c.Random("outside", i) * 360 : angle) * Math.PI / 180;
				x += Math.Sin(a) * far * (1 - p);
				y += -Math.Cos(a) * far * (1 - p);
			});
		}

		private static void FromRandomSide(Aup2Context c) {
			double time = c.V("時間", 1.5), turns = c.V("回転", 3);
			bool ease = c.Flag("加減速");
			double far = Math.Max(c.FrameW, c.FrameH);
			EachPiece(c, (int i, int n, ref double x, ref double y, ref double z, ref double sx, ref double sy, ref double rx, ref double ry, ref double rz, ref double op) => {
				double p = Progress(c, time);
				if (ease) p = EaseOut(p);
				double a = c.Random("side", i) * Math.PI * 2;
				x += Math.Cos(a) * far * (1 - p);
				y += Math.Sin(a) * far * (1 - p);
				rz += (c.Random("spin", i) < 0.5 ? -1 : 1) * turns * 360 * (1 - p);
				op *= p;
			});
		}

		private static void ScaleIn(Aup2Context c) {
			double time = c.V("時間", 0.3), from = c.V("拡大率", 300) / 100;
			bool ease = c.Flag("加減速");
			EachPiece(c, (int i, int n, ref double x, ref double y, ref double z, ref double sx, ref double sy, ref double rx, ref double ry, ref double rz, ref double op) => {
				double p = Progress(c, time);
				double q = ease ? EaseOut(p) : p;
				double s = from + (1 - from) * q;
				sx *= s; sy *= s;
				op *= p;
			});
		}

		private static void FallIn(Aup2Context c) {
			double time = c.V("時間", 0.5), spread = c.V("間隔", 1), distance = c.V("距離", 400);
			bool ease = c.Flag("加減速");
			EachPiece(c, (int i, int n, ref double x, ref double y, ref double z, ref double sx, ref double sy, ref double rx, ref double ry, ref double rz, ref double op) => {
				double p = Progress(c, time, c.Random("fall", i) * Math.Abs(spread));
				double q = ease ? EaseOut(p) : p;
				y -= distance * (1 - q);
				op *= p;
			});
		}

		private static void BounceIn(Aup2Context c) {
			double time = c.V("時間", 2), height = c.V("高さ", 200), count = Math.Max(1, c.V("回数", 4));
			EachPiece(c, (int i, int n, ref double x, ref double y, ref double z, ref double sx, ref double sy, ref double rx, ref double ry, ref double rz, ref double op) => {
				double p = Progress(c, time);
				// each bounce lower than the one before
				y -= height * Math.Abs(Math.Cos(p * count * Math.PI)) * (1 - p) * (1 - p);
			});
		}

		private static void SpreadIn(Aup2Context c) {
			double time = c.V("時間", 0.3);
			bool vertical = c.Flag("縦方向");
			EachPiece(c, (int i, int n, ref double x, ref double y, ref double z, ref double sx, ref double sy, ref double rx, ref double ry, ref double rz, ref double op) => {
				double p = Progress(c, time);
				if (vertical) sy *= p; else sx *= p;
			});
		}

		private static void RiseIn(Aup2Context c) {
			double time = c.V("時間", 0.6), force = c.V("勢い", 2);
			EachPiece(c, (int i, int n, ref double x, ref double y, ref double z, ref double sx, ref double sy, ref double rx, ref double ry, ref double rz, ref double op) => {
				double p = Progress(c, time);
				// from lying flat to upright, swinging past and back
				double swing = Math.Exp(-force * 2 * p) * Math.Cos(p * Math.PI * (1 + force));
				rx += 90 * swing * (1 - p * p * p);
			});
		}

		private static void FromNowhere(Aup2Context c) {
			double time = c.V("時間", 0.3), blur = c.V("ぼかし", 1), pos = c.V("位置", 1.5);
			double p0 = Progress(c, time);
			EachPiece(c, (int i, int n, ref double x, ref double y, ref double z, ref double sx, ref double sy, ref double rx, ref double ry, ref double rz, ref double op) => {
				double a = c.Random("nowhere", i) * Math.PI * 2;
				double far = pos * 100 * (1 - EaseOut(p0));
				x += Math.Cos(a) * far; y += Math.Sin(a) * far;
				op *= p0;
			});
			double radius = blur * 20 * (1 - p0);
			if (radius >= 0.5 && c.Pixels()) {
				int grow = (int)Math.Ceiling(radius);
				c.Grow(grow);
				const string frag = @"uniform vec2 u_dir;
void main(){
    vec4 s = vec4(0.0);
    for (int i = -8; i <= 8; i++) s += texture(u_src, v_uv + u_dir * float(i) / 8.0);
    o = s / 17.0;
}";
				c.Filter(frag, new Aup2Uniforms().V2("u_dir", radius / c.Surface!.W, 0));
				c.Filter(frag, new Aup2Uniforms().V2("u_dir", 0, radius / c.Surface!.H));
			}
		}

		private static void BlinkIn(Aup2Context c) {
			double time = c.V("時間", 1), every = Math.Max(1, c.V("点滅間隔", 1));
			bool even = c.Flag("点滅間隔を一定にする");
			double p = Progress(c, time);
			if (p >= 1) return;
			double frames = p * Math.Abs(time) * c.Scene.Rate;
			// the flashes come faster as the object settles, unless they are kept even
			double clock = even ? frames / every : Math.Pow(p, 2) * Math.Abs(time) * c.Scene.Rate / every;
			if (((long)Math.Floor(clock) & 1) == 0) c.Item.Opacity = 0;
		}

		private static void Wipe(Aup2Context c) {
			double fin = c.V("イン", 0.5), fout = c.V("アウト", 0.5), blur = Math.Max(0, c.V("ぼかし", 2));
			double p = 1;
			bool invert = false;
			if (fin > 0 && c.Time < fin) { p = c.Time / fin; invert = c.Flag("反転(イン)"); }
			else if (fout > 0 && c.TimeLeft < fout) { p = c.TimeLeft / fout; invert = c.Flag("反転(アウト)"); }
			if (p >= 1) return;
			if (p <= 0) { c.Item.Opacity = 0; return; }
			if (!c.Pixels()) return;
			string kind = c.S("ワイプの種類", "ワイプ(円)");
			int k = kind.Contains("円") ? 0 : kind.Contains("四角") ? 1 : kind.Contains("時計") ? 2 : kind.Contains("横") ? 3 : kind.Contains("縦") ? 4 : 0;
			c.Filter(@"uniform int u_kind; uniform float u_p; uniform float u_blur; uniform int u_inv; uniform int u_fixed;
void main(){
    vec4 s = texture(u_src, v_uv);
    vec2 q = (v_uv - 0.5) * u_size;
    vec2 half_ = u_size * 0.5;
    float d;   // > 0 inside the shown part, in pixels
    if (u_kind == 0) {
        float r = length(half_);
        d = u_fixed == 1 ? u_p * r - length(q) : (u_p - length(q / half_) / 1.41421356) * r;
    } else if (u_kind == 1) {
        vec2 e = u_fixed == 1 ? vec2(max(half_.x, half_.y)) : half_;
        vec2 a = e * u_p - abs(q);
        d = min(a.x, a.y);
    } else if (u_kind == 2) {
        float a = atan(q.x, -q.y) / 6.2831853;
        if (a < 0.0) a += 1.0;
        d = (u_p - a) * length(half_);
    } else if (u_kind == 3) d = u_p * u_size.x - v_uv.x * u_size.x;
    else d = u_p * u_size.y - v_uv.y * u_size.y;
    float m = clamp(d / max(u_blur, 0.001) + 0.5, 0.0, 1.0);
    if (u_inv == 1) m = 1.0 - m;
    o = s * m;
}", new Aup2Uniforms().I("u_kind", k).F("u_p", invert ? 1 - p : p).F("u_blur", blur).I("u_inv", invert ? 1 : 0).I("u_fixed", c.Flag("縦横比固定") ? 1 : 0));
		}
		#endregion

		#region Staying in motion
		private static void Vibrate(Aup2Context c) {
			double ax = c.V("X", 10), ay = c.V("Y", 10), az = c.V("Z"), period = Math.Max(1, c.V("周期", 1));
			bool vary = c.Flag("ランダムに強さを変える"), complex = c.Flag("複雑に振動");
			double clock = c.Frame / period;
			double k = vary ? 0.25 + 0.75 * c.Random("strength", (long)Math.Floor(clock / 4)) : 1;
			double nx, ny, nz;
			if (complex) {
				nx = Wobble(c, "vx", clock) * 0.7 + Wobble(c, "vx2", clock * 2.7) * 0.3;
				ny = Wobble(c, "vy", clock) * 0.7 + Wobble(c, "vy2", clock * 2.7) * 0.3;
				nz = Wobble(c, "vz", clock);
			} else {
				long step = (long)Math.Floor(clock);
				nx = c.Random("vx", step) * 2 - 1; ny = c.Random("vy", step) * 2 - 1; nz = c.Random("vz", step) * 2 - 1;
			}
			c.Item.X += nx * ax * k; c.Item.Y += ny * ay * k; c.Item.Z += nz * az * k;
		}

		private static void Tremble(Aup2Context c) {
			double amp = c.V("振幅", 5), angle = c.V("角度", 10), every = c.V("間隔");
			long step = every > 0 ? (long)Math.Floor(c.Frame / every) : (long)Math.Floor(c.Frame);
			EachPiece(c, (int i, int n, ref double x, ref double y, ref double z, ref double sx, ref double sy, ref double rx, ref double ry, ref double rz, ref double op) => {
				x += (c.Random("tx", step * 4099 + i) * 2 - 1) * amp;
				y += (c.Random("ty", step * 4099 + i) * 2 - 1) * amp;
				rz += (c.Random("tr", step * 4099 + i) * 2 - 1) * angle;
			});
		}

		private static void Pendulum(Aup2Context c) {
			double speed = c.V("速さ", 2), angle = c.V("角度", 30), shift = c.V("ずらし");
			EachPiece(c, (int i, int n, ref double x, ref double y, ref double z, ref double sx, ref double sy, ref double rx, ref double ry, ref double rz, ref double op) => {
				rz += angle * Math.Sin((c.Time * speed + i * shift / 100.0) * Math.PI * 2 / 2);
			});
		}

		private static void Bounce(Aup2Context c) {
			double speed = c.V("速さ", 20), height = c.V("高さ", 150), angle = c.V("角度") * Math.PI / 180, shift = c.V("ずらし");
			EachPiece(c, (int i, int n, ref double x, ref double y, ref double z, ref double sx, ref double sy, ref double rx, ref double ry, ref double rz, ref double op) => {
				double h = height * Math.Abs(Math.Sin((c.Time * speed / 10 + i * shift / 100.0) * Math.PI));
				x += Math.Sin(angle) * h;
				y -= Math.Cos(angle) * h;
			});
		}

		private static void BackAndForth(Aup2Context c) {
			double speed = c.V("速さ", 20), distance = c.V("距離", 100), angle = c.V("角度") * Math.PI / 180, shift = c.V("ずらし");
			EachPiece(c, (int i, int n, ref double x, ref double y, ref double z, ref double sx, ref double sy, ref double rx, ref double ry, ref double rz, ref double op) => {
				double d = distance * Math.Sin((c.Time * speed / 10 + i * shift / 100.0) * Math.PI);
				x += Math.Cos(angle) * d;
				y += Math.Sin(angle) * d;
			});
		}

		private static void Blink(Aup2Context c) {
			double period = c.V("速さ", 1), share = Math.Clamp(c.V("点滅割合", 50) / 100, 0, 1);
			double on = Math.Max(1, c.V("点灯速度", 100)) / 100, off = Math.Max(1, c.V("消灯速度", 75)) / 100;
			if (period <= 0) return;
			double phase = c.Time / period % 1.0;
			double lit = share;   // lit for this share of each period
			double a;
			if (phase < lit) a = Math.Clamp(phase / Math.Max(1e-6, lit) * on * 4, 0, 1);
			else a = 1 - Math.Clamp((phase - lit) / Math.Max(1e-6, 1 - lit) * off * 4, 0, 1);
			c.Item.Opacity *= a;
		}
		#endregion

		#region Particles
		private static void Particles(Aup2Context c) => Emit(c, false);
		private static void ParticlesInSpace(Aup2Context c) => Emit(c, true);

		// The object's picture sent out again and again: each copy leaves the place the object had when it
		// was sent, flies its way, turns, grows and fades until its life is over. Every copy is worked out
		// from its number and the time alone, so a seek shows the same thing as playing up to there.
		// space: the copies leave along a direction in depth and turn about all three axes.
		private static void Emit(Aup2Context c, bool space) {
			if (!c.Pixels()) return;
			var s = c.Surface!;
			var item = c.Item;
			double rate = Math.Max(0, c.V("出力頻度", 20)), speed = c.V("出力速度", 400), accel = c.V("加速度");
			double spread = c.V("拡散角度", 30) * Math.PI / 180;
			double fade = c.V("透過率") / 100, fadeSpeed = c.V("透過速度") / 100;
			double scale = c.V("拡大率", 100) / 100, grow = c.V("拡大速度") / 100;
			double gravity = c.V("重力"), life = Math.Max(0.01, c.V("生存時間", 1));
			double stopBefore = Math.Max(0, c.V("出力終了調整"));
			bool endClean = c.Flag("終了点で全て消えるように調節する");
			bool anywhere = c.Flag("移動範囲の座標からランダムに出力"), along = c.Flag("出力方向の基準を移動方向にする");
			// 2D: one angle, 0 is up, clockwise. 3D: a turn about each axis of the direction that points up.
			double dir = c.V("出力方向") * Math.PI / 180;
			var aim = space ? Aup2Evaluator.Turn(c.V("出力方向X"), c.V("出力方向Y"), c.V("出力方向Z")) : Matrix4x4.Identity;
			double turnX = space ? c.V("X軸回転") : 0, turnY = space ? c.V("Y軸回転") : 0, turnZ = space ? c.V("Z軸回転") : c.V("回転角");
			double spinX = space ? c.V("X軸回転速度") : 0, spinY = space ? c.V("Y軸回転速度") : 0, spinZ = space ? c.V("Z軸回転速度") : c.V("回転速度");
			string basis = c.S("回転角の基準");
			bool faceWay = space ? basis.Contains("出力") || basis.Contains("移動") : c.Flag("回転角の基準を出力方向にする");
			bool tumble = !space && c.Flag("3Dランダム回転");
			if (space && basis.Contains("カメラ")) item.Billboard = true;

			// the effect places the object itself; a copy stays where the object was when it left
			Aup2Track tx = c.Effect.Track("X"), ty = c.Effect.Track("Y"), tz = c.Effect.Track("Z");
			int seedX = c.Seed("X"), seedY = c.Seed("Y"), seedZ = c.Seed("Z");
			bool moving = tx.IsAnimated || ty.IsAnimated || tz.IsAnimated;
			Vector3 At(double frame) => new Vector3((float)tx.Evaluate(frame, c.Object.Frames, seedX), (float)ty.Evaluate(frame, c.Object.Frames, seedY), (float)tz.Evaluate(frame, c.Object.Frames, seedZ));
			var here = At(c.Frame);
			item.X += here.X; item.Y += here.Y; item.Z += here.Z;
			if (Aup2Evaluator.BlendOf(c.S("合成モード", "通常")) is Aup2Blend blend) item.Blend = blend;

			if (rate <= 0) { c.Skip = true; return; }
			double now = c.Time;
			long newest = (long)Math.Floor(now * rate), oldest = Math.Max(0, (long)Math.Ceiling((now - life) * rate));
			const int most = 2000;
			if (newest - oldest > most) oldest = newest - most;
			int span = Math.Max(1, c.Object.End - c.Object.Start);
			bool camera = item.UnderCamera;
			var parts = new List<Aup2Part>();
			for (long i = oldest; i <= newest; i++) {
				double born = i / rate, age = now - born;
				if (age < 0 || age > life) continue;
				if (endClean && born + life > c.Duration) continue;
				if (stopBefore > 0 && born > c.Duration - stopBefore) continue;
				double opacity = Math.Clamp(1 - fade - fadeSpeed * age, 0, 1);
				double size = Math.Max(0, scale + grow * age);
				if (opacity <= 0 || size <= 0) continue;
				var from = Vector3.Zero;
				var heading = Vector3.Zero;
				if (moving) {
					double f = anywhere ? c.Random("pfrom", i) * span : born * c.Scene.Rate;
					var was = At(f);
					from = was - here;
					if (along) {
						heading = At(f + 1) - was;
						if (heading.LengthSquared() < 1e-9f) heading = was - At(f - 1);
					}
				}
				bool led = heading.LengthSquared() >= 1e-9f;
				Vector3 way;
				if (space) {
					// somewhere inside a cone around the direction
					var axis = Vector3.Normalize(Vector3.Transform(led ? Vector3.Normalize(heading) : new Vector3(0, -1, 0), aim));
					var side = Vector3.Cross(axis, Math.Abs(axis.Z) < 0.9f ? Vector3.UnitZ : Vector3.UnitX);
					side = Vector3.Normalize(side);
					var other = Vector3.Cross(axis, side);
					double off = c.Random("pdir", i) * spread / 2, round = c.Random("pturn", i) * Math.PI * 2;
					way = axis * (float)Math.Cos(off) + (side * (float)Math.Cos(round) + other * (float)Math.Sin(round)) * (float)Math.Sin(off);
				} else {
					double a = dir + (led ? Math.Atan2(heading.X, -heading.Y) : 0) + (c.Random("pdir", i) - 0.5) * spread;
					way = new Vector3((float)Math.Sin(a), (float)-Math.Cos(a), 0);
				}
				double v = speed * age + 0.5 * accel * age * age;
				var p = new Aup2Part {
					Index = (int)(i - oldest), RectX = 0, RectY = 0, RectW = s.W, RectH = s.H,
					X = from.X + way.X * v, Y = from.Y + way.Y * v + 0.5 * gravity * age * age, Z = from.Z + way.Z * v,
					ScaleX = size, ScaleY = size, RotX = turnX + spinX * age, RotY = turnY + spinY * age, RotZ = turnZ + spinZ * age, Opacity = opacity,
				};
				if (tumble) {
					// each copy spins about all three axes, no faster than the speed set
					p.RotX = (c.Random("psx", i) * 2 - 1) * spinZ * age;
					p.RotY = (c.Random("psy", i) * 2 - 1) * spinZ * age;
					p.RotZ = turnZ + (c.Random("psz", i) * 2 - 1) * spinZ * age;
				}
				if (faceWay) p.RotZ += Math.Atan2(way.X, -way.Y) * 180 / Math.PI;
				// without a camera the eye stands 1024 in front of the frame: a copy stays ahead of it
				if (!camera) p.Z = Math.Max(-900, p.Z);
				parts.Add(p);
			}
			foreach (var p in parts) p.Count = parts.Count;
			if (parts.Count == 0) { c.Skip = true; return; }
			item.Parts = parts;
		}
		#endregion

		#region Breaking apart
		private static void Shatter(Aup2Context c) {
			double start = c.V("開始時間"), rate = c.V("再生速度", 1);
			double t = (c.Time - start) * rate;
			if (t <= 0 || !c.Pixels()) return;
			var s = c.Surface!;
			double size = Math.Max(4, c.V("破片サイズ", 50));
			int cols = Math.Clamp((int)Math.Ceiling(s.W / size), 1, 64), rows = Math.Clamp((int)Math.Ceiling(s.H / size), 1, 64);
			double cx = s.W / 2.0 + c.V("中心X"), cy = s.H / 2.0 + c.V("中心Y");
			double speed = c.V("速度", 100), gravity = c.V("重力", 100), lag = c.V("時間差", 100) / 100, reach = c.V("距離影響", 100) / 100;
			double spin = c.V("ランダム回転", 100) / 100, stray = c.V("ランダム方向", 100) / 100;
			double far = Math.Max(1, Math.Sqrt((double)s.W * s.W + (double)s.H * s.H) / 2);
			var parts = new List<Aup2Part>(cols * rows);
			double pw = (double)s.W / cols, ph = (double)s.H / rows;
			int n = cols * rows;
			for (int j = 0; j < rows; j++) {
				for (int i = 0; i < cols; i++) {
					int idx = j * cols + i;
					var p = new Aup2Part { Index = idx, Count = n, RectX = i * pw, RectY = j * ph, RectW = pw, RectH = ph };
					double dx = (i + 0.5) * pw - cx, dy = (j + 0.5) * ph - cy;
					double dist = Math.Sqrt(dx * dx + dy * dy);
					// pieces near the centre leave first
					double local = t - dist / far * lag;
					if (local > 0) {
						double a = Math.Atan2(dy, dx) + (c.Random("dir", idx) - 0.5) * Math.PI * stray;
						double v = speed * 4 * (1 + (1 - dist / far) * reach) * (0.6 + 0.8 * c.Random("vel", idx));
						p.X = Math.Cos(a) * v * local;
						p.Y = Math.Sin(a) * v * local + gravity * 5 * local * local;
						p.Z = (c.Random("z", idx) - 0.5) * v * local * 0.5;
						p.RotX = (c.Random("rx", idx) - 0.5) * 720 * spin * local;
						p.RotY = (c.Random("ry", idx) - 0.5) * 720 * spin * local;
						p.RotZ = (c.Random("rz", idx) - 0.5) * 720 * spin * local;
					}
					parts.Add(p);
				}
			}
			c.Item.Parts = parts;
		}

		private static void Reel(Aup2Context c) {
			double turns = c.V("回転数"), blur = c.V("ブラー", 100) / 100;
			if (turns == 0 || !c.Pixels()) return;
			// the reel slows to a stop at the object's end
			double p = Math.Clamp(c.Time / Math.Max(1e-6, c.Duration), 0, 1);
			double left = turns * (1 - p) * (1 - p);
			double speed = Math.Abs(turns) * 2 * (1 - p) / Math.Max(1e-6, c.Duration * c.Scene.Rate);
			c.Filter(@"uniform float u_off; uniform float u_blur; uniform int u_h;
void main(){
    vec4 s = vec4(0.0);
    for (int i = -8; i <= 8; i++) {
        float k = u_off + u_blur * float(i) / 8.0;
        vec2 q = u_h == 1 ? vec2(fract(v_uv.x + k), v_uv.y) : vec2(v_uv.x, fract(v_uv.y + k));
        s += texture(u_src, q);
    }
    o = s / 17.0;
}", new Aup2Uniforms().F("u_off", left % 1.0).F("u_blur", speed * blur * 0.5).I("u_h", c.Flag("横回転") ? 1 : 0));
		}

		private static void Particlize(Aup2Context c) {
			double start = c.V("開始時間"), rate = c.V("再生速度", 1);
			double t = (c.Time - start) * rate;
			if (t <= 0 || !c.Pixels()) return;
			double speed = c.V("速度", 20), angle = c.V("角度") * Math.PI / 180, grain = c.V("粒子速度", 1), swell = c.V("拡大速度", 1);
			double waver = c.V("ゆらぎ", 100) / 100, warp = c.V("ゆがみ", 100) / 100;
			int grow = (int)Math.Min(600, 40 + t * speed * 12);
			c.Grow(grow);
			var s = c.Surface!;
			// the picture turns to grains from the starting point outward, and the grains drift away
			c.Filter(@"uniform float u_t; uniform vec2 u_origin; uniform float u_speed; uniform vec2 u_dir; uniform float u_grain;
uniform float u_swell; uniform float u_waver; uniform float u_warp;
float h(vec2 p){ return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
void main(){
    vec2 pos = v_uv * u_size;
    float front = u_t * u_speed * 20.0;
    vec2 cell = floor(pos / 4.0);
    float r1 = h(cell), r2 = h(cell + 17.0);
    // each grain remembers where it came from
    float age = max(0.0, u_t - length(pos - u_origin) / max(u_speed * 20.0, 0.001));
    vec2 drift = (u_dir * 40.0 + (vec2(r1, r2) - 0.5) * 80.0 * u_waver) * age * u_grain;
    drift += vec2(sin(pos.y * 0.05 + u_t * 3.0), cos(pos.x * 0.05 + u_t * 3.0)) * 6.0 * u_warp * age;
    vec2 from = pos - drift;
    vec4 s = texture(u_src, from / u_size);
    float gone = clamp((front - length(from - u_origin)) / 60.0, 0.0, 1.0);
    float keep = 1.0 - gone * clamp(age * (0.6 + r1) * (0.5 + u_swell * 0.5), 0.0, 1.0);
    o = s * keep;
}", new Aup2Uniforms().F("u_t", t).V2("u_origin", s.W / 2.0 + c.V("起点X"), s.H / 2.0 + c.V("起点Y")).F("u_speed", Math.Max(0.01, speed))
				.V2("u_dir", Math.Sin(angle), -Math.Cos(angle)).F("u_grain", grain).F("u_swell", swell).F("u_waver", waver).F("u_warp", warp));
		}
		#endregion
	}
}
