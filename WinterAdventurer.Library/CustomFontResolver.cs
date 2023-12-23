using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinterAdventurer.Library
{
    public class CustomFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            var name = familyName.ToLower();

            switch (name)
            {
                case "arial":
                    return new FontResolverInfo("Arial#");
                // Add more cases for other fonts if needed.
                // ...

                default:
                    return PlatformFontResolver.ResolveTypeface(familyName, isBold, isItalic);
            }
        }

        public byte[] GetFont(string faceName)
        {
            switch (faceName.ToLower())
            {
                case "arial#":
                    // Load Arial font data (replace with your actual font data).
                    return LoadFontData("C:\\Windows\\Fonts\\arial.ttf");
                // Add more cases for other fonts if needed.
                // ...

                default:
                    return LoadFontData(faceName);
            }
        }

        private byte[] LoadFontData(string fontFilePath)
        {
            try
            {
                // Read the font file as a byte array.
                return File.ReadAllBytes(fontFilePath);
            }
            catch (Exception ex)
            {
                // Handle any exceptions (e.g., file not found, permissions, etc.).
                // You can log the error or provide a fallback font data.
                Console.WriteLine($"Error loading font data: {ex.Message}");
                // Return a fallback font (e.g., Helvetica) as a byte array.
                return LoadFallbackFontData();
            }
        }

        private byte[] LoadFallbackFontData()
        {
            // Load your fallback font data here.
            // Replace this with the actual font data for your chosen fallback font.
            // Example: Load Arial font data.
            return LoadFontData("C:\\Windows\\Fonts\\arial.ttf");
        }

    }
}
