#nullable enable

namespace NServiceBus
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;

    static class Guard
    {
        public static void ThrowIfNegativeOrZero(TimeSpan argument, [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (argument <= TimeSpan.Zero)
            {
                ThrowArgumentOutOfRangeException(paramName);
            }
        }

        public static void ThrowIfNegative(TimeSpan argument, [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (argument < TimeSpan.Zero)
            {
                ThrowArgumentOutOfRangeException(paramName);
            }
        }

        [DoesNotReturn]
        static void ThrowArgumentOutOfRangeException(string? paramName)
            => throw new ArgumentOutOfRangeException(paramName);
    }
}