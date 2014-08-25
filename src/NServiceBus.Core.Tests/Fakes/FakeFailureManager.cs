namespace NServiceBus.Core.Tests.Fakes
{
    using System;
    using Faults;

    public class FakeFailureManager : IManageMessageFailures
    {
        public void SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            
        }

        public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            
        }

        public void Init(string address)
        {
            
        }
    }
}