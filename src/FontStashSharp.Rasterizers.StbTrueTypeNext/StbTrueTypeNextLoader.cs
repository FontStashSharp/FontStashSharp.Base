using FontStashSharp.Interfaces;

namespace FontStashSharp.Rasterizers.StbTrueTypeNext
{
	/// <summary>
	/// StbTrueTypeNext font loader implementation
	/// </summary>
	public class StbTrueTypeNextLoader : IFontLoader
	{
		private readonly StbTrueTypeNextSettings _settings;

		/// <summary>
		/// Initializes a new instance of StbTrueTypeNextLoader with specified settings
		/// </summary>
		/// <param name="settings">Configuration settings for the font loader</param>
		public StbTrueTypeNextLoader(StbTrueTypeNextSettings settings)
		{
			_settings = settings;
		}

		/// <inheritdoc/>
		public IFontSource Load(byte[] data)
		{
			return new StbTrueTypeNextSource(data, _settings);
		}
	}
}
