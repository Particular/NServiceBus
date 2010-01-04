using System;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Faults.InMemory
{
    public class FaultManager : IManageMessageFailures
    {
        public void SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            
        }

        public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            
        }
    }
}
