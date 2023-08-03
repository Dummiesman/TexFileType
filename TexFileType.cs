// Copyright (c) 2023 Dummiesman
// Licensed under the MIT license. See LICENSE.txt

using libtex;
using PaintDotNet;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System.Collections.Generic;
using System.IO;

namespace TexFileType
{
    internal class PropertyNames
    {
        public static readonly string Format = "Format";
        public static readonly string CloudShadowsLow = "Cloud Shadows (Low)";
        public static readonly string CloudShadowsHigh = "Cloud Shadows (High)";
        public static readonly string ClampU = "Clamp U";
        public static readonly string ClampV = "Clamp V";
        public static readonly string AlphaCutoff = "Transparency Threshold";
        public static readonly string GenerateMips = "Generate Mipmaps";
        public static readonly string MipCount = "Max. Mip Count";

        public static IEnumerable<string> EnumerateFlagPropertyNames()
        {
            yield return CloudShadowsLow;
            yield return CloudShadowsHigh;
            yield return ClampU;
            yield return ClampV;
            yield return GenerateMips;
        }
    }

    [PluginSupportInfo<PluginSupportInfo>]
    public sealed class TexFileType : PropertyBasedFileType
    {
        public TexFileType()
            : base("Angel Studios TEX", new FileTypeOptions
            {
                LoadExtensions = new[] { ".tex", ".xtex" },
                SaveExtensions = new[] { ".tex", ".xtex" },
                SupportsLayers = false
            })
        {
        }

        public override PropertyCollection OnCreateSavePropertyCollection()
        {
            List<Property> props = new()
            {
                new StaticListChoiceProperty(PropertyNames.Format, new object[]{TextureFormat.P4,
                                                                                TextureFormat.PA4,
                                                                                TextureFormat.P8,
                                                                                TextureFormat.PA8,
                                                                                TextureFormat.P8A8,
                                                                                TextureFormat.A4I4,
                                                                                TextureFormat.A8I8,
                                                                                TextureFormat.I8,
                                                                                TextureFormat.A8,
                                                                                TextureFormat.A1R5G5B5,
                                                                                TextureFormat.RGB888,
                                                                                TextureFormat.RGB8888,
                                                                                TextureFormat.DXT1,
                                                                                TextureFormat.DXT5}, 11),

                new BooleanProperty(PropertyNames.ClampU, false),
                new BooleanProperty(PropertyNames.ClampV, false),
                new BooleanProperty(PropertyNames.CloudShadowsLow, false),
                new BooleanProperty(PropertyNames.CloudShadowsHigh, false),
                new Int32Property(PropertyNames.AlphaCutoff, 128, 0, 255),
                new BooleanProperty(PropertyNames.GenerateMips, true),
                new Int32Property(PropertyNames.MipCount, 8, 1, 12),
            };

            List<PropertyCollectionRule> rules = new()
            { 
                    new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>(
                    PropertyNames.AlphaCutoff,
                    PropertyNames.Format,
                    TextureFormat.A1R5G5B5,
                    true),
                    new ReadOnlyBoundToValueRule<bool, BooleanProperty>(
                    PropertyNames.MipCount,
                    PropertyNames.GenerateMips,
                    true,
                    true),
            };

            return new PropertyCollection(props, rules);
        }

        public override ControlInfo OnCreateSaveConfigUI(PropertyCollection props)
        {
            ControlInfo info = CreateDefaultSaveConfigUI(props);

            info.FindControlForPropertyName(PropertyNames.MipCount).ControlProperties[ControlInfoPropertyNames.ShowHeaderLine].Value = false;
            info.FindControlForPropertyName(PropertyNames.Format).ControlProperties[ControlInfoPropertyNames.DisplayName].Value = "Format";

            foreach (string propName in PropertyNames.EnumerateFlagPropertyNames())
            {
                info.SetPropertyControlValue(propName, ControlInfoPropertyNames.DisplayName, string.Empty);
                info.SetPropertyControlValue(propName, ControlInfoPropertyNames.Description, propName);
            }

            info.FindControlForPropertyName(PropertyNames.ClampU).ControlProperties[ControlInfoPropertyNames.DisplayName].Value = "Flags";
            info.FindControlForPropertyName(PropertyNames.GenerateMips).ControlProperties[ControlInfoPropertyNames.DisplayName].Value = "Mipmaps";

            return info;
        }

        protected override Document OnLoad(Stream input)
        {
            return TexLoad.Load(input);
        }

        protected override void OnSaveT(Document input, Stream output, PropertyBasedSaveConfigToken token, Surface scratchSurface, ProgressEventHandler progressCallback)
        {
            TextureFormat format = (TextureFormat)token.GetProperty<StaticListChoiceProperty>(PropertyNames.Format).Value;
            bool lowCloudShadows = token.GetProperty<BooleanProperty>(PropertyNames.CloudShadowsLow).Value;
            bool highCloudShadows = token.GetProperty<BooleanProperty>(PropertyNames.CloudShadowsHigh).Value;
            bool clampU = token.GetProperty<BooleanProperty>(PropertyNames.ClampU).Value;
            bool clampV = token.GetProperty<BooleanProperty>(PropertyNames.ClampV).Value;
            int alphaRef = token.GetProperty<Int32Property>(PropertyNames.AlphaCutoff).Value;
            
            bool mips = token.GetProperty<BooleanProperty>(PropertyNames.GenerateMips).Value;
            int additionalMips = token.GetProperty<Int32Property>(PropertyNames.MipCount).Value;

            TexSave.Save(input, output, scratchSurface, format, lowCloudShadows, highCloudShadows, clampU, clampV, (mips) ? additionalMips : 0, alphaRef);
        }
    }
}
