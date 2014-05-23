namespace NServiceBus.Faults.InMemory
{
    using System;
    using Logging;

    /// <summary>
    /// Logging implementation of IManageMessageFailures.
    /// </summary>
    public class FaultManager : IManageMessageFailures
    {
        void IManageMessageFailures.SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            logger.Error("Serialization failed for message with ID " + message.Id + ".", e);
        }

        void IManageMessageFailures.ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            logger.Error("Message processing always fails for message with ID " + message.Id + ".", e);
        }

        /// <summary>
        /// Initializes the fault manager
        /// </summary>
        /// <param name="address">The address of the message source</param>
        public void Init(Address address)
        {
            
        }

        static ILog logger = LogManager.GetLogger<FaultManager>();
    }
}
