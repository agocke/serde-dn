
using System;
using System.Collections.Generic;
using System.Linq;

namespace Serde
{
    internal static class ExceptionUtilities
    {
        public static Exception Unreachable => new InvalidOperationException("This location is thought to be unreachable");

        public static Exception UnexpectedValue<T>(T t) => new InvalidOperationException($"Value {t} was unexpected");
    }

    internal static class Utilities
    {
        public static void Deconstruct<K, V>(this KeyValuePair<K, V> kvp, out K key, out V value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }

        public static IEnumerable<U> SelectNotNull<T, U>(this IEnumerable<T> en, Func<T, U?> f) where U : class
        {
            foreach (var item in en.Select(f))
            {
                if (item is not null)
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<U> SelectNotNull<T, U>(this IEnumerable<T> en, Func<T, U?> f) where U : struct
        {
            foreach (var item in en.Select(f))
            {
                if (item is U u)
                {
                    yield return u;
                }
            }
        }
    }
}