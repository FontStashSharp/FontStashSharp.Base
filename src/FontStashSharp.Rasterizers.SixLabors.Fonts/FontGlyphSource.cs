using System;
using System.Collections.Generic;
using System.Numerics;
using FontStashSharp.Samples.SixLabors;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace TrippyGL.Fonts.Building
{
	/// <summary>
	/// An implementation that sources glyphs from a SixLabors font instance.
	/// </summary>
	internal sealed class FontGlyphSource
	{
		private const float Dpi = 96;
		private const float PointsPerInch = 72;

		/// <summary>
		/// The font instance from which this source gets glyph data.
		/// </summary>
		public readonly IFontInstance FontInstance;

		/// <summary>
		/// Configuration for how glyphs should be rendered.
		/// </summary>
		public DrawingOptions DrawingOptions;

		/// <summary>
		/// Whether to include kerning if present in the font. Default is true.
		/// </summary>
		public bool IncludeKerningIfPresent = true;

		/// <summary>
		/// Initializes a new instance of FontGlyphSource.
		/// </summary>
		/// <param name="fontInstance">The font instance to use for glyph rendering</param>
		public FontGlyphSource(IFontInstance fontInstance)
		{
			FontInstance = fontInstance ?? throw new ArgumentNullException(nameof(fontInstance));

			DrawingOptions = new DrawingOptions
			{
				ShapeOptions = { IntersectionRule = IntersectionRule.Nonzero },
			};
		}

		/// <summary>
		/// Creates the path collection for a glyph with its size, bounds, and colors.
		/// </summary>
		/// <param name="size">The font size</param>
		/// <param name="codepoint">The Unicode codepoint of the glyph</param>
		/// <returns>A glyph path with rendering information, or null if empty</returns>
		public GlyphPath CreatePath(float size, int codepoint)
		{
			ColorGlyphRenderer glyphRenderer = new ColorGlyphRenderer();
			glyphRenderer.Reset();
			GlyphInstance glyphInstance = FontInstance.GetGlyph(codepoint);
			var pointSize = size * PointsPerInch / Dpi;
			glyphInstance.RenderTo(glyphRenderer, pointSize, new Vector2(0, 0), new Vector2(Dpi, Dpi), 0);
			IPathCollection p = glyphRenderer.Paths;
			RectangleF bounds = p.Bounds;

			var area = bounds.Width * bounds.Height;
			if (area == 0)
			{
				return null;
			}

			if (char.IsWhiteSpace((char)codepoint))
			{
				p = null;
			}

			return new GlyphPath
			{
				Size = size,
				Codepoint = codepoint,
				Bounds = new Rectangle((int)bounds.X, (int)bounds.Y, (int)Math.Ceiling(bounds.Width), (int)Math.Ceiling(bounds.Height)),
				Paths = p
			};
		}

		/// <summary>
		/// Gets the advance width for a glyph at the specified size.
		/// </summary>
		/// <param name="size">The font size</param>
		/// <param name="codepoint">The Unicode codepoint</param>
		/// <returns>The advance width at the specified size</returns>
		public float GetAdvance(float size, int codepoint)
		{
			GlyphInstance inst = FontInstance.GetGlyph(codepoint);
			return inst.AdvanceWidth * size / FontInstance.EmSize;
		}

		/// <summary>
		/// Gets the kerning offset between two glyphs.
		/// </summary>
		/// <param name="size">The font size</param>
		/// <param name="codepoint1">The first Unicode codepoint</param>
		/// <param name="codepoint2">The second Unicode codepoint</param>
		/// <returns>The kerning offset as a vector</returns>
		public Vector2 GetKerning(float size, int codepoint1, int codepoint2)
		{
			GlyphInstance aInstance = FontInstance.GetGlyph(codepoint1);
			Vector2 offset = FontInstance.GetOffset(FontInstance.GetGlyph(codepoint2), aInstance);
			return offset * size / FontInstance.EmSize;
		}

		/// <summary>
		/// Draws a glyph path to an image at the specified location.
		/// </summary>
		/// <param name="glyphPath">The glyph path to draw</param>
		/// <param name="location">The location in the image to draw at</param>
		/// <param name="image">The image to draw to</param>
		public void DrawGlyphToImage(GlyphPath glyphPath, System.Drawing.Point location, Image<Rgba32> image)
		{
			var paths = glyphPath.Paths;
			if (paths == null)
			{
				return;
			}

			paths = paths.Translate(location.X - glyphPath.Bounds.X, location.Y - glyphPath.Bounds.Y);
			DrawColoredPaths(image, paths);
		}

		/// <summary>
		/// Draws a collection of paths with their colors onto the image.
		/// </summary>
		/// <param name="image">The image to draw to</param>
		/// <param name="paths">The paths to draw</param>
		private void DrawColoredPaths(Image<Rgba32> image, IPathCollection paths)
		{
			IEnumerator<IPath> pathEnumerator = paths.GetEnumerator();

			int i = 0;
			while (pathEnumerator.MoveNext())
			{
				IPath path = pathEnumerator.Current;
				image.Mutate(x => x.Fill(DrawingOptions, Color.White, path));
				i++;
			}
		}
	}
}