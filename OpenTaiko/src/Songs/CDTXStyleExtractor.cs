using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

using SheetRank = (int tier, int style, int side);

namespace OpenTaiko;

/// <summary>
/// CDTXStyleExtractor determines if there is a session notation, and if there is then
/// it returns a sheet of music clipped according to the specified player Side.
///
/// The process operates as follows:
/// 1. Break the string up into top-level sections of the following types, recording pre- or post- COURSE:
///   a) STYLE Single
///   b) STYLE Double/Couple
///   c) STYLE N
///   d) STYLE unrecognized
///   e) non-STYLE
/// 2. Within the top-level sections, break each up into sub-sections of the following types:
///   a) sheet START PN
///   b) sheet START bare
///   c) sheet START unrecognized
///   d) non-sheet
/// 3. For the current playerCount and playerSide, rank the found sheets
///    using a per-playerCount and per-playerSide set of rankings for each
///    relevant section/subsection combination.
///    Non-sheets (header sections) have the best rank.
/// 4. Determine the best-ranked sheet. Pre-COURSE: sheets have worst ranks if current difficulty is Oni, or otherwise skipped entirely.
/// 5. Select and return the best-ranked sheet and its immediate header (contains #HBSCROLL and on), mark all sheets to be skipped from upper headers
/// 6. Mark top-level STYLE-type sections which no longer contain a sheet to be skipped
/// 7. Remove all sections beyond the selected sheet
/// 8. Reassemble the upper header string
/// </summary>
public static class CDTXStyleExtractor {
	// The sections are splitted right before the header or command for the section kind,
	// so only the first line need to be considered.
	private const RegexOptions StyleGetSectionKindRegexOptions =
		RegexOptions.Compiled |
		RegexOptions.CultureInvariant |
		RegexOptions.IgnoreCase |
		RegexOptions.Singleline;

	// For splitting the sections, all lines need to be considered.
	private const RegexOptions StyleSectionSplitRegexOptions =
		StyleGetSectionKindRegexOptions |
		RegexOptions.Multiline;

	private const string StylePrefixRegexPattern = @"^STYLE\s*:";
	private const string SheetStartPrefixRegexPattern = @"^#START";

	private static int strConvertStyle(string argument) => argument.ToLower() switch {
		"single" => 1,
		"double" or "couple" => 2,
		_ => int.Parse(argument),
	};

	private static readonly string StyleSectionRegexMatchPattern =
		$@"{StylePrefixRegexPattern}\s*(\w+|\d+)";

	private static readonly string SheetStartBareRegexMatchPattern =
		$"{SheetStartPrefixRegexPattern}$";

	private static readonly string SheetStartPnRegexMatchPattern =
		$@"{SheetStartPrefixRegexPattern}\s*P(\d+)";

	private static readonly Regex SectionSplitRegex = new Regex($@"(?={StylePrefixRegexPattern})", StyleSectionSplitRegexOptions);
	private static readonly Regex SubSectionSplitRegex = new Regex($@"(?={SheetStartPrefixRegexPattern})|(?<=^#END\n)", StyleSectionSplitRegexOptions);

	private static readonly Regex StyleNSectionMatchRegex = new Regex(StyleSectionRegexMatchPattern, StyleGetSectionKindRegexOptions);
	private static readonly Regex StyleUnrecognizedSectionMatchRegex = new Regex(StylePrefixRegexPattern, StyleGetSectionKindRegexOptions);

	private static readonly Regex SheetStartBareMatchRegex = new Regex(SheetStartBareRegexMatchPattern, StyleGetSectionKindRegexOptions);
	private static readonly Regex SheetStartPnMatchRegex = new Regex(SheetStartPnRegexMatchPattern, StyleGetSectionKindRegexOptions);
	private static readonly Regex SheetStartUnrecognizedMatchRegex = new Regex(SheetStartPrefixRegexPattern, StyleGetSectionKindRegexOptions);

	private static Func<SectionKind, SubSectionKind, SheetRank> GetRanker(int playerCount, int playerSide)
		=> (playerCount <= 1) ? (sk, ssk) => (sk, ssk) switch {
			// Non-sheets have best rank
			(_, <= SubSectionKind.NonSheet) => (0, 0, 0),

			// equal or less player count, correct player side
			(SectionKind.StyleSingle, SubSectionKind.SheetStartBare) => (1, 0, 0),
			(SectionKind.StyleSingle, SubSectionKind.SheetStartP1) => (2, 0, 0),
			(SectionKind.StyleSingle, SubSectionKind.SheetStartUnrecognized) => (3, 0, 0),
			(SectionKind.NonStyle, SubSectionKind.SheetStartBare) => (4, 0, 0),
			(SectionKind.NonStyle, SubSectionKind.SheetStartP1) => (5, 0, 0),
			(SectionKind.NonStyle, SubSectionKind.SheetStartUnrecognized) => (6, 0, 0),

			// more player count (less is better), equal or earlier player side (more is better)
			(<= SectionKind.StyleUnrecognized, SubSectionKind.SheetStartBare) => (7, 0, 0),
			(<= SectionKind.StyleUnrecognized, SubSectionKind.SheetStartUnrecognized) => (8, 0, 0),
			(<= SectionKind.StyleUnrecognized, SubSectionKind.SheetStartP1) => (9, 0, 0),
			(>= SectionKind.StyleDouble, SubSectionKind.SheetStartP1) => (10, (int)sk, 0),
			(>= SectionKind.StyleDouble, SubSectionKind.SheetStartBare) => (11, (int)sk, 0),
			(>= SectionKind.StyleDouble, SubSectionKind.SheetStartUnrecognized) => (12, (int)sk, 0),

			// equal or less player count (more is better), equal or earlier player side (more is better) or unspecified or later player side (less is better)
			(SectionKind.StyleSingle, >= SubSectionKind.SheetStartP2) => (13, 1, (int)ssk),
			(SectionKind.NonStyle, >= SubSectionKind.SheetStartP2) => (14, 1, (int)ssk),
			(<= SectionKind.StyleUnrecognized, >= SubSectionKind.SheetStartP2) => (15, 1, (int)ssk),

			// more player count (less is better), unspecified or later player side (less is better)
			(>= SectionKind.StyleDouble, >= SubSectionKind.SheetStartP2) => (16, (int)sk, (int)ssk),
		}
		: (sk, ssk) => (sk, ssk) switch {
			// Non-sheets have best rank
			(_, <= SubSectionKind.NonSheet) => (0, 0, 0),

			// equal or less player count (more is better), correct player side
			(>= SectionKind.StyleSingle, >= SubSectionKind.SheetStartP1)
				when (int)sk == playerCount && (int)ssk == playerSide % (int)sk + 1 => (1, 0, 0),
			(SectionKind.StyleUnrecognized, >= SubSectionKind.SheetStartP1)
				when (int)ssk == playerSide + 1 => (2, 0, 0),
			(SectionKind.NonStyle, >= SubSectionKind.SheetStartP1)
				when (int)ssk == playerSide + 1 => (3, 0, 0),
			(>= SectionKind.StyleSingle, >= SubSectionKind.SheetStartP1)
				when (int)sk < playerCount && (int)ssk == playerSide % (int)sk + 1 => (4, -(int)sk, 0),

			// equal or less player count (more is better), unspecified player side
			(>= SectionKind.StyleSingle, SubSectionKind.SheetStartBare)
				when (int)sk == playerCount => (5, 0, 0),
			(>= SectionKind.StyleSingle, SubSectionKind.SheetStartUnrecognized)
				when (int)sk == playerCount => (6, 0, 0),
			(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartBare) => (7, 0, 0),
			(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartUnrecognized) => (8, 0, 0),
			(>= SectionKind.StyleSingle, SubSectionKind.SheetStartBare)
				when (int)sk < playerCount => (9, -(int)sk, 0),
			(>= SectionKind.StyleSingle, SubSectionKind.SheetStartUnrecognized)
				when (int)sk < playerCount => (10, -(int)sk, 0),
			(<= SectionKind.NonStyle, SubSectionKind.SheetStartBare) => (11, 0, 0),
			(<= SectionKind.NonStyle, SubSectionKind.SheetStartUnrecognized) => (12, 0, 0),

			// equal or less player count (more is better), equal or earlier player side (more is better) or unspecified or later player side (less is better)
			(>= SectionKind.StyleSingle, >= SubSectionKind.SheetStartP1)
				when (int)sk == playerCount && (int)ssk <= playerSide + 1 => (13, 0, -(int)ssk),
			(>= SectionKind.StyleSingle, _)
				when (int)sk == playerCount => (13, 1, (int)ssk),
			(SectionKind.StyleUnrecognized, >= SubSectionKind.SheetStartP1)
				when (int)ssk <= playerSide + 1 => (14, 0, -(int)ssk),
			(SectionKind.StyleUnrecognized, _) => (14, 1, (int)ssk),
			(<= SectionKind.NonStyle, >= SubSectionKind.SheetStartP1)
				when (int)ssk <= playerSide + 1 => (15, 0, -(int)ssk),
			(<= SectionKind.NonStyle, _) => (15, 1, (int)ssk),
			(>= SectionKind.StyleSingle, >= SubSectionKind.SheetStartP1)
				when (int)sk < playerCount && (int)ssk <= playerSide + 1 => (16, -(int)sk, -(int)ssk),
			(>= SectionKind.StyleSingle, _)
				when (int)sk < playerCount => (17, -(int)sk, (int)ssk),

			// more player count (less is better), equal or earlier player side (more is better) or unspecified or later player side (less is better)
			(>= SectionKind.StyleSingle, >= SubSectionKind.SheetStartP1)
				when (int)ssk <= playerSide + 1 => (18, (int)sk, -(int)ssk),
			(>= SectionKind.StyleSingle, _) => (19, (int)sk, (int)ssk),
		};

	public static (string upperHeaders, string sheet) tSessionChart(
		string strTJAGlobal, string strTJACourse, Difficulty difficulty, int playerCount, int playerSide, string strFileNameAbsolutePath
		) {
		void TraceError(string subMessage) {
			Trace.TraceError(FormatTraceMessage(subMessage));
		}

		string FormatTraceMessage(string subMessage) {
			return $"{nameof(CDTXStyleExtractor)} {subMessage} (playerCount={playerCount}, playerSide={playerSide}, {strFileNameAbsolutePath})";
		}

		//入力された譜面がnullでないかチェック。
		if (string.IsNullOrEmpty(strTJAGlobal) && string.IsNullOrEmpty(strTJACourse)) {
			TraceError("is returning its input value early due to null or empty strTJA.");
			return ("", "");
		}
		strTJAGlobal ??= "";
		strTJACourse ??= "";

		var sections = GetSections(strTJAGlobal, strTJACourse);
		SubdivideSectionsIntoSubSections(sections);
		RankSheets(playerCount, playerSide, sections);

		int bestPostCourseRank;
		SheetRank bestRank;
		try {
			(bestPostCourseRank, bestRank) = GetBestRank(difficulty, sections);
		} catch (Exception) {
			TraceError("is returning its input value early due to an inability to determine the best rank. This can occur if a course contains no #START.");
			return ("", (strTJAGlobal ?? "") + (strTJACourse ?? ""));
		}

		var (idxSection, idxSubSection, sheet) = SelectBestRankedSheet(sections, bestPostCourseRank, bestRank);
		MarkSkippedRecognizedStyleSectionsWithoutSheets(sections);
		RemoveStyleSectionSubSectionsBeyondTheSelectedSheet(sections, idxSection, idxSubSection);
		return (ReassembleUpperHeaders(sections), sheet);
	}

	// 1. Break the string up into top-level sections of the following types, recording pre- or post- COURSE:
	//   a) STYLE Single
	//   b) STYLE Double/Couple
	//   c) STYLE N
	//   d) STYLE unrecognized
	//   e) non-STYLE
	private static List<Section> GetSections(string strTJAGlobal, string strTJACourse) {
		return SectionSplitRegex.Split(strTJAGlobal)
			.Select(style => new Section(false, GetSectionKind(style), style))
			.Concat(
				SectionSplitRegex.Split(strTJACourse)
				.Select(style => new Section(true, GetSectionKind(style), style)))
			.ToList();
	}

	private static SectionKind GetSectionKind(string section) {
		var match = StyleNSectionMatchRegex.Match(section);
		if (match.Success) {
			try {
				return (SectionKind)strConvertStyle(match.Groups[1].Value);
			} catch (FormatException) {
				return SectionKind.StyleUnrecognized;
			}
		}

		if (StyleUnrecognizedSectionMatchRegex.IsMatch(section)) {
			return SectionKind.StyleUnrecognized;
		}

		return SectionKind.NonStyle;
	}

	private enum SectionKind {
		NonStyle = -1,
		StyleUnrecognized = 0,
		StyleSingle = 1,
		StyleDouble = 2,
		// StyleN = N, ...
	}

	private sealed class Section {
		public static int GetPostCourseRank(bool isPostCourse) => isPostCourse ? 0 : 1;

		public readonly bool IsPostCourse;
		public int PostCourseRank => GetPostCourseRank(IsPostCourse);
		public readonly SectionKind SectionKind;
		public readonly string OriginalRawValue;

		public List<SubSection> SubSections = [];
		public bool Skipped;

		public Section(bool isPostCourse, SectionKind sectionKind, string originalRawValue) {
			IsPostCourse = isPostCourse;
			SectionKind = sectionKind;
			OriginalRawValue = originalRawValue;
		}
	}

	// 2. Within the top-level sections, break each up into sub-sections of the following types:
	//   a) sheet START PN
	//   b) sheet START bare
	//   c) sheet START unrecognized
	//   d) non-sheet
	private static void SubdivideSectionsIntoSubSections(IEnumerable<Section> sections) {
		foreach (var section in sections) {
			section.SubSections = SubSectionSplitRegex
				.Split(section.OriginalRawValue)
				.Select(o => new SubSection(GetSubsectionKind(o), o))
				.ToList();
		}
	}

	private static SubSectionKind GetSubsectionKind(string subsection) {
		var match = SheetStartPnMatchRegex.Match(subsection);
		if (match.Success) {
			try {
				return (SubSectionKind)int.Parse(match.Groups[1].Value);
			} catch (FormatException) {
				return SubSectionKind.SheetStartUnrecognized;
			}
		}

		if (SheetStartBareMatchRegex.IsMatch(subsection)) {
			return SubSectionKind.SheetStartBare;
		}

		if (SheetStartUnrecognizedMatchRegex.IsMatch(subsection)) {
			return SubSectionKind.SheetStartUnrecognized;
		}

		return SubSectionKind.NonSheet;
	}

	private enum SubSectionKind {
		NonSheet = -2,
		SheetStartBare = -1,
		SheetStartUnrecognized = 0,
		SheetStartP1 = 1,
		SheetStartP2 = 2,
		// SheetStartPN = N, ...
	}

	private sealed class SubSection {
		public readonly SubSectionKind SubSectionKind;
		public readonly string OriginalRawValue;

		public SheetRank Rank;
		public bool Skipped;

		public SubSection(SubSectionKind subSectionKind, string originalRawValue) {
			SubSectionKind = subSectionKind;
			OriginalRawValue = originalRawValue;
		}
	}

	// 3. For the current playerCount and playerSide, rank the found sheets
	//    using a per-playerCount and per-playerSide set of rankings for each
	//    relevant section/subsection combination.
	//    Non-sheets (header sections) have the best rank.
	private static void RankSheets(int playerCount, int playerSide, IList<Section> sections) {
		var ranker = GetRanker(playerCount, playerSide);
		foreach (var section in sections) {
			foreach (var subSection in section.SubSections) {
				if (subSection.SubSectionKind != SubSectionKind.NonSheet)
					subSection.Rank = ranker(section.SectionKind, subSection.SubSectionKind);
			}
		}
	}

	// 4. Determine the best-ranked sheet. Pre-COURSE: sheets have worst ranks if current difficulty is Oni, or otherwise skipped entirely.
	private static (int postCourseRank, SheetRank rank) GetBestRank(Difficulty difficulty, IList<Section> sections) {
		return sections
			.SelectMany(s => s.SubSections.Select(ss => (post: s.IsPostCourse, ss)))
			.Where(pss => (pss.post || (difficulty == Difficulty.Oni)) && pss.ss.SubSectionKind != SubSectionKind.NonSheet)
			.Min(pss => (Section.GetPostCourseRank(pss.post), pss.ss.Rank));
	}

	// 5. Select and return the best-ranked sheet and its immediate header (contains #HBSCROLL and on), mark all sheets to be skipped from upper headers
	private static (int idxSection, int idxSubSection, string section) SelectBestRankedSheet(IList<Section> sections, int bestPostCourseRank, SheetRank bestRank) {
		// We can safely remove based on > bestRank because the subsection types
		// which are never removed always have a Rank value of 0.

		foreach (var section in sections) {
			foreach (var o in section.SubSections.Where(o => o.SubSectionKind != SubSectionKind.NonSheet))
				o.Skipped = true;
		}

		// If there was a tie for the best sheet,
		// take the first and remove the rest.
		var bestRankedSheets = sections
			.SelectMany((s, i) => s.SubSections.Select((ss, j) => (s, i, ss, j)))
			.Where(sissj => sissj.s.PostCourseRank == bestPostCourseRank && sissj.ss.Rank == bestRank);

		var (s, i, ss, j) = bestRankedSheets.First();
		string sheet = ss.OriginalRawValue;
		if (j > 0 && s.SubSections[j - 1].SubSectionKind == SubSectionKind.NonSheet) // has immediate header
			sheet = s.SubSections[j - 1].OriginalRawValue + sheet;
		return (i, j, sheet);
	}

	// 6. Mark top-level STYLE-type sections which no longer contain a sheet to be skipped
	private static void MarkSkippedRecognizedStyleSectionsWithoutSheets(List<Section> sections) {
		// Note that we dare not remove SectionKind.StyleUnrecognized instances without sheets.
		// The reason is because there are plenty of .tja files with weird STYLE: header values
		// and which are located very early in the file. Removing those sections would remove
		// important information, and was one of the problems with the years-old splitting code
		// which was replaced in late summer 2018 and which is now being overhauled in early fall 2018.

		foreach (var section in sections.Where(o =>
			(o.SectionKind == SectionKind.StyleSingle || o.SectionKind == SectionKind.StyleDouble) &&
			o.SubSections.Count(subSection => subSection.SubSectionKind == SubSectionKind.NonSheet) == o.SubSections.Count)
			) {
			section.Skipped = true;
		}
	}

	// 7. Remove all sections beyond the selected sheet
	private static void RemoveStyleSectionSubSectionsBeyondTheSelectedSheet(List<Section> sections, int idxSectionSelected, int idxSubSectionSelected) {
		sections.RemoveRange(idxSectionSelected + 1, sections.Count - 1 - idxSectionSelected);
		var subSections = sections[idxSectionSelected].SubSections;
		subSections.RemoveRange(idxSubSectionSelected + 1, subSections.Count - 1 - idxSubSectionSelected);
	}

	// 8. Reassemble the upper header string
	private static string ReassembleUpperHeaders(List<Section> sections) {
		var sb = new StringBuilder();

		foreach (var section in sections) {
			if (!section.Skipped) {
				foreach (var subSection in section.SubSections) {
					if (!subSection.Skipped)
						sb.Append(subSection.OriginalRawValue);
				}
			}
		}

		return sb.ToString();
	}
}
