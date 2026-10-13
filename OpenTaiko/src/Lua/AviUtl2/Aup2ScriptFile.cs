using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace OpenTaiko.AviUtl2 {
	internal enum Aup2SettingKind { Track, Check, Color, File, Folder, Font, Figure, Select, Text, Value, Dialog }

	// One setting a script declares at its head: a "--track@var:label,min,max,default" line, one of its
	// kin, or an entry of an older "--dialog:" line.
	internal sealed class Aup2ScriptSetting {
		public Aup2SettingKind Kind;
		public string Variable = "";
		public string Label = "";
		// the default as the script wrote it
		public string Default = "";
		public double Number, Min, Max, Step = 1;
		// written the old way: --track0:, --check0:, --color:, --file:, --dialog:
		public bool Legacy;
		// the old numbered ones (track0..3, check0): the number, else -1
		public int Slot = -1;
		// a check whose variable is true or false, not 0 or 1
		public bool Boolean;
		// a dialog entry declared with "local"
		public bool Local;
		// the "/chk", "/col", "/fig" a dialog label ends with
		public string Hint = "";
		public List<KeyValuePair<string, string>> Choices = new();

		// the label without the "group::" in front
		public string ShortLabel {
			get {
				int at = Label.LastIndexOf("::", StringComparison.Ordinal);
				return at < 0 ? Label : Label[(at + 2)..];
			}
		}

		// the names the setting may be saved under in a project, most likely first
		public IEnumerable<string> Keys() {
			if (Label.Length > 0) yield return Label;
			if (ShortLabel != Label && ShortLabel.Length > 0) yield return ShortLabel;
			if (Hint.Length > 0) yield return Label + "/" + Hint;
			if (Variable.Length > 0) yield return Variable;
			if (Kind == Aup2SettingKind.Track && Slot >= 0) yield return "track" + Slot.ToString(CultureInfo.InvariantCulture);
			if (Kind == Aup2SettingKind.Check && Slot >= 0) yield return "check" + Slot.ToString(CultureInfo.InvariantCulture);
			if (Legacy && Kind == Aup2SettingKind.Color) yield return "color";
			if (Legacy && Kind == Aup2SettingKind.File) yield return "file";
		}
	}

	// One script of a file: its name, its settings and its Lua code.
	internal sealed class Aup2ScriptSection {
		public string Name = "";
		public Aup2ScriptFile File = null!;
		public List<Aup2ScriptSetting> Settings { get; } = new();
		// code the old "--param:" and odd "--dialog:" entries ask to run first
		public List<string> Preamble { get; } = new();
		public string Code = "";
		// the line of the file the code starts at, from 1
		public int FirstLine = 1;
		public bool Filter;
		public string Information = "";

		public Aup2ScriptSetting? Find(string variable) {
			foreach (var s in Settings)
				if (s.Variable == variable) return s;
			return null;
		}

		public Aup2ScriptSetting? Track(int slot) {
			foreach (var s in Settings)
				if (s.Kind == Aup2SettingKind.Track && s.Slot == slot) return s;
			return null;
		}
	}

	// A script file (*.anm2, *.obj2 and the older *.anm, *.obj): its text and the scripts in it.
	internal sealed class Aup2ScriptFile {
		public string Path = "";
		// the file's name without its extension and without the "@" of a file of several scripts
		public string Stem = "";
		public bool Legacy;
		public bool Multi;
		public List<Aup2ScriptSection> Sections { get; } = new();

		private static readonly string[] NewKinds = { ".anm2", ".obj2", ".cam2", ".scn2", ".tra2" };
		private static readonly string[] OldKinds = { ".anm", ".obj", ".cam", ".scn", ".tra" };

		public static bool IsScript(string path) => IsLegacy(path) || Array.IndexOf(NewKinds, System.IO.Path.GetExtension(path).ToLowerInvariant()) >= 0;
		public static bool IsLegacy(string path) => Array.IndexOf(OldKinds, System.IO.Path.GetExtension(path).ToLowerInvariant()) >= 0;

		// the older files are in Shift-JIS, the newer ones in utf-8; a byte order mark wins over both
		public static string Decode(byte[] bytes, bool legacy) {
			if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
			if (!legacy) return Encoding.UTF8.GetString(bytes);
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			return Encoding.GetEncoding(932).GetString(bytes);
		}

		public static Aup2ScriptFile Load(string path) => Parse(Decode(System.IO.File.ReadAllBytes(path), IsLegacy(path)), path);

		// `path` only gives the name and the kind: nothing is read from it
		public static Aup2ScriptFile Parse(string text, string path) {
			string name = System.IO.Path.GetFileNameWithoutExtension(path);
			var file = new Aup2ScriptFile { Path = path, Legacy = IsLegacy(path), Multi = name.StartsWith('@') };
			file.Stem = file.Multi ? name[1..] : name;
			var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

			if (!file.Multi) {
				file.Sections.Add(Section(file, file.Stem, lines, 0, lines.Length));
				return file;
			}
			// every "@name" line starts a script; what stands before the first one belongs to none
			int start = -1;
			string current = "";
			for (int i = 0; i <= lines.Length; i++) {
				bool head = i < lines.Length && lines[i].Length > 1 && lines[i][0] == '@';
				if (i < lines.Length && !head) continue;
				if (start >= 0) file.Sections.Add(Section(file, current, lines, start, i));
				if (head) { current = lines[i][1..].Trim(); start = i + 1; }
			}
			return file;
		}

		private static Aup2ScriptSection Section(Aup2ScriptFile file, string name, string[] lines, int from, int to) {
			var s = new Aup2ScriptSection { Name = name, File = file, FirstLine = from + 1 };
			s.Code = string.Join("\n", lines, from, to - from);
			for (int i = from; i < to; i++) {
				string line = lines[i];
				if (line.Length < 4 || line[0] != '-' || line[1] != '-' || line[2] == '-' || line[2] == '[' || line[2] == ' ') continue;
				ReadSetting(s, line[2..].TrimEnd());
			}
			return s;
		}

		private static double Num(string text, double fallback = 0) {
			text = text.Trim();
			if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && long.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hex)) return hex;
			return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : fallback;
		}

		private static void ReadSetting(Aup2ScriptSection s, string line) {
			int colon = line.IndexOf(':');
			string head = colon < 0 ? line : line[..colon], body = colon < 0 ? "" : line[(colon + 1)..];
			int at = head.IndexOf('@');
			string word = at < 0 ? head : head[..at], variable = at < 0 ? "" : head[(at + 1)..].Trim();

			if (at < 0) {
				// the old forms, and the lines that carry no setting
				if (word.Length == 6 && word.StartsWith("track", StringComparison.Ordinal) && char.IsDigit(word[5])) {
					var p = body.Split(',');
					var t = new Aup2ScriptSetting { Kind = Aup2SettingKind.Track, Legacy = true, Slot = word[5] - '0', Variable = word, Label = p[0].Trim() };
					Range(t, p);
					s.Settings.Add(t);
				} else if (word.Length == 6 && word.StartsWith("check", StringComparison.Ordinal) && char.IsDigit(word[5])) {
					var p = body.Split(',');
					s.Settings.Add(new Aup2ScriptSetting {
						Kind = Aup2SettingKind.Check, Legacy = true, Boolean = true, Slot = word[5] - '0', Variable = word, Label = p[0].Trim(),
						Default = p.Length > 1 ? p[1].Trim() : "0", Number = p.Length > 1 ? Truth(p[1]) : 0,
					});
				} else if (word == "color" && colon >= 0) {
					s.Settings.Add(new Aup2ScriptSetting { Kind = Aup2SettingKind.Color, Legacy = true, Variable = "color", Label = "color", Default = body.Trim(), Number = Num(body, 0xffffff) });
				} else if (word == "file" && colon >= 0) {
					s.Settings.Add(new Aup2ScriptSetting { Kind = Aup2SettingKind.File, Legacy = true, Variable = "file", Label = "file", Default = body.Trim() });
				} else if (word == "dialog" && colon >= 0) {
					foreach (string entry in SplitOutside(body, ';')) ReadDialog(s, entry);
				} else if (word == "param" && colon >= 0) {
					if (body.Trim().Length > 0) s.Preamble.Add(body.Trim());
				} else if (word == "filter") {
					s.Filter = true;
				} else if (word == "information") {
					s.Information = body.Trim();
				}
				return;
			}
			if (variable.Length == 0) return;

			int comma = body.IndexOf(',');
			string label = (comma < 0 ? body : body[..comma]).Trim(), rest = comma < 0 ? "" : body[(comma + 1)..];
			var item = new Aup2ScriptSetting { Variable = variable, Label = label, Default = rest.Trim() };
			switch (word) {
				case "track":
					item.Kind = Aup2SettingKind.Track;
					Range(item, body.Split(','));
					break;
				case "check":
				case "checksection": {
					item.Kind = Aup2SettingKind.Check;
					string first = rest.Split(',')[0].Trim();
					item.Default = first;
					item.Boolean = first == "true" || first == "false";
					item.Number = Truth(first);
					break;
				}
				case "color":
					item.Kind = Aup2SettingKind.Color;
					item.Number = Num(rest, double.NaN);
					break;
				case "file": item.Kind = Aup2SettingKind.File; break;
				case "folder": item.Kind = Aup2SettingKind.Folder; break;
				case "font": item.Kind = Aup2SettingKind.Font; break;
				case "figure": item.Kind = Aup2SettingKind.Figure; break;
				case "text":
				case "string":
					item.Kind = Aup2SettingKind.Text;
					item.Default = rest.Replace("\\n", "\n");
					break;
				case "value": item.Kind = Aup2SettingKind.Value; break;
				case "select": {
					// "label=default,choice=value,choice=value"
					item.Kind = Aup2SettingKind.Select;
					int eq = label.IndexOf('=');
					string chosen = "";
					if (eq >= 0) { chosen = label[(eq + 1)..].Trim(); item.Label = label[..eq].Trim(); }
					foreach (string choice in rest.Split(',')) {
						int e = choice.LastIndexOf('=');
						if (e < 0) continue;
						item.Choices.Add(new(choice[..e].Trim(), choice[(e + 1)..].Trim()));
					}
					item.Default = chosen.Length > 0 ? chosen : item.Choices.Count > 0 ? item.Choices[0].Value : "0";
					item.Number = Num(item.Default);
					break;
				}
				default:
					return;   // trackgroup, hide, data: nothing a script reads as a variable
			}
			s.Settings.Add(item);
		}

		private static double Truth(string text) {
			text = text.Trim();
			return text == "true" || (text != "false" && Num(text) != 0) ? 1 : 0;
		}

		// "label,min,max,default,step"
		private static void Range(Aup2ScriptSetting t, string[] p) {
			t.Min = p.Length > 1 ? Num(p[1]) : 0;
			t.Max = p.Length > 2 ? Num(p[2]) : 100;
			t.Number = p.Length > 3 ? Num(p[3]) : 0;
			t.Step = p.Length > 4 ? Num(p[4], 1) : 1;
			t.Default = p.Length > 3 ? p[3].Trim() : "0";
		}

		// "label,local name=default": a variable with a default; anything else is code to run as it is
		private static void ReadDialog(Aup2ScriptSection s, string entry) {
			int comma = entry.IndexOf(',');
			if (comma < 0) return;
			string label = entry[..comma].Trim(), code = entry[(comma + 1)..].Trim();
			if (code.Length == 0) return;
			string hint = "";
			int slash = label.LastIndexOf('/');
			if (slash >= 0 && label.Length - slash <= 5) { hint = label[(slash + 1)..]; label = label[..slash]; }
			string body = code;
			bool local = false;
			if (body.StartsWith("local", StringComparison.Ordinal) && body.Length > 5 && char.IsWhiteSpace(body[5])) { local = true; body = body[5..].TrimStart(); }
			int eq = body.IndexOf('=');
			string name = eq < 0 ? "" : body[..eq].Trim();
			if (eq < 0 || name.Length == 0 || (eq + 1 < body.Length && body[eq + 1] == '=') || !IsName(name)) { s.Preamble.Add(code); return; }
			string value = body[(eq + 1)..].Trim();
			s.Settings.Add(new Aup2ScriptSetting {
				Kind = Aup2SettingKind.Dialog, Legacy = true, Variable = name, Label = label, Hint = hint, Local = local, Default = value, Number = Num(value),
			});
		}

		private static bool IsName(string text) {
			foreach (char ch in text)
				if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch >= 0x80)) return false;
			return text.Length > 0 && !char.IsDigit(text[0]);
		}

		// splits at `sep` where it stands outside quotes and brackets
		internal static List<string> SplitOutside(string text, char sep) {
			var parts = new List<string>();
			int depth = 0, from = 0;
			char quote = '\0';
			for (int i = 0; i < text.Length; i++) {
				char ch = text[i];
				if (quote != '\0') {
					if (ch == '\\') i++;
					else if (ch == quote) quote = '\0';
				} else if (ch == '"' || ch == '\'') quote = ch;
				else if (ch == '{' || ch == '(' || ch == '[') depth++;
				else if (ch == '}' || ch == ')' || ch == ']') depth--;
				else if (ch == sep && depth <= 0) { parts.Add(text[from..i]); from = i + 1; }
			}
			if (from < text.Length) parts.Add(text[from..]);
			return parts;
		}

		// a name the Lua here accepts: a letter outside ascii becomes "_u" and its code in four hex digits
		public static string Mangle(string name) {
			StringBuilder? sb = null;
			for (int i = 0; i < name.Length; i++) {
				if (name[i] < 0x80) { sb?.Append(name[i]); continue; }
				sb ??= new StringBuilder(name, 0, i, name.Length + 8);
				sb.Append("_u").Append(((int)name[i]).ToString("x4", CultureInfo.InvariantCulture));
			}
			return sb?.ToString() ?? name;
		}

		// the number of "=" of a long bracket opening at `at` ("[[", "[==["), or -1
		private static int LongOpen(string s, int at) {
			if (at >= s.Length || s[at] != '[') return -1;
			int i = at + 1;
			while (i < s.Length && s[i] == '=') i++;
			return i < s.Length && s[i] == '[' ? i - at - 1 : -1;
		}

		private static int LongEnd(string s, int at, int level) {
			string close = "]" + new string('=', level) + "]";
			int end = s.IndexOf(close, at + level + 2, StringComparison.Ordinal);
			return end < 0 ? s.Length : end + close.Length;
		}

		// The scripts are written for Lua 5.1 and LuaJIT. What the newer Lua refuses to read is rewritten:
		// names with letters outside ascii, and backslashes in front of a letter that is no escape.
		// Strings and comments keep their text; no line is added or removed.
		public static string Portable(string src) {
			bool plain = true;
			foreach (char ch in src)
				if (ch >= 0x80 || ch == '\\') { plain = false; break; }
			if (plain) return src;

			var sb = new StringBuilder(src.Length + 64);
			int i = 0, n = src.Length;
			while (i < n) {
				char ch = src[i];
				if (ch == '-' && i + 1 < n && src[i + 1] == '-') {
					int level = LongOpen(src, i + 2);
					int end = level >= 0 ? LongEnd(src, i + 2, level) : src.IndexOf('\n', i);
					if (end < 0) end = n;
					sb.Append(src, i, end - i);
					i = end;
				} else if (ch == '[' && LongOpen(src, i) >= 0) {
					int end = LongEnd(src, i, LongOpen(src, i));
					sb.Append(src, i, end - i);
					i = end;
				} else if (ch == '"' || ch == '\'') {
					sb.Append(ch);
					i++;
					while (i < n && src[i] != ch && src[i] != '\n') {
						if (src[i] == '\\' && i + 1 < n) {
							char e = src[i + 1];
							bool known = char.IsDigit(e) || "abfnrtvxzu\\\"'\n\r".IndexOf(e) >= 0;
							if (known) sb.Append('\\');
							sb.Append(e);
							i += 2;
						} else sb.Append(src[i++]);
					}
					if (i < n && src[i] == ch) { sb.Append(ch); i++; }
				} else if (ch >= 0x80) {
					sb.Append("_u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
					i++;
				} else {
					sb.Append(ch);
					i++;
				}
			}
			return sb.ToString();
		}
	}
}
