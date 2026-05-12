using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

namespace FontStashSharp.Samples.SixLabors
{
	/// <summary>
	/// Represents a glyph path with metrics and rendering information
	/// </summary>
	internal class GlyphPath
	{
		/// <summary>
		/// The font size used for rendering the glyph
		/// </summary>
		public float Size;

		/// <summary>
		/// The Unicode codepoint of the glyph
		/// </summary>
		public int Codepoint;

		/// <summary>
		/// The bounding rectangle of the glyph
		/// </summary>
		public Rectangle Bounds;

		/// <summary>
		/// The path collection representing the glyph outline
		/// </summary>
		public IPathCollection Paths;
	}
}
