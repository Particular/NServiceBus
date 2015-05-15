namespace NServiceBus
{
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Extensions to the outgoing pipeline
    /// </summary>
    public static class HeaderExtensions
    {
        /// <summary>
        /// Allows headers to be set for the outgoing message
        /// </summary>
        /// <param name="context">Context to extend</param>
        /// <param name="key">The header key</param>
        /// <param name="value">The header value</param>
        public static void SetHeader(this OutgoingContext context, string key, string value)
        {
            Guard.AgainstNullAndEmpty(key,"key");

            context.Extensions.GetOrCreate<DispatchMessageToTransportBehavior.State>()
                .Headers[key] = value;
        }

        /// <summary>
        /// Allows headers to be set for the outgoing message
        /// </summary>
        /// <param name="context">Context to extend</param>
        /// <param name="key">The header key</param>
        /// <param name="value">The header value</param>
        public static void SetHeader(this ExtendableOptions context, string key, string value)
        {
            Guard.AgainstNullAndEmpty(key, "key");
            
            context.Extensions.GetOrCreate<DispatchMessageToTransportBehavior.State>()
                .Headers[key] = value;
        }

        /// <summary>
        /// Allows headers to be set for the outgoing message
        /// </summary>
        /// <param name="context">Context to extend</param>
        /// <param name="key">The header key</param>
        /// <param name="value">The header value</param>
        public static void SetHeader(this PhysicalOutgoingContextStageBehavior.Context context, string key, string value)
        {
            Guard.AgainstNullAndEmpty(key, "key");
            
            context.Extensions.GetOrCreate<DispatchMessageToTransportBehavior.State>()
                .Headers[key] = value;
        }
    }
}