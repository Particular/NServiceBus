namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
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
            readonly OutgoingContext parent;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="body"></param>
            /// <param name="parentContext"></param>
            public Context( byte[] body,OutgoingContext parentContext)
                : base(parentContext)
            {
                parent = parentContext;
                Body = body;
            }

            /// <summary>
            /// 
            /// </summary>
            public DeliveryOptions DeliveryOptions { get { return parent.DeliveryOptions; } }

            /// <summary>
            /// 
            /// </summary>
            public byte[] Body { get; set; }


            /// <summary>
            ///     Gets other applicative out-of-band information.
            /// </summary>
            public Dictionary<string, string> Headers { get { return parent.Headers; } }

            /// <summary>
            /// This id of this message
            /// </summary>
            public string MessageId { get { return parent.MessageId; } }
        }
    }
}