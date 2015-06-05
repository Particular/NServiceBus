namespace NServiceBus
{
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Extensions members to give access use adn manipulate the correlation id.
    /// </summary>
    public static class CorrelationContextExtensions
    {
        /// <summary>
        /// Retrieves the <see cref="Headers.CorrelationId"/> header from the <see cref="LogicalMessagesProcessingStageBehavior.Context"/>.
        /// </summary>
        public static string GetCorrelationId(this LogicalMessagesProcessingStageBehavior.Context context)
        {
            string correlationId;

            if(context.PhysicalMessage.Headers.TryGetValue(Headers.CorrelationId,out correlationId))
            {
                return correlationId;
            }

            return null;
        }
        /// <summary>
        /// Allows users to set a custom correlation id
        /// </summary>
        /// <param name="options">The instance of <see cref="SendOptions"/> to extend.</param>
        /// <param name="correlationId">The custom correlation id</param>
        public static void SetCorrelationId(this SendOptions options,string correlationId)
        {
            Guard.AgainstNullAndEmpty(correlationId, "correlationId");

            options.Extensions.GetOrCreate<AttachCorrelationIdBehavior.State>()
                .CustomCorrelationId = correlationId;
        }

        /// <summary>
        /// Allows users to set a custom correlation id
        /// </summary>
        /// <param name="options">The instance of <see cref="SendOptions"/> to extend.</param>
        /// <param name="correlationId">The custom correlation id</param>
        public static void SetCorrelationId(this SendLocalOptions options, string correlationId)
        {
            Guard.AgainstNullAndEmpty(correlationId, "correlationId");

            options.Extensions.GetOrCreate<AttachCorrelationIdBehavior.State>()
                .CustomCorrelationId = correlationId;
        }
    }
}