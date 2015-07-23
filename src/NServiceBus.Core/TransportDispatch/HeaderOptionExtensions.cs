namespace NServiceBus
{
    using NServiceBus.Extensibility;

    /// <summary>
    /// Extensions to the options to provide ways to set message headers.
    /// </summary>
    public static class HeaderOptionExtensions
    {
      
        /// <summary>
        /// Allows headers to be set for the outgoing message.
        /// </summary>
        /// <param name="context">Context to extend.</param>
        /// <param name="key">The header key.</param>
        /// <param name="value">The header value.</param>
        public static void SetHeader(this ExtendableOptions context, string key, string value)
        {
            Guard.AgainstNull(context, "context");
            Guard.AgainstNullAndEmpty(key, "key");
            
            context.Context.GetOrCreate<DispatchMessageToTransportConnector.State>()
                .Headers[key] = value;
        }

      
    }
}