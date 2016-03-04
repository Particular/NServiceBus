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

        /// <summary>
        /// Retrieves the correlation id specified by the user by using
        /// <see cref="SetCorrelationId(NServiceBus.SendOptions,string)" />.
        /// </summary>
        /// <param name="options">Options being extended.</param>
        /// <returns>The configured correlation id or <c>null</c> when no correlation id was configured.</returns>
        public static string GetCorrelationId(this SendOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            AttachCorrelationIdBehavior.State state;
            options.Context.TryGet(out state);

            return state?.CustomCorrelationId;
        }

        /// <summary>
        /// Retrieves the correlation id specified by the user by using
        /// <see cref="SetCorrelationId(NServiceBus.ReplyOptions,string)" />.
        /// </summary>
        /// <param name="options">Options being extended.</param>
        /// <returns>The configured correlation id or <c>null</c> when no correlation id was configured.</returns>
        public static string GetCorrelationId(this ReplyOptions options)
        {
            Guard.AgainstNull(nameof(options), options);

            AttachCorrelationIdBehavior.State state;
            options.Context.TryGet(out state);

            return state?.CustomCorrelationId;
        }
    }
}