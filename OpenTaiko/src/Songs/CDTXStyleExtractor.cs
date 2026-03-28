using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenTaiko;

/// <summary>
/// CDTXStyleExtractor determines if there is a session notation, and if there is then
/// it returns a sheet of music clipped according to the specified player Side.
///
/// The process operates as follows:
/// 1. Break the string up into top-level sections of the following types, recording pre- or post- COURSE:
///   a) STYLE Single
///   b) STYLE Double/Couple
///   c) STYLE unrecognized
///   d) non-STYLE
/// 2. Within the top-level sections, break each up into sub-sections of the following types:
///   a) sheet START P1
///   b) sheet START P2
///   c) sheet START bare
///   d) sheet START unrecognized
///   e) non-sheet
/// 3. For the current seqNo, rank the found sheets
///    using a per-seqNo set of rankings for each
///    relevant section/subsection combination.
///    Non-sheets (header sections) have the best rank.
/// 4. Determine the best-ranked sheet. Pre-COURSE: sheets have worst ranks if current difficulty is Oni, or otherwise skipped entirely.
/// 5. Remove sheets other than the best-ranked, keeping non-sheets
/// 6. Remove top-level STYLE-type sections which no longer contain a sheet
/// 7. From supported STYLE-type sections, remove non-sheet subsections beyond
///    the selected sheet, to reduce risk of incorrect command processing.
/// 8. Reassemble the string
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

	private static readonly string StyleSingleSectionRegexMatchPattern =
		$"{StylePrefixRegexPattern}\\s*(?:Single|1)";

	private static readonly string StyleDoubleSectionRegexMatchPattern =
		$"{StylePrefixRegexPattern}\\s*(?:Double|Couple|2)";

	private static readonly string SheetStartBareRegexMatchPattern =
		$"{SheetStartPrefixRegexPattern}$";

	private static readonly string SheetStartP1RegexMatchPattern =
		$"{SheetStartPrefixRegexPattern}\\s*P1";

	private static readonly string SheetStartP2RegexMatchPattern =
		$"{SheetStartPrefixRegexPattern}\\s*P2";

	private static readonly Regex SectionSplitRegex = new Regex($"(?={StylePrefixRegexPattern})", StyleSectionSplitRegexOptions);
	private static readonly Regex SubSectionSplitRegex = new Regex($"(?={SheetStartPrefixRegexPattern})|(?<=^#END\\n)", StyleSectionSplitRegexOptions);

	private static readonly Regex StyleSingleSectionMatchRegex = new Regex(StyleSingleSectionRegexMatchPattern, StyleGetSectionKindRegexOptions);
	private static readonly Regex StyleDoubleSectionMatchRegex = new Regex(StyleDoubleSectionRegexMatchPattern, StyleGetSectionKindRegexOptions);
	private static readonly Regex StyleUnrecognizedSectionMatchRegex = new Regex(StylePrefixRegexPattern, StyleGetSectionKindRegexOptions);

	private static readonly Regex SheetStartBareMatchRegex = new Regex(SheetStartBareRegexMatchPattern, StyleGetSectionKindRegexOptions);
	private static readonly Regex SheetStartP1MatchRegex = new Regex(SheetStartP1RegexMatchPattern, StyleGetSectionKindRegexOptions);
	private static readonly Regex SheetStartP2MatchRegex = new Regex(SheetStartP2RegexMatchPattern, StyleGetSectionKindRegexOptions);
	private static readonly Regex SheetStartUnrecognizedMatchRegex = new Regex(SheetStartPrefixRegexPattern, StyleGetSectionKindRegexOptions);

	private static readonly IDictionary<SectionKindAndSubSectionKind, int>[]
		SeqNoSheetRanksBySectionKindAndSubSectionKind =
		{
			// seqNo 0
			new Dictionary<SectionKindAndSubSectionKind, int>
			{
				[new(SectionKind.StyleSingle, SubSectionKind.SheetStartBare)] = 1,
				[new(SectionKind.StyleSingle, SubSectionKind.SheetStartP1)] = 2,
				[new(SectionKind.StyleSingle, SubSectionKind.SheetStartUnrecognized)] = 3,
				[new(SectionKind.NonStyle, SubSectionKind.SheetStartBare)] = 4,
				[new(SectionKind.NonStyle, SubSectionKind.SheetStartP1)] = 5,
				[new(SectionKind.NonStyle, SubSectionKind.SheetStartUnrecognized)] = 6,

				[new(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartBare)] = 7,
				[new(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartUnrecognized)] = 8,
				[new(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartP1)] = 9,
				[new(SectionKind.StyleDouble, SubSectionKind.SheetStartP1)] = 10,
				[new(SectionKind.StyleDouble, SubSectionKind.SheetStartBare)] = 11,
				[new(SectionKind.StyleDouble, SubSectionKind.SheetStartUnrecognized)] = 12,

				[new(SectionKind.StyleSingle, SubSectionKind.SheetStartP2)] = 13,
				[new(SectionKind.NonStyle, SubSectionKind.SheetStartP2)] = 14,
				[new(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartP2)] = 15,
				[new(SectionKind.StyleDouble, SubSectionKind.SheetStartP2)] = 16,
			},
			// seqNo 1
			new Dictionary<SectionKindAndSubSectionKind, int>
			{
				[new(SectionKind.StyleDouble, SubSectionKind.SheetStartP1)] = 1,
				[new(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartP1)] = 2,
				[new(SectionKind.NonStyle, SubSectionKind.SheetStartP1)] = 3,
				[new(SectionKind.StyleSingle, SubSectionKind.SheetStartP1)] = 4,

				[new(SectionKind.StyleDouble, SubSectionKind.SheetStartBare)] = 5,
				[new(SectionKind.StyleDouble, SubSectionKind.SheetStartUnrecognized)] = 6,
				[new(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartBare)] = 7,
				[new(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartUnrecognized)] = 8,
				[new(SectionKind.StyleSingle, SubSectionKind.SheetStartBare)] = 9,
				[new(SectionKind.StyleSingle, SubSectionKind.SheetStartUnrecognized)] = 10,
				[new(SectionKind.NonStyle, SubSectionKind.SheetStartBare)] = 11,
				[new(SectionKind.NonStyle, SubSectionKind.SheetStartUnrecognized)] = 12,

				[new(SectionKind.StyleDouble, SubSectionKind.SheetStartP2)] = 13,
				[new(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartP2)] = 14,
				[new(SectionKind.NonStyle, SubSectionKind.SheetStartP2)] = 15,
				[new(SectionKind.StyleSingle, SubSectionKind.SheetStartP2)] = 16,
			},
			// seqNo 2
			new Dictionary<SectionKindAndSubSectionKind, int>
			{
				[new(SectionKind.StyleDouble, SubSectionKind.SheetStartP2)] = 1,
				[new(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartP2)] = 2,
				[new(SectionKind.NonStyle, SubSectionKind.SheetStartP2)] = 3,
				[new(SectionKind.StyleSingle, SubSectionKind.SheetStartP2)] = 4,

				[new(SectionKind.StyleDouble, SubSectionKind.SheetStartUnrecognized)] = 5,
				[new(SectionKind.StyleDouble, SubSectionKind.SheetStartBare)] = 6,
				[new(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartUnrecognized)] = 7,
				[new(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartBare)] = 8,
				[new(SectionKind.StyleSingle, SubSectionKind.SheetStartUnrecognized)] = 9,
				[new(SectionKind.StyleSingle, SubSectionKind.SheetStartBare)] = 10,
				[new(SectionKind.NonStyle, SubSectionKind.SheetStartUnrecognized)] = 11,
				[new(SectionKind.NonStyle, SubSectionKind.SheetStartBare)] = 12,

				[new(SectionKind.StyleDouble, SubSectionKind.SheetStartP1)] = 13,
				[new(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartP1)] = 14,
				[new(SectionKind.NonStyle, SubSectionKind.SheetStartP1)] = 15,
				[new(SectionKind.StyleSingle, SubSectionKind.SheetStartP1)] = 16,
			},
		};

	public static string tセッション譜面がある(string strTJAGlobal, string strTJACourse, Difficulty difficulty, int seqNo, string strファイル名の絶対パス) {
		void TraceError(string subMessage) {
			Trace.TraceError(FormatTraceMessage(subMessage));
		}

		string FormatTraceMessage(string subMessage) {
			return $"{nameof(CDTXStyleExtractor)} {subMessage} (seqNo={seqNo}, {strファイル名の絶対パス})";
		}

		//入力された譜面がnullでないかチェック。
		if (string.IsNullOrEmpty(strTJAGlobal) && string.IsNullOrEmpty(strTJACourse)) {
			TraceError("is returning its input value early due to null or empty strTJA.");
			return "";
		}
		strTJAGlobal ??= "";
		strTJACourse ??= "";

		var sections = GetSections(strTJAGlobal, strTJACourse);
		SubdivideSectionsIntoSubSections(sections);
		RankSheets(seqNo, sections);

		int bestPostCourseRank, bestRank;
		try {
			(bestPostCourseRank, bestRank) = GetBestRank(difficulty, sections);
		} catch (Exception) {
			TraceError("is returning its input value early due to an inability to determine the best rank. This can occur if a course contains no #START.");
			return (strTJAGlobal ?? "") + (strTJACourse ?? "");
		}

		RemoveSheetsOtherThanTheBestRanked(sections, bestPostCourseRank, bestRank);
		RemoveRecognizedStyleSectionsWithoutSheets(sections);
		RemoveStyleSectionSubSectionsBeyondTheSelectedSheet(sections);
		return Reassemble(sections);
	}

	// 1. Break the string up into top-level sections of the following types, recording pre- or post- COURSE:
	//   a) STYLE Single
	//   b) STYLE Double/Couple
	//   c) STYLE unrecognized
	//   d) non-STYLE
	private static List<Section> GetSections(string strTJAGlobal, string strTJACourse) {
		return SectionSplitRegex.Split(strTJAGlobal)
			.Select(style => new Section(false, GetSectionKind(style), style))
			.Concat(
				SectionSplitRegex.Split(strTJACourse)
				.Select(style => new Section(true, GetSectionKind(style), style)))
			.ToList();
	}

	private static SectionKind GetSectionKind(string section) {
		if (StyleSingleSectionMatchRegex.IsMatch(section)) {
			return SectionKind.StyleSingle;
		}

		if (StyleDoubleSectionMatchRegex.IsMatch(section)) {
			return SectionKind.StyleDouble;
		}

		if (StyleUnrecognizedSectionMatchRegex.IsMatch(section)) {
			return SectionKind.StyleUnrecognized;
		}

		return SectionKind.NonStyle;
	}

	private enum SectionKind {
		StyleSingle,
		StyleDouble,
		StyleUnrecognized,
		NonStyle
	}

	private sealed class Section {
		public readonly bool IsPostCourse;
		public readonly SectionKind SectionKind;
		public readonly string OriginalRawValue;

		public List<SubSection> SubSections = [];

		public Section(bool isPostCourse, SectionKind sectionKind, string originalRawValue) {
			IsPostCourse = isPostCourse;
			SectionKind = sectionKind;
			OriginalRawValue = originalRawValue;
		}
	}

	// 2. Within the top-level sections, break each up into sub-sections of the following types:
	//   a) sheet START P1
	//   b) sheet START P2
	//   c) sheet START bare
	//   d) sheet START unrecognized
	//   e) non-sheet
	private static void SubdivideSectionsIntoSubSections(IEnumerable<Section> sections) {
		foreach (var section in sections) {
			section.SubSections = SubSectionSplitRegex
				.Split(section.OriginalRawValue)
				.Select(o => new SubSection(GetSubsectionKind(o), o))
				.ToList();
		}
	}

	private static SubSectionKind GetSubsectionKind(string subsection) {
		if (SheetStartBareMatchRegex.IsMatch(subsection)) {
			return SubSectionKind.SheetStartBare;
		}

		if (SheetStartP1MatchRegex.IsMatch(subsection)) {
			return SubSectionKind.SheetStartP1;
		}

		if (SheetStartP2MatchRegex.IsMatch(subsection)) {
			return SubSectionKind.SheetStartP2;
		}

		if (SheetStartUnrecognizedMatchRegex.IsMatch(subsection)) {
			return SubSectionKind.SheetStartUnrecognized;
		}

		return SubSectionKind.NonSheet;
	}

	private enum SubSectionKind {
		SheetStartP1,
		SheetStartP2,
		SheetStartBare,
		SheetStartUnrecognized,
		NonSheet
	}

	private sealed class SubSection {
		public readonly SubSectionKind SubSectionKind;
		public readonly string OriginalRawValue;

		public int Rank;

		public SubSection(SubSectionKind subSectionKind, string originalRawValue) {
			SubSectionKind = subSectionKind;
			OriginalRawValue = originalRawValue;
		}
	}

	// 3. For the current seqNo, rank the found sheets
	//    using a per-seqNo set of rankings for each
	//    relevant section/subsection combination.
	//    Non-sheets (header sections) have the best rank.
	private static void RankSheets(int seqNo, IList<Section> sections) {
		var sheetRanksBySectionKindAndSubSectionKind = SeqNoSheetRanksBySectionKindAndSubSectionKind[((seqNo - 1) % 2) + 1];
		foreach (var section in sections) {
			foreach (var subSection in section.SubSections) {
				if (subSection.SubSectionKind != SubSectionKind.NonSheet)
					subSection.Rank = sheetRanksBySectionKindAndSubSectionKind[new(section.SectionKind, subSection.SubSectionKind)];
			}
		}
	}

	private readonly record struct SectionKindAndSubSectionKind(SectionKind SectionKind, SubSectionKind SubSectionKind);

	// 4. Determine the best-ranked sheet. Pre-COURSE: sheets have worst ranks if current difficulty is Oni, or otherwise skipped entirely.
	private static (int postCourseRank, int rank) GetBestRank(Difficulty difficulty, IList<Section> sections) {
		return sections
			.SelectMany(s => s.SubSections.Select(ss => (post: s.IsPostCourse, ss)))
			.Where(pss => (pss.post || (difficulty == Difficulty.Oni)) && pss.ss.SubSectionKind != SubSectionKind.NonSheet)
			.Min(pss => (pss.post ? 0 : 1, pss.ss.Rank));
	}

	// 5. Remove sheets other than the best-ranked, keeping non-sheets
	private static void RemoveSheetsOtherThanTheBestRanked(IList<Section> sections, int bestPostCourseRank, int bestRank) {
		// We can safely remove based on > bestRank because the subsection types
		// which are never removed always have a Rank value of 0.

		foreach (var section in sections) {
			var postCourseRank = section.IsPostCourse ? 0 : 1;
			section.SubSections.RemoveAll(o => (o.Rank != 0) && (postCourseRank, o.Rank).CompareTo((bestPostCourseRank, bestRank)) > 0);
		}

		// If there was a tie for the best sheet,
		// take the first and remove the rest.
		var extraBestRankedSheets = sections
			.SelectMany(s => s.SubSections.Select(ss => (s, ss)))
			.Where(sSs => sSs.ss.Rank == bestRank)
			.Skip(1);

		foreach (var (s, ss) in extraBestRankedSheets) {
			s.SubSections.Remove(ss);
		}
	}

	// 6. Remove top-level STYLE-type sections which no longer contain a sheet
	private static void RemoveRecognizedStyleSectionsWithoutSheets(List<Section> sections) {
		// Note that we dare not remove SectionKind.StyleUnrecognized instances without sheets.
		// The reason is because there are plenty of .tja files with weird STYLE: header values
		// and which are located very early in the file. Removing those sections would remove
		// important information, and was one of the problems with the years-old splitting code
		// which was replaced in late summer 2018 and which is now being overhauled in early fall 2018.

		sections.RemoveAll(o =>
			(o.SectionKind == SectionKind.StyleSingle || o.SectionKind == SectionKind.StyleDouble) &&
			o.SubSections.Count(subSection => subSection.SubSectionKind == SubSectionKind.NonSheet) == o.SubSections.Count);
	}

	// 7. From supported STYLE-type sections, remove non-sheet subsections beyond
	//    the selected sheet, to reduce risk of incorrect command processing.
	private static void RemoveStyleSectionSubSectionsBeyondTheSelectedSheet(List<Section> sections) {
		foreach (var section in sections) {
			if (section.SectionKind == SectionKind.StyleSingle || section.SectionKind == SectionKind.StyleDouble) {
				var subSections = section.SubSections;

				var lastIndex = subSections.FindIndex(o => o.SubSectionKind != SubSectionKind.NonSheet);
				var removalIndex = lastIndex + 1;

				if (lastIndex != -1 && removalIndex < subSections.Count) {
					subSections.RemoveRange(removalIndex, subSections.Count - removalIndex);
				}
			}
		}
	}

	// 8. Reassemble the string
	private static string Reassemble(List<Section> sections) {
		var sb = new StringBuilder();

		foreach (var section in sections) {
			foreach (var subSection in section.SubSections) {
				sb.Append(subSection.OriginalRawValue);
			}
		}

		return sb.ToString();
	}
}
