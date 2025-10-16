using System.Runtime.CompilerServices;

namespace BackgroundThrust.Utils;

internal static class MathUtil
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
}
