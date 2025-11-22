using PdfSharp.Fonts;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace WinterAdventurer.Library
{
    public class CustomFontResolver : IFontResolver
    {
        /// <summary>
        /// Resolves font family names to embedded font resource identifiers for PDF generation.
        /// Maps high-level font names (NotoSans, Oswald, Roboto) to specific font file variants (Regular/Bold).
        /// Falls back to NotoSans for unknown fonts to ensure PDFs always render properly.
        /// </summary>
        /// <param name="familyName">Font family name requested by PDF generation (e.g., "NotoSans", "Oswald", "Arial").</param>
        /// <param name="isBold">True to use bold variant of the font.</param>
        /// <param name="isItalic">True to use italic variant (currently ignored, only bold is supported).</param>
        /// <returns>FontResolverInfo containing the font face name to load from embedded resources.</returns>
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
                    // Fall back to NotoSans for unknown fonts
                    return PlatformFontResolver.ResolveTypeface(familyName, isBold, isItalic)
                        ?? new FontResolverInfo("NotoSans-Regular");
            }
        }

        /// <summary>
        /// Loads font file data from embedded resources as byte array for PDF rendering.
        /// Fonts are embedded in the assembly to ensure PDFs render consistently on any system.
        /// </summary>
        /// <param name="faceName">Font face name (e.g., "NotoSans-Regular", "Oswald-Bold") to load.</param>
        /// <returns>Byte array containing the TTF font file data.</returns>
        /// <exception cref="InvalidOperationException">Thrown if font resource is not found in assembly.</exception>
        public byte[] GetFont(string faceName)
        {
            string? resourceName = GetFontResourceName(faceName);
            if (resourceName == null)
                throw new InvalidOperationException($"Font resource for {faceName} not found.");

            return LoadFontFromResource(resourceName);
        }

        /// <summary>
        /// Maps font face names to their corresponding embedded resource paths in the assembly.
        /// Resource names follow .NET embedded resource naming convention (folders become dots).
        /// </summary>
        /// <param name="faceName">Font face name (e.g., "NotoSans-Regular") to look up.</param>
        /// <returns>Full embedded resource name, or null if face name is not recognized.</returns>
        private string? GetFontResourceName(string faceName)
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

        /// <summary>
        /// Loads font file from embedded assembly resource into byte array.
        /// Reads the entire TTF file into memory for PDF generation engine to use.
        /// </summary>
        /// <param name="resourceName">Full embedded resource name (e.g., "WinterAdventurer.Library.Resources.Fonts...").</param>
        /// <returns>Byte array containing the complete TTF font file.</returns>
        /// <exception cref="InvalidOperationException">Thrown if resource stream cannot be opened.</exception>
        private byte[] LoadFontFromResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
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
