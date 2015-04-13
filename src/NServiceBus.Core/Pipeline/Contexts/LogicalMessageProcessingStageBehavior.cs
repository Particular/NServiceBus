namespace NServiceBus.Pipeline.Contexts
{
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
            /// <param name="parentContext">The wrapped context</param>
            public Context(LogicalMessage logicalMessage,Dictionary<string,string> headers, LogicalMessagesProcessingStageBehavior.Context parentContext)
                : base(parentContext)
            {
                Headers = headers;
                IncomingLogicalMessage = logicalMessage;

            }

            /// <summary>
            /// Allows context inheritence
            /// </summary>
            /// <param name="parentContext"></param>
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
            /// Tells if this incoming message is a control message
            /// </summary>
            /// <returns></returns>
            public bool IsControlMessage()
            {
                return Headers.ContainsKey(NServiceBus.Headers.ControlMessageHeader);
            }
        }
    }
}