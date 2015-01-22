namespace NServiceBus.Pipeline.Contexts
{
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
            /// <param name="parentContext">The wrapped context</param>
            public Context(LogicalMessage logicalMessage, LogicalMessagesProcessingStageBehavior.Context parentContext)
                : base(parentContext)
            {
                IncomingLogicalMessage = logicalMessage;
                Set(new OutgoingHeaders());
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
        }
    }
}