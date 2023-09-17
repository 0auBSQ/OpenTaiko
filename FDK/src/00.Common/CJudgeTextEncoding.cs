using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace TJAPlayer3
{
	public class CJudgeTextEncoding
	{
		/// <summary>
		/// Hnc8様のReadJEncを使用して文字コードの判別をする。
		/// </summary>
		public static Encoding JudgeFileEncoding(string path)
		{
			if (!File.Exists(path)) return null;
			Encoding enc;
			FileInfo file = new FileInfo(path);

			using (Hnx8.ReadJEnc.FileReader reader = new Hnx8.ReadJEnc.FileReader(file))
			{
				// 判別読み出し実行。判別結果はReadメソッドの戻り値で把握できます
				Hnx8.ReadJEnc.CharCode c = reader.Read(file);
				// 戻り値のNameプロパティから文字コード名を取得できます
				string name = c.Name;
				Console.WriteLine("【" + name + "】" + file.Name);
				// GetEncoding()を呼び出すと、エンコーディングを取得できます
				enc = c.GetEncoding();
			}
			Debug.Print(path + " Encoding=" + enc.CodePage);

			if (enc == null)
			{
				enc = Encoding.GetEncoding(932);
			}
			return enc;
		}
		/// <summary>
		/// Hnc8様のReadJEncを使用してテキストファイルを読み込む。
		/// 改行文字は、勝手に\nに統一する
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static string ReadTextFile(string path)
		{
			if (!File.Exists(path)) return null;
			string str = null;
			FileInfo file = new FileInfo(path);

			using (Hnx8.ReadJEnc.FileReader reader = new Hnx8.ReadJEnc.FileReader(file))
			{
				reader.Read(file);
				str = reader.Text;
			}

			str = str.Replace(JudgeNewLine(str), "\n");

			return str;
		}

		/// <summary>
		/// Environment.NewLineはプラットフォーム依存である。
		/// だが、ファイルごとに改行コードは違うので、使用すべきではない。
		/// なので、勝手に改行文字を判断する。
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string JudgeNewLine(string str)
		{
			if (str.Contains("\r\n"))
				return ("\r\n");

			if (str.Contains("\r"))
				return ("\r");

			return ("\n");
		}

	}
}