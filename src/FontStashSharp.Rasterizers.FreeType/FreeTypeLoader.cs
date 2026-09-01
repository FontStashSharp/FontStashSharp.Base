using FontStashSharp.Interfaces;

namespace FontStashSharp.Rasterizers.FreeType
{
	/// <summary>
	/// FreeType font loader implementation
	/// </summary>
	public class FreeTypeLoader : IFontLoader
	{
		/// <inheritdoc/>
		public IFontSource Load(byte[] data)
		{
			return new FreeTypeSource(data);
		}
	}
}
