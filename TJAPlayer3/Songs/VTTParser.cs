using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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
        - Tag support (lang, timestamp, color(?))
        - Adjust bitmap position/anchor when lyric contains newlines (currently snapped to top-center, gets trimmed going down) (also how on earth do i do this lmao)
        - NOTE support (for comments within file)

        */

        private static string[] _vttdelimiter;

        private static Regex regexTimestamp;
        private static Regex regexTag;

        private static Regex regexOffset;
        private static Regex regexLang;

        private bool _isDisposed;

        public VTTParser()
        {
            _vttdelimiter = new[] { "-->", "- >", "->" };

            regexTimestamp = new Regex(@"(-)?(([0-9]+):)?([0-9]+):([0-9]+)[,\\.]([0-9]+)");
            regexTag = new Regex(@"<[^>]*>"); // For now, all tags will be ignored & removed.

            regexOffset = new Regex(@"Offset:\s*((-)?(([0-9]+):)?([0-9]+):([0-9]+)[,\\.]([0-9]+));?"); // i.e. "WebVTT Offset: 00:01.001;"
            regexLang = new Regex(@"Language:\s*([A-Za-z]+);?"); // i.e. "WebVTT Language: ja;"
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
                regexTag = null;

                regexOffset = null;
                regexLang = null;

                _isDisposed = true;
            }

        }
        #endregion

        internal List<STLYRIC> ParseVTTFile(string filepath, int order, CPrivateFastFont drawer)
        {
            List<STLYRIC> lrclist = new List<STLYRIC>() { };
            List<string> lines = File.ReadAllLines(filepath).ToList();
            long offset = 0;

            // Header stuff
            if (lines[0].StartsWith("WEBVTT"))
            {
                Match languageMatch = regexLang.Match(lines[0]);
                if (languageMatch.Success)
                {
                    if (!(languageMatch.Groups[1].Value.ToLower() == CLangManager.fetchLang()))
                    {
                        Debug.WriteLine("WebVTT header language does not match user's selected language. Aborting VTT parse.");
                        return lrclist;
                    }
                }
                Match offsetMatch = regexOffset.Match(lines[0]);
                if (offsetMatch.Success)
                {
                    offset = ParseTimestamp(offsetMatch.Groups[1].Value);
                }
            }
            else
            {
                Debug.WriteLine("WebVTT lyric file at {0} does not start with \"WEBVTT\". Aborting VTT parse.");
                return lrclist;
            }

            List<(string, long)> lyrics = new List<(string, long)>() { };

            long startTime = -1;
            long endTime = -1;
            string line = String.Empty;

            for (int i = 1; i < lines.Count; i++) // Skip header line (line 0)
            {
                long start;
                long end;

                if (TryParseTimestamp(lines[i], out start, out end))
                {
                    if (start > endTime && endTime != -1)
                    {
                        lyrics.Add((string.Empty, endTime + offset));
                        // If new start timestamp is greater than old end timestamp,
                        // it is assumed there is a gap in-between displaying lyrics.
                    }

                    if (startTime != -1)
                    {
                        lyrics.Add((line, startTime + offset));
                        // When new timestamp is found,
                        // create lyrics with existing timestamp.
                    }

                    startTime = start;
                    endTime = end;

                    line = string.Empty;
                }
                else // If timestamp parse failed, let's assume it's a lyric. ¯\_(ツ)_/¯
                {
                    if (line != "") line += Environment.NewLine;
                    line += String.Join(String.Empty, regexTag.Split(lines[i])); // Remove tags by splitting, then join back into single string
                }
            }
            lyrics.Add((line, startTime + offset));
            lyrics.Add((String.Empty, endTime + offset));

            foreach ((string, long) lyric in lyrics)
            {
                lrclist.Add(CreateLyric(lyric, drawer, order));
            }

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
        internal STLYRIC CreateLyric(string lyric, long time, CPrivateFastFont draw, int order)
        {
            return CreateLyric((lyric, time), draw, order);
        }
        internal STLYRIC CreateLyric((string, long) lyric, CPrivateFastFont draw, int order)
        {
            string[] split = lyric.Item1.Split(new string[]{"\\r\\n", "\\n", "\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries);

            List<Bitmap> textures = new List<Bitmap>();
            int width = 1;
            int height = 1;

            for (int i = 0; i < split.Length; i++)
            {
                textures.Add(draw.DrawPrivateFont(split[i], TJAPlayer3.Skin.Game_Lyric_ForeColor, TJAPlayer3.Skin.Game_Lyric_BackColor));
                width = width < textures[i].Width ? textures[i].Width : width;
                height += textures[i].Height;
            }

            Bitmap texture = new Bitmap(width, height);

            if (textures.Count != 0)
            {
                using (Graphics canvas = Graphics.FromImage(texture))
                {
                    canvas.Clear(Color.Transparent);

                    Point pos = new Point(0, 0);
                    foreach (Bitmap bitmap in textures)
                    {
                        pos.X = (width - bitmap.Width) / 2;
                        canvas.DrawImage(bitmap, pos);
                        pos.Y += bitmap.Height;
                    }
                }
            }

            STLYRIC stlrc = new STLYRIC()
            {

                Text = lyric.Item1,
                TextTex = texture,
                Time = lyric.Item2,
                index = order
            };

            return stlrc;
        }
    }
}
