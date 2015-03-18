namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using NServiceBus.Unicast.Behaviors;
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// A behavior that belongs to the handling stage
    /// </summary>
    public abstract class HandlingStageBehavior : Behavior<HandlingStageBehavior.Context>
    {
        /// <summary>
        /// A context of handling a logical message by a handler
        /// </summary>
        public class Context : IncomingContext
        {
            internal Context(MessageHandler handler, LogicalMessageProcessingStageBehavior.Context parentContext)
                : base(parentContext)
            {
                MessageHandler = handler;
                Headers = parentContext.Headers;
                MessageBeingHandled = parentContext.IncomingLogicalMessage.Instance;
                MessageMetadata = parentContext.IncomingLogicalMessage.Metadata;
                MessageId = parentContext.PhysicalMessage.Id;
            }

            /// <summary>
            /// Allows context inheritence
            /// </summary>
            /// <param name="context"></param>
            protected Context(BehaviorContext context)
                : base(context)
            {
            }

            /// <summary>
            /// The current <see cref="IHandleMessages{T}"/> being executed.
            /// </summary>
            public MessageHandler MessageHandler { get; private set; }

            /// <summary>
            /// Message headers
            /// </summary>
            public Dictionary<string,string> Headers{ get; private set; }

            /// <summary>
            /// The message instance beeing handled
            /// </summary>
            public object MessageBeingHandled { get; private set; }

            /// <summary>
            /// Call this to stop the invocation of handlers.
            /// </summary>
            public void DoNotInvokeAnyMoreHandlers()
            {
                HandlerInvocationAborted = true;
            }

            /// <summary>
            /// <code>true</code> if DoNotInvokeAnyMoreHandlers has been called.
            /// </summary>
            public bool HandlerInvocationAborted { get; private set; }

            /// <summary>
            /// Metadata for the incoming message
            /// </summary>
            public MessageMetadata MessageMetadata { get; private set; }

            /// <summary>
            /// Id of the incoming message
            /// </summary>
            public string MessageId { get; private set; }
        }
    }
}