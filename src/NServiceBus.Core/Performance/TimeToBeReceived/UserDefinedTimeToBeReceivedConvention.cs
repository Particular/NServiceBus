#nullable enable

namespace NServiceBus;

using System;

class UserDefinedTimeToBeReceivedConvention(Func<Type, TimeSpan> retrieveTimeToBeReceived)
{
    public Func<Type, TimeSpan> GetTimeToBeReceivedForMessage { get; } = retrieveTimeToBeReceived;
}