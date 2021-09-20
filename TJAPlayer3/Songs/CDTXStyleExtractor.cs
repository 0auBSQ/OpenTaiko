using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TJAPlayer3
{
    /// <summary>
    /// CDTXStyleExtractor determines if there is a session notation, and if there is then
    /// it returns a sheet of music clipped according to the specified player Side.
    ///
    /// The process operates as follows:
    /// 1. Break the string up into top-level sections of the following types:
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
    /// 4. Determine the best-ranked sheet
    /// 5. Remove sheets other than the best-ranked
    /// 6. Remove top-level STYLE-type sections which no longer contain a sheet
    /// 7. From supported STYLE-type sections, remove non-sheet subsections beyond
    ///    the selected sheet, to reduce risk of incorrect command processing.
    /// 8. Reassemble the string
    /// </summary>
    public static class CDTXStyleExtractor
    {
        private const RegexOptions StyleExtractorRegexOptions =
            RegexOptions.Compiled |
            RegexOptions.CultureInvariant |
            RegexOptions.IgnoreCase |
            RegexOptions.Multiline |
            RegexOptions.Singleline;

        private const string StylePrefixRegexPattern = @"^STYLE\s*:\s*";
        private const string SheetStartPrefixRegexPattern = @"^#START";

        private static readonly string StyleSingleSectionRegexMatchPattern =
            $"{StylePrefixRegexPattern}(?:Single|1)";

        private static readonly string StyleDoubleSectionRegexMatchPattern =
            $"{StylePrefixRegexPattern}(?:Double|Couple|2)";

        private static readonly string StyleUnrecognizedSectionRegexMatchPattern =
            $"{StylePrefixRegexPattern}";

        private static readonly string SheetStartBareRegexMatchPattern =
            $"{SheetStartPrefixRegexPattern}$";
        
        private static readonly string SheetStartP1RegexMatchPattern =
            $"{SheetStartPrefixRegexPattern}\\s*P1";

        private static readonly string SheetStartP2RegexMatchPattern =
            $"{SheetStartPrefixRegexPattern}\\s*P2";
        
        private static readonly string SheetStartUnrecognizedRegexMatchPattern =
            $"{SheetStartPrefixRegexPattern}.*$";

        private static readonly Regex SectionSplitRegex = new Regex($"(?={StylePrefixRegexPattern})", StyleExtractorRegexOptions);
        private static readonly Regex SubSectionSplitRegex = new Regex($"(?={SheetStartPrefixRegexPattern})|(?<=#END\\n)", StyleExtractorRegexOptions);

        private static readonly Regex StyleSingleSectionMatchRegex = new Regex(StyleSingleSectionRegexMatchPattern, StyleExtractorRegexOptions);
        private static readonly Regex StyleDoubleSectionMatchRegex = new Regex(StyleDoubleSectionRegexMatchPattern, StyleExtractorRegexOptions);
        private static readonly Regex StyleUnrecognizedSectionMatchRegex = new Regex(StyleUnrecognizedSectionRegexMatchPattern, StyleExtractorRegexOptions);

        private static readonly Regex SheetStartPrefixMatchRegex = new Regex(SheetStartPrefixRegexPattern, StyleExtractorRegexOptions);
        private static readonly Regex SheetStartBareMatchRegex = new Regex(SheetStartBareRegexMatchPattern, StyleExtractorRegexOptions);
        private static readonly Regex SheetStartP1MatchRegex = new Regex(SheetStartP1RegexMatchPattern, StyleExtractorRegexOptions);
        private static readonly Regex SheetStartP2MatchRegex = new Regex(SheetStartP2RegexMatchPattern, StyleExtractorRegexOptions);
        private static readonly Regex SheetStartUnrecognizedMatchRegex = new Regex(SheetStartUnrecognizedRegexMatchPattern, StyleExtractorRegexOptions);

        private static readonly SectionKindAndSubSectionKind StyleSingleAndSheetStartBare =
            new SectionKindAndSubSectionKind(SectionKind.StyleSingle, SubSectionKind.SheetStartBare);

        private static readonly SectionKindAndSubSectionKind StyleSingleAndSheetStartP1 =
            new SectionKindAndSubSectionKind(SectionKind.StyleSingle, SubSectionKind.SheetStartP1);

        private static readonly SectionKindAndSubSectionKind StyleSingleAndSheetStartP2 =
            new SectionKindAndSubSectionKind(SectionKind.StyleSingle, SubSectionKind.SheetStartP2);

        private static readonly SectionKindAndSubSectionKind StyleSingleAndSheetStartUnrecognized =
            new SectionKindAndSubSectionKind(SectionKind.StyleSingle, SubSectionKind.SheetStartUnrecognized);

        private static readonly SectionKindAndSubSectionKind StyleDoubleAndSheetStartBare =
            new SectionKindAndSubSectionKind(SectionKind.StyleDouble, SubSectionKind.SheetStartBare);

        private static readonly SectionKindAndSubSectionKind StyleDoubleAndSheetStartP1 =
            new SectionKindAndSubSectionKind(SectionKind.StyleDouble, SubSectionKind.SheetStartP1);

        private static readonly SectionKindAndSubSectionKind StyleDoubleAndSheetStartP2 =
            new SectionKindAndSubSectionKind(SectionKind.StyleDouble, SubSectionKind.SheetStartP2);

        private static readonly SectionKindAndSubSectionKind StyleDoubleAndSheetStartUnrecognized =
            new SectionKindAndSubSectionKind(SectionKind.StyleDouble, SubSectionKind.SheetStartUnrecognized);

        private static readonly SectionKindAndSubSectionKind StyleUnrecognizedAndSheetStartBare =
            new SectionKindAndSubSectionKind(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartBare);

        private static readonly SectionKindAndSubSectionKind StyleUnrecognizedAndSheetStartP1 =
            new SectionKindAndSubSectionKind(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartP1);

        private static readonly SectionKindAndSubSectionKind StyleUnrecognizedAndSheetStartP2 =
            new SectionKindAndSubSectionKind(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartP2);

        private static readonly SectionKindAndSubSectionKind StyleUnrecognizedAndSheetStartUnrecognized =
            new SectionKindAndSubSectionKind(SectionKind.StyleUnrecognized, SubSectionKind.SheetStartUnrecognized);

        private static readonly SectionKindAndSubSectionKind NonStyleAndSheetStartBare =
            new SectionKindAndSubSectionKind(SectionKind.NonStyle, SubSectionKind.SheetStartBare);

        private static readonly SectionKindAndSubSectionKind NonStyleAndSheetStartP1 =
            new SectionKindAndSubSectionKind(SectionKind.NonStyle, SubSectionKind.SheetStartP1);

        private static readonly SectionKindAndSubSectionKind NonStyleAndSheetStartP2 =
            new SectionKindAndSubSectionKind(SectionKind.NonStyle, SubSectionKind.SheetStartP2);

        private static readonly SectionKindAndSubSectionKind NonStyleAndSheetStartUnrecognized =
            new SectionKindAndSubSectionKind(SectionKind.NonStyle, SubSectionKind.SheetStartUnrecognized);

        private static readonly IDictionary<SectionKindAndSubSectionKind, int>[]
            SeqNoSheetRanksBySectionKindAndSubSectionKind =
            {
                // seqNo 0
                new Dictionary<SectionKindAndSubSectionKind, int>
                {
                    [StyleSingleAndSheetStartBare] = 1,
                    [StyleSingleAndSheetStartP1] = 2,
                    [StyleSingleAndSheetStartUnrecognized] = 3,
                    [NonStyleAndSheetStartBare] = 4,
                    [NonStyleAndSheetStartP1] = 5,
                    [NonStyleAndSheetStartUnrecognized] = 6,

                    [StyleUnrecognizedAndSheetStartBare] = 7,
                    [StyleUnrecognizedAndSheetStartUnrecognized] = 8,
                    [StyleUnrecognizedAndSheetStartP1] = 9,
                    [StyleDoubleAndSheetStartP1] = 10,
                    [StyleDoubleAndSheetStartBare] = 11,
                    [StyleDoubleAndSheetStartUnrecognized] = 12,

                    [StyleSingleAndSheetStartP2] = 13,
                    [NonStyleAndSheetStartP2] = 14,
                    [StyleUnrecognizedAndSheetStartP2] = 15,
                    [StyleDoubleAndSheetStartP2] = 16,
                },
                // seqNo 1
                new Dictionary<SectionKindAndSubSectionKind, int>
                {
                    [StyleDoubleAndSheetStartP1] = 1,
                    [StyleUnrecognizedAndSheetStartP1] = 2,
                    [NonStyleAndSheetStartP1] = 3,
                    [StyleSingleAndSheetStartP1] = 4,

                    [StyleDoubleAndSheetStartBare] = 5,
                    [StyleDoubleAndSheetStartUnrecognized] = 6,
                    [StyleUnrecognizedAndSheetStartBare] = 7,
                    [StyleUnrecognizedAndSheetStartUnrecognized] = 8,
                    [StyleSingleAndSheetStartBare] = 9,
                    [StyleSingleAndSheetStartUnrecognized] = 10,
                    [NonStyleAndSheetStartBare] = 11,
                    [NonStyleAndSheetStartUnrecognized] = 12,

                    [StyleDoubleAndSheetStartP2] = 13,
                    [StyleUnrecognizedAndSheetStartP2] = 14,
                    [NonStyleAndSheetStartP2] = 15,
                    [StyleSingleAndSheetStartP2] = 16,
                },
                // seqNo 2
                new Dictionary<SectionKindAndSubSectionKind, int>
                {
                    [StyleDoubleAndSheetStartP2] = 1,
                    [StyleUnrecognizedAndSheetStartP2] = 2,
                    [NonStyleAndSheetStartP2] = 3,
                    [StyleSingleAndSheetStartP2] = 4,

                    [StyleDoubleAndSheetStartUnrecognized] = 5,
                    [StyleDoubleAndSheetStartBare] = 6,
                    [StyleUnrecognizedAndSheetStartUnrecognized] = 7,
                    [StyleUnrecognizedAndSheetStartBare] = 8,
                    [StyleSingleAndSheetStartUnrecognized] = 9,
                    [StyleSingleAndSheetStartBare] = 10,
                    [NonStyleAndSheetStartUnrecognized] = 11,
                    [NonStyleAndSheetStartBare] = 12,

                    [StyleDoubleAndSheetStartP1] = 13,
                    [StyleUnrecognizedAndSheetStartP1] = 14,
                    [NonStyleAndSheetStartP1] = 15,
                    [StyleSingleAndSheetStartP1] = 16,
                },
            };

        public static string tセッション譜面がある(string strTJA, int seqNo, string strファイル名の絶対パス)
        {
            void TraceError(string subMessage)
            {
                Trace.TraceError(FormatTraceMessage(subMessage));
            }

            string FormatTraceMessage(string subMessage)
            {
                return $"{nameof(CDTXStyleExtractor)} {subMessage} (seqNo={seqNo}, {strファイル名の絶対パス})";
            }

            //入力された譜面がnullでないかチェック。
            if (string.IsNullOrEmpty(strTJA))
            {
                TraceError("is returning its input value early due to null or empty strTJA.");
                return strTJA;
            }

            // 1. Break the string up into top-level sections of the following types:
            //   a) STYLE Single
            //   b) STYLE Double/Couple
            //   c) STYLE unrecognized
            //   d) non-STYLE
            var sections = GetSections(strTJA);

            // 2. Within the top-level sections, break each up into sub-sections of the following types:
            //   a) sheet START P1
            //   b) sheet START P2
            //   c) sheet START bare
            //   d) sheet START unrecognized
            //   e) non-sheet
            SubdivideSectionsIntoSubSections(sections);

            // 3. For the current seqNo, rank the found sheets
            //    using a per-seqNo set of rankings for each
            //    relevant section/subsection combination.
            RankSheets(seqNo, sections);

            // 4. Determine the best-ranked sheet
            int bestRank;
            try
            {
                bestRank = GetBestRank(sections);
            }
            catch (Exception)
            {
                TraceError("is returning its input value early due to an inability to determine the best rank. This can occur if a course contains no #START.");
                return strTJA;
            }

            // 5. Remove sheets other than the best-ranked
            RemoveSheetsOtherThanTheBestRanked(sections, bestRank);

            // 6. Remove top-level STYLE-type sections which no longer contain a sheet
            RemoveRecognizedStyleSectionsWithoutSheets(sections);

            // 7. From supported STYLE-type sections, remove non-sheet subsections beyond
            //    the selected sheet, to reduce risk of incorrect command processing.
            RemoveStyleSectionSubSectionsBeyondTheSelectedSheet(sections);

            // 8. Reassemble the string
            return Reassemble(sections);
        }

        // 1. Break the string up into top-level sections of the following types:
        //   a) STYLE Single
        //   b) STYLE Double/Couple
        //   c) STYLE unrecognized
        //   d) non-STYLE
        private static List<Section> GetSections(string strTJA)
        {
            return SectionSplitRegex
                .Split(strTJA)
                .Select(o => new Section(GetSectionKind(o), o))
                .ToList();
        }

        private static SectionKind GetSectionKind(string section)
        {
            if (StyleSingleSectionMatchRegex.IsMatch(section))
            {
                return SectionKind.StyleSingle;
            }

            if (StyleDoubleSectionMatchRegex.IsMatch(section))
            {
                return SectionKind.StyleDouble;
            }

            if (StyleUnrecognizedSectionMatchRegex.IsMatch(section))
            {
                return SectionKind.StyleUnrecognized;
            }

            return SectionKind.NonStyle;
        }

        private enum SectionKind
        {
            StyleSingle,
            StyleDouble,
            StyleUnrecognized,
            NonStyle
        }

        private sealed class Section
        {
            public readonly SectionKind SectionKind;
            public readonly string OriginalRawValue;

            public List<SubSection> SubSections;

            public Section(SectionKind sectionKind, string originalRawValue)
            {
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
        private static void SubdivideSectionsIntoSubSections(IEnumerable<Section> sections)
        {
            foreach (var section in sections)
            {
                section.SubSections = SubSectionSplitRegex
                    .Split(section.OriginalRawValue)
                    .Select(o => new SubSection(GetSubsectionKind(o), o))
                    .ToList();
            }
        }

        private static SubSectionKind GetSubsectionKind(string subsection)
        {
            if (SheetStartPrefixMatchRegex.IsMatch(subsection))
            {
                if (SheetStartBareMatchRegex.IsMatch(subsection))
                {
                    return SubSectionKind.SheetStartBare;
                }

                if (SheetStartP1MatchRegex.IsMatch(subsection))
                {
                    return SubSectionKind.SheetStartP1;
                }

                if (SheetStartP2MatchRegex.IsMatch(subsection))
                {
                    return SubSectionKind.SheetStartP2;
                }
            
                if (SheetStartUnrecognizedMatchRegex.IsMatch(subsection))
                {
                    return SubSectionKind.SheetStartUnrecognized;
                }
            }

            return SubSectionKind.NonSheet;
        }

        private enum SubSectionKind
        {
            SheetStartP1,
            SheetStartP2,
            SheetStartBare,
            SheetStartUnrecognized,
            NonSheet
        }

        private sealed class SubSection
        {
            public readonly SubSectionKind SubSectionKind;
            public readonly string OriginalRawValue;

            public int Rank;

            public SubSection(SubSectionKind subSectionKind, string originalRawValue)
            {
                SubSectionKind = subSectionKind;
                OriginalRawValue = originalRawValue;
            }
        }

        // 3. For the current seqNo, rank the found sheets
        //    using a per-seqNo set of rankings for each
        //    relevant section/subsection combination.
        private static void RankSheets(int seqNo, IList<Section> sections)
        {
            var sheetRanksBySectionKindAndSubSectionKind = SeqNoSheetRanksBySectionKindAndSubSectionKind[seqNo];

            foreach (var section in sections)
            {
                var sectionKind = section.SectionKind;

                foreach (var subSection in section.SubSections)
                {
                    var subSectionKind = subSection.SubSectionKind;

                    if (subSectionKind == SubSectionKind.NonSheet)
                    {
                        continue;
                    }

                    var sectionKindAndSubSectionKind = new SectionKindAndSubSectionKind(
                        sectionKind, subSectionKind);

                    subSection.Rank = sheetRanksBySectionKindAndSubSectionKind[sectionKindAndSubSectionKind];
                }
            }
        }

        private sealed class SectionKindAndSubSectionKind : IEquatable<SectionKindAndSubSectionKind>
        {
            public readonly SectionKind SectionKind;
            public readonly SubSectionKind SubSectionKind;

            public SectionKindAndSubSectionKind(SectionKind sectionKind, SubSectionKind subSectionKind)
            {
                SectionKind = sectionKind;
                SubSectionKind = subSectionKind;
            }

            public bool Equals(SectionKindAndSubSectionKind other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return SectionKind == other.SectionKind && SubSectionKind == other.SubSectionKind;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is SectionKindAndSubSectionKind other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int) SectionKind * 397) ^ (int) SubSectionKind;
                }
            }

            public static bool operator ==(SectionKindAndSubSectionKind left, SectionKindAndSubSectionKind right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(SectionKindAndSubSectionKind left, SectionKindAndSubSectionKind right)
            {
                return !Equals(left, right);
            }
        }

        // 4. Determine the best-ranked sheet
        private static int GetBestRank(IList<Section> sections)
        {
            return sections
                .SelectMany(o => o.SubSections)
                .Where(o => o.SubSectionKind != SubSectionKind.NonSheet)
                .Select(o => o.Rank)
                .Min();
        }

        // 5. Remove sheets other than the best-ranked
        private static void RemoveSheetsOtherThanTheBestRanked(IList<Section> sections, int bestRank)
        {
            // We can safely remove based on > bestRank because the subsection types
            // which are never removed always have a Rank value of 0.

            foreach (var section in sections)
            {
                section.SubSections.RemoveAll(o => o.Rank > bestRank);
            }

            // If there was a tie for the best sheet,
            // take the first and remove the rest.
            var extraBestRankedSheets = new HashSet<SubSection>(sections
                .SelectMany(o => o.SubSections)
                .Where(o => o.Rank == bestRank)
                .Skip(1));

            foreach (var section in sections)
            {
                section.SubSections.RemoveAll(extraBestRankedSheets.Contains);
            }
        }

        // 6. Remove top-level STYLE-type sections which no longer contain a sheet
        private static void RemoveRecognizedStyleSectionsWithoutSheets(List<Section> sections)
        {
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
        private static void RemoveStyleSectionSubSectionsBeyondTheSelectedSheet(List<Section> sections)
        {
            foreach (var section in sections)
            {
                if (section.SectionKind == SectionKind.StyleSingle || section.SectionKind == SectionKind.StyleDouble)
                {
                    var subSections = section.SubSections;

                    var lastIndex = subSections.FindIndex(o => o.SubSectionKind != SubSectionKind.NonSheet);
                    var removalIndex = lastIndex + 1;

                    if (lastIndex != -1 && removalIndex < subSections.Count)
                    {
                        subSections.RemoveRange(removalIndex, subSections.Count - removalIndex);
                    }
                }
            }
        }

        // 8. Reassemble the string
        private static string Reassemble(List<Section> sections)
        {
            var sb = new StringBuilder();

            foreach (var section in sections)
            {
                foreach (var subSection in section.SubSections)
                {
                    sb.Append(subSection.OriginalRawValue);
                }
            }

            return sb.ToString();
        }
    }
}