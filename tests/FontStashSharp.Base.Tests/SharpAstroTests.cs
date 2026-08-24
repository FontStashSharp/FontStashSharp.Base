using System;
using Xunit;

#if NET10_0_OR_GREATER
using FontStashSharp.Interfaces;
using FontStashSharp.Rasterizers.SharpAstro;

namespace FontStashSharp.Tests
{
	public class SharpAstroTests
	{
		private const float UnitsPerEm = 2048f;

		private static IFontSource CreateFontSource()
		{
			var assembly = typeof(SharpAstroTests).Assembly;

			return new SharpAstroLoader().Load(assembly.ReadResourceAsBytes("Resources.DroidSans.ttf"));
		}

		[Fact]
		public void GetMetricsForSizeWorks()
		{
			using (var source = CreateFontSource())
			{
				source.GetMetricsForSize(32f, out var ascent, out var descent, out var lineHeight);

				Assert.Equal(30, ascent);
				Assert.Equal(-8, descent);
				Assert.Equal(37, lineHeight);
			}
		}

		[Fact]
		public void GetGlyphIdWorks()
		{
			using (var source = CreateFontSource())
			{
				Assert.Equal(36, source.GetGlyphId('A'));
				Assert.Null(source.GetGlyphId(0x4E2D));
			}
		}

		[Fact]
		public void GetGlyphMetricsMatchesRasterizedBitmap()
		{
			using (var source = CreateFontSource())
			{
				var glyphId = source.GetGlyphId('A').Value;
				source.GetGlyphMetrics(glyphId, 32f, out var advance, out var x0, out var y0, out var x1, out var y1);

				Assert.Equal(19, advance);
				var width = x1 - x0;
				var height = y1 - y0;
				Assert.True(width > 0);
				Assert.True(height > 0);
				Assert.True(y0 < 0);

				var buffer = new byte[width * height];
				source.RasterizeGlyphBitmap(glyphId, 32f, buffer, 0, width, height, width);

				Assert.Contains(buffer, b => b > 0);
				Assert.All(buffer, b => Assert.InRange(b, (byte)0, (byte)255));
			}
		}

		[Fact]
		public void RasterizeGlyphBitmapRespectsStride()
		{
			using (var source = CreateFontSource())
			{
				var glyphId = source.GetGlyphId('l').Value;
				source.GetGlyphMetrics(glyphId, 64f, out _, out var x0, out var y0, out var x1, out var y1);

				var width = x1 - x0;
				var height = y1 - y0;
				var stride = width + 7;
				var buffer = new byte[stride * height];
				source.RasterizeGlyphBitmap(glyphId, 64f, buffer, 0, width, height, stride);

				for (var y = 0; y < height; ++y)
				{
					for (var x = width; x < stride; ++x)
					{
						Assert.Equal(0, buffer[y * stride + x]);
					}
				}
			}
		}

		[Fact]
		public void SpaceHasZeroSizeBitmapAndPositiveAdvance()
		{
			using (var source = CreateFontSource())
			{
				var glyphId = source.GetGlyphId(' ').Value;
				source.GetGlyphMetrics(glyphId, 32f, out var advance, out var x0, out var y0, out var x1, out var y1);

				Assert.True(advance > 0);
				Assert.Equal(0, x1 - x0);
				Assert.Equal(0, y1 - y0);
			}
		}

		[Fact]
		public void GetGlyphKernAdvanceWorks()
		{
			using (var source = CreateFontSource())
			{
				var glyphA = source.GetGlyphId('A').Value;
				var glyphV = source.GetGlyphId('V').Value;

				Assert.True(source.GetGlyphKernAdvance(glyphA, glyphV, 128f) < 0);
				Assert.Equal(0, source.GetGlyphKernAdvance(glyphA, glyphA, 128f));
			}
		}

		[Fact]
		public void CalculateScaleForTextShaperWorks()
		{
			using (var source = CreateFontSource())
			{
				Assert.Equal(32f / UnitsPerEm, source.CalculateScaleForTextShaper(32f));
			}
		}
	}
}
#endif
