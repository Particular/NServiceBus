using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Common.Logging;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Subscriptions.Msmq;
using System.Collections.Specialized;
using NServiceBus.Unicast.Config;

namespace NServiceBus.Host.Internal
{
    public class GenericHost : MarshalByRefObject
    {
        public void Start()
        {
            Trace.WriteLine("Starting host for " + endpointType.FullName);

            var specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);
            Configure cfg;

            if (!(specifier is IDontWantLog4Net))
            {
                var props = new NameValueCollection();
                props["configType"] = "INLINE";
                LogManager.Adapter = new Common.Logging.Log4Net.Log4NetLoggerFactoryAdapter(props);

                if (specifier is ISpecify.MyOwnLog4NetConfiguration)
                    (specifier as ISpecify.MyOwnLog4NetConfiguration).ConfigureLog4Net();
                else
                {
                    var layout = new log4net.Layout.PatternLayout("%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");
                    var appender = new log4net.Appender.ConsoleAppender
                                       {
                                           Layout = layout,
                                           Threshold = log4net.Core.Level.Debug
                                       };
                    log4net.Config.BasicConfigurator.Configure(appender);
                }
            }

            if (specifier is ISpecify.TypesToScan)
                cfg = Configure.With((specifier as ISpecify.TypesToScan).TypesToScan);
            else
                if (specifier is ISpecify.AssembliesToScan)
                    cfg = Configure.With(new List<Assembly>((specifier as ISpecify.AssembliesToScan).AssembliesToScan).ToArray());
                else
                    if (specifier is ISpecify.ProbeDirectory)
                        cfg = Configure.With((specifier as ISpecify.ProbeDirectory).ProbeDirectory);
                    else
                        cfg = Configure.With();

            Action startupAction = null;
            IContainer container = null;

            if (specifier is ISpecify.StartupAction)
                startupAction = (specifier as ISpecify.StartupAction).StartupAction;

            if (specifier is ISpecify.ContainerInstanceToUse)
                container = (specifier as ISpecify.ContainerInstanceToUse).ContainerInstance;
            
            Type containerType = null;
            Type messageEndpointType = null;

            foreach (var t in endpointType.GetInterfaces())
            {
                var args = t.GetGenericArguments();
                if (args.Length == 1)
                {
                    if (typeof(IContainer).IsAssignableFrom(args[0]))
                        if (typeof(ISpecify.ContainerTypeToUse<>).MakeGenericType(args[0]).IsAssignableFrom(endpointType))
                            containerType = args[0];

                    if (typeof(IMessageEndpoint).IsAssignableFrom(args[0]))
                        if (typeof(ISpecify.ToRun<>).MakeGenericType(args[0]).IsAssignableFrom(endpointType))
                            messageEndpointType = args[0];
                }
            }

            if (container != null)
            {
                ObjectBuilder.Common.Config.ConfigureCommon.With(cfg, container);
            }
            else if (containerType != null)
                ObjectBuilder.Common.Config.ConfigureCommon.With(
                                cfg,
                                Activator.CreateInstance(containerType) as IContainer
                                );
            else
                cfg.SpringBuilder();

            if (specifier is As.aClient && specifier is As.aServer)
                throw new InvalidOperationException("Cannot specify endpoint both as a client and as a server.");

            ConfigUnicastBus configUnicastBus = null;

            if (specifier is As.aClient)
                configUnicastBus = cfg
                    .MsmqTransport()
                        .IsTransactional(false)
                        .PurgeOnStartup(true)
                    .UnicastBus()
                        .ImpersonateSender(false);

            if (specifier is As.aServer)
            {
                configUnicastBus = cfg
                    .MsmqTransport()
                        .IsTransactional(true)
                        .PurgeOnStartup(false)
                    .Sagas()
                    .UnicastBus()
                        .ImpersonateSender(true);

                if (!(specifier is ISpecify.MyOwnSagaPersistence))
                    cfg.Configurer.ConfigureComponent<InMemorySagaPersister>(ComponentCallModelEnum.Singleton);

                if (specifier is As.aPublisher)
                {
                    var subscriptionConfig =
                        Configure.GetConfigSection<Config.DbSubscriptionStorageConfig>();

                    if (subscriptionConfig == null)
                    {
                        string q = Program.GetEndpointId(endpointType) + "_subscriptions";
                        cfg.Configurer.ConfigureComponent<MsmqSubscriptionStorage>(ComponentCallModelEnum.Singleton)
                            .ConfigureProperty(s => s.Queue, q);
                    }
                    else
                        cfg.DbSubscriptionStorage();
                }
            }

            if (configUnicastBus != null)
            {
                if (specifier is ISpecify.MessageHandlerOrdering)
                    (specifier as ISpecify.MessageHandlerOrdering).SpecifyOrder(new Order {config = configUnicastBus});
                else
                    configUnicastBus.LoadMessageHandlers();

                if (specifier is IDontWantToSubscribeAutomatically)
                    configUnicastBus.DoNotAutoSubscribe();
            }

            if (specifier is ISpecify.ToUseXmlSerialization)
            {
                if (specifier is ISpecify.XmlSerializationNamespace)
                    cfg.XmlSerializer((specifier as ISpecify.XmlSerializationNamespace).Namespace);
                else
                    cfg.XmlSerializer();
            }
            else
                if (!(specifier is ISpecify.MyOwnSerialization))
                    cfg.BinarySerializer();

            if (specifier is IWantCustomInitialization)
                (specifier as IWantCustomInitialization).Init(cfg);

            if (messageEndpointType != null)
                Configure.TypeConfigurer.ConfigureComponent(messageEndpointType, ComponentCallModelEnum.Singleton);

            messageEndpoint = Configure.ObjectBuilder.Build<IMessageEndpoint>();

            if (!(specifier is IDontWantTheBusStartedAutomatically))
                cfg.CreateBus().Start(startupAction);

            if (messageEndpoint != null)
                messageEndpoint.OnStart();
        }

        public void Stop()
        {
            if (messageEndpoint != null)
                messageEndpoint.OnStop();
        }

        public GenericHost(Type endpointType)
        {
            this.endpointType = endpointType;
        }

        private readonly Type endpointType;
        private IMessageEndpoint messageEndpoint;
    }
}