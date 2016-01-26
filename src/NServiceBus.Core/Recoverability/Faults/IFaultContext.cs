namespace NServiceBus.Pipeline
{
    using System;
    using NServiceBus.Transports;

    /// <summary>
    /// Provide context to behaviors on the error pipeline.
    /// </summary>
    public interface IFaultContext : IBehaviorContext
    {
        /// <summary>
        /// The message to which error relates to.
        /// </summary>
        OutgoingMessage Message { get; }

        /// <summary>
        /// Address of the error queue.
        /// </summary>
        string ErrorQueueAddress { get; }

        /// <summary>
        /// Exception that occurred while processing the message.
        /// </summary>
        Exception Exception { get; }

        /// <summary>
        /// Adds information about faults related to current message.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        void AddFaultData(string key, string value);
    }
}