namespace NServiceBus
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Common date time extensions.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Converts the <see cref="DateTime" /> to a <see cref="string" /> suitable for transport over the wire.
        /// </summary>
        public static string ToWireFormattedString(DateTime dateTime)
        {
            return dateTime.ToUniversalTime().ToString(format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a wire formatted <see cref="string" /> from <see cref="ToWireFormattedString" /> to a UTC
        /// <see cref="DateTime" />.
        /// </summary>
        public static DateTime ToUtcDateTime(string wireFormattedString)
        {
            Guard.AgainstNullAndEmpty(nameof(wireFormattedString), wireFormattedString);

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
                        year = year * 10 + (digit - '0');
                        break;

                    case 'M':
                        month = month * 10 + (digit - '0');
                        break;

                    case 'd':
                        day = day * 10 + (digit - '0');
                        break;

                    case 'H':
                        hour = hour * 10 + (digit - '0');
                        break;

                    case 'm':
                        minute = minute * 10 + (digit - '0');
                        break;

                    case 's':
                        second = second * 10 + (digit - '0');
                        break;

                    case 'f':
                        microSecond = microSecond * 10 + (digit - '0');
                        break;
                }
            }

            return new DateTime(year, month, day, hour, minute, second, microSecond / 1000, DateTimeKind.Utc);
        }

        const string format = "yyyy-MM-dd HH:mm:ss:ffffff Z";
    }
}