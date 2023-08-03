# TEXFileFormat
Angel Studios TEX/XTEX File Format for Paint.NET

**Compatibility**: 5.0+

 
**Installation:**

 

1. Close Paint.NET.
2. Place TexFileType.dll in the Paint.NET FileTypes folder which is usually located in one the following locations depending on the Paint.NET version you have installed.

  - Classic: C:\Program Files\Paint.NET\FileTypes    
  - Microsoft Store: Documents\paint.net App Files\FileTypes

3. Restart Paint.NET.

**Supported TEX Formats**
- P8 - 8 bit paletted image, 256 colors. no alpha channel
- PA8 -  8 bit paletted image, 256 colors with alpha channel
- P8A8 - 8 bit paletted image, 256 colors, each pixel gets its own unique alpha value
- A4I4 - 4 bits greyscale image with 4 bits alpha
- A8I8 - 8 bits greyscale image with 8 bits alpha
- I8 - 8 bit greyscale image
- A8 - 8 bit alpha mask image
- P4 - 4 bit paletted image, 16 colors
- PA4 - 4 bit paletted image, 16 colors with alpha channel
- A1R5G5B5 - 16 bit RGB image, 5 bits per color channel and 1 bit alpha
- RGB888 - 24 bit RGB image, no alpha channel
- RGB8888 - 32 bit RGB image, with alpha channel
- DXT1 - DXT1 block compression
- DXT3* - DXT3 block compression (load only)
- DXT5 - DXT5 block compression
