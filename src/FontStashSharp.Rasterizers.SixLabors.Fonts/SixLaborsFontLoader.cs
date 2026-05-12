using FontStashSharp.Interfaces;

namespace FontStashSharp.Rasterizers.SixLabors.Fonts
{
	/// <summary>
	/// SixLabors font loader implementation
	/// </summary>
	public class SixLaborsFontLoader : IFontLoader
	{
		/// <summary>
		/// Loads a font from byte data using SixLabors
		/// </summary>
		/// <param name="data">The font file data (TTF/OTF)</param>
		/// <returns>A font source for rendering glyphs</returns>
		public IFontSource Load(byte[] data)
		{
			return new SixLaborsFontSource(data);
		}
	}
}
