// Copyright (c) 2023 Dummiesman
// Licensed under the MIT license. See LICENSE.txt

using System;
using System.Reflection;
using PaintDotNet;

namespace TexFileType
{
    public sealed class PluginSupportInfo : IPluginSupportInfo
    {
        public string DisplayName => "Angel Studios TEX/XTEX FileType";

        public string Author => "Dummiesman";

        public string Copyright
        {
            get
            {
                object[] attributes = typeof(PluginSupportInfo).Assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);

                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public Version Version => typeof(PluginSupportInfo).Assembly.GetName().Version;

        public Uri WebsiteUri => new("https://github.com/Dummiesman/TEXFileFormat");
    }
}
