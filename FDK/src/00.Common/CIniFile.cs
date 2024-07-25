using System.Text;

namespace FDK {
	/// <summary>
	/// 汎用的な .iniファイルを扱う。
	/// </summary>

	public class CIniFile {
		// プロパティ

		public string FileName {
			get;
			private set;
		}
		public List<CSection> Sections {
			get;
			set;
		}
		public class CSection {
			public string SectionName = "";
			public List<KeyValuePair<string, string>> Parameters = new List<KeyValuePair<string, string>>();
		}


		// コンストラクタ

		public CIniFile() {
			this.FileName = "";
			this.Sections = new List<CSection>();
		}
		public CIniFile(string fileName)
			: this() {
			this.tRead(fileName);
		}


		// メソッド

		public void tRead(string fileName) {
			this.FileName = fileName;

			StreamReader sr = null;
			CSection section = null;
			try {
				sr = new StreamReader(this.FileName, Encoding.GetEncoding("Shift_JIS"));    // ファイルが存在しない場合は例外発生。

				string line;
				while ((line = sr.ReadLine()) != null) {
					line = line.Replace('\t', ' ').TrimStart(new char[] { '\t', ' ' });
					if (string.IsNullOrEmpty(line) || line[0] == ';')   // ';'以降はコメントとして無視
						continue;

					if (line[0] == '[') {
						#region [ セクションの変更 ]
						//-----------------------------
						var builder = new StringBuilder(32);
						int num = 1;
						while ((num < line.Length) && (line[num] != ']'))
							builder.Append(line[num++]);

						// 変数 section が使用中の場合は、List<CSection> に追加して新しい section を作成する。
						if (section != null)
							this.Sections.Add(section);

						section = new CSection();
						section.SectionName = builder.ToString();
						//-----------------------------
						#endregion

						continue;
					}

					string[] strArray = line.Split(new char[] { '=' });
					if (strArray.Length != 2)
						continue;

					string key = strArray[0].Trim();
					string value = strArray[1].Trim();

					if (section != null && !string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
						section.Parameters.Add(new KeyValuePair<string, string>(key, value));
				}

				if (section != null)
					this.Sections.Add(section);
			} finally {
				if (sr != null)
					sr.Close();
			}
		}
		public void tWrite(string fileName) {
			this.FileName = fileName;
			this.tWrite();
		}
		public void tWrite() {
			StreamWriter sw = null;
			try {
				sw = new StreamWriter(this.FileName, false, Encoding.GetEncoding("Shift_JIS")); // オープン失敗の場合は例外発生。

				foreach (CSection section in this.Sections) {
					sw.WriteLine("[{0}]", section.SectionName);

					foreach (KeyValuePair<string, string> kvp in section.Parameters)
						sw.WriteLine("{0}={1}", kvp.Key, kvp.Value);
				}
			} finally {
				if (sw != null)
					sw.Close();
			}
		}
	}
}
