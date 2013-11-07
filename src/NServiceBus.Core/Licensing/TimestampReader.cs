namespace NServiceBus.Licensing
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    internal static class TimestampReader
    {
        public static DateTimeOffset GetBuildTimestamp()
        {
            var attribute = (dynamic)Assembly.GetExecutingAssembly()
                .GetCustomAttributes(false)
                .First(x => x.GetType().Name == "TimestampAttribute");

            string timestamp = attribute.Timestamp;
            return  DateTimeOffset.Parse(timestamp,null,DateTimeStyles.AssumeUniversal);
        }
    }
}