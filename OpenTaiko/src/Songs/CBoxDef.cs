using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace TJAPlayer3 {
	internal class CBoxDef {
		// プロパティ

		public Color Color;
		public string SelectBG;
		public string Genre;
		public CLocalizationData Title = new CLocalizationData();
		public CLocalizationData[] strBoxText = new CLocalizationData[3] { new CLocalizationData(), new CLocalizationData(), new CLocalizationData() };
		public Color ForeColor;
		public Color BackColor;
		public bool IsChangedForeColor;
		public bool IsChangedBackColor;
		public Color BoxColor;
		public bool IsChangedBoxColor;
		public Color BgColor;
		public bool IsChangedBgColor;
		public string BoxType;
		public string BgType;
		public bool IsChangedBoxType;
		public bool IsChangedBgType;
		public string BoxChara;
		public bool IsChangedBoxChara;
		public string DefaultPreimage;
		public string ScenePreset;

		// コンストラクタ

		public CBoxDef() {
			this.Genre = "";
			ForeColor = Color.White;
			BackColor = Color.Black;
			BoxColor = Color.White;
			BoxType = "0";
			BgType = "0";
			BoxChara = "0";
			BgColor = Color.White;
			DefaultPreimage = null;
			ScenePreset = "";
		}
		public CBoxDef(string boxdefファイル名)
			: this() {
			this.t読み込み(boxdefファイル名);
		}

		// メソッド

		public void t読み込み(string boxdefファイル名) {
			StreamReader reader = new StreamReader(boxdefファイル名, Encoding.GetEncoding(TJAPlayer3.sEncType));
			string str = null;
			while ((str = reader.ReadLine()) != null) {
				if (str.Length != 0) {
					try {
						char[] ignoreCharsWoColon = new char[] { ' ', '\t' };

						str = str.TrimStart(ignoreCharsWoColon);
						if ((str[0] == '#') && (str[0] != ';')) {
							if (str.IndexOf(';') != -1) {
								str = str.Substring(0, str.IndexOf(';'));
							}

							char[] ignoreChars = new char[] { ':', ' ', '\t' };

							var split = str.Split(':');
							if (split.Length == 2) {
								string key = split[0];
								string value = split[1];

								if (key == "#TITLE") {
									this.Title.SetString("default", value.Trim(ignoreChars));
								} else if (key.StartsWith("#TITLE")) {
									string _lang = key.Substring(6).ToLowerInvariant();
									this.Title.SetString(_lang, value.Trim(ignoreChars));
								} else if (key == "#GENRE") {
									this.Genre = value.Trim(ignoreChars);
								} else if (key == "#SELECTBG") {
									this.SelectBG = value.Trim(ignoreChars);
								} else if (key == "#FONTCOLOR") {
									this.Color = ColorTranslator.FromHtml(value.Trim(ignoreChars));
								} else if (key == "#FORECOLOR") {
									this.ForeColor = ColorTranslator.FromHtml(value.Trim(ignoreChars));
									IsChangedForeColor = true;
								} else if (key == "#BACKCOLOR") {
									this.BackColor = ColorTranslator.FromHtml(value.Trim(ignoreChars));
									IsChangedBackColor = true;
								} else if (key == "#BOXCOLOR") {
									this.BoxColor = ColorTranslator.FromHtml(value.Trim(ignoreChars));
									IsChangedBoxColor = true;
								} else if (key == "#BGCOLOR") {
									this.BgColor = ColorTranslator.FromHtml(value.Trim(ignoreChars));
									IsChangedBgColor = true;
								} else if (key == "#BGTYPE") {
									this.BgType = value.Trim(ignoreChars);
									IsChangedBgType = true;
								} else if (key == "#BOXTYPE") {
									this.BoxType = value.Trim(ignoreChars);
									IsChangedBoxType = true;
								} else if (key == "#BOXCHARA") {
									this.BoxChara = value.Trim(ignoreChars);
									IsChangedBoxChara = true;
								} else if (key == "#SCENEPRESET") {
									this.ScenePreset = value.Trim(ignoreChars);
								} else if (key == "#DEFAULTPREIMAGE") {
									this.DefaultPreimage = Path.Combine(Directory.GetParent(boxdefファイル名).FullName, value.Trim(ignoreChars));
								} else {
									for (int i = 0; i < 3; i++) {
										if (key == "#BOXEXPLANATION" + (i + 1).ToString()) {
											this.strBoxText[i].SetString("default", value.Trim(ignoreChars));
										} else if (key.StartsWith("#BOXEXPLANATION") && key.EndsWith((i + 1).ToString())) {
											string _lang = key.Substring(15)[..^1].ToLowerInvariant();
											this.strBoxText[i].SetString(_lang, value.Trim(ignoreChars));
										}
									}
								}
							}
						}
						continue;
					} catch (Exception e) {
						Trace.TraceError(e.ToString());
						Trace.TraceError("例外が発生しましたが処理を継続します。 (178a9a36-a59e-4264-8e4c-b3c3459db43c)");
						continue;
					}
				}
			}
			reader.Close();

			/*
			if (!IsChangedBoxType)
            {
				this.BoxType = this.nStrジャンルtoNum(this.Genre);
            }
			if (!IsChangedBgType)
            {
				this.BgType = this.nStrジャンルtoNum(this.Genre);
			}
			*/
		}



	}
}
