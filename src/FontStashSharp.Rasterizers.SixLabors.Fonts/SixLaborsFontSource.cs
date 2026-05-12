using FontStashSharp.Interfaces;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using TrippyGL.Fonts.Building;

namespace FontStashSharp.Rasterizers.SixLabors.Fonts
{
	/// <summary>
	/// SixLabors font source implementation
	/// </summary>
	public class SixLaborsFontSource : IFontSource
	{
		private FontGlyphSource _source;
		private float AscentBase, DescentBase, LineHeightBase;

		/// <summary>
		/// Initializes a new instance of SixLaborsFontSource from font data
		/// </summary>
		/// <param name="data">The font file data (TTF/OTF)</param>
		public SixLaborsFontSource(byte[] data)
		{
			FontInstance fontInstance;
			using (var ms = new MemoryStream(data))
			{
				fontInstance = FontInstance.LoadFont(ms);
				_source = new FontGlyphSource(fontInstance);
			}

			var fh = fontInstance.Ascender - fontInstance.Descender;
			AscentBase = fontInstance.Ascender / (float)fh;
			DescentBase = fontInstance.Descender / (float)fh;
			LineHeightBase = fontInstance.LineHeight / (float)fh;
		}

		/// <summary>
		/// Calculates the scale factor for text shaping
		/// </summary>
		/// <param name="fontSize">The desired font size</param>
		/// <returns>The scale factor to apply</returns>
		public float CalculateScaleForTextShaper(float fontSize)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Releases resources used by the font source
		/// </summary>
		public void Dispose()
		{
		}

		/// <summary>
		/// Gets the glyph ID for a Unicode codepoint
		/// </summary>
		/// <param name="codepoint">The Unicode codepoint</param>
		/// <returns>The glyph ID, or null if not found</returns>
		public int? GetGlyphId(int codepoint)
		{
			return codepoint;
		}

		/// <summary>
		/// Gets the kerning advance between two glyphs
		/// </summary>
		/// <param name="previousGlyphId">The previous glyph ID</param>
		/// <param name="glyphId">The current glyph ID</param>
		/// <param name="fontSize">The font size</param>
		/// <returns>The kerning advance in pixels</returns>
		public int GetGlyphKernAdvance(int previousGlyphId, int glyphId, float fontSize)
		{
			var kerning = _source.GetKerning(fontSize, previousGlyphId, glyphId);
			return (int)kerning.X;
		}

		/// <summary>
		/// Gets the metrics for a specific glyph at a given font size
		/// </summary>
		/// <param name="glyphId">The glyph ID</param>
		/// <param name="fontSize">The font size</param>
		/// <param name="advance">Output: horizontal advance width</param>
		/// <param name="x0">Output: left boundary of glyph bounds</param>
		/// <param name="y0">Output: top boundary of glyph bounds</param>
		/// <param name="x1">Output: right boundary of glyph bounds</param>
		/// <param name="y1">Output: bottom boundary of glyph bounds</param>
		public void GetGlyphMetrics(int glyphId, float fontSize, out int advance, out int x0, out int y0, out int x1, out int y1)
		{
			var path = _source.CreatePath(fontSize, glyphId);

			advance = (int)_source.GetAdvance(fontSize, glyphId);
			x0 = path.Bounds.X;
			y0 = path.Bounds.Y;
			x1 = path.Bounds.Right;
			y1 = path.Bounds.Bottom;
		}

		/// <summary>
		/// Gets font metrics for the specified font size
		/// </summary>
		/// <param name="fontSize">The font size</param>
		/// <param name="ascent">Output: ascent in pixels</param>
		/// <param name="descent">Output: descent in pixels</param>
		/// <param name="lineHeight">Output: line height in pixels</param>
		public void GetMetricsForSize(float fontSize, out int ascent, out int descent, out int lineHeight)
		{
			ascent = (int)(fontSize * AscentBase + 0.5f);
			descent = (int)(fontSize * DescentBase - 0.5f);
			lineHeight = (int)(fontSize * LineHeightBase + 0.5f);
		}

		/// <summary>
		/// Rasterizes a glyph bitmap to a buffer
		/// </summary>
		/// <param name="glyphId">The glyph ID</param>
		/// <param name="fontSize">The font size</param>
		/// <param name="buffer">The output buffer for bitmap data</param>
		/// <param name="startIndex">Starting index in the buffer</param>
		/// <param name="outWidth">Width of the output bitmap</param>
		/// <param name="outHeight">Height of the output bitmap</param>
		/// <param name="outStride">Stride (bytes per row) of the output bitmap</param>
		public void RasterizeGlyphBitmap(int glyphId, float fontSize, byte[] buffer, int startIndex, int outWidth, int outHeight, int outStride)
		{
			var path = _source.CreatePath(fontSize, glyphId);
			Image<Rgba32> image = new Image<Rgba32>(path.Bounds.Width, path.Bounds.Height);
			_source.DrawGlyphToImage(path, new System.Drawing.Point(0, 0), image);

			for(var y = 0; y < outHeight; ++y)
			{
				var pos = (y * outStride) + startIndex;
				for (var x = 0; x < outWidth; ++x)
				{
					var color = image[x, y];
					buffer[pos] = color.A;
					++pos;
				}
			}
		}
	}
}
