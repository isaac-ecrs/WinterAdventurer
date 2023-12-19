using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinterAdventurer.Library.Extensions
{
    using System;

    public static class StringExtensions
    {
        public static string GetLeaderName(this string input)
        {
            return input.ExtractFromParentheses(true);
        }
        public static string GetWorkshopName(this string input)
        {
            return input.ExtractFromParentheses(false);
        }
        public static string ExtractFromParentheses(this string input, bool pullFromInside = true)
        {
            int openingParenthesisIndex = input.IndexOf('(');
            int closingParenthesisIndex = input.IndexOf(')');

            if (openingParenthesisIndex >= 0 && closingParenthesisIndex > openingParenthesisIndex)
            {
                if (pullFromInside)
                {
                    // Extract the substring between parentheses
                    return input.Substring(openingParenthesisIndex + 1, closingParenthesisIndex - openingParenthesisIndex - 1).Trim();
                }
                else
                {
                    // Extract the substring before the opening parenthesis
                    return input.Substring(0, openingParenthesisIndex).Trim();
                }
            }
            else
            {
                // Return an empty string if no valid content found
                return string.Empty;
            }
        }
    }

}
