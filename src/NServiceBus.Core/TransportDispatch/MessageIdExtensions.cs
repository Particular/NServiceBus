namespace NServiceBus
{
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Extensions to the outgoing pipeline
    /// </summary>
    public static class MessageIdExtensions
    {
        /// <summary>
        /// Returns the id for this message
        /// </summary>
        /// <param name="context">Context beeing extended</param>
        /// <returns>The message id</returns>
        public static string GetMessageId(this PhysicalOutgoingContextStageBehavior.Context context)
        {
            return context.Extensions.GetOrCreate<DispatchMessageToTransportBehavior.State>().MessageId;
        }
        /// <summary>
        /// Returns the id for this message
        /// </summary>
        /// <param name="context">Context beeing extended</param>
        /// <returns>The message id</returns>
        public static string GetMessageId(this OutgoingContext context)
        {
            return context.Extensions.GetOrCreate<DispatchMessageToTransportBehavior.State>().MessageId;
        }

        /// <summary>
        /// Allows the user to set the message id
        /// </summary>
        /// <param name="context">Context to extend</param>
        /// <param name="messageId">The message id to use</param>
        public static void SetMessageId(this ExtendableOptions context, string messageId)
        {
            Guard.AgainstNullAndEmpty(messageId,messageId);

            context.Extensions.GetOrCreate<DispatchMessageToTransportBehavior.State>()
                .MessageId = messageId;
        }

    }
}