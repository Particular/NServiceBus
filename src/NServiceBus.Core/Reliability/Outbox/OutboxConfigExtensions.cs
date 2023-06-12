﻿namespace NServiceBus
{
    using Outbox;

    /// <summary>
    /// Config methods for the outbox.
    /// </summary>
    public static class OutboxConfigExtensions
    {
        /// <summary>
        /// Enables the outbox feature.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static OutboxSettings EnableOutbox(this EndpointConfiguration config)
        {
            Guard.ThrowIfNull(config);

            var outboxSettings = new OutboxSettings(config.Settings);

            config.EnableFeature<Features.Outbox>();
            return outboxSettings;
        }
    }
}