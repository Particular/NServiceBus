namespace NServiceBus.TransportDispatch
{
    using OutgoingPipeline;
    using Pipeline.Contexts;

    /// <summary>
    /// Extensions to the the pipeline contexts to provide ways to set message headers.
    /// </summary>
    public static class HeaderContextExtensions
    {
        /// <summary>
        /// Allows headers to be set for the outgoing message.
        /// </summary>
        /// <param name="context">Context to extend.</param>
        /// <param name="key">The header key.</param>
        /// <param name="value">The header value.</param>
        public static void SetHeader(this OutgoingLogicalMessageContext context, string key, string value)
        {
            Guard.AgainstNull("context", context);
            Guard.AgainstNullAndEmpty("key", key);
            Guard.AgainstNullAndEmpty("value", value);

            context.GetOrCreate<OutgoingPhysicalToRoutingConnector.State>()
                .Headers[key] = value;
        }

        /// <summary>
        /// Allows headers to be set for the outgoing message.
        /// </summary>
        /// <param name="context">Context to extend.</param>
        /// <param name="key">The header key.</param>
        /// <param name="value">The header value.</param>
        public static void SetHeader(this OutgoingPublishContext context, string key, string value)
        {
            Guard.AgainstNull("context", context);
            Guard.AgainstNullAndEmpty("key", key);
            Guard.AgainstNullAndEmpty("value", value);

            context.GetOrCreate<OutgoingPhysicalToRoutingConnector.State>()
                .Headers[key] = value;
        }

        /// <summary>
        /// Allows headers to be set for the outgoing message.
        /// </summary>
        /// <param name="context">Context to extend.</param>
        /// <param name="key">The header key.</param>
        /// <param name="value">The header value.</param>
        public static void SetHeader(this OutgoingSendContext context, string key, string value)
        {
            Guard.AgainstNull("context", context);
            Guard.AgainstNullAndEmpty("key", key);
            Guard.AgainstNullAndEmpty("value", value);

            context.GetOrCreate<OutgoingPhysicalToRoutingConnector.State>()
                .Headers[key] = value;
        }

        /// <summary>
        /// Allows headers to be set for the outgoing message.
        /// </summary>
        /// <param name="context">Context to extend.</param>
        /// <param name="key">The header key.</param>
        /// <param name="value">The header value.</param>
        public static void SetHeader(this OutgoingReplyContext context, string key, string value)
        {
            Guard.AgainstNull("context", context);
            Guard.AgainstNullAndEmpty("key", key);
            Guard.AgainstNullAndEmpty("value", value);

            context.GetOrCreate<OutgoingPhysicalToRoutingConnector.State>()
                .Headers[key] = value;
        }

        /// <summary>
        /// Allows headers to be set for the outgoing message.
        /// </summary>
        /// <param name="context">Context to extend.</param>
        /// <param name="key">The header key.</param>
        /// <param name="value">The header value.</param>
        public static void SetHeader(this OutgoingPhysicalMessageContext context, string key, string value)
        {
            Guard.AgainstNull("context", context);
            Guard.AgainstNullAndEmpty("key", key);
            Guard.AgainstNullAndEmpty("value", value);

            context.GetOrCreate<OutgoingPhysicalToRoutingConnector.State>()
                .Headers[key] = value;
        }
    }
}