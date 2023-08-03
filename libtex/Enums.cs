// Copyright (c) 2023 Dummiesman
// Licensed under the MIT license. See LICENSE.txt

namespace libtex
{
    [System.Flags]
    public enum TextureFlags
    {
        ClampU = 0x01,
        CloudShadowsLow = 0x04,
        CloudShadowsHigh = 0x02,
        //UnknownThing=0x8000 
        ClampV = 0x10000,
        Transparent=0x20000
        //Unknown=0x40000 (Set on RenderTexture and HW textures)
        //HasMipmaps=0x80000 (Set internally on load, doesn't seem to ever be set externally)
    }

    public enum TextureFormat
    {
        /// <summary>
        /// 8 bit opaque image
        /// </summary>
        P8 = 1,
        /// <summary>
        /// 8 bit image with 8 bits alpha per pixel (16 bit pixels)
        /// </summary>
        P8A8 = 2,
        /// <summary>
        /// 16 bit image with 5 bits per channel and 1 bit alpha
        /// </summary>
        A1R5G5B5 = 6,
        /// <summary>
        /// 8 bit greyscale image
        /// </summary>
        I8 = 8,
        /// <summary>
        /// 4 bits alpha, 4 bits greyscale
        /// </summary>
        A4I4 = 9,
        /// <summary>
        /// 8 bits alpha, 8 bits greyscale
        /// </summary>
        A8I8 = 10,
        /// <summary>
        /// Alpha channel only
        /// </summary>
        A8 = 11,
        /// <summary>
        /// 8 bit image with alpha
        /// </summary>
        PA8 = 14,
        /// <summary>
        /// 4 bit opaque image
        /// </summary>
        P4 = 15,
        /// <summary>
        /// 4 bit image with alpha
        /// </summary>
        PA4 = 16,
        /// <summary>
        /// 24 bit image
        /// </summary>
        RGB888 = 17,
        /// <summary>
        /// 32 bit image with alpha
        /// </summary>
        RGB8888 = 18,

        //Type19= 19, // 16bpp, unknown format
        //Type20 = 20, // 24bpp, unknown format
        //Type21 = 21, // 32bpp, unknown format

        /// <summary>
        /// DXT1 block compression
        /// </summary>
        DXT1 = 22,
        /// <summary>
        /// DXT3 block compression
        /// </summary>
        DXT3 = 24,
        /// <summary>
        /// DXT5 block compression
        /// </summary>
        DXT5 = 26 
    }
}
