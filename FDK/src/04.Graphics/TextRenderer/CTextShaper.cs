using System.Collections.Concurrent;
using System.Globalization;
using HarfBuzzSharp;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using HBBuffer = HarfBuzzSharp.Buffer;
using HBFont = HarfBuzzSharp.Font;

namespace FDK;

// Text shaping with HarfBuzz: the glyphs of a string and where they go, with the font's kerning, its marks and its
// complex-script rules. A string is split into segments of one typeface, one script and one direction; the segments
// come back in the order they are drawn, left to right.
internal static class CTextShaper {
	// the skin's choices (SkinConfig.ini), set through CFontRenderer.SetShapingFeatures
	private static volatile bool _kerning = true;
	private static volatile bool _palt = false;
	private static int _generation;

	internal static bool Kerning => _kerning;
	internal static bool Palt => _palt;
	// changes whenever the features change, so caches of measured or drawn text know to start over
	internal static int Generation => Volatile.Read(ref _generation);

	internal static void SetFeatures(bool kerning, bool palt) {
		if (kerning == _kerning && palt == _palt)
			return;
		_kerning = kerning;
		_palt = palt;
		Interlocked.Increment(ref _generation);
	}

	internal readonly record struct Options(bool Kerning, bool Palt) {
		// the skin's features for horizontal text
		public static Options Current => new(_kerning, _palt);
		// one character per cell (vertical text): no kerning and no proportional widths
		public static readonly Options PerCharacter = new(false, false);
	}

	// one shaped segment: glyph ids, their positions from the segment's origin (y down) and the pen advance
	internal sealed class Segment {
		public SKTypeface Typeface = null!;
		public ushort[] Glyphs = [];
		public SKPoint[] Positions = [];
		public float Advance;
	}

	// a part of the text that one typeface draws (from the renderer's fallback split)
	internal readonly record struct FaceRun(int Start, int Length, SKTypeface Typeface);

	// ── HarfBuzz fonts ───────────────────────────────────────────────────────────────────────────
	// One per typeface, at the font's own units: without hinting, positions scale linearly with the text size, so one
	// font serves every size. The blob wraps the typeface's data (no copy when Skia holds it in memory).
	private sealed class HbFace {
		public Blob Blob = null!;
		public Face Face = null!;
		public HBFont Font = null!;
		public int UnitsPerEm;
	}
	private static readonly ConcurrentDictionary<SKTypeface, Lazy<HbFace?>> _faces = new();

	private static HbFace? FaceFor(SKTypeface typeface)
		=> _faces.GetOrAdd(typeface, tf => new Lazy<HbFace?>(() => CreateFace(tf))).Value;

	// typefaces loaded from a font file: HarfBuzz maps the file itself, where the typeface's stream would be a second
	// copy of the font in memory for a typeface Skia loaded from a stream (mobile)
	private static readonly ConcurrentDictionary<SKTypeface, string> _files = new();

	internal static void RegisterFile(SKTypeface typeface, string path) => _files[typeface] = path;

	private static HbFace? CreateFace(SKTypeface typeface) {
		try {
			Blob? blob = null;
			int index = 0;
			if (_files.TryGetValue(typeface, out string? path)) {
				blob = Blob.FromFile(path);
				if (blob.Length == 0) { blob.Dispose(); blob = null; }
			}
			if (blob == null) {
				// the blob owns the stream from here (it may wrap the stream's memory) and disposes it with itself
				var stream = typeface.OpenStream(out index);
				if (stream == null)
					return null;
				blob = stream.ToHarfBuzzBlob();
			}
			var face = new Face(blob, index);
			// the face would otherwise report a wrong size for some fonts (SkiaSharp issue 1810)
			int upem = typeface.UnitsPerEm > 0 ? typeface.UnitsPerEm : 1000;
			face.UnitsPerEm = upem;
			var font = new HBFont(face);
			font.SetScale(upem, upem);
			font.SetFunctionsOpenType();
			return new HbFace { Blob = blob, Face = face, Font = font, UnitsPerEm = upem };
		} catch (Exception e) {
			System.Diagnostics.Trace.TraceWarning($"Text shaping is unavailable for '{typeface.FamilyName}': {e.Message}");
			return null;
		}
	}

	// ── itemizing ────────────────────────────────────────────────────────────────────────────────

	private enum Kind : byte { Left, Right, Digit, Neutral, Mark }

	private static bool IsRightToLeft(Script script) => script.HorizontalDirection == Direction.RightToLeft;

	// Splits the text into segments of one typeface, one script and one embedding level (a small form of the Unicode
	// bidirectional algorithm: right-to-left words, numbers inside them and neutral characters between them), shapes
	// each, and returns them in drawing order.
	public static List<Segment> Shape(string text, IReadOnlyList<FaceRun> faceRuns, float textSize, Options options) {
		var segments = new List<Segment>();
		if (string.IsNullOrEmpty(text))
			return segments;

		int n = text.Length;
		var scripts = new Script[n];
		var kinds = new Kind[n];
		var levels = new byte[n];
		var unicode = UnicodeFunctions.Default;

		// script and direction class of every code point (stored on its first UTF-16 unit)
		bool anyRtl = false;
		for (int i = 0; i < n;) {
			int len = char.IsSurrogatePair(text, i) ? 2 : 1;
			int cp = len == 2 ? char.ConvertToUtf32(text, i) : text[i];
			Script script = unicode.GetScript((uint)cp);
			Kind kind;
			if (CharUnicodeInfo.GetUnicodeCategory(cp) == UnicodeCategory.DecimalDigitNumber)
				kind = Kind.Digit;
			else if (script == Script.Inherited)
				kind = Kind.Mark;
			else if (script == Script.Common || script == Script.Unknown || script == Script.Invalid)
				kind = Kind.Neutral;
			else
				kind = IsRightToLeft(script) ? Kind.Right : Kind.Left;
			if (kind == Kind.Right)
				anyRtl = true;
			for (int k = 0; k < len; k++) { scripts[i + k] = script; kinds[i + k] = kind; }
			i += len;
		}

		// marks take the class and script of the character they sit on
		for (int i = 1; i < n; i++) {
			if (kinds[i] == Kind.Mark) {
				kinds[i] = kinds[i - 1];
				scripts[i] = scripts[i - 1];
			}
		}

		if (anyRtl) {
			// the paragraph direction is the first strong character's
			bool rtl = false;
			for (int i = 0; i < n; i++) {
				if (kinds[i] == Kind.Left) break;
				if (kinds[i] == Kind.Right) { rtl = true; break; }
			}
			byte strongL = (byte)(rtl ? 2 : 0);
			Kind prevStrong = rtl ? Kind.Right : Kind.Left;
			for (int i = 0; i < n; i++) {
				switch (kinds[i]) {
					case Kind.Left: levels[i] = strongL; prevStrong = Kind.Left; break;
					case Kind.Right: levels[i] = 1; prevStrong = Kind.Right; break;
					// numbers read left to right, one level above right-to-left text around them
					case Kind.Digit: levels[i] = (byte)(rtl || prevStrong == Kind.Right ? 2 : 0); break;
				}
			}
			// neutrals between two characters of one direction take it, other neutrals the paragraph's
			for (int i = 0; i < n;) {
				if (kinds[i] != Kind.Neutral && kinds[i] != Kind.Mark) { i++; continue; }
				int j = i;
				while (j < n && (kinds[j] == Kind.Neutral || kinds[j] == Kind.Mark)) j++;
				bool before = i > 0 ? kinds[i - 1] != Kind.Left : rtl;    // numbers count as right-to-left here
				bool after = j < n ? kinds[j] != Kind.Left : rtl;
				byte level = before == after ? (byte)(before ? 1 : strongL) : (byte)(rtl ? 1 : 0);
				for (int k = i; k < j; k++) levels[k] = level;
				i = j;
			}
		}

		// characters of no script of their own take their neighbour's at the same level (the one before first)
		for (int i = 0; i < n; i++) {
			if (!IsCommon(scripts[i])) continue;
			Script found = Script.Common;
			for (int k = i - 1; k >= 0; k--) {
				if (levels[k] != levels[i]) break;
				if (!IsCommon(scripts[k])) { found = scripts[k]; break; }
			}
			if (IsCommon(found)) {
				for (int k = i + 1; k < n; k++) {
					if (levels[k] != levels[i]) break;
					if (!IsCommon(scripts[k])) { found = scripts[k]; break; }
				}
			}
			scripts[i] = found;
		}

		// shape each piece of one typeface, script and level, in logical order
		var logical = new List<(Segment seg, byte level)>();
		foreach (FaceRun run in faceRuns) {
			int end = run.Start + run.Length;
			for (int i = run.Start; i < end;) {
				int j = i + 1;
				while (j < end && levels[j] == levels[i] && scripts[j] == scripts[i]) j++;
				logical.Add((ShapeItem(text, i, j - i, run.Typeface, scripts[i], (levels[i] & 1) == 1, textSize, options), levels[i]));
				i = j;
			}
		}

		// right-to-left levels reverse their order on screen, highest level first
		if (anyRtl) {
			int maxLevel = 0;
			foreach (var item in logical) maxLevel = Math.Max(maxLevel, item.level);
			for (int level = maxLevel; level >= 1; level--) {
				for (int i = 0; i < logical.Count;) {
					if (logical[i].level < level) { i++; continue; }
					int j = i;
					while (j < logical.Count && logical[j].level >= level) j++;
					logical.Reverse(i, j - i);
					i = j;
				}
			}
		}
		foreach (var item in logical)
			segments.Add(item.seg);
		return segments;
	}

	private static bool IsCommon(Script script) => script == Script.Common || script == Script.Inherited || script == Script.Unknown || script == Script.Invalid;

	// scripts whose ligatures are a style choice rather than how the script is written: kept off so the letters look
	// as they always did
	private static bool OptionalLigatures(Script script)
		=> script == Script.Latin || script == Script.Greek || script == Script.Cyrillic || IsCommon(script);

	private static readonly Tag KernTag = Tag.Parse("kern");
	private static readonly Tag PaltTag = Tag.Parse("palt");
	private static readonly Tag LigaTag = Tag.Parse("liga");
	private static readonly Tag CligTag = Tag.Parse("clig");

	private static Segment ShapeItem(string text, int start, int length, SKTypeface typeface, Script script, bool rtl, float textSize, Options options) {
		var segment = new Segment { Typeface = typeface };
		HbFace? hb = FaceFor(typeface);
		if (hb == null)
			return Unshaped(segment, text.Substring(start, length), typeface, textSize);

		using var buffer = new HBBuffer();
		// the whole text goes in as context (joining and marks across the segment's edges), the item is shaped
		buffer.AddUtf16(text.AsSpan(), start, length);
		buffer.Direction = rtl ? Direction.RightToLeft : Direction.LeftToRight;
		if (!IsCommon(script))
			buffer.Script = script;
		buffer.GuessSegmentProperties();

		var features = new List<Feature>(4) { new Feature(KernTag, options.Kerning ? 1u : 0u) };
		if (options.Palt)
			features.Add(new Feature(PaltTag, 1u));
		if (OptionalLigatures(buffer.Script)) {
			features.Add(new Feature(LigaTag, 0u));
			features.Add(new Feature(CligTag, 0u));
		}
		hb.Font.Shape(buffer, features.ToArray());

		ReadOnlySpan<GlyphInfo> infos = buffer.GetGlyphInfoSpan();
		ReadOnlySpan<GlyphPosition> positions = buffer.GetGlyphPositionSpan();
		float scale = textSize / hb.UnitsPerEm;
		segment.Glyphs = new ushort[infos.Length];
		segment.Positions = new SKPoint[infos.Length];
		int pen = 0;
		for (int i = 0; i < infos.Length; i++) {
			segment.Glyphs[i] = (ushort)infos[i].Codepoint;
			segment.Positions[i] = new SKPoint((pen + positions[i].XOffset) * scale, -positions[i].YOffset * scale);
			pen += positions[i].XAdvance;
		}
		segment.Advance = pen * scale;
		return segment;
	}

	// the typeface could not be shaped: its glyphs side by side, as Skia lays them out
	private static Segment Unshaped(Segment segment, string text, SKTypeface typeface, float textSize) {
		using var font = new SKFont(typeface, textSize);
		segment.Glyphs = new ushort[font.CountGlyphs(text)];
		font.GetGlyphs(text, segment.Glyphs);
		segment.Positions = new SKPoint[segment.Glyphs.Length];
		font.GetGlyphPositions(segment.Glyphs, segment.Positions, SKPoint.Empty);
		segment.Advance = font.MeasureText(segment.Glyphs);
		return segment;
	}
}
