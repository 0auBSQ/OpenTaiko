using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using OpenTaiko.AviUtl2;
using Xunit;
using Xunit.Abstractions;

namespace OpenTaikoTests {
	// The script files of a project and the Lua that runs them: no drawing here, a stand-in takes the
	// host's place and counts what the scripts ask for.
	public class AviUtl2ScriptTests : IDisposable {
		private const string Head =
			"[project]\r\nversion=2011100\r\nfile=C:\\Users\\Someone\\Desktop\\work\\project.aup2\r\ndisplay.scene=0\r\n" +
			"[scene.0]\r\nscene=0\r\nname=Root\r\nvideo.width=1920\r\nvideo.height=1080\r\nvideo.rate=60\r\nvideo.scale=1\r\naudio.rate=44100\r\n";

		private readonly ITestOutputHelper _output;
		private readonly string _folder;

		public AviUtl2ScriptTests(ITestOutputHelper output) {
			_output = output;
			_folder = Path.Combine(Path.GetTempPath(), "aup2-scripts-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_folder);
		}

		public void Dispose() {
			try { Directory.Delete(_folder, true); } catch (IOException) { }
		}

		private sealed class FakeHost : IAup2Host {
			public int Polys, Triangles, Draws, Figures, Reads, Composed;
			public List<string> Texts = new();
			public List<(string dst, string src)> Copies = new();
			public List<Vector3> Corners = new();
			public List<double> DrawX = new();
			public byte[] Written;
			private readonly HashSet<string> _buffers = new();

			public Aup2Gpu Gpu => null;
			public Aup2Camera CurrentCamera => null;

			public bool Materialize(Aup2Context c) {
				c.Surface ??= new Aup2Surface { W = Math.Max(1, (int)c.Item.ShapeWidth), H = Math.Max(1, (int)c.Item.ShapeHeight) };
				return true;
			}
			public void Compose(Aup2Context c) => Composed++;
			public Aup2Surface FrameCopy() => null;
			public void ReplaceFrame(Aup2Surface s) { }
			public void BeginCamera(Aup2Camera camera) { }
			public void EndCamera() { }
			public void AddLight(Aup2Light light) { }

			public void ScriptDraw(Aup2Context c, double ox, double oy, double oz, double zoom, double alpha, double rx, double ry, double rz) {
				Draws++;
				DrawX.Add(c.Item.X + ox);
				if (!c.Flat) c.Drawn = true;
			}
			public void ScriptPoly(Aup2Context c, ReadOnlySpan<Vector3> corners, ReadOnlySpan<Vector2> uv, double alpha) {
				Polys++;
				if (corners.Length == 3) Triangles++;
				foreach (var p in corners) Corners.Add(p);
				if (!c.Flat) c.Drawn = true;
			}
			public void SetDrawTarget(Aup2Context c, bool temp, int w, int h) => c.Flat = temp;
			public bool CopyBuffer(Aup2Context c, string dst, string src) {
				bool has = src == "object" ? Materialize(c) : _buffers.Contains(src);
				if (!has) return false;
				Copies.Add((dst, src));
				if (dst == "object") c.Surface = new Aup2Surface { W = 8, H = 8 };
				else _buffers.Add(dst);
				return true;
			}
			public bool LoadFigure(Aup2Context c, string kind, int rgb, double size, double line) {
				Figures++;
				c.Surface = new Aup2Surface { W = Math.Max(1, (int)size), H = Math.Max(1, (int)size) };
				return true;
			}
			public bool LoadText(Aup2Context c, Aup2TextStyle style) {
				Texts.Add(style.Text);
				c.Surface = new Aup2Surface { W = 100, H = 40 };
				return true;
			}
			public bool LoadImage(Aup2Context c, string path) => false;
			public byte[] ReadPixels(Aup2Context c, out int w, out int h) {
				w = h = 0;
				if (c.Surface == null) return null;
				Reads++;
				w = c.Surface.W; h = c.Surface.H;
				var bytes = new byte[w * h * 4];
				for (int i = 0; i < bytes.Length; i += 4) { bytes[i] = 0x10; bytes[i + 1] = 0x20; bytes[i + 2] = 0x30; bytes[i + 3] = 255; }
				return bytes;
			}
			public void WritePixels(Aup2Context c, byte[] rgba, int w, int h) {
				Written = rgba;
				c.Surface = new Aup2Surface { W = w, H = h };
			}
		}

		private string Write(string name, string text, Encoding encoding = null) {
			string path = Path.Combine(_folder, name);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllBytes(path, (encoding ?? new UTF8Encoding(false)).GetBytes(text));
			return path;
		}

		// a square on layer 1 for two seconds, drawn at (100, 0), with these effects after its drawing
		private static string Shape(params string[] effects) {
			var sb = new StringBuilder("[0]\r\nlayer=1\r\nframe=0,119\r\n" +
				"[0.0]\r\neffect.name=図形\r\n図形の種類=四角形\r\nサイズ=64\r\n縦横比=0.00\r\nライン幅=4000\r\n色=ff8040\r\n" +
				"[0.1]\r\neffect.name=標準描画\r\nX=100.00\r\nY=0.00\r\nZ=0.00\r\n拡大率=100.000\r\n透明度=0.00\r\n合成モード=通常\r\n");
			for (int i = 0; i < effects.Length; i++) sb.Append("[0.").Append(i + 2).Append("]\r\n").Append(effects[i]);
			return sb.ToString();
		}

		private static Aup2Item Play(Aup2ScriptLibrary lib, FakeHost host, string body, double frame) {
			var ev = new Aup2Evaluator(Aup2Document.Parse(Head + body).Scenes[0], lib);
			Assert.Empty(ev.Unsupported);
			return ev.Evaluate(frame, host).Draws.Single();
		}

		[Fact]
		public void SettingsInTheNewSyntax() {
			var file = Aup2ScriptFile.Parse(
				"--track@vx:X速度,-10,10,2.5,0.01\n--check@grav:重力,0\n--check@fast:速度,true\n--color@col:図形色,0xff8000\n" +
				"--file@path:画像ファイル\n--select@deco:装飾タイプ=3,標準文字=0,縁取り文字=3\n--value@pts:座標,{0,0,0}\n" +
				"--text@txt:Group::テキスト,一行目\\n二行目\n--trackgroup@vx,vx:Group\n--information:sample\n" +
				"obj.ox = obj.ox + vx * obj.time\n", "move.anm2");
			Assert.False(file.Legacy);
			Assert.False(file.Multi);
			Assert.Equal("move", file.Stem);
			var s = file.Sections.Single();
			Assert.Equal("move", s.Name);
			Assert.Equal("sample", s.Information);
			Assert.Equal(new[] { "vx", "grav", "fast", "col", "path", "deco", "pts", "txt" }, s.Settings.Select(x => x.Variable));
			var vx = s.Find("vx");
			Assert.Equal(Aup2SettingKind.Track, vx.Kind);
			Assert.Equal("X速度", vx.Label);
			Assert.Equal((-10, 10, 2.5, 0.01), (vx.Min, vx.Max, vx.Number, vx.Step));
			Assert.False(s.Find("grav").Boolean);
			Assert.True(s.Find("fast").Boolean);
			Assert.Equal(1, s.Find("fast").Number);
			Assert.Equal(0xff8000, s.Find("col").Number);
			Assert.Equal(Aup2SettingKind.File, s.Find("path").Kind);
			var deco = s.Find("deco");
			Assert.Equal("装飾タイプ", deco.Label);
			Assert.Equal("3", deco.Default);
			Assert.Equal(new KeyValuePair<string, string>("縁取り文字", "3"), deco.Choices[1]);
			Assert.Equal("{0,0,0}", s.Find("pts").Default);
			Assert.Equal("一行目\n二行目", s.Find("txt").Default);
			Assert.Equal("テキスト", s.Find("txt").ShortLabel);
		}

		[Fact]
		public void SettingsInTheOlderSyntaxAndSeveralScriptsInOneShiftJisFile() {
			string text =
				"-- a note before any script\r\n" +
				"@回る\r\n--track0:速さ,-100,100,10\r\n--track1:幅,0,500,50,0.01\r\n--check0:逆回転,1\r\n" +
				"--dialog:分割数,local div=32;表示/chk,local show=0;点,pos={{0,100}};名前,name=\"a;b\";\r\n" +
				"--color:0xffffff\r\n--file:\r\n--param:extra={}\r\nobj.rz = obj.rz + obj.track0 * obj.time\r\n" +
				"@跳ねる\r\n--track0:高さ,0,500,100\r\nobj.oy = obj.oy - obj.track0\r\n";
			string path = Write("@動き.anm", text, CodePagesEncodingProvider.Instance.GetEncoding(932));
			var file = Aup2ScriptFile.Load(path);
			Assert.True(file.Legacy);
			Assert.True(file.Multi);
			Assert.Equal("動き", file.Stem);
			Assert.Equal(new[] { "回る", "跳ねる" }, file.Sections.Select(x => x.Name));

			var spin = file.Sections[0];
			Assert.Equal(3, spin.FirstLine);
			Assert.Equal("速さ", spin.Track(0).Label);
			Assert.Equal(10, spin.Track(0).Number);
			Assert.Equal(0.01, spin.Track(1).Step);
			var check = spin.Settings.Single(x => x.Kind == Aup2SettingKind.Check);
			Assert.Equal((0, true, 1.0, "逆回転"), (check.Slot, check.Boolean, check.Number, check.Label));
			var dialog = spin.Settings.Where(x => x.Kind == Aup2SettingKind.Dialog).ToArray();
			Assert.Equal(new[] { "div", "show", "pos", "name" }, dialog.Select(x => x.Variable));
			Assert.Equal(new[] { "32", "0", "{{0,100}}", "\"a;b\"" }, dialog.Select(x => x.Default));
			Assert.Equal(new[] { true, true, false, false }, dialog.Select(x => x.Local));
			Assert.Equal(("表示", "chk"), (dialog[1].Label, dialog[1].Hint));
			Assert.Contains("表示/chk", dialog[1].Keys());
			Assert.Equal(0xffffff, spin.Find("color").Number);
			Assert.Equal(Aup2SettingKind.File, spin.Find("file").Kind);
			Assert.Equal(new[] { "extra={}" }, spin.Preamble);
			Assert.Contains("track0", spin.Track(0).Keys());
			Assert.Equal(100, file.Sections[1].Track(0).Number);
			Assert.StartsWith("--track0:高さ", file.Sections[1].Code);
		}

		[Fact]
		public void NamesOutsideAsciiAndOldEscapesAreMadeReadable() {
			string src = "local 幅 = 3 -- 幅\nlocal s = \"幅\\q\\n\" .. [[幅\\q]]\nreturn 幅";
			string made = Aup2ScriptFile.Portable(src);
			Assert.Equal("local _u5e45 = 3 -- 幅\nlocal s = \"幅q\\n\" .. [[幅\\q]]\nreturn _u5e45", made);
			Assert.Equal("_u5e45x", Aup2ScriptFile.Mangle("幅x"));
			Assert.Equal("plain", Aup2ScriptFile.Portable("plain"));
		}

		[Fact]
		public void TheLibraryFindsScriptsUnderEveryNameAProjectMayUse() {
			Write("move.anm2", "obj.ox = 1\n");
			Write("sub/@pack.anm2", "@one\nobj.ox = 1\n@two\nobj.ox = 2\n");
			Write("sub/@other.anm2", "@two\nobj.ox = 3\n");
			Write("deeper/still/hidden.anm2", "obj.ox = 1\n");
			Write("notes.txt", "obj.ox = 1\n");
			using var lib = new Aup2ScriptLibrary(new[] { _folder, Path.Combine(_folder, "missing") });
			Assert.True(lib.Knows(Aup2ScriptLibrary.ScriptControl));
			Assert.True(lib.Knows("move"));
			Assert.True(lib.Knows("one@pack"));
			Assert.True(lib.Knows("one"));
			Assert.True(lib.Knows("two@pack"));
			Assert.True(lib.Knows("two@other"));
			Assert.False(lib.Knows("two"));            // two files have it
			Assert.False(lib.Knows("hidden"));         // two levels down
			Assert.False(lib.Knows("notes"));
			Assert.False(lib.Knows("標準描画"));
		}

		[Fact]
		public void AScriptMovesTheObjectWithItsSetting() {
			Write("move.anm2", "--track@vx:X速度,-10,10,2\n--check@down:下へ,0\nobj.ox = obj.ox + vx * obj.time\nif down == 1 then obj.oy = 7 end\n");
			using var lib = new Aup2ScriptLibrary(new[] { _folder });
			var host = new FakeHost();
			string constant = Shape("effect.name=move\r\nX速度=5.00\r\n下へ=1\r\n");
			Assert.Equal(100, Play(lib, host, constant, 0).X, 6);
			var at1 = Play(lib, host, constant, 60);
			Assert.Equal(105, at1.X, 6);
			Assert.Equal(7, at1.Y, 6);
			// the declared default when the project says nothing, the variable's name as a key, a moving value
			Assert.Equal(102, Play(lib, host, Shape("effect.name=move\r\n"), 60).X, 6);
			Assert.Equal(97, Play(lib, host, Shape("effect.name=move\r\nvx=-3\r\n"), 60).X, 6);
			string moving = Shape("effect.name=move\r\nX速度=0.00,10.00,直線移動,0\r\n");
			double v = 10 * 60.0 / 119;
			Assert.Equal(100 + v * 1, Play(lib, host, moving, 60).X, 4);
			Assert.Empty(lib.Errors);
			Assert.Equal(0, host.Polys + host.Draws);
			Assert.True(host.Composed > 0);
		}

		[Fact]
		public void TheOlderNumberedSettingsAndTheDialogReachTheScript() {
			Write("@old.anm",
				"@shift\n--track0:dx,-100,100,10\n--check0:twice,0\n--dialog:倍率,local k=2;on/chk,local on=1;list,local pts={1,2,3};\n" +
				"local d = obj.track0 * k\nif obj.check0 then d = d * 2 end\nif on == 1 then obj.ox = obj.ox + d + #pts end\n",
				CodePagesEncodingProvider.Instance.GetEncoding(932));
			using var lib = new Aup2ScriptLibrary(new[] { _folder });
			var host = new FakeHost();
			Assert.Equal(100 + 10 * 2 + 3, Play(lib, host, Shape("effect.name=shift@old\r\n"), 0).X, 6);
			Assert.Equal(100 + 4 * 3 * 2 + 2, Play(lib, host, Shape("effect.name=shift\r\ndx=4.00\r\ntwice=1\r\n倍率=3\r\nlist={5,6}\r\n"), 0).X, 6);
			Assert.Equal(100, Play(lib, host, Shape("effect.name=shift@old\r\ntrack0=4\r\non=0\r\n"), 0).X, 6);
			Assert.Empty(lib.Errors);
		}

		[Fact]
		public void ScriptControlRunsItsOwnText() {
			using var lib = new Aup2ScriptLibrary(Array.Empty<string>());
			var host = new FakeHost();
			string body = Shape("effect.name=スクリプト制御\r\nスクリプト=obj.oy = 12\\nobj.zoom = 2\\nobj.rz = obj.frame\\nobj.alpha = 0.5\\nprint(\"w=\" .. obj.w, obj.layer)\r\n");
			var item = Play(lib, host, body, 30);
			Assert.Equal((100.0, 12.0), (item.X, item.Y));
			Assert.Equal((2.0, 2.0), (item.ScaleX, item.ScaleY));
			Assert.Equal(30, item.RotZ, 6);
			Assert.Equal(0.5, item.Opacity, 6);
			Assert.Equal("w=642", lib.Messages.Single());
			Assert.Empty(lib.Errors);
		}

		[Fact]
		public void ScriptsDrawLoadAndReadPixels() {
			Write("many.anm2",
				"--color@col:色,0x00ff00\n" +
				"local c, a = obj.getpixel(1, 1)\nlocal r, g, b = RGB(c)\nobj.getpixel(2, 2)\n" +
				"obj.load(\"figure\", \"円\", col, 20)\nlocal w, h = obj.getpixel()\n" +
				"for i = 0, 2 do obj.draw(i * 10, 0, 0) end\n" +
				"obj.drawpoly(-5, -5, 0, 5, -5, 0, 5, 5, 0, -5, 5, 0)\n" +
				"obj.drawpoly({ { 0, 0, 0, 0, 0 }, { 1, 0, 0, 1, 0 }, { 1, 1, 0, 1, 1 }, { 0, 0, 1, 0, 0 }, { 1, 0, 1, 1, 0 }, { 1, 1, 1, 1, 1 } }, 3)\n" +
				"obj.drawpoly({ { 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0, 0, 0, w, 0, w, h, 0, h } })\n" +
				"print(r, \",\", g, \",\", b, \",\", a, \",\", w, \",\", h, \",\", obj.w, \",\", RGB(1, 2, 3), \",\", OR(1, 2), \",\", SHIFT(8, -1), \",\", 7 / 2 * 2)\n");
			using var lib = new Aup2ScriptLibrary(new[] { _folder });
			var host = new FakeHost();
			Play(lib, host, Shape("effect.name=many\r\n"), 0);
			Assert.Empty(lib.Errors);
			Assert.Equal("16,32,48,1,20,20,20,66051,3,4,7", lib.Messages.Single());
			Assert.Equal(1, host.Reads);               // read once, then kept
			Assert.Equal(1, host.Figures);
			Assert.Equal(new[] { 100.0, 110.0, 120.0 }, host.DrawX);
			Assert.Equal(4, host.Polys);
			Assert.Equal(2, host.Triangles);
			Assert.Equal(0, host.Composed);            // it drew itself
		}

		[Fact]
		public void TheMachineIsOutOfReach() {
			using var lib = new Aup2ScriptLibrary(Array.Empty<string>());
			var host = new FakeHost();
			string code =
				"local found = pcall(require, \"socket\")\\n" +
				"local chunk = load(\"\\\\27Lua\")\\n" +
				"print(type(os.execute), type(os.remove), type(os.exit), type(os.getenv(\"PATH\")), tostring((io.popen(\"dir\"))), tostring((io.open(\"C:/x\", \"r\"))), " +
				"type(package.loadlib), type(debug), type(luanet), type(import), type(CLRPackage), type(ffi), type(string.dump), tostring((loadfile(\"x\"))), " +
				"tostring(found), type(require(\"rikky_module\")), type(rikky_module), type(chunk), tostring(pcall(dofile, \"x\")))\\n" +
				"obj.ox = 5";
			var item = Play(lib, host, Shape("effect.name=スクリプト制御\r\nスクリプト=" + code + "\r\n"), 0);
			Assert.Empty(lib.Errors);
			Assert.Equal("nil" + "nil" + "nil" + "nil" + "nil" + "nil" + "nil" + "nil" + "nil" + "nil" + "nil" + "nil" + "nil" + "nil" + "false" + "table" + "nil" + "nil" + "false",
				lib.Messages.Single());
			Assert.Equal(105, item.X, 6);
		}

		[Fact]
		public void AnErrorOrAnEndlessLoopLeavesTheObjectAsItWas() {
			Write("@bad.anm2", "@broken\nobj.ox = 50\nobj.zoom = 3\nerror(\"no\")\n@typo\nobj.ox = = 1\n@forever\nobj.ox = 9\nwhile true do end\n@fine\nobj.ox = 1\n");
			using var lib = new Aup2ScriptLibrary(new[] { _folder }) { BudgetMs = 150 };
			var host = new FakeHost();
			foreach (string name in new[] { "broken", "typo", "forever" }) {
				var item = Play(lib, host, Shape("effect.name=" + name + "@bad\r\n"), 0);
				Assert.Equal((100.0, 1.0), (item.X, item.ScaleX));
				Play(lib, host, Shape("effect.name=" + name + "@bad\r\n"), 1);
			}
			// one line per script, however often it fails; the others still run
			Assert.Equal(3, lib.Errors.Count);
			Assert.Contains("broken@bad", lib.Errors[0]);
			Assert.Contains(":4:", lib.Errors[0]);     // the line of the file
			Assert.Contains("typo@bad", lib.Errors[1]);
			Assert.Contains("too long", lib.Errors[2]);
			Assert.Equal(101, Play(lib, host, Shape("effect.name=fine@bad\r\n"), 0).X, 6);
		}
	}
}
