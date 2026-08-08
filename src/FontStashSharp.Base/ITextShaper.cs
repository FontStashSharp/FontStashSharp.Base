namespace FontStashSharp.Interfaces
{
	/// <summary>
	/// Provides information for text shaping operations
	/// </summary>
	public interface ITextShapingInfoProvider
	{
		/// <summary>
		/// Gets the font source ID for a specific codepoint
		/// </summary>
		/// <param name="codepoint">The Unicode codepoint to look up</param>
		/// <returns>The font source ID, or null if not available</returns>
		int? GetFontSourceId(int codepoint);

		/// <summary>
		/// Gets the text shaper font ID for a specific font source ID
		/// </summary>
		/// <param name="fontSourceId">The font source ID</param>
		/// <returns>The text shaper font ID</returns>
		int GetTextShaperFontId(int fontSourceId);

		/// <summary>
		/// Calculates the scale factor for text shaping
		/// </summary>
		/// <param name="fontSourceId">The font source ID</param>
		/// <param name="fontSize">The desired font size</param>
		/// <returns>The scale factor to apply</returns>
		float CalculateScale(int fontSourceId, float fontSize);
	}


	/// <summary>
	/// Provides text shaping functionality
	/// </summary>
	public interface ITextShaper
	{
		/// <summary>
		/// Registers a ttf font
		/// </summary>
		/// <param name="data"></param>
		/// <returns>Assigned id</returns>
		int RegisterTtfFont(byte[] data);

		/// <summary>
		/// Disposes and removes font from the text shaper
		/// </summary>
		/// <param name="id"></param>
		void RemoveFont(int id);

		/// <summary>
		/// Shape text using HarfBuzz
		/// </summary>
		/// <param name="text">The text to shape</param>
		/// <param name="fontSize">The font size</param>
		/// <param name="infoProvider">Provides info for the text shaping</param>
		/// <returns>Shaped text with glyph information</returns>
		ShapedText Shape(string text, float fontSize, ITextShapingInfoProvider infoProvider);
	}
}
