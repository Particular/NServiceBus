#nullable enable

namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;

    static class Guard
    {
        public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (argument is null)
            {
                ThrowArgumentNullException(paramName);
            }
        }

        public static void ThrowIfNullOrEmpty(string argument, [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                ThrowArgumentNullException(paramName);
            }
        }

        public static void ThrowIfNullOrEmpty([NotNull] ICollection? argument, [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (argument is null)
            {
                ThrowArgumentNullException(paramName);
            }
            if (argument.Count == 0)
            {
                ThrowArgumentOutOfRangeException(paramName);
            }
        }

        [DoesNotReturn]
        static void ThrowArgumentNullException(string? paramName)
            => throw new ArgumentNullException(paramName);

        public static void ThrowIfNegativeOrZero(TimeSpan argument, [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (argument <= TimeSpan.Zero)
            {
                ThrowArgumentOutOfRangeException(paramName);
            }
        }

        public static void ThrowIfNegativeOrZero(int argument, [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (argument <= 0)
            {
                ThrowArgumentOutOfRangeException(paramName);
            }
        }

        public static void ThrowIfNegative(int argument, [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (argument < 0)
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