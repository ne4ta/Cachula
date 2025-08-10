#if NETSTANDARD2_0 || NETSTANDARD2_1 || NET461 || NET48
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.Linq
{
    internal static class EnumerableChunkPolyfill
    {
        public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int size)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            T[]? bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                bucket ??= new T[size];
                bucket[count++] = item;
                if (count == size)
                {
                    yield return bucket;
                    bucket = null;
                    count = 0;
                }
            }

            if (bucket != null && count > 0)
            {
                Array.Resize(ref bucket, count);
                yield return bucket;
            }
        }
    }
}
#endif
