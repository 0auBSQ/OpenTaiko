using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using SlimDX;
using SlimDX.Direct3D9;

using Device = SampleFramework.DeviceCache;

namespace FDK
{
	/// <summary>
	/// 縦長_横長の画像を自動で折りたたんでテクスチャ化するCTexture。
	/// 例えば、768x30 のテクスチャファイルが入力されたら、
	/// 内部で256x90 など、2のべき乗サイズに収めるよう、内部でテクスチャ画像を自動的に折り返す。
	/// 必要に応じて、正方形テクスチャにもする。
	/// また、t2D描画は、その折り返しを加味して実行する。
	/// </summary>
	public class CTextureAf : CTexture, IDisposable
	{

				/// <summary>
		/// <para>指定された画像ファイルから Managed テクスチャを作成する。</para>
		/// <para>利用可能な画像形式は、BMP, JPG, PNG, TGA, DDS, PPM, DIB, HDR, PFM のいずれか。</para>
		/// </summary>
		/// <param name="device">Direct3D9 デバイス。</param>
		/// <param name="strファイル名">画像ファイル名。</param>
		/// <param name="format">テクスチャのフォーマット。</param>
		/// <param name="b黒を透過する">画像の黒（0xFFFFFFFF）を透過させるなら true。</param>
		/// <exception cref="CTextureCreateFailedException">テクスチャの作成に失敗しました。</exception>
		public CTextureAf( Device device, string strファイル名, Format format, bool b黒を透過する )
			: this( device, strファイル名, format, b黒を透過する, Pool.Managed )
		{
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
		public CTextureAf( Device device, string strファイル名, Format format, bool b黒を透過する, Pool pool )
		{
			MakeTexture( device, strファイル名, format, b黒を透過する, pool );
		}




		public new void MakeTexture( Device device, string strファイル名, Format format, bool b黒を透過する, Pool pool )
		{
			if ( !File.Exists( strファイル名 ) )		// #27122 2012.1.13 from: ImageInformation では FileNotFound 例外は返ってこないので、ここで自分でチェックする。わかりやすいログのために。
				throw new FileNotFoundException( string.Format( "ファイルが存在しません。\n[{0}]", strファイル名 ) );

			Byte[] _txData = File.ReadAllBytes( strファイル名 );
			bool b条件付きでサイズは２の累乗でなくてもOK = ( device.Capabilities.TextureCaps & TextureCaps.NonPow2Conditional ) != 0;
			bool bサイズは２の累乗でなければならない = ( device.Capabilities.TextureCaps & TextureCaps.Pow2 ) != 0;
			bool b正方形でなければならない = ( device.Capabilities.TextureCaps & TextureCaps.SquareOnly ) != 0;

			// そもそもこんな最適化をしなくてよいのなら、さっさとbaseに処理を委ねて終了
			if ( !bサイズは２の累乗でなければならない && b条件付きでサイズは２の累乗でなくてもOK )
			{
				//Debug.WriteLine( Path.GetFileName( strファイル名 )  + ": 最適化は不要です。" );
				base.MakeTexture( device, strファイル名, format, b黒を透過する, pool );
				return;
			}

			var information = ImageInformation.FromMemory( _txData );
			int orgWidth = information.Width, orgHeight = information.Height;
			int w = orgWidth, h = orgHeight, foldtimes;

			#region [ 折りたたみありで最適なテクスチャサイズがどうなるかを確認する(正方形にするかは考慮せず) ]
			if ( orgWidth >= orgHeight )		// 横長画像なら
			{
				this.b横長のテクスチャである = true;
				if ( !GetFoldedTextureSize( ref w, ref h, out foldtimes ) )
				{
//Debug.WriteLine( Path.GetFileName( strファイル名 ) + ": 最適化を断念。" );
					base.MakeTexture( device, strファイル名, format, b黒を透過する, pool );
					return;
				}
			}
			else								// 縦長画像なら
			{
				this.b横長のテクスチャである = false;
				if ( !GetFoldedTextureSize( ref h, ref w, out foldtimes ) )	// 縦横入れ替えて呼び出し
				{
//Debug.WriteLine( Path.GetFileName( strファイル名 ) + ": 最適化を断念。" );
					base.MakeTexture( device, strファイル名, format, b黒を透過する, pool );
					return;
				}
			}
			#endregion

//Debug.WriteLine( Path.GetFileName( strファイル名 ) + ": texture最適化結果: width=" + w + ", height=" + h + ", 折りたたみ回数=" + foldtimes );
			#region [ 折りたたみテクスチャ画像を作り、テクスチャ登録する ]
			// バイナリ(Byte配列)をBitmapに変換
			MemoryStream mms = new MemoryStream( _txData );
			Bitmap bmpOrg = new Bitmap( mms );
			mms.Close();

			Bitmap bmpNew = new Bitmap( w, h );
			Graphics g = Graphics.FromImage( bmpNew );

			for ( int n = 0; n <= foldtimes; n++ )
			{
				if ( b横長のテクスチャである )
				{
					int x = n * w;
					int currentHeight = n * orgHeight;
					int currentWidth = ( n < foldtimes ) ? w : orgWidth - x;
					Rectangle r = new Rectangle( x, 0, currentWidth, orgHeight );
					Bitmap bmpTmp = bmpOrg.Clone( r, bmpOrg.PixelFormat );				// ここがボトルネックになるようなら、後日unsafeコードにする。
					g.DrawImage( bmpTmp, 0, currentHeight, currentWidth, orgHeight );
					bmpTmp.Dispose();
				}
				else
				{
					int y = n * h;
					int currentWidth = n * orgWidth;
					int currentHeight = ( n < foldtimes ) ? h : orgHeight - y;
					Rectangle r = new Rectangle( 0, y, orgWidth, currentHeight );
					Bitmap bmpTmp = bmpOrg.Clone( r, bmpOrg.PixelFormat );				// ここがボトルネックになるようなら、後日unsafeコードにする。
					g.DrawImage( bmpTmp, currentWidth, 0, orgWidth, currentHeight );
					bmpTmp.Dispose();
				}
			};
			if ( b黒を透過する )
			{
				bmpNew.MakeTransparent( Color.Black );
			}
			g.Dispose();
			g = null;
			bmpOrg.Dispose();
			bmpOrg = null;
			base.MakeTexture( device, bmpNew, format, b黒を透過する, pool );
			bmpNew.Dispose();
			bmpNew = null;
			#endregion

			_orgWidth = orgWidth;
			_orgHeight = orgHeight;
			_foldtimes = foldtimes;
			this.sz画像サイズ = new Size( orgWidth, orgHeight );
		}

		/// <summary>
		/// 横長画像を適切なサイズに折りたたんだときの最適テクスチャサイズを得る。
		/// 縦長画像に対しては、width/heightを入れ替えて呼び出すこと。
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="foldtimes"></param>
		/// <returns></returns>
		private bool GetFoldedTextureSize( ref int width, ref int height, out int foldtimes )
		{
			int orgWidth = width, orgHeight = height;

			#region [ widthが、2のべき乗からどれくらい溢れているか確認 ]
			int pow = 1;
			while ( orgWidth >= pow )
			{
				pow *= 2;
			}
			pow /= 2;
			#endregion
			#region [ まず、2のべき乗からあふれる分を折り返して、2のべき乗の正方形サイズに収まるかを確認 ]
			foldtimes = ( orgWidth == pow ) ? 0 : 1;	// 2のべき乗からの溢れがあれば、まずその溢れ分で1回折り畳む
			if ( foldtimes != 0 )
			{
//Debug.WriteLine( "powちょうどではないので、溢れあり。まずは1回折りたたむ。" );
				// 試しに、widthをpowに切り詰め、1回折り返してみる。
				// width>heightを維持しているなら、テクスチャサイズはより最適な状態になったということになる。
				if ( pow <= orgHeight * 2 )		// 新width > 新heightを維持できなくなったなら
				{								// 最適化不可とみなし、baseの処理に委ねる
					return false;
				}
			}
			#endregion
			#region [ width > height ではなくなるまで、折りたたみ続ける ]
			width = pow;
			height = orgHeight * 2;		// 初期値＝1回折りたたんだ状態
			do
			{
				width /= 2;
				foldtimes = ( orgWidth / width ) + ( ( orgWidth % width > 0 ) ? 1 : 0 ) - 1;
				height = orgHeight * ( foldtimes + 1 );
			} while ( width > height );
			width *= 2;
			foldtimes = ( orgWidth / width ) + ( ( orgWidth % width > 0 ) ? 1 : 0 ) - 1;
			height = orgHeight * ( foldtimes + 1 );
			#endregion

			return true;
		}



		/// <summary>
		/// テクスチャを 2D 画像と見なして描画する。
		/// </summary>
		/// <param name="device">Direct3D9 デバイス。</param>
		/// <param name="x">描画位置（テクスチャの左上位置の X 座標[dot]）。</param>
		/// <param name="y">描画位置（テクスチャの左上位置の Y 座標[dot]）。</param>
		public new void t2D描画( Device device, int x, int y )
		{
#if TEST_FOLDTEXTURE
			base.t2D描画( device, x, y, 1f, rc全画像 );
#else
			for ( int n = 0; n <= _foldtimes; n++ )
			{
				Rectangle r;
				if ( b横長のテクスチャである )
				{
					int currentHeight = n * _orgHeight;
					r = new Rectangle( 0, currentHeight, this.rc全画像.Width, _orgHeight );
					base.t2D描画( device, x + n * this.rc全画像.Width, y, 1f, r );
				}
				else
				{
					int currentWidth = n * _orgWidth;
					r = new Rectangle( currentWidth, 0, _orgWidth, this.rc全画像.Height );
					base.t2D描画( device, x, y + n * this.rc全画像.Height, 1f, r );
				}
			}
#endif
		}
		public new void t2D描画( Device device, int x, int y, Rectangle rc )
		{
			Rectangle r;
			if ( b横長のテクスチャである )
			{
				int beginFold = rc.X / this.rc全画像.Width;
				int endFold = ( rc.X + rc.Width ) / rc全画像.Width;
				for ( int i = beginFold; i <= endFold; i++ )
				{
					if ( i > _foldtimes ) break;

					int newRcY = i * _orgHeight + rc.Y;
					int newRcX = ( i == beginFold ) ? ( rc.X % this.rc全画像.Width ) : 0;
					int newRcWidth = ( newRcX + rc.Width > rc全画像.Width ) ? rc全画像.Width - newRcX : rc.Width;

					r = new Rectangle( newRcX, newRcY, newRcWidth, rc.Height );
					base.t2D描画( device, x, y, 1f, r );

					int deltaX = ( i == beginFold ) ? ( i + 1 ) * rc全画像.Width - rc.X : rc全画像.Width;
					int newWidth = rc.Width - deltaX;
					x += deltaX;
					rc.Width = newWidth;
				}
			}
			else
			{
				int beginFold = rc.Y / this.rc全画像.Height;
				int endFold = ( rc.Y + rc.Height ) / rc全画像.Height;
				for ( int i = beginFold; i <= endFold; i++ )
				{
					if ( i > _foldtimes ) break;

					int newRcX = i * _orgWidth + rc.X;
					int newRcY = ( i == beginFold ) ? ( rc.Y % this.rc全画像.Height ) : 0;
					int newRcHeight = ( newRcY + rc.Height > rc全画像.Height ) ? rc全画像.Height - newRcY : rc.Height;

					r = new Rectangle( newRcX, newRcY, rc.Width, newRcHeight );
					base.t2D描画( device, x, y, 1f, r );

					int deltaY = ( i == beginFold ) ? ( i + 1 ) * rc全画像.Height - rc.Y : rc全画像.Height;
					int newHeight = rc.Height - deltaY;
					y += deltaY;
					rc.Height = newHeight;
				}
			}

		}
		public new void t2D描画( Device device, float x, float y )
		{
			t2D描画( device, (int) x, (int) y );
		}
		public void t2D描画( Device device, float x, float y, Rectangle rc )
		{
			t2D描画( device, (int) x, (int) y, rc );
		}

        /// <summary>
		/// テクスチャを 2D 画像と見なして描画する。
		/// </summary>
		/// <param name="device">Direct3D9 デバイス。</param>
		/// <param name="x">描画位置（テクスチャの左上位置の X 座標[dot]）。</param>
		/// <param name="y">描画位置（テクスチャの左上位置の Y 座標[dot]）。</param>
		public void t3D描画( Device device, Matrix mat, float x, float y )
		{
#if TEST_FOLDTEXTURE
			base.t2D描画( device, x, y, 1f, rc全画像 );
#else
			for ( int n = 0; n <= _foldtimes; n++ )
			{
				Rectangle r;
				if ( b横長のテクスチャである )
				{
					int currentHeight = n * _orgHeight;
					r = new Rectangle( 0, currentHeight, this.rc全画像.Width, _orgHeight );
					base.t3D描画( device, mat );
				}
				else
				{
					int currentWidth = n * _orgWidth;
					r = new Rectangle( currentWidth, 0, _orgWidth, this.rc全画像.Height );
					base.t3D描画( device, mat );
				}
			}
#endif
		}
		public void t3D描画( Device device, Matrix mat, float x, float y, Rectangle rc )
		{
			Rectangle r;
			if ( b横長のテクスチャである )
			{
				int beginFold = rc.X / this.rc全画像.Width;
				int endFold = ( rc.X + rc.Width ) / rc全画像.Width;
				for ( int i = beginFold; i <= endFold; i++ )
				{
					if ( i > _foldtimes ) break;

					int newRcY = i * _orgHeight + rc.Y;
					int newRcX = ( i == beginFold ) ? ( rc.X % this.rc全画像.Width ) : 0;
					int newRcWidth = ( newRcX + rc.Width > rc全画像.Width ) ? rc全画像.Width - newRcX : rc.Width;

					r = new Rectangle( newRcX, newRcY, newRcWidth, rc.Height );
					base.t3D描画( device, mat, r );

					int deltaX = ( i == beginFold ) ? ( i + 1 ) * rc全画像.Width - rc.X : rc全画像.Width;
					int newWidth = rc.Width - deltaX;
					x += deltaX;
					rc.Width = newWidth;
				}
			}
			else
			{
				int beginFold = rc.Y / this.rc全画像.Height;
				int endFold = ( rc.Y + rc.Height ) / rc全画像.Height;
				for ( int i = beginFold; i <= endFold; i++ )
				{
					if ( i > _foldtimes ) break;

					int newRcX = i * _orgWidth + rc.X;
					int newRcY = ( i == beginFold ) ? ( rc.Y % this.rc全画像.Height ) : 0;
					int newRcHeight = ( newRcY + rc.Height > rc全画像.Height ) ? rc全画像.Height - newRcY : rc.Height;

					r = new Rectangle( newRcX, newRcY, rc.Width, newRcHeight );
					base.t3D描画( device, mat, r );

					int deltaY = ( i == beginFold ) ? ( i + 1 ) * rc全画像.Height - rc.Y : rc全画像.Height;
					int newHeight = rc.Height - deltaY;
					y += deltaY;
					rc.Height = newHeight;
				}
			}

		}

		#region [ private ]
		//-----------------
		private bool b横長のテクスチャである;

		/// <summary>
		/// 元画像のWidth
		/// </summary>
		private int _orgWidth;
		/// <summary>
		/// 元画像のHeight
		/// </summary>
		private int _orgHeight;
		/// <summary>
		/// 折りたたみ回数
		/// </summary>
		private int _foldtimes;
		//-----------------
		#endregion
	}
}
