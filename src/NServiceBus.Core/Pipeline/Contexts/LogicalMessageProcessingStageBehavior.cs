namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// A behavior that belongs to logical message processing stage.
    /// </summary>
    public abstract class LogicalMessageProcessingStageBehavior : Behavior<LogicalMessageProcessingStageBehavior.Context>
    {
        /// <summary>
        /// A context of behavior execution in logical message processing stage.
        /// </summary>
        public class Context : LogicalMessagesProcessingStageBehavior.Context
        {
            /// <summary>
            /// Initializes a new instance of <see cref="Context"/>.
            /// </summary>
            /// <param name="logicalMessage">The logical message.</param>
            /// <param name="headers">The headers for the incoming message.</param>
            /// <param name="messageType">The message type.</param>
            /// <param name="parentContext">The wrapped context.</param>
            public Context(LogicalMessage logicalMessage,Dictionary<string,string> headers,Type messageType, LogicalMessagesProcessingStageBehavior.Context parentContext)
                : base(parentContext)
            {
                Headers = headers;
                MessageType = messageType;
                Set(logicalMessage);

                if (parentContext != null)
                {
                    MessageHandled = parentContext.MessageHandled;
                }
            }

            /// <summary>
            /// Allows context inheritance.
            /// </summary>
            protected Context(BehaviorContext parentContext)
                : base(parentContext)
            {
            }

            /// <summary>
            ///    Headers for the incoming message.
            /// </summary>
            public Dictionary<string, string> Headers { get; private set; }

            /// <summary>
            /// The message type of the message being processed.
            /// </summary>
            public Type MessageType { get; set; }
        }
    }
}