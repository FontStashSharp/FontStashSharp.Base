using System;

namespace FontStashSharp.Interfaces
{
	/// <summary>
	/// Specifies the mode used to rasterize glyph bitmaps
	/// </summary>
	public enum FontRasterizationMode
	{
		/// <summary>
		/// Standard anti-aliased rasterization
		/// </summary>
		Standard,

		/// <summary>
		/// Signed distance field (SDF) rasterization
		/// </summary>
		SDF
	}

	/// <summary>
	/// Represents a font source that can be used to rasterize glyphs
	/// </summary>
	public interface IFontSource: IDisposable
	{
		/// <summary>
		/// Gets the font metrics for the specified font size
		/// </summary>
		/// <param name="fontSize">The font size</param>
		/// <param name="ascent">The distance from the baseline to the top of the em box</param>
		/// <param name="descent">The distance from the baseline to the bottom of the em box</param>
		/// <param name="lineHeight">The total line height</param>
		void GetMetricsForSize(float fontSize, out int ascent, out int descent, out int lineHeight);

		/// <summary>
		/// Gets the glyph id for the specified codepoint
		/// </summary>
		/// <param name="codepoint">The Unicode codepoint</param>
		/// <returns>The glyph id, or null if the codepoint is not present in the font</returns>
		int? GetGlyphId(int codepoint);

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
		void GetGlyphMetrics(int glyphId, float fontSize, out int advance, out int x0, out int y0, out int x1, out int y1);

		/// <summary>
		/// Rasterizes the glyph into a bitmap
		/// </summary>
		/// <param name="mode">The rasterization mode</param>
		/// <param name="glyphId">The glyph id</param>
		/// <param name="fontSize">The font size</param>
		/// <param name="buffer">The target buffer to write the bitmap into</param>
		/// <param name="startIndex">The starting index in the buffer</param>
		/// <param name="outWidth">The width of the output bitmap</param>
		/// <param name="outHeight">The height of the output bitmap</param>
		/// <param name="outStride">The stride (number of bytes per row) of the output bitmap</param>
		void RasterizeGlyphBitmap(FontRasterizationMode mode, int glyphId, float fontSize, byte[] buffer, int startIndex, int outWidth, int outHeight, int outStride);

		/// <summary>
		/// Gets the kerning advance between two adjacent glyphs
		/// </summary>
		/// <param name="previousGlyphId">The glyph id of the previous glyph</param>
		/// <param name="glyphId">The glyph id of the current glyph</param>
		/// <param name="fontSize">The font size</param>
		/// <returns>The kerning advance</returns>
		int GetGlyphKernAdvance(int previousGlyphId, int glyphId, float fontSize);

		/// <summary>
		/// Calculates the scale used by the text shaper for the specified font size
		/// </summary>
		/// <param name="fontSize">The font size</param>
		/// <returns>The calculated scale</returns>
		float CalculateScaleForTextShaper(float fontSize);
	}

	/// <summary>
	/// Font Rasterization Service
	/// </summary>
	public interface IFontLoader
	{
		/// <summary>
		/// Loads a font from the specified data
		/// </summary>
		/// <param name="data">The font data</param>
		/// <returns>The loaded font source</returns>
		IFontSource Load(byte[] data);
	}
}
