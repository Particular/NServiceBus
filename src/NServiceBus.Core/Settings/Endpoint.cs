namespace NServiceBus.Settings
{
    using System;

    class DefaultEndpointSettings: ISetDefaultSettings
    {
        public DefaultEndpointSettings()
        {
            SettingsHolder.SetDefault("Endpoint.SendOnly", false);
        }
    }

    class DefaultEndpointAdvancedSettings : ISetDefaultSettings
    {
        public DefaultEndpointAdvancedSettings()
        {
            SettingsHolder.SetDefault("Endpoint.DurableMessages", true);
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
            Configure.Transactions.Disable();
            Configure.Transactions.Advanced(s => s.DoNotWrapHandlersExecutionInATransactionScope(true)
                                                  .SuppressDistributedTransactions(true));

            Advanced(settings => settings.DurableMessages(false));

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
            SettingsHolder.Set("Endpoint.SendOnly", true);
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
            /// Configures endpoint with whether messages are guaranteed to be delivered in the event of a computer failure or network problem, or not.
            /// </summary>
            /// <param name="value"><c>true</c> to indicate that messages are guaranteed to be delivered in the event of a computer failure or network problem, otherwise <c>false</c>.</param>
            /// <returns></returns>
            public EndpointAdvancedSettings DurableMessages(bool value)
            {
                SettingsHolder.Set("Endpoint.DurableMessages", value);
                return this;
            }
        }
    }
}