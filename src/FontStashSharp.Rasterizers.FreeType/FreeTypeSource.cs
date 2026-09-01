using FontStashSharp.Interfaces;
using FreeTypeSharp;
using System;
using System.Runtime.InteropServices;

namespace FontStashSharp.Rasterizers.FreeType
{
	internal unsafe class FreeTypeSource : IFontSource
	{
		private static FT_LibraryRec_* _libraryHandle;
		private GCHandle _memoryHandle;
		private FT_FaceRec_* _faceHandle;

		/// <summary>
		/// Initializes a new instance of FreeTypeSource from font file data
		/// </summary>
		/// <param name="data">The font file data (TTF/OTF)</param>
		/// <exception cref="FreeTypeException">Thrown when the FreeType library or the font face could not be initialized</exception>
		public FreeTypeSource(byte[] data)
		{
			FT_Error err;
			if (_libraryHandle == default)
			{
				FT_LibraryRec_* libraryRef;
				err = FT.FT_Init_FreeType(&libraryRef);

				if (err != FT_Error.FT_Err_Ok)
					throw new FreeTypeException(err);

				_libraryHandle = libraryRef;
			}

			_memoryHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

			FT_FaceRec_* faceRef;
			err = FT.FT_New_Memory_Face(_libraryHandle, (byte*)_memoryHandle.AddrOfPinnedObject(), (IntPtr)data.Length, IntPtr.Zero, &faceRef);

			if (err != FT_Error.FT_Err_Ok)
				throw new FreeTypeException(err);

			_faceHandle = faceRef;
		}

		/// <summary>
		/// Finalizes an instance of the FreeTypeSource class
		/// </summary>
		~FreeTypeSource()
		{
			Dispose(false);
		}

		/// <summary>
		/// Releases unmanaged resources held by the font source
		/// </summary>
		/// <param name="disposing">Whether managed resources should also be released</param>
		protected virtual void Dispose(bool disposing)
		{
			if (_faceHandle != default)
			{
				FT.FT_Done_Face(_faceHandle);
				_faceHandle = default;
			}

			if (_memoryHandle.IsAllocated)
				_memoryHandle.Free();
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <inheritdoc/>
		public int? GetGlyphId(int codepoint)
		{
			var result = FT.FT_Get_Char_Index(_faceHandle, (UIntPtr)codepoint);
			if (result == 0)
			{
				return null;
			}

			return (int?)result;
		}

		/// <inheritdoc/>
		public int GetGlyphKernAdvance(int previousGlyphId, int glyphId, float fontSize)
		{
			FT_Vector_ kerning;
			if (FT.FT_Get_Kerning(_faceHandle, (uint)previousGlyphId, (uint)glyphId, FT_Kerning_Mode_.FT_KERNING_DEFAULT, &kerning) != FT_Error.FT_Err_Ok)
			{
				return 0;
			}

			return (int)kerning.x >> 6;
		}

		/// <summary>
		/// Sets the pixel sizes (resolution) of the face in 26.6 fixed-point units
		/// </summary>
		/// <param name="width">The pixel width</param>
		/// <param name="height">The pixel height</param>
		private void SetPixelSizes(float width, float height)
		{
			var err = FT.FT_Set_Pixel_Sizes(_faceHandle, (uint)width, (uint)height);
			if (err != FT_Error.FT_Err_Ok)
				throw new FreeTypeException(err);
		}

		/// <summary>
		/// Loads the specified glyph into the face's glyph slot
		/// </summary>
		/// <param name="glyphId">The glyph id</param>
		private void LoadGlyph(int glyphId)
		{
			var err = FT.FT_Load_Glyph(_faceHandle, (uint)glyphId, FT_LOAD.FT_LOAD_DEFAULT | FT_LOAD.FT_LOAD_COLOR);
			if (err != FT_Error.FT_Err_Ok)
				throw new FreeTypeException(err);
		}

		/// <summary>
		/// Gets the currently loaded glyph slot instance
		/// </summary>
		/// <param name="glyph">The glyph slot structure</param>
		private void GetCurrentGlyph(out FT_GlyphSlotRec_ glyph)
		{
			glyph = Marshal.PtrToStructure<FT_GlyphSlotRec_>((IntPtr)_faceHandle->glyph);
		}

		/// <inheritdoc/>
		public void GetGlyphMetrics(int glyphId, float fontSize, out int advance, out int x0, out int y0, out int x1, out int y1)
		{
			SetPixelSizes(0, fontSize);
			LoadGlyph(glyphId);

			FT_GlyphSlotRec_ glyph;
			GetCurrentGlyph(out glyph);
			advance = (int)glyph.advance.x >> 6;
			x0 = (int)glyph.metrics.horiBearingX >> 6;
			y0 = -(int)glyph.metrics.horiBearingY >> 6;
			x1 = x0 + ((int)glyph.metrics.width >> 6);
			y1 = y0 + ((int)glyph.metrics.height >> 6);
		}

		/// <inheritdoc/>
		public void GetMetricsForSize(float fontSize, out int ascent, out int descent, out int lineHeight)
		{
			SetPixelSizes(0, fontSize);
			var sizeRec = _faceHandle->size;

			ascent = (int)sizeRec->metrics.ascender >> 6;
			descent = (int)sizeRec->metrics.descender >> 6;
			lineHeight = (int)sizeRec->metrics.height >> 6;
		}

		/// <inheritdoc/>
		public void RasterizeGlyphBitmap(int glyphId, float fontSize, byte[] buffer, int startIndex, int outWidth, int outHeight, int outStride)
		{
			SetPixelSizes(0, fontSize);
			LoadGlyph(glyphId);

			FT.FT_Render_Glyph(_faceHandle->glyph, FT_Render_Mode_.FT_RENDER_MODE_NORMAL);

			FT_GlyphSlotRec_ glyph;
			GetCurrentGlyph(out glyph);
			var ftbmp = glyph.bitmap;

			fixed (byte* bptr = buffer)
			{
				for (var y = 0; y < outHeight; ++y)
				{
					var pos = (y * outStride) + startIndex;

					byte* dst = bptr + pos;
					byte* src = ftbmp.buffer + y * ftbmp.pitch;

					if (ftbmp.pixel_mode == FT_Pixel_Mode_.FT_PIXEL_MODE_GRAY)
					{
						for (var x = 0; x < outWidth; ++x)
						{
							*dst++ = *src++;
						}
					}
					else if (ftbmp.pixel_mode == FT_Pixel_Mode_.FT_PIXEL_MODE_MONO)
					{
						for (var x = 0; x < outWidth; x += 8)
						{
							var bits = *src++;
							for (int b = 0; b < Math.Min(8, ftbmp.width - x); b++)
							{
								var color = ((bits >> (7 - b)) & 1) == 0 ? 0 : 255;
								*dst++ = (byte)color;
							}
						}
					}
				}
			}
		}

		/// <inheritdoc/>
		public void RasterizeGlyphSDF(int glyphId, float fontSize, byte[] buffer, int startIndex, int padding, byte onedge_value, float pixel_dist_scale)
		{
			throw new NotImplementedException("FreeTypeSource doesn't support SDF.");
		}

		/// <inheritdoc/>
		public float CalculateScaleForTextShaper(float fontSize)
		{
			return fontSize / (float)_faceHandle->units_per_EM;
		}

	}
}
