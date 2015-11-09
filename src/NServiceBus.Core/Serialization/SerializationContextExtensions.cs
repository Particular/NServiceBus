namespace NServiceBus
{
    using NServiceBus.Pipeline.OutgoingPipeline;

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
        public static void SkipSerialization(this OutgoingLogicalMessageContext context)
        {
            context.Set("MessageSerialization.Skip", true);
        }

        /// <summary>
        /// The serializer should skip serializing the message.
        /// </summary>
        public static bool ShouldSkipSerialization(this OutgoingLogicalMessageContext context)
        {
            bool shouldSkipSerialization;
            if (context.TryGet("MessageSerialization.Skip", out shouldSkipSerialization))
            {
                return shouldSkipSerialization;
            }
            return false;
        }
    }
}