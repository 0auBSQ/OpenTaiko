using System.Drawing;
using Silk.NET.Maths;
using Silk.NET.OpenGLES;
using SkiaSharp;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;

namespace FDK;

public partial class CTexture : IDisposable {   // streaming subsystem is in CTexture.Streaming.cs
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
	private static int GradientMapSamplerID;
	private static int GradientBlendID;

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
                precision highp float;

                uniform vec4 color;
                uniform sampler2D texture1;
                uniform sampler2D gradientMap;
                uniform vec4 textureRect;
                uniform vec2 scale;
                uniform int noteMode;
				uniform int useNoiseEffect;
				uniform float gradientBlend;
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

					vec4 rawColor = texture2D(texture1, rect);

					if (gradientBlend > 0.0) {
						vec3 straight = rawColor.a > 0.001 ? rawColor.rgb / rawColor.a : vec3(0.0);
						float luma = dot(straight, vec3(0.299, 0.587, 0.114));
						vec4 mapped = texture2D(gradientMap, vec2(luma, 0.5));
						rawColor = vec4(mix(straight, mapped.rgb, gradientBlend), rawColor.a * mapped.a);
					}

					vec4 texColor = rawColor * color;

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
		GradientMapSamplerID = Game.Gl.GetUniformLocation(ShaderProgram, "gradientMap");
		GradientBlendID = Game.Gl.GetUniformLocation(ShaderProgram, "gradientBlend");

		// Bind sampler uniforms to fixed texture units (persists for the program's lifetime).
		// texture1 stays on unit 0 (the default); gradientMap uses unit 1.
		Game.Gl.UseProgram(ShaderProgram);
		Game.Gl.Uniform1(Game.Gl.GetUniformLocation(ShaderProgram, "texture1"), 0);
		Game.Gl.Uniform1(GradientMapSamplerID, 1);

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

	/// <summary>
	/// When non-zero, applied as the gradient map for every draw call until reset to 0.
	/// Takes priority over per-instance <see cref="SetGradientMap"/>.
	/// </summary>
	public static uint ActiveGradientMapId = 0;
	public static float ActiveGradientMapBlend = 1.0f;

	private uint _gradientMapTextureId = 0;
	private float _gradientMapBlend = 1.0f;
	/// <summary>Assigns a permanent gradient map to this texture instance.</summary>
	public void SetGradientMap(uint textureId, float blend = 1.0f) { _gradientMapTextureId = textureId; _gradientMapBlend = blend; }
	/// <summary>Removes the per-instance gradient map.</summary>
	public void ClearGradientMap() { _gradientMapTextureId = 0; _gradientMapBlend = 1.0f; }

	public bool bUseNoiseEffect {
		get;
		set;
	}

	public BlendType blendType = BlendType.Normal;
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

	private TextureWrapMode _wrapMode = TextureWrapMode.ClampToEdge;
	public TextureWrapMode WrapMode {
		get { return _wrapMode; }
		set { SetTextureWrapMode(value); }
	}

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

	public void UpdateTexture(CTexture texture, int width, int height) {
		if (texture.bDisposeCompleteDone)
			return;
		Pointer = texture.Pointer;
		this.szImageSize = new Size(width, height);
		this.szTextureSize = this.tGetOptimalTextureSize(this.szImageSize);
		this.rcFullImage = new Rectangle(0, 0, this.szImageSize.Width, this.szImageSize.Height);

		this.bDisposeCompleteDone = texture.bDisposeCompleteDone;
	}

	public void UpdateTexture(IntPtr texture, int width, int height, PixelFormat rgbaType) {
		if (texture == 0)
			return;
		unsafe {
			Game.Gl.DeleteTexture(Pointer); //解放
			void* data = texture.ToPointer();
			Pointer = GenTexture(data, (uint)width, (uint)height, rgbaType);
		}
		this.szImageSize = new Size(width, height);
		this.szTextureSize = this.tGetOptimalTextureSize(this.szImageSize);
		this.rcFullImage = new Rectangle(0, 0, this.szImageSize.Width, this.szImageSize.Height);
	}

	private int _pixBufW = 0;
	private int _pixBufH = 0;

	// Render-scale: a surface whose GL pixel buffer is smaller than its LOGICAL display size (a 3D scene rendered at
	// reduced internal resolution). When set, the texture lays out + draws at the logical size while sampling the
	// smaller GL texture (UVs are fractions of rcFullImage, so this is transparent). Re-asserted after pixel-buffer
	// reallocations so a CPU-rasterised canvas keeps presenting at full size.
	private int _logicalW = 0, _logicalH = 0;
	public void SetLogicalSize(int w, int h) {
		_logicalW = w; _logicalH = h;
		if (w > 0 && h > 0) {
			this.szImageSize = new Size(w, h);
			this.rcFullImage = new Rectangle(0, 0, w, h);
			this.szTextureSize = this.tGetOptimalTextureSize(this.szImageSize);
		}
	}

	/// <summary>
	/// Creates or updates this texture from a raw RGBA pixel buffer
	/// (length = width * height * 4, top-left origin).
	/// Uses glTexSubImage2D when the size is unchanged (cheap per-frame upload),
	/// otherwise (re)allocates the GL texture. Safe to call off the render thread
	/// (the upload is then deferred to the main thread with a copy of the data).
	/// </summary>
	public void UpdatePixelBuffer(byte[] data, int width, int height) {
		if (data == null || width <= 0 || height <= 0)
			return;

		void DoUpload(byte[] buf) {
			unsafe {
				fixed (byte* p = buf) {
					if (Pointer == 0 || width != _pixBufW || height != _pixBufH) {
						if (Pointer != 0)
							Game.Gl.DeleteTexture(Pointer);
						Pointer = GenTexture(p, (uint)width, (uint)height, PixelFormat.Rgba);
						_pixBufW = width;
						_pixBufH = height;
						this.szImageSize = new Size(width, height);
						this.szTextureSize = this.tGetOptimalTextureSize(this.szImageSize);
						this.rcFullImage = new Rectangle(0, 0, width, height);
						if (_logicalW > 0 && _logicalH > 0) {   // render-scale: keep the logical display size despite the smaller buffer
							this.szImageSize = new Size(_logicalW, _logicalH);
							this.rcFullImage = new Rectangle(0, 0, _logicalW, _logicalH);
							this.szTextureSize = this.tGetOptimalTextureSize(this.szImageSize);
						}
					} else {
						Game.Gl.BindTexture(TextureTarget.Texture2D, Pointer);
						Game.Gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0,
							(uint)width, (uint)height, PixelFormat.Rgba, GLEnum.UnsignedByte, p);
						Game.Gl.BindTexture(TextureTarget.Texture2D, 0);
					}
				}
			}
		}

		if (Thread.CurrentThread.ManagedThreadId == Game.MainThreadID) {
			DoUpload(data);
		} else {
			var copy = (byte[])data.Clone();
			Game.AsyncActions.Enqueue(() => DoUpload(copy));
		}
	}

	private byte[] _regionBuf = null;

	/// <summary>
	/// Updates only a sub-rectangle of the texture from the full RGBA buffer
	/// <paramref name="data"/> (size <paramref name="fullW"/>×<paramref name="fullH"/>).
	/// Far cheaper than re-uploading the whole texture when only a small area changed
	/// (e.g. a brush stroke). Falls back to a full (re)create when needed.
	/// </summary>
	public void UpdatePixelBufferRegion(byte[] data, int fullW, int fullH,
		int rx, int ry, int rw, int rh) {
		if (data == null || fullW <= 0 || fullH <= 0) return;
		// First upload or a resize → must allocate the whole texture.
		if (Pointer == 0 || fullW != _pixBufW || fullH != _pixBufH) {
			UpdatePixelBuffer(data, fullW, fullH);
			return;
		}
		// clamp the region to the texture
		if (rx < 0) { rw += rx; rx = 0; }
		if (ry < 0) { rh += ry; ry = 0; }
		if (rx + rw > fullW) rw = fullW - rx;
		if (ry + rh > fullH) rh = fullH - ry;
		if (rw <= 0 || rh <= 0) return;
		// whole-texture update → upload directly without the row-copy
		if (rx == 0 && ry == 0 && rw == fullW && rh == fullH) {
			UpdatePixelBuffer(data, fullW, fullH);
			return;
		}

		void DoUpload(byte[] buf) {
			int rowBytes = rw * 4;
			int need = rowBytes * rh;
			if (_regionBuf == null || _regionBuf.Length < need)
				_regionBuf = new byte[need];
			// pack the (possibly non-contiguous) sub-rect rows into a tight buffer
			for (int row = 0; row < rh; row++)
				System.Buffer.BlockCopy(buf, ((ry + row) * fullW + rx) * 4, _regionBuf, row * rowBytes, rowBytes);
			unsafe {
				fixed (byte* p = _regionBuf) {
					Game.Gl.BindTexture(TextureTarget.Texture2D, Pointer);
					Game.Gl.TexSubImage2D(TextureTarget.Texture2D, 0, rx, ry,
						(uint)rw, (uint)rh, PixelFormat.Rgba, GLEnum.UnsignedByte, p);
					Game.Gl.BindTexture(TextureTarget.Texture2D, 0);
				}
			}
		}

		if (Thread.CurrentThread.ManagedThreadId == Game.MainThreadID) {
			DoUpload(data);
		} else {
			var copy = (byte[])data.Clone();
			Game.AsyncActions.Enqueue(() => DoUpload(copy));
		}
	}

	/// <summary>
	/// Reads this texture's pixels back from the GPU as a top-left-origin RGBA byte buffer
	/// (length = Width*Height*4). Attaches the texture to a temporary framebuffer and uses
	/// glReadPixels. Returns null if the texture isn't allocated. Main-thread only (GL calls);
	/// note this forces a GPU sync, so use it for one-off operations, not per frame.
	/// </summary>
	public byte[]? ReadPixelsRGBA(out int width, out int height) {
		tRealizeDeferred();   // a never-drawn lazy texture still has pixels to give
		width = this.szImageSize.Width;
		height = this.szImageSize.Height;
		if (Pointer == 0 || width <= 0 || height <= 0) return null;
		byte[] px = new byte[width * height * 4];
		// remember the currently-bound framebuffer so we restore the engine's render target
		uint prevFbo = (uint)Game.Gl.GetInteger(GLEnum.FramebufferBinding);
		uint fbo = Game.Gl.GenFramebuffer();
		Game.Gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
		Game.Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer,
			FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Pointer, 0);
		bool ok = Game.Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == GLEnum.FramebufferComplete;
		if (ok) {
			unsafe {
				fixed (byte* p = px) {
					Game.Gl.ReadPixels(0, 0, (uint)width, (uint)height,
						PixelFormat.Rgba, GLEnum.UnsignedByte, p);
				}
			}
		}
		Game.Gl.BindFramebuffer(FramebufferTarget.Framebuffer, prevFbo);
		Game.Gl.DeleteFramebuffer(fbo);
		return ok ? px : null;
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
	public CTexture(string strFileName, bool bBlackTransparent, int maxDimension)
		: this() {
		MakeTexture(strFileName, bBlackTransparent, maxDimension);
	}
	public void MakeTexture(string strFileName, bool bBlackTransparent) => MakeTexture(strFileName, bBlackTransparent, 0);

	// maxDimension > 0 clamps the DECODED size: the bitmap is downscaled at load time so its long side is at
	// most maxDimension px. Use for oversized art drawn small (song jackets: a 3000x3000 source decodes to
	// 34MB but displays at ~400px). Width/Height then report the clamped size — callers that scale to a
	// target box (w/tex.Width) are unaffected.
	public void MakeTexture(string strFileName, bool bBlackTransparent, int maxDimension) {
		// Remember the source so the LRU cache can re-decode this texture at the same clamp after eviction.
		this._sourcePath = strFileName;
		this._sourceBlackTransparent = bBlackTransparent;
		this._sourceMaxDimension = maxDimension;
		// Async fast path: queue the decode + GL upload instead of doing GL here (see CTexture.Streaming.cs).
		// Triggered by a runtime async load (AsyncLoad) or a load phase (StreamingLoad); SyncForce overrides it
		// (pixels needed now). The GL upload always lands on the render thread (Game.AsyncActions), so this is
		// REQUIRED off the render thread (e.g. the off-thread chart parse's #ADDOBJECT textures) — doing GL there
		// is unreliable. Background decode workers call the MakeTexture(SKBitmap) overload, never this. The texture
		// stays blank (t2DDraw no-ops) until uploaded.
		if (!SyncForce && (StreamingLoad || AsyncLoad)
			&& tQueueAsyncTexture(strFileName, bBlackTransparent, maxDimension))
			return;

		if (!FileExistsCached(strFileName))     // #27122 2012.1.13 from: ImageInformation では FileNotFound 例外は返ってこないので、ここで自分でチェックする。わかりやすいログのために。
			throw new FileNotFoundException(string.Format("ファイルが存在しません。\n[{0}]", strFileName));

		// Read only the header dimensions now and defer the pixel decode + GL upload to first draw,
		// so undrawn textures never reach GPU memory.
		// Skip when maxDimension clamps the size: the header is full-size and would misreport Width/Height.
		// Skip under SyncForce: the caller needs the PIXELS now (e.g. RegisterSpriteFromTexture reads them
		// back immediately) — deferring here left Pointer==0 and every billboard sprite registered empty.
		try {
			using var codec = SKCodec.Create(strFileName);
			if (!SyncForce && maxDimension == 0 && codec != null && codec.Info.Width > 0 && codec.Info.Height > 0) {
				this.szImageSize = new Size(codec.Info.Width, codec.Info.Height);
				this.szTextureSize = this.tGetOptimalTextureSize(this.szImageSize);
				// t2DDraw reads rcFullImage before the deferred upload sets it, so set it here too.
				this.rcFullImage = new Rectangle(0, 0, this.szImageSize.Width, this.szImageSize.Height);
				this._deferred = true;
				return;
			}
		} catch { /* fall through to eager decode */ }

		// Decode straight into the zero-copy upload format (BGRA8888 unpremul) — a plain SKBitmap.Decode
		// yields premul, forcing a full-size conversion pass per image at upload time. A failed decode
		// falls through to MakeTexture's null-guard (blank 10x10), as before.
		SKBitmap bitmap = tClampToMaxDimension(tDecodeForUpload(strFileName), maxDimension);
		MakeTexture(bitmap, bBlackTransparent);
		bitmap?.Dispose();
	}

	// Downscale so the long side is at most maxDimension px (0 = no clamp). Keeps the zero-copy upload
	// format; disposes the original when a resize happens.
	internal static SKBitmap tClampToMaxDimension(SKBitmap bmp, int maxDimension) {
		if (bmp == null || maxDimension <= 0) return bmp;
		int longSide = Math.Max(bmp.Width, bmp.Height);
		if (longSide <= maxDimension) return bmp;
		double f = (double)maxDimension / longSide;
		int dw = Math.Max(1, (int)Math.Round(bmp.Width * f));
		int dh = Math.Max(1, (int)Math.Round(bmp.Height * f));
		var resized = bmp.Resize(new SKImageInfo(dw, dh, SKColorType.Bgra8888, SKAlphaType.Unpremul), SKFilterQuality.Medium);
		if (resized == null) return bmp;
		bmp.Dispose();
		return resized;
	}

	// ── Streamed (deferred) texture loading ───────────────────────────────────────────────────────
	// Implemented in CTexture.Streaming.cs (StreamingLoad / BeginStreaming / StartStreamDecode / PumpUploads
	// / EndStreaming / CancelStreaming / StreamFraction / StreamComplete + tQueueStreamedTexture). The
	// streaming branch at the top of MakeTexture(string) above calls tQueueStreamedTexture.

	public CTexture(SKBitmap bitmap, bool bBlackTransparent)
		: this() {
		MakeTexture(bitmap, bBlackTransparent);
	}

	// iOS GPUs can report a smaller GL_MAX_TEXTURE_SIZE than desktop; uploads larger than it render black.
	// Cached once (0 = not yet queried) and used by MakeTexture(SKBitmap) to downscale oversized bitmaps.
	private static int _maxTextureSize = 0;

	// iOS texture memory cache: LRU eviction on top of the streaming model.
	// Live GPU texture memory: bytes uploaded via GenTexture, freed on Dispose or eviction. Under memory
	// pressure the least-recently-drawn file-backed textures free their GL handle and re-decode on next draw.
	public static long s_gpuTextureBytes = 0;
	public static int s_gpuTextureCount = 0;
	public static long TotalTextureBytes => s_gpuTextureBytes;
	private long _gpuBytes = 0;
	private string? _sourcePath;            // file this texture was loaded from, for re-decode after eviction
	private bool _sourceBlackTransparent;
	private int _sourceMaxDimension;         // clamp applied at load, re-applied when re-decoding after eviction
	private uint _lastDrawnTick;            // Game.TimeMs at last draw — LRU order
	private bool _deferred;                  // Pointer==0 but uploadable from _sourcePath on next draw (lazy-pending OR evicted)
	private static readonly HashSet<CTexture> s_liveTextures = new();
	private static readonly object s_cacheLock = new();

	/// <summary>Free the least-recently-drawn file-backed textures (keeping them re-uploadable) until the
	/// total uploaded texture memory is at or below <paramref name="targetBytes"/>. Render thread (GL context
	/// current). Returns the bytes freed.</summary>
	public static long EvictLeastRecentlyDrawnDownTo(long targetBytes) {
		if (s_gpuTextureBytes <= targetBytes) return 0;
		CTexture[] snapshot;
		lock (s_cacheLock) snapshot = s_liveTextures.ToArray();
		Array.Sort(snapshot, static (a, b) => a._lastDrawnTick.CompareTo(b._lastDrawnTick)); // oldest first
		long freed = 0;
		foreach (var t in snapshot) {
			if (s_gpuTextureBytes <= targetBytes) break;
			if (t._sourcePath == null || t.Pointer == 0) continue;   // not re-loadable (render target/text) ⇒ keep
			long b = t._gpuBytes;
			t.ReleaseGpu();
			freed += b;
		}
		return freed;
	}

	// Free this texture's GL handle but keep it re-uploadable (re-decoded from _sourcePath on next draw).
	internal void ReleaseGpu() {
		if (Pointer == 0 || _sourcePath == null) return;
		try { Game.Gl.DeleteTexture(Pointer); } catch { }
		Pointer = 0;
		if (_gpuBytes > 0) { s_gpuTextureBytes -= _gpuBytes; s_gpuTextureCount--; _gpuBytes = 0; }
		_deferred = true;
		lock (s_cacheLock) s_liveTextures.Remove(this);
	}

	// At the top of every draw: stamp the LRU time, and upload if the texture is deferred
	// (first draw or after eviction).
	private void tCacheOnDraw() {
		_lastDrawnTick = (uint)Game.TimeMs;
		tRealizeDeferred();
	}

	// Realize a deferred/evicted texture NOW (decode from _sourcePath + GL upload). Also used by
	// ReadPixelsRGBA: a readback must see the pixels even if the texture was never drawn.
	private void tRealizeDeferred() {
		if (Pointer == 0 && _deferred && _sourcePath != null && !bDisposeCompleteDone) {
			_deferred = false;
			try { using SKBitmap bmp = tClampToMaxDimension(tDecodeForUpload(_sourcePath), _sourceMaxDimension); if (bmp != null) MakeTexture(bmp, _sourceBlackTransparent); }
			catch { }
		}
	}

	private unsafe uint GenTexture(void* data, uint width, uint height, PixelFormat pixelFormat) {
		//テクスチャハンドルの作成-----
		uint handle = Game.Gl.GenTexture();
		Game.Gl.BindTexture(TextureTarget.Texture2D, handle);
		//-----

		//テクスチャのデータをVramに送る
		if (OperatingSystem.IsIOS()) {
			// iOS ES 2.0 requires the unsized GL_RGBA internalformat for BGRA uploads; sized formats render black.
			int internalFormat = pixelFormat switch {
				PixelFormat.Bgra => (int)InternalFormat.Rgba,
				PixelFormat.Rgba => (int)InternalFormat.Rgba,
				PixelFormat.Rgb => (int)InternalFormat.Rgb,
				_ => (int)InternalFormat.Rgba
			};

			Game.Gl.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, pixelFormat, GLEnum.UnsignedByte, data);
		} else if (OperatingSystem.IsMacOS()) {
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
		Game.Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Linear); //この場合は補完しない
		Game.Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Linear);
		Game.Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)_wrapMode);
		Game.Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)_wrapMode);
		//------

		Game.Gl.BindTexture(TextureTarget.Texture2D, 0); //バインドを解除することを忘れないように

		// Account live GPU bytes (replace this instance's prior figure to handle re-uploads) + register for LRU.
		long newBytes = (long)width * height * 4;
		s_gpuTextureBytes += newBytes - _gpuBytes;
		if (_gpuBytes == 0) s_gpuTextureCount++;
		_gpuBytes = newBytes;
		_deferred = false;
		lock (s_cacheLock) s_liveTextures.Add(this);   // HashSet ⇒ idempotent

		return handle;
	}

	/// <summary>Create a GL texture from a bitmap, ZERO-COPY when possible. The engine blends with straight
	/// (unpremultiplied) alpha, so: if the bitmap is already Unpremul Bgra/Rgba, hand GL its native buffer via
	/// GetPixels() (no managed copy); otherwise read .Pixels, which converts to unpremultiplied BGRA (correct,
	/// but copies the whole image — the old behavior, kept as a safe fallback). Render thread only (calls GL).</summary>
	// Returns a bitmap tGenFromBitmap can upload zero-copy (straight-alpha or opaque BGRA/RGBA), owned by the
	// caller. takeOwnership: src is either returned directly or disposed after conversion; otherwise src is
	// left intact and a copy is returned.
	private static SKBitmap tPrepareUploadBitmap(SKBitmap src, bool takeOwnership) {
		bool eligible = (src.AlphaType == SKAlphaType.Unpremul || src.AlphaType == SKAlphaType.Opaque)
			&& (src.ColorType == SKColorType.Bgra8888 || src.ColorType == SKColorType.Rgba8888);
		if (eligible)
			return takeOwnership ? src : src.Copy();

		var info = new SKImageInfo(src.Width, src.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
		var conv = new SKBitmap(info);
		bool ok;
		using (var pm = src.PeekPixels())
			ok = pm != null && conv.GetPixels() != IntPtr.Zero && pm.ReadPixels(info, conv.GetPixels(), conv.RowBytes);
		if (!ok) {
			conv.Dispose();
			return takeOwnership ? src : src.Copy();   // tGenFromBitmap's fallback converts it instead
		}
		if (takeOwnership) src.Dispose();
		return conv;
	}

	private uint tGenFromBitmap(SKBitmap b) {
		// Zero-copy upload: raw bytes are already straight-alpha (or fully opaque — decoded JPEGs) BGRA/RGBA.
		if ((b.AlphaType == SKAlphaType.Unpremul || b.AlphaType == SKAlphaType.Opaque)
			&& (b.ColorType == SKColorType.Bgra8888 || b.ColorType == SKColorType.Rgba8888)) {
			var fmt = b.ColorType == SKColorType.Rgba8888 ? PixelFormat.Rgba : PixelFormat.Bgra;
			unsafe {
				void* p = (void*)b.GetPixels();
				if (p != null)
					return GenTexture(p, (uint)b.Width, (uint)b.Height, fmt);
			}
		}
		// Everything else (premul PNG decodes, exotic color types): convert to straight-alpha BGRA natively.
		// Never use SKBitmap.Pixels here — it allocates a w*h managed SKColor[] per upload (8-30MB of LOH
		// garbage per decoded image; scrolling song select stacked hundreds of MB of it between Gen2 GCs).
		var info = new SKImageInfo(b.Width, b.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
		using (var conv = new SKBitmap(info))
		using (var pm = b.PeekPixels()) {
			unsafe {
				void* dst = (void*)conv.GetPixels();
				if (dst != null && pm != null && pm.ReadPixels(info, conv.GetPixels(), conv.RowBytes))
					return GenTexture(dst, (uint)b.Width, (uint)b.Height, PixelFormat.Bgra);
			}
		}
		unsafe {
			fixed (void* data = b.Pixels) {
				return GenTexture(data, (uint)b.Width, (uint)b.Height, PixelFormat.Bgra);
			}
		}
	}

	// Live-texture accounting for the [MEMTRACE] debug line: decoded size (w*h*4) is added on MakeTexture
	// and removed on Dispose, so a climbing LiveBytes means textures are created but never disposed.
	public static int LiveCount;
	public static long LiveBytes;
	private long _countedBytes;

	public void MakeTexture(SKBitmap bitmap, bool bBlackTransparent) {
		try {
			if (bitmap == null)
				bitmap = new SKBitmap(10, 10);

			// Scale down if the bitmap exceeds GL_MAX_TEXTURE_SIZE (smaller on iOS GPUs); oversized uploads render black.
			if (_maxTextureSize == 0) {
				_maxTextureSize = Game.Gl.GetInteger(GLEnum.MaxTextureSize);
				if (_maxTextureSize <= 0) _maxTextureSize = 4096;
			}
			SKBitmap scaledBitmap = null;
			if (bitmap.Width > _maxTextureSize || bitmap.Height > _maxTextureSize) {
				float scale = Math.Min((float)_maxTextureSize / bitmap.Width, (float)_maxTextureSize / bitmap.Height);
				int newW = Math.Max(1, (int)(bitmap.Width * scale));
				int newH = Math.Max(1, (int)(bitmap.Height * scale));
				scaledBitmap = new SKBitmap(newW, newH);
				using (var canvas = new SKCanvas(scaledBitmap)) {
					canvas.DrawBitmap(bitmap, new SKRect(0, 0, newW, newH));
				}
				bitmap = scaledBitmap;
			}

			int origW = bitmap.Width, origH = bitmap.Height;   // LOGICAL size: layout + UV (fractions of rcFullImage) use this

			long newBytes = (long)origW * origH * 4;
			Interlocked.Add(ref LiveBytes, newBytes - _countedBytes);
			if (_countedBytes == 0)
				Interlocked.Increment(ref LiveCount);
			_countedBytes = newBytes;

			// Render-scale: downsample the GL texture to cut VRAM + upload bandwidth on low-end machines. szImageSize
			// stays the ORIGINAL size, so every draw (incl. sprite-sheet sub-rects) is unchanged — only sampling detail
			// drops. UVs are fractions of rcFullImage, independent of the GL texture's pixel count, so this is transparent.
			SKBitmap glBitmap = bitmap;
			bool ownGlBitmap = false;
			float rs = Game.RenderScale;
			if (rs < 0.999f) {
				int dw = Math.Max(1, (int)Math.Round(origW * rs));
				int dh = Math.Max(1, (int)Math.Round(origH * rs));
				if (dw < origW || dh < origH) {
					var resized = bitmap.Resize(new SKImageInfo(dw, dh), SKFilterQuality.Medium);
					if (resized != null) { glBitmap = resized; ownGlBitmap = true; }
				}
			}
			if (Thread.CurrentThread.ManagedThreadId == Game.MainThreadID) {
				Pointer = tGenFromBitmap(glBitmap);
				if (ownGlBitmap) glBitmap.Dispose();
			} else {
				// Pre-convert on this (background) thread so the queued render-thread action is a pure
				// zero-copy GL upload — converting a large image there stalls the frame.
				var asyncCopy = tPrepareUploadBitmap(glBitmap, ownGlBitmap);
				Action createInstance = () => {
					try {
						Pointer = tGenFromBitmap(asyncCopy);
					} finally {
						asyncCopy.Dispose();
					}
				};
				Game.AsyncActions.Enqueue(createInstance);
			}

			this.szImageSize = new Size(origW, origH);
			this.rcFullImage = new Rectangle(0, 0, this.szImageSize.Width, this.szImageSize.Height);
			this.szTextureSize = this.tGetOptimalTextureSize(this.szImageSize);

			scaledBitmap?.Dispose();
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

	public void SetTextureWrapMode(TextureWrapMode wrapMode) {
		Game.Gl.BindTexture(TextureTarget.Texture2D, Pointer);

		Game.Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)wrapMode);
		Game.Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)wrapMode);

		Game.Gl.BindTexture(TextureTarget.Texture2D, 0);
		_wrapMode = wrapMode;
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
		tCacheOnDraw();             // LRU stamp + transparent re-upload if this texture was evicted
		if (Pointer == 0) return;   // not-yet-streamed stub or disposed texture ⇒ clean no-op
		this.color4.Alpha = this._opacity / 255f;

		BlendType blend_type = blendType;
		if (blend_type == BlendType.Normal) {
			if (bAddBlend) {
				blend_type = BlendType.Add;
			} else if (bMultiplyBlend) {
				blend_type = BlendType.Multi;
			} else if (bSubtractBlend) {
				blend_type = BlendType.Sub;
			} else if (bScreenBlend) {
				blend_type = BlendType.Screen;
			} else {
				blend_type = BlendType.Normal;
			}
		}

		BlendHelper.SetBlend(blend_type);

		Game.Gl.UseProgram(ShaderProgram);//Uniform4よりこれが先

		Game.Gl.ActiveTexture(TextureUnit.Texture0);
		Game.Gl.BindTexture(TextureTarget.Texture2D, Pointer); //テクスチャをバインド

		uint _gmId = ActiveGradientMapId != 0 ? ActiveGradientMapId : _gradientMapTextureId;
		float _gmBlend = ActiveGradientMapId != 0 ? ActiveGradientMapBlend : _gradientMapBlend;
		if (_gmId != 0) {
			Game.Gl.ActiveTexture(TextureUnit.Texture1);
			Game.Gl.BindTexture(TextureTarget.Texture2D, _gmId);
			Game.Gl.ActiveTexture(TextureUnit.Texture0);
			Game.Gl.Uniform1(GradientBlendID, _gmBlend);
		} else {
			Game.Gl.Uniform1(GradientBlendID, 0.0f);
		}

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

	// ── Solid-rectangle primitive ────────────────────────────────────────────────────────────────────
	// A shared 1×1 white texture, tinted + scaled to draw an arbitrary filled rectangle without needing
	// any asset file. Used for the programmatic loading bars (see CLoadingProgress / the Lua loading
	// screens). Only ever called from the render thread during draw, so lazy GL creation is safe; the
	// single instance lives for the app lifetime (1 texel — negligible) and is never disposed.
	private static CTexture _solidWhite;
	public static void FillBox(int x, int y, int w, int h, int r, int g, int b, int a) {
		if (w <= 0 || h <= 0 || a <= 0)
			return;

		if (_solidWhite == null) {
			using var bmp = new SKBitmap(1, 1);
			bmp.SetPixel(0, 0, new SKColor(0xFF, 0xFF, 0xFF, 0xFF));
			_solidWhite = new CTexture(bmp);
		}

		var tex = _solidWhite;
		// Save/restore: it is a shared instance and other FillBox calls (or a future reuse) must not
		// inherit this call's tint/scale.
		var savedScale = tex.vcScaleRatio;
		var savedColor = tex.color4;
		var savedOpacity = tex.Opacity;

		tex.vcScaleRatio = new Vector3D<float>(w, h, 1f);   // 1px image × (w,h) ⇒ a w×h rectangle
		tex.color4 = new Color4(r / 255f, g / 255f, b / 255f, 1f);
		tex.Opacity = a;
		tex.t2DDraw(x, y);

		tex.vcScaleRatio = savedScale;
		tex.color4 = savedColor;
		tex.Opacity = savedOpacity;
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
		tCacheOnDraw();             // LRU stamp + transparent re-upload if this texture was evicted
		if (Pointer == 0) return;   // not-yet-streamed stub or disposed texture ⇒ clean no-op
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

		Game.Gl.ActiveTexture(TextureUnit.Texture0);
		Game.Gl.BindTexture(TextureTarget.Texture2D, Pointer); //テクスチャをバインド

		uint _gmIdSong = ActiveGradientMapId != 0 ? ActiveGradientMapId : _gradientMapTextureId;
		float _gmBlendSong = ActiveGradientMapId != 0 ? ActiveGradientMapBlend : _gradientMapBlend;
		if (_gmIdSong != 0) {
			Game.Gl.ActiveTexture(TextureUnit.Texture1);
			Game.Gl.BindTexture(TextureTarget.Texture2D, _gmIdSong);
			Game.Gl.ActiveTexture(TextureUnit.Texture0);
			Game.Gl.Uniform1(GradientBlendID, _gmBlendSong);
		} else {
			Game.Gl.Uniform1(GradientBlendID, 0.0f);
		}

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
			this.Pointer = 0;
			s_gpuTextureBytes -= _gpuBytes;
			if (_gpuBytes > 0) s_gpuTextureCount--;
			_gpuBytes = 0;
			lock (s_cacheLock) s_liveTextures.Remove(this);

			if (_countedBytes != 0) {
				Interlocked.Add(ref LiveBytes, -_countedBytes);
				Interlocked.Decrement(ref LiveCount);
				_countedBytes = 0;
			}

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
