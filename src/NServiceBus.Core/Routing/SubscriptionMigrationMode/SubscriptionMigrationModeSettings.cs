namespace NServiceBus
{
    using System;
    using System.Reflection;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NServiceBus.Settings;

    /// <summary>
    /// Provides configuration for subscription migration mode.
    /// </summary>
    public class SubscriptionMigrationModeSettings : ExposeSettings
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SubscriptionMigrationModeSettings" />.
        /// </summary>
        public SubscriptionMigrationModeSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Sets an authorizer to be used when processing a <see cref="MessageIntentEnum.Subscribe" /> or
        /// <see cref="MessageIntentEnum.Unsubscribe" /> message.
        /// </summary>
        /// <param name="authorizer">The authorization callback to execute. If the callback returns <code>true</code> for a message, it is authorized to subscribe/unsubscribe, otherwise it is not authorized.</param>
        public void SubscriptionAuthorizer(Func<IIncomingPhysicalMessageContext, bool> authorizer)
        {
            Guard.AgainstNull(nameof(authorizer), authorizer);

            Settings.Set("SubscriptionAuthorizer", authorizer);
        }

        /// <summary>
        /// Registers a publisher endpoint for a given event type.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="publisherEndpoint">The publisher endpoint.</param>
        public void RegisterPublisher(Type eventType, string publisherEndpoint)
        {
            Guard.AgainstNullAndEmpty(nameof(publisherEndpoint), publisherEndpoint);

            ThrowOnAddress(publisherEndpoint);
            Settings.GetOrCreate<ConfiguredPublishers>().Add(new TypePublisherSource(eventType, PublisherAddress.CreateFromEndpointName(publisherEndpoint)));
        }

        /// <summary>
        /// Registers a publisher endpoint for all event types in a given assembly.
        /// </summary>
        /// <param name="assembly">The assembly containing the event types.</param>
        /// <param name="publisherEndpoint">The publisher endpoint.</param>
        public void RegisterPublisher<T>(Assembly assembly, string publisherEndpoint)
        {
            Guard.AgainstNull(nameof(assembly), assembly);
            Guard.AgainstNullAndEmpty(nameof(publisherEndpoint), publisherEndpoint);

            ThrowOnAddress(publisherEndpoint);

            Settings.GetOrCreate<ConfiguredPublishers>().Add(new AssemblyPublisherSource(assembly, PublisherAddress.CreateFromEndpointName(publisherEndpoint)));
        }

        /// <summary>
        /// Registers a publisher endpoint for all event types in a given assembly and namespace.
        /// </summary>
        /// <param name="assembly">The assembly containing the event types.</param>
        /// <param name="namespace"> The namespace containing the event types. The given value must exactly match the target namespace.</param>
        /// <param name="publisherEndpoint">The publisher endpoint.</param>
        public void RegisterPublisher<T>(Assembly assembly, string @namespace, string publisherEndpoint)
        {
            Guard.AgainstNull(nameof(assembly), assembly);
            Guard.AgainstNullAndEmpty(nameof(publisherEndpoint), publisherEndpoint);

            ThrowOnAddress(publisherEndpoint);

            // empty namespace is null, not string.empty
            @namespace = @namespace == string.Empty ? null : @namespace;

            Settings.GetOrCreate<ConfiguredPublishers>().Add(new NamespacePublisherSource(assembly, @namespace, PublisherAddress.CreateFromEndpointName(publisherEndpoint)));
        }

        static void ThrowOnAddress(string publisherEndpoint)
        {
            if (publisherEndpoint.Contains("@"))
            {
                throw new ArgumentException($"A logical endpoint name should not contain '@', but received '{publisherEndpoint}'. To specify an endpoint's address, use the instance mapping file for the MSMQ transport, or refer to the routing documentation.");
            }
        }
    }
}
