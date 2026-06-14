using System.Drawing;
using Silk.NET.Maths;
using Silk.NET.OpenGLES;
using SkiaSharp;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;

namespace FDK;

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

	private static int NoiseEffectID;
	private static int TimeID;

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
                uniform int noteMode;
				uniform int useNoiseEffect;
				uniform float time;

                varying vec2 texcoord;

				float randomGrayscale(vec2 uv) {
					return fract(sin(dot(uv.xy * 10.0, vec2(12.9898, 78.233))) * (43758.5453 * (time + 1.0) * 0.02));
				}


                void main()
                {
                    vec2 rect;
                    if (noteMode == 1)
                    {
                        rect = textureRect.xy + (texcoord * textureRect.zw * scale);

                        rect = rect - (floor((rect - textureRect.xy) / textureRect.zw) * textureRect.zw);
                    }
                    else
                    {
                        rect = vec2(textureRect.xy + (texcoord * textureRect.zw));
                    }

					vec4 texColor = texture2D(texture1, rect) * color;

					if (useNoiseEffect == 1) {
						float n = randomGrayscale(rect);
						texColor.rgb = vec3(n);
						// texColor.a = 1.0;
					}

                    gl_FragColor = texColor;
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
		NoiseEffectID = Game.Gl.GetUniformLocation(ShaderProgram, "useNoiseEffect");
		TimeID = Game.Gl.GetUniformLocation(ShaderProgram, "time");

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

	// Properties

	public bool bUseNoiseEffect {
		get;
		set;
	}

	public bool bAddBlend {
		get;
		set;
	}
	public bool bMultiplyBlend {
		get;
		set;
	}
	public bool bSubtractBlend {
		get;
		set;
	}
	public bool bScreenBlend {
		get;
		set;
	}
	public float fZAxisCenterRotate {
		get;
		set;
	}
	public float fZRotation {
		get => fZAxisCenterRotate;
		set { fZAxisCenterRotate = value; }
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
	public Size szImageSize {
		get;
		protected set;
	}
	public Vector3D<float> vcScaleRatio;

	// 画面が変わるたび以下のプロパティを設定し治すこと。

	public static Size szLogicLogicScreen = Size.Empty;
	public static Size szPhysicalScreen = Size.Empty;
	public static Rectangle rcPhysicalScreenDrawRegion = Rectangle.Empty;
	/// <summary>
	/// <para>論理画面を1とする場合の物理画面の倍率。</para>
	/// <para>論理値×画面比率＝物理値。</para>
	/// </summary>
	public static float fScreenRatio = 1.0f;

	public uint Pointer { get; internal set; }

	// Constructor

	public CTexture() {
		this.szImageSize = new Size(0, 0);
		this.szTextureSize = new Size(0, 0);
		this._opacity = 0xff;
		this.bAddBlend = false;
		this.fZAxisCenterRotate = 0f;
		this.vcScaleRatio = new Vector3D<float>(1f, 1f, 1f);
		//			this._txData = null;
	}

	public CTexture(CTexture tx) {
		this.szImageSize = tx.szImageSize;
		this.szTextureSize = tx.szTextureSize;
		this._opacity = tx._opacity;
		this.bAddBlend = tx.bAddBlend;
		this.fZAxisCenterRotate = tx.fZAxisCenterRotate;
		this.vcScaleRatio = tx.vcScaleRatio;
		Pointer = tx.Pointer;
		//			this._txData = null;
	}

	public void UpdateTexture(CTexture texture, int nWidth, int nHeight) {
		Pointer = texture.Pointer;
		this.szImageSize = new Size(nWidth, nHeight);
		this.szTextureSize = this.tGetOptimalTextureSize(this.szImageSize);
		this.rcFullImage = new Rectangle(0, 0, this.szImageSize.Width, this.szImageSize.Height);
	}

	public void UpdateTexture(IntPtr texture, int width, int height, PixelFormat rgbaType) {
		unsafe {
			Game.Gl.DeleteTexture(Pointer); //解放
			void* data = texture.ToPointer();
			Pointer = GenTexture(data, (uint)width, (uint)height, rgbaType);
		}
		this.szImageSize = new Size(width, height);
		this.szTextureSize = this.tGetOptimalTextureSize(this.szImageSize);
		this.rcFullImage = new Rectangle(0, 0, this.szImageSize.Width, this.szImageSize.Height);
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

	public CTexture(int nWidth, int nHeight)
		: this() {
		try {
			this.szImageSize = new Size(nWidth, nHeight);
			this.szTextureSize = this.tGetOptimalTextureSize(this.szImageSize);
			this.rcFullImage = new Rectangle(0, 0, this.szImageSize.Width, this.szImageSize.Height);
		} catch (Exception ex) {
			this.Dispose();
			throw new CTextureCreateFailedException(string.Format("テクスチャの生成に失敗しました。\n({0}x{1}, {2})", nWidth, nHeight), innerException: ex);
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
	public CTexture(string strFileName, bool bBlackTransparent)
		: this() {
		MakeTexture(strFileName, bBlackTransparent);
	}
	public void MakeTexture(string strFileName, bool bBlackTransparent) {
		if (!File.Exists(strFileName))     // #27122 2012.1.13 from: ImageInformation では FileNotFound 例外は返ってこないので、ここで自分でチェックする。わかりやすいログのために。
			throw new FileNotFoundException(string.Format("ファイルが存在しません。\n[{0}]", strFileName));

		SKBitmap bitmap = SKBitmap.Decode(strFileName);
		MakeTexture(bitmap, bBlackTransparent);
		bitmap.Dispose();
	}

	public CTexture(SKBitmap bitmap, bool bBlackTransparent)
		: this() {
		MakeTexture(bitmap, bBlackTransparent);
	}

	private unsafe uint GenTexture(void* data, uint width, uint height, PixelFormat pixelFormat) {
		//テクスチャハンドルの作成-----
		uint handle = Game.Gl.GenTexture();
		Game.Gl.BindTexture(TextureTarget.Texture2D, handle);
		//-----

		//テクスチャのデータをVramに送る
		if (OperatingSystem.IsMacOS()) {
			// Desktop OpenGL requires sized internal formats
			int internalFormat = pixelFormat switch {
				PixelFormat.Bgra => (int)InternalFormat.Rgba8,
				PixelFormat.Rgba => (int)InternalFormat.Rgba8,
				PixelFormat.Rgb => (int)InternalFormat.Rgb8,
				_ => (int)InternalFormat.Rgba8
			};
			
			Game.Gl.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, pixelFormat, GLEnum.UnsignedByte, data);
		} else {
			// OpenGL ES allows unsized internal formats
			Game.Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)pixelFormat, width, height, 0, pixelFormat, GLEnum.UnsignedByte, data);
		}
		//-----

		//拡大縮小の時の補完を指定------
		Game.Gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Nearest); //この場合は補完しない
		Game.Gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMinFilter.Nearest);
		//------

		Game.Gl.BindTexture(TextureTarget.Texture2D, 0); //バインドを解除することを忘れないように

		return handle;
	}

	public void MakeTexture(SKBitmap bitmap, bool bBlackTransparent) {
		try {
			if (bitmap == null) {
				bitmap = new SKBitmap(10, 10);
			}

			unsafe {
				fixed (void* data = bitmap.Pixels) {
					if (Thread.CurrentThread.ManagedThreadId == Game.MainThreadID) {
						Pointer = GenTexture(data, (uint)bitmap.Width, (uint)bitmap.Height, PixelFormat.Bgra);
					} else {
						SKBitmap bm = bitmap.Copy();
						Action createInstance = () => {
							fixed (void* data2 = bitmap.Pixels) {
								Pointer = GenTexture(data2, (uint)bitmap.Width, (uint)bitmap.Height, PixelFormat.Bgra);
							}
							bm.Dispose();
						};
						Game.AsyncActions.Add(createInstance);
						while (Game.AsyncActions.Contains(createInstance)) {

						}
					}
				}
			}

			this.szImageSize = new Size(bitmap.Width, bitmap.Height);
			this.rcFullImage = new Rectangle(0, 0, this.szImageSize.Width, this.szImageSize.Height);
			this.szTextureSize = this.tGetOptimalTextureSize(this.szImageSize);
		} catch (Exception ex) {
			this.Dispose();
			// throw new CTextureCreateFailedException( string.Format( "テクスチャの生成に失敗しました。\n{0}", strファイル名 ) );
			throw new CTextureCreateFailedException(string.Format("テクスチャの生成に失敗しました。\n"), innerException: ex);
		}
	}

	public void tSetScale(float x, float y) {
		vcScaleRatio.X = x;
		vcScaleRatio.Y = y;
	}

	// メソッド

	// 2016.11.10 kairera0467 拡張
	// Rectangleを使う場合、座標調整のためにテクスチャサイズの値をそのまま使うとまずいことになるため、Rectragleから幅を取得して調整をする。
	public void t2DCenterBasedDraw(int x, int y) {
		this.t2DDraw(x - (this.szTextureSize.Width / 2), y - (this.szTextureSize.Height / 2), 1f, this.rcFullImage);
	}

	public void t2DCenterBasedDrawMirrored(int x, int y) {
		this.t2DFlipHDraw(x - (this.szTextureSize.Width / 2), y - (this.szTextureSize.Height / 2), 1f, this.rcFullImage);
	}

	public void t2DCenterBasedDrawMirrored(float x, float y) {
		this.t2DFlipHDraw(x - (this.szTextureSize.Width / 2), y - (this.szTextureSize.Height / 2), 1f, this.rcFullImage);
	}

	public void t2DCenterBasedDraw(int x, int y, Rectangle rcImageInDrawRegion) {
		this.t2DDraw(x - (rcImageInDrawRegion.Width / 2), y - (rcImageInDrawRegion.Height / 2), 1f, rcImageInDrawRegion);
	}
	public void t2DCenterBasedDraw(float x, float y) {
		this.t2DDraw((int)x - (this.szTextureSize.Width / 2), (int)y - (this.szTextureSize.Height / 2), 1f, this.rcFullImage);
	}
	public void t2DCenterBasedDraw(float x, float y, Rectangle rcImageInDrawRegion) {
		this.t2DDraw((int)x - (rcImageInDrawRegion.Width / 2), (int)y - (rcImageInDrawRegion.Height / 2), 1.0f, rcImageInDrawRegion);
	}
	public void t2DCenterBasedDraw(float x, float y, float depth, Rectangle rcImageInDrawRegion) {
		this.t2DDraw((int)x - (rcImageInDrawRegion.Width / 2), (int)y - (rcImageInDrawRegion.Height / 2), depth, rcImageInDrawRegion);
	}

	// 下を基準にして描画する(拡大率考慮)メソッドを追加。 (AioiLight)
	public void t2DScaledBottomBasedDraw(int x, int y) {
		this.t2DDraw(x, y - (szTextureSize.Height * this.vcScaleRatio.Y), 1f, this.rcFullImage);
	}
	public void t2DScaledBottomBasedDraw(int x, int y, Rectangle rcImageInDrawRegion) {
		this.t2DDraw(x, y - (rcImageInDrawRegion.Height * this.vcScaleRatio.Y), 1f, rcImageInDrawRegion);
	}
	public void t2DScaledBottomCenterBasedDraw(int x, int y) {
		this.t2DDraw(x - (this.szTextureSize.Width / 2 * this.vcScaleRatio.X), y - (szTextureSize.Height * this.vcScaleRatio.Y), 1f, this.rcFullImage);
	}

	public void t2DScaledBottomCenterBasedDrawMirrored(int x, int y) {
		this.t2DFlipHDraw(x - (this.szTextureSize.Width / 2 * this.vcScaleRatio.X), y - (szTextureSize.Height * this.vcScaleRatio.Y), 1f, this.rcFullImage);
	}
	public void t2DScaledBottomCenterBasedDrawMirrored(float x, float y) {
		this.t2DFlipHDraw(x - (this.szTextureSize.Width / 2 * this.vcScaleRatio.X), y - (szTextureSize.Height * this.vcScaleRatio.Y), 1f, this.rcFullImage);
	}

	public void t2DScaledBottomBasedDraw(float x, float y) {
		this.t2DDraw(x, y - (szTextureSize.Height * this.vcScaleRatio.Y), 1f, this.rcFullImage);
	}
	public void t2DScaledBottomBasedDraw(float x, float y, RectangleF rcImageInDrawRegion) {
		this.t2DDraw(x, y - (rcImageInDrawRegion.Height * this.vcScaleRatio.Y), 1f, rcImageInDrawRegion);
	}
	public void t2DScaledBottomCenterBasedDraw(float x, float y) {
		this.t2DScaledBottomCenterBasedDraw((int)x, (int)y);
	}

	public void t2DScaledBottomCenterBasedDraw(int x, int y, Rectangle rcImageInDrawRegion) {
		this.t2DDraw(x - ((rcImageInDrawRegion.Width / 2)), y - (rcImageInDrawRegion.Height * this.vcScaleRatio.Y), 1f, rcImageInDrawRegion);
	}
	public void t2DScaledBottomCenterBasedDraw(float x, float y, Rectangle rcImageInDrawRegion) {
		this.t2DScaledBottomCenterBasedDraw((int)x, (int)y, rcImageInDrawRegion);
	}
	public void t2DBottomCenterBasedDraw(int x, int y) {
		this.t2DDraw(x - (this.szTextureSize.Width / 2), y - (szTextureSize.Height), this.rcFullImage);
	}
	public void t2DBottomCenterBasedDraw(int x, int y, Rectangle rcImageInDrawRegion) {
		this.t2DDraw(x - (rcImageInDrawRegion.Width / 2), y - (rcImageInDrawRegion.Height), rcImageInDrawRegion);
		//this.t2D描画(devicek x, y, rc画像内の描画領域;
	}

	public void t2D_DisplayImage_RollNote(int x, int y, RectangleF rc) {
		this.t2DDraw(x - (rc.Width / 2 * this.vcScaleRatio.X), y - (rc.Height / 2 * this.vcScaleRatio.Y), 1f, rc, true);
	}

	public void t2DScaledCenterBasedDraw(int x, int y) {
		this.t2DDraw(x - (this.szTextureSize.Width / 2 * this.vcScaleRatio.X), y - (szTextureSize.Height / 2 * this.vcScaleRatio.Y), 1f, this.rcFullImage);
	}
	public void t2DScaledCenterBasedDraw(int x, int y, RectangleF rc) {
		this.t2DDraw(x - (rc.Width / 2 * this.vcScaleRatio.X), y - (rc.Height / 2 * this.vcScaleRatio.Y), 1f, rc);
	}
	public void t2D_DisplayImage_AnchorCenterLeft(int x, int y, RectangleF rc) {
		this.t2DDraw(x, y - (rc.Height / 2 * this.vcScaleRatio.Y), 1f, rc);
	}
	public void t2DScaledTopCenterBasedDraw(int x, int y, RectangleF rc) {
		this.t2DDraw(x - (rc.Width / 2 * this.vcScaleRatio.X), y, 1f, rc);
	}
	public void t2D_DisplayImage_AnchorUpRight(int x, int y, RectangleF rc) {
		this.t2DDraw(x - (rc.Width * this.vcScaleRatio.X), y, 1f, rc);
	}
	public void t2DScaledTopCenterBasedDraw(int x, int y) {
		this.t2DDraw(x - (rcFullImage.Width / 2 * this.vcScaleRatio.X), y, 1f, rcFullImage);
	}
	public void t2DScaledCenterBasedDraw(float x, float y) {
		this.t2DDraw(x - (this.szTextureSize.Width / 2 * this.vcScaleRatio.X), y - (szTextureSize.Height / 2 * this.vcScaleRatio.Y), 1f, this.rcFullImage);
	}
	public void t2DScaledCenterBasedDrawMirrored(float x, float y) {
		this.t2DFlipHDraw(x - (this.szTextureSize.Width / 2 * this.vcScaleRatio.X), y - (szTextureSize.Height / 2 * this.vcScaleRatio.Y), 1f, this.rcFullImage);
	}
	public void t2DScaledCenterBasedDraw(float x, float y, RectangleF rc) {
		this.t2DDraw(x - (rc.Width / 2 * this.vcScaleRatio.X), y - (rc.Height / 2 * this.vcScaleRatio.Y), 1f, rc);
	}
	public void t2DScaledDraw(RefPnt refpnt, float x, float y) {
		this.t2DScaledDraw(refpnt, x, y, rcFullImage);
	}
	public void t2DScaledDraw(RefPnt refpnt, float x, float y, Rectangle rect) {
		this.t2DScaledDraw(refpnt, x, y, 1f, rect);
	}
	public void t2DScaledDraw(RefPnt refpnt, float x, float y, float depth, Rectangle rect) {
		switch (refpnt) {
			case RefPnt.UpLeft:
				this.t2DDraw(x, y, depth, rect);
				break;
			case RefPnt.Up:
				this.t2DDraw(x - (rect.Width / 2 * this.vcScaleRatio.X), y, depth, rect);
				break;
			case RefPnt.UpRight:
				this.t2DDraw(x - rect.Width * this.vcScaleRatio.X, y, depth, rect);
				break;
			case RefPnt.Left:
				this.t2DDraw(x, y - (rect.Height / 2 * this.vcScaleRatio.Y), depth, rect);
				break;
			case RefPnt.Center:
				this.t2DDraw(x - (rect.Width / 2 * this.vcScaleRatio.X), y - (rect.Height / 2 * this.vcScaleRatio.Y), depth, rect);
				break;
			case RefPnt.Right:
				this.t2DDraw(x - rect.Width * this.vcScaleRatio.X, y - (rect.Height / 2 * this.vcScaleRatio.Y), depth, rect);
				break;
			case RefPnt.DownLeft:
				this.t2DDraw(x, y - rect.Height * this.vcScaleRatio.Y, depth, rect);
				break;
			case RefPnt.Down:
				this.t2DDraw(x - (rect.Width / 2 * this.vcScaleRatio.X), y - rect.Height * this.vcScaleRatio.Y, depth, rect);
				break;
			case RefPnt.DownRight:
				this.t2DDraw(x - rect.Width * this.vcScaleRatio.X, y - rect.Height * this.vcScaleRatio.Y, depth, rect);
				break;
			default:
				break;
		}

	}
	public void t2D_DisplayImage_AnchorCenter(int x, int y) {
		this.t2DDraw(x - (this.rcFullImage.Width / 2 * this.vcScaleRatio.X), y - (this.rcFullImage.Height / 2 * this.vcScaleRatio.Y), 1f, this.rcFullImage);
	}
	public void t2D_DisplayImage_AnchorCenter(int x, int y, Rectangle rc) {
		this.t2DDraw(x - (rc.Width / 2 * this.vcScaleRatio.X), y - (rc.Height / 2 * this.vcScaleRatio.Y), 1f, rc);
	}
	public void t2D_DisplayImage_AnchorCenter(int x, int y, RectangleF rc) {
		this.t2DDraw(x - (rc.Width / 2 * this.vcScaleRatio.X), y - (rc.Height / 2 * this.vcScaleRatio.Y), 1f, rc);
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
		this.t2DDraw(x, y, 1f, this.rcFullImage);
	}
	public void t2D_DisplayImage(int x, int y, Rectangle rcImageInDrawRegion) {
		this.t2DDraw(x, y, 1f, rcImageInDrawRegion);
	}
	public void t2D_DisplayImage(int x, int y, RectangleF rc) {
		this.t2DDraw(x, y, 1f, rc);
	}

	/// <summary>
	/// テクスチャを 2D 画像と見なして描画する。
	/// </summary>
	/// <param name="device">Direct3D9 デバイス。</param>
	/// <param name="x">描画位置（テクスチャの左上位置の X 座標[dot]）。</param>
	/// <param name="y">描画位置（テクスチャの左上位置の Y 座標[dot]）。</param>
	public void t2DDraw(int x, int y) {
		this.t2DDraw(x, y, 1f, this.rcFullImage);
	}
	public void t2DDraw(int x, int y, RectangleF rcImageInDrawRegion) {
		this.t2DDraw(x, y, 1f, rcImageInDrawRegion);
	}
	public void t2DDraw(float x, float y) {
		this.t2DDraw((int)x, (int)y, 1f, this.rcFullImage);
	}
	public void t2DDraw(float x, float y, RectangleF rcImageInDrawRegion) {
		this.t2DDraw((int)x, (int)y, 1f, rcImageInDrawRegion);
	}
	public void t2DDraw(float x, float y, float depth, RectangleF rcImageInDrawRegion, bool flipX = false, bool flipY = false, bool rollMode = false) {
		this.color4.Alpha = this._opacity / 255f;

		BlendType blendType;
		if (bAddBlend) {
			blendType = BlendType.Add;
		} else if (bMultiplyBlend) {
			blendType = BlendType.Multi;
		} else if (bSubtractBlend) {
			blendType = BlendType.Sub;
		} else if (bScreenBlend) {
			blendType = BlendType.Screen;
		} else {
			blendType = BlendType.Normal;
		}

		BlendHelper.SetBlend(blendType);

		Game.Gl.UseProgram(ShaderProgram);//Uniform4よりこれが先

		Game.Gl.BindTexture(TextureTarget.Texture2D, Pointer); //テクスチャをバインド

		//MVPを設定----
		unsafe {
			Matrix4X4<float> mvp = Matrix4X4<float>.Identity;

			float gameAspect = (float)GameWindowSize.Width / GameWindowSize.Height;


			//スケーリング-----
			mvp *= Matrix4X4.CreateScale(rcImageInDrawRegion.Width / GameWindowSize.Width, rcImageInDrawRegion.Height / GameWindowSize.Height, 1) *
				   Matrix4X4.CreateScale(flipX ? -vcScaleRatio.X : vcScaleRatio.X, flipY ? -vcScaleRatio.Y : vcScaleRatio.Y, 1.0f);
			//-----

			//回転-----
			mvp *= Matrix4X4.CreateScale(1.0f * gameAspect, 1.0f, 1.0f) * //ここでアスペクト比でスケーリングしないとおかしなことになる
				   Matrix4X4.CreateRotationZ(fZAxisCenterRotate) *
				   Matrix4X4.CreateScale(1.0f / gameAspect, 1.0f, 1.0f);//回転した後戻してあげる
																		//-----

			//移動----
			float offsetX = rcImageInDrawRegion.Width * vcScaleRatio.X / GameWindowSize.Width;
			float offsetY = rcImageInDrawRegion.Height * vcScaleRatio.Y / GameWindowSize.Height;
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
			rcImageInDrawRegion.X / rcFullImage.Width, rcImageInDrawRegion.Y / rcFullImage.Height, //始まり
			rcImageInDrawRegion.Width / rcFullImage.Width, rcImageInDrawRegion.Height / rcFullImage.Height)); //大きさ、終わりではない

		Game.Gl.Uniform1(NoteModeID, rollMode ? 1 : 0);

		float _time = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) % 100;
		Game.Gl.Uniform1(TimeID, _time);
		Game.Gl.Uniform1(NoiseEffectID, bUseNoiseEffect ? 1 : 0);


		//描画-----
		Game.Gl.BindVertexArray(VAO);
		unsafe {
			Game.Gl.DrawElements(PrimitiveType.Triangles, IndicesCount, DrawElementsType.UnsignedInt, (void*)0);//描画!
		}

		BlendHelper.SetBlend(BlendType.Normal);
	}
	public void t2DDraw(int x, int y, float depth, Rectangle rcImageInDrawRegion) {
		t2DDraw((float)x, (float)y, depth, rcImageInDrawRegion);
	}
	public void t2DFlipVDraw(int x, int y) {
		this.t2DFlipVDraw(x, y, 1f, this.rcFullImage);
	}
	public void t2DFlipVDraw(int x, int y, Rectangle rcImageInDrawRegion) {
		this.t2DFlipVDraw(x, y, 1f, rcImageInDrawRegion);
	}
	public void t2DFlipHDraw(int x, int y) {
		this.t2DFlipHDraw(x, y, 1f, this.rcFullImage);
	}
	public void t2DFlipHDraw(float x, float y) {
		this.t2DFlipHDraw(x, y, 1f, this.rcFullImage);
	}
	public void t2DFlipHDraw(int x, int y, Rectangle rcImageInDrawRegion) {
		this.t2DFlipHDraw(x, y, 1f, rcImageInDrawRegion);
	}
	public void t2DFlipHDraw(float x, float y, float depth, Rectangle rcImageInDrawRegion) {
		t2DDraw(x, y, depth, rcImageInDrawRegion, flipX: true);
	}
	public void t2DFlipVDraw(int x, int y, float depth, Rectangle rcImageInDrawRegion) {
		t2DDraw(x, y, depth, rcImageInDrawRegion, flipY: true);
	}
	public void t2DFlipVDraw(Point pt) {
		this.t2DFlipVDraw(pt.X, pt.Y, 1f, this.rcFullImage);
	}
	public void t2DFlipVDraw(Point pt, Rectangle rcImageInDrawRegion) {
		this.t2DFlipVDraw(pt.X, pt.Y, 1f, rcImageInDrawRegion);
	}
	public void t2DFlipVDraw(Point pt, float depth, Rectangle rcImageInDrawRegion) {
		this.t2DFlipVDraw(pt.X, pt.Y, depth, rcImageInDrawRegion);
	}

	public static Vector3D<float> tLogicalToWorldCoord(int x, int y) {
		return CTexture.tLogicalToWorldCoord(new Vector3D<float>((float)x, (float)y, 0f));
	}
	public static Vector3D<float> tLogicalToWorldCoord(float x, float y) {
		return CTexture.tLogicalToWorldCoord(new Vector3D<float>(x, y, 0f));
	}
	public static Vector3D<float> tLogicalToWorldCoord(Point ptLogicalScreenCoord) {
		return CTexture.tLogicalToWorldCoord(new Vector3D<float>(ptLogicalScreenCoord.X, ptLogicalScreenCoord.Y, 0.0f));
	}
	public static Vector3D<float> tLogicalToWorldCoord(Vector2D<float> v2LogicalScreenCoord) {
		return CTexture.tLogicalToWorldCoord(new Vector3D<float>(v2LogicalScreenCoord, 0f));
	}
	public static Vector3D<float> tLogicalToWorldCoord(Vector3D<float> v3LogicalScreenCoord) {
		return new Vector3D<float>(
			(v3LogicalScreenCoord.X - (CTexture.szLogicLogicScreen.Width / 2.0f)) * CTexture.fScreenRatio,
			(-(v3LogicalScreenCoord.Y - (CTexture.szLogicLogicScreen.Height / 2.0f)) * CTexture.fScreenRatio),
			v3LogicalScreenCoord.Z);
	}




	public void t2DDrawSongObj(float x, float y, float xScale, float yScale) {
		this.color4.Alpha = this._opacity / 255f;

		var rcImageInDrawRegion = rcFullImage;
		this.color4.Alpha = this._opacity / 255f;

		BlendType blendType;
		if (bAddBlend) {
			blendType = BlendType.Add;
		} else if (bMultiplyBlend) {
			blendType = BlendType.Multi;
		} else if (bSubtractBlend) {
			blendType = BlendType.Sub;
		} else if (bScreenBlend) {
			blendType = BlendType.Screen;
		} else {
			blendType = BlendType.Normal;
		}

		BlendHelper.SetBlend(blendType);

		Game.Gl.UseProgram(ShaderProgram);//Uniform4よりこれが先

		Game.Gl.BindTexture(TextureTarget.Texture2D, Pointer); //テクスチャをバインド

		//MVPを設定----
		unsafe {
			Matrix4X4<float> mvp = Matrix4X4<float>.Identity;

			float gameAspect = (float)GameWindowSize.Width / GameWindowSize.Height;


			//スケーリング-----
			mvp *= Matrix4X4.CreateScale((float)rcImageInDrawRegion.Width / GameWindowSize.Width, (float)rcImageInDrawRegion.Height / GameWindowSize.Height, 1) *
				   Matrix4X4.CreateScale(xScale, yScale, 1.0f);
			//-----

			//回転-----
			mvp *= Matrix4X4.CreateScale(1.0f * gameAspect, 1.0f, 1.0f) * //ここでアスペクト比でスケーリングしないとおかしなことになる
				   Matrix4X4.CreateRotationZ(fZAxisCenterRotate) *
				   Matrix4X4.CreateScale(1.0f / gameAspect, 1.0f, 1.0f);//回転した後戻してあげる
																		//-----

			//移動----
			float offsetX = rcImageInDrawRegion.Width * xScale / GameWindowSize.Width;
			float offsetY = rcImageInDrawRegion.Height * yScale / GameWindowSize.Height;
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
			rcImageInDrawRegion.X / rcFullImage.Width, rcImageInDrawRegion.Y / rcFullImage.Height, //始まり
			rcImageInDrawRegion.Width / rcFullImage.Width, rcImageInDrawRegion.Height / rcFullImage.Height)); //大きさ、終わりではない

		Game.Gl.Uniform1(NoteModeID, 0);

		float _time = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) % 100;
		Game.Gl.Uniform1(TimeID, _time);
		Game.Gl.Uniform1(NoiseEffectID, bUseNoiseEffect ? 1 : 0);

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
		if (!this.bDisposeCompleteDone) {
			Game.Gl.DeleteTexture(Pointer); //解放

			this.bDisposeCompleteDone = true;
		}
	}
	//-----------------
	#endregion


	// その他

	#region [ private ]
	//-----------------
	private int _opacity;
	private bool bDisposeCompleteDone;

	private Size tGetOptimalTextureSize(Size szSpecifiedSize) {
		return szSpecifiedSize;
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

	protected Rectangle rcFullImage;                              // テクスチャ作ったらあとは不変
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
