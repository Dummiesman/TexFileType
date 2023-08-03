// Copyright (c) 2023 Dummiesman
// Licensed under the MIT license. See LICENSE.txt

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using libtex.DXT;
using PaintDotNet;

namespace libtex
{
    public class Texture
    {
        //
        public TextureFlags Flags { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public readonly List<Color> Palette = new List<Color>();
        public TextureFormat Format { get; private set; }
        public int MipmapCount => mipmaps.Count;
        private readonly List<byte[]> mipmaps = new List<byte[]>();

        /// <summary>
        /// Alpha ref used for 1555 mode SetPixel
        /// </summary>
        public static int AlphaRef = 128;

        /// <summary>
        /// Remove palette alphas if they exist, and the format is not alpha
        /// </summary>
        public static bool RemovePaletteAlphas = true;

        /// <summary>
        /// Mip divisors, used for math
        /// </summary>
        private static int[] MipDivisors = new int[] { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536};

        /// <summary>
        /// Get the pixel stride. Will return negative for division
        /// </summary>
        /// <returns></returns>
        public int GetStride()
        {
            switch (Format)
            {
                case TextureFormat.P4:
                case TextureFormat.PA4:
                    return -2;
                case TextureFormat.A1R5G5B5:
                case TextureFormat.P8A8:
                case TextureFormat.A8I8:
                    return 2;
                case TextureFormat.PA8:
                case TextureFormat.P8:
                case TextureFormat.A8:
                case TextureFormat.I8:
                case TextureFormat.A4I4:
                    return 1;
                case TextureFormat.RGB888:
                    return 3;
                case TextureFormat.RGB8888:
                    return 4;
                case TextureFormat.DXT1:
                    return -2;
                case TextureFormat.DXT3:
                case TextureFormat.DXT5:
                    return 1;

            }

            //this should never happen
            throw new Exception("A wild texture format has appeared");
        }

        public static bool FormatSupportsAlpha(TextureFormat format)
        {
            return format == TextureFormat.A8 || format == TextureFormat.PA8 || format == TextureFormat.PA4 
                || format == TextureFormat.RGB8888 || format == TextureFormat.P8A8 || format == TextureFormat.A1R5G5B5
                || format == TextureFormat.A4I4 || format == TextureFormat.A8I8 || format == TextureFormat.DXT5 || format == TextureFormat.DXT3;
        }

        public static bool FormatIsCompressed(TextureFormat format)
        {
            return format == TextureFormat.DXT1 || format == TextureFormat.DXT5 || format == TextureFormat.DXT3;
        }

        public static bool FormatIsPaletted(TextureFormat format)
        {
            return format == TextureFormat.P4 || format == TextureFormat.PA4
                || format == TextureFormat.P8 || format == TextureFormat.PA8
                || format == TextureFormat.P8A8;
        }

        public static int FormatPaletteSize(TextureFormat format)
        {
            switch(format)
            {
                case TextureFormat.PA4:
                case TextureFormat.P4:
                    return 16;
                case TextureFormat.PA8:
                case TextureFormat.P8:
                case TextureFormat.P8A8:
                    return 256;
                default:
                    return 0;
            }
        }

        public static int CalculateMaxMipmaps(int width, int height)
        {
            int count = 1;
            while(width > 0 && height > 0 && (width % 2) == 0 && (height % 2) == 0)
            {
                count++;
                width /= 2;
                height /= 2;
            }
            return count;
        }

        private static int RemapColorRange(int value, int maximum)
        {
            return (int)((value / 255f) * maximum);
        }

        public Size CalculateMipSize(int mipIndex)
        {
            if (mipIndex < 0)
                throw new ArgumentException("mipIndex must not be < 0");

            return new Size(Width >> mipIndex, Height >> mipIndex);
        }

        private int CalculateMipArraySize(int mipIndex)
        {
            var size = CalculateMipSize(mipIndex);
            if(FormatIsCompressed(Format))
            {
                int blockSize = (Format == TextureFormat.DXT1) ? 8 : 16;
                int bcw = (size.Width + 3) / 4;
                int bch = (size.Height + 3) / 4;
                return bcw * bch * blockSize;
            }
            else
            {
                int stride = GetStride();
                int pixelCount = (size.Width * size.Height);

                if (stride < 0)
                {
                    if ((pixelCount % 2) > 0)
                        pixelCount++;
                    return pixelCount / -stride;
                }
                else
                {
                    return pixelCount * stride;
                }
            }
        }

        /// <summary>
        /// Compress in-place the data in this texture. compressFormat must refer to a compressed format type.
        /// </summary>
        public void Compress(TextureFormat compressFormat)
        {
            if (FormatIsCompressed(Format))
                throw new InvalidOperationException("Texture is already compressed.");
            if (!FormatIsCompressed(compressFormat))
                throw new ArgumentException("compressFormat must be a compressed format.");
            if (Format != TextureFormat.RGB8888)
                throw new InvalidOperationException($"Sorry, only RGB8888 can be compressed at this time.");

            var oldFormat = Format;
            Format = compressFormat;
            
            for (int i = 0; i < MipmapCount; i++)
            {
                var mipSize = CalculateMipSize(i);

                if (compressFormat == TextureFormat.DXT1)
                {
                    mipmaps[i] = DXTEncoding.CompressDXT1(mipSize.Width, mipSize.Height, mipmaps[i], StbDxtSharp.CompressionMode.HighQuality);
                }
                else if(compressFormat == TextureFormat.DXT5)
                {
                    mipmaps[i] = DXTEncoding.CompressDXT5(mipSize.Width, mipSize.Height, mipmaps[i], StbDxtSharp.CompressionMode.HighQuality);
                }
                else
                {
                    throw new NotImplementedException("DXT3 compression is not currently implemented.");
                }
            }

        }

        /// <summary>
        /// Decompress in-place the data in this texture
        /// </summary>
        public void Decompress()
        {
            if (!FormatIsCompressed(Format))
                throw new InvalidOperationException("Texture is not compressed.");

            // Change over to a 32 bit uncompressed
            var oldFormat = Format;
            Format = TextureFormat.RGB8888;

            for(int i=0; i < MipmapCount; i++)
            {
                var mipSize = CalculateMipSize(i);
                byte[] newData = new byte[CalculateMipArraySize(i)];

                if(oldFormat == TextureFormat.DXT1)
                {
                    DXTEncoding.DecompressDXT1(mipmaps[i], mipSize.Width, mipSize.Height, newData);
                }
                else if(oldFormat == TextureFormat.DXT3)
                {
                    DXTEncoding.DecompressDXT3(mipmaps[i], mipSize.Width, mipSize.Height, newData);
                }
                else
                {
                    DXTEncoding.DecompressDXT5(mipmaps[i], mipSize.Width, mipSize.Height, newData);
                }
                mipmaps[i] = newData;
            }
        }

        public Bitmap ConvertToBitmap(int mipmap, bool useAlphaChannel)
        {
            //create bitmap
            PixelFormat pixelFormat;
            if (FormatSupportsAlpha(Format))
            {
                pixelFormat = PixelFormat.Format32bppArgb;
            }
            else
            {
                pixelFormat = PixelFormat.Format24bppRgb;
            }

            Texture convertTexture = this;
            if(FormatIsCompressed(Format))
            {
                convertTexture = this.Clone();
                convertTexture.Decompress();
            }

            var mipSize = convertTexture.CalculateMipSize(mipmap);
            var db = new Bitmap(mipSize.Width, mipSize.Height, pixelFormat);
            for (int y = 0; y < mipSize.Height; y++)
            {
                for (int x = 0; x < mipSize.Width; x++)
                {
                    var pixel = convertTexture.GetPixel(x, y, mipmap);
                    if (!useAlphaChannel)
                        pixel = Color.FromArgb(255, pixel);
                    db.SetPixel(x, y, pixel);
                }
            }
            return db;
        }

        public Bitmap ConvertToBitmap(int mipmap)
        {
            return ConvertToBitmap(mipmap, true);
        }

        public Bitmap ConvertToBitmap()
        {
            return ConvertToBitmap(0);            
        }

        /// <summary>
        /// Gets a pixel color from the top level mipmap
        /// </summary>
        /// <param name="x">The X location of the pixel. Must be scaled for mipmaps.</param>
        /// <param name="y">The Y location of the pixel. Must be scaled for mipmaps.</param>
        /// <param name="mipmapIndex">The mipmap index to get the pixel from</param>
        public Color GetPixel(int x, int y)
        {
            return GetPixel(x, y, 0);
        }


        /// <summary>
        /// Sets a pixel color on the top level mipmap
        /// If the image type is paletted, it will try to find the color in the palette. If it's not present, palette index 0xFF is used.
        /// </summary>
        /// <param name="x">The X location of the pixel. Must be scaled for mipmaps.</param>
        /// <param name="y">The Y location of the pixel. Must be scaled for mipmaps.</param>
        public void SetPixel(int x, int y, Color color)
        {
            SetPixel(x, y, color, 0);
        }

        /// <summary>
        /// Sets a pixel color on the specified mipmap level
        /// If the image type is paletted, it will try to find the color in the palette. If it's not present, palette index 0xFF is used.
        /// </summary>
        /// <param name="x">The X location of the pixel. Must be scaled for mipmaps.</param>
        /// <param name="y">The Y location of the pixel. Must be scaled for mipmaps.</param>
        /// <param name="mipmapIndex">The mipmap index to get the pixel from</param>
        public void SetPixel(int x, int y, Color color, int mipmapIndex)
        {
            if (FormatIsCompressed(Format))
                throw new InvalidOperationException("Cannot set pixel on compressed format. Call Decompress() first.");

            var mipData = mipmaps[mipmapIndex];
            var mipSize = CalculateMipSize(mipmapIndex);
            int stride = GetStride();

            int dataIndex = stride > 0 ? ((y * mipSize.Width) + x) * stride
                                       : ((y * mipSize.Width) + x) / -stride;
            switch (Format)
            {
                case TextureFormat.P8:
                case TextureFormat.PA8:
                {
                    byte palIndex = (byte)Palette.IndexOf(color);
                    mipData[dataIndex] = palIndex;
                    break;
                }
                case TextureFormat.P4:
                case TextureFormat.PA4:
                {
                    int nibbleIdx = ((y * mipSize.Width) + x) % 2;
                    byte palIndex = (byte)Palette.IndexOf(color);
                    if (nibbleIdx > 0)
                    {
                        mipData[dataIndex] = (byte)((palIndex << 4) | (mipData[dataIndex] & 0x0F));
                    }
                    else
                    {
                        mipData[dataIndex] = (byte)((mipData[dataIndex] & 0xF0) | (palIndex & 0x0F));
                    }
                    break;
                }
                case TextureFormat.P8A8:
                {
                    Color noalpha = Color.FromArgb(255, color.R, color.G, color.B);
                    mipData[dataIndex] = (byte)Palette.IndexOf(noalpha);
                    mipData[dataIndex + 1] = color.A;
                    break;
                }
                case TextureFormat.A4I4:
                {
                    byte grey = (byte)RemapColorRange(color.Average(), 15);
                    byte alpha = (byte)RemapColorRange(color.A, 15);
                    mipData[dataIndex] = (byte)(((alpha & 0xF) << 4) | (grey & 0xF));
                    break;
                }
                case TextureFormat.A8I8:
                {
                    byte grey = (byte)color.Average();
                    mipData[dataIndex] = color.A;
                    mipData[dataIndex + 1] = grey;
                    break;
                }
                case TextureFormat.I8:
                {
                    byte grey = (byte)color.Average();
                    mipData[dataIndex] = grey;
                    break;
                }
                case TextureFormat.A8:
                {
                    mipData[dataIndex] = color.A;
                    break;
                }
                case TextureFormat.RGB888:
                {
                    mipData[dataIndex] = color.R;
                    mipData[dataIndex+1] = color.G;
                    mipData[dataIndex+2] = color.B;
                    break;
                }
                case TextureFormat.RGB8888:
                {
                    mipData[dataIndex] = color.R;
                    mipData[dataIndex + 1] = color.G;
                    mipData[dataIndex + 2] = color.B;
                    mipData[dataIndex + 3] = color.A;
                    break;
                }
                case TextureFormat.A1R5G5B5:
                {
                    int r5 = RemapColorRange(color.R, 31);
                    int g5 = RemapColorRange(color.G, 31);
                    int b5 = RemapColorRange(color.B, 31);
                    int a1 = (color.A < AlphaRef) ? 0 : 1;

                    ushort packed = (ushort)((a1 << 15) | (r5 << 10) | (g5 << 5) | b5);

                    mipData[dataIndex] = (byte)(packed & 255);
                    mipData[dataIndex+1] = (byte)(packed >> 8);
                    break;
                }
            }
        }

        /// <summary>
        /// Gets a pixel color from the specified mipmap level
        /// </summary>
        /// <param name="x">The X location of the pixel. Must be scaled for mipmaps.</param>
        /// <param name="y">The Y location of the pixel. Must be scaled for mipmaps.</param>
        /// <param name="mipmapIndex">The mipmap index to get the pixel from</param>
        public Color GetPixel(int x, int y, int mipmapIndex)
        {
            if (FormatIsCompressed(Format))
                throw new InvalidOperationException("Cannot get pixel from compressed format. Call Decompress() first.");

            var mipData = mipmaps[mipmapIndex];
            var mipSize = CalculateMipSize(mipmapIndex);
            int stride = GetStride();

            int dataIndex = stride > 0 ? ((y * mipSize.Width) + x) * stride
                                       : ((y * mipSize.Width) + x) / -stride;
            switch (Format)
            {
                case TextureFormat.PA4:
                case TextureFormat.P4:
                {
                    byte nibbles = mipData[dataIndex];
                    int nibbleIdx = ((y * mipSize.Width) + x) % 2;
                    return (nibbleIdx > 0) ? Palette[(nibbles & 0xF0) >> 4] : Palette[nibbles & 0x0F];
                }
                case TextureFormat.P8:
                case TextureFormat.PA8:
                {
                    byte palIndex = mipData[dataIndex];
                    return Palette[palIndex];
                }
                case TextureFormat.P8A8:
                {
                    byte palIndex = mipData[dataIndex];
                    byte alpha = mipData[dataIndex + 1];
                    return Color.FromArgb(alpha, Palette[palIndex]);
                }
                case TextureFormat.A4I4:
                {
                    byte nibbles = mipData[dataIndex];
                    byte alpha = (byte)(((nibbles >> 4) & 0xF) * 17);
                    byte grey = (byte)((nibbles & 0xF) * 17);
                    return Color.FromArgb(alpha, grey, grey, grey);
                }
                case TextureFormat.A8I8:
                {
                    byte alpha = mipData[dataIndex];
                    byte grey = mipData[dataIndex + 1];
                    return Color.FromArgb(alpha, grey, grey, grey);
                }
                case TextureFormat.I8:
                {
                    return Color.FromArgb(255, mipData[dataIndex], mipData[dataIndex], mipData[dataIndex]);
                }
                case TextureFormat.A8:
                {
                    return Color.FromArgb(mipData[dataIndex], 255, 255, 255);
                }
                case TextureFormat.RGB888:
                {
                    return Color.FromArgb(255, mipData[dataIndex], mipData[dataIndex + 1], mipData[dataIndex + 2]);
                }
                case TextureFormat.RGB8888:
                {
                    return Color.FromArgb(mipData[dataIndex + 3], mipData[dataIndex], mipData[dataIndex + 1], mipData[dataIndex + 2]);
                }
                case TextureFormat.A1R5G5B5:
                {
                    int colorShort = BitConverter.ToUInt16(mipData, dataIndex);
                    int maskA = 32768; 
                    int maskR = 0x7C00;
                    int maskG = 0x3E0; 
                    int maskB = 0x1F;  

                    int alpha = ((maskA & colorShort) >> 8);
                    int red = ((maskR & colorShort) >> 7);
                    int green = ((maskG & colorShort) >> 2);
                    int blue = ((maskB & colorShort) << 3);
                    alpha = alpha > 0 ? 255 : 0;

                    red = (red & 0x8) == 0x8 ? red | 0xF : red;
                    green = (green & 0x8) == 0x8 ? green | 0xF : green;
                    blue = (blue & 0x8) == 0x8 ? blue | 0xF : blue;

                    return Color.FromArgb((byte)alpha, (byte)red, (byte)green, (byte)blue);
                }
            }

            return Color.FromArgb(255, 255, 255, 255);
        }


        /// <summary>
        /// Generate mipmaps with bicubic filtering
        /// </summary>
        private void GenerateInterpolatedMipmapsRGB()
        {
            Bitmap bitmap = ConvertToBitmap();

            for(int i=1; i < mipmaps.Count; i++)
            {
                var size = CalculateMipSize(i);
                Bitmap tmpBitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
                
                // scale
                Graphics g = Graphics.FromImage(tmpBitmap);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(bitmap, new Rectangle(Point.Empty, size));
                g.Dispose();
                
                // copy over the data
                for(int y=0; y < size.Height; y++)
                {
                    for(int x=0; x < size.Width; x++)
                    {
                        SetPixel(x, y, tmpBitmap.GetPixel(x, y), i);
                    }
                }
                
                // dispose of temp bmap
                tmpBitmap.Dispose();
            }

            bitmap.Dispose();
        }


        /// <summary>
        /// Generate mipmaps with Paint.NET Fant filtering
        /// </summary>
        private void GenerateInterpolatedMipmapsRGB_PDN(Surface inputSurface)
        {
            for (int i = 1; i < mipmaps.Count; i++)
            {
                var size = CalculateMipSize(i);
                
                var tempSurface = new Surface(size);
                tempSurface.FitSurface(ResamplingAlgorithm.Fant, inputSurface, FitSurfaceOptions.Default);

                for (int y = 0; y < size.Height; y++)
                {
                    for (int x = 0; x < size.Width; x++)
                    {
                        SetPixel(x, y, tempSurface.GetPoint(x, y), i);
                    }
                }

                tempSurface.Dispose();
            }
        }

        /// <summary>
        /// Generate lame mipmaps without any filtering
        /// </summary>
        private void GenerateMipmapsRGB()
        {
            for (int i = 1; i < mipmaps.Count; i++)
            {
                int divisor = MipDivisors[i];
                var size = CalculateMipSize(i);
                for (int y = 0; y < size.Height; y++)
                {
                    for (int x = 0; x < size.Width; x++)
                    {
                        SetPixel(x, y, GetPixel(x * divisor, y * divisor), i);
                    }
                }
            }
        }

        /// <summary>
        /// Generate lame palettized mipmaps
        /// </summary>
        private void GenerateMipmapsPalletized()
        {
            int stride = this.GetStride();
            byte[] sourceMip = mipmaps[0];

            for (int i = 1; i < mipmaps.Count; i++)
            {
                int pixelDivisor = MipDivisors[i];
                byte[] mipData = mipmaps[i];
                var size = CalculateMipSize(i);

                for(int y=0; y < size.Height; y++)
                {
                    for(int x=0; x < size.Width; x++)
                    {
                        int srcX = x * pixelDivisor;
                        int srcY = y * pixelDivisor;

                        int dataIndexSrc = (srcY * Width) + srcX;
                        int dataIndex = (y * size.Width) + x;

                        //if 4bit, stride == -2
                        if (stride < 0)
                        {
                            int nibbleIdxSrc = dataIndexSrc % 2;
                            int nibbleIdx = dataIndex % 2;

                            dataIndexSrc /= -stride;
                            dataIndex /= -stride;

                            byte bSrc = sourceMip[dataIndexSrc];
                            byte colorIndex = (nibbleIdxSrc > 0) ? (byte)((bSrc & 0xF0) >> 4) : (byte)(bSrc & 0x0F);

                            if (nibbleIdx > 0)
                            {
                                mipData[dataIndex] = (byte)((colorIndex << 4) | (mipData[dataIndex] & 0x0F));
                            }
                            else
                            {
                                mipData[dataIndex] = (byte)((mipData[dataIndex] & 0xF0) | (colorIndex & 0x0F));
                            }
                        }
                        else
                        {
                            dataIndexSrc *= stride;
                            dataIndex *= stride;

                            for (int s = 0; s < stride; s++)
                            {
                                mipData[dataIndex+s] = sourceMip[dataIndexSrc+s];
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Allocate the data for mipmaps storage, that can then be filled with GenerateMipmaps
        /// Note that the maxCount includes the base layer, so full size + 1 mipmap would be maxCount = 2
        /// minWidth/minHeight are inclusive. A value of 8 will make the smallest mipmap 8x8
        /// </summary>
        public void AllocateMipmapData(int count)
        {
            int mipIndex = 0;
            int width = Width;
            int height = Height;
            count = Math.Min(MipDivisors.Length, count);

            if(!CanGenerateMips())
            {
                count = 1;
            }

            while (mipIndex < count && width > 0 && height > 0)
            {
                if (mipIndex >= mipmaps.Count)
                {
                    this.mipmaps.Add(new byte[CalculateMipArraySize(mipIndex)]);
                }
                width /= 2;
                height /= 2;
                mipIndex++;
            }
        }

        public bool CanGenerateMips()
        {
            return (Width % 2) == 0 && (Height % 2) == 0;
        }

        /// <summary>
        /// Generates mipmaps to fill MipmapCount mipmaps
        /// To add more mipmaps, call AddMipmapData first
        /// </summary>
        public void GenerateMipmaps()
        {
            if (!CanGenerateMips())
                return;

            if (FormatIsPaletted(Format))
            {
                GenerateMipmapsPalletized();
            }
            else
            {
                GenerateInterpolatedMipmapsRGB();
            }
        }

        /// <summary>
        /// Generates mipmaps to fill MipmapCount mipmaps
        /// Extended for Paint.NET superior filtering types
        /// To add more mipmaps, call AddMipmapData first
        /// </summary>
        public void GenerateMipmaps_PDN(Surface inputSurface)
        {
            if (!CanGenerateMips())
                return;

            if (FormatIsPaletted(Format))
            {
                GenerateMipmapsPalletized();
            }
            else
            {
                GenerateInterpolatedMipmapsRGB_PDN(inputSurface);
            }
        }

        /// <summary>
        /// Removes mipmaps that are a certain size or less
        /// </summary>
        /// <param name="width">The exclusive min width</param>
        /// <param name="height">The exclusive min height</param>
        public void RemoveMipmapsBelowSize(int width, int height)
        {
            int removeIndex = -1;
            for(int i=1; i < mipmaps.Count; i++)
            {
                var size = CalculateMipSize(i);
                if(size.Width < width && size.Height < height)
                {
                    removeIndex = i;
                    break;
                }
            }

            if(removeIndex >= 0)
            {
                while(mipmaps.Count > removeIndex)
                {
                    mipmaps.RemoveAt(removeIndex);
                }
            }
        }

        public bool IsValid()
        {
            return Width != 0 && Height != 0;
        }

        public void ClearPaletteAlpha()
        {
            if (!RemovePaletteAlphas)
                return;

            for(int i=0; i < Palette.Count; i++)
            {
                var col = Palette[i];
                Palette[i] = Color.FromArgb(255, col);
            }
        }

        private static List<Color> ReadPalette(BinaryReader reader, int size = 256)
        {
            var returnList = new List<Color>();
            for(int i=0; i < size; i++)
            {
                byte r, g, b, a;
                b = reader.ReadByte();
                g = reader.ReadByte();
                r = reader.ReadByte();
                a = reader.ReadByte();
                returnList.Add(Color.FromArgb(a, r, g, b));
            }
            return returnList;
        }

        public static Texture FromBitmap(Bitmap bitmap, bool mipmap)
        {
            TextureFormat texFormat;
            if(bitmap.PixelFormat == PixelFormat.Format16bppArgb1555)
            {
                texFormat = TextureFormat.A1R5G5B5;
            }
            else if(bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                bool hasAlpha = bitmap.Palette.Entries.Min(x => x.A) == 255;
                texFormat = (hasAlpha) ? TextureFormat.PA8 : TextureFormat.P8;
            }
            else if (bitmap.PixelFormat == PixelFormat.Format4bppIndexed || bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
            {
                bool hasAlpha = bitmap.Palette.Entries.Min(x => x.A) == 255;
                texFormat = (hasAlpha) ? TextureFormat.PA4 : TextureFormat.P4;
            }
             else if (bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppPArgb 
                || bitmap.PixelFormat == PixelFormat.Format64bppArgb || bitmap.PixelFormat == PixelFormat.Format64bppArgb 
                || bitmap.PixelFormat == PixelFormat.Format64bppPArgb || bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
            {
                texFormat = TextureFormat.RGB8888;
            }
            else
            {
                texFormat = TextureFormat.RGB888;
            }

            var texture = new Texture(bitmap.Width, bitmap.Height, texFormat, mipmap);
            if (FormatIsPaletted(texture.Format))
            {
                texture.Palette.AddRange(bitmap.Palette.Entries);
            }

            for(int y = 0; y < bitmap.Height; y++)
            {
                for(int x = 0; x < bitmap.Width; x++)
                {
                    texture.SetPixel(x, y, bitmap.GetPixel(x, y));
                }
            }
            texture.GenerateMipmaps();
            return texture;
        }

        public void Save(Stream stream)
        {
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                writer.Write((ushort)Width);
                writer.Write((ushort)Height);
                writer.Write((ushort)Format);
                writer.Write((ushort)MipmapCount);
                writer.Write((ushort)1);
                writer.Write((int)Flags);

                //write palette
                if (FormatIsPaletted(Format))
                {
                    for (int i = 0; i < Palette.Count; i++)
                    {
                        var palEntry = Palette[i];
                        writer.Write(palEntry.B);
                        writer.Write(palEntry.G);
                        writer.Write(palEntry.R);
                        writer.Write(palEntry.A);
                    }
                }

                //write mips
                for (int i = 0; i < MipmapCount; i++)
                {
                    writer.Write(mipmaps[i]);
                }
            }
        }

        public void Save(string filePath, bool overwrite = true)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
            Save(File.OpenWrite(filePath));
        }

        public static Texture FromFile(string filePath)
        {
            using(var stream = File.OpenRead(filePath))
            {
                return FromStream(stream);
            }
        }

        public static Texture FromStream(Stream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.ASCII, true))
            {
                var returnTexture = new Texture()
                {
                    Width = reader.ReadUInt16(),
                    Height = reader.ReadUInt16(),
                    Format = (TextureFormat)reader.ReadUInt16()
                };

                if(!Enum.IsDefined(typeof(TextureFormat), returnTexture.Format))
                {
                    throw new Exception($"Unsupported TEX format: {returnTexture.Format}");
                }

                int mipCount = reader.ReadUInt16();
                reader.BaseStream.Seek(2, SeekOrigin.Current);
                returnTexture.Flags = (TextureFlags)reader.ReadInt32();

                //read palette if needed
                if (returnTexture.Format == TextureFormat.P4 || returnTexture.Format == TextureFormat.PA4)
                {
                    returnTexture.Palette.AddRange(ReadPalette(reader, 16));
                }
                else if (returnTexture.Format == TextureFormat.P8A8 || returnTexture.Format == TextureFormat.P8 || returnTexture.Format == TextureFormat.PA8)
                {
                    returnTexture.Palette.AddRange(ReadPalette(reader, 256));
                }

                //remove alpha from these
                if (!FormatSupportsAlpha(returnTexture.Format))
                    returnTexture.ClearPaletteAlpha();

                //read texture
                for(int i = 0; i < mipCount; i++)
                {
                    byte[] data = reader.ReadBytes(returnTexture.CalculateMipArraySize(i));
                    if (data.Length == 0)
                        break;
                    returnTexture.mipmaps.Add(data);
                }

                //
                return returnTexture;
            }
        }

        public Texture Clone()
        {
            var texture = new Texture()
            {
                Width = this.Width,
                Height = this.Height,
                Flags = this.Flags,
                Format = this.Format,
            };

            texture.Palette.AddRange(this.Palette);
            for(int i=0; i < MipmapCount; i++)
            {
                byte[] newMipData = new byte[this.mipmaps[i].Length];
                Array.Copy(this.mipmaps[i], newMipData, newMipData.Length);
                texture.mipmaps.Add(newMipData);
            }

            return texture;
        }

        public Texture(int width, int height, TextureFormat format, bool mipmaps)   
                      : this(width, height, format, (mipmaps) ? CalculateMaxMipmaps(width, height) : 1)
        {

        }

        public Texture(int width, int height, TextureFormat format, int mipmapCount)
        {
            if(mipmapCount < 1)
            {
                throw new ArgumentException("Must have at least one mipmap level.", nameof(mipmapCount));
            }

            Width = width;
            Height = height;
            Format = format;
            AllocateMipmapData(mipmapCount);
        }

        public Texture(int width, int height) : this(width, height, TextureFormat.RGB8888, false)
        {
            
        }

        public Texture(int width, int height, TextureFormat format) : this(width, height, format, false)
        {

        }
        
        private Texture()
        {

        }
    }
}
