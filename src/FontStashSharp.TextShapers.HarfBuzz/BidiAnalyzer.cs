using HarfBuzzSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace FontStashSharp
{
	/// <summary>
	/// Direction of text flow
	/// </summary>
	public enum TextDirection
	{
		/// <summary>
		/// Left-to-right text (Latin, Cyrillic, etc.)
		/// </summary>
		LTR,

		/// <summary>
		/// Right-to-left text (Arabic, Hebrew, etc.)
		/// </summary>
		RTL
	}

	/// <summary>
	/// Represents a segment of text with uniform directionality
	/// </summary>
	internal struct DirectionalRun
	{
		/// <summary>
		/// Start position in the original text
		/// </summary>
		public int Start;

		/// <summary>
		/// Length of the run
		/// </summary>
		public int Length;

		/// <summary>
		/// Direction of this run
		/// </summary>
		public TextDirection Direction;
	}

	/// <summary>
	/// A simple bidirectional text analyzer for mixed LTR/RTL text
	/// </summary>
	internal static class BiDiAnalyzer
	{
		private static readonly ConcurrentDictionary<int, UnicodeCategory> s_supplementaryCategoryCache = new ConcurrentDictionary<int, UnicodeCategory>();

		// Common bracket pairs from BidiBrackets.txt (subset).
		private static readonly Dictionary<int, int> s_openingToClosing = new Dictionary<int, int>
		{
			{ '(', ')' }, { '[', ']' }, { '{', '}' },
			{ 0x2329, 0x232A },   // 〈 〉
			{ 0x3008, 0x3009 },   // 〈 〉
			{ 0x300A, 0x300B },   // 《 》
			{ 0x300C, 0x300D },   // 「 」
			{ 0x300E, 0x300F },   // 『 』
			{ 0x3010, 0x3011 },   // 【 】
			{ 0x3014, 0x3015 },   // 〔 〕
			{ 0x3016, 0x3017 },   // 〖 〗
			{ 0x3018, 0x3019 },   // 〘 〙
			{ 0x301A, 0x301B },   // 〚 〛
			{ 0xFE59, 0xFE5A },   // ﹙ ﹚
			{ 0xFE5B, 0xFE5C },   // ﹛ ﹜
			{ 0xFE5D, 0xFE5E },   // ﹝ ﹞
			{ 0xFF08, 0xFF09 },   // （ ）
			{ 0xFF3B, 0xFF3D },   // ［ ］
			{ 0xFF5B, 0xFF5D },   // ｛ ｝
			{ 0xFF5F, 0xFF60 },   // ｟ ｠
			{ 0xFF62, 0xFF63 },   // ｢ ｣
		};

		private static readonly Dictionary<int, int> s_closingToOpening = BuildInverse(s_openingToClosing);

		private static Dictionary<int, int> BuildInverse(Dictionary<int, int> src)
		{
			var inv = new Dictionary<int, int>(src.Count);
			foreach (var kv in src)
				inv[kv.Value] = kv.Key;
			return inv;
		}

		public static TextDirection DetectParagraphDirection(string text)
		{
			if (string.IsNullOrEmpty(text))
				return TextDirection.LTR;

			for (int i = 0; i < text.Length; i++)
			{
				int cp;

				if (char.IsHighSurrogate(text[i]) &&
					i + 1 < text.Length &&
					char.IsLowSurrogate(text[i + 1]))
				{
					cp = char.ConvertToUtf32(text[i], text[i + 1]);
					i++;
				}
				else
				{
					cp = text[i];
				}

				TextDirection? direction = GetStrongDirection(cp);
				if (direction.HasValue)
					return direction.Value;
			}

			return TextDirection.LTR;
		}
		/// <summary>
		/// Segments text into runs of consistent directionality
		/// </summary>
		public static List<DirectionalRun> SegmentIntoDirectionalRuns(string text, TextDirection? baseDirection = null)
		{
			var runs = new List<DirectionalRun>();
			if (string.IsNullOrEmpty(text))
				return runs;

			// ---- Pass 1: split into codepoints and classify each one. ------
			var segStarts = new List<int>(text.Length);
			var segCodepoints = new List<int>(text.Length);
			var segDirs = new List<TextDirection?>(text.Length);
			var segIsMark = new List<bool>(text.Length);

			int i = 0;
			while (i < text.Length)
			{
				int codepoint;
				int charCount;

				if (i + 1 < text.Length && char.IsSurrogatePair(text, i))
				{
					codepoint = char.ConvertToUtf32(text, i);
					charCount = 2;
				}
				else
				{
					codepoint = text[i];
					charCount = 1;
				}

				UnicodeCategory cat = GetCategory(codepoint);

				segStarts.Add(i);
				segCodepoints.Add(codepoint);
				segDirs.Add(GetStrongDirection(codepoint));
				segIsMark.Add(cat == UnicodeCategory.NonSpacingMark || cat == UnicodeCategory.EnclosingMark);
				i += charCount;
			}

			int count = segStarts.Count;
			var resolved = new TextDirection?[count];
			for (int s = 0; s < count; s++)
				resolved[s] = segDirs[s];

			TextDirection paraDir = baseDirection ?? DetectParagraphDirection(text);

			// ---- Pass 2: W1 — nonspacing marks inherit the preceding dir. --
			for (int s = 1; s < count; s++)
			{
				if (segIsMark[s] && !resolved[s].HasValue && resolved[s - 1].HasValue)
					resolved[s] = resolved[s - 1];
			}

			// ---- Pass 3: N0 — pair brackets with the enclosed strong dir. --
			ResolveBracketPairs(segCodepoints, resolved, paraDir);

			// ---- Pass 4: N1/N2 — resolve remaining neutral spans. ----------
			for (int s = 0; s < count; s++)
			{
				if (resolved[s].HasValue)
					continue;

				int spanStart = s;
				int spanEnd = s;
				while (spanEnd + 1 < count && !resolved[spanEnd + 1].HasValue)
					spanEnd++;

				TextDirection prevDir = spanStart > 0 ? resolved[spanStart - 1].Value : paraDir;
				TextDirection nextDir = spanEnd < count - 1 ? resolved[spanEnd + 1].Value : paraDir;
				TextDirection neutralDir = prevDir == nextDir ? prevDir : paraDir;

				for (int k = spanStart; k <= spanEnd; k++)
					resolved[k] = neutralDir;

				s = spanEnd;
			}

			// ---- Pass 5: merge adjacent segments with the same direction. --
			int curStart = 0;
			TextDirection curDir = resolved[0].Value;
			for (int s = 1; s < count; s++)
			{
				if (resolved[s].Value != curDir)
				{
					int startChar = segStarts[curStart];
					int endChar = segStarts[s];
					runs.Add(new DirectionalRun
					{
						Start = startChar,
						Length = endChar - startChar,
						Direction = curDir
					});
					curStart = s;
					curDir = resolved[s].Value;
				}
			}

			runs.Add(new DirectionalRun
			{
				Start = segStarts[curStart],
				Length = text.Length - segStarts[curStart],
				Direction = curDir
			});

			return runs;
		}

		/// <summary>
		/// Checks if a character is right-to-left
		/// </summary>
		/// <remarks>https://www.ssec.wisc.edu/~tomw/java/unicode.html</remarks>
		private static void ResolveBracketPairs(List<int> codepoints, TextDirection?[] resolved, TextDirection paraDir)
		{
			var stack = new Stack<int>();

			for (int s = 0; s < codepoints.Count; s++)
			{
				int cp = codepoints[s];

				if (s_openingToClosing.ContainsKey(cp))
				{
					stack.Push(s);
					continue;
				}

				if (!s_closingToOpening.ContainsKey(cp))
					continue;

				if (stack.Count == 0)
					continue;

				int open = stack.Peek();
				if (s_openingToClosing[codepoints[open]] != cp)
					continue; // mismatched close — ignore (N0 says unmatched)

				stack.Pop();

				bool hasR = false;
				bool hasL = false;
				for (int j = open + 1; j < s; j++)
				{
					if (!resolved[j].HasValue)
						continue;

					if (resolved[j].Value == TextDirection.RTL)
						hasR = true;
					else
						hasL = true;
				}

				TextDirection? contentDir = null;
				if (hasR && hasL) contentDir = paraDir;
				else if (hasR) contentDir = TextDirection.RTL;
				else if (hasL) contentDir = TextDirection.LTR;

				if (contentDir.HasValue)
				{
					resolved[open] = contentDir;
					resolved[s] = contentDir;
				}
			}
		}

		private static TextDirection? GetStrongDirection(int codePoint)
		{
			UnicodeCategory category = GetCategory(codePoint);

			// NSM and Me never introduce strong direction (W1 handles inheritance)
			if (category == UnicodeCategory.NonSpacingMark ||
				category == UnicodeCategory.EnclosingMark)
			{
				return null;
			}

			// SpacingCombiningMark (Mc) is NOT strong — treat as neutral
			if (category == UnicodeCategory.SpacingCombiningMark)
			{
				return null;
			}

			// Strong letters (L, R, AL)
			bool isLetter =
				category == UnicodeCategory.UppercaseLetter ||
				category == UnicodeCategory.LowercaseLetter ||
				category == UnicodeCategory.TitlecaseLetter ||
				category == UnicodeCategory.ModifierLetter ||
				category == UnicodeCategory.OtherLetter;

			if (isLetter)
			{
				var dir = UnicodeFunctions.Default
					.GetScript(codePoint)
					.HorizontalDirection;

				return dir == Direction.RightToLeft
					? TextDirection.RTL
					: TextDirection.LTR;
			}

			// Digits → weak (EN/AN) → neutral here
			if (category == UnicodeCategory.DecimalDigitNumber)
				return TextDirection.LTR;

			// Everything else → neutral
			return null;
		}

		private static UnicodeCategory GetCategory(int codePoint)
		{
			if (codePoint < 0 || codePoint > 0x10FFFF ||
				(codePoint >= 0xD800 && codePoint <= 0xDFFF))
			{
				throw new ArgumentOutOfRangeException(nameof(codePoint));
			}

			if (codePoint <= 0xFFFF)
				return CharUnicodeInfo.GetUnicodeCategory((char)codePoint);

			return s_supplementaryCategoryCache.GetOrAdd(codePoint, cp => CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(cp), 0));
		}
	}
}
