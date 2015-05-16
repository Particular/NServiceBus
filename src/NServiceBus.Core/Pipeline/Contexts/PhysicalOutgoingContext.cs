namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using NServiceBus.Extensibility;

    /// <summary>
    /// Represent the part of the outgoing pipeline where the message has been serialized to a byte[]
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
                MessageType = parentContext.MessageType;
                Extensions = parentContext.Extensions;
            }

            /// <summary>
            /// The logical message type
            /// </summary>
            public Type MessageType { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            /// <summary>
            /// A <see cref="byte"/> array containing the serialized contents of the outgoing message.
            /// </summary>
            public byte[] Body { get; set; }

            /// <summary>
            /// Place for extensions to store their data
            /// </summary>
            public OptionExtensionContext Extensions { get; private set; }
        }
    }
}