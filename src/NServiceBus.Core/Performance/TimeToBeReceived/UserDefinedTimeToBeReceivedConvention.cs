namespace NServiceBus
{
    using System;

    class UserDefinedTimeToBeReceivedConvention
    {
        public Func<Type, TimeSpan> GetTimeToBeReceivedForMessage { get; private set; }

        public UserDefinedTimeToBeReceivedConvention(Func<Type, TimeSpan> retrieveTimeToBeReceived)
        {
            GetTimeToBeReceivedForMessage = retrieveTimeToBeReceived;
        }
    }
}