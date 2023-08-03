using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using JeremyAnsel.ColorQuant;

namespace JeremyAnsel.ColorQuant
{
    internal class Extensions
    {
        public static byte[] ImageToARGBArray(Bitmap bitmap)
        {
            byte[] data = new byte[bitmap.Width * bitmap.Height * 4];
            for(int y=0; y<bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    int dataIndex = ((y *  bitmap.Width) + x) * 4;
                    var color = bitmap.GetPixel(x, y);

                    data[dataIndex] = color.A;
                    data[dataIndex+1] = color.R;
                    data[dataIndex+2] = color.G;
                    data[dataIndex+3] = color.B;
                }
            }
            return data;
        }
        
        public static Color[] CQuantResultToPalette(ColorQuantizerResult res)
        {
            Color[] palette = new Color[256];
            int paletteIndex = 0;

            for (int i = 0; i < res.Palette.Length / 4; i++)
            {
                int dataOffset = i * 4;
                palette[paletteIndex++] = Color.FromArgb(res.Palette[dataOffset], res.Palette[dataOffset + 1], res.Palette[dataOffset + 2], res.Palette[dataOffset + 3]);
            }
            return palette;
        }

        public static Bitmap CQuantResultToImage(ColorQuantizerResult res, int width, int height)
        {
            var palette = CQuantResultToPalette(res);   

            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            var newAliasForPalette = bitmap.Palette; 
            for (int i = 0; i < 256; i++)
            {
                newAliasForPalette.Entries[i] = palette[i];
            }
            bitmap.Palette = newAliasForPalette;

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            Int64 scan0 = data.Scan0.ToInt64();
            for (Int32 y = 0; y < height; y++)
                Marshal.Copy(res.Bytes, y * width, new IntPtr(scan0 + y * data.Stride), width);
            bitmap.UnlockBits(data);

            return bitmap;
        }
    }
}
