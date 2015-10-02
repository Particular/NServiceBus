namespace NServiceBus.OutgoingPipeline
{
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
        public OutgoingPhysicalMessageContext(byte[] body, OutgoingLogicalMessageContext parentContext)
            : base(parentContext)
        {
            Body = body;
        }


        /// <summary>
        /// The serialized body of the outgoing message.
        /// </summary>
        /// <summary>
        /// A <see cref="byte"/> array containing the serialized contents of the outgoing message.
        /// </summary>
        public byte[] Body { get; set; }
    }
}