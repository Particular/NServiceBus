namespace NServiceBus;

using System;

/// <summary>
/// Common date time extensions.
/// </summary>
public static class DateTimeOffsetHelper
{
    /// <summary>
    /// Converts the <see cref="DateTimeOffset" /> to a <see cref="string" /> suitable for transport over the wire.
    /// </summary>
    public static string ToWireFormattedString(DateTimeOffset dateTime)
    {
        // Formatted by hand rather than via ToString(format): a custom format string is reparsed on every
        // call, and that parsing dominates the cost. The layout here is fixed and ASCII, so the digits can be
        // written straight into the string buffer instead. This mirrors how the framework formats its own
        // standard date formats (see DateTimeFormat.TryFormatO), including writing each two digit field from
        // a lookup table rather than dividing per digit. ToDateTimeOffset below already parses by hand for
        // the same reason.
        return string.Create(FormatLength, dateTime.ToUniversalTime(), static (destination, utc) =>
        {
            var (yearHigh, yearLow) = Math.DivRem((uint)utc.Year, 100);
            WriteTwoDigits(destination[..2], yearHigh);
            WriteTwoDigits(destination.Slice(2, 2), yearLow);
            destination[4] = '-';
            WriteTwoDigits(destination.Slice(5, 2), (uint)utc.Month);
            destination[7] = '-';
            WriteTwoDigits(destination.Slice(8, 2), (uint)utc.Day);
            destination[10] = ' ';
            WriteTwoDigits(destination.Slice(11, 2), (uint)utc.Hour);
            destination[13] = ':';
            WriteTwoDigits(destination.Slice(14, 2), (uint)utc.Minute);
            destination[16] = ':';
            WriteTwoDigits(destination.Slice(17, 2), (uint)utc.Second);
            destination[19] = ':';

            var microseconds = (uint)(utc.Ticks % TimeSpan.TicksPerSecond / 10);
            var (microHigh, microRest) = Math.DivRem(microseconds, 10000);
            var (microMid, microLow) = Math.DivRem(microRest, 100);
            WriteTwoDigits(destination.Slice(20, 2), microHigh);
            WriteTwoDigits(destination.Slice(22, 2), microMid);
            WriteTwoDigits(destination.Slice(24, 2), microLow);

            destination[26] = ' ';
            destination[27] = 'Z';
        });

        // Both characters come from the table in one copy, which the JIT turns into a single 4 byte move.
        static void WriteTwoDigits(Span<char> destination, uint value) => TwoDigits.Slice((int)value * 2, 2).CopyTo(destination);
    }

    // Every two digit value from "00" to "99", indexed by value * 2. Spanning a literal allocates nothing.
    static ReadOnlySpan<char> TwoDigits =>
        "00010203040506070809" +
        "10111213141516171819" +
        "20212223242526272829" +
        "30313233343536373839" +
        "40414243444546474849" +
        "50515253545556575859" +
        "60616263646566676869" +
        "70717273747576777879" +
        "80818283848586878889" +
        "90919293949596979899";

    /// <summary>
    /// Converts a wire formatted <see cref="string" /> from <see cref="ToWireFormattedString" /> to a UTC
    /// <see cref="DateTimeOffset" />.
    /// </summary>
    public static DateTimeOffset ToDateTimeOffset(string wireFormattedString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wireFormattedString);

        if (wireFormattedString.Length != format.Length)
        {
            throw new FormatException(errorMessage);
        }

        var year = 0;
        var month = 0;
        var day = 0;
        var hour = 0;
        var minute = 0;
        var second = 0;
        var microSecond = 0;

        for (var i = 0; i < format.Length; i++)
        {
            var digit = wireFormattedString[i];

            switch (format[i])
            {
                case 'y':
                    if (digit is < '0' or > '9')
                    {
                        throw new FormatException(errorMessage);
                    }

                    year = (year * 10) + (digit - '0');
                    break;

                case 'M':
                    if (digit is < '0' or > '9')
                    {
                        throw new FormatException(errorMessage);
                    }

                    month = (month * 10) + (digit - '0');
                    break;

                case 'd':
                    if (digit is < '0' or > '9')
                    {
                        throw new FormatException(errorMessage);
                    }

                    day = (day * 10) + (digit - '0');
                    break;

                case 'H':
                    if (digit is < '0' or > '9')
                    {
                        throw new FormatException(errorMessage);
                    }

                    hour = (hour * 10) + (digit - '0');
                    break;

                case 'm':
                    if (digit is < '0' or > '9')
                    {
                        throw new FormatException(errorMessage);
                    }

                    minute = (minute * 10) + (digit - '0');
                    break;

                case 's':
                    if (digit is < '0' or > '9')
                    {
                        throw new FormatException(errorMessage);
                    }

                    second = (second * 10) + (digit - '0');
                    break;

                case 'f':
                    if (digit is < '0' or > '9')
                    {
                        throw new FormatException(errorMessage);
                    }

                    microSecond = (microSecond * 10) + (digit - '0');
                    break;

                default:
                    break;
            }
        }

        var timestamp = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
        timestamp = timestamp.AddMicroseconds(microSecond);
        return timestamp;
    }

    const string format = "yyyy-MM-dd HH:mm:ss:ffffff Z";
    const int FormatLength = 28; // format.Length, as a constant usable by string.Create
    const string errorMessage = "String was not recognized as a valid DateTime.";
}