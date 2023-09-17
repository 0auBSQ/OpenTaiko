using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using Silk.NET.Maths;
using SkiaSharp;

using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;
using SampleFramework;

namespace FDK
{
    public class CTexture : IDisposable
    {
        // プロパティ
        public bool b加算合成
        {
            get;
            set;
        }
        public bool b乗算合成
        {
            get;
            set;
        }
        public bool b減算合成
        {
            get;
            set;
        }
        public bool bスクリーン合成
        {
            get;
            set;
        }
        public float fZ軸中心回転
        {
            get;
            set;
        }
        public int Opacity
        {
            get
            {
                return this._opacity;
            }
            set
            {
                if (value < 0)
                {
                    this._opacity = 0;
                }
                else if (value > 0xff)
                {
                    this._opacity = 0xff;
                }
                else
                {
                    this._opacity = value;
                }
            }
        }
        public Size szテクスチャサイズ
        {
            get;
            private set;
        }
        public Size sz画像サイズ
        {
            get;
            protected set;
        }
        public Vector3D<float> vc拡大縮小倍率;

        // 画面が変わるたび以下のプロパティを設定し治すこと。

        public static Size sz論理画面 = Size.Empty;
        public static Size sz物理画面 = Size.Empty;
        public static Rectangle rc物理画面描画領域 = Rectangle.Empty;
        /// <summary>
        /// <para>論理画面を1とする場合の物理画面の倍率。</para>
        /// <para>論理値×画面比率＝物理値。</para>
        /// </summary>
        public static float f画面比率 = 1.0f;

        internal ITexture Texture_;

        // コンストラクタ

        public CTexture()
        {
            this.sz画像サイズ = new Size(0, 0);
            this.szテクスチャサイズ = new Size(0, 0);
            this._opacity = 0xff;
            this.b加算合成 = false;
            this.fZ軸中心回転 = 0f;
            this.vc拡大縮小倍率 = new Vector3D<float>(1f, 1f, 1f);
            //			this._txData = null;
        }

        public CTexture(CTexture tx)
        {
            this.sz画像サイズ = tx.sz画像サイズ;
            this.szテクスチャサイズ = tx.szテクスチャサイズ;
            this._opacity = tx._opacity;
            this.b加算合成 = tx.b加算合成;
            this.fZ軸中心回転 = tx.fZ軸中心回転;
            this.vc拡大縮小倍率 = tx.vc拡大縮小倍率;
            Texture_ = tx.Texture_;
            //			this._txData = null;
        }

        public void UpdateTexture(CTexture texture, int n幅, int n高さ)
        {
            Texture_ = texture.Texture_;
            this.sz画像サイズ = new Size(n幅, n高さ);
            this.szテクスチャサイズ = this.t指定されたサイズを超えない最適なテクスチャサイズを返す(this.sz画像サイズ);
            this.rc全画像 = new Rectangle(0, 0, this.sz画像サイズ.Width, this.sz画像サイズ.Height);
        }

        public void UpdateTexture(IntPtr texture, int width, int height, RgbaType rgbaType)
        {
            unsafe 
            {
                Texture_?.Dispose();
                void* data = texture.ToPointer();
                Texture_ = Game.GraphicsDevice.GenTexture(data, width, height, rgbaType);
            }
            this.sz画像サイズ = new Size(width, height);
            this.szテクスチャサイズ = this.t指定されたサイズを超えない最適なテクスチャサイズを返す(this.sz画像サイズ);
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
            : this()
        {
            try
            {
                MakeTexture(bitmap, false);
            }
            catch (Exception e)
            {
                this.Dispose();
                throw new CTextureCreateFailedException("ビットマップからのテクスチャの生成に失敗しました。", e);
            }
        }

        public CTexture(int n幅, int n高さ)
            : this()
        {
            try
            {
                this.sz画像サイズ = new Size(n幅, n高さ);
                this.szテクスチャサイズ = this.t指定されたサイズを超えない最適なテクスチャサイズを返す(this.sz画像サイズ);
                this.rc全画像 = new Rectangle(0, 0, this.sz画像サイズ.Width, this.sz画像サイズ.Height);
            }
            catch
            {
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
            : this()
        {
            MakeTexture(strファイル名, b黒を透過する);
        }
        public void MakeTexture(string strファイル名, bool b黒を透過する)
        {
            if (!File.Exists(strファイル名))     // #27122 2012.1.13 from: ImageInformation では FileNotFound 例外は返ってこないので、ここで自分でチェックする。わかりやすいログのために。
                throw new FileNotFoundException(string.Format("ファイルが存在しません。\n[{0}]", strファイル名));

            SKBitmap bitmap = SKBitmap.Decode(strファイル名);
            MakeTexture(bitmap, b黒を透過する);
            bitmap.Dispose();
        }

        public CTexture(SKBitmap bitmap, bool b黒を透過する)
            : this()
        {
            MakeTexture(bitmap, b黒を透過する);
        }
        public void MakeTexture(SKBitmap bitmap, bool b黒を透過する)
        {
            try
            {
                if (bitmap == null)
                {
                    bitmap = new SKBitmap(10, 10);
                }

                unsafe 
                {
                    fixed(void* data = bitmap.Pixels)
                    {
                        Texture_ = Game.GraphicsDevice.GenTexture(data, bitmap.Width, bitmap.Height, RgbaType.Bgra);
                    }
                }

                this.sz画像サイズ = new Size(bitmap.Width, bitmap.Height);
                this.rc全画像 = new Rectangle(0, 0, this.sz画像サイズ.Width, this.sz画像サイズ.Height);
                this.szテクスチャサイズ = this.t指定されたサイズを超えない最適なテクスチャサイズを返す(this.sz画像サイズ);
            }
            catch
            {
                this.Dispose();
                // throw new CTextureCreateFailedException( string.Format( "テクスチャの生成に失敗しました。\n{0}", strファイル名 ) );
                throw new CTextureCreateFailedException(string.Format("テクスチャの生成に失敗しました。\n"));
            }
        }
        // メソッド

        // 2016.11.10 kairera0467 拡張
        // Rectangleを使う場合、座標調整のためにテクスチャサイズの値をそのまま使うとまずいことになるため、Rectragleから幅を取得して調整をする。
        public void t2D中心基準描画(int x, int y)
        {
            this.t2D描画(x - (this.szテクスチャサイズ.Width / 2), y - (this.szテクスチャサイズ.Height / 2), 1f, this.rc全画像);
        }

        public void t2D中心基準描画Mirrored(int x, int y)
        {
            this.t2D左右反転描画(x - (this.szテクスチャサイズ.Width / 2), y - (this.szテクスチャサイズ.Height / 2), 1f, this.rc全画像);
        }

        public void t2D中心基準描画Mirrored(float x, float y)
        {
            this.t2D左右反転描画(x - (this.szテクスチャサイズ.Width / 2), y - (this.szテクスチャサイズ.Height / 2), 1f, this.rc全画像);
        }

        public void t2D中心基準描画(int x, int y, Rectangle rc画像内の描画領域)
        {
            this.t2D描画(x - (rc画像内の描画領域.Width / 2), y - (rc画像内の描画領域.Height / 2), 1f, rc画像内の描画領域);
        }
        public void t2D中心基準描画(float x, float y)
        {
            this.t2D描画((int)x - (this.szテクスチャサイズ.Width / 2), (int)y - (this.szテクスチャサイズ.Height / 2), 1f, this.rc全画像);
        }
        public void t2D中心基準描画(float x, float y, Rectangle rc画像内の描画領域)
        {
            this.t2D描画((int)x - (rc画像内の描画領域.Width / 2), (int)y - (rc画像内の描画領域.Height / 2), 1.0f, rc画像内の描画領域);
        }
        public void t2D中心基準描画(float x, float y, float depth, Rectangle rc画像内の描画領域)
        {
            this.t2D描画((int)x - (rc画像内の描画領域.Width / 2), (int)y - (rc画像内の描画領域.Height / 2), depth, rc画像内の描画領域);
        }

        // 下を基準にして描画する(拡大率考慮)メソッドを追加。 (AioiLight)
        public void t2D拡大率考慮下基準描画(int x, int y)
        {
            this.t2D描画(x, y - (szテクスチャサイズ.Height * this.vc拡大縮小倍率.Y), 1f, this.rc全画像);
        }
        public void t2D拡大率考慮下基準描画(int x, int y, Rectangle rc画像内の描画領域)
        {
            this.t2D描画(x, y - (rc画像内の描画領域.Height * this.vc拡大縮小倍率.Y), 1f, rc画像内の描画領域);
        }
        public void t2D拡大率考慮下中心基準描画(int x, int y)
        {
            this.t2D描画(x - (this.szテクスチャサイズ.Width / 2 * this.vc拡大縮小倍率.X), y - (szテクスチャサイズ.Height * this.vc拡大縮小倍率.Y), 1f, this.rc全画像);
        }

        public void t2D拡大率考慮下中心基準描画Mirrored(int x, int y)
        {
            this.t2D左右反転描画(x - (this.szテクスチャサイズ.Width / 2 * this.vc拡大縮小倍率.X), y - (szテクスチャサイズ.Height * this.vc拡大縮小倍率.Y), 1f, this.rc全画像);
        }
        public void t2D拡大率考慮下中心基準描画Mirrored(float x, float y)
        {
            this.t2D左右反転描画(x - (this.szテクスチャサイズ.Width / 2 * this.vc拡大縮小倍率.X), y - (szテクスチャサイズ.Height * this.vc拡大縮小倍率.Y), 1f, this.rc全画像);
        }

        public void t2D拡大率考慮下基準描画(float x, float y)
        {
            this.t2D描画(x, y - (szテクスチャサイズ.Height * this.vc拡大縮小倍率.Y), 1f, this.rc全画像);
        }
        public void t2D拡大率考慮下基準描画(float x, float y, RectangleF rc画像内の描画領域)
        {
            this.t2D描画(x, y - (rc画像内の描画領域.Height * this.vc拡大縮小倍率.Y), 1f, rc画像内の描画領域);
        }
        public void t2D拡大率考慮下中心基準描画(float x, float y)
        {
            this.t2D拡大率考慮下中心基準描画((int)x, (int)y);
        }

        public void t2D拡大率考慮下中心基準描画(int x, int y, Rectangle rc画像内の描画領域)
        {
            this.t2D描画(x - ((rc画像内の描画領域.Width / 2)), y - (rc画像内の描画領域.Height * this.vc拡大縮小倍率.Y), 1f, rc画像内の描画領域);
        }
        public void t2D拡大率考慮下中心基準描画(float x, float y, Rectangle rc画像内の描画領域)
        {
            this.t2D拡大率考慮下中心基準描画((int)x, (int)y, rc画像内の描画領域);
        }
        public void t2D下中央基準描画(int x, int y)
        {
            this.t2D描画(x - (this.szテクスチャサイズ.Width / 2), y - (szテクスチャサイズ.Height), this.rc全画像);
        }
        public void t2D下中央基準描画(int x, int y, Rectangle rc画像内の描画領域)
        {
            this.t2D描画(x - (rc画像内の描画領域.Width / 2), y - (rc画像内の描画領域.Height), rc画像内の描画領域);
            //this.t2D描画(devicek x, y, rc画像内の描画領域;
        }


        public void t2D拡大率考慮中央基準描画(int x, int y)
        {
            this.t2D描画(x - (this.szテクスチャサイズ.Width / 2 * this.vc拡大縮小倍率.X), y - (szテクスチャサイズ.Height / 2 * this.vc拡大縮小倍率.Y), 1f, this.rc全画像);
        }
        public void t2D拡大率考慮中央基準描画(int x, int y, RectangleF rc)
        {
            this.t2D描画(x - (rc.Width / 2 * this.vc拡大縮小倍率.X), y - (rc.Height / 2 * this.vc拡大縮小倍率.Y), 1f, rc);
        }
        public void t2D_DisplayImage_AnchorCenterLeft(int x, int y, RectangleF rc)
        {
            this.t2D描画(x, y - (rc.Height / 2 * this.vc拡大縮小倍率.Y), 1f, rc);
        }
        public void t2D拡大率考慮上中央基準描画(int x, int y, RectangleF rc)
        {
            this.t2D描画(x - (rc.Width / 2 * this.vc拡大縮小倍率.X), y, 1f, rc);
        }
        public void t2D_DisplayImage_AnchorUpRight(int x, int y, RectangleF rc)
        {
            this.t2D描画(x - (rc.Width * this.vc拡大縮小倍率.X), y, 1f, rc);
        }
        public void t2D拡大率考慮上中央基準描画(int x, int y)
        {
            this.t2D描画(x - (rc全画像.Width / 2 * this.vc拡大縮小倍率.X), y, 1f, rc全画像);
        }
        public void t2D拡大率考慮中央基準描画(float x, float y)
        {
            this.t2D描画(x - (this.szテクスチャサイズ.Width / 2 * this.vc拡大縮小倍率.X), y - (szテクスチャサイズ.Height / 2 * this.vc拡大縮小倍率.Y), 1f, this.rc全画像);
        }
        public void t2D拡大率考慮中央基準描画Mirrored(float x, float y)
        {
            this.t2D左右反転描画(x - (this.szテクスチャサイズ.Width / 2 * this.vc拡大縮小倍率.X), y - (szテクスチャサイズ.Height / 2 * this.vc拡大縮小倍率.Y), 1f, this.rc全画像);
        }
        public void t2D拡大率考慮中央基準描画(float x, float y, RectangleF rc)
        {
            this.t2D描画(x - (rc.Width / 2 * this.vc拡大縮小倍率.X), y - (rc.Height / 2 * this.vc拡大縮小倍率.Y), 1f, rc);
        }
        public void t2D拡大率考慮描画(RefPnt refpnt, float x, float y)
        {
            this.t2D拡大率考慮描画(refpnt, x, y, rc全画像);
        }
        public void t2D拡大率考慮描画(RefPnt refpnt, float x, float y, Rectangle rect)
        {
            this.t2D拡大率考慮描画(refpnt, x, y, 1f, rect);
        }
        public void t2D拡大率考慮描画(RefPnt refpnt, float x, float y, float depth, Rectangle rect)
        {
            switch (refpnt)
            {
                case RefPnt.UpLeft:
                    this.t2D描画(x, y, depth, rect);
                    break;
                case RefPnt.Up:
                    this.t2D描画(x - (rect.Width / 2 * this.vc拡大縮小倍率.X), y, depth, rect);
                    break;
                case RefPnt.UpRight:
                    this.t2D描画(x - rect.Width * this.vc拡大縮小倍率.X, y, depth, rect);
                    break;
                case RefPnt.Left:
                    this.t2D描画(x, y - (rect.Height / 2 * this.vc拡大縮小倍率.Y), depth, rect);
                    break;
                case RefPnt.Center:
                    this.t2D描画(x - (rect.Width / 2 * this.vc拡大縮小倍率.X), y - (rect.Height / 2 * this.vc拡大縮小倍率.Y), depth, rect);
                    break;
                case RefPnt.Right:
                    this.t2D描画(x - rect.Width * this.vc拡大縮小倍率.X, y - (rect.Height / 2 * this.vc拡大縮小倍率.Y), depth, rect);
                    break;
                case RefPnt.DownLeft:
                    this.t2D描画(x, y - rect.Height * this.vc拡大縮小倍率.Y, depth, rect);
                    break;
                case RefPnt.Down:
                    this.t2D描画(x - (rect.Width / 2 * this.vc拡大縮小倍率.X), y - rect.Height * this.vc拡大縮小倍率.Y, depth, rect);
                    break;
                case RefPnt.DownRight:
                    this.t2D描画(x - rect.Width * this.vc拡大縮小倍率.X, y - rect.Height * this.vc拡大縮小倍率.Y, depth, rect);
                    break;
                default:
                    break;
            }

        }

        public enum RefPnt
        {
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

        /// <summary>
        /// テクスチャを 2D 画像と見なして描画する。
        /// </summary>
        /// <param name="device">Direct3D9 デバイス。</param>
        /// <param name="x">描画位置（テクスチャの左上位置の X 座標[dot]）。</param>
        /// <param name="y">描画位置（テクスチャの左上位置の Y 座標[dot]）。</param>
        public void t2D描画(int x, int y)
        {
            this.t2D描画(x, y, 1f, this.rc全画像);
        }
        public void t2D描画(int x, int y, RectangleF rc画像内の描画領域)
        {
            this.t2D描画(x, y, 1f, rc画像内の描画領域);
        }
        public void t2D描画(float x, float y)
        {
            this.t2D描画((int)x, (int)y, 1f, this.rc全画像);
        }
        public void t2D描画(float x, float y, RectangleF rc画像内の描画領域)
        {
            this.t2D描画((int)x, (int)y, 1f, rc画像内の描画領域);
        }
        public void t2D描画(float x, float y, float depth, RectangleF rc画像内の描画領域)
        {
            this.color4.Alpha = this._opacity / 255f;

            float offsetX = rc画像内の描画領域.Width;
            float offsetY = rc画像内の描画領域.Height;

            Matrix4X4<float> mvp = Matrix4X4<float>.Identity;


            Matrix4X4<float> scaling()
            {
                Matrix4X4<float> resizeMatrix = Matrix4X4.CreateScale((float)rc画像内の描画領域.Width / GameWindowSize.Width, (float)rc画像内の描画領域.Height / GameWindowSize.Height, 0.0f);
                Matrix4X4<float> scaleMatrix = Matrix4X4.CreateScale(vc拡大縮小倍率.X, vc拡大縮小倍率.Y, vc拡大縮小倍率.Z);
                return resizeMatrix * scaleMatrix;
            }

            Matrix4X4<float> rotation(float rotate)
            {
                Matrix4X4<float> rotationMatrix = Matrix4X4.CreateScale(1.0f * Game.ScreenAspect, 1.0f, 1.0f);
                rotationMatrix *= 
                Matrix4X4.CreateRotationX(0.0f) * 
                Matrix4X4.CreateRotationY(0.0f) * 
                Matrix4X4.CreateRotationZ(rotate);
                rotationMatrix *= Matrix4X4.CreateScale(1.0f / Game.ScreenAspect, 1.0f, 1.0f);
                
                return rotationMatrix;
            }

            Matrix4X4<float> translation()
            {
                float api_x = (-1 + (x * 2.0f / GameWindowSize.Width));
                float api_y = (-1 + (y * 2.0f / GameWindowSize.Height)) * -1;

                Matrix4X4<float> translation = Matrix4X4.CreateTranslation(api_x, api_y, 0.0f);
                Matrix4X4<float> translation2 = Matrix4X4.CreateTranslation(
                    (rc画像内の描画領域.Width * vc拡大縮小倍率.X / GameWindowSize.Width), 
                    (rc画像内の描画領域.Height * vc拡大縮小倍率.Y / GameWindowSize.Height) * -1, 
                    0.0f);
                return translation * translation2;
            }

            mvp *= scaling();
            mvp *= rotation(fZ軸中心回転);
            mvp *= translation();

            Game.Shader_.SetColor(new Vector4D<float>(color4.Red, color4.Green, color4.Blue, color4.Alpha));
            Vector4D<float> rect = new(
                rc画像内の描画領域.X / rc全画像.Width,
                rc画像内の描画領域.Y / rc全画像.Height,
                rc画像内の描画領域.Width / rc全画像.Width,
                rc画像内の描画領域.Height / rc全画像.Height);
            Game.Shader_.SetTextureRect(rect);
            Game.Shader_.SetMVP(mvp);

            Game.Shader_.SetCamera(Game.Camera);

            if (b加算合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Add);
            }
            else if (b乗算合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Multi);
            }
            else if (b減算合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Sub);
            }
            else if (bスクリーン合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Screen);
            }
            else 
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Normal);
            }
        }
        public void t2D描画(int x, int y, float depth, Rectangle rc画像内の描画領域)
        {
            t2D描画((float)x, (float)y, depth, rc画像内の描画領域);
        }
        public void t2D上下反転描画(int x, int y)
        {
            this.t2D上下反転描画(x, y, 1f, this.rc全画像);
        }
        public void t2D上下反転描画(int x, int y, Rectangle rc画像内の描画領域)
        {
            this.t2D上下反転描画(x, y, 1f, rc画像内の描画領域);
        }
        public void t2D左右反転描画(int x, int y)
        {
            this.t2D左右反転描画(x, y, 1f, this.rc全画像);
        }
        public void t2D左右反転描画(float x, float y)
        {
            this.t2D左右反転描画(x, y, 1f, this.rc全画像);
        }
        public void t2D左右反転描画(int x, int y, Rectangle rc画像内の描画領域)
        {
            this.t2D左右反転描画(x, y, 1f, rc画像内の描画領域);
        }
        public void t2D左右反転描画(float x, float y, float depth, Rectangle rc画像内の描画領域)
        {
            t2D描画(x, y, depth, new RectangleF(rc画像内の描画領域.Width, 0, -rc画像内の描画領域.Width, rc画像内の描画領域.Height));
        }
        public void t2D上下反転描画(int x, int y, float depth, Rectangle rc画像内の描画領域)
        {
            t2D描画(x, y, depth, new RectangleF(0, rc画像内の描画領域.Height, rc画像内の描画領域.Width, -rc画像内の描画領域.Height));
        }
        public void t2D上下反転描画(Point pt)
        {
            this.t2D上下反転描画(pt.X, pt.Y, 1f, this.rc全画像);
        }
        public void t2D上下反転描画(Point pt, Rectangle rc画像内の描画領域)
        {
            this.t2D上下反転描画(pt.X, pt.Y, 1f, rc画像内の描画領域);
        }
        public void t2D上下反転描画(Point pt, float depth, Rectangle rc画像内の描画領域)
        {
            this.t2D上下反転描画(pt.X, pt.Y, depth, rc画像内の描画領域);
        }

        public static Vector3D<float> t論理画面座標をワールド座標へ変換する(int x, int y)
        {
            return CTexture.t論理画面座標をワールド座標へ変換する(new Vector3D<float>((float)x, (float)y, 0f));
        }
        public static Vector3D<float> t論理画面座標をワールド座標へ変換する(float x, float y)
        {
            return CTexture.t論理画面座標をワールド座標へ変換する(new Vector3D<float>(x, y, 0f));
        }
        public static Vector3D<float> t論理画面座標をワールド座標へ変換する(Point pt論理画面座標)
        {
            return CTexture.t論理画面座標をワールド座標へ変換する(new Vector3D<float>(pt論理画面座標.X, pt論理画面座標.Y, 0.0f));
        }
        public static Vector3D<float> t論理画面座標をワールド座標へ変換する(Vector2D<float> v2論理画面座標)
        {
            return CTexture.t論理画面座標をワールド座標へ変換する(new Vector3D<float>(v2論理画面座標, 0f));
        }
        public static Vector3D<float> t論理画面座標をワールド座標へ変換する(Vector3D<float> v3論理画面座標)
        {
            return new Vector3D<float>(
                (v3論理画面座標.X - (CTexture.sz論理画面.Width / 2.0f)) * CTexture.f画面比率,
                (-(v3論理画面座標.Y - (CTexture.sz論理画面.Height / 2.0f)) * CTexture.f画面比率),
                v3論理画面座標.Z);
        }

        /// <summary>
        /// テクスチャを 3D 画像と見なして描画する。
        /// </summary>
        public void t3D描画(Matrix4X4<float> mat)
        {
            this.t3D描画(mat, this.rc全画像);
        }
        public void t3D描画(Matrix4X4<float> mat, Rectangle rc画像内の描画領域)
        {
            float x = ((float)rc画像内の描画領域.Width) / 2f;
            float y = ((float)rc画像内の描画領域.Height) / 2f;
            float z = 0.0f;
            float f左U値 = ((float)rc画像内の描画領域.Left) / ((float)this.szテクスチャサイズ.Width);
            float f右U値 = ((float)rc画像内の描画領域.Right) / ((float)this.szテクスチャサイズ.Width);
            float f上V値 = ((float)rc画像内の描画領域.Top) / ((float)this.szテクスチャサイズ.Height);
            float f下V値 = ((float)rc画像内の描画領域.Bottom) / ((float)this.szテクスチャサイズ.Height);
            this.color4.Alpha = ((float)this._opacity) / 255f;
            int color = ToArgb(this.color4);

            Matrix4X4<float> mvp = mat;

            Game.Shader_.SetColor(new Vector4D<float>(color4.Red, color4.Green, color4.Blue, color4.Alpha));
            Game.Shader_.SetMVP(mvp);

            if (b加算合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Add);
            }
            else if (b乗算合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Multi);
            }
            else if (b減算合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Sub);
            }
            else if (bスクリーン合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Screen);
            }
            else 
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Normal);
            }
        }

        public void t3D左上基準描画(Matrix4X4<float> mat)
        {
            this.t3D左上基準描画(mat, this.rc全画像);
        }
        /// <summary>
        /// ○覚書
        ///   SlimDX.Matrix mat = SlimDX.Matrix.Identity;
        ///   mat *= SlimDX.Matrix.Translation( x, y, z );
        /// 「mat =」ではなく「mat *=」であることを忘れないこと。
        /// </summary>
        public void t3D左上基準描画(Matrix4X4<float> mat, Rectangle rc画像内の描画領域)
        {
            float x = 0.0f;
            float y = 0.0f;
            float z = 0.0f;
            float f左U値 = ((float)rc画像内の描画領域.Left) / ((float)this.szテクスチャサイズ.Width);
            float f右U値 = ((float)rc画像内の描画領域.Right) / ((float)this.szテクスチャサイズ.Width);
            float f上V値 = ((float)rc画像内の描画領域.Top) / ((float)this.szテクスチャサイズ.Height);
            float f下V値 = ((float)rc画像内の描画領域.Bottom) / ((float)this.szテクスチャサイズ.Height);
            this.color4.Alpha = ((float)this._opacity) / 255f;
            int color = ToArgb(this.color4);

            Matrix4X4<float> mvp = mat;

            Game.Shader_.SetColor(new Vector4D<float>(color4.Red, color4.Green, color4.Blue, color4.Alpha));
            Game.Shader_.SetMVP(mvp);
            
            if (b加算合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Add);
            }
            else if (b乗算合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Multi);
            }
            else if (b減算合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Sub);
            }
            else if (bスクリーン合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Screen);
            }
            else 
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Normal);
            }
        }




        public void t2D描画SongObj(float x, float y, float xScale, float yScale)
        {
            this.color4.Alpha = this._opacity / 255f;

            var rc画像内の描画領域 = rc全画像;

            float offsetX = rc画像内の描画領域.Width;
            float offsetY = rc画像内の描画領域.Height;

            Matrix4X4<float> mvp = Matrix4X4<float>.Identity;

            Matrix4X4<float> scaling()
            {
                Matrix4X4<float> resizeMatrix = Matrix4X4.CreateScale((float)rc画像内の描画領域.Width / GameWindowSize.Width, (float)rc画像内の描画領域.Height / GameWindowSize.Height, 0.0f);
                Matrix4X4<float> scaleMatrix = Matrix4X4.CreateScale(xScale, yScale, 1.0f);
                return resizeMatrix * scaleMatrix;
            }

            Matrix4X4<float> rotation(float rotate)
            {
                Matrix4X4<float> rotationMatrix = Matrix4X4.CreateScale(1.0f * Game.ScreenAspect, 1.0f, 1.0f);
                rotationMatrix *= 
                Matrix4X4.CreateRotationX(0.0f) * 
                Matrix4X4.CreateRotationY(0.0f) * 
                Matrix4X4.CreateRotationZ(rotate);
                rotationMatrix *= Matrix4X4.CreateScale(1.0f / Game.ScreenAspect, 1.0f, 1.0f);
                
                return rotationMatrix;
            }

            Matrix4X4<float> translation()
            {
                float api_x = (-1 + (x * 2.0f / GameWindowSize.Width));
                float api_y = (-1 + (y * 2.0f / GameWindowSize.Height)) * -1;

                Matrix4X4<float> translation = Matrix4X4.CreateTranslation(api_x, api_y, 0.0f);
                Matrix4X4<float> translation2 = Matrix4X4.CreateTranslation(
                    (rc画像内の描画領域.Width * xScale / GameWindowSize.Width), 
                    (rc画像内の描画領域.Height * yScale / GameWindowSize.Height) * -1, 
                    0.0f);
                return translation * translation2;
            }

            mvp *= scaling();
            mvp *= rotation(fZ軸中心回転);
            mvp *= translation();

            Game.Shader_.SetColor(new Vector4D<float>(color4.Red, color4.Green, color4.Blue, color4.Alpha));
            Vector4D<float> rect = new(
                rc画像内の描画領域.X / rc全画像.Width,
                rc画像内の描画領域.Y / rc全画像.Height,
                rc画像内の描画領域.Width / rc全画像.Width,
                rc画像内の描画領域.Height / rc全画像.Height);
            Game.Shader_.SetTextureRect(rect);
            Game.Shader_.SetMVP(mvp);

            Game.Shader_.SetCamera(Game.Camera);

            if (b加算合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Add);
            }
            else if (b乗算合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Multi);
            }
            else if (b減算合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Sub);
            }
            else if (bスクリーン合成)
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Screen);
            }
            else 
            {
                Game.GraphicsDevice.DrawPolygon(Game.Polygon_, Game.Shader_, Texture_, BlendType.Normal);
            }
        }


        #region [ IDisposable 実装 ]
        //-----------------
        public void Dispose()
        {
            if (!this.bDispose完了済み)
            {
                Texture_?.Dispose();




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

        private Size t指定されたサイズを超えない最適なテクスチャサイズを返す(Size sz指定サイズ)
        {
            return sz指定サイズ;
        }

        private int ToArgb(Color4 col4)
        {
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

        public void tUpdateColor4(Color4 c4)
        {
            this.color4 = c4;
        }

        public void tUpdateOpacity(int o)
        {
            this.Opacity = o;
        }
                                                            //-----------------
        #endregion
    }
}
