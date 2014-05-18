namespace NServiceBus.Unicast.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AutomaticSubscriptions;
    using Behaviors;
    using Logging;
    using Messages;
    using NServiceBus.Config;
    using Routing;

    class FinalizeUnicastBusConfiguration : IFinalizeConfiguration
    {
        public void FinalizeConfiguration(Configure config)
        {
            var knownMessages = Configure.TypesToScan
                .Where(MessageConventionExtensions.IsMessageType)
                .ToList();

            RegisterMessageOwnersAndBusAddress(config,knownMessages);

            ConfigureMessageRegistry(config,knownMessages);
        }

        void RegisterMessageOwnersAndBusAddress(Configure config,IEnumerable<Type> knownMessages)
        {
            var unicastConfig = config.GetConfigSection<UnicastBusConfig>();
            var router = new StaticMessageRouter(knownMessages);
            var key = typeof(AutoSubscriptionStrategy).FullName + ".SubscribePlainMessages";

            if (config.Settings.HasSetting(key))
            {
                router.SubscribeToPlainMessages = config.Settings.Get<bool>(key);
            }

            config.Configurer.RegisterSingleton<StaticMessageRouter>(router);

            if (unicastConfig == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(unicastConfig.ForwardReceivedMessagesTo))
            {
                var forwardAddress = Address.Parse(unicastConfig.ForwardReceivedMessagesTo);
                config.Configurer.ConfigureProperty<ForwardBehavior>(b => b.ForwardReceivedMessagesTo, forwardAddress);
            }
            config.Configurer.ConfigureProperty<ForwardBehavior>(b => b.TimeToBeReceivedOnForwardedMessages, unicastConfig.TimeToBeReceivedOnForwardedMessages);
            
            var messageEndpointMappings = unicastConfig.MessageEndpointMappings.Cast<MessageEndpointMapping>()
                .OrderByDescending(m => m)
                .ToList();

            foreach (var mapping in messageEndpointMappings)
            {
                mapping.Configure((messageType, address) =>
                {
                    if (!(MessageConventionExtensions.IsMessageType(messageType) || MessageConventionExtensions.IsEventType(messageType) || MessageConventionExtensions.IsCommandType(messageType)))
                    {
                        return;
                    }

                    if (MessageConventionExtensions.IsEventType(messageType))
                    {
                        router.RegisterEventRoute(messageType, address);
                        return;
                    }

                    router.RegisterMessageRoute(messageType, address);
                });
            }
        }

        void ConfigureMessageRegistry(Configure config,List<Type> knownMessages)
        {
            var messageRegistry = new MessageMetadataRegistry
            {
                DefaultToNonPersistentMessages = !config.Settings.Get<bool>("Endpoint.DurableMessages")
            };

            knownMessages.ForEach(messageRegistry.RegisterMessageType);

            config.Configurer.RegisterSingleton<MessageMetadataRegistry>(messageRegistry);
            config.Configurer.ConfigureComponent<LogicalMessageFactory>(DependencyLifecycle.SingleInstance);

            if (!Logger.IsInfoEnabled)
            {
                return;
            }

            var messageDefinitions = messageRegistry.GetAllMessages().ToList();

            Logger.InfoFormat("Number of messages found: {0}", messageDefinitions.Count());

            if (!Logger.IsDebugEnabled)
            {
                return;
            }

            Logger.DebugFormat("Message definitions: \n {0}",
                string.Concat(messageDefinitions.Select(md => md.ToString() + "\n")));
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(FinalizeUnicastBusConfiguration));
    }
}