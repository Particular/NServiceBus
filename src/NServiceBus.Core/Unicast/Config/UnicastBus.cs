namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Config;
    using NServiceBus.Logging;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Settings.Concurrency;
    using NServiceBus.Settings.Throttling;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Routing;
    using TransactionSettings = NServiceBus.Unicast.Transport.TransactionSettings;

    class UnicastBus : Feature
    {
        internal UnicastBus()
        {
            EnableByDefault();

            Defaults(s =>
            {
                s.SetDefault<IConcurrencyConfig>(new SharedConcurrencyConfig(null));
                s.SetDefault<IThrottlingConfig>(new NoLimitThrottlingConfig());
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<BusNotifications>(DependencyLifecycle.SingleInstance);


            var concurrencyConfig = context.Settings.Get<IConcurrencyConfig>();
            var throttlingConfig = context.Settings.Get<IThrottlingConfig>();

            var transportConfig = context.Settings.GetConfigSection<TransportConfig>();

            if (transportConfig != null)
            {
                if (transportConfig.MaximumConcurrencyLevel != 0)
                {
                    concurrencyConfig = new SharedConcurrencyConfig(transportConfig.MaximumConcurrencyLevel);
                }
                if (transportConfig.MaximumMessageThroughputPerSecond == 0)
                {
                    throttlingConfig = new NoLimitThrottlingConfig();
                }
                else if (transportConfig.MaximumMessageThroughputPerSecond != -1)
                {
                    throttlingConfig = new SharedLimitThrottlingConfig(transportConfig.MaximumConcurrencyLevel);
                }
            }

            context.Container.ConfigureComponent(b => throttlingConfig.WrapExecutor(concurrencyConfig.BuildExecutor()), DependencyLifecycle.SingleInstance);

            context.Container.ConfigureComponent<BehaviorContextStacker>(DependencyLifecycle.SingleInstance);

            context.Container.ConfigureComponent(b => b.Build<BehaviorContextStacker>().GetCurrentOrRootContext(), DependencyLifecycle.InstancePerCall);

            //Hack because we can't register as IStartableBus because it would automatically register as IBus and overrode the proper IBus registration.
            context.Container.ConfigureComponent<IRealBus>(CreateBus, DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => (IStartableBus)b.Build<IRealBus>(), DependencyLifecycle.SingleInstance);

            context.Container.ConfigureComponent(b =>
            {
                var stacker = b.Build<BehaviorContextStacker>();
                if (stacker.Current != null)
                {
                    return CreateContextualBus(b, () => stacker.Current);
                }
                return (IBus)b.Build<IRealBus>();
            }, DependencyLifecycle.InstancePerCall);

            var knownMessages = context.Settings.GetAvailableTypes()
                .Where(context.Settings.Get<Conventions>().IsMessageType)
                .ToList();

            ConfigureMessageRegistry(context, knownMessages);

            HardcodedPipelineSteps.RegisterOutgoingCoreBehaviors(context.Pipeline);

            if (context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                return;
            }



            HardcodedPipelineSteps.RegisterIncomingCoreBehaviors(context.Pipeline);

            var transactionSettings = new TransactionSettings(context.Settings);

            if (transactionSettings.DoNotWrapHandlersExecutionInATransactionScope)
            {
                context.Pipeline.Register<SuppressAmbientTransactionBehavior.Registration>();
            }
            else
            {
                context.Pipeline.Register<HandlerTransactionScopeWrapperBehavior.Registration>();
            }
        }

        Unicast.UnicastBus CreateBus(IBuilder builder)
        {
            var bus = new Unicast.UnicastBus(
                builder.Build<BehaviorContextStacker>().Root,
                builder.Build<IExecutor>(),
                builder.Build<CriticalError>(),
                builder.Build<IMessageMapper>(),
                builder,
                builder.Build<Configure>(),
                builder.Build<IManageSubscriptions>(),
                builder.Build<ReadOnlySettings>(),
                builder.Build<TransportDefinition>(),
                builder.Build<IDispatchMessages>(),
                builder.Build<StaticMessageRouter>());

            return bus;
        }

        ContextualBus CreateContextualBus(IBuilder builder, Func<BehaviorContext> currentContextGetter)
        {
            return new ContextualBus(currentContextGetter,
                builder.Build<IMessageMapper>(),
                builder,
                builder.Build<Configure>(),
                builder.Build<IManageSubscriptions>(),
                builder.Build<ReadOnlySettings>(),
                builder.Build<TransportDefinition>(),
                builder.Build<IDispatchMessages>(),
                builder.Build<StaticMessageRouter>());
        }

        void ConfigureMessageRegistry(FeatureConfigurationContext context, IEnumerable<Type> knownMessages)
        {
            var messageRegistry = new MessageMetadataRegistry(context.Settings.Get<Conventions>());

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