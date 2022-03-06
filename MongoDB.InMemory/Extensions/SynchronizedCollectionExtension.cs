using System;
using System.Collections.Generic;

namespace MongoDB.InMemory.Extensions
{
    internal static class SynchronizedCollectionExtension
    {
        public static int FindIndex<T>(this SynchronizedCollection<T> collection, Func<T, bool> predicate)
        {
            var idx = 0;
            foreach (var item in collection)
            {
                idx++;
                if (predicate(item))
                    return idx;
            }

            return -1;
        }
    }
}