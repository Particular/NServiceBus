namespace NServiceBus.Settings
{
    using System;
    using Features;
    using Persistence;

    class DefaultEndpointSettings : IWantToRunBeforeConfiguration
    {
        public void Init(Configure configure)
        {
            configure.Settings.SetDefault("Endpoint.SendOnly", false);
        }
    }

    class DefaultEndpointAdvancedSettings : IWantToRunBeforeConfiguration
    {
        public void Init(Configure configure)
        {
            configure.Settings.SetDefault("Endpoint.DurableMessages", true);
        }
    }

    /// <summary>
    ///     Configuration class for Endpoint settings.
    /// </summary>
    public class Endpoint
    {
        readonly Configure config;

        readonly EndpointAdvancedSettings endpointAdvancedSettings;

        public Endpoint(Configure config)
        {
            this.config = config;

            endpointAdvancedSettings = new EndpointAdvancedSettings(config);
        }

        /// <summary>
        ///     Tells the endpoint to not enforce durability (using InMemory storages, non durable messages, ...).
        /// </summary>
        public Endpoint AsVolatile()
        {
            config.Settings.Set("Persistence", typeof(InMemory));

            config.Transactions.Disable();
            config.Transactions.Advanced(s => s.DoNotWrapHandlersExecutionInATransactionScope()
                                                  .DisableDistributedTransactions());

            Advanced(settings => settings.DisableDurableMessages());

            return this;
        }

        /// <summary>
        ///     Configures this endpoint as a send only endpoint.
        /// </summary>
        /// <remarks>
        ///     Use this in endpoints whose only purpose is sending messages, websites are often a good example of send only endpoints.
        /// </remarks>
        public Endpoint AsSendOnly()
        {
            config.Settings.Set("Endpoint.SendOnly", true);
            config.Features.Disable<TimeoutManager>();
            return this;
        }

        /// <summary>
        ///     <see cref="Endpoint" /> advance settings.
        /// </summary>
        /// <param name="action">A lambda to set the advance settings.</param>
        public Endpoint Advanced(Action<EndpointAdvancedSettings> action)
        {
            action(endpointAdvancedSettings);
            return this;
        }

        /// <summary>
        ///     <see cref="Endpoint" /> advance settings.
        /// </summary>
        public class EndpointAdvancedSettings
        {
            readonly Configure config;

            public EndpointAdvancedSettings(Configure config)
            {
                this.config = config;
            }

            /// <summary>
            /// Configures endpoint with messages guaranteed to be delivered in the event of a computer failure or network problem.
            /// </summary>
            public EndpointAdvancedSettings EnableDurableMessages()
            {
                config.Settings.Set("Endpoint.DurableMessages", true);
                return this;
            }

            /// <summary>
            /// Configures endpoint with messages that are not guaranteed to be delivered in the event of a computer failure or network problem.
            /// </summary>
            public EndpointAdvancedSettings DisableDurableMessages()
            {
                config.Settings.Set("Endpoint.DurableMessages", false);
                return this;
            }
        }
    }
}