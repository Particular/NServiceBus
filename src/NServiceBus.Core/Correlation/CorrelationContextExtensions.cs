namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Extension methods for manipulating the message Correlation Id.
    /// </summary>
    public static class CorrelationContextExtensions
    {
        /// <summary>
        /// Get the Correlation Id from the current <see cref="LogicalMessagesProcessingStageBehavior.Context"/>.
        /// </summary>
        /// <returns>The exisitng <see cref="Headers.CorrelationId"/> if it exists; otherwise null.</returns>
        public static string GetCorrelationId(this LogicalMessagesProcessingStageBehavior.Context context)
        {
            Guard.AgainstNull(context, "context");
     
            string correlationId;
            if (context.GetIncomingPhysicalMessage().Headers.TryGetValue(Headers.CorrelationId, out correlationId))
            {
                return correlationId;
            }
            return null;
        }

        /// <summary>
        /// Allows users to set a custom correlation id.
        /// </summary>
        /// <param name="options">Options being extended.</param>
        /// <param name="correlationId">The custom correlation id.</param>
        public static void SetCorrelationId(this SendOptions options, string correlationId)
        {
            Guard.AgainstNull(options,"options");
            Guard.AgainstNullAndEmpty(correlationId, "correlationId");

            options.Extensions.GetOrCreate<AttachCorrelationIdBehavior.State>()
                .CustomCorrelationId = correlationId;
        }

        /// <summary>
        /// Allows users to set a custom correlation id.
        /// </summary>
        /// <param name="options">Options being extended.</param>
        /// <param name="correlationId">The custom correlation id.</param>
        public static void SetCorrelationId(this ReplyOptions options, string correlationId)
        {
            Guard.AgainstNull(options, "options");
            Guard.AgainstNullAndEmpty(correlationId, "correlationId");

            options.Extensions.GetOrCreate<AttachCorrelationIdBehavior.State>()
                .CustomCorrelationId = correlationId;
        }


        /// <summary>
        /// Allows users to set a custom correlation id.
        /// </summary>
        /// <param name="options">Options being extended.</param>
        /// <param name="correlationId">The custom correlation id.</param>
        public static void SetCorrelationId(this SendLocalOptions options, string correlationId)
        {
            Guard.AgainstNull(options, "options");
            Guard.AgainstNullAndEmpty(correlationId, "correlationId");

            options.Extensions.GetOrCreate<AttachCorrelationIdBehavior.State>()
                .CustomCorrelationId = correlationId;
        }
    }
}