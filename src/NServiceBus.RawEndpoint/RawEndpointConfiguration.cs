namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Config.ConfigurationSource;
    using Settings;
    using Transport;

    /// <summary>
    /// Configuration used to create a raw endpoint instance.
    /// </summary>
    public class RawEndpointConfiguration
    {
        Func<MessageContext, IDispatchMessages, Task> onMessage;
        internal SettingsHolder Settings = new SettingsHolder();

        /// <summary>
        /// Creates a send-only raw endpoint config.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint being configured.</param>
        /// <returns></returns>
        public static RawEndpointConfiguration CreateSendOnly(string endpointName)
        {
            return new RawEndpointConfiguration(endpointName, null);
        }

        /// <summary>
        /// Creates a regular raw endpoint config.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint being configured.</param>
        /// <param name="onMessage">Callback invoked when a message is received.</param>
        /// <returns></returns>
        public static RawEndpointConfiguration Create(string endpointName, Func<MessageContext, IDispatchMessages, Task> onMessage)
        {
            return new RawEndpointConfiguration(endpointName, onMessage);
        }

        RawEndpointConfiguration(string endpointName, Func<MessageContext, IDispatchMessages, Task> onMessage)
        {
            this.onMessage = onMessage;
            ValidateEndpointName(endpointName);

            Settings.Set("Endpoint.SendOnly", onMessage == null);
            Settings.Set("TypesToScan", new Type[0]);
            Settings.Set("NServiceBus.Routing.EndpointName", endpointName);

            Settings.SetDefault<IConfigurationSource>(new DefaultConfigurationSource());

            Settings.Set<QueueBindings>(new QueueBindings());

            Settings.SetDefault("Endpoint.SendOnly", false);
            Settings.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
            Settings.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);
        }

        /// <summary>
        /// Configure error queue Settings.
        /// </summary>
        /// <param name="errorQueue">The name of the error queue to use.</param>
        public void SendFailedMessagesTo(string errorQueue)
        {
            Guard.AgainstNullAndEmpty(nameof(errorQueue), errorQueue);
            Settings.Set("errorQueue", errorQueue);
        }

        /// <summary>
        /// Creates the configuration object.
        /// </summary>
        internal InitializableRawEndpoint Build()
        {
            return new InitializableRawEndpoint(Settings, onMessage);
        }

        static void ValidateEndpointName(string endpointName)
        {
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new ArgumentException("Endpoint name must not be empty", nameof(endpointName));
            }

            if (endpointName.Contains("@"))
            {
                throw new ArgumentException("Endpoint name must not contain an '@' character.", nameof(endpointName));
            }
        }
    }
}