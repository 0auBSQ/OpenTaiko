using System.Diagnostics;
using System.Globalization;

namespace OpenTaiko;

// Parses .osu files for osu! Taiko mode (Mode:1).
internal class COsu {
	public record struct OsuNote(int TimeMs, int ChannelNo, int Duration);

	public record struct OsuTimingPoint(int OffsetMs, double MsPerBeat, int Meter, bool Uninherited);

	public string Title = "";
	public string TitleUnicode = "";
	public string Artist = "";
	public string ArtistUnicode = "";
	public string Creator = "";
	public string Version = "";
	public string AudioFilename = "";
	public int PreviewTime = 0;
	public double OverallDifficulty = 5.0;
	public double SliderMultiplier = 1.4;
	public bool IsValidTaiko { get; private set; }

	public List<OsuNote> Notes { get; } = new();
	public List<OsuTimingPoint> TimingPoints { get; } = new();

	// Inherited timing points: slider velocity multiplier = -100 / beatLength
	private readonly List<(int OffsetMs, double SvMult)> _svPoints = new();
	public IReadOnlyList<(int OffsetMs, double SvMult)> SvPoints => _svPoints;

	public double GetBPMAt(int timeMs) {
		double result = 120.0;
		foreach (var tp in TimingPoints) {
			if (tp.OffsetMs <= timeMs) result = 60000.0 / tp.MsPerBeat;
			else break;
		}
		return result;
	}

	// Returns accumulated 16th-beat count at timeMs (for fBMSCROLLTime).
	public double GetFBMSCROLLAt(int timeMs) {
		if (TimingPoints.Count == 0) return timeMs * 120.0 / 15000.0;
		double fBMS = 0.0;
		int prevTime = TimingPoints[0].OffsetMs;
		double prevBPM = 60000.0 / TimingPoints[0].MsPerBeat;
		for (int i = 1; i < TimingPoints.Count; i++) {
			if (TimingPoints[i].OffsetMs > timeMs) break;
			fBMS += (TimingPoints[i].OffsetMs - prevTime) * prevBPM / 15000.0;
			prevTime = TimingPoints[i].OffsetMs;
			prevBPM = 60000.0 / TimingPoints[i].MsPerBeat;
		}
		fBMS += (timeMs - prevTime) * prevBPM / 15000.0;
		return fBMS;
	}

	public COsu(string filePath, bool metadataOnly = false) {
		try {
			Parse(filePath, metadataOnly);
		} catch (Exception ex) {
			Trace.TraceWarning($"[COsu] Failed to parse {filePath}: {ex.Message}");
		}
	}

	public static string? ReadCreator(string osuFilePath) {
		if (!File.Exists(osuFilePath)) return null;
		try {
			using var reader = new StreamReader(osuFilePath);
			string? line;
			bool inMeta = false;
			while ((line = reader.ReadLine()) != null) {
				line = line.Trim();
				if (line.StartsWith("[")) {
					if (line == "[Metadata]") { inMeta = true; continue; }
					if (inMeta) break; // past metadata
					inMeta = false;
				}
				if (inMeta && line.StartsWith("Creator:", StringComparison.OrdinalIgnoreCase))
					return line["Creator:".Length..].Trim();
			}
		} catch { /* ignore */ }
		return null;
	}

	private void Parse(string filePath, bool metadataOnly) {
		if (!File.Exists(filePath)) return;
		var lines = File.ReadAllLines(filePath);
		string section = "";

		foreach (var rawLine in lines) {
			var line = rawLine.Trim();
			if (line.Length == 0 || line.StartsWith("//")) continue;

			if (line.StartsWith("[") && line.EndsWith("]")) {
				section = line;
				if (metadataOnly && section == "[HitObjects]") break; // parse timing points; skip only notes
				continue;
			}

			switch (section) {
				case "[General]":
					ParseKV(line, "Mode", v => {
						// Mode 1 = Taiko, Mode 0 = osu! standard (autoconvert to Taiko)
						if (int.TryParse(v, out int mode)) IsValidTaiko = mode == 0 || mode == 1;
					});
					ParseKV(line, "AudioFilename", v => AudioFilename = v);
					ParseKV(line, "PreviewTime", v => { if (int.TryParse(v, out int pt)) PreviewTime = pt; });
					break;
				case "[Metadata]":
					ParseKV(line, "TitleUnicode", v => TitleUnicode = v);
					ParseKV(line, "Title", v => { if (!line.StartsWith("TitleUnicode:")) Title = v; });
					ParseKV(line, "ArtistUnicode", v => ArtistUnicode = v);
					ParseKV(line, "Artist", v => { if (!line.StartsWith("ArtistUnicode:")) Artist = v; });
					ParseKV(line, "Creator", v => Creator = v);
					ParseKV(line, "Version", v => Version = v);
					break;
				case "[Difficulty]":
					ParseKV(line, "OverallDifficulty", v => {
						if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double od)) OverallDifficulty = od;
					});
					ParseKV(line, "SliderMultiplier", v => {
						if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double sm)) SliderMultiplier = sm;
					});
					break;
				case "[TimingPoints]":
					ParseTimingPoint(line);
					break;
				case "[HitObjects]":
					if (IsValidTaiko && !metadataOnly) ParseHitObject(line);
					break;
			}
		}

		if (!metadataOnly)
			Notes.Sort((a, b) => a.TimeMs.CompareTo(b.TimeMs));
	}

	private void ParseTimingPoint(string line) {
		// offset,beatLength,meter,sampleSet,sampleIndex,volume,uninherited,effects
		var p = line.Split(',');
		if (p.Length < 2) return;
		if (!int.TryParse(p[0].Trim(), out int offset)) return;
		if (!double.TryParse(p[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double beatLen)) return;
		bool uninherited = p.Length < 7 || p[6].Trim() == "1";
		if (uninherited) {
			if (beatLen <= 0) return;
			int meter = (p.Length >= 3 && int.TryParse(p[2].Trim(), out int m) && m > 0) ? m : 4;
			TimingPoints.Add(new OsuTimingPoint(offset, beatLen, meter, true));
		} else {
			// Inherited: beatLen is negative; svMult = -100 / beatLen
			if (beatLen < 0)
				_svPoints.Add((offset, -100.0 / beatLen));
		}
	}

	public double GetSvMult(int time) {
		double result = 1.0;
		foreach (var sv in _svPoints) {
			if (sv.OffsetMs <= time) result = sv.SvMult;
			else break;
		}
		return result;
	}

	private void ParseHitObject(string line) {
		// x,y,time,type,hitSound[,objectParams][,hitSample]
		var p = line.Split(',');
		if (p.Length < 5) return;
		if (!int.TryParse(p[2].Trim(), out int time)) return;
		if (!int.TryParse(p[3].Trim(), out int type)) return;
		if (!int.TryParse(p[4].Trim(), out int hitSound)) return;

		bool isCircle = (type & 1) != 0;
		bool isSlider = (type & 2) != 0;
		bool isSpinner = (type & 8) != 0;
		bool isKat = (hitSound & 2) != 0 || (hitSound & 8) != 0;
		bool isBig = (hitSound & 4) != 0;

		if (isCircle) {
			int ch = isKat ? (isBig ? 0x14 : 0x12) : (isBig ? 0x13 : 0x11);
			Notes.Add(new OsuNote(time, ch, 0));
		} else if (isSlider && p.Length >= 8) {
			double msPerBeat = GetMsPerBeat(time);
			double svMult    = GetSvMult(time);
			if (!int.TryParse(p[6].Trim(), out int slides)) slides = 1;
			if (!double.TryParse(p[7].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double length)) length = 0;
			// duration = (length / (SliderMultiplier * svMult * 100)) * msPerBeat * slides
			int dur = (int)Math.Round(slides * length / (SliderMultiplier * svMult * 100.0) * msPerBeat);
			int rollCh = isBig ? 0x16 : 0x15;
			Notes.Add(new OsuNote(time, rollCh, dur));
			Notes.Add(new OsuNote(time + Math.Max(dur, 1), 0x18, 0));
		} else if (isSpinner) {
			int endTime = time;
			if (p.Length >= 6 && int.TryParse(p[5].Trim(), out int et)) endTime = et;
			int dur = Math.Max(endTime - time, 1);
			Notes.Add(new OsuNote(time, 0x17, dur));
			Notes.Add(new OsuNote(time + dur, 0x18, 0));
		}
	}

	private double GetMsPerBeat(int time) {
		double result = 500.0; // 120 BPM default
		foreach (var tp in TimingPoints) {
			if (tp.OffsetMs <= time) result = tp.MsPerBeat;
			else break;
		}
		return result;
	}

	private static void ParseKV(string line, string key, Action<string> setter) {
		string prefix = key + ":";
		if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			setter(line[prefix.Length..].Trim());
	}
}
