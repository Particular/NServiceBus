namespace NServiceBus.Pipeline.Contexts
{
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// Provides ways to access the current logical message being processed
    /// </summary>
    public static class LogicalMessagesContextExtensions
    {
        /// <summary>
        /// The current logical message being processed.
        /// </summary>
        public static LogicalMessage GetIncomingLogicalMessage(this LogicalMessageProcessingStageBehavior.Context context)
        {
            return context.Get<LogicalMessage>();
        }
    }
}