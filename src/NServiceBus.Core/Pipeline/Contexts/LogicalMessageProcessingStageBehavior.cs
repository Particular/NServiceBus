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
            const string IncomingLogicalMessageKey = "NServiceBus.IncomingLogicalMessageKey";

            /// <summary>
            /// Creates new instance.
            /// </summary>
            /// <param name="logicalMessage">The logical message</param>
            /// <param name="headers">The headers for the incoming message</param>
            /// <param name="messageType">The message type</param>
            /// <param name="parentContext">The wrapped context</param>
            public Context(LogicalMessage logicalMessage,Dictionary<string,string> headers,Type messageType, LogicalMessagesProcessingStageBehavior.Context parentContext)
                : base(parentContext)
            {
                Headers = headers;
                MessageType = messageType;
                IncomingLogicalMessage = logicalMessage;

                if (parentContext != null)
                {
                    MessageHandled = parentContext.MessageHandled;
                }
            }

            /// <summary>
            /// Allows context inheritance
            /// </summary>
            protected Context(BehaviorContext parentContext)
                : base(parentContext)
            {
            }

            /// <summary>
            /// The current logical message being processed.
            /// </summary>
            public LogicalMessage IncomingLogicalMessage
            {
                get { return Get<LogicalMessage>(IncomingLogicalMessageKey); }
                private set { Set(IncomingLogicalMessageKey, value); }
            }


            /// <summary>
            ///    Headers for the incoming message
            /// </summary>
            public Dictionary<string, string> Headers { get; private set; }

            /// <summary>
            /// The message type of the message being processed
            /// </summary>
            public Type MessageType { get; set; }
        }
    }
}