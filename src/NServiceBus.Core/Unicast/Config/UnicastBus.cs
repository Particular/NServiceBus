namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Transactions;
    using AutomaticSubscriptions;
    using Config;
    using Faults;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;
    using Unicast.Behaviors;
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
            Defaults(s =>
            {
               s.SetDefault("Endpoint.SendOnly", false);
               s.SetDefault("Endpoint.DurableMessages", true);
               s.SetDefault("Transactions.Enabled", true);
               s.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
               s.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);
               s.SetDefault("Transactions.SuppressDistributedTransactions", false);
               s.SetDefault("Transactions.DoNotWrapHandlersExecutionInATransactionScope", false);
            });
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<Unicast.UnicastBus>(DependencyLifecycle.SingleInstance);

            ConfigureSubscriptionAuthorization(context);

            context.Container.ConfigureComponent<PipelineExecutor>(DependencyLifecycle.SingleInstance);
            ConfigureBehaviors(context);

            var knownMessages = context.Settings.GetAvailableTypes()
            .Where(context.Settings.Get<Conventions>().IsMessageType)
            .ToList();

            RegisterMessageOwnersAndBusAddress(context, knownMessages);

            ConfigureMessageRegistry(context, knownMessages);

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

            context.Container.ConfigureComponent(b => new TransportReceiver(transactionSettings, maximumConcurrencyLevel, maximumThroughput, b.Build<IDequeueMessages>(), b.Build<IManageMessageFailures>(), context.Settings)
            {
              CriticalError =  b.Build<CriticalError>()
            }, DependencyLifecycle.InstancePerCall);
        }

        void ConfigureBehaviors(FeatureConfigurationContext context)
        {
            // ReSharper disable HeapView.SlowDelegateCreation
            context.Settings.GetAvailableTypes().Where(t => (typeof(IBehavior<IncomingContext>).IsAssignableFrom(t)  || typeof(IBehavior<OutgoingContext>).IsAssignableFrom(t))
                                                            && !(t.IsAbstract || t.IsInterface))
                // ReSharper restore HeapView.SlowDelegateCreation
                .ToList()
                .ForEach(behaviorType => context.Container.ConfigureComponent(behaviorType,DependencyLifecycle.InstancePerCall));
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

            context.Container.RegisterSingleton<StaticMessageRouter>(router);

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
        void ConfigureMessageRegistry(FeatureConfigurationContext context, List<Type> knownMessages)
        {
            var messageRegistry = new MessageMetadataRegistry(!context.Settings.Get<bool>("Endpoint.DurableMessages"), context.Settings.Get<Conventions>());

            knownMessages.ForEach(messageRegistry.RegisterMessageType);

            context.Container.RegisterSingleton<MessageMetadataRegistry>(messageRegistry);
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