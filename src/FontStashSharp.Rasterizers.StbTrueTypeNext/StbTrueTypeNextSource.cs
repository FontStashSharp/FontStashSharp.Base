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

		/// <inheritdoc/>
		public void GetMetricsForSize(float fontSize, out int ascent, out int descent, out int lineHeight)
		{
			var scale = CalculateScale(fontSize);
			ascent = (int)(_ascent * scale + 0.5f);
			descent = (int)(_descent * scale - 0.5f);
			lineHeight = (int)(_lineHeight * scale + 0.5f);
		}

		/// <inheritdoc/>
		public int? GetGlyphId(int codepoint)
		{
			var result = _font.FindGlyphIndex(codepoint);
			if (result == 0)
			{
				return null;
			}

			return result;
		}

		/// <inheritdoc/>
		public void GetGlyphMetrics(int glyphId, float fontSize, out int advance, out int x0, out int y0, out int x1, out int y1)
		{
			var scale = CalculateScale(fontSize);

			int advanceTemp, lsbTemp;
			_font.GetGlyphHMetrics(glyphId, out advanceTemp, out lsbTemp);
			advance = (int)(advanceTemp * scale + 0.5f);

			var box = _font.GetGlyphBoxInPixels(glyphId, scale, scale) ?? STBGlyphBitmapBox.Zero;
			x0 = box.X0;
			y0 = box.Y0;
			x1 = box.X1 + _settings.KernelWidth;
			y1 = box.Y1 + _settings.KernelHeight;
		}

		/// <inheritdoc/>
		public void RasterizeGlyphBitmap(int glyphId, float fontSize, byte[] buffer, int startIndex, int outStride)
		{
			var scale = CalculateScale(fontSize);

			_font.DrawGlyphOnBitmap(glyphId, buffer, startIndex, outStride, scale, scale, 0, 0, _settings.KernelWidth, _settings.KernelHeight);
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		public int GetGlyphKernAdvance(int glyph1, int glyph2, float fontSize)
		{
			var scale = CalculateScale(fontSize);
			var result = _font.GetGlyphKernAdvance(glyph1, glyph2);

			return (int)(result * scale);
		}

		/// <inheritdoc/>
		public float CalculateScaleForTextShaper(float fontSize) =>
			_settings.UseEmToPixelsScale ? _font.ScaleForMappingEmToPixels(fontSize) : _font.ScaleForPixelHeight(fontSize);
	}
}
