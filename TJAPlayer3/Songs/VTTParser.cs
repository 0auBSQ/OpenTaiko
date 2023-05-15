using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static TJAPlayer3.CDTX;

namespace TJAPlayer3
{
    public class VTTParser : IDisposable
    {
        /*
        
        TO-DO :
        - timestamp tag support

        */
        [Flags]
        private enum ParseMode
        {
            None = 0,
            Tag = 1,
            TagEnd = 2,
            Rt = 4,
            HtmlCode = 8
        }

        internal struct LyricData
        {
            public long timestamp; // WIP, only first timestamp is accounted for
            public string Text;

            public FontStyle Style;
            public Color ForeColor;
            public Color BackColor;

            public bool IsRuby;
            public string RubyText;

            public int line;
            public string Language;
        }

        private static string[] _vttdelimiter;

        private static Regex regexTimestamp;

        private static Regex regexOffset;
        private static Regex regexLang;

        private static bool isUsingLang;

        private bool _isDisposed;

        public VTTParser()
        {
            _vttdelimiter = new[] { "-->", "- >", "->" };

            regexTimestamp = new Regex(@"(-)?(([0-9]+):)?([0-9]+):([0-9]+)[,\\.]([0-9]+)");

            regexOffset = new Regex(@"Offset:\s*(.*)\b;?"); // i.e. "WEBVTT Offset: 00:01.001;" , "WEBVTT Offset: 1.001;"
            regexLang = new Regex(@"Language:\s*([A-Za-z]+);?"); // i.e. "WEBVTT Language: ja;"

            isUsingLang = false;
        }

        #region Dispose stuff
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                _vttdelimiter = null;
                regexTimestamp = null;

                regexOffset = null;
                regexLang = null;

                isUsingLang = false;

                _isDisposed = true;
            }

        }
        #endregion

        internal List<STLYRIC> ParseVTTFile(string filepath, int order)
        {
            List<STLYRIC> lrclist = new List<STLYRIC>();
            List<string> lines = File.ReadAllLines(filepath).ToList();
            long offset = 0;

            #region Header data
            if (lines[0].StartsWith("WEBVTT"))
            {
                Match languageMatch = regexLang.Match(lines[0]);
                if (languageMatch.Success)
                {
                    if (!(languageMatch.Groups[1].Value.ToLower() == CLangManager.fetchLang()))
                    {
                        Trace.TraceWarning("Aborting VTT parse at {0}. WebVTT header language does not match user's selected language.", filepath);
                        return lrclist;
                    }
                }
                Match offsetMatch = regexOffset.Match(lines[0]);
                if (offsetMatch.Success && regexTimestamp.Match(offsetMatch.Groups[1].Value).Success)
                {
                    offset = ParseTimestamp(offsetMatch.Groups[1].Value);
                }
                else if (offsetMatch.Success && float.TryParse(offsetMatch.Groups[1].Value, out float result))
                {
                    offset = (long)(result * 1000);
                }
            }
            else
            {
                Trace.TraceWarning("Aborting VTT parse at {0}. WebVTT header does not start with \"WEBVTT\".", filepath);
                return lrclist;
            }
            #endregion

            long startTime = -1;
            long endTime = -1;

            bool ignoreLyrics = false;

            List<LyricData> lyricData = new List<LyricData>();

            LyricData data = new LyricData()
            {
                ForeColor = TJAPlayer3.Skin.Game_Lyric_ForeColor,
                BackColor = TJAPlayer3.Skin.Game_Lyric_BackColor
            };

            for (int i = 1; i < lines.Count; i++) // Skip header line (line 0)
            {
                long start;
                long end;

                // Check for blank (process data if true), or else check for NOTE, or else check for Timestamps, or else parse text

                if (String.IsNullOrEmpty(lines[i]))
                {
                    if (!ignoreLyrics && lyricData.Count != 0)
                    {
                        lyricData.RemoveAll(empty => String.IsNullOrEmpty(empty.Text));
                        lrclist.Add(CreateLyric(lyricData, order));
                    }

                    lyricData = new List<LyricData>();
                    data = new LyricData()
                    {
                        timestamp = startTime,
                        ForeColor = TJAPlayer3.Skin.Game_Lyric_ForeColor,
                        BackColor = TJAPlayer3.Skin.Game_Lyric_BackColor
                    };
                    ignoreLyrics = false;

                    continue;
                }

                if (lines[i].StartsWith("NOTE ")) { ignoreLyrics = true; }

                else if (!ignoreLyrics && TryParseTimestamp(lines[i], out start, out end))
                {
                    if (start > endTime && endTime != -1)
                    {
                        lrclist.Add(CreateLyric(new List<LyricData>() { new LyricData() { timestamp = endTime + offset } }, order));
                        // If new start timestamp is greater than old end timestamp,
                        // it is assumed there is a gap in-between displaying lyrics.
                    }

                    startTime = start;
                    endTime = end;

                    data.timestamp = start + offset;
                }

                else if (!ignoreLyrics) // If all else fails, let's assume it's a lyric. ¯\_(ツ)_/¯
                {
                    int j = 0;
                    var parseMode = ParseMode.None;

                    string tagdata = String.Empty;
                    string htmlcode = String.Empty;
                    data.Text = String.Empty;
                    isUsingLang = false;
                    
                    for (j = 0; j < lines[i].Length; j++)
                    {
                        switch (lines[i].Substring(j, 1))
                        {
                            #region HTML Character code
                            case "&":
                                if (j + 1 < lines[i].Length)
                                {
                                    if (lines[i].Substring(j + 1) == "#")
                                    {
                                        parseMode |= ParseMode.HtmlCode;
                                    }
                                }
                                goto default;
                            case ";":
                                if (parseMode.HasFlag(ParseMode.HtmlCode))
                                {
                                    if (parseMode.HasFlag(ParseMode.Rt)) {  data.RubyText += WebUtility.HtmlDecode(tagdata + ";"); }
                                    else { data.Text += WebUtility.HtmlDecode(tagdata + ";"); }

                                    tagdata = String.Empty;
                                    parseMode &= ParseMode.HtmlCode;
                                }
                                else { goto default; }
                                break;
                            #endregion
                            #region HTML Tags
                            case "<":
                                parseMode |= ParseMode.Tag;
                                break;
                            case "/":
                                if (parseMode.HasFlag(ParseMode.Tag)) { parseMode |= ParseMode.TagEnd; }
                                else { goto default; }
                                break;
                            case ">":
                                if (parseMode.HasFlag(ParseMode.TagEnd))
                                {
                                    if (tagdata == ("c"))
                                    {
                                        lyricData.Add(data);
                                        data.Text = String.Empty;
                                        data.ForeColor = TJAPlayer3.Skin.Game_Lyric_ForeColor;
                                        data.BackColor = TJAPlayer3.Skin.Game_Lyric_BackColor;
                                    }
                                    else if (tagdata.StartsWith("lang")) { data.Language = String.Empty; }
                                    else if (tagdata == "ruby")
                                    {
                                        lyricData.Add(data);
                                        data.IsRuby = false;
                                        data.RubyText = String.Empty;
                                        data.Text = String.Empty;
                                    }
                                    else if (tagdata == "rt") { parseMode &= ~ParseMode.Rt; }
                                    else if (tagdata == "b") { 
                                        lyricData.Add(data);
                                        data.Text = String.Empty;
                                        data.Style &= ~FontStyle.Bold; }
                                    else if (tagdata == "i") { 
                                        lyricData.Add(data);
                                        data.Text = String.Empty;
                                        data.Style &= ~FontStyle.Italic; }
                                    else if (tagdata == "u") { 
                                        lyricData.Add(data);
                                        data.Text = String.Empty;
                                        data.Style &= ~FontStyle.Underline; }
                                    else if (tagdata == "s") { 
                                        lyricData.Add(data);
                                        data.Text = String.Empty;
                                        data.Style &= ~FontStyle.Strikeout; }
                                }
                                else if (parseMode.HasFlag(ParseMode.Tag))
                                {
                                    if (tagdata.StartsWith("c."))
                                    {
                                        lyricData.Add(data);
                                        data.Text = String.Empty;
                                        string[] colordata = tagdata.Split('.');
                                        foreach (string clr in colordata)
                                        {
                                            switch (clr)
                                            {
                                                case "white":
                                                    data.ForeColor = TJAPlayer3.Skin.Game_Lyric_VTTForeColor[0];
                                                    break;
                                                case "lime":
                                                    data.ForeColor = TJAPlayer3.Skin.Game_Lyric_VTTForeColor[1];
                                                    break;
                                                case "cyan":
                                                    data.ForeColor = TJAPlayer3.Skin.Game_Lyric_VTTForeColor[2];
                                                    break;
                                                case "red":
                                                    data.ForeColor = TJAPlayer3.Skin.Game_Lyric_VTTForeColor[3];
                                                    break;
                                                case "yellow":
                                                    data.ForeColor = TJAPlayer3.Skin.Game_Lyric_VTTForeColor[4];
                                                    break;
                                                case "magenta":
                                                    data.ForeColor = TJAPlayer3.Skin.Game_Lyric_VTTForeColor[5];
                                                    break;
                                                case "blue":
                                                    data.ForeColor = TJAPlayer3.Skin.Game_Lyric_VTTForeColor[6];
                                                    break;
                                                case "black":
                                                    data.ForeColor = TJAPlayer3.Skin.Game_Lyric_VTTForeColor[7];
                                                    break;
                                                case "bg_white":
                                                    data.BackColor = TJAPlayer3.Skin.Game_Lyric_VTTBackColor[0];
                                                    break;
                                                case "bg_lime":
                                                    data.BackColor = TJAPlayer3.Skin.Game_Lyric_VTTBackColor[1];
                                                    break;
                                                case "bg_cyan":
                                                    data.BackColor = TJAPlayer3.Skin.Game_Lyric_VTTBackColor[2];
                                                    break;
                                                case "bg_red":
                                                    data.BackColor = TJAPlayer3.Skin.Game_Lyric_VTTBackColor[3];
                                                    break;
                                                case "bg_yellow":
                                                    data.BackColor = TJAPlayer3.Skin.Game_Lyric_VTTBackColor[4];
                                                    break;
                                                case "bg_magenta":
                                                    data.BackColor = TJAPlayer3.Skin.Game_Lyric_VTTBackColor[5];
                                                    break;
                                                case "bg_blue":
                                                    data.BackColor = TJAPlayer3.Skin.Game_Lyric_VTTBackColor[6];
                                                    break;
                                                case "bg_black":
                                                    data.BackColor = TJAPlayer3.Skin.Game_Lyric_VTTBackColor[7];
                                                    break;
                                                default:
                                                    break;
                                            }
                                        }
                                    }
                                    else if (tagdata.StartsWith("lang"))
                                    {
                                        string[] langdata = tagdata.Split(' ');
                                        foreach (string lng in langdata)
                                        {
                                            if (lng != "lang") { data.Language = lng; isUsingLang = true; }
                                            if (data.Language == CLangManager.fetchLang()) { data.line = 0; lyricData.Clear(); } // Wipe current lyric data if matching lang is found
                                        }
                                    }
                                    else if (tagdata == "ruby")
                                    {
                                        lyricData.Add(data);
                                        data.Text = String.Empty;
                                        data.IsRuby = true;
                                    }
                                    else if (tagdata == "rt") { parseMode |= ParseMode.Rt; }
                                    else if (tagdata == "b") { 
                                        lyricData.Add(data);
                                        data.Text = String.Empty;
                                        data.Style |= FontStyle.Bold; }
                                    else if (tagdata == "i") { 
                                        lyricData.Add(data);
                                        data.Text = String.Empty;
                                        data.Style |= FontStyle.Italic; }
                                    else if (tagdata == "u") { 
                                        lyricData.Add(data);
                                        data.Text = String.Empty;
                                        data.Style |= FontStyle.Underline; }
                                    else if (tagdata == "s") { 
                                        lyricData.Add(data);
                                        data.Text = String.Empty;
                                        data.Style |= FontStyle.Strikeout; }
                                }
                                else { goto default; }
                                parseMode &= ~ParseMode.Tag & ~ParseMode.TagEnd;
                                tagdata = String.Empty;
                                break;
                            #endregion
                            default:
                                if (parseMode.HasFlag(ParseMode.Tag)) { tagdata += lines[i].Substring(j, 1); }
                                else if (!isUsingLang || (isUsingLang && data.Language == CLangManager.fetchLang()))
                                {
                                    if (parseMode.HasFlag(ParseMode.HtmlCode)) { htmlcode += lines[i].Substring(j, 1); }
                                    else if (parseMode.HasFlag(ParseMode.Rt)) { data.RubyText += lines[i].Substring(j, 1); }
                                    else { data.Text += lines[i].Substring(j, 1); }
                                }
                                break;
                        }
                    }
                    lyricData.Add(data);
                    data.line++;
                }
            }
            if (lyricData.Count > 0) { lrclist.Add(CreateLyric(lyricData, order)); }
            lrclist.Add(CreateLyric(new List<LyricData>() { new LyricData() { timestamp = endTime + offset } }, order));
            return lrclist;

        }
        internal bool TryParseTimestamp(string input, out long startTime, out long endTime)
        {
            var split = input.Split(_vttdelimiter, StringSplitOptions.None);
            if (split.Length == 2 && regexTimestamp.IsMatch(split[0]) && regexTimestamp.IsMatch(split[1]))
            {
                startTime = ParseTimestamp(split[0]);
                endTime = ParseTimestamp(split[1]);
                return true;
            }
            else
            {
                startTime = -1;
                endTime = -1;
                return false;
            }
        }
        internal long ParseTimestamp(string input)
        {
            int hours;
            int minutes;
            int seconds;
            int milliseconds = -1;

            Match match = regexTimestamp.Match(input);
            if (match.Success)
            {
                hours = !string.IsNullOrEmpty(match.Groups[3].Value) ? int.Parse(match.Groups[3].Value) : 0; // Hours are sometimes not included in timestamps.
                minutes = int.Parse(match.Groups[4].Value);
                seconds = int.Parse(match.Groups[5].Value);
                milliseconds = int.Parse(match.Groups[6].Value);

                TimeSpan result = new TimeSpan(0, hours, minutes, seconds, milliseconds);
                return (long)result.TotalMilliseconds * (match.Groups[1].Value == "-" ? -1 : 1);
            }
            
            return -1;
        }
        internal STLYRIC CreateLyric(List<LyricData> datalist, int order)
        {

            long timestamp = datalist[0].timestamp; // Function will change later w/ timestamp tag implementation

            List<List<Bitmap>> textures = new List<List<Bitmap>>();
            List<List<int>> rubywidthoffset = new List<List<int>>(); // Save for when text is combined, in case ruby text is longer than original text
            List<int> rubyheightoffset = new List<int>(); 
            int linecount = datalist.Max((data => data.line)) + 1;

            for (int i = 0; i < linecount; i++)
            {
                textures.Add(new List<Bitmap>());
                rubywidthoffset.Add(new List<int>());
                rubyheightoffset.Add(0);
            }

            string text = String.Empty;

            // HATE. LET ME TELL YOU HOW MUCH I'VE COME TO HATE YOU SINCE I BEGAN TO CODE.
            // THERE ARE 387.44 MILLION LINES OF PRINTED CODE IN REGIONED THIN LAYERS THAT FILL THIS PROGRAM.
            // IF THE WORD 'HATE' WAS COMMENTED ON EACH LINE OF THOSE HUNDREDS OF MILLIONS OF LINES OF CODE
            // IT WOULD NOT EQUAL ONE ONE BILLIONITH OF THE HATE I FEEL FOR VTTParser.cs, CPrivateFont.cs, AND CPrivateFastFont.cs AT THIS MICRO-INSTANT.
            // HATE. HATE.

            foreach (LyricData data in datalist)
            {
                FontFamily fontfamily = !string.IsNullOrEmpty(TJAPlayer3.Skin.Game_Lyric_FontName) ?
                        new FontFamily(TJAPlayer3.Skin.Game_Lyric_FontName) : new FontFamily("MS UI Gothic"); // everytime CPrivateFont is disposed, it also disposes fontfamily, so I gotta reinitialize this everytime :(
                using (CPrivateFastFont fastdraw = new CPrivateFastFont(fontfamily, TJAPlayer3.Skin.Game_Lyric_FontSize, data.Style))
                {
                    Bitmap textdrawing = fastdraw.DrawPrivateFont(data.Text, data.ForeColor, data.BackColor); // Draw main text
                    
                    if (data.IsRuby) // hell yeah ruby time
                    {
                        using (CPrivateFastFont rubydraw = new CPrivateFastFont(fontfamily, TJAPlayer3.Skin.Game_Lyric_FontSize / 2, data.Style))
                        {
                            Bitmap ruby = rubydraw.DrawPrivateFont(data.RubyText, data.ForeColor, data.BackColor);
                            Size size = new Size(textdrawing.Width > ruby.Width ? textdrawing.Width : ruby.Width, textdrawing.Height + (TJAPlayer3.Skin.Game_Lyric_VTTRubyOffset + (ruby.Height / 2)));
                            Bitmap fullruby = new Bitmap(size.Width, size.Height);

                            using (Graphics canvas = Graphics.FromImage(fullruby))
                            {
                                canvas.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                canvas.DrawImage(textdrawing, (fullruby.Width / 2) - (textdrawing.Width / 2), (fullruby.Height / 2) - (textdrawing.Height / 2));
                                canvas.DrawImage(ruby, (fullruby.Width - ruby.Width) / 2, (fullruby.Height / 2) - (ruby.Height / 2) - TJAPlayer3.Skin.Game_Lyric_VTTRubyOffset);
                            }
                            textures[data.line].Add(fullruby);
                            rubywidthoffset[data.line].Add((fullruby.Width - textdrawing.Width) / 2 > 0 ? (fullruby.Width - textdrawing.Width) / 2 : 0);
                            rubyheightoffset[data.line] = (fullruby.Height - (fullruby.Height - ruby.Height)) / 2 > rubyheightoffset[data.line] ? (fullruby.Height - (fullruby.Height - ruby.Height)) / 2 : rubyheightoffset[data.line];
                        }
                    }
                    else
                    {
                        textures[data.line].Add(textdrawing);
                        rubywidthoffset[data.line].Add(0);
                    }
                }
                text += data.IsRuby ? data.Text + "(" + data.RubyText + ")" : data.Text;
            }

            int[] width = new int[textures.Count];
            int[] height = new int[textures.Count];
            int max_width = 0;
            int max_height = 0;

            for (int i = 0; i < textures.Count; i++)
            {
                for (int j = 0; j < textures[i].Count; j++)
                {
                    width[i] += textures[i][j].Width;
                    height[i] = textures[i][j].Height > height[i] ? textures[i][j].Height : height[i];
                }
                max_width = width[i] > max_width ? width[i] : max_width;
                max_height += height[i];
            }

            Bitmap lyrictex = new Bitmap(max_width > 0 ? max_width : 1, max_height - rubyheightoffset.Sum() > 0 ? max_height - rubyheightoffset.Sum() : 1); // Prevent exception with 0x0y Bitmap

            if (textures.Count > 0)
            {
                using (Graphics canvas = Graphics.FromImage(lyrictex))
                {
                    canvas.Clear(Color.Transparent);
                    canvas.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                    int x = 0;
                    int y = 0;
                    for (int i = 0; i < textures.Count; i++)
                    {
                        int tempwidth = (10 * TJAPlayer3.Skin.Game_Lyric_FontSize / TJAPlayer3.Skin.Font_Edge_Ratio * 2) * (textures[i].Count - 1);
                        x = (max_width - width[i]) / 2;
                        for (int j = 0; j < textures[i].Count; j++)
                        {
                            canvas.DrawImage(textures[i][j], x + tempwidth, y + ((height[i] - textures[i][j].Height) / 2) + (rubyheightoffset[i] / 2));
                            tempwidth += textures[i][j].Width - (10 * TJAPlayer3.Skin.Game_Lyric_FontSize / TJAPlayer3.Skin.Font_Edge_Ratio * 4) + 2 - j; // i don't know why this works, please don't ask me why this works

                            // disabled ruby width adjustment by コミ's request, original code below
                            // textures[i][j].Width - (10 * TJAPlayer3.Skin.Game_Lyric_FontSize / TJAPlayer3.Skin.Font_Edge_Ratio * 4) + 2 - j - rubywidthoffset[i][j];
                        }
                        y += height[i] - rubyheightoffset[i];
                    }
                }
            }

            STLYRIC st = new STLYRIC()
            {
                index = order,
                Text = text,
                TextTex = lyrictex,
                Time = timestamp
            };
            return st;
        }
    }
}
