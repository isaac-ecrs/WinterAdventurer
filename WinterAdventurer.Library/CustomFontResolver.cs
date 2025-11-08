using PdfSharp.Fonts;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace WinterAdventurer.Library
{
    public class CustomFontResolver : IFontResolver
    {
        // Resolve font family names to embedded resource names
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            var name = familyName.ToLower();

            switch (name)
            {
                case "noto":
                case "notosans":
                case "arial":
                    if (isBold)
                        return new FontResolverInfo("NotoSans-Bold");
                    return new FontResolverInfo("NotoSans-Regular");
                case "oswald":
                    if (isBold)
                        return new FontResolverInfo("Oswald-Bold");
                    return new FontResolverInfo("Oswald-Regular");
                case "roboto":
                    if (isBold)
                        return new FontResolverInfo("Roboto-Bold");
                    return new FontResolverInfo("Roboto-Regular");

                default:
                    return PlatformFontResolver.ResolveTypeface(familyName, isBold, isItalic);
            }
        }

        // Get the font from embedded resources
        public byte[] GetFont(string faceName)
        {
            string resourceName = GetFontResourceName(faceName);
            if (resourceName == null)
                throw new InvalidOperationException($"Font resource for {faceName} not found.");

            return LoadFontFromResource(resourceName);
        }

        // Map font family name to the corresponding embedded resource name
        private string GetFontResourceName(string faceName)
        {
            switch (faceName.ToLower())
            {
                case "notosans-regular":
                    return "WinterAdventurer.Library.Resources.Fonts.Noto_Sans.static.NotoSans-Regular.ttf";
                case "notosans-bold":
                    return "WinterAdventurer.Library.Resources.Fonts.Noto_Sans.static.NotoSans-Bold.ttf";
                case "oswald-regular":
                    return "WinterAdventurer.Library.Resources.Fonts.Oswald.static.Oswald-Regular.ttf";
                case "oswald-bold":
                    return "WinterAdventurer.Library.Resources.Fonts.Oswald.static.Oswald-Bold.ttf";
                case "roboto-regular":
                    return "WinterAdventurer.Library.Resources.Fonts.Roboto.Roboto-Regular.ttf";
                case "roboto-bold":
                    return "WinterAdventurer.Library.Resources.Fonts.Roboto.Roboto-Bold.ttf";
                default:
                    return null;
            }
        }

        // Load the font from the embedded resource stream
        private byte[] LoadFontFromResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException($"Resource {resourceName} not found.");

                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}
