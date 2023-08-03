// Copyright (c) 2023 Dummiesman
// Licensed under the MIT license. See LICENSE.txt

using System.Drawing;

namespace libtex
{
    internal static class LibtexExtensions
    {
        public static int Average(this Color color)
        {
            return (color.R + color.G + color.B) / 3;
        }
    }
}
