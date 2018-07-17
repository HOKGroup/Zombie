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
    }
}
