using System;
using System.Collections.Generic;

namespace ZombieUtilities
{
    public static class Extensions
    {
        #region string.TrimLastCharacter

        /// <summary>
        /// Extension for String so that it removes the last character if it matches input.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <param name="character">Character to remove.</param>
        /// <returns></returns>
        public static string TrimLastCharacter(this string str, string character)
        {
            return string.IsNullOrEmpty(str)
                ? str
                : str.EndsWith(character)
                    ? str.TrimEnd(str[str.Length - 1])
                    : str;
        }

        #endregion

        #region string.Split

        public static IEnumerable<string> Split(this string str,
            Func<char, bool> controller)
        {
            var nextPiece = 0;

            for (var c = 0; c < str.Length; c++)
            {
                if (controller(str[c]))
                {
                    yield return str.Substring(nextPiece, c - nextPiece);
                    nextPiece = c + 1;
                }
            }

            yield return str.Substring(nextPiece);
        }

        #endregion

        public static string TrimMatchingQuotes(this string input, char quote)
        {
            if ((input.Length >= 2) &&
                (input[0] == quote) && (input[input.Length - 1] == quote))
                return input.Substring(1, input.Length - 2);

            return input;
        }
    }
}
