using FontStashSharp.Interfaces;
using StbTrueTypeNext;
using System;

namespace FontStashSharp.Rasterizers.StbTrueTypeNext
{
	/// <summary>
	/// StbTrueTypeNext font source implementation
	/// </summary>
	internal class StbTrueTypeNextSource : IFontSource
	{
		private readonly STBFont _font;
		private int _ascent, _descent, _lineHeight;
		private readonly StbTrueTypeNextSettings _settings;


		/// <summary>
		/// Initializes a new instance of StbTrueTypeNextSource from font file data
		/// </summary>
		/// <param name="data">The font file data (TTF/OTF)</param>
		/// <param name="settings">The configuration settings for font rendering</param>
		/// <exception cref="ArgumentNullException">data is null</exception>
		/// <exception cref="Exception">Thrown when the font data could not be parsed</exception>
		public StbTrueTypeNextSource(byte[] data, StbTrueTypeNextSettings settings)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			_font = new STBFont(data);

			if (settings.UseOldRasterizer)
			{
				_font.RasterizerVersion = STBRasterizerVersion.V1;
			}
			else
			{
				_font.RasterizerVersion = STBRasterizerVersion.V2;
			}

			_settings = settings;

			int ascent, descent, lineGap;
			_font.GetFontVMetrics(out ascent, out descent, out lineGap);

			_ascent = ascent;
			_descent = descent;
			_lineHeight = ascent - descent + lineGap;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
		}

		/// <summary>
		/// Calculates the scale factor for the specified font size
		/// </summary>
		/// <param name="size">The font size</param>
		/// <returns>The scale factor</returns>
		private float CalculateScale(float size) => _settings.UseEmToPixelsScale ? _font.ScaleForMappingEmToPixels(size) : _font.ScaleForPixelHeight(size);

		/// <summary>
		/// Gets the font metrics for the specified font size
		/// </summary>
		/// <param name="fontSize">The font size</param>
		/// <param name="ascent">The distance from the baseline to the top of the em box</param>
		/// <param name="descent">The distance from the baseline to the bottom of the em box</param>
		/// <param name="lineHeight">The total line height</param>
		public void GetMetricsForSize(float fontSize, out int ascent, out int descent, out int lineHeight)
		{
			var scale = CalculateScale(fontSize);
			ascent = (int)(_ascent * scale + 0.5f);
			descent = (int)(_descent * scale - 0.5f);
			lineHeight = (int)(_lineHeight * scale + 0.5f);
		}

		/// <summary>
		/// Gets the glyph id for the specified codepoint
		/// </summary>
		/// <param name="codepoint">The Unicode codepoint</param>
		/// <returns>The glyph id, or null if the codepoint is not present in the font</returns>
		public int? GetGlyphId(int codepoint)
		{
			var result = _font.FindGlyphIndex(codepoint);
			if (result == 0)
			{
				return null;
			}

			return result;
		}

		/// <summary>
		/// Gets the glyph metrics for the specified glyph
		/// </summary>
		/// <param name="glyphId">The glyph id</param>
		/// <param name="fontSize">The font size</param>
		/// <param name="advance">The horizontal advance of the glyph</param>
		/// <param name="x0">The x coordinate of the top-left corner of the glyph bounding box</param>
		/// <param name="y0">The y coordinate of the top-left corner of the glyph bounding box</param>
		/// <param name="x1">The x coordinate of the bottom-right corner of the glyph bounding box</param>
		/// <param name="y1">The y coordinate of the bottom-right corner of the glyph bounding box</param>
		public void GetGlyphMetrics(int glyphId, float fontSize, out int advance, out int x0, out int y0, out int x1, out int y1)
		{
			var scale = CalculateScale(fontSize);

			int advanceTemp, lsbTemp;
			_font.GetGlyphHMetrics(glyphId, out advanceTemp, out lsbTemp);
			advance = (int)(advanceTemp * scale + 0.5f);

			int x0Temp, y0Temp, x1Temp, y1Temp;
			_font.GetGlyphBitmapBox(glyphId, scale, scale, out x0Temp, out y0Temp, out x1Temp, out y1Temp);
			x0 = x0Temp;
			y0 = y0Temp;
			x1 = x1Temp + _settings.KernelWidth;
			y1 = y1Temp + _settings.KernelHeight;
		}

		/// <summary>
		/// Rasterizes a glyph into an 8-bit bitmap buffer
		/// </summary>
		/// <param name="glyphId">The glyph id</param>
		/// <param name="fontSize">The font size</param>
		/// <param name="buffer">The destination buffer</param>
		/// <param name="startIndex">The index in the buffer where the bitmap data starts</param>
		/// <param name="outWidth">The width of the output bitmap</param>
		/// <param name="outHeight">The height of the output bitmap</param>
		/// <param name="outStride">The number of bytes per row in the output buffer</param>
		public void RasterizeGlyphBitmap(int glyphId, float fontSize, byte[] buffer, int startIndex, int outWidth, int outHeight, int outStride)
		{
			var scale = CalculateScale(fontSize);

			float subX, subY;
			_font.MakeGlyphBitmapSubpixelPrefilter(buffer, startIndex, outWidth, outHeight, outStride, scale, scale, 0, 0,
				_settings.KernelWidth, _settings.KernelHeight, out subX, out subY, glyphId);
		}

		/// <summary>
		/// Rasterizes a glyph into a signed distance field (SDF) representation
		/// </summary>
		/// <param name="glyphId">The glyph id</param>
		/// <param name="fontSize">The font size</param>
		/// <param name="buffer">The destination buffer</param>
		/// <param name="startIndex">The index in the buffer where the SDF data starts</param>
		/// <param name="padding">The padding added around the glyph</param>
		/// <param name="onedge_value">The value used on the glyph edge</param>
		/// <param name="pixel_dist_scale">The scale of pixel distances</param>
		public void RasterizeGlyphSDF(int glyphId, float fontSize, byte[] buffer, int startIndex, int padding, byte onedge_value, float pixel_dist_scale)
		{
			var scale = CalculateScale(fontSize);

			int w, h, x, y;
			var data = _font.GetGlyphSDF(scale, glyphId, padding, onedge_value, pixel_dist_scale, out w, out h, out x, out y);
			if (data != null)
			{
				for (var i = 0; i < w * h; ++i)
				{
					buffer[i + startIndex] = data[i];
				}
			}
		}

		/// <summary>
		/// Gets the kerning advance between two adjacent glyphs
		/// </summary>
		/// <param name="glyph1">The glyph id of the first glyph</param>
		/// <param name="glyph2">The glyph id of the second glyph</param>
		/// <param name="fontSize">The font size</param>
		/// <returns>The kerning advance</returns>
		public int GetGlyphKernAdvance(int glyph1, int glyph2, float fontSize)
		{
			var scale = CalculateScale(fontSize);
			var result = _font.GetGlyphKernAdvance(glyph1, glyph2);

			return (int)(result * scale);
		}

		/// <summary>
		/// Calculates the scale used by the text shaper for the specified font size
		/// </summary>
		/// <param name="fontSize">The font size</param>
		/// <returns>The calculated scale</returns>
		public float CalculateScaleForTextShaper(float fontSize) =>
			_settings.UseEmToPixelsScale ? _font.ScaleForMappingEmToPixels(fontSize) : _font.ScaleForPixelHeight(fontSize);
	}
}
