using FontStashSharp.Interfaces;

namespace FontStashSharp.Rasterizers.SharpAstro
{
	/// <summary>
	/// SharpAstro.Fonts font loader implementation
	/// </summary>
	public class SharpAstroLoader : IFontLoader
	{
		/// <summary>
		/// Loads a font from byte data using SharpAstro.Fonts
		/// </summary>
		/// <param name="data">The font file data (TTF/OTF)</param>
		/// <returns>A font source for rendering glyphs</returns>
		public IFontSource Load(byte[] data)
		{
			return new SharpAstroSource(data);
		}
	}
}
