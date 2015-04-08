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
                Headers = parentContext.Headers;
                MessageId = parentContext.MessageId;
            }

            /// <summary>
            /// 
            /// </summary>
            public DeliveryMessageOptions DeliveryMessageOptions { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            public byte[] Body { get; set; }


            /// <summary>
            ///     Gets other applicative out-of-band information.
            /// </summary>
            public Dictionary<string, string> Headers { get; private set; }

            /// <summary>
            /// This id of this message
            /// </summary>
            public string MessageId { get; private set; }
        }
    }
}