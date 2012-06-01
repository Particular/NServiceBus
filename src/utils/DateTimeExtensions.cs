namespace NServiceBus
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Common date time extensions
    /// </summary>
    public static class DateTimeExtensions
    {
        const string Format = "yyyy-MM-dd HH:mm:ss:ffffff Z";

        /// <summary>
        /// Converts the date time to a string suitable for transport over the wire
        /// </summary>
        /// <returns></returns>
        public static string ToWireFormattedString(this DateTime time)
        {
            return time.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the date time to a string suitable for transport over the wire
        /// </summary>
        /// <returns></returns>
        public static DateTime ToUtcDateTime(this string time)
        {
            return DateTime.ParseExact(time, Format, CultureInfo.InvariantCulture).ToUniversalTime();
        }
    }
}
