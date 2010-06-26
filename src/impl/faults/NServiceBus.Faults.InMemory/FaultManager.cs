using System;
using NServiceBus.Unicast.Transport;
using Common.Logging;

namespace NServiceBus.Faults.InMemory
{
    public class FaultManager : IManageMessageFailures
    {
        public void SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            Logger.Error("Serialization failed for message with ID " + message.IdForCorrelation + ".", e);
        }

        public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            Logger.Error("Message processing always fails for message with ID " + message.IdForCorrelation + ".", e);
        }

        private ILog Logger = LogManager.GetLogger(typeof(FaultManager));
    }
}
