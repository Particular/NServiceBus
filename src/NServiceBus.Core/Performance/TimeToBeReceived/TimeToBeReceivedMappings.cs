namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class TimeToBeReceivedMappings
    {
        public TimeToBeReceivedMappings(IEnumerable<Type> knownMessages, Func<Type, TimeSpan> convention)
        {
            mappings = new Dictionary<Type, TimeSpan>();
   
            foreach (var messageType in knownMessages)
            {
                var timeToBeReceived = convention(messageType);

                if (timeToBeReceived <= TimeSpan.Zero)
                {
                    throw new Exception("TimeToBeReceived must be greater that 0");
                }

                if (timeToBeReceived < TimeSpan.MaxValue)
                {
                    mappings[messageType] = timeToBeReceived;
                }
            }
        }

        public bool HasEntries
        {
            get { return mappings.Any(); }
        }

        public bool TryGetTimeToBeReceived(Type messageType, out TimeSpan timeToBeReceived)
        {
            return mappings.TryGetValue(messageType, out timeToBeReceived);
        }

        public static Func<Type, TimeSpan> DefaultConvention = t =>
        {
            var attributes = t.GetCustomAttributes(typeof(TimeToBeReceivedAttribute), true)
                .Select(s => s as TimeToBeReceivedAttribute)
                .ToList();

            return attributes.Count > 0 ? attributes.Last().TimeToBeReceived : TimeSpan.MaxValue;
        };
      
        Dictionary<Type, TimeSpan> mappings;
    }
}