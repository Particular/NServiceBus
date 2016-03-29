namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// A testable implementation of <see cref="IFaultContext" />.
    /// </summary>
    public class TestableFaultContext : TestableBehaviorContext, IFaultContext
    {
        /// <summary>
        /// Contains data added by <see cref="AddFaultData" />.
        /// </summary>
        public IDictionary<string, string> FaultData { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Adds information about faults related to current message.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public virtual void AddFaultData(string key, string value)
        {
            FaultData.Add(key, value);
        }

        /// <summary>
        /// The message to which error relates to.
        /// </summary>
        public OutgoingMessage Message { get; set; } = new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);

        /// <summary>
        /// Address of the error queue.
        /// </summary>
        public string ErrorQueueAddress { get; set; } = "error queue address";

        /// <summary>
        /// Exception that occurred while processing the message.
        /// </summary>
        public Exception Exception { get; set; } = new Exception("sample exception");
    }
}