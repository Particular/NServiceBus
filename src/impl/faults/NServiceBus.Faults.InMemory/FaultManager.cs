using System;
using NServiceBus.Logging;

namespace NServiceBus.Faults.InMemory
{
    /// <summary>
    /// Logging implementation of IManageMessageFailures.
    /// </summary>
    public class FaultManager : IManageMessageFailures
    {
        void IManageMessageFailures.SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            logger.Error("Serialization failed for message with ID " + message.IdForCorrelation + ".", e);
        }

        void IManageMessageFailures.ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            logger.Error("Message processing always fails for message with ID " + message.IdForCorrelation + ".", e);
        }

        /// <summary>
        /// Initializes the fault manager
        /// </summary>
        /// <param name="address">The address of the message source</param>
        public void Init(Address address)
        {
            
        }

        readonly ILog logger = LogManager.GetLogger(typeof(FaultManager));
    }
}
