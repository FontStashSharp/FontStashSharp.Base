using FontStashSharp.Interfaces;
using System;
using System.Runtime.InteropServices;
using static StbTrueTypeSharp.StbTrueType;

namespace FontStashSharp.Rasterizers.StbTrueTypeSharp
{
	/// <summary>
	/// StbTrueTypeSharp font source implementation
	/// </summary>
	internal unsafe class StbTrueTypeSharpSource : IFontSource
	{
		private int _ascent, _descent, _lineHeight;
		private readonly StbTrueTypeSharpSettings _settings;

		public stbtt_fontinfo _font;

		/// <summary>
		/// Initializes a new instance of StbTrueTypeSharpSource from font file data
		/// </summary>
		/// <param name="data">The font file data (TTF/OTF)</param>
		/// <param name="settings">The configuration settings for font rendering</param>
		/// <exception cref="ArgumentNullException">data is null</exception>
		/// <exception cref="Exception">Thrown when the font data could not be parsed</exception>
		public StbTrueTypeSharpSource(byte[] data, StbTrueTypeSharpSettings settings)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			_font = CreateFont(data, 0);
			if (_font == null)
				throw new Exception("stbtt_InitFont failed");

			_font.useOldRasterizer = settings.UseOldRasterizer;

			_settings = settings;

			int ascent, descent, lineGap;
			stbtt_GetFontVMetrics(_font, &ascent, &descent, &lineGap);

			_ascent = ascent;
			_descent = descent;
			_lineHeight = ascent - descent + lineGap;
		}

		/// <summary>
		/// Finalizes an instance of the StbTrueTypeSharpSource class
		/// </summary>
		~StbTrueTypeSharpSource()
		{
			Dispose(false);
		}

		/// <summary>
		/// Releases unmanaged resources held by the font source
		/// </summary>
		/// <param name="disposing">Whether managed resources should also be released</param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing && _font != null)
			{
				_font.Dispose();
				_font = null;
			}
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Calculates the scale factor for the specified font size
		/// </summary>
		/// <param name="size">The font size</param>
		/// <returns>The scale factor</returns>
		private float CalculateScale(float size) => _settings.UseEmToPixelsScale ? stbtt_ScaleForMappingEmToPixels(_font, size) : stbtt_ScaleForPixelHeight(_font, size);

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
			var result = stbtt_FindGlyphIndex(_font, codepoint);
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
			stbtt_GetGlyphHMetrics(_font, glyphId, &advanceTemp, &lsbTemp);
			advance = (int)(advanceTemp * scale + 0.5f);

			int x0Temp, y0Temp, x1Temp, y1Temp;
			stbtt_GetGlyphBitmapBox(_font, glyphId, scale, scale, &x0Temp, &y0Temp, &x1Temp, &y1Temp);
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
			fixed (byte* output = &buffer[startIndex])
			{
				stbtt_MakeGlyphBitmap(_font, output, outWidth, outHeight, outStride, scale, scale, glyphId);
				if (_settings.KernelWidth > 0)
					stbtt__v_prefilter(output, outWidth, outHeight, outStride, (uint)_settings.KernelWidth);
				if (_settings.KernelHeight > 0)
					stbtt__h_prefilter(output, outWidth, outHeight, outStride, (uint)_settings.KernelHeight);
			}
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

			byte* data = null;
			try
			{
				int w, h, x, y;
				data = stbtt_GetGlyphSDF(_font, scale, glyphId, padding, onedge_value, pixel_dist_scale, &w, &h, &x, &y);
				if (data != null)
				{
					for (var i = 0; i < w * h; ++i)
					{
						buffer[i + startIndex] = data[i];
					}
				}
			}
			catch (Exception)
			{
				if (data != null)
				{
					Marshal.FreeHGlobal(new IntPtr(data));
				}

				throw;
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
			var result = stbtt_GetGlyphKernAdvance(_font, glyph1, glyph2);

			return (int)(result * scale);
		}

		/// <summary>
		/// Calculates the scale used by the text shaper for the specified font size
		/// </summary>
		/// <param name="fontSize">The font size</param>
		/// <returns>The calculated scale</returns>
		public float CalculateScaleForTextShaper(float fontSize) => _settings.UseEmToPixelsScale ? stbtt_ScaleForMappingEmToPixels(_font, fontSize) : stbtt_ScaleForPixelHeight(_font, fontSize);
	}
}
