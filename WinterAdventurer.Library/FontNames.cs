namespace WinterAdventurer.Library
{
    /// <summary>
    /// Font family names used in PDF generation.
    /// These are base family names - bold/italic variants are set via Format.Font.Bold and Format.Font.Italic properties.
    /// </summary>
    public static class FontNames
    {
        /// <summary>
        /// Noto Sans font family - clean, readable sans-serif font
        /// Used for body text, headers, and general content
        /// </summary>
        public const string NotoSans = "NotoSans";

        /// <summary>
        /// Oswald font family - condensed sans-serif font
        /// Used for prominent titles and headers
        /// </summary>
        public const string Oswald = "Oswald";

        /// <summary>
        /// Roboto font family - modern sans-serif font
        /// Used for participant lists and detailed information
        /// </summary>
        public const string Roboto = "Roboto";
    }
}
