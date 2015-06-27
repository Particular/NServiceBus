namespace NServiceBus.TransportDispatch
{
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;

    /// <summary>
    /// Extensions to the the pipeline contexts to provide ways to set message headers
    /// </summary>
    public static class HeaderContextExtensions
    {
        /// <summary>
        /// Allows headers to be set for the outgoing message
        /// </summary>
        /// <param name="context">Context to extend</param>
        /// <param name="key">The header key</param>
        /// <param name="value">The header value</param>
        public static void SetHeader(this OutgoingContext context, string key, string value)
        {
            Guard.AgainstNullAndEmpty(key, "key");

            context.GetOrCreate<DispatchMessageToTransportConnector.State>()
                .Headers[key] = value;
        }

        /// <summary>
        /// Allows headers to be set for the outgoing message
        /// </summary>
        /// <param name="context">Context to extend</param>
        /// <param name="key">The header key</param>
        /// <param name="value">The header value</param>
        public static void SetHeader(this OutgoingPublishContext context, string key, string value)
        {
            Guard.AgainstNullAndEmpty(key, "key");

            context.GetOrCreate<DispatchMessageToTransportConnector.State>()
                .Headers[key] = value;
        }

        /// <summary>
        /// Allows headers to be set for the outgoing message
        /// </summary>
        /// <param name="context">Context to extend</param>
        /// <param name="key">The header key</param>
        /// <param name="value">The header value</param>
        public static void SetHeader(this OutgoingSendContext context, string key, string value)
        {
            Guard.AgainstNullAndEmpty(key, "key");

            context.GetOrCreate<DispatchMessageToTransportConnector.State>()
                .Headers[key] = value;
        }

        /// <summary>
        /// Allows headers to be set for the outgoing message
        /// </summary>
        /// <param name="context">Context to extend</param>
        /// <param name="key">The header key</param>
        /// <param name="value">The header value</param>
        public static void SetHeader(this OutgoingReplyContext context, string key, string value)
        {
            Guard.AgainstNullAndEmpty(key, "key");

            context.GetOrCreate<DispatchMessageToTransportConnector.State>()
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

            context.GetOrCreate<DispatchMessageToTransportConnector.State>()
                .Headers[key] = value;
        }

        /// <summary>
        /// Allows headers to be set for the outgoing message
        /// </summary>
        /// <param name="context">Context to extend</param>
        /// <param name="key">The header key</param>
        /// <param name="value">The header value</param>
        public static void SetHeader(this DispatchContext context, string key, string value)
        {
            Guard.AgainstNullAndEmpty(key, "key");

            context.Get<OutgoingMessage>().Headers[key] = value;
        }
    }
}