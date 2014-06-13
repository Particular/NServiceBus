namespace NServiceBus.Settings
{
    using System;
    using Features;
    using Persistence;

    /// <summary>
    ///     Configuration class for Endpoint settings.
    /// </summary>
    public class EndpointSettings
    {
        readonly Configure config;

        readonly EndpointAdvancedSettings endpointAdvancedSettings;

        public EndpointSettings(Configure config)
        {
            this.config = config;

            endpointAdvancedSettings = new EndpointAdvancedSettings(config);
        }

        /// <summary>
        ///     Tells the endpoint to not enforce durability (using InMemory storages, non durable messages, ...).
        /// </summary>
        public void AsVolatile()
        {
            config.UsePersistence<InMemory>();

            config.Transactions(t =>
            {
                t.Disable();
                t.Advanced(a =>
                {
                    a.DoNotWrapHandlersExecutionInATransactionScope();
                    a.DisableDistributedTransactions();
                });
            });

            Advanced(settings => settings.DisableDurableMessages());
        }

        /// <summary>
        ///     Configures this endpoint as a send only endpoint.
        /// </summary>
        /// <remarks>
        ///     Use this in endpoints whose only purpose is sending messages, websites are often a good example of send only endpoints.
        /// </remarks>
        public void AsSendOnly()
        {
            config.Settings.Set("Endpoint.SendOnly", true);
            config.Features(f=>f.Disable<TimeoutManager>());
        }

        /// <summary>
        ///     <see cref="EndpointSettings" /> advance settings.
        /// </summary>
        /// <param name="action">A lambda to set the advance settings.</param>
        public void Advanced(Action<EndpointAdvancedSettings> action)
        {
            action(endpointAdvancedSettings);
        }

        /// <summary>
        ///     <see cref="EndpointSettings" /> advance settings.
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
            public void EnableDurableMessages()
            {
                config.Settings.Set("Endpoint.DurableMessages", true);
            }

            /// <summary>
            /// Configures endpoint with messages that are not guaranteed to be delivered in the event of a computer failure or network problem.
            /// </summary>
            public void DisableDurableMessages()
            {
                config.Settings.Set("Endpoint.DurableMessages", false);
            }
        }
    }
}