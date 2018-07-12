
namespace ZombieUtilities
{
    public static class Extensions
    {
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
    }
}
