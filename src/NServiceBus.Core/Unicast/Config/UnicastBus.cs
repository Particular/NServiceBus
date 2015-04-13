namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Config;
    using Logging;
    using NServiceBus.Hosting;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using NServiceBus.Settings.Concurrency;
    using NServiceBus.Settings.Throttling;
    using NServiceBus.Support;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using Pipeline;
    using Unicast.Messages;
    using Unicast.Routing;
    using TransactionSettings = NServiceBus.Unicast.Transport.TransactionSettings;

    class UnicastBus : Feature
    {
        internal const string HostIdSettingsKey = "NServiceBus.HostInformation.HostId";

        internal UnicastBus()
        {
            EnableByDefault();

            Defaults(s =>
            {
                string fullPathToStartingExe;
                s.SetDefault(HostIdSettingsKey, GenerateDefaultHostId(out fullPathToStartingExe));
                s.SetDefault("NServiceBus.HostInformation.DisplayName", RuntimeEnvironment.MachineName);
                s.SetDefault("NServiceBus.HostInformation.Properties", new Dictionary<string, string>
                {
                    {"Machine", RuntimeEnvironment.MachineName},
                    {"ProcessID", Process.GetCurrentProcess().Id.ToString()},
                    {"UserName", Environment.UserName},
                    {"PathToExecutable", fullPathToStartingExe}
                });
                s.SetDefault<IConcurrencyConfig>(new SharedConcurrencyConfig(null));
                s.SetDefault<IThrottlingConfig>(new NoLimitThrottlingConfig());
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var hostInfo = new HostInformation(context.Settings.Get<Guid>(HostIdSettingsKey),
                context.Settings.Get<string>("NServiceBus.HostInformation.DisplayName"),
                context.Settings.Get<Dictionary<string, string>>("NServiceBus.HostInformation.Properties"));

            context.Container.RegisterSingleton(hostInfo);
            context.Pipeline.Register<HostInformationBehavior.Registration>();

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

            context.Container.ConfigureComponent(b => throttlingConfig.WrapExecutor(concurrencyConfig.BuildExecutor(b.Build<BusNotifications>())), DependencyLifecycle.SingleInstance);

            context.Container.ConfigureComponent<MainPipelineFactory>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<SatellitePipelineFactory>(DependencyLifecycle.SingleInstance);

            context.Container.ConfigureComponent<BehaviorContextStacker>(DependencyLifecycle.SingleInstance);

            context.Container.ConfigureComponent(b => b.Build<BehaviorContextStacker>().GetCurrentContext(), DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent<CallbackMessageLookup>(DependencyLifecycle.SingleInstance);

            //Hack because we can't register as IStartableBus because it would automatically register as IBus and overrode the proper IBus registration.
            context.Container.ConfigureComponent<IRealBus>(b => CreateBus(b, hostInfo), DependencyLifecycle.SingleInstance);
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


			ConfigureSubscriptionAuthorization(context);


            var knownMessages = context.Settings.GetAvailableTypes()
                .Where(context.Settings.Get<Conventions>().IsMessageType)
                .ToList();

            RegisterMessageOwnersAndBusAddress(context, knownMessages);

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
           
            context.Pipeline.Register<EnforceMessageIdBehavior.Registration>();   
        }

        Unicast.UnicastBus CreateBus(IBuilder builder, HostInformation hostInfo)
        {
            var bus = new Unicast.UnicastBus(
                builder.Build<IExecutor>(),
                builder.Build<CriticalError>(),
                builder.BuildAll<PipelineFactory>(),
                builder.Build<IMessageMapper>(),
                builder,
                builder.Build<Configure>(),
                builder.Build<IManageSubscriptions>(),
                builder.Build<MessageMetadataRegistry>(),
                builder.Build<ReadOnlySettings>(),
                builder.Build<TransportDefinition>(),
                builder.Build<ISendMessages>(),
                builder.Build<StaticMessageRouter>(),
                builder.Build<CallbackMessageLookup>())
            {
                HostInformation = hostInfo
            };
            return bus;
        }

        ContextualBus CreateContextualBus(IBuilder builder, Func<BehaviorContext> currentContextGetter)
        {
            return new ContextualBus(currentContextGetter,
                builder.Build<IMessageMapper>(),
                builder,
                builder.Build<Configure>(),
                builder.Build<IManageSubscriptions>(),
                builder.Build<MessageMetadataRegistry>(), 
                builder.Build<ReadOnlySettings>(),
                builder.Build<TransportDefinition>(),
                builder.Build<ISendMessages>(),
                builder.Build<StaticMessageRouter>(),
                builder.Build<CallbackMessageLookup>());
        }

        static Guid GenerateDefaultHostId(out string fullPathToStartingExe)
        {
            var gen = new DefaultHostIdGenerator(Environment.CommandLine, RuntimeEnvironment.MachineName);

            fullPathToStartingExe = gen.FullPathToStartingExe;

            return gen.HostId;
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
            
            context.Container.RegisterSingleton(router);

            if (unicastConfig == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(unicastConfig.ForwardReceivedMessagesTo))
            {
                var forwardAddress = unicastConfig.ForwardReceivedMessagesTo;
                context.Container.ConfigureProperty<ForwardBehavior>(b => b.ForwardReceivedMessagesTo, forwardAddress);
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