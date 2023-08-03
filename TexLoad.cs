// Copyright (c) 2023 Dummiesman
// Licensed under the MIT license. See LICENSE.txt

using libtex;
using PaintDotNet;
using System.IO;

namespace TexFileType
{
    internal static class TexLoad
    {
        public static Document Load(Stream input)
        {
            var image = Texture.FromStream(input);
            if (Texture.FormatIsCompressed(image.Format))
            {
                image.Decompress();
            }

            Document document = new Document(image.Width, image.Height);
            document.Metadata.SetUserValue("TexFlags", image.Flags.ToString());
            document.Metadata.SetUserValue("TexFormat", image.Format.ToString());

            Surface surface;
            using (var bitmap = image.ConvertToBitmap())
            {
                surface = Surface.CopyFromBitmap(bitmap);
            }
            document.Layers.Add(Layer.CreateBackgroundLayer(surface, takeOwnership: true));
            return document;
        }
    }
}
