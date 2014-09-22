using System;

namespace NServiceBus.Persistence.FileSystem
{
    using NServiceBus.Timeout.Core;

    static class SerilalizationHelpers
    {
        public static string ToTabbedString(this TimeoutData timeout, string id)
        {
            // This is a simplified version of serialization, as some important fields are left behind
            return string.Format("{0}\t{1}\t{2}\t{3}\t{4}", id, timeout.SagaId, timeout.Time, timeout.Destination, timeout.OwningTimeoutManager);
        }

        public static TimeoutData ToTimeoutData(this string str)
        {
            var items = str.Split(new[] { '\t' });
            return new TimeoutData
            {
                Id = items[0],
                SagaId = Guid.Parse(items[1]),
                Time = DateTime.Parse(items[2]),
                Destination = Address.Parse(items[3]),
                OwningTimeoutManager = items[4],
            };
        }
    }
}
