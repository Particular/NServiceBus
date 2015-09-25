namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using System.Collections.Generic;
    using Unicast.Messages;

    /// <summary>
    /// A behavior that belongs to logical message processing stage.
    /// </summary>
    public abstract class LogicalMessageProcessingStageBehavior : Behavior<LogicalMessageProcessingStageBehavior.Context>
    {
        /// <summary>
        /// A context of behavior execution in logical message processing stage.
        /// </summary>
        public class Context : IncomingContext
        {
            /// <summary>
            /// Initializes a new instance of <see cref="Context"/>.
            /// </summary>
            /// <param name="logicalMessage">The logical message.</param>
            /// <param name="headers">The headers for the incoming message.</param>
            /// <param name="parentContext">The wrapped context.</param>
            public Context(LogicalMessage logicalMessage,Dictionary<string,string> headers, IncomingContext parentContext)
                : base(parentContext)
            {
                Headers = headers;
                MessageType = logicalMessage.MessageType;
                Set(logicalMessage);
            }

            /// <summary>
            ///    Headers for the incoming message.
            /// </summary>
            public Dictionary<string, string> Headers { get; private set; }

            /// <summary>
            /// The message type of the message being processed.
            /// </summary>
            public Type MessageType { get; set; }

            /// <summary>
            /// Tells if the message has been handled.
            /// </summary>
            public bool MessageHandled { get; set; }
        }
    }
}