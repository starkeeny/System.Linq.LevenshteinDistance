using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace System.Linq.LevenshteinDistance
{
    /// <summary>
    /// LD Unit to configure in <see cref="LevenshteinDistanceOptions"/>
    /// </summary>
    public enum LevenshteinDistanceUnit
    {
        /// <summary>
        /// LD is configured with an absolute value.
        /// </summary>
        Absolute,

        /// <summary>
        /// LD is configured in percent based on max length of the strings to compare.
        /// </summary>
        Percentage
    }

    /// <summary>
    /// Configuration options for LD.
    /// </summary>
    public class LevenshteinDistanceOptions
    {
        /// <summary>
        /// Unit of the distance.
        /// </summary>
        public LevenshteinDistanceUnit Unit { get; }

        /// <summary>
        /// Allowed LD (=tolerance for equality).
        /// </summary>
        public int Distance { get; }

        /// <summary>
        /// Enables the option to remove all digits in the strings.
        /// </summary>
        public bool RemoveAllDigits { get; }

        /// <summary>
        /// Enables the option to remove all guids.
        /// </summary>
        public bool RemoveStandardFormattedGuid { get; }

        /// <summary>
        /// Initializes the Opertions-Object.
        /// </summary>
        /// <param name="distance">Absolute distance.</param>
        public LevenshteinDistanceOptions(int distance) : this(LevenshteinDistanceUnit.Absolute, distance, false, false) { }

        /// <summary>
        /// Initializes the Opertions-Object.
        /// </summary>
        /// <param name="unit">Indicates how the distance is measured.</param>
        /// <param name="distance">Distance based on unit.</param>
        public LevenshteinDistanceOptions(LevenshteinDistanceUnit unit, int distance) : this(unit, distance, false, false) { }

        /// <summary>
        /// Initializes the Opertions-Object.
        /// </summary>
        /// <param name="unit">Indicates how the distance is measured.</param>
        /// <param name="distance">Distance based on unit.</param>
        /// <param name="removeAllDigits">Enables digit removal.</param>
        /// <param name="removeStandardFormattedGuid">Enables guid removal.</param>
        public LevenshteinDistanceOptions(LevenshteinDistanceUnit unit, int distance, bool removeAllDigits = false, bool removeStandardFormattedGuid = false)
        {
            this.Unit = unit;
            this.Distance = distance;
            this.RemoveAllDigits = removeAllDigits;
            this.RemoveStandardFormattedGuid = removeStandardFormattedGuid;
        }
    }

    /// <summary>
    /// Objects of this class will be returned as a result of the group by clause.
    /// </summary>
    public class LevenshteinDistanceGroupByResult<TElement> : IGrouping<string, TElement>
    {
        /// <summary>
        /// Ruft den Schlüssel von <see cref="T:System.Linq.IGrouping`2" /> ab.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Items which are grouped below the <see cref="Key"/>
        /// </summary>
        public IEnumerable<TElement> Items { get; internal set; }

        public LevenshteinDistanceGroupByResult(string key, IEnumerable<TElement> items)
        {
            this.Key = key;
            this.Items = items;
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return ((IEnumerable<TElement>)Items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Items).GetEnumerator();
        }
    }

    /// <summary>
    /// Linq extension class. (Main class of this project)
    /// </summary>
    public static class LevenshteinDistanceLinqExtension
    {
        public static IEnumerable<IGrouping<string, TSource>> GroupBy<TSource>(
            this IEnumerable<TSource> source, 
            LevenshteinDistanceOptions options)
        {
            return LevenshteinDistanceLinqExtension.GroupBy<TSource, TSource,IGrouping <string, TSource>>(
                source: source, 
                options: options, 
                keySelector: x => x.ToString(), 
                elementSelector: x => x, 
                resultSelector: (key, elements) => new LevenshteinDistanceGroupByResult<TSource>(key, elements));
        }

        public static IEnumerable<IGrouping<string, TSource>> GroupBy<TSource>(
            this IEnumerable<TSource> source, 
            LevenshteinDistanceOptions options, 
            Func<TSource, string> keySelector)
        {
            return LevenshteinDistanceLinqExtension.GroupBy<TSource, TSource, IGrouping<string, TSource>>(
                source: source,
                options: options,
                keySelector: keySelector,
                elementSelector: x => x,
                resultSelector: (key, elements) => new LevenshteinDistanceGroupByResult<TSource>(key, elements));
        }

        public static IEnumerable<IGrouping<string, TElement>> GroupBy<TSource, TElement>(
            this IEnumerable<TSource> source,
            LevenshteinDistanceOptions options,
            Func<TSource, string> keySelector,
            Func<TSource, TElement> elementSelector)
        {
            return LevenshteinDistanceLinqExtension.GroupBy<TSource, TElement, IGrouping<string, TElement>>(
                source: source,
                options: options,
                keySelector: keySelector,
                elementSelector: elementSelector,
                resultSelector: (key, elements) => new LevenshteinDistanceGroupByResult<TElement>(key, elements));
        }

        /// <summary>
        /// Groups the elements of a string sequence similar to the already existing GroupBy functions in linq
        /// but here you can configure a tolerance which is measured in Levenshtein Distance (LD). For additional
        /// information about LD see: https://en.wikipedia.org/wiki/Levenshtein_distance .
        /// 
        /// The grouping works as follows:
        /// * iterate over the items
        /// * store it if it is different than the previous items (count = 1)
        /// * if there is already a similar item (inside configured LD) then add item to list
        /// 
        /// keySelector from linq is always the call of ToString()
        /// comparer is the LD comparer.
        /// result returns always the string values
        /// 
        /// problem: 
        /// * different sorting can lead to different solutions.
        /// </summary>
        /// <param name="source">The list to group.</param>
        /// <param name="options">The grouping-options.</param>
        /// <param name="keySelector">Function to extract a key.</param>
        /// <param name="elementSelector"></param>
        /// <returns></returns>
        public static IEnumerable<TResult> GroupBy<TSource, TElement, TResult>(
            this IEnumerable<TSource> source, 
            LevenshteinDistanceOptions options, 
            Func<TSource, string> keySelector, 
            Func<TSource, TElement> elementSelector, 
            Func<string, IEnumerable<TElement>, TResult> resultSelector)
        {
            var store = new Dictionary<string, List<TElement>>();
            var keys = new List<string>();

            // create tuples, to before to be able to sort... 
            foreach (Tuple<string, TElement> rawItem in source.Select(x => Tuple.Create(keySelector(x), elementSelector(x))).OrderBy(x => x.Item1))
            {
                string key = rawItem.Item1.RemoveGuid(options.RemoveStandardFormattedGuid).RemoveDigits(options.RemoveAllDigits);

                bool matchingGroupKeyFound = false;
                foreach (var groupKey in keys)
                {
                    int distanceAllowed = options.Unit == LevenshteinDistanceUnit.Absolute
                        ? options.Distance
                        : (int)((double)Math.Max(key.Length, groupKey.Length) * (double)options.Distance / 100.00);

                    if (matchingGroupKeyFound = (IsLDInRange(key, groupKey, distanceAllowed)))
                    {
                        store[groupKey].Add(rawItem.Item2);
                        break;
                    }
                }

                if (!matchingGroupKeyFound)
                {
                    // add in revert order, because this makes ways shorter if 
                    // the distance is not already in the first place.
                    keys.Insert(0, key);
                    store.Add(key, new List<TElement>() { rawItem.Item2 });
                }
            }

            return store
                .Select(x => new LevenshteinDistanceGroupByResult<TElement>(x.Key, x.Value))
                .Select(x => resultSelector(x.Key, x.Items));
        }

        /// <summary>
        /// Removes . - and digits in the string.
        /// </summary>
        /// <param name="text">Text to replace in.</param>
        /// <param name="shouldRemoveDigits">Is function activated.</param>
        /// <returns>Replaced text if activated or original string.</returns>
        private static string RemoveDigits(this string text, bool shouldRemoveDigits)
        {
            if (!shouldRemoveDigits) return text;

            return Text.RegularExpressions.Regex.Replace(text, "[-.,0-9]*", "");
        }

        /// <summary>
        /// Removes all guids in the string with format 8-4-4-4-12 hex chars.
        /// </summary>
        /// <remarks>- is optional, not case-sensitive for A-F.</remarks>
        /// <param name="text">Text to replace in.</param>
        /// <param name="shouldRemoveGuid">Is function activated.</param>
        /// <returns>Replaced text if activated or original string.</returns>
        private static string RemoveGuid(this string text, bool shouldRemoveGuid)
        {
            if (!shouldRemoveGuid) return text;

            return Text.RegularExpressions.Regex.Replace(text, @"[A-Fa-f0-9]{8}([-]?[A-Fa-f0-9]{4}){3}[-]?[A-Fa-f0-9]{12}", "");
        }

        /// <summary>
        /// Math.Min with 3 parameters instead of 2.
        /// </summary>
        /// <param name="a">The first number.</param>
        /// <param name="b">The second number.</param>
        /// <param name="c">The third number.</param>
        /// <returns>The lowest number.</returns>
        private static int Min(int a, int b, int c)
        {
            return Math.Min(Math.Min(a, b), c);
        }

        /// <summary>
        /// Checks whether 2 numbers are inside a given LD.
        /// </summary>
        /// <param name="a">First string.</param>
        /// <param name="b">Second string.</param>
        /// <param name="allowedDistance">Absolute distance.</param>
        /// <returns>Whether a and b is just allowedDistnace different.</returns>
        private static bool IsLDInRange(string a, string b, int allowedDistance)
        {
            if (allowedDistance == 0)
            {
                return a.Equals(b);
            }

            return CalculateLD(a, b) <= allowedDistance;
        }

        /// <summary>
        /// Calculates the Levenshtein distance.
        /// </summary>
        /// <remarks>
        /// https://en.wikipedia.org/wiki/Levenshtein_distance
        /// </remarks>
        /// <param name="a">First of the strings to compare.</param>
        /// <param name="b">Second of the strings to compare.</param>
        /// <returns>The distance.</returns>
        private static int CalculateLD(string a, string b)
        {
            if (IsLDEdgeCase(a, b, out var distance))
            {
                return distance;
            }

            var data = GenerateWorkArray(a, b);
            InitializeFirstAxisXY(data, a, b);
            CalculateWorkArrayDistances(data, a, b);
            return GetFinalDistanceFromWorkArray(data);
        }

        /// <summary>
        /// Check parameters for edge cases (null or empty).
        /// </summary>
        /// <param name="a">First string.</param>
        /// <param name="b">Second string.</param>
        /// <param name="distance">The LD if an edge case applies.</param>
        /// <returns>Whether it is an edge case or not.</returns>
        private static bool IsLDEdgeCase(string a, string b, out int distance)
        {
            distance = 0;

            if (string.IsNullOrEmpty(a))
            {
                if (!string.IsNullOrEmpty(b))
                {
                    distance = b.Length;
                }

                return true;
            }

            if (string.IsNullOrEmpty(b))
            {
                if (!string.IsNullOrEmpty(a))
                {
                    distance = a.Length;
                }

                return true;
            }

            if (a.Equals(b))
            {
                distance = 0;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Generates an array to work on.
        /// </summary>
        /// <param name="a">First string.</param>
        /// <param name="b">Second string.</param>
        /// <returns>An array of integers.</returns>
        private static int[,] GenerateWorkArray(string a, string b)
        {
            return new int[a.Length + 1, b.Length + 1];
        }

        /// <summary>
        /// Initializes the first X and Y axis of the work-array.
        /// </summary>
        /// <param name="data">The work-array.</param>
        /// <param name="a">First string.</param>
        /// <param name="b">Second string.</param>
        /// <returns>The data array.</returns>
        private static int[,] InitializeFirstAxisXY(int[,] data, string a, string b)
        {
            for (int i = 0; i < a.Length + 1; i++)
            {
                data[i, 0] = i;
            }

            for (int i = 0; i < b.Length + 1; i++)
            {
                data[0, i] = i;
            }

            return data;
        }

        private static int[,] CalculateWorkArrayDistances(int[,] data, string a, string b)
        {
            for (int i = 1; i <= data.GetUpperBound(0); i++)
            {
                for (int j = 1; j <= data.GetUpperBound(1); j++)
                {
                    data[i, j] = Min(data[i - 1, j - 0] + 1,                                  // left + 1
                                     data[i - 0, j - 1] + 1,                                  // above + 1
                                     data[i - 1, j - 1] + ((a[i - 1] != b[j - 1]) ? 1 : 0));  // the current series; if change then add 1 
                }
            }

            return data;
        }

        /// <summary>
        /// Gets the LD from the work array.
        /// </summary>
        /// <param name="data">Work array.</param>
        /// <returns>The LD.</returns>
        private static int GetFinalDistanceFromWorkArray(int[,] data)
        {
            return data[data.GetUpperBound(0), data.GetUpperBound(1)]; // right, lower corner
        }
    }
}
