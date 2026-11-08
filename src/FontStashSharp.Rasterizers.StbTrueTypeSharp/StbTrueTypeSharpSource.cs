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
			var result = stbtt_FindGlyphIndex(_font, codepoint);
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
			stbtt_GetGlyphHMetrics(_font, glyphId, &advanceTemp, &lsbTemp);
			advance = (int)(advanceTemp * scale + 0.5f);

			int x0Temp, y0Temp, x1Temp, y1Temp;
			stbtt_GetGlyphBitmapBox(_font, glyphId, scale, scale, &x0Temp, &y0Temp, &x1Temp, &y1Temp);
			x0 = x0Temp;
			y0 = y0Temp;
			x1 = x1Temp + _settings.KernelWidth;
			y1 = y1Temp + _settings.KernelHeight;
		}

		/// <inheritdoc/>
		public void RasterizeGlyphBitmap(int glyphId, float fontSize, byte[] buffer, int startIndex, int outStride)
		{
			var scale = CalculateScale(fontSize);
			fixed (byte* output = &buffer[startIndex])
			{
				int x0Temp, y0Temp, x1Temp, y1Temp;
				stbtt_GetGlyphBitmapBox(_font, glyphId, scale, scale, &x0Temp, &y0Temp, &x1Temp, &y1Temp);

				var outWidth = x1Temp - x0Temp;
				var outHeight = y1Temp - y0Temp;
				stbtt_MakeGlyphBitmap(_font, output, outWidth, outHeight, outStride, scale, scale, glyphId);
				if (_settings.KernelWidth > 0)
					stbtt__v_prefilter(output, outWidth, outHeight, outStride, (uint)_settings.KernelWidth);
				if (_settings.KernelHeight > 0)
					stbtt__h_prefilter(output, outWidth, outHeight, outStride, (uint)_settings.KernelHeight);
			}
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		public int GetGlyphKernAdvance(int glyph1, int glyph2, float fontSize)
		{
			var scale = CalculateScale(fontSize);
			var result = stbtt_GetGlyphKernAdvance(_font, glyph1, glyph2);

			return (int)(result * scale);
		}

		/// <inheritdoc/>
		public float CalculateScaleForTextShaper(float fontSize) => _settings.UseEmToPixelsScale ? stbtt_ScaleForMappingEmToPixels(_font, fontSize) : stbtt_ScaleForPixelHeight(_font, fontSize);
	}
}
