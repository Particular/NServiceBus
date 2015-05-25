namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using NServiceBus.Extensibility;
    using NServiceBus.Unicast;

    /// <summary>
    /// 
    /// </summary>
    public abstract class PhysicalOutgoingContextStageBehavior : Behavior<PhysicalOutgoingContextStageBehavior.Context>
    {
        /// <summary>
        /// 
        /// </summary>
        public class Context : BehaviorContext
        {

            /// <summary>
            /// 
            /// </summary>
            /// <param name="body"></param>
            /// <param name="parentContext"></param>
            public Context(byte[] body, OutgoingContext parentContext)
                : base(parentContext)
            {
                Body = body;
                DeliveryMessageOptions = parentContext.DeliveryMessageOptions;
                MessageType = parentContext.MessageType;
                Intent = parentContext.Intent;
                Extensions = parentContext.Extensions;
            }

            /// <summary>
            /// The logical message type
            /// </summary>
            public Type MessageType { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            public DeliveryMessageOptions DeliveryMessageOptions { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            public byte[] Body { get; set; }

            /// <summary>
            /// The intent of this message
            /// </summary>
            public MessageIntentEnum Intent { get; private set; }

            /// <summary>
            /// Place for extensions to store their data
            /// </summary>
            public OptionExtensionContext Extensions { get; private set; }
        }
    }
}