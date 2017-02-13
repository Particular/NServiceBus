namespace NServiceBus.Routing
{
    using System.Collections.Generic;
    using Extensibility;
    using Pipeline;

    /// <summary>
    /// The distribution context for the DistributionStrategy implementers.
    /// </summary>
    public class DistributionContext
    {
        /// <summary>
        /// Creates a new distribution context.
        /// </summary>
        public DistributionContext(string[] receiverAddresses, OutgoingLogicalMessage message, string messageId, Dictionary<string, string> headers, ContextBag context)
        {
            ReceiverAddresses = receiverAddresses;
            Message = message;
            MessageId = messageId;
            Headers = headers;
            Context = context;
        }

        /// <summary>
        /// The receiver addresses that can be taken into account for distribution.
        /// </summary>
        public string[] ReceiverAddresses { get; }

        /// <summary>
        /// The id of the outgoing message.
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        /// The headers of the outgoing message.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// The outgoing message.
        /// </summary>
        public OutgoingLogicalMessage Message { get; }

        /// <summary>
        /// The context bag.
        /// </summary>
        public ContextBag Context { get; }
    }
}