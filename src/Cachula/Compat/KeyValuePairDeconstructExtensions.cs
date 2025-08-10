#if NETSTANDARD2_0 || NETSTANDARD2_1 || NET461 || NET48
// ReSharper disable once CheckNamespace
namespace Cachula
{
    internal static class KeyValuePairDeconstructExtensions
    {
        public static void Deconstruct<TKey, TValue>(
            this KeyValuePair<TKey, TValue> kvp,
            out TKey key,
            out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
}
#endif
