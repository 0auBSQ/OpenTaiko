using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using FDK;

namespace TJAPlayer3 {
	internal class CLang {
		public string Id { get; private set; } = "foo";
		public string Language { get; private set; } = "Unknown Language";
		public Dictionary<string, string> Entries { get; private set; } = new Dictionary<string, string>();
		public string InvalidKey { get; set; } = "KEY NOT FOUND: {0}";

		public string FontName {
			get { return File.Exists(Path.Combine(Folder, _fontName)) ? Path.Combine(Folder, _fontName) : _fontName; }
			private set { _fontName = value; }
		}
		public string BoxFontName {
			get { return File.Exists(Path.Combine(Folder, _boxFontName)) ? Path.Combine(Folder, _boxFontName) : _boxFontName; }
			private set { _boxFontName = value; }
		}
		public string Folder { get { return @$"{TJAPlayer3.strEXEのあるフォルダ}Lang{Path.DirectorySeparatorChar}{Id}{Path.DirectorySeparatorChar}"; } }

		private string _fontName = CFontRenderer.DefaultFontName;
		private string _boxFontName = CFontRenderer.DefaultFontName;

		public CLang(string id) {
			Id = id;
		}
		public static CLang GetCLang(string id) {
			CLang clang = new CLang(id);
			if (clang.LangPathIsValid(out string path)) {
				string data = File.ReadAllText(path);

				JsonNodeOptions options = new JsonNodeOptions() { PropertyNameCaseInsensitive = false };
				JsonDocumentOptions doc = new JsonDocumentOptions() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
				JsonNode node = JsonNode.Parse(data, options, doc);

				clang.Id = id;
				clang.Language = node["Language"].Deserialize<string>();
				clang.Entries = node["Entries"].Deserialize<Dictionary<string, string>>();
				clang.InvalidKey = node["InvalidKey"].Deserialize<string>();
				clang.FontName = node["FontName"].Deserialize<string>();
				clang.BoxFontName = node["BoxFontName"].Deserialize<string>();

				return clang;
			} else {
				Trace.TraceError(@$"Language file for {id} at {path} could not be found. Did you remember to create a lang.json file for {id}?");
				return clang;
			}
		}
		public static string GetLanguage(string id) {
			CLang clang = GetCLang(id);
			return clang.Language;
		}
		private bool LangPathIsValid(out string out_path) {
			out_path = Path.Combine(Folder, "lang.json");
			return File.Exists(out_path);
		}

		public string GetString(string key) {
			return (Entries.TryGetValue(key, out string? value)) ? value : InvalidKey.SafeFormat(key);
		}
		public string GetString(string key, params object?[] values) {
			return (Entries.TryGetValue(key, out string? value)) ? value.SafeFormat(values) : InvalidKey.SafeFormat(key);
		}

		public string GetDifficulty(int diff) {
			switch (diff) {
				case -1:
					return GetString("DIFF_ANY");
				case (int)Difficulty.Easy:
					return GetString("DIFF_EASY");
				case (int)Difficulty.Normal:
					return GetString("DIFF_NORMAL");
				case (int)Difficulty.Hard:
					return GetString("DIFF_HARD");
				case (int)Difficulty.Oni:
					return GetString("DIFF_EX");
				case (int)Difficulty.Edit:
					return GetString("DIFF_EXTRA");
				case (int)Difficulty.Tower:
					return GetString("DIFF_TOWER");
				case (int)Difficulty.Dan:
					return GetString("DIFF_DAN");
				default:
					return GetString("DIFF_UNKNOWN");
			}
		}
		public string GetDifficulty(Difficulty diff) { return GetDifficulty((int)diff); }
		public string GetExamName(int exam) {
			switch (exam) {
				case 0:
					return CLangManager.LangInstance.GetString("DAN_CONDITION_NAME_SOUL");
				case 1:
					return CLangManager.LangInstance.GetString("DAN_CONDITION_NAME_GOOD");
				case 2:
					return CLangManager.LangInstance.GetString("DAN_CONDITION_NAME_OK");
				case 3:
					return CLangManager.LangInstance.GetString("DAN_CONDITION_NAME_BAD");
				case 4:
					return CLangManager.LangInstance.GetString("DAN_CONDITION_NAME_SCORE");
				case 5:
					return CLangManager.LangInstance.GetString("DAN_CONDITION_NAME_ROLL");
				case 6:
					return CLangManager.LangInstance.GetString("DAN_CONDITION_NAME_HIT");
				case 7:
					return CLangManager.LangInstance.GetString("DAN_CONDITION_NAME_COMBO");
				case 8:
					return CLangManager.LangInstance.GetString("DAN_CONDITION_NAME_ACCURACY");
				case 9:
					return CLangManager.LangInstance.GetString("DAN_CONDITION_NAME_ADLIB");
				case 10:
					return CLangManager.LangInstance.GetString("DAN_CONDITION_NAME_BOMB");
				default:
					goto case 0;
			}
		}
	}
}
