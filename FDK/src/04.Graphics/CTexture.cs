using System.Drawing;
using SampleFramework;
using Silk.NET.Maths;
using Silk.NET.OpenGLES;
using SkiaSharp;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;

namespace FDK {
	public class CTexture : IDisposable {
		/// <summary>
		/// バッファの集まり
		/// </summary>
		private static uint VAO;

		/// <summary>
		/// 頂点バッファ
		/// </summary>
		private static uint VBO;

		/// <summary>
		/// 頂点バッファの使用順バッファ
		/// </summary>
		private static uint EBO;

		/// <summary>
		/// テクスチャで使用するUV座標バッファ
		/// </summary>
		private static uint UVBO;

		/// <summary>
		/// 頂点バッファの使用順の数
		/// </summary>
		private static uint IndicesCount;

		/// <summary>
		/// シェーダー
		/// </summary>
		private static uint ShaderProgram;

		/// <summary>
		/// 移動、回転、拡大縮小に使うMatrixのハンドル
		/// </summary>
		private static int MVPID;

		/// <summary>
		/// 色合いのハンドル
		/// </summary>
		private static int ColorID;

		/// <summary>
		/// 拡大率のハンドル
		/// </summary>
		private static int ScaleID;

		/// <summary>
		/// テクスチャの切り抜きのハンドル
		/// </summary>
		private static int TextureRectID;

		private static int CameraID;

		private static int NoteModeID;

		/// <summary>
		/// 描画に使用する共通のバッファを作成
		/// </summary>
		public static void Init() {
			//シェーダーを作成、実際のコードはCreateShaderProgramWithShaderを見てください
			ShaderProgram = ShaderHelper.CreateShaderProgramFromSource(
				@"#version 100
                precision mediump float;

                attribute vec3 aPosition;
                attribute vec2 aUV;

                uniform mat4 mvp;
                uniform vec4 color;
                uniform mat4 camera;

                varying vec2 texcoord;

                void main()
                {
                    vec4 position = vec4(aPosition, 1.0);
                    position = camera * mvp * position;

                    texcoord = vec2(aUV.x, aUV.y);
                    gl_Position = position;
                }"
				,
				@"#version 100
                precision mediump float;

                uniform vec4 color;
                uniform sampler2D texture1;
                uniform vec4 textureRect;
                uniform vec2 scale;
                uniform bool noteMode;

                varying vec2 texcoord;

                void main()
                {
                    vec2 rect;
                    if (noteMode)
                    {
                        rect = textureRect.xy + (texcoord * textureRect.zw * scale);
                        rect = rect - (floor((rect - textureRect.xy) / textureRect.zw) * textureRect.zw);
                    }
                    else
                    {
                        rect = vec2(textureRect.xy + (texcoord * textureRect.zw));
                    }
                    gl_FragColor = texture2D(texture1, rect) * color;
                }"
			);
			//------

			//シェーダーに値を送るためのハンドルを取得------
			MVPID = Game.Gl.GetUniformLocation(ShaderProgram, "mvp"); //拡大縮小、移動、回転のMatrix
			ColorID = Game.Gl.GetUniformLocation(ShaderProgram, "color"); //色合い
			ScaleID = Game.Gl.GetUniformLocation(ShaderProgram, "scale"); //スケール
			TextureRectID = Game.Gl.GetUniformLocation(ShaderProgram, "textureRect"); //テクスチャの切り抜きの座標と大きさ
			CameraID = Game.Gl.GetUniformLocation(ShaderProgram, "camera"); //テクスチャの切り抜きの座標と大きさ
			NoteModeID = Game.Gl.GetUniformLocation(ShaderProgram, "noteMode"); //テクスチャの切り抜きの座標と大きさ


			//------

			//2DSprite専用のバッファーを作成する... なんとVAOは一つでOK!



			//VAOを作成----
			VAO = Game.Gl.GenVertexArray();
			Game.Gl.BindVertexArray(VAO);
			//----

			//VBOを作成-----
			float[] vertices = new float[] //頂点データ
            {
                //x, y, z
                -1.0f, 1.0f, 0.0f,
				1.0f, 1.0f, 0.0f,
				-1.0f, -1.0f, 0.0f,
				1.0f, -1.0f, 0.0f,
			};
			VBO = Game.Gl.GenBuffer(); //頂点バッファを作る
			Game.Gl.BindBuffer(BufferTargetARB.ArrayBuffer, VBO); //頂点バッファをバインドをする
			unsafe {
				fixed (float* data = vertices) {
					Game.Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), data, BufferUsageARB.StaticDraw); //VRAMに頂点データを送る
				}
			}

			uint locationPosition = (uint)Game.Gl.GetAttribLocation(ShaderProgram, "aPosition");
			Game.Gl.EnableVertexAttribArray(locationPosition); //layout (location = 0)を使用可能に
			unsafe {
				Game.Gl.VertexAttribPointer(locationPosition, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0); //float3個で一つのxyzの塊として頂点を作る
			}
			//-----



			//EBOを作成------
			//普通に四角を描画すると頂点データのxyzの塊が6個も必要だけど四つだけ作成して読み込む順番をこうやって登録すればメモリが少なくなる!

			EBO = Game.Gl.GenBuffer(); //頂点バッファの使用順バッファを作る
			Game.Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, EBO); //頂点バッファの使用順バッファをバインドする

			uint[] indices = new uint[] //
            {
				0, 1, 2,
				2, 1, 3
			};
			IndicesCount = (uint)indices.Length; //数を登録する
			unsafe {
				fixed (uint* data = indices) {
					Game.Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), data, BufferUsageARB.StaticDraw); //VRAMに送る
				}
			}
			//-----

			//テクスチャの読み込みに使用するUV座標のバッファを作成、処理はVBOと大体同じ
			UVBO = Game.Gl.GenBuffer();
			Game.Gl.BindBuffer(BufferTargetARB.ArrayBuffer, UVBO);

			float[] uvs = new float[]
			{
				0.0f, 0.0f,
				1.0f, 0.0f,
				0.0f, 1.0f,
				1.0f, 1.0f,
			};
			unsafe {
				fixed (float* data = uvs) {
					Game.Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(uvs.Length * sizeof(float)), data, BufferUsageARB.StaticDraw);
				}
			}

			uint locationUV = (uint)Game.Gl.GetAttribLocation(ShaderProgram, "aUV");
			Game.Gl.EnableVertexAttribArray(locationUV);
			unsafe {
				Game.Gl.VertexAttribPointer(locationUV, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), (void*)0);
			}
			//-----


			//バインドを解除 厳密には必須ではないが何かのはずみでバインドされたままBufferSubDataでデータが更新されたらとかされたらまあ大変-----
			Game.Gl.BindVertexArray(0);
			Game.Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
			Game.Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
			//-----


		}

		/// <summary>
		/// 描画に使用する共通のバッファを解放
		/// </summary>
		public static void Terminate() {
			//ちゃんとバッファは解放すること
			Game.Gl.DeleteVertexArray(VAO);
			Game.Gl.DeleteBuffer(VBO);
			Game.Gl.DeleteBuffer(EBO);
			Game.Gl.DeleteBuffer(UVBO);
			Game.Gl.DeleteProgram(ShaderProgram);
		}

		// プロパティ
		public bool b加算合成 {
			get;
			set;
		}
		public bool b乗算合成 {
			get;
			set;
		}
		public bool b減算合成 {
			get;
			set;
		}
		public bool bスクリーン合成 {
			get;
			set;
		}
		public float fZ軸中心回転 {
			get;
			set;
		}
		public float fZRotation {
			get => fZ軸中心回転;
			set { fZ軸中心回転 = value; }
		}
		public int Opacity {
			get {
				return this._opacity;
			}
			set {
				if (value < 0) {
					this._opacity = 0;
				} else if (value > 0xff) {
					this._opacity = 0xff;
				} else {
					this._opacity = value;
				}
			}
		}
		public Size szTextureSize {
			get;
			private set;
		}
		public Size sz画像サイズ {
			get;
			protected set;
		}
		public Vector3D<float> vcScaleRatio;

		// 画面が変わるたび以下のプロパティを設定し治すこと。

		public static Size sz論理画面 = Size.Empty;
		public static Size sz物理画面 = Size.Empty;
		public static Rectangle rc物理画面描画領域 = Rectangle.Empty;
		/// <summary>
		/// <para>論理画面を1とする場合の物理画面の倍率。</para>
		/// <para>論理値×画面比率＝物理値。</para>
		/// </summary>
		public static float f画面比率 = 1.0f;

		internal uint Texture_;

		// コンストラクタ

		public CTexture() {
			this.sz画像サイズ = new Size(0, 0);
			this.szTextureSize = new Size(0, 0);
			this._opacity = 0xff;
			this.b加算合成 = false;
			this.fZ軸中心回転 = 0f;
			this.vcScaleRatio = new Vector3D<float>(1f, 1f, 1f);
			//			this._txData = null;
		}

		public CTexture(CTexture tx) {
			this.sz画像サイズ = tx.sz画像サイズ;
			this.szTextureSize = tx.szTextureSize;
			this._opacity = tx._opacity;
			this.b加算合成 = tx.b加算合成;
			this.fZ軸中心回転 = tx.fZ軸中心回転;
			this.vcScaleRatio = tx.vcScaleRatio;
			Texture_ = tx.Texture_;
			//			this._txData = null;
		}

		public void UpdateTexture(CTexture texture, int n幅, int n高さ) {
			Texture_ = texture.Texture_;
			this.sz画像サイズ = new Size(n幅, n高さ);
			this.szTextureSize = this.t指定されたサイズを超えない最適なテクスチャサイズを返す(this.sz画像サイズ);
			this.rc全画像 = new Rectangle(0, 0, this.sz画像サイズ.Width, this.sz画像サイズ.Height);
		}

		public void UpdateTexture(IntPtr texture, int width, int height, PixelFormat rgbaType) {
			unsafe {
				Game.Gl.DeleteTexture(Texture_); //解放
				void* data = texture.ToPointer();
				Texture_ = GenTexture(data, (uint)width, (uint)height, rgbaType);
			}
			this.sz画像サイズ = new Size(width, height);
			this.szTextureSize = this.t指定されたサイズを超えない最適なテクスチャサイズを返す(this.sz画像サイズ);
			this.rc全画像 = new Rectangle(0, 0, this.sz画像サイズ.Width, this.sz画像サイズ.Height);
		}

		/// <summary>
		/// <para>指定されたビットマップオブジェクトから Managed テクスチャを作成する。</para>
		/// <para>テクスチャのサイズは、BITMAP画像のサイズ以上、かつ、D3D9デバイスで生成可能な最小のサイズに自動的に調節される。
		/// その際、テクスチャの調節後のサイズにあわせた画像の拡大縮小は行わない。</para>
		/// <para>その他、ミップマップ数は 1、Usage は None、Pool は Managed、イメージフィルタは Point、ミップマップフィルタは
		/// None、カラーキーは 0xFFFFFFFF（完全なる黒を透過）になる。</para>
		/// </summary>
		/// <param name="device">Direct3D9 デバイス。</param>
		/// <param name="bitmap">作成元のビットマップ。</param>
		/// <param name="format">テクスチャのフォーマット。</param>
		/// <exception cref="CTextureCreateFailedException">テクスチャの作成に失敗しました。</exception>
		public CTexture(SKBitmap bitmap)
			: this() {
			try {
				MakeTexture(bitmap, false);
			} catch (Exception e) {
				this.Dispose();
				throw new CTextureCreateFailedException("ビットマップからのテクスチャの生成に失敗しました。", e);
			}
		}

		public CTexture(int n幅, int n高さ)
			: this() {
			try {
				this.sz画像サイズ = new Size(n幅, n高さ);
				this.szTextureSize = this.t指定されたサイズを超えない最適なテクスチャサイズを返す(this.sz画像サイズ);
				this.rc全画像 = new Rectangle(0, 0, this.sz画像サイズ.Width, this.sz画像サイズ.Height);
			} catch {
				this.Dispose();
				throw new CTextureCreateFailedException(string.Format("テクスチャの生成に失敗しました。\n({0}x{1}, {2})", n幅, n高さ));
			}
		}

		/// <summary>
		/// <para>画像ファイルからテクスチャを生成する。</para>
		/// <para>利用可能な画像形式は、BMP, JPG, PNG, TGA, DDS, PPM, DIB, HDR, PFM のいずれか。</para>
		/// <para>テクスチャのサイズは、画像のサイズ以上、かつ、D3D9デバイスで生成可能な最小のサイズに自動的に調節される。
		/// その際、テクスチャの調節後のサイズにあわせた画像の拡大縮小は行わない。</para>
		/// <para>その他、ミップマップ数は 1、Usage は None、イメージフィルタは Point、ミップマップフィルタは None になる。</para>
		/// </summary>
		/// <param name="device">Direct3D9 デバイス。</param>
		/// <param name="strファイル名">画像ファイル名。</param>
		/// <param name="format">テクスチャのフォーマット。</param>
		/// <param name="b黒を透過する">画像の黒（0xFFFFFFFF）を透過させるなら true。</param>
		/// <param name="pool">テクスチャの管理方法。</param>
		/// <exception cref="CTextureCreateFailedException">テクスチャの作成に失敗しました。</exception>
		public CTexture(string strファイル名, bool b黒を透過する)
			: this() {
			MakeTexture(strファイル名, b黒を透過する);
		}
		public void MakeTexture(string strファイル名, bool b黒を透過する) {
			if (!File.Exists(strファイル名))     // #27122 2012.1.13 from: ImageInformation では FileNotFound 例外は返ってこないので、ここで自分でチェックする。わかりやすいログのために。
				throw new FileNotFoundException(string.Format("ファイルが存在しません。\n[{0}]", strファイル名));

			SKBitmap bitmap = SKBitmap.Decode(strファイル名);
			MakeTexture(bitmap, b黒を透過する);
			bitmap.Dispose();
		}

		public CTexture(SKBitmap bitmap, bool b黒を透過する)
			: this() {
			MakeTexture(bitmap, b黒を透過する);
		}

		private unsafe uint GenTexture(void* data, uint width, uint height, PixelFormat pixelFormat) {
			//テクスチャハンドルの作成-----
			uint handle = Game.Gl.GenTexture();
			Game.Gl.BindTexture(TextureTarget.Texture2D, handle);
			//-----

			//テクスチャのデータをVramに送る
			Game.Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)pixelFormat, width, height, 0, pixelFormat, GLEnum.UnsignedByte, data);
			//-----

			//拡大縮小の時の補完を指定------
			Game.Gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Nearest); //この場合は補完しない
			Game.Gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMinFilter.Nearest);
			//------

			Game.Gl.BindTexture(TextureTarget.Texture2D, 0); //バインドを解除することを忘れないように

			return handle;
		}

		public void MakeTexture(SKBitmap bitmap, bool b黒を透過する) {
			try {
				if (bitmap == null) {
					bitmap = new SKBitmap(10, 10);
				}

				unsafe {
					fixed (void* data = bitmap.Pixels) {
						if (Thread.CurrentThread.ManagedThreadId == Game.MainThreadID) {
							Texture_ = GenTexture(data, (uint)bitmap.Width, (uint)bitmap.Height, PixelFormat.Bgra);
						} else {
							SKBitmap bm = bitmap.Copy();
							Action createInstance = () => {
								fixed (void* data2 = bitmap.Pixels) {
									Texture_ = GenTexture(data2, (uint)bitmap.Width, (uint)bitmap.Height, PixelFormat.Bgra);
								}
								bm.Dispose();
							};
							Game.AsyncActions.Add(createInstance);
							while (Game.AsyncActions.Contains(createInstance)) {

							}
						}
					}
				}

				this.sz画像サイズ = new Size(bitmap.Width, bitmap.Height);
				this.rc全画像 = new Rectangle(0, 0, this.sz画像サイズ.Width, this.sz画像サイズ.Height);
				this.szTextureSize = this.t指定されたサイズを超えない最適なテクスチャサイズを返す(this.sz画像サイズ);
			} catch {
				this.Dispose();
				// throw new CTextureCreateFailedException( string.Format( "テクスチャの生成に失敗しました。\n{0}", strファイル名 ) );
				throw new CTextureCreateFailedException(string.Format("テクスチャの生成に失敗しました。\n"));
			}
		}

		public void tSetScale(float x, float y) {
			vcScaleRatio.X = x;
			vcScaleRatio.Y = y;
		}

		// メソッド

		// 2016.11.10 kairera0467 拡張
		// Rectangleを使う場合、座標調整のためにテクスチャサイズの値をそのまま使うとまずいことになるため、Rectragleから幅を取得して調整をする。
		public void t2D中心基準描画(int x, int y) {
			this.t2D描画(x - (this.szTextureSize.Width / 2), y - (this.szTextureSize.Height / 2), 1f, this.rc全画像);
		}

		public void t2D中心基準描画Mirrored(int x, int y) {
			this.t2D左右反転描画(x - (this.szTextureSize.Width / 2), y - (this.szTextureSize.Height / 2), 1f, this.rc全画像);
		}

		public void t2D中心基準描画Mirrored(float x, float y) {
			this.t2D左右反転描画(x - (this.szTextureSize.Width / 2), y - (this.szTextureSize.Height / 2), 1f, this.rc全画像);
		}

		public void t2D中心基準描画(int x, int y, Rectangle rc画像内の描画領域) {
			this.t2D描画(x - (rc画像内の描画領域.Width / 2), y - (rc画像内の描画領域.Height / 2), 1f, rc画像内の描画領域);
		}
		public void t2D中心基準描画(float x, float y) {
			this.t2D描画((int)x - (this.szTextureSize.Width / 2), (int)y - (this.szTextureSize.Height / 2), 1f, this.rc全画像);
		}
		public void t2D中心基準描画(float x, float y, Rectangle rc画像内の描画領域) {
			this.t2D描画((int)x - (rc画像内の描画領域.Width / 2), (int)y - (rc画像内の描画領域.Height / 2), 1.0f, rc画像内の描画領域);
		}
		public void t2D中心基準描画(float x, float y, float depth, Rectangle rc画像内の描画領域) {
			this.t2D描画((int)x - (rc画像内の描画領域.Width / 2), (int)y - (rc画像内の描画領域.Height / 2), depth, rc画像内の描画領域);
		}

		// 下を基準にして描画する(拡大率考慮)メソッドを追加。 (AioiLight)
		public void t2D拡大率考慮下基準描画(int x, int y) {
			this.t2D描画(x, y - (szTextureSize.Height * this.vcScaleRatio.Y), 1f, this.rc全画像);
		}
		public void t2D拡大率考慮下基準描画(int x, int y, Rectangle rc画像内の描画領域) {
			this.t2D描画(x, y - (rc画像内の描画領域.Height * this.vcScaleRatio.Y), 1f, rc画像内の描画領域);
		}
		public void t2D拡大率考慮下中心基準描画(int x, int y) {
			this.t2D描画(x - (this.szTextureSize.Width / 2 * this.vcScaleRatio.X), y - (szTextureSize.Height * this.vcScaleRatio.Y), 1f, this.rc全画像);
		}

		public void t2D拡大率考慮下中心基準描画Mirrored(int x, int y) {
			this.t2D左右反転描画(x - (this.szTextureSize.Width / 2 * this.vcScaleRatio.X), y - (szTextureSize.Height * this.vcScaleRatio.Y), 1f, this.rc全画像);
		}
		public void t2D拡大率考慮下中心基準描画Mirrored(float x, float y) {
			this.t2D左右反転描画(x - (this.szTextureSize.Width / 2 * this.vcScaleRatio.X), y - (szTextureSize.Height * this.vcScaleRatio.Y), 1f, this.rc全画像);
		}

		public void t2D拡大率考慮下基準描画(float x, float y) {
			this.t2D描画(x, y - (szTextureSize.Height * this.vcScaleRatio.Y), 1f, this.rc全画像);
		}
		public void t2D拡大率考慮下基準描画(float x, float y, RectangleF rc画像内の描画領域) {
			this.t2D描画(x, y - (rc画像内の描画領域.Height * this.vcScaleRatio.Y), 1f, rc画像内の描画領域);
		}
		public void t2D拡大率考慮下中心基準描画(float x, float y) {
			this.t2D拡大率考慮下中心基準描画((int)x, (int)y);
		}

		public void t2D拡大率考慮下中心基準描画(int x, int y, Rectangle rc画像内の描画領域) {
			this.t2D描画(x - ((rc画像内の描画領域.Width / 2)), y - (rc画像内の描画領域.Height * this.vcScaleRatio.Y), 1f, rc画像内の描画領域);
		}
		public void t2D拡大率考慮下中心基準描画(float x, float y, Rectangle rc画像内の描画領域) {
			this.t2D拡大率考慮下中心基準描画((int)x, (int)y, rc画像内の描画領域);
		}
		public void t2D下中央基準描画(int x, int y) {
			this.t2D描画(x - (this.szTextureSize.Width / 2), y - (szTextureSize.Height), this.rc全画像);
		}
		public void t2D下中央基準描画(int x, int y, Rectangle rc画像内の描画領域) {
			this.t2D描画(x - (rc画像内の描画領域.Width / 2), y - (rc画像内の描画領域.Height), rc画像内の描画領域);
			//this.t2D描画(devicek x, y, rc画像内の描画領域;
		}

		public void t2D_DisplayImage_RollNote(int x, int y, RectangleF rc) {
			this.t2D描画(x - (rc.Width / 2 * this.vcScaleRatio.X), y - (rc.Height / 2 * this.vcScaleRatio.Y), 1f, rc, true);
		}

		public void t2D拡大率考慮中央基準描画(int x, int y) {
			this.t2D描画(x - (this.szTextureSize.Width / 2 * this.vcScaleRatio.X), y - (szTextureSize.Height / 2 * this.vcScaleRatio.Y), 1f, this.rc全画像);
		}
		public void t2D拡大率考慮中央基準描画(int x, int y, RectangleF rc) {
			this.t2D描画(x - (rc.Width / 2 * this.vcScaleRatio.X), y - (rc.Height / 2 * this.vcScaleRatio.Y), 1f, rc);
		}
		public void t2D_DisplayImage_AnchorCenterLeft(int x, int y, RectangleF rc) {
			this.t2D描画(x, y - (rc.Height / 2 * this.vcScaleRatio.Y), 1f, rc);
		}
		public void t2D拡大率考慮上中央基準描画(int x, int y, RectangleF rc) {
			this.t2D描画(x - (rc.Width / 2 * this.vcScaleRatio.X), y, 1f, rc);
		}
		public void t2D_DisplayImage_AnchorUpRight(int x, int y, RectangleF rc) {
			this.t2D描画(x - (rc.Width * this.vcScaleRatio.X), y, 1f, rc);
		}
		public void t2D拡大率考慮上中央基準描画(int x, int y) {
			this.t2D描画(x - (rc全画像.Width / 2 * this.vcScaleRatio.X), y, 1f, rc全画像);
		}
		public void t2D拡大率考慮中央基準描画(float x, float y) {
			this.t2D描画(x - (this.szTextureSize.Width / 2 * this.vcScaleRatio.X), y - (szTextureSize.Height / 2 * this.vcScaleRatio.Y), 1f, this.rc全画像);
		}
		public void t2D拡大率考慮中央基準描画Mirrored(float x, float y) {
			this.t2D左右反転描画(x - (this.szTextureSize.Width / 2 * this.vcScaleRatio.X), y - (szTextureSize.Height / 2 * this.vcScaleRatio.Y), 1f, this.rc全画像);
		}
		public void t2D拡大率考慮中央基準描画(float x, float y, RectangleF rc) {
			this.t2D描画(x - (rc.Width / 2 * this.vcScaleRatio.X), y - (rc.Height / 2 * this.vcScaleRatio.Y), 1f, rc);
		}
		public void t2D拡大率考慮描画(RefPnt refpnt, float x, float y) {
			this.t2D拡大率考慮描画(refpnt, x, y, rc全画像);
		}
		public void t2D拡大率考慮描画(RefPnt refpnt, float x, float y, Rectangle rect) {
			this.t2D拡大率考慮描画(refpnt, x, y, 1f, rect);
		}
		public void t2D拡大率考慮描画(RefPnt refpnt, float x, float y, float depth, Rectangle rect) {
			switch (refpnt) {
				case RefPnt.UpLeft:
					this.t2D描画(x, y, depth, rect);
					break;
				case RefPnt.Up:
					this.t2D描画(x - (rect.Width / 2 * this.vcScaleRatio.X), y, depth, rect);
					break;
				case RefPnt.UpRight:
					this.t2D描画(x - rect.Width * this.vcScaleRatio.X, y, depth, rect);
					break;
				case RefPnt.Left:
					this.t2D描画(x, y - (rect.Height / 2 * this.vcScaleRatio.Y), depth, rect);
					break;
				case RefPnt.Center:
					this.t2D描画(x - (rect.Width / 2 * this.vcScaleRatio.X), y - (rect.Height / 2 * this.vcScaleRatio.Y), depth, rect);
					break;
				case RefPnt.Right:
					this.t2D描画(x - rect.Width * this.vcScaleRatio.X, y - (rect.Height / 2 * this.vcScaleRatio.Y), depth, rect);
					break;
				case RefPnt.DownLeft:
					this.t2D描画(x, y - rect.Height * this.vcScaleRatio.Y, depth, rect);
					break;
				case RefPnt.Down:
					this.t2D描画(x - (rect.Width / 2 * this.vcScaleRatio.X), y - rect.Height * this.vcScaleRatio.Y, depth, rect);
					break;
				case RefPnt.DownRight:
					this.t2D描画(x - rect.Width * this.vcScaleRatio.X, y - rect.Height * this.vcScaleRatio.Y, depth, rect);
					break;
				default:
					break;
			}

		}
		public void t2D_DisplayImage_AnchorCenter(int x, int y) {
			this.t2D描画(x - (this.rc全画像.Width / 2 * this.vcScaleRatio.X), y - (this.rc全画像.Height / 2 * this.vcScaleRatio.Y), 1f, this.rc全画像);
		}
		public void t2D_DisplayImage_AnchorCenter(int x, int y, Rectangle rc) {
			this.t2D描画(x - (rc.Width / 2 * this.vcScaleRatio.X), y - (rc.Height / 2 * this.vcScaleRatio.Y), 1f, rc);
		}
		public void t2D_DisplayImage_AnchorCenter(int x, int y, RectangleF rc) {
			this.t2D描画(x - (rc.Width / 2 * this.vcScaleRatio.X), y - (rc.Height / 2 * this.vcScaleRatio.Y), 1f, rc);
		}

		public enum RefPnt {
			UpLeft,
			Up,
			UpRight,
			Left,
			Center,
			Right,
			DownLeft,
			Down,
			DownRight,
		}

		public void t2D_DisplayImage(int x, int y) {
			this.t2D描画(x, y, 1f, this.rc全画像);
		}
		public void t2D_DisplayImage(int x, int y, Rectangle rc画像内の描画領域) {
			this.t2D描画(x, y, 1f, rc画像内の描画領域);
		}
		public void t2D_DisplayImage(int x, int y, RectangleF rc) {
			this.t2D描画(x, y, 1f, rc);
		}

		/// <summary>
		/// テクスチャを 2D 画像と見なして描画する。
		/// </summary>
		/// <param name="device">Direct3D9 デバイス。</param>
		/// <param name="x">描画位置（テクスチャの左上位置の X 座標[dot]）。</param>
		/// <param name="y">描画位置（テクスチャの左上位置の Y 座標[dot]）。</param>
		public void t2D描画(int x, int y) {
			this.t2D描画(x, y, 1f, this.rc全画像);
		}
		public void t2D描画(int x, int y, RectangleF rc画像内の描画領域) {
			this.t2D描画(x, y, 1f, rc画像内の描画領域);
		}
		public void t2D描画(float x, float y) {
			this.t2D描画((int)x, (int)y, 1f, this.rc全画像);
		}
		public void t2D描画(float x, float y, RectangleF rc画像内の描画領域) {
			this.t2D描画((int)x, (int)y, 1f, rc画像内の描画領域);
		}
		public void t2D描画(float x, float y, float depth, RectangleF rc画像内の描画領域, bool flipX = false, bool flipY = false, bool rollMode = false) {
			this.color4.Alpha = this._opacity / 255f;

			BlendType blendType;
			if (b加算合成) {
				blendType = BlendType.Add;
			} else if (b乗算合成) {
				blendType = BlendType.Multi;
			} else if (b減算合成) {
				blendType = BlendType.Sub;
			} else if (bスクリーン合成) {
				blendType = BlendType.Screen;
			} else {
				blendType = BlendType.Normal;
			}

			BlendHelper.SetBlend(blendType);

			Game.Gl.UseProgram(ShaderProgram);//Uniform4よりこれが先

			Game.Gl.BindTexture(TextureTarget.Texture2D, Texture_); //テクスチャをバインド

			//MVPを設定----
			unsafe {
				Matrix4X4<float> mvp = Matrix4X4<float>.Identity;

				float gameAspect = (float)GameWindowSize.Width / GameWindowSize.Height;


				//スケーリング-----
				mvp *= Matrix4X4.CreateScale(rc画像内の描画領域.Width / GameWindowSize.Width, rc画像内の描画領域.Height / GameWindowSize.Height, 1) *
					Matrix4X4.CreateScale(flipX ? -vcScaleRatio.X : vcScaleRatio.X, flipY ? -vcScaleRatio.Y : vcScaleRatio.Y, 1.0f);
				//-----

				//回転-----
				mvp *= Matrix4X4.CreateScale(1.0f * gameAspect, 1.0f, 1.0f) * //ここでアスペクト比でスケーリングしないとおかしなことになる
					Matrix4X4.CreateRotationZ(fZ軸中心回転) *
					Matrix4X4.CreateScale(1.0f / gameAspect, 1.0f, 1.0f);//回転した後戻してあげる
																		 //-----

				//移動----
				float offsetX = rc画像内の描画領域.Width * vcScaleRatio.X / GameWindowSize.Width;
				float offsetY = rc画像内の描画領域.Height * vcScaleRatio.Y / GameWindowSize.Height;
				mvp *= Matrix4X4.CreateTranslation(offsetX, -offsetY, 0.0f);
				mvp *= Matrix4X4.CreateTranslation(-1.0f, 1.0f, 0);
				mvp *= Matrix4X4.CreateTranslation(x / GameWindowSize.Width * 2, -y / GameWindowSize.Height * 2, 0.0f);
				//-----

				Game.Gl.UniformMatrix4(MVPID, 1, false, (float*)&mvp); //MVPに値を設定
				Matrix4X4<float> camera = Game.Camera;
				Game.Gl.UniformMatrix4(CameraID, 1, false, (float*)&camera);
			}
			//------

			Game.Gl.Uniform4(ColorID, new System.Numerics.Vector4(color4.Red, color4.Green, color4.Blue, color4.Alpha)); //変色用のカラーを設定
			Game.Gl.Uniform2(ScaleID, new System.Numerics.Vector2(vcScaleRatio.X, vcScaleRatio.Y)); //変色用のカラーを設定

			//テクスチャの切り抜きの座標と大きさを設定
			Game.Gl.Uniform4(TextureRectID, new System.Numerics.Vector4(
				rc画像内の描画領域.X / rc全画像.Width, rc画像内の描画領域.Y / rc全画像.Height, //始まり
				rc画像内の描画領域.Width / rc全画像.Width, rc画像内の描画領域.Height / rc全画像.Height)); //大きさ、終わりではない

			Game.Gl.Uniform1(NoteModeID, rollMode ? 1 : 0);

			//描画-----
			Game.Gl.BindVertexArray(VAO);
			unsafe {
				Game.Gl.DrawElements(PrimitiveType.Triangles, IndicesCount, DrawElementsType.UnsignedInt, (void*)0);//描画!
			}

			BlendHelper.SetBlend(BlendType.Normal);
		}
		public void t2D描画(int x, int y, float depth, Rectangle rc画像内の描画領域) {
			t2D描画((float)x, (float)y, depth, rc画像内の描画領域);
		}
		public void t2D上下反転描画(int x, int y) {
			this.t2D上下反転描画(x, y, 1f, this.rc全画像);
		}
		public void t2D上下反転描画(int x, int y, Rectangle rc画像内の描画領域) {
			this.t2D上下反転描画(x, y, 1f, rc画像内の描画領域);
		}
		public void t2D左右反転描画(int x, int y) {
			this.t2D左右反転描画(x, y, 1f, this.rc全画像);
		}
		public void t2D左右反転描画(float x, float y) {
			this.t2D左右反転描画(x, y, 1f, this.rc全画像);
		}
		public void t2D左右反転描画(int x, int y, Rectangle rc画像内の描画領域) {
			this.t2D左右反転描画(x, y, 1f, rc画像内の描画領域);
		}
		public void t2D左右反転描画(float x, float y, float depth, Rectangle rc画像内の描画領域) {
			t2D描画(x, y, depth, rc画像内の描画領域, flipX: true);
		}
		public void t2D上下反転描画(int x, int y, float depth, Rectangle rc画像内の描画領域) {
			t2D描画(x, y, depth, rc画像内の描画領域, flipY: true);
		}
		public void t2D上下反転描画(Point pt) {
			this.t2D上下反転描画(pt.X, pt.Y, 1f, this.rc全画像);
		}
		public void t2D上下反転描画(Point pt, Rectangle rc画像内の描画領域) {
			this.t2D上下反転描画(pt.X, pt.Y, 1f, rc画像内の描画領域);
		}
		public void t2D上下反転描画(Point pt, float depth, Rectangle rc画像内の描画領域) {
			this.t2D上下反転描画(pt.X, pt.Y, depth, rc画像内の描画領域);
		}

		public static Vector3D<float> t論理画面座標をワールド座標へ変換する(int x, int y) {
			return CTexture.t論理画面座標をワールド座標へ変換する(new Vector3D<float>((float)x, (float)y, 0f));
		}
		public static Vector3D<float> t論理画面座標をワールド座標へ変換する(float x, float y) {
			return CTexture.t論理画面座標をワールド座標へ変換する(new Vector3D<float>(x, y, 0f));
		}
		public static Vector3D<float> t論理画面座標をワールド座標へ変換する(Point pt論理画面座標) {
			return CTexture.t論理画面座標をワールド座標へ変換する(new Vector3D<float>(pt論理画面座標.X, pt論理画面座標.Y, 0.0f));
		}
		public static Vector3D<float> t論理画面座標をワールド座標へ変換する(Vector2D<float> v2論理画面座標) {
			return CTexture.t論理画面座標をワールド座標へ変換する(new Vector3D<float>(v2論理画面座標, 0f));
		}
		public static Vector3D<float> t論理画面座標をワールド座標へ変換する(Vector3D<float> v3論理画面座標) {
			return new Vector3D<float>(
				(v3論理画面座標.X - (CTexture.sz論理画面.Width / 2.0f)) * CTexture.f画面比率,
				(-(v3論理画面座標.Y - (CTexture.sz論理画面.Height / 2.0f)) * CTexture.f画面比率),
				v3論理画面座標.Z);
		}




		public void t2D描画SongObj(float x, float y, float xScale, float yScale) {
			this.color4.Alpha = this._opacity / 255f;

			var rc画像内の描画領域 = rc全画像;
			this.color4.Alpha = this._opacity / 255f;

			BlendType blendType;
			if (b加算合成) {
				blendType = BlendType.Add;
			} else if (b乗算合成) {
				blendType = BlendType.Multi;
			} else if (b減算合成) {
				blendType = BlendType.Sub;
			} else if (bスクリーン合成) {
				blendType = BlendType.Screen;
			} else {
				blendType = BlendType.Normal;
			}

			BlendHelper.SetBlend(blendType);

			Game.Gl.UseProgram(ShaderProgram);//Uniform4よりこれが先

			Game.Gl.BindTexture(TextureTarget.Texture2D, Texture_); //テクスチャをバインド

			//MVPを設定----
			unsafe {
				Matrix4X4<float> mvp = Matrix4X4<float>.Identity;

				float gameAspect = (float)GameWindowSize.Width / GameWindowSize.Height;


				//スケーリング-----
				mvp *= Matrix4X4.CreateScale((float)rc画像内の描画領域.Width / GameWindowSize.Width, (float)rc画像内の描画領域.Height / GameWindowSize.Height, 1) *
					Matrix4X4.CreateScale(xScale, yScale, 1.0f);
				//-----

				//回転-----
				mvp *= Matrix4X4.CreateScale(1.0f * gameAspect, 1.0f, 1.0f) * //ここでアスペクト比でスケーリングしないとおかしなことになる
					Matrix4X4.CreateRotationZ(fZ軸中心回転) *
					Matrix4X4.CreateScale(1.0f / gameAspect, 1.0f, 1.0f);//回転した後戻してあげる
																		 //-----

				//移動----
				float offsetX = rc画像内の描画領域.Width * xScale / GameWindowSize.Width;
				float offsetY = rc画像内の描画領域.Height * yScale / GameWindowSize.Height;
				mvp *= Matrix4X4.CreateTranslation(offsetX, -offsetY, 0.0f);
				mvp *= Matrix4X4.CreateTranslation(-1.0f, 1.0f, 0);
				mvp *= Matrix4X4.CreateTranslation(x / GameWindowSize.Width * 2, -y / GameWindowSize.Height * 2, 0.0f);
				//-----

				Game.Gl.UniformMatrix4(MVPID, 1, false, (float*)&mvp); //MVPに値を設定
				Matrix4X4<float> camera = Game.Camera;
				Game.Gl.UniformMatrix4(CameraID, 1, false, (float*)&camera);
			}
			//------

			Game.Gl.Uniform4(ColorID, new System.Numerics.Vector4(color4.Red, color4.Green, color4.Blue, color4.Alpha)); //変色用のカラーを設定
			Game.Gl.Uniform2(ScaleID, new System.Numerics.Vector2(vcScaleRatio.X, vcScaleRatio.Y)); //変色用のカラーを設定

			//テクスチャの切り抜きの座標と大きさを設定
			Game.Gl.Uniform4(TextureRectID, new System.Numerics.Vector4(
				rc画像内の描画領域.X / rc全画像.Width, rc画像内の描画領域.Y / rc全画像.Height, //始まり
				rc画像内の描画領域.Width / rc全画像.Width, rc画像内の描画領域.Height / rc全画像.Height)); //大きさ、終わりではない

			Game.Gl.Uniform1(NoteModeID, 0);

			//描画-----
			Game.Gl.BindVertexArray(VAO);
			unsafe {
				Game.Gl.DrawElements(PrimitiveType.Triangles, IndicesCount, DrawElementsType.UnsignedInt, (void*)0);//描画!
			}

			BlendHelper.SetBlend(BlendType.Normal);
		}


		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose() {
			if (!this.bDispose完了済み) {
				Game.Gl.DeleteTexture(Texture_); //解放

				this.bDispose完了済み = true;
			}
		}
		//-----------------
		#endregion


		// その他

		#region [ private ]
		//-----------------
		private int _opacity;
		private bool bDispose完了済み;

		private Size t指定されたサイズを超えない最適なテクスチャサイズを返す(Size sz指定サイズ) {
			return sz指定サイズ;
		}

		private int ToArgb(Color4 col4) {
			uint a = (uint)(col4.Alpha * 255.0f) & 255;
			uint r = (uint)(col4.Red * 255.0f) & 255;
			uint g = (uint)(col4.Green * 255.0f) & 255;
			uint b = (uint)(col4.Blue * 255.0f) & 255;

			uint value = b;
			value |= g << 8;
			value |= r << 16;
			value |= a << 24;

			return (int)value;
		}


		// 2012.3.21 さらなる new の省略作戦

		protected Rectangle rc全画像;                              // テクスチャ作ったらあとは不変
		public Color4 color4 = new Color4(1f, 1f, 1f, 1f);  // アルファ以外は不変

		public void tUpdateColor4(Color4 c4) {
			this.color4 = c4;
		}

		public void tUpdateOpacity(int o) {
			this.Opacity = o;
		}
		//-----------------
		#endregion
	}
}
