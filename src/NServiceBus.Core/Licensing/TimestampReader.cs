namespace NServiceBus.Licensing
{
    using System;
    using System.Linq;
    using System.Reflection;

    static class TimestampReader
    {
        public static DateTime GetBuildTimestamp()
        {
            var attribute = (dynamic)Assembly.GetExecutingAssembly()
                .GetCustomAttributes(false)
                .First(x => x.GetType().Name == "TimestampAttribute");

            return UniversalDateParser.Parse((string)attribute.Timestamp);
        }
    }
}