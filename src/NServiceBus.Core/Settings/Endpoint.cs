namespace NServiceBus.Settings
{
    using System;
    using Features;
    using Persistence.InMemory;

    class DefaultEndpointSettings: ISetDefaultSettings
    {
        public DefaultEndpointSettings()
        {
            SettingsHolder.Instance.SetDefault("Endpoint.SendOnly", false);
        }
    }

    class DefaultEndpointAdvancedSettings : ISetDefaultSettings
    {
        public DefaultEndpointAdvancedSettings()
        {
            SettingsHolder.Instance.SetDefault("Endpoint.DurableMessages", true);
        }
    }

    /// <summary>
    ///     Configuration class for Endpoint settings.
    /// </summary>
    public class Endpoint
    {
        private readonly EndpointAdvancedSettings endpointAdvancedSettings = new EndpointAdvancedSettings();

        /// <summary>
        ///     Tells the endpoint to not enforce durability (using InMemory storages, non durable messages, ...).
        /// </summary>
        public Endpoint AsVolatile()
        {
            InMemoryPersistence.UseAsDefault();

            Configure.Transactions.Disable();
            Configure.Transactions.Advanced(s => s.DoNotWrapHandlersExecutionInATransactionScope()
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
            SettingsHolder.Instance.Set("Endpoint.SendOnly", true);
            Feature.Disable<TimeoutManager>();
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
            /// <summary>
            /// Configures endpoint with messages guaranteed to be delivered in the event of a computer failure or network problem.
            /// </summary>
            public EndpointAdvancedSettings EnableDurableMessages()
            {
                SettingsHolder.Instance.Set("Endpoint.DurableMessages", true);
                return this;
            }

            /// <summary>
            /// Configures endpoint with messages that are not guaranteed to be delivered in the event of a computer failure or network problem.
            /// </summary>
            public EndpointAdvancedSettings DisableDurableMessages()
            {
                SettingsHolder.Instance.Set("Endpoint.DurableMessages", false);
                return this;
            }
        }
    }
}