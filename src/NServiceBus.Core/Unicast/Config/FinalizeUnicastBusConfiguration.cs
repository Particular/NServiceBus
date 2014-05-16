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
    using Settings;

    class FinalizeUnicastBusConfiguration : IFinalizeConfiguration
    {
        public void FinalizeConfiguration()
        {
            var knownMessages = Configure.TypesToScan
                .Where(MessageConventionExtensions.IsMessageType)
                .ToList();

            RegisterMessageOwnersAndBusAddress(knownMessages);

            ConfigureMessageRegistry(knownMessages);

            if (SettingsHolder.Instance.GetOrDefault<bool>("UnicastBus.AutoSubscribe"))
            {
                InfrastructureServices.Enable<IAutoSubscriptionStrategy>();
            }
        }

        void RegisterMessageOwnersAndBusAddress(IEnumerable<Type> knownMessages)
        {
            var unicastConfig = Configure.GetConfigSection<UnicastBusConfig>();
            var router = new StaticMessageRouter(knownMessages);
            var key = typeof(DefaultAutoSubscriptionStrategy).FullName + ".SubscribePlainMessages";

            if (SettingsHolder.Instance.HasSetting(key))
            {
                router.SubscribeToPlainMessages = SettingsHolder.Instance.Get<bool>(key);
            }

            Configure.Instance.Configurer.RegisterSingleton<StaticMessageRouter>(router);

            if (unicastConfig == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(unicastConfig.ForwardReceivedMessagesTo))
            {
                var forwardAddress = Address.Parse(unicastConfig.ForwardReceivedMessagesTo);
                Configure.Instance.Configurer.ConfigureProperty<ForwardBehavior>(b => b.ForwardReceivedMessagesTo, forwardAddress);
            }
            Configure.Instance.Configurer.ConfigureProperty<ForwardBehavior>(b => b.TimeToBeReceivedOnForwardedMessages, unicastConfig.TimeToBeReceivedOnForwardedMessages);
            
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

        void ConfigureMessageRegistry(List<Type> knownMessages)
        {
            var messageRegistry = new MessageMetadataRegistry
            {
                DefaultToNonPersistentMessages = !SettingsHolder.Instance.Get<bool>("Endpoint.DurableMessages")
            };

            knownMessages.ForEach(messageRegistry.RegisterMessageType);

            Configure.Instance.Configurer.RegisterSingleton<MessageMetadataRegistry>(messageRegistry);
            Configure.Instance.Configurer.ConfigureComponent<LogicalMessageFactory>(DependencyLifecycle.SingleInstance);

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