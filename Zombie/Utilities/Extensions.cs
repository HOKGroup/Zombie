using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zombie.Utilities
{
    public static class Extensions
    {
        /// <summary>
        /// Extension for an IEnumerable so that it can be cast to ObservableCollection.
        /// </summary>
        /// <typeparam name="T">Type of Collection.</typeparam>
        /// <param name="source">Source Collection.</param>
        /// <returns></returns>
        public static ObservableCollection<T> ToObservableCollection<T>
            (this IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return new ObservableCollection<T>(source);
        }

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
