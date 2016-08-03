namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    class TimeToBeReceivedMappings
    {
        public TimeToBeReceivedMappings(IEnumerable<Type> knownMessages, Func<Type, TimeSpan> convention, bool doesTransportSupportDiscardIfNotReceivedBefore)
        {
            this.doesTransportSupportDiscardIfNotReceivedBefore = doesTransportSupportDiscardIfNotReceivedBefore;
            this.convention = convention;

            mappings = new ConcurrentDictionary<Type, TimeSpan>();

            foreach (var messageType in knownMessages)
            {
                mappings[messageType] = GetTimeToBeReceived(convention, messageType, doesTransportSupportDiscardIfNotReceivedBefore);
            }
        }

        public bool TryGetTimeToBeReceived(Type messageType, out TimeSpan timeToBeReceived)
        {
            timeToBeReceived = mappings.GetOrAdd(messageType, type => GetTimeToBeReceived(convention, type, doesTransportSupportDiscardIfNotReceivedBefore));
            return timeToBeReceived != TimeSpan.MaxValue;
        }

        // ReSharper disable once UnusedParameter.Local
        static TimeSpan GetTimeToBeReceived(Func<Type, TimeSpan> convention, Type messageType, bool doesTransportSupportDiscardIfNotReceivedBefore)
        {
            var timeToBeReceived = convention(messageType);

            if (timeToBeReceived < TimeSpan.MaxValue && !doesTransportSupportDiscardIfNotReceivedBefore)
            {
                throw new Exception("Messages with TimeToBeReceived found but the selected transport does not support this type of restriction. Remove TTBR from messages or select a transport that does support TTBR");
            }

            if (timeToBeReceived <= TimeSpan.Zero)
            {
                throw new Exception("TimeToBeReceived must be greater that 0");
            }
            return timeToBeReceived;
        }

        ConcurrentDictionary<Type, TimeSpan> mappings;

        Func<Type, TimeSpan> convention;

        bool doesTransportSupportDiscardIfNotReceivedBefore;

        public static Func<Type, TimeSpan> DefaultConvention = t =>
        {
            var timeToBeReceived = TimeSpan.MaxValue;
            foreach (var customAttribute in t.GetCustomAttributes(typeof(TimeToBeReceivedAttribute), true))
            {
                var attribute = customAttribute as TimeToBeReceivedAttribute;
                if (attribute != null)
                {
                    timeToBeReceived = attribute.TimeToBeReceived;
                }
            }
            return timeToBeReceived;
        };
    }
}