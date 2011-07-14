using System;
using NServiceBus.Unicast.Transport;
using Common.Logging;

namespace NServiceBus.Faults.InMemory
{
    /// <summary>
    /// Logging implementation of IManageMessageFailures.
    /// </summary>
    public class FaultManager : IManageMessageFailures
    {
        void IManageMessageFailures.SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            Logger.Error("Serialization failed for message with ID " + message.IdForCorrelation + ".", e);
        }

        void IManageMessageFailures.ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            Logger.Error("Message processing always fails for message with ID " + message.IdForCorrelation + ".", e);
        }

        private ILog Logger = LogManager.GetLogger(typeof(FaultManager));
    }
}
