using FontStashSharp.Interfaces;
using System;
using Xunit;

namespace FontStashSharp.Tests
{
	public class HarfBuzzTests
	{
		private class ShaperWrapper : ITextShapingInfoProvider
		{
			private readonly HarfBuzzTextShaper _shaper;

			public ShaperWrapper()
			{
				_shaper = new HarfBuzzTextShaper();

				var assembly = typeof(HarfBuzzTests).Assembly;

				var fontId = _shaper.RegisterTtfFont(assembly.ReadResourceAsBytes("Resources.DroidSans.ttf"));
			}

			public float CalculateScale(int fontSourceId, float fontSize) => 1.0f;

			public int? GetFontSourceId(int codepoint) => 0;

			public int GetTextShaperFontId(int fontSourceId) => 0;

			public ShapedText Shape(string text, float size) => _shaper.Shape(text, size, this);
		}

		[Theory]
		[InlineData("Hello World", 1, TextDirection.LTR)]
		[InlineData("مرحبا", 1, TextDirection.RTL)]
		[InlineData("", 0, TextDirection.LTR)]
		[InlineData("123", 1, TextDirection.LTR)]
		public void BiDiAnalyzer_SingleDirectionText(string text, int expectedRunCount, TextDirection expectedDirection)
		{
			var runs = BiDiAnalyzer.SegmentIntoDirectionalRuns(text);

			Assert.Equal(expectedRunCount, runs.Count);

			if (expectedRunCount > 0)
			{
				Assert.Equal(expectedDirection, runs[0].Direction);
				Assert.Equal(0, runs[0].Start);
				Assert.Equal(text.Length, runs[0].Length);
			}
		}

		[Fact]
		public void BiDiAnalyzer_MixedLtrRtlText()
		{
			// English "Hello" + Arabic "مرحبا"
			var text = "Hello مرحبا";
			var runs = BiDiAnalyzer.SegmentIntoDirectionalRuns(text);

			// Should have 2 runs: LTR for "Hello " and RTL for "مرحبا"
			Assert.Equal(2, runs.Count);

			Assert.Equal(TextDirection.LTR, runs[0].Direction);
			Assert.Equal(0, runs[0].Start);
			Assert.Equal(TextDirection.RTL, runs[1].Direction);
		}

		[Fact]
		public void BiDiAnalyzer_LeadingNeutralCharacters()
		{
			// Spaces before text should be assigned to the following run's direction
			var text = "  Hello";
			var runs = BiDiAnalyzer.SegmentIntoDirectionalRuns(text);

			Assert.Single(runs);
			Assert.Equal(TextDirection.LTR, runs[0].Direction);
			Assert.Equal(0, runs[0].Start);
			Assert.Equal(text.Length, runs[0].Length);
		}

		[Fact]
		public void BiDiAnalyzer_OnlyNeutralCharacters()
		{
			// Text with only neutral characters should default to LTR
			var text = "   ...   ";
			var runs = BiDiAnalyzer.SegmentIntoDirectionalRuns(text);

			Assert.Single(runs);
			Assert.Equal(TextDirection.LTR, runs[0].Direction);
		}

		[Fact]
		public void TextShaper_EmptyString()
		{
			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape("", 32);

			Assert.NotNull(shaped);
			Assert.NotNull(shaped.Glyphs);
			Assert.Empty(shaped.Glyphs);
			Assert.Equal("", shaped.OriginalText);
		}

		[Fact]
		public void TextShaper_NullString()
		{
			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape(null, 32);

			Assert.NotNull(shaped);
			Assert.NotNull(shaped.Glyphs);
			Assert.Empty(shaped.Glyphs);
			Assert.Equal("", shaped.OriginalText);
		}

		[Fact]
		public void TextShaper_SimpleText()
		{
			// Create font system with BiDi disabled but text shaping enabled
			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape("Hello", 32);

			Assert.NotNull(shaped);
			Assert.NotNull(shaped.Glyphs);
			Assert.True(shaped.Glyphs.Length > 0);
			Assert.Equal("Hello", shaped.OriginalText);
			Assert.Equal(32, shaped.FontSize);

			// Each glyph should have valid advance values
			foreach (var glyph in shaped.Glyphs)
			{
				Assert.True(glyph.XAdvance > 0);
			}
		}

		[Fact]
		public void TextShaper_WithBiDiEnabled()
		{
			// Create font system with BiDi enabled
			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape("Test", 32);

			Assert.NotNull(shaped);
			Assert.NotNull(shaped.Glyphs);
			Assert.True(shaped.Glyphs.Length > 0);
			Assert.Equal("Test", shaped.OriginalText);
		}

		[Fact]
		public void TextShaper_SurrogatePair_FormsSingleCluster()
		{
			var text = "😀"; // U+1F600 (surrogate pair)
			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape(text, 32);

			Assert.True(shaped.Glyphs.Length <= 1, "Emoji surrogate pair should form a single cluster");
		}

		[Fact]
		public void TextShaper_EmojiZWJSequence()
		{
			// Family: 👨‍👩‍👧‍👦 (multiple codepoints joined by ZWJ)
			var text = "👨‍👩‍👧‍👦";
			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape(text, 32);

			Assert.True(shaped.Glyphs.Length < text.Length,
					"ZWJ sequences should combine into fewer glyphs");
		}

		[Fact]
		public void ShapedText_PreservesOriginalText()
		{
			var originalText = "Testing 123";

			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape(originalText, 32);

			Assert.Equal(originalText, shaped.OriginalText);
		}

		[Fact]
		public void ShapedGlyphs_HaveValidClusterIndices()
		{
			var text = "Hello";

			var shaper = new ShaperWrapper();
			var shaped = shaper.Shape(text, 32);

			// All cluster indices should be within the text length
			foreach (var glyph in shaped.Glyphs)
			{
				Assert.True(glyph.Cluster >= 0);
				Assert.True(glyph.Cluster < text.Length);
			}
		}
	}
}
