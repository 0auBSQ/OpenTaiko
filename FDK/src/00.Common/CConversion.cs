namespace FDK;

public class CConversion {
	// Properties

	public static readonly string HexChars = "0123456789ABCDEFabcdef";
	public static readonly string Base36Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

	// Methods

	public static bool bONorOFF(char c) {
		return (c != '0');
	}

	public static double DegreeToRadian(double angle) {
		return ((Math.PI * angle) / 180.0);
	}
	public static double RadianToDegree(double angle) {
		return (angle * 180.0 / Math.PI);
	}
	public static float DegreeToRadian(float angle) {
		return (float)DegreeToRadian((double)angle);
	}
	public static float RadianToDegree(float angle) {
		return (float)RadianToDegree((double)angle);
	}

	public static int ClampValue(int value, int min, int max) {
		if (value < min)
			return min;

		if (value > max)
			return max;

		return value;
	}

	public static int ParseIntInRange(string text, int min, int max, int defaultValue) {
		int num;
		if ((int.TryParse(text, out num) && (num >= min)) && (num <= max))
			return num;

		return defaultValue;
	}

	public static double ParseDoubleInRange(string text, double min, double max, double defaultValue) {
		double num;
		if ((double.TryParse(text, out num) && (num >= min)) && (num <= max))
			return num;

		return defaultValue;
	}

	public static int ParseIntInRangeAndClamp(string text, int min, int max, int defaultValue) {
		int num;
		if (int.TryParse(text, out num)) {
			if ((num >= min) && (num <= max))
				return num;
			if (num < min)
				return min;
			if (num > max)
				return max;
		}

		return defaultValue;
	}

	public static int StringToInt(string text, int defaultValue) {
		int num;
		if (!int.TryParse(text, out num))
			num = defaultValue;

		return num;
	}

	private static bool TryParseHexChar(char num, out int digit) {
		digit = HexChars.IndexOf(num);
		if (digit < 0)
			return false;
		// treat lower case as upper case
		if (digit >= 0x10)
			digit -= (0x10 - 10);
		return true;
	}

	public static bool TryParseBase36Char(char num, out int digit) {
		digit = Base36Chars.IndexOf(num);
		if (digit < 0)
			return false;
		// treat lower case as upper case
		if (digit >= 36)
			digit -= (36 - 10);
		return true;
	}

	public static int HexStringToInt(string strNum) =>
		(strNum.Length >= 2
			&& TryParseHexChar(strNum[0], out int digit2)
			&& TryParseHexChar(strNum[1], out int digit1)
		) ? digit2 * 0x10 + digit1
		: -1;

	public static int Base36StringToInt(string strNum) =>
		(strNum.Length >= 2
			&& TryParseBase36Char(strNum[0], out int digit2)
			&& TryParseBase36Char(strNum[1], out int digit1)
		) ? digit2 * 36 + digit1
		: -1;

	public static int ParseSectionNumber(string strNum) =>
		(strNum.Length >= 3
			&& TryParseBase36Char(strNum[0], out int digit3)
			&& TryParseHexChar(strNum[1], out int digit2)
			&& TryParseHexChar(strNum[2], out int digit1)
		) ? digit3 * 100 + digit2 * 10 + digit1
		: -1;

	public static bool TryConvertToHexChar(int num, out char digit) {
		if (num >= 0 && num < 0x10) {
			digit = HexChars[num];
			return true;
		}
		digit = '\0';
		return false;
	}

	public static bool TryConvertToBase36Char(int num, out char digit) {
		if (num >= 0 && num < 36) {
			digit = Base36Chars[num];
			return true;
		}
		digit = '\0';
		return false;
	}

	public static string SectionNumberToString(int num) =>
		(TryConvertToBase36Char(num / 100, out char ch3)
			&& TryConvertToHexChar((num % 100) / 10, out char ch2)
			&& TryConvertToHexChar((num % 100) % 10, out char ch1)
		) ? (ch3.ToString() + ch2.ToString() + ch1.ToString())
		: "000";

	public static string IntToHexString(int num) =>
		(TryConvertToHexChar(num / 0x10, out char ch2)
			&& TryConvertToHexChar(num % 0x10, out char ch1)
		) ? (ch2.ToString() + ch1.ToString())
		: "00";


	public static string IntToBase36String(int num) =>
		(TryConvertToBase36Char(num / 36, out char ch2)
			&& TryConvertToBase36Char(num % 36, out char ch1)
		) ? (ch2.ToString() + ch1.ToString())
		: "00";

	public static int[] StringToIntArray(string str) {
		//0,1,2 ...の形式で書かれたstringをint配列に変換する。
		//一応実装はしたものの、例外処理などはまだ完成していない。
		//str = "0,1,2";
		if (String.IsNullOrEmpty(str))
			return null;

		string[] strArray = str.Split(',');
		List<int> listIntArray;
		listIntArray = new List<int>();

		for (int n = 0; n < strArray.Length; n++) {
			int n追加する数値 = Convert.ToInt32(strArray[n]);
			listIntArray.Add(n追加する数値);
		}
		int[] nArray = new int[] { 1 };
		nArray = listIntArray.ToArray();

		return nArray;
	}

	/// <summary>
	/// Converts a percentage value to a value on a scale of 255 (for opacity).
	/// </summary>
	/// <param name="num"></param>
	/// <returns></returns>
	public static int PercentageTo255(double num) {
		return (int)(255.0 * num);
	}

	/// <summary>
	/// Converts a value from a scale of 255 to a percentage.
	/// </summary>
	/// <param name="num"></param>
	/// <returns></returns>
	public static int N255ToPercentage(int num) {
		return (int)(100.0 / num);
	}

	public static Color4 N255ToColor4(int nR, int nG, int nB) {
		float fR = N255ToPercentage(nR);
		float fG = N255ToPercentage(nG);
		float fB = N255ToPercentage(nB);

		return new Color4(fR, fG, fB, 1f);
	}

	public static Color4 ColorToColor4(System.Drawing.Color col) {
		return new Color4(col.R / 255f, col.G / 255f, col.B / 255f, col.A / 255f);
	}

	public static int[] SeparateDigits(int num) {
		int[] digits = new int[num.ToString().Length];
		for (int i = 0; i < digits.Length; i++) {
			digits[i] = num % 10;
			num /= 10;
		}
		return digits;
	}

	#region [ private ]
	//-----------------

	// private コンストラクタでインスタンス生成を禁止する。
	private CConversion() {
	}
	//-----------------
	#endregion
}
