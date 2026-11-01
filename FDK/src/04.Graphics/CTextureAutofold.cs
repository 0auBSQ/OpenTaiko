using Rectangle = System.Drawing.Rectangle;

namespace FDK;

/// <summary>
/// A CTexture loaded from an image file, which must exist.
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
}
