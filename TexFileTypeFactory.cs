// Copyright (c) 2023 Dummiesman
// Licensed under the MIT license. See LICENSE.txt

using PaintDotNet;

namespace TexFileType
{
    public sealed class TexFileTypeFactory : IFileTypeFactory2
    {
        public FileType[] GetFileTypeInstances(IFileTypeHost host)
        {
            return new FileType[] { new TexFileType() };
        }
    }
}
