namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AutomaticSubscriptions;
    using Config;
    using Faults;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;
    using Unicast.Messages;
    using Unicast.Routing;
    using Unicast.Transport;

    /// <summary>
    ///   Used to configure the <see cref="Unicast.UnicastBus"/>
    /// </summary>
    class UnicastBus : Feature
    {
        internal UnicastBus()
        {
            EnableByDefault();

            Defaults(s => s.SetDefault("NServiceBus.LocalAddress", s.EndpointName()));
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var defaultAddress = Address.Parse(context.Settings.Get<string>("NServiceBus.LocalAddress"));

            context.Container.ConfigureComponent<Unicast.UnicastBus>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(u => u.InputAddress, defaultAddress);

            ConfigureSubscriptionAuthorization(context);

            context.Container.ConfigureComponent<PipelineExecutor>(DependencyLifecycle.SingleInstance);
            ConfigureBehaviors(context);

            var knownMessages = context.Settings.GetAvailableTypes()
                .Where(context.Settings.Get<Conventions>().IsMessageType)
                .ToList();

            RegisterMessageOwnersAndBusAddress(context, knownMessages);

            ConfigureMessageRegistry(context, knownMessages);

            if (context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                return;
            }

            SetTransportThresholds(context);
        }

        void SetTransportThresholds(FeatureConfigurationContext context)
        {
            var transportConfig = context.Settings.GetConfigSection<TransportConfig>();
            var maximumThroughput = 0;
            var maximumNumberOfRetries = 5;
            var maximumConcurrencyLevel = 1;

            if (transportConfig != null)
            {
                maximumNumberOfRetries = transportConfig.MaxRetries;
                maximumThroughput = transportConfig.MaximumMessageThroughputPerSecond;
                maximumConcurrencyLevel = transportConfig.MaximumConcurrencyLevel;
            }

            var transactionSettings = new TransactionSettings(context.Settings)
            {
                MaxRetries = maximumNumberOfRetries
            };

            context.Container.ConfigureComponent(b => new TransportReceiver(transactionSettings, maximumConcurrencyLevel, maximumThroughput, b.Build<IDequeueMessages>(), b.Build<IManageMessageFailures>(), context.Settings, b.Build<Configure>())
            {
                CriticalError = b.Build<CriticalError>(),
                ErrorSubscribers = b.Build<ErrorSubscribersCoordinator>()
            }, DependencyLifecycle.InstancePerCall);
        }

        void ConfigureBehaviors(FeatureConfigurationContext context)
        {
            // ReSharper disable HeapView.SlowDelegateCreation
            var behaviorTypes = context.Settings.GetAvailableTypes().Where(t => (typeof(IBehavior<IncomingContext>).IsAssignableFrom(t) || typeof(IBehavior<OutgoingContext>).IsAssignableFrom(t))
                                                            && !(t.IsAbstract || t.IsInterface));
            // ReSharper restore HeapView.SlowDelegateCreation
            foreach (var behaviorType in behaviorTypes)
            {
                context.Container.ConfigureComponent(behaviorType, DependencyLifecycle.InstancePerCall);
            }
        }

        void ConfigureSubscriptionAuthorization(FeatureConfigurationContext context)
        {
            var authType = context.Settings.GetAvailableTypes().FirstOrDefault(t => typeof(IAuthorizeSubscriptions).IsAssignableFrom(t) && !t.IsInterface);

            if (authType != null)
            {
                context.Container.ConfigureComponent(authType, DependencyLifecycle.SingleInstance);
            }
        }

        void RegisterMessageOwnersAndBusAddress(FeatureConfigurationContext context, IEnumerable<Type> knownMessages)
        {
            var unicastConfig = context.Settings.GetConfigSection<UnicastBusConfig>();
            var router = new StaticMessageRouter(knownMessages);
            var key = typeof(AutoSubscriptionStrategy).FullName + ".SubscribePlainMessages";

            if (context.Settings.HasSetting(key))
            {
                router.SubscribeToPlainMessages = context.Settings.Get<bool>(key);
            }

            context.Container.RegisterSingleton(router);

            if (unicastConfig == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(unicastConfig.ForwardReceivedMessagesTo))
            {
                var forwardAddress = Address.Parse(unicastConfig.ForwardReceivedMessagesTo);
                context.Container.ConfigureProperty<ForwardBehavior>(b => b.ForwardReceivedMessagesTo, forwardAddress);
            }

            if (unicastConfig.TimeToBeReceivedOnForwardedMessages != TimeSpan.Zero)
            {
                context.Container.ConfigureProperty<ForwardBehavior>(b => b.TimeToBeReceivedOnForwardedMessages, unicastConfig.TimeToBeReceivedOnForwardedMessages);
            }

            var messageEndpointMappings = unicastConfig.MessageEndpointMappings.Cast<MessageEndpointMapping>()
                .OrderByDescending(m => m)
                .ToList();

            foreach (var mapping in messageEndpointMappings)
            {
                mapping.Configure((messageType, address) =>
                {
                    var conventions = context.Settings.Get<Conventions>();
                    if (!(conventions.IsMessageType(messageType) || conventions.IsEventType(messageType) || conventions.IsCommandType(messageType)))
                    {
                        return;
                    }

                    if (conventions.IsEventType(messageType))
                    {
                        router.RegisterEventRoute(messageType, address);
                        return;
                    }

                    router.RegisterMessageRoute(messageType, address);
                });
            }
        }
        void ConfigureMessageRegistry(FeatureConfigurationContext context, IEnumerable<Type> knownMessages)
        {
            var messageRegistry = new MessageMetadataRegistry(!DurableMessagesConfig.GetDurableMessagesEnabled(context.Settings), context.Settings.Get<Conventions>());

            foreach (var msg in knownMessages)
            {
                messageRegistry.RegisterMessageType(msg);
            }

            context.Container.RegisterSingleton(messageRegistry);
            context.Container.ConfigureComponent<LogicalMessageFactory>(DependencyLifecycle.SingleInstance);

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

        static ILog Logger = LogManager.GetLogger<UnicastBus>();
    }
}