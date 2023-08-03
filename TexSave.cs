// Copyright (c) 2023 Dummiesman
// Licensed under the MIT license. See LICENSE.txt

using System.Drawing;
using System.IO;
using PaintDotNet;
using libtex;
using System.Collections.Generic;
using System.Linq;
using JeremyAnsel.ColorQuant;

namespace TexFileType
{
    internal static class TexSave
    {
        /// <summary>
        /// Try to detect the most suitable TextureFormat for this Surface
        /// Left over for potential future use, but not currently used because it may choose a format not supported by the target game
        /// </summary>
        private static TextureFormat DetermineBestFormat(Surface inSurface)
        {
            int colorCount = 0;
            HashSet<Color> colors = new HashSet<Color>();
            int colorCountRgb = 0;
            HashSet<Color> colorsRgb = new HashSet<Color>();

            bool hasAlpha = false;
            bool hasSemitransAlpha = false;

            bool isGreyscale = true;

            for(int y=0; y < inSurface.Height; y++)
            {
                for(int x = 0; x < inSurface.Width; x++)
                {
                    Color c = inSurface.GetPoint(x, y);
                    if (colors.Add(c))
                    {
                        colorCount++;
                    }

                    Color crgb = Color.FromArgb(255, c);
                    if(colorsRgb.Add(crgb))
                    {
                        colorCountRgb++;
                    }

                    hasAlpha |= (c.A < 255);
                    hasSemitransAlpha |= (c.A > 0 && c.A < 255);

                    if(c.R != c.B || c.R != c.G)
                    {
                        isGreyscale = false;
                    }
                }
            }

            var firstColor = colorsRgb.First();
            if (colorCountRgb == 1 && firstColor.R == 255 && isGreyscale && hasAlpha)
            {
                // single channel image
                return TextureFormat.A8;
            }
            else if(isGreyscale && !hasAlpha)
            {
                // single channel image
                return TextureFormat.I8;
            }
            else if (colorCountRgb <= 16)
            {
                // 16 color image
                return (hasAlpha) ? TextureFormat.PA4 : TextureFormat.P4;
            }
            else if(colorCountRgb <= 256)
            {
                // 256 color image
                return (hasAlpha) ? TextureFormat.PA8 : TextureFormat.P8;
            }
            else
            {
                // true color
                return (hasAlpha) ? TextureFormat.RGB8888 : TextureFormat.RGB888;
            }
        }

        public static void Save(Document input,
                                Stream output, 
                                Surface scratchSurface, 
                                TextureFormat format, 
                                bool cloudShadowsLow, 
                                bool cloudShadowsHigh, 
                                bool clampU, 
                                bool clampV,
                                int additionalMipCount,
                                int alphaCutoff)
        {
            // create a texture from the first layer
            var surface = ((BitmapLayer)input.Layers[0]).Surface;
            TextureFormat actualFormat = Texture.FormatIsCompressed(format) ? TextureFormat.RGB8888 : format;
            var texture = new Texture(input.Width, input.Height, actualFormat, 1 + additionalMipCount);

            if (clampU) texture.Flags |= TextureFlags.ClampU;
            if (clampV) texture.Flags |= TextureFlags.ClampV;
            if (cloudShadowsLow) texture.Flags |= TextureFlags.CloudShadowsLow;
            if (cloudShadowsHigh) texture.Flags |= TextureFlags.CloudShadowsHigh;
            Texture.AlphaRef = alphaCutoff;

            if (Texture.FormatIsPaletted(format))
            {
                // qnautize it first
                int colorCount = Texture.FormatPaletteSize(format);
                bool useAlpha = Texture.FormatSupportsAlpha(format) && format != TextureFormat.P8A8;
                var myBitmap = surface.CreateAliasedBitmap(useAlpha);

                IColorQuantizer quantizer = new WuAlphaColorQuantizer();
                var quantizedImage = quantizer.Quantize(Extensions.ImageToARGBArray(myBitmap), colorCount);
                var quantizedPalette = Extensions.CQuantResultToPalette(quantizedImage);

                // setup new texture palette
                texture.Palette.AddRange(quantizedPalette.Take(colorCount));

                // set pixels on new texture
                for (int y = 0; y < texture.Height; y++)
                {
                    for (int x = 0; x < texture.Width; x++)
                    {
                        int colorIndex = quantizedImage.Bytes[(y * texture.Width) + x];
                        Color color = texture.Palette[colorIndex];
                        if (format == TextureFormat.P8A8)
                        {
                            color = Color.FromArgb(surface.GetPoint(x, y).A, color);
                        }
                        color.GetHashCode();
                        texture.SetPixel(x, y, color);
                    }
                }
            }
            else
            {
                for (int y = 0; y < surface.Height; y++)
                {
                    for (int x = 0; x < surface.Width; x++)
                    {
                        texture.SetPixel(x, y, surface.GetPoint(x, y));
                    }
                }
            }

            texture.GenerateMipmaps_PDN(surface);
            texture.RemoveMipmapsBelowSize(8, 8);

            // compress if desired
            if (Texture.FormatIsCompressed(format))
                texture.Compress(format);

            // setup scratch surface
            scratchSurface.Clear();
            using (var bitmap = texture.ConvertToBitmap())
            {
                scratchSurface.CopyFromGdipBitmap(bitmap);
            }

            // write output
            texture.Save(output);
        }
    }
}
