// <copyright file="StringExtensions.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using System.Diagnostics.CodeAnalysis;

namespace WinterAdventurer.Library.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a string to proper case (Title Case) where each word starts with an uppercase letter.
        /// Handles multiple spaces between words and trims leading/trailing whitespace.
        /// </summary>
        /// <param name="input">String to convert to proper case.</param>
        /// <returns>String with each word capitalized (first letter uppercase, remaining lowercase).</returns>
        [SuppressMessage(
            "Globalization",
            "CA1308:Normalize strings to uppercase",
            Justification = "This method is for proper-casing display text, not for culture-invariant comparisons or lookups. Lowercase is required for the output format.")]
        public static string ToProper(this string input)
        {
            var splitInput = input.Split(' ');
            var combinedInput = string.Empty;

            foreach (var word in splitInput)
            {
                // Check for null, whitespace, or empty strings before accessing word[0]
                if (!string.IsNullOrWhiteSpace(word) && word.Length > 0)
                {
                    combinedInput = combinedInput + " " + word[0].ToString().ToUpperInvariant() + word.Substring(1).ToLowerInvariant();
                }
            }

            return combinedInput.Trim();
        }

        /// <summary>
        /// Extracts the leader name from workshop format string "Workshop Name (Leader Name)".
        /// Returns the text inside parentheses, or empty string if format is malformed.
        /// </summary>
        /// <param name="input">Workshop string in format "Workshop Name (Leader Name)".</param>
        /// <returns>Leader name extracted from inside parentheses, or empty string if parentheses are missing or malformed.</returns>
        public static string GetLeaderName(this string input)
        {
            return input.ExtractFromParentheses(true);
        }

        /// <summary>
        /// Extracts the workshop name from workshop format string "Workshop Name (Leader Name)".
        /// Returns the text before the opening parenthesis, or empty string if format is malformed.
        /// </summary>
        /// <param name="input">Workshop string in format "Workshop Name (Leader Name)".</param>
        /// <returns>Workshop name extracted from before the opening parenthesis, or empty string if parentheses are missing or malformed.</returns>
        public static string GetWorkshopName(this string input)
        {
            return input.ExtractFromParentheses(false);
        }

        /// <summary>
        /// Extracts text either inside or outside parentheses from workshop format string.
        /// Used to parse Excel workshop cells in format "Workshop Name (Leader Name)".
        /// </summary>
        /// <param name="input">String containing parentheses, typically "Workshop Name (Leader Name)".</param>
        /// <param name="pullFromInside">True to extract text inside parentheses (leader name), false to extract text before parentheses (workshop name).</param>
        /// <returns>Extracted text, or empty string if parentheses are malformed or missing.</returns>
        public static string ExtractFromParentheses(this string input, bool pullFromInside = true)
        {
            int openingParenthesisIndex = input.IndexOf('(');
            int closingParenthesisIndex = input.IndexOf(')');

            if (openingParenthesisIndex >= 0 && closingParenthesisIndex > openingParenthesisIndex)
            {
                if (pullFromInside)
                {
                    // Extract leader name from workshop format: "Workshop Name (Leader Name)"
                    return input.Substring(openingParenthesisIndex + 1, closingParenthesisIndex - openingParenthesisIndex - 1).Trim();
                }
                else
                {
                    // Extract workshop name from workshop format: "Workshop Name (Leader Name)"
                    return input.Substring(0, openingParenthesisIndex).Trim();
                }
            }
            else
            {
                // Handle malformed Excel entries that don't follow "Name (Leader)" format
                return string.Empty;
            }
        }
    }
}
