using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    /// <summary>
    /// Configuration used to create a raw endpoint instance.
    /// </summary>
    public class RawEndpointConfiguration
    {
        Func<MessageContext, IDispatchMessages, Task> onMessage;
        QueueBindings queueBindings;

        /// <summary>
        /// Creates a send-only raw endpoint config.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint being configured.</param>
        /// <returns></returns>
        public static RawEndpointConfiguration CreateSendOnly(string endpointName)
        {
            return new RawEndpointConfiguration(endpointName, null, null);
        }

        /// <summary>
        /// Creates a regular raw endpoint config.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint being configured.</param>
        /// <param name="onMessage">Callback invoked when a message is received.</param>
        /// <param name="poisonMessageQueue">Queue to move poison messages that can't be received from transport.</param>
        /// <returns></returns>
        public static RawEndpointConfiguration Create(string endpointName, Func<MessageContext, IDispatchMessages, Task> onMessage, string poisonMessageQueue)
        {
            return new RawEndpointConfiguration(endpointName, onMessage, poisonMessageQueue);
        }

        RawEndpointConfiguration(string endpointName, Func<MessageContext, IDispatchMessages, Task> onMessage, string poisonMessageQueue)
        {
            this.onMessage = onMessage;
            ValidateEndpointName(endpointName);

            var sendOnly = onMessage == null;
            Settings.Set("Endpoint.SendOnly", sendOnly);
            Settings.Set("TypesToScan", new Type[0]);
            Settings.Set("NServiceBus.Routing.EndpointName", endpointName);
            Settings.Set<Conventions>(new Conventions()); //Hack for ASB
            Settings.Set<StartupDiagnosticEntries>(new StartupDiagnosticEntries());

            queueBindings = new QueueBindings();
            Settings.Set<QueueBindings>(queueBindings);

            Settings.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
            Settings.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);

            if (!sendOnly)
            {
                queueBindings.BindSending(poisonMessageQueue);
                Settings.Set("NServiceBus.Raw.PoisonMessageQueue", poisonMessageQueue);
                Settings.Set("errorQueue", poisonMessageQueue); //Hack for MSMQ
                Settings.SetDefault<IErrorHandlingPolicy>(new DefaultErrorHandlingPolicy(poisonMessageQueue, 5));
            }
        }

        /// <summary>
        /// Exposes raw settings object.
        /// </summary>
        public SettingsHolder Settings { get; } = new SettingsHolder();

        /// <summary>
        /// Instructs the endpoint to use a custom error handling policy.
        /// </summary>
        public void CustomErrorHandlingPolicy(IErrorHandlingPolicy customPolicy)
        {
            Guard.AgainstNull(nameof(customPolicy), customPolicy);
            Settings.Set<IErrorHandlingPolicy>(customPolicy);
        }

        /// <summary>
        /// Sets the number of immediate retries when message processing fails.
        /// </summary>
        public void DefaultErrorHandlingPolicy(string errorQueue, int immediateRetryCount)
        {
            Guard.AgainstNegative(nameof(immediateRetryCount), immediateRetryCount);
            Guard.AgainstNullAndEmpty(nameof(errorQueue), errorQueue);
            Settings.Set<IErrorHandlingPolicy>(new DefaultErrorHandlingPolicy(errorQueue, immediateRetryCount));
        }

        /// <summary>
        /// Instructs the endpoint to automatically create input queue and poison queue if they do not exist.
        /// </summary>
        public void AutoCreateQueue(string identity = null)
        {
            Settings.Set("NServiceBus.Raw.CreateQueue", true);
            if (identity != null)
            {
                Settings.Set("NServiceBus.Raw.Identity", identity);
            }
        }

        /// <summary>
        /// Instructs the endpoint to automatically create input queue, poison queue and provided additional queues if they do not exist.
        /// </summary>
        public void AutoCreateQueues(string[] additionalQueues, string identity = null)
        {
            foreach (var additionalQueue in additionalQueues)
            {
                queueBindings.BindSending(additionalQueue);
            }
            AutoCreateQueue(identity);
        }

        /// <summary>
        /// Instructs the transport to limits the allowed concurrency when processing messages.
        /// </summary>
        /// <param name="maxConcurrency">The max concurrency allowed.</param>
        public void LimitMessageProcessingConcurrencyTo(int maxConcurrency)
        {
            Guard.AgainstNegativeAndZero(nameof(maxConcurrency), maxConcurrency);

            Settings.Set("MaxConcurrency", maxConcurrency);
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public TransportExtensions<T> UseTransport<T>() where T : TransportDefinition, new()
        {
            var type = typeof(TransportExtensions<>).MakeGenericType(typeof(T));
            var transportDefinition = new T();
            var extension = (TransportExtensions<T>)Activator.CreateInstance(type, Settings);

            ConfigureTransport(transportDefinition);
            return extension;
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public TransportExtensions UseTransport(Type transportDefinitionType)
        {
            Guard.AgainstNull(nameof(transportDefinitionType), transportDefinitionType);
            Guard.TypeHasDefaultConstructor(transportDefinitionType, nameof(transportDefinitionType));

            var transportDefinition = Construct<TransportDefinition>(transportDefinitionType);
            ConfigureTransport(transportDefinition);
            return new TransportExtensions(Settings);
        }

        void ConfigureTransport(TransportDefinition transportDefinition)
        {
            Settings.Set<TransportDefinition>(transportDefinition);
        }

        static T Construct<T>(Type type)
        {
            var defaultConstructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[]
            {
            }, null);
            if (defaultConstructor != null)
            {
                return (T)defaultConstructor.Invoke(null);
            }

            return (T)Activator.CreateInstance(type);
        }

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