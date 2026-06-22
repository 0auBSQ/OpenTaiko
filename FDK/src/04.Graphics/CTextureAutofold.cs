using Rectangle = System.Drawing.Rectangle;

namespace FDK;

/// <summary>
/// 縦長_横長の画像を自動で折りたたんでテクスチャ化するCTexture。
/// 例えば、768x30 のテクスチャファイルが入力されたら、
/// 内部で256x90 など、2のべき乗サイズに収めるよう、内部でテクスチャ画像を自動的に折り返す。
/// 必要に応じて、正方形テクスチャにもする。
/// また、t2D描画は、その折り返しを加味して実行する。
/// </summary>
public class CTextureAf : CTexture, IDisposable {

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
	public CTextureAf(string strFileName, bool bBlackTransparent) {
		MakeTexture(strFileName, bBlackTransparent);
	}




	public new void MakeTexture(string strFileName, bool bBlackTransparent) {
		if (!File.Exists(strFileName))     // #27122 2012.1.13 from: ImageInformation では FileNotFound 例外は返ってこないので、ここで自分でチェックする。わかりやすいログのために。
			throw new FileNotFoundException(string.Format("ファイルが存在しません。\n[{0}]", strFileName));

		base.MakeTexture(strFileName, bBlackTransparent);
	}

	/// <summary>
	/// 横長画像を適切なサイズに折りたたんだときの最適テクスチャサイズを得る。
	/// 縦長画像に対しては、width/heightを入れ替えて呼び出すこと。
	/// </summary>
	/// <param name="width"></param>
	/// <param name="height"></param>
	/// <param name="foldtimes"></param>
	/// <returns></returns>
	private bool GetFoldedTextureSize(ref int width, ref int height, out int foldtimes) {
		int orgWidth = width, orgHeight = height;

		#region [ widthが、2のべき乗からどれくらい溢れているか確認 ]
		int pow = 1;
		while (orgWidth >= pow) {
			pow *= 2;
		}
		pow /= 2;
		#endregion
		#region [ まず、2のべき乗からあふれる分を折り返して、2のべき乗の正方形サイズに収まるかを確認 ]
		foldtimes = (orgWidth == pow) ? 0 : 1;  // 2のべき乗からの溢れがあれば、まずその溢れ分で1回折り畳む
		if (foldtimes != 0) {
			//Debug.WriteLine( "powちょうどではないので、溢れあり。まずは1回折りたたむ。" );
			// 試しに、widthをpowに切り詰め、1回折り返してみる。
			// width>heightを維持しているなら、テクスチャサイズはより最適な状態になったということになる。
			if (pow <= orgHeight * 2)       // 新width > 新heightを維持できなくなったなら
			{                               // 最適化不可とみなし、baseの処理に委ねる
				return false;
			}
		}
		#endregion
		#region [ width > height ではなくなるまで、折りたたみ続ける ]
		width = pow;
		height = orgHeight * 2;     // 初期値＝1回折りたたんだ状態
		do {
			width /= 2;
			foldtimes = (orgWidth / width) + ((orgWidth % width > 0) ? 1 : 0) - 1;
			height = orgHeight * (foldtimes + 1);
		} while (width > height);
		width *= 2;
		foldtimes = (orgWidth / width) + ((orgWidth % width > 0) ? 1 : 0) - 1;
		height = orgHeight * (foldtimes + 1);
		#endregion

		return true;
	}



	/// <summary>
	/// テクスチャを 2D 画像と見なして描画する。
	/// </summary>
	/// <param name="device">Direct3D9 デバイス。</param>
	/// <param name="x">描画位置（テクスチャの左上位置の X 座標[dot]）。</param>
	/// <param name="y">描画位置（テクスチャの左上位置の Y 座標[dot]）。</param>
	public new void t2DDraw(int x, int y) {
#if TEST_FOLDTEXTURE
			base.t2D描画( x, y, 1f, rc全画像 );
#else
		for (int n = 0; n <= _foldtimes; n++) {
			Rectangle r;
			if (bWideLengthTexture) {
				int currentHeight = n * _orgHeight;
				r = new Rectangle(0, currentHeight, this.rcFullImage.Width, _orgHeight);
				base.t2DDraw(x + n * this.rcFullImage.Width, y, 1f, r);
			} else {
				int currentWidth = n * _orgWidth;
				r = new Rectangle(currentWidth, 0, _orgWidth, this.rcFullImage.Height);
				base.t2DDraw(x, y + n * this.rcFullImage.Height, 1f, r);
			}
		}
#endif
	}
	public new void t2DDraw(int x, int y, Rectangle rc) {
		Rectangle r;
		if (bWideLengthTexture) {
			int beginFold = rc.X / this.rcFullImage.Width;
			int endFold = (rc.X + rc.Width) / rcFullImage.Width;
			for (int i = beginFold; i <= endFold; i++) {
				if (i > _foldtimes) break;

				int newRcY = i * _orgHeight + rc.Y;
				int newRcX = (i == beginFold) ? (rc.X % this.rcFullImage.Width) : 0;
				int newRcWidth = (newRcX + rc.Width > rcFullImage.Width) ? rcFullImage.Width - newRcX : rc.Width;

				r = new Rectangle(newRcX, newRcY, newRcWidth, rc.Height);
				base.t2DDraw(x, y, 1f, r);

				int deltaX = (i == beginFold) ? (i + 1) * rcFullImage.Width - rc.X : rcFullImage.Width;
				int newWidth = rc.Width - deltaX;
				x += deltaX;
				rc.Width = newWidth;
			}
		} else {
			int beginFold = rc.Y / this.rcFullImage.Height;
			int endFold = (rc.Y + rc.Height) / rcFullImage.Height;
			for (int i = beginFold; i <= endFold; i++) {
				if (i > _foldtimes) break;

				int newRcX = i * _orgWidth + rc.X;
				int newRcY = (i == beginFold) ? (rc.Y % this.rcFullImage.Height) : 0;
				int newRcHeight = (newRcY + rc.Height > rcFullImage.Height) ? rcFullImage.Height - newRcY : rc.Height;

				r = new Rectangle(newRcX, newRcY, rc.Width, newRcHeight);
				base.t2DDraw(x, y, 1f, r);

				int deltaY = (i == beginFold) ? (i + 1) * rcFullImage.Height - rc.Y : rcFullImage.Height;
				int newHeight = rc.Height - deltaY;
				y += deltaY;
				rc.Height = newHeight;
			}
		}

	}
	public new void t2DDraw(float x, float y) {
		t2DDraw((int)x, (int)y);
	}
	public void t2DDraw(float x, float y, Rectangle rc) {
		t2DDraw((int)x, (int)y, rc);
	}

	#region [ private ]
	//-----------------
	private bool bWideLengthTexture;

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
