using FontStashSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace FontStashSharp
{
	/// <summary>
	/// Text shaper implementation using HarfBuzz for advanced text layout
	/// </summary>
	public class HarfBuzzTextShaper : ITextShaper
	{
		private int _lastId = 0;
		private readonly Dictionary<int, HarfBuzzFont> _harfBuzzFonts = new Dictionary<int, HarfBuzzFont>();

		/// <summary>
		/// Gets or sets whether to enable bidirectional (BiDi) text support for mixed LTR/RTL text.
		/// When enabled, text with mixed Latin and RTL scripts (Arabic, Hebrew, etc.) will be displayed in correct order.
		/// Default: true
		/// </summary>
		public bool EnableBiDi { get; set; } = true;

		/// <inheritdoc/>
		public int RegisterTtfFont(byte[] data)
		{
			var hbFont = new HarfBuzzFont(data);

			_harfBuzzFonts[_lastId] = hbFont;
			var result = _lastId;

			++_lastId;

			return result;
		}

		/// <inheritdoc/>
		public void RemoveFont(int id)
		{
			var font = _harfBuzzFonts[id];
			font.Dispose();

			_harfBuzzFonts.Remove(id);
		}

		private struct FontRun
		{
			public int Start;
			public int Length;
			public int FontSourceId;
		}

		private struct Grapheme
		{
			public int Start;
			public int Length;
			public int FontSourceId;
		}
		private static List<Grapheme> BuildGraphemes(string text, ITextShapingInfoProvider infoProvider)
		{
			List<Grapheme> result = new List<Grapheme>();
			TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);

			while (enumerator.MoveNext())
			{
				string element = enumerator.GetTextElement();
				int start = enumerator.ElementIndex;
				int length = element.Length;

				int fontSourceId = 0;

				int i = 0;
				while (i < element.Length)
				{
					int codepoint;

					if (i < element.Length - 1 && char.IsSurrogatePair(element, i))
					{
						codepoint = char.ConvertToUtf32(element, i);
						i += 2;
					}
					else
					{
						codepoint = element[i];
						i += 1;
					}

					int? fs = infoProvider.GetFontSourceId(codepoint);
					if (fs.HasValue)
					{
						fontSourceId = fs.Value;
						break;
					}
				}

				Grapheme g;
				g.Start = start;
				g.Length = length;
				g.FontSourceId = fontSourceId;

				result.Add(g);
			}

			return result;
		}

		private static int FindFirstOverlapping(List<Grapheme> graphemes, int start)
		{
			int lo = 0;
			int hi = graphemes.Count;

			while (lo < hi)
			{
				int mid = (lo + hi) / 2;
				Grapheme g = graphemes[mid];

				if (g.Start + g.Length <= start)
					lo = mid + 1;
				else
					hi = mid;
			}

			return lo;
		}
		private static List<FontRun> SegmentIntoFontRuns(List<Grapheme> graphemes, int start, int length)
		{
			List<FontRun> runs = new List<FontRun>();
			if (graphemes == null || length <= 0)
				return runs;

			int end = start + length;

			int index = FindFirstOverlapping(graphemes, start);
			for (int i = index; i < graphemes.Count; i++)
			{
				Grapheme g = graphemes[i];

				if (g.Start >= end)
					break;

				if (g.Start < start || g.Start + g.Length > end)
					continue;

				if (runs.Count > 0)
				{
					int lastIndex = runs.Count - 1;
					FontRun last = runs[lastIndex];

					if (last.FontSourceId == g.FontSourceId &&
						last.Start + last.Length == g.Start)
					{
						last.Length += g.Length;
						runs[lastIndex] = last;
						continue;
					}
				}

				FontRun fr;
				fr.Start = g.Start;
				fr.Length = g.Length;
				fr.FontSourceId = g.FontSourceId;

				runs.Add(fr);
			}

			return runs;
		}

		private static bool IsRTLParagraph(List<DirectionalRun> runs)
		{
			if (runs == null)
				return false;

			for (int i = 0; i < runs.Count; i++)
			{
				if (runs[i].Direction == TextDirection.RTL)
					return true;

				if (runs[i].Direction == TextDirection.LTR)
					return false;
			}

			return false;
		}

		/// <inheritdoc/>
		public ShapedText Shape(string text, float fontSize, ITextShapingInfoProvider infoProvider)
		{
			if (string.IsNullOrEmpty(text))
			{
				ShapedText empty = new ShapedText();
				empty.Glyphs = new ShapedGlyph[0];
				empty.OriginalText = text ?? "";
				empty.FontSize = fontSize;
				return empty;
			}

			// 1. BIDI RUNS
			List<DirectionalRun> dirRuns;
			if (EnableBiDi)
			{
				dirRuns = BiDiAnalyzer.SegmentIntoDirectionalRuns(text);
			}
			else
			{
				dirRuns = new List<DirectionalRun>();
				DirectionalRun dr;
				dr.Start = 0;
				dr.Length = text.Length;
				dr.Direction = TextDirection.LTR;
				dirRuns.Add(dr);
			}

			// 2. GRAPHEMES
			List<Grapheme> graphemes = BuildGraphemes(text, infoProvider);

			List<List<ShapedGlyph>> shapedRuns = new List<List<ShapedGlyph>>(dirRuns.Count);

			// 3. SHAPE EACH RUN
			for (int r = 0; r < dirRuns.Count; r++)
			{
				DirectionalRun dr = dirRuns[r];
				bool isRtl = dr.Direction == TextDirection.RTL;

				List<ShapedGlyph> glyphs = new List<ShapedGlyph>();
				List<FontRun> fontRuns = SegmentIntoFontRuns(graphemes, dr.Start, dr.Length);

				if (isRtl)
					fontRuns.Reverse();

				for (int f = 0; f < fontRuns.Count; f++)
				{
					FontRun fr = fontRuns[f];

					int fontId = infoProvider.GetTextShaperFontId(fr.FontSourceId);

					HarfBuzzFont hbFont;
					if (!_harfBuzzFonts.TryGetValue(fontId, out hbFont))
						throw new InvalidOperationException("Font not registered: " + fontId);

					float scale = infoProvider.CalculateScale(fr.FontSourceId, fontSize);

					using (HarfBuzzSharp.Buffer buffer = new HarfBuzzSharp.Buffer())
					{
						buffer.AddUtf16(text, fr.Start, fr.Length);

						buffer.Direction = isRtl
							? HarfBuzzSharp.Direction.RightToLeft
							: HarfBuzzSharp.Direction.LeftToRight;

						buffer.GuessSegmentProperties();
						hbFont.Shape(buffer);

						HarfBuzzSharp.GlyphInfo[] infos = buffer.GlyphInfos;
						HarfBuzzSharp.GlyphPosition[] positions = buffer.GlyphPositions;

						for (int i = 0; i < infos.Length; i++)
						{
							HarfBuzzSharp.GlyphInfo info = infos[i];
							HarfBuzzSharp.GlyphPosition pos = positions[i];

							ShapedGlyph sg = new ShapedGlyph();
							sg.GlyphId = (int)info.Codepoint;
							sg.Cluster = (int)info.Cluster;
							sg.FontSourceId = fr.FontSourceId;
							sg.XAdvance = pos.XAdvance * scale;
							sg.YAdvance = pos.YAdvance * scale;
							sg.XOffset = pos.XOffset * scale;
							sg.YOffset = -pos.YOffset * scale;

							glyphs.Add(sg);
						}
					}
				}

				shapedRuns.Add(glyphs);
			}

			// 4. PARAGRAPH VISUAL ORDER
			if (IsRTLParagraph(dirRuns))
				shapedRuns.Reverse();

			// 5. FLATTEN
			List<ShapedGlyph> finalGlyphs = new List<ShapedGlyph>();
			for (int i = 0; i < shapedRuns.Count; i++)
				finalGlyphs.AddRange(shapedRuns[i]);

			ShapedText shaped = new ShapedText();
			shaped.Glyphs = finalGlyphs.ToArray();
			shaped.OriginalText = text;
			shaped.FontSize = fontSize;

			return shaped;
		}
	}
}
