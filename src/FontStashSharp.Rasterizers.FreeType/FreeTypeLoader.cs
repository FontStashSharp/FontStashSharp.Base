using FontStashSharp.Interfaces;

namespace FontStashSharp.Rasterizers.FreeType
{
	/// <summary>
	/// FreeType font loader implementation
	/// </summary>
	public class FreeTypeLoader : IFontLoader
	{
		/// <summary>
		/// Loads a font from byte data using FreeType
		/// </summary>
		/// <param name="data">The font file data (TTF/OTF)</param>
		/// <returns>A font source for rendering glyphs</returns>
		public IFontSource Load(byte[] data)
		{
			return new FreeTypeSource(data);
		}
	}
}
