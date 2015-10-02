namespace NServiceBus.OutgoingPipeline
{
    using System.Collections.Generic;
    using NServiceBus.Routing;
    using Pipeline;
    using Pipeline.Contexts;

    /// <summary>
    /// Represent the part of the outgoing pipeline where the message has been serialized to a byte[].
    /// </summary>
    public class OutgoingPhysicalMessageContext : BehaviorContext
    {

        /// <summary>
        /// Initializes an instance of the context.
        /// </summary>
        public OutgoingPhysicalMessageContext(byte[] body, IReadOnlyCollection<AddressLabel> addressLabels, OutgoingLogicalMessageContext parentContext)
            : base(parentContext)
        {
            Body = body;
            AddressLabels = addressLabels;
        }


        /// <summary>
        /// The serialized body of the outgoing message.
        /// </summary>
        /// <summary>
        /// A <see cref="byte"/> array containing the serialized contents of the outgoing message.
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// The address labels for this message.
        /// </summary>
        public IReadOnlyCollection<AddressLabel> AddressLabels { get; } 
    }
}