namespace NServiceBus
{
    using Pipeline;

    /// <summary>
    /// Allows users to control serialization.
    /// </summary>
    public static class SerializationContextExtensions
    {
        /// <summary>
        /// Requests the serializer to skip serializing the message.
        /// </summary>
        /// <remarks>
        /// This can be used by an extension point needs to take control of the serialization.
        /// For example the Callbacks implementation that skips serialization and instead uses
        /// headers for passing the enum or int value.
        /// </remarks>
        public static void SkipSerialization(this IOutgoingLogicalMessageContext context)
        {
            Guard.AgainstNull(nameof(context), context);

            // Prefix the setting key with the current message id to prevent the setting from leaking to nested send operations for different messages
            context.Extensions.Set($"{context.MessageId}:MessageSerialization.Skip", true);
        }

        /// <summary>
        /// The serializer should skip serializing the message.
        /// </summary>
        public static bool ShouldSkipSerialization(this IOutgoingLogicalMessageContext context)
        {
            Guard.AgainstNull(nameof(context), context);
            if (context.Extensions.TryGet($"{context.Message}:MessageSerialization.Skip", out bool shouldSkipSerialization))
            {
                return shouldSkipSerialization;
            }
            return false;
        }
    }
}