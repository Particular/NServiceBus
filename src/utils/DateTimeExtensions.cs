namespace NServiceBus
{
    using System;

    /// <summary>
    /// Common date time extensions
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Converts the date time to a string suitable for transport over the wire
        /// </summary>
        /// <returns></returns>
        public static string ToWireFormat(this DateTime time)
        {
            return time.ToUniversalTime().ToString("yyyy-MM-dd hh:mm:ss:ffffff");
        }
    }
}
