#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

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
        timeToBeReceived = mappings.GetOrAdd(messageType, static (type, @this) => GetTimeToBeReceived(@this.convention, type, @this.doesTransportSupportDiscardIfNotReceivedBefore), this);
        return timeToBeReceived != TimeSpan.MaxValue;
    }

    static TimeSpan GetTimeToBeReceived(Func<Type, TimeSpan> convention, Type messageType, bool doesTransportSupportDiscardIfNotReceivedBefore)
    {
        var timeToBeReceived = convention(messageType);

        if (timeToBeReceived < TimeSpan.MaxValue && !doesTransportSupportDiscardIfNotReceivedBefore)
        {
            throw new Exception("Messages with TimeToBeReceived found but the selected transport does not support this type of restriction. Remove TTBR from messages or select a transport that does support TTBR");
        }

        return timeToBeReceived <= TimeSpan.Zero ? throw new Exception("TimeToBeReceived must be greater that 0") : timeToBeReceived;
    }

    readonly ConcurrentDictionary<Type, TimeSpan> mappings;

    readonly Func<Type, TimeSpan> convention;

    readonly bool doesTransportSupportDiscardIfNotReceivedBefore;

    public static readonly Func<Type, TimeSpan> DefaultConvention = t =>
    {
        var timeToBeReceivedAttribute = t.GetCustomAttribute<TimeToBeReceivedAttribute>(true);
        return timeToBeReceivedAttribute?.TimeToBeReceived ?? TimeSpan.MaxValue;
    };
}
