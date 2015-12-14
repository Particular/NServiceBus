namespace NServiceBus
{
    /// <summary>
    /// Extension methods for manipulating the message Correlation Id.
    /// </summary>
    public static class CorrelationContextExtensions
    {
        /// <summary>
        /// Allows users to set a custom correlation id.
        /// </summary>
        /// <param name="options">Options being extended.</param>
        /// <param name="correlationId">The custom correlation id.</param>
        public static void SetCorrelationId(this SendOptions options, string correlationId)
        {
            Guard.AgainstNull(nameof(options), options);
            Guard.AgainstNullAndEmpty(nameof(correlationId), correlationId);

            options.Context.GetOrCreate<AttachCorrelationIdBehavior.State>()
                .CustomCorrelationId = correlationId;
        }

        /// <summary>
        /// Allows users to set a custom correlation id.
        /// </summary>
        /// <param name="options">Options being extended.</param>
        /// <param name="correlationId">The custom correlation id.</param>
        public static void SetCorrelationId(this ReplyOptions options, string correlationId)
        {
            Guard.AgainstNull(nameof(options), options);
            Guard.AgainstNullAndEmpty(nameof(correlationId), correlationId);

            options.Context.GetOrCreate<AttachCorrelationIdBehavior.State>()
                .CustomCorrelationId = correlationId;
        }
    }
}