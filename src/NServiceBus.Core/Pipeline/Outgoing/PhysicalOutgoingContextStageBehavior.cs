namespace NServiceBus.OutgoingPipeline
{
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Represent the part of the outgoing pipeline where the message has been serialized to a byte[].
    /// </summary>
    public abstract class PhysicalOutgoingContextStageBehavior : Behavior<PhysicalOutgoingContextStageBehavior.Context>
    {
        /// <summary>
        /// The <see cref="BehaviorContext"/> for <see cref="PhysicalOutgoingContextStageBehavior"/>.
        /// </summary>
        public class Context : BehaviorContext
        {

            /// <summary>
            /// Initializes an instance of <see cref="Context"/>.
            /// </summary>
            public Context(byte[] body, OutgoingContext parentContext)
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
}