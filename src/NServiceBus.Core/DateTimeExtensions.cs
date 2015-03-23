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
        /// Converts the <see cref="DateTime"/> to a <see cref="string"/> suitable for transport over the wire
        /// </summary>
        public static string ToWireFormattedString(DateTime dateTime)
        {
            return dateTime.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a wire formatted <see cref="string"/> from <see cref="ToWireFormattedString"/> to a UTC <see cref="DateTime"/>
        /// </summary>
        public static DateTime ToUtcDateTime(string wireFormattedString)
        {
            Guard.AgainstNullAndEmpty(wireFormattedString, "wireFormattedString");
            return DateTime.ParseExact(wireFormattedString, Format, CultureInfo.InvariantCulture).ToUniversalTime();
        }
    }
}
