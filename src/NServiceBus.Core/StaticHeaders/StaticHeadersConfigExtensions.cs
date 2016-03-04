namespace NServiceBus
{
    /// <summary>
    /// Extensions to the public configuration api.
    /// </summary>
    public static class StaticHeadersConfigExtensions
    {
        /// <summary>
        /// Adds a header that will be attached to all outgoing messages for this endpoint. These headers can not be changed at
        /// runtime. Use a outgoing message mutator
        /// if you need to apply headers that needs to be dynamic per message. You can also set headers explicitly for a given
        /// message using any of the Send/Reply or PublishOptions.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="key">The static header key.</param>
        /// <param name="value">The static header value.</param>
        public static void AddHeaderToAllOutgoingMessages(this EndpointConfiguration config, string key, string value)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);

            CurrentStaticHeaders headers;

            if (!config.Settings.TryGet(out headers))
            {
                headers = new CurrentStaticHeaders();

                config.Settings.Set<CurrentStaticHeaders>(headers);
            }


            headers[key] = value;
        }
    }
}