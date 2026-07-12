using FDK;
using Color = System.Drawing.Color;

namespace OpenTaiko;

public sealed class TitleTextureKey {

	// Static
	internal static readonly Dictionary<TitleTextureKey, CTexture> _titledictionary
		= new Dictionary<TitleTextureKey, CTexture>();

	public static CTexture ResolveTitleTexture(TitleTextureKey titleTextureKey) {
		if (titleTextureKey == null) return null;
		if (!_titledictionary.TryGetValue(titleTextureKey, out var texture)) {
			texture = GenerateTitleTexture(titleTextureKey);
			_titledictionary.Add(titleTextureKey, texture);
		}

		return texture;
	}

	public static CTexture ResolveTitleTexture(TitleTextureKey titleTextureKey, bool bVertical, bool keepCenter = false) {
		if (titleTextureKey == null) return null;
		if (!_titledictionary.TryGetValue(titleTextureKey, out var texture)) {
			if (bVertical)
				texture = GenerateTitleTextureTate(titleTextureKey, keepCenter);
			else
				texture = GenerateTitleTexture(titleTextureKey, keepCenter);
			_titledictionary.Add(titleTextureKey, texture);
		}

		return texture;
	}

	public static CTexture ResolveTitleTextureTate(TitleTextureKey titleTextureKey) {
		if (titleTextureKey == null) return null;
		if (!_titledictionary.TryGetValue(titleTextureKey, out var texture)) {
			texture = GenerateTitleTextureTate(titleTextureKey);
			_titledictionary.Add(titleTextureKey, texture);
		}

		return texture;
	}

	private static CTexture GenerateTitleTextureTate(TitleTextureKey titleTextureKey, bool keepCenter = false) {
		if (titleTextureKey == null) return null;
		using (var bmp = titleTextureKey.cPrivateFastFont.DrawText_V(
				   titleTextureKey.str, titleTextureKey.forecolor, titleTextureKey.backcolor, titleTextureKey.secondEdge, 30, keepCenter)) {
			CTexture txTextTexture = OpenTaiko.tTextureCreate(bmp, false);
			txTextTexture.CacheKeys[_titledictionary] = titleTextureKey;
			if (txTextTexture.szTextureSize.Height > titleTextureKey.maxWidth) {
				//tx文字テクスチャ.vc拡大縮小倍率.X = (float)(((double)titleTextureKey.maxWidth) / tx文字テクスチャ.szテクスチャサイズ.Height);
				txTextTexture.vcScaleRatio.X = 1.0f;
				txTextTexture.vcScaleRatio.Y = (float)(((double)titleTextureKey.maxWidth) / txTextTexture.szTextureSize.Height);
			}

			return txTextTexture;
		}
	}

	private static CTexture GenerateTitleTexture(TitleTextureKey titleTextureKey, bool keepCenter = false) {
		if (titleTextureKey == null) return null;
		using (var bmp = titleTextureKey.cPrivateFastFont.DrawText(
				   titleTextureKey.str, titleTextureKey.forecolor, titleTextureKey.backcolor, titleTextureKey.secondEdge, 30, keepCenter)) {
			CTexture txTextTexture = OpenTaiko.tTextureCreate(bmp, false);
			txTextTexture.CacheKeys[_titledictionary] = titleTextureKey;
			if (txTextTexture.szTextureSize.Width > titleTextureKey.maxWidth) {
				txTextTexture.vcScaleRatio.X = (float)(((double)titleTextureKey.maxWidth) / txTextTexture.szTextureSize.Width);
				txTextTexture.vcScaleRatio.Y = 1.0f;// (float) (((double) titleTextureKey.maxWidth) / tx文字テクスチャ.szテクスチャサイズ.Width);

			}

			return txTextTexture;
		}
	}

	private static void ClearTitleTextureCache() {
		// Was initially used when disposing the song select screen (at the end of the program), probably unused
		foreach (var titleTexture in _titledictionary.Values) {
			titleTexture.Dispose();
		}

		_titledictionary.Clear();
	}

	// Non-static
	public readonly string str;
	public readonly CCachedFontRenderer cPrivateFastFont;
	public readonly Color forecolor;
	public readonly Color backcolor;
	public readonly int maxWidth;
	public readonly Color? secondEdge;

	public TitleTextureKey(string strText, CCachedFontRenderer cPrivateFastFont, Color forecolor, Color backcolor, int maxHeight, Color? secondEdge = null) {
		this.str = strText;
		this.cPrivateFastFont = cPrivateFastFont;
		this.forecolor = forecolor;
		this.backcolor = backcolor;
		this.maxWidth = maxHeight;
		this.secondEdge = secondEdge;
	}

	private bool Equals(TitleTextureKey other) {
		return string.Equals(str, other.str) &&
			   cPrivateFastFont.Equals(other.cPrivateFastFont) &&
			   forecolor.Equals(other.forecolor) &&
			   backcolor.Equals(other.backcolor) &&
			   secondEdge.Equals(other.secondEdge) &&
			   maxWidth == other.maxWidth;
	}

	public override bool Equals(object obj) {
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		return obj is TitleTextureKey other && Equals(other);
	}

	public override int GetHashCode() {
		unchecked {
			var hashCode = str.GetHashCode();
			hashCode = (hashCode * 397) ^ cPrivateFastFont.GetHashCode();
			hashCode = (hashCode * 397) ^ forecolor.GetHashCode();
			hashCode = (hashCode * 397) ^ backcolor.GetHashCode();
			hashCode = (hashCode * 397) ^ maxWidth;
			if (secondEdge != null)
				hashCode = (hashCode * 397) ^ secondEdge.GetHashCode();
			return hashCode;
		}
	}

	public static bool operator ==(TitleTextureKey left, TitleTextureKey right) {
		return Equals(left, right);
	}

	public static bool operator !=(TitleTextureKey left, TitleTextureKey right) {
		return !Equals(left, right);
	}
}
