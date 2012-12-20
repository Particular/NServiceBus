namespace NServiceBus.Faults
{
    using System;

    /// <summary>
    /// Interface for defining how message failures will be handled.
    /// </summary>
    public interface IManageMessageFailures
    {
        /// <summary>
        /// Invoked when the deserialization of a message failed.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="e"></param>
        void SerializationFailedForMessage(TransportMessage message, Exception e);

        /// <summary>
        /// Invoked when a message has failed its processing the maximum number of time configured.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="e"></param>
        void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e);

        /// <summary>
        /// Initializes the fault manager
        /// </summary>
        /// <param name="address">The address of the message source</param>
        void Init(Address address);
    }
}
