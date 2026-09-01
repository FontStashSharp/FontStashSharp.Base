using FontStashSharp.Interfaces;

namespace FontStashSharp.Rasterizers.SharpAstro
{
	/// <summary>
	/// SharpAstro.Fonts font loader implementation
	/// </summary>
	public class SharpAstroLoader : IFontLoader
	{
		/// <inheritdoc/>
		public IFontSource Load(byte[] data)
		{
			return new SharpAstroSource(data);
		}
	}
}
