namespace FontStashSharp.Rasterizers.StbTrueTypeSharp
{
	/// <summary>
	/// Configuration settings for StbTrueTypeSharp font rendering
	/// </summary>
	public struct StbTrueTypeSharpSettings
	{
		/// <summary>
		/// Kernel width for glyph bitmap filtering (default: 0)
		/// </summary>
		public int KernelWidth;

		/// <summary>
		/// Kernel height for glyph bitmap filtering (default: 0)
		/// </summary>
		public int KernelHeight;

		/// <summary>
		/// Whether to use the old rasterizer algorithm (default: false)
		/// </summary>
		public bool UseOldRasterizer;

		/// <summary>
		/// Whether to use EM to pixels scale instead of pixel height scale (default: false)
		/// </summary>
		public bool UseEmToPixelsScale;
	}
}
