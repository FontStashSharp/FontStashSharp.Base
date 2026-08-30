using FontStashSharp.Interfaces;
using SharpAstro.Fonts;
using SharpAstro.Fonts.Rasterizer;
using System;
using System.Collections.Generic;

namespace FontStashSharp.Rasterizers.SharpAstro
{
	/// <summary>
	/// SharpAstro.Fonts font source implementation
	/// </summary>
	public class SharpAstroSource : IFontSource
	{
		private readonly OpenTypeFont _font;
		private readonly Dictionary<long, int> _kernings = new Dictionary<long, int>();

		/// <summary>
		/// Initializes a new instance of SharpAstroSource from font file data
		/// </summary>
		/// <param name="data">The font file data (TTF/OTF)</param>
		/// <exception cref="ArgumentNullException">data is null</exception>
		/// <exception cref="Exception">Thrown when the font data could not be parsed</exception>
		public SharpAstroSource(byte[] data)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			_font = OpenTypeFont.Load(data);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
		}

		/// <inheritdoc/>
		public void GetMetricsForSize(float fontSize, out int ascent, out int descent, out int lineHeight)
		{
			var scale = CalculateScale(fontSize);

			ascent = (int)(_font.Hhea.Ascender * scale + 0.5f);
			descent = (int)(_font.Hhea.Descender * scale - 0.5f);
			lineHeight = (int)((_font.Hhea.Ascender - _font.Hhea.Descender + _font.Hhea.LineGap) * scale + 0.5f);
		}

		/// <inheritdoc/>
		public int? GetGlyphId(int codepoint)
		{
			var result = _font.GetGlyphId((uint)codepoint);
			if (result == 0)
			{
				return null;
			}

			return (int)result;
		}

		/// <inheritdoc/>
		public void GetGlyphMetrics(int glyphId, float fontSize, out int advance, out int x0, out int y0, out int x1, out int y1)
		{
			var scale = CalculateScale(fontSize);

			advance = (int)(_font.Hmtx.GetAdvanceWidth((uint)glyphId) * scale + 0.5f);

			var bitmap = RenderGlyph(glyphId, fontSize);
			x0 = bitmap.Left;
			y0 = -bitmap.Top;
			x1 = x0 + bitmap.Width;
			y1 = y0 + bitmap.Height;
		}

		/// <inheritdoc/>
		public void RasterizeGlyphBitmap(FontRasterizationMode mode, int glyphId, float fontSize, byte[] buffer, int startIndex, int outWidth, int outHeight, int outStride)
		{
			if (mode == FontRasterizationMode.SDF)
			{
				throw new NotImplementedException("FreeTypeSource doesn't support SDF.");
			}

			var bitmap = RenderGlyph(glyphId, fontSize);
			if (bitmap.IsEmpty)
			{
				return;
			}

			var width = Math.Min(outWidth, bitmap.Width);
			var height = Math.Min(outHeight, bitmap.Height);
			for (var y = 0; y < height; ++y)
			{
				var srcIndex = y * bitmap.Width;
				var dstIndex = startIndex + y * outStride;
				for (var x = 0; x < width; ++x)
				{
					buffer[dstIndex + x] = bitmap.Alpha[srcIndex + x];
				}
			}
		}

		/// <inheritdoc/>
		public int GetGlyphKernAdvance(int previousGlyphId, int glyphId, float fontSize)
		{
			var key = ((long)previousGlyphId << 32) | (uint)glyphId;
			int result;
			if (!_kernings.TryGetValue(key, out result))
			{
				result = _font.GetKerning((uint)previousGlyphId, (uint)glyphId);
				_kernings[key] = result;
			}

			return (int)(result * CalculateScale(fontSize));
		}

		/// <inheritdoc/>
		public float CalculateScaleForTextShaper(float fontSize) => CalculateScale(fontSize);

		private float CalculateScale(float fontSize) => fontSize / _font.UnitsPerEm;

		private GlyphBitmap RenderGlyph(int glyphId, float fontSize) => _font.RenderGlyph((uint)glyphId, fontSize);
	}
}
