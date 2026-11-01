using System;
using System.Linq;
using OpenTaiko.AviUtl2;
using Xunit;

namespace OpenTaikoTests {
	// The .aup2 reader and the frame it works out: no drawing here.
	public class AviUtl2ProjectTests {
		private const string Head =
			"[project]\r\nversion=2011100\r\nfile=C:\\Users\\Someone\\Desktop\\work\\project.aup2\r\ndisplay.scene=0\r\n" +
			"[scene.0]\r\nscene=0\r\nname=Root\r\nvideo.width=1920\r\nvideo.height=1080\r\nvideo.rate=60\r\nvideo.scale=1\r\naudio.rate=44100\r\n";

		private static string Image(int id, int layer, string frame, string draw, string extra = "") =>
			$"[{id}]\r\nlayer={layer}\r\nframe={frame}\r\n" +
			$"[{id}.0]\r\neffect.name=画像ファイル\r\nファイル=C:\\Users\\Someone\\Desktop\\work\\Lines\\a.png\r\n" +
			$"[{id}.1]\r\neffect.name=標準描画\r\n{draw}合成モード=通常\r\n{extra}";

		private static Aup2Evaluator Scene(string body) => new(Aup2Document.Parse(Head + body).Scenes[0]);

		[Fact]
		public void EverySectionAndKeyIsKeptInOrder() {
			string text = Head + Image(0, 2, "10,69", "X=5.00\r\nUnknownKey=a=b,c\r\n") + "[0.2]\r\neffect.name=SomeFutureEffect\r\nfoo=bar\r\n";
			var doc = Aup2Document.Parse(text);
			Assert.Equal(text, doc.Serialize());
			Assert.Equal(2011100, doc.Version);
			var scene = doc.Scenes.Single();
			Assert.Equal(1920, scene.Width);
			Assert.Equal(60, scene.Rate);
			var obj = scene.Objects.Single();
			Assert.Equal(2, obj.Layer);
			Assert.Equal(new[] { 10, 69 }, obj.Frames);
			Assert.Equal(70, scene.FrameCount);
			Assert.Equal("a=b,c", obj.Effects[1].Get("UnknownKey"));
			Assert.Equal("SomeFutureEffect", obj.Effects[2].Name);
		}

		[Fact]
		public void ConstantAndLinearValues() {
			Assert.Equal(42.5, Aup2Track.Parse("42.50").Evaluate(3, new[] { 0, 10 }));
			var t = Aup2Track.Parse("-100.00,100.00,直線移動,0");
			Assert.Equal(Aup2Motion.Linear, t.Motion);
			Assert.Equal(-100, t.Evaluate(0, new[] { 0, 10 }), 6);
			Assert.Equal(0, t.Evaluate(5, new[] { 0, 10 }), 6);
			Assert.Equal(100, t.Evaluate(10, new[] { 0, 10 }), 6);
			Assert.Equal(100, t.Evaluate(99, new[] { 0, 10 }), 6);   // past the end holds the end
			Assert.Equal("通常", Aup2Track.Parse("通常").Text);
		}

		[Fact]
		public void TimeControlHandlesCountFromTheirOwnEnd() {
			var frames = new[] { 0, 100 };
			// second handle pulled all the way: fast first, slow last
			var slowEnd = Aup2Track.Parse("0.00,100.00,直線移動(時間制御),0|0,0,1,0");
			Assert.True(slowEnd.TimeControl);
			Assert.Equal(new[] { 0.0, 0.0, 1.0, 0.0 }, slowEnd.Curve);
			Assert.True(slowEnd.Evaluate(50, frames) > 75);
			// the same handle on the other end: slow first, and the first curve read backwards
			var slowStart = Aup2Track.Parse("0.00,100.00,直線移動(時間制御),0|0,0,0,1");
			Assert.True(slowStart.Evaluate(50, frames) < 25);
			Assert.Equal(30, slowStart.Evaluate(slowEnd.Evaluate(30, frames), frames), 3);
			// no handles is a straight line
			Assert.Equal(40, Aup2Track.Parse("0.00,100.00,直線移動(時間制御),0|0,0,0,0").Evaluate(40, frames), 3);
			Assert.Equal(0, slowEnd.Evaluate(0, frames), 6);
			Assert.Equal(100, slowEnd.Evaluate(100, frames), 6);
		}

		// measured on a video AviUtl2 rendered from a project using these settings
		[Fact]
		public void MatchesWhatAviUtlRendered() {
			var frames = new[] { 0, 59 };
			// "0|0,1,0,0": 58 percent of the way after 10 of 59 frames, 72 after 20, about 90 after 40
			var fast = Aup2Track.Parse("0.00,100.00,直線移動(時間制御),0|0,1,0,0");
			Assert.InRange(fast.Evaluate(10, frames), 54, 64);
			Assert.InRange(fast.Evaluate(20, frames), 68, 80);
			Assert.InRange(fast.Evaluate(40, frames), 88, 98);
			// a turn about X brings the top edge towards the viewer, one about Y the right edge
			var top = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(0, -100, 0), Aup2Evaluator.Turn(30, 0, 0));
			Assert.True(top.Z < 0);
			var right = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(100, 0, 0), Aup2Evaluator.Turn(0, 30, 0));
			Assert.True(right.Z < 0);
			// a turn about Z is clockwise: the top goes to the right
			var turned = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(0, -100, 0), Aup2Evaluator.Turn(0, 0, 90));
			Assert.Equal(100, turned.X, 3);
		}

		[Fact]
		public void APlaybackRangeSaysWhereTheSoundIs() {
			string audio = "[3]\r\nlayer=0\r\nframe=100,199\r\n[3.0]\r\neffect.name=音声ファイル\r\n再生位置=5.000,7.000,再生範囲,0\r\n再生速度=100.00\r\nファイル=C:\\a.ogg\r\n[3.1]\r\neffect.name=音声再生\r\n音量=100.00\r\n左右=-50.00\r\n";
			var a = Scene(audio).Evaluate(100 + 49.5).Audio.Single();
			Assert.Equal(6000, a.OffsetMs, 3);
			Assert.Equal(-0.5, a.Pan, 6);
		}

		[Fact]
		public void MiddlePointsSplitTheMovement() {
			var frames = new[] { 0, 10, 30 };
			var t = Aup2Track.Parse("0.00,100.00,50.00,直線移動,0");
			Assert.Equal(50, t.Evaluate(5, frames), 6);
			Assert.Equal(100, t.Evaluate(10, frames), 6);
			Assert.Equal(75, t.Evaluate(20, frames), 6);
			Assert.Equal(50, t.Evaluate(30, frames), 6);
			var jump = Aup2Track.Parse("0.00,100.00,50.00,瞬間移動,0");
			Assert.Equal(0, jump.Evaluate(9, frames), 6);
			Assert.Equal(100, jump.Evaluate(10, frames), 6);
		}

		[Fact]
		public void OtherMovementsStayInsideTheirRange() {
			var frames = new[] { 0, 60 };
			foreach (string v in new[] { "0.00,10.00,反復移動,4|15", "0.00,10.00,回転,4|360", "0.00,10.00,ランダム移動,4|15", "0.00,10.00,補間移動,3", "0.00,10.00,加減速移動,0" }) {
				var t = Aup2Track.Parse(v);
				Assert.NotEqual(Aup2Motion.Unknown, t.Motion);
				for (int f = 0; f <= 60; f++) Assert.InRange(t.Evaluate(f, frames, 7), -1e-9, 10 + 1e-9);
			}
			var repeat = Aup2Track.Parse("0.00,10.00,反復移動,4|15");
			Assert.Equal(15, repeat.Setting);
			Assert.Equal(10, repeat.Evaluate(15, frames), 6);
			Assert.Equal(0, repeat.Evaluate(30, frames), 6);
			var random = Aup2Track.Parse("0.00,10.00,ランダム移動,4|15");
			// a new value every 15 frames, reached in a straight line
			double r0 = random.Evaluate(0, frames, 1), r15 = random.Evaluate(15, frames, 1);
			Assert.Equal((r0 + r15) / 2, random.Evaluate(7.5, frames, 1), 6);
		}

		[Fact]
		public void ObjectsShowOnlyInTheirFramesInLayerOrder() {
			var ev = Scene(Image(0, 5, "10,19", "X=100.00\r\nY=-50.00\r\n透明度=0.00\r\n") + Image(1, 1, "0,29", "透明度=0.00\r\n"));
			Assert.Empty(ev.Unsupported);
			Assert.Single(ev.Evaluate(5).Draws);
			var both = ev.Evaluate(10).Draws;
			Assert.Equal(new[] { 1, 0 }, both.Select(d => d.ObjectId).ToArray());   // lower layers first
			Assert.Equal(100, both[1].X);
			Assert.Equal(-50, both[1].Y);
			Assert.Equal(2, ev.Evaluate(19.9).Draws.Count);
			Assert.Single(ev.Evaluate(20).Draws);
			Assert.Empty(ev.Evaluate(30).Draws);
		}

		[Fact]
		public void FullyTransparentObjectsAreSkipped() {
			var ev = Scene(Image(0, 0, "0,10", "透明度=100.00,0.00,直線移動,0\r\n"));
			Assert.Empty(ev.Evaluate(0).Draws);
			Assert.Equal(0.5, ev.Evaluate(5).Draws.Single().Opacity, 6);
		}

		[Fact]
		public void ClippingKeepsTheRestOfThePictureInPlace() {
			string clip = "[0.2]\r\neffect.name=クリッピング\r\n上=100\r\n下=0\r\n左=0\r\n右=0\r\n中心の位置を変更=0\r\n";
			var item = Scene(Image(0, 0, "0,10", "X=10.00\r\n透明度=0.00\r\n", clip)).Evaluate(0).Draws.Single();
			Assert.True(item.Place(400, 300, 1920, 1080, out double cx, out double cy, out double cw, out double ch, out double x, out double y));
			Assert.Equal((0.0, 100.0, 400.0, 200.0), (cx, cy, cw, ch));
			// the lower 200 px of a picture centred 10 px right of the frame's centre
			Assert.Equal(970, x, 6);
			Assert.Equal(540 + 50, y, 6);

			string recentre = clip.Replace("中心の位置を変更=0", "中心の位置を変更=1");
			item = Scene(Image(0, 0, "0,10", "透明度=0.00\r\n", recentre)).Evaluate(0).Draws.Single();
			item.Place(400, 300, 1920, 1080, out _, out _, out _, out _, out x, out y);
			Assert.Equal((960.0, 540.0), (x, y));

			string all = clip.Replace("上=100", "上=300");
			item = Scene(Image(0, 0, "0,10", "透明度=0.00\r\n", all)).Evaluate(0).Draws.Single();
			Assert.False(item.Place(400, 300, 1920, 1080, out _, out _, out _, out _, out _, out _));
		}

		[Fact]
		public void RotationTurnsTheCroppedPartAboutThePictureCentre() {
			string clip = "[0.2]\r\neffect.name=クリッピング\r\n上=0\r\n下=0\r\n左=200\r\n右=0\r\n中心の位置を変更=0\r\n";
			var item = Scene(Image(0, 0, "0,10", "Z軸回転=90.00\r\n拡大率=200.000\r\n透明度=0.00\r\n", clip)).Evaluate(0).Draws.Single();
			item.Place(400, 300, 1920, 1080, out _, out _, out _, out _, out double x, out double y);
			// the right half sits 100 px right of the centre; doubled and turned a quarter clockwise, 200 px below
			Assert.Equal(960, x, 6);
			Assert.Equal(740, y, 6);
			Assert.Equal(2, item.ScaleX, 6);
			Assert.Equal(90, item.Rotation, 6);
		}

		[Fact]
		public void GroupControlMovesTheLayersBelowIt() {
			string group = "[5]\r\nlayer=0\r\nframe=0,10\r\n[5.0]\r\neffect.name=グループ制御\r\nX=100.00\r\nY=0.00\r\n拡大率=200.00\r\nZ軸回転=0.00\r\n対象レイヤー数=1\r\n";
			var ev = Scene(group + Image(0, 1, "0,10", "X=10.00\r\n透明度=0.00\r\n") + Image(1, 2, "0,10", "X=10.00\r\n透明度=0.00\r\n"));
			var draws = ev.Evaluate(0).Draws;
			Assert.Equal(120, draws[0].X, 6);
			Assert.Equal(2, draws[0].ScaleX, 6);
			Assert.Equal(10, draws[1].X, 6);   // past the group's one layer
		}

		[Fact]
		public void AGroupControlsEffectsRunOnItsObjects() {
			// a fade on the group control, on the group's own clock (it starts 30 frames before the picture)
			string group = "[5]\r\nlayer=0\r\nframe=0,119\r\n[5.0]\r\neffect.name=グループ制御\r\n対象レイヤー数=1\r\n[5.1]\r\neffect.name=フェード\r\nイン=1.00\r\nアウト=0.00\r\n";
			var ev = Scene(group + Image(0, 1, "30,119", "") + Image(1, 2, "30,119", ""));
			var draws = ev.Evaluate(30).Draws;
			Assert.Equal(0.5, draws[0].Opacity, 6);
			Assert.Equal(1, draws[1].Opacity, 6);   // past the group's one layer
		}

		private const string CameraHead = "[9]\r\nlayer=0\r\nframe=0,119\r\n[9.0]\r\neffect.name=カメラ制御\r\nX=0.00\r\nY=0.00\r\nZ=-1024.00\r\n目標X=100.00\r\n目標Y=0.00\r\n目標Z=0.00\r\n";

		[Fact]
		public void ACameraLooksAtItsTargetLayer() {
			string cam = CameraHead + "目標レイヤー=3\r\n焦点レイヤー=2\r\n傾き=0.00\r\n深度ぼけ=0.00\r\n視野角=0.00\r\n対象レイヤー数=0\r\n";
			var ev = Scene(cam + Image(1, 1, "0,119", "X=0.00\r\nY=0.00\r\nZ=476.00\r\n") + Image(2, 2, "0,119", "X=500.00\r\nY=-40.00\r\nZ=30.00\r\n"));
			var r = ev.Evaluate(10);
			var c = Assert.Single(r.Cameras);
			// layer 3 is the third from the top: the picture at 500, -40, 30, and the target values count from it
			Assert.Equal(600, c.Target.X, 3);
			Assert.Equal(-40, c.Target.Y, 3);
			Assert.Equal(30, c.Target.Z, 3);
			Assert.Equal(1500, c.FocusDistance, 3);
			Assert.All(r.Draws, d => Assert.True(d.UnderCamera));
			Assert.Empty(ev.Unsupported);
		}

		[Fact]
		public void NoAngleIsThePlainView() {
			Assert.Equal(1024, new Aup2Camera().ScreenDistance(1080), 6);
			// on a frame 1080 high, 55.6 degrees shows the same as no angle
			Assert.InRange(new Aup2Camera { FieldOfView = 55.6 }.ScreenDistance(1080), 1020, 1028);
			Assert.True(new Aup2Camera { FieldOfView = 21 }.ScreenDistance(1080) > 2900);
		}

		[Fact]
		public void ASceneMayHaveABackgroundColour() {
			Assert.Null(Scene("").Scene.Background);
			var doc = Aup2Document.Parse(Head.Replace("video.scale=1\r\n", "video.scale=1\r\nvideo.background=ff8000\r\n"));
			var bg = doc.Scenes[0].Background;
			Assert.NotNull(bg);
			Assert.Equal(1, bg!.Value.X, 3);
			Assert.Equal(128 / 255.0, bg.Value.Y, 3);
			Assert.Equal(0, bg.Value.Z, 3);
			// an empty setting is no background
			Assert.Null(Aup2Document.Parse(Head.Replace("video.scale=1\r\n", "video.scale=1\r\nvideo.background=\r\n")).Scenes[0].Background);
		}

		[Fact]
		public void UnderACameraAFilterObjectWorksOnTheObjectAboveIt() {
			string cam = CameraHead + "目標レイヤー=0\r\n焦点レイヤー=0\r\n傾き=0.00\r\n深度ぼけ=0.00\r\n視野角=0.00\r\n対象レイヤー数=0\r\n";
			string move = "[7]\r\nlayer=3\r\nframe=0,119\r\n[7.0]\r\neffect.name=座標\r\nX=50.00\r\nY=0.00\r\nZ=0.00\r\n";
			var draws = Scene(cam + Image(1, 1, "0,119", "X=0.00\r\n") + Image(2, 2, "0,119", "X=10.00\r\n") + move).Evaluate(10).Draws;
			// the object just above moves; the one before it and the frame do not
			Assert.Equal(2, draws.Count);
			Assert.All(draws, d => Assert.Equal(Aup2ItemKind.Image, d.Kind));
			Assert.Equal(0, draws[0].X, 6);
			Assert.Equal(60, draws[1].X, 6);
			// with no camera the same object works on the frame so far
			var flat = Scene(Image(1, 1, "0,119", "X=0.00\r\n") + move.Replace("layer=3", "layer=2")).Evaluate(10).Draws;
			Assert.Contains(flat, d => d.Kind == Aup2ItemKind.Filter);
		}

		[Fact]
		public void CameraEffectsMoveTheEye() {
			string cam = CameraHead.Replace("目標X=100.00", "目標X=0.00") + "目標レイヤー=0\r\n焦点レイヤー=0\r\n傾き=0.00\r\n深度ぼけ=0.00\r\n視野角=0.00\r\n対象レイヤー数=0\r\n"
				+ "[9.1]\r\neffect.name=目標中心回転\r\n水平回転=90.00\r\n垂直回転=0.00\r\n距離=0.00\r\n";
			var c = Scene(cam).Evaluate(10).Cameras.Single();
			Assert.Equal(1024, Math.Abs(c.Eye.X), 2);
			Assert.Equal(0, c.Eye.Z, 2);
			Assert.Equal(1024, (c.Eye - c.Target).Length(), 2);
		}

		[Fact]
		public void LightsBelongToTheCamera() {
			string cam = CameraHead + "目標レイヤー=0\r\n焦点レイヤー=0\r\n傾き=0.00\r\n深度ぼけ=0.00\r\n視野角=0.00\r\n対象レイヤー数=0\r\n";
			string light = "[4]\r\nlayer=1\r\nframe=0,119\r\n[4.0]\r\neffect.name=点光源(カメラ制御)\r\nX=10.00\r\nY=20.00\r\nZ=-300.00\r\n明るさ=50.00\r\n減衰=0.00\r\n色=ff0000\r\n";
			var ev = Scene(cam + light);
			var l = Assert.Single(ev.Evaluate(5).Lights);
			Assert.Equal(Aup2LightKind.Point, l.Kind);
			Assert.Equal(0.5, l.Brightness, 6);
			Assert.Equal(-300, l.Position.Z, 3);
			Assert.Equal(1, l.Color.X, 3);
			Assert.Equal(0, l.Color.Y, 3);
			Assert.Empty(ev.Unsupported);
			// with no camera a light lights nothing
			Assert.Empty(Scene(light).Evaluate(5).Lights);
		}

		[Fact]
		public void SoundEffectsChangeTheVolume() {
			string group = "[5]\r\nlayer=0\r\nframe=0,599\r\n[5.0]\r\neffect.name=グループ制御(音声)\r\n音量=50.00\r\n左右=0.00\r\n対象レイヤー数=0\r\n";
			string audio = "[3]\r\nlayer=1\r\nframe=0,119\r\n[3.0]\r\neffect.name=音声ファイル\r\n再生位置=0.000\r\n再生速度=100.00\r\nファイル=C:\\a.ogg\r\n[3.1]\r\neffect.name=音声再生\r\n音量=100.00\r\n左右=0.00\r\n[3.2]\r\neffect.name=音量フェード\r\nイン=1.00\r\nアウト=0.00\r\n";
			var ev = Scene(group + audio);
			Assert.Equal(0.25, ev.Evaluate(30).Audio.Single().Volume, 3);
			Assert.Equal(0.5, ev.Evaluate(90).Audio.Single().Volume, 3);
			Assert.Empty(ev.Unsupported);
		}

		[Fact]
		public void TheBundledCameraProjectIsFullyRead() {
			string dir = AppContext.BaseDirectory;
			string? path = null;
			for (int i = 0; i < 8 && dir != null; i++, dir = System.IO.Path.GetDirectoryName(dir)) {
				string p = System.IO.Path.Combine(dir!, "OpenTaiko", "Global", "Modules", "Stages", "demo_aviutl2", "Camera", "camera.aup2");
				if (System.IO.File.Exists(p)) { path = p; break; }
			}
			if (path == null) return;
			var scene = Aup2Document.Parse(System.IO.File.ReadAllText(path)).Scenes[0];
			var ev = new Aup2Evaluator(scene);
			foreach (int f in new[] { 1, 60, 150, 287 }) {
				var r = ev.Evaluate(f);
				Assert.Single(r.Cameras);
				Assert.Equal(3, r.Draws.Count(d => d.UnderCamera && d.Kind == Aup2ItemKind.Image));
				var model = Assert.Single(r.Draws, d => d.Kind == Aup2ItemKind.Model);
				Assert.EndsWith("cube.obj", model.Path);
				Assert.True(model.UnderCamera);
			}
			// the camera ends looking at the clear text plus 600 across and 840 up, level, at 21 degrees
			var last = ev.Evaluate(288).Cameras[0];
			Assert.Equal(976.67 + 600, last.Target.X, 1);
			Assert.Equal(570.22 - 840, last.Target.Y, 1);
			Assert.Equal(21, last.FieldOfView, 1);
			Assert.Equal(0, last.Roll, 1);
			Assert.Contains("object: モデルファイル", ev.Approximated);
			Assert.Empty(ev.Unsupported);
		}

		[Fact]
		public void UnknownPartsAreListedOnce() {
			string odd = "[0.2]\r\neffect.name=謎のフィルタ\r\n範囲=5\r\n";
			string thing = "[9]\r\nlayer=3\r\nframe=0,10\r\n[9.0]\r\neffect.name=謎のオブジェクト\r\n[9.1]\r\neffect.name=標準描画\r\n合成モード=通常\r\n";
			var ev = Scene(Image(0, 0, "0,10", "X=0.00,1.00,謎の移動,0\r\n", odd) + Image(1, 1, "0,10", "", odd.Replace("[0.2]", "[1.2]")) + thing);
			Assert.Equal(new[] { "movement: 謎の移動", "effect: 謎のフィルタ", "object: 謎のオブジェクト" }, ev.Unsupported.ToArray());
			Assert.Equal(2, ev.Evaluate(0).Draws.Count);   // the pictures still draw without the unknown effect
		}

		[Fact]
		public void EveryBlendModeOfTheFormatHasAName() {
			foreach (string name in new[] { "通常", "加算", "減算", "乗算", "スクリーン", "オーバーレイ", "比較(明)", "比較(暗)", "輝度", "色差", "陰影", "明暗", "差分" }) {
				var item = Scene(Image(0, 0, "0,10", "").Replace("合成モード=通常", "合成モード=" + name)).Evaluate(0).Draws.Single();
				Assert.True(name == "通常" ? item.Blend == Aup2Blend.Normal : item.Blend != Aup2Blend.Normal, name);
			}
		}

		[Fact]
		public void TimeControlMovesTheClockOfTheLayersBelow() {
			string control = "[5]\r\nlayer=0\r\nframe=0,99\r\n[5.0]\r\neffect.name=時間制御\r\n位置=50.00\r\n対象レイヤー数=1\r\n";
			var ev = Scene(control + Image(0, 1, "0,99", "X=0.00,990.00,直線移動,0\r\n") + Image(1, 2, "0,99", "X=0.00,990.00,直線移動,0\r\n"));
			var draws = ev.Evaluate(10).Draws;
			Assert.Equal(495, draws[0].X, 6);   // held at the middle of the control
			Assert.Equal(100, draws[1].X, 6);   // past the control's one layer
		}

		[Fact]
		public void CameraControlMarksItsLayers() {
			string camera = "[5]\r\nlayer=0\r\nframe=0,10\r\n[5.0]\r\neffect.name=カメラ制御\r\nX=0.00\r\nY=0.00\r\nZ=-1024.00\r\n対象レイヤー数=1\r\n";
			var draws = Scene(camera + Image(0, 1, "0,10", "Z=200.00\r\n") + Image(1, 2, "0,10", "")).Evaluate(0).Draws;
			Assert.True(draws[0].UnderCamera);
			Assert.Equal(200, draws[0].Z);
			Assert.False(draws[1].UnderCamera);
		}

		[Fact]
		public void ShapesAndTextsAreObjects() {
			string shape = "[2]\r\nlayer=0\r\nframe=0,59\r\n[2.0]\r\neffect.name=図形\r\n図形の種類=円\r\nサイズ=200\r\n縦横比=50.00\r\nライン幅=4000\r\n色=ff8000\r\n[2.1]\r\neffect.name=標準描画\r\n合成モード=通常\r\n";
			string text = "[3]\r\nlayer=1\r\nframe=0,59\r\n[3.0]\r\neffect.name=テキスト\r\nサイズ=40.00\r\n表示速度=2.00\r\n文字色=ffffff\r\n影・縁色=000000\r\n文字装飾=縁取り文字\r\n文字揃え=中央揃え[中]\r\nテキスト=ab<#ff0000>c\\nd\r\n[3.1]\r\neffect.name=標準描画\r\n合成モード=通常\r\n";
			var ev = Scene(shape + text);
			Assert.Empty(ev.Unsupported);
			var draws = ev.Evaluate(30).Draws;
			Assert.Equal(Aup2ItemKind.Shape, draws[0].Kind);
			Assert.Equal((100.0, 200.0), (draws[0].ShapeWidth, draws[0].ShapeHeight));
			Assert.Equal((255, 128, 0), (draws[0].R, draws[0].G, draws[0].B));
			var style = draws[1].Text!;
			Assert.Equal("ab<#ff0000>c\nd", style.Text);
			// two letters a second, half a second in: one letter
			Assert.Equal(1, Aup2TextRaster.ShownCount(style));
			using var all = Aup2TextRaster.Render(new Aup2TextStyle { Text = style.Text, Size = 40, Align = style.Align, Decoration = style.Decoration });
			Assert.Equal(4, all.Letters.Count);   // the tag is not a letter
			Assert.True(all.Bitmap.Width > 40 && all.Bitmap.Height > 70);
			Assert.InRange(all.AnchorX, all.Bitmap.Width * 0.3, all.Bitmap.Width * 0.7);
		}

		[Fact]
		public void EntrancesStartAwayAndEndInPlace() {
			string fx = "[0.2]\r\neffect.name=画面外から登場\r\n時間=0.5\r\n角度=90.0\r\n数=1\r\nランダム方向=0\r\n";
			var ev = Scene(Image(0, 0, "0,119", "X=10.00\r\n", fx));
			Assert.True(ev.Evaluate(0).Draws.Single().X > 1000);   // from the right, off the frame
			Assert.Equal(10, ev.Evaluate(60).Draws.Single().X, 6);
			string fade = "[0.2]\r\neffect.name=フェード\r\nイン=1.00\r\nアウト=0.00\r\n";
			Assert.Equal(0.5, Scene(Image(0, 0, "0,119", "", fade)).Evaluate(30).Draws.Single().Opacity, 6);
		}

		[Fact]
		public void AnObjectOfFiltersWorksOnTheFrame() {
			string filter = "[4]\r\nlayer=5\r\nframe=0,10\r\n[4.0]\r\neffect.name=反転\r\n上下反転=0\r\n左右反転=0\r\n輝度反転=1\r\n";
			var ev = Scene(Image(0, 0, "0,10", "") + filter);
			Assert.Empty(ev.Unsupported);
			Assert.Equal(new[] { Aup2ItemKind.Image, Aup2ItemKind.Filter }, ev.Evaluate(0).Draws.Select(d => d.Kind).ToArray());
		}

		[Fact]
		public void SoundsReportTheirOwnTime() {
			string audio = "[3]\r\nlayer=0\r\nframe=60,179\r\n[3.0]\r\neffect.name=音声ファイル\r\nファイル=C:\\a.ogg\r\n再生位置=1.00\r\n再生速度=100.00\r\nループ再生=0\r\n[3.1]\r\neffect.name=音声再生\r\n音量=50.00\r\n";
			var ev = Scene(audio);
			Assert.Empty(ev.Evaluate(0).Audio);
			var a = ev.Evaluate(120).Audio.Single();
			Assert.Equal(2000, a.OffsetMs, 6);
			Assert.Equal(0.5, a.Volume, 6);
		}
	}
}
