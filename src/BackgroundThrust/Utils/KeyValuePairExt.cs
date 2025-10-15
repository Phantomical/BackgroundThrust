using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BackgroundThrust.Utils;

internal static class KeyValuePairExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deconstruct<K, V>(this KeyValuePair<K, V> pair, out K key, out V value)
    {
        key = pair.Key;
        value = pair.Value;
    }
}
