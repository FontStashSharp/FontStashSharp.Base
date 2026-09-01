using FontStashSharp.Interfaces;

namespace FontStashSharp.Rasterizers.StbTrueTypeSharp
{
	/// <summary>
	/// StbTrueTypeSharp font loader implementation
	/// </summary>
	public class StbTrueTypeSharpLoader : IFontLoader
	{
		private readonly StbTrueTypeSharpSettings _settings;

		/// <summary>
		/// Initializes a new instance of StbTrueTypeSharpLoader with specified settings
		/// </summary>
		/// <param name="settings">Configuration settings for the font loader</param>
		public StbTrueTypeSharpLoader(StbTrueTypeSharpSettings settings)
		{
			_settings = settings;
		}

		/// <inheritdoc/>
		public IFontSource Load(byte[] data)
		{
			return new StbTrueTypeSharpSource(data, _settings);
		}
	}
}
