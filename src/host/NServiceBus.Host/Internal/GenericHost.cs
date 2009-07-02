using System;
using System.Collections.Generic;
using System.Reflection;
using Common.Logging;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Subscriptions.Msmq;

namespace NServiceBus.Host.Internal
{
    public class GenericHost : MarshalByRefObject
    {
        public void Start()
        {
            Logger.Debug("Starting host for " + endpointType.Name);

            var specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);
            Configure cfg;
            

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
            bool startBusAutomatically = true;

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

                if (t == typeof(IDontWantTheBusStartedAutomatically))
                    startBusAutomatically = false;
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

            if (specifier is As.aClient)
                cfg
                    .MsmqTransport()
                        .IsTransactional(false)
                        .PurgeOnStartup(true)
                    .UnicastBus()
                        .ImpersonateSender(false)
                        .LoadMessageHandlers();

            if (specifier is As.aServer)
            {
                cfg
                    .MsmqTransport()
                        .IsTransactional(true)
                        .PurgeOnStartup(false)
                    .UnicastBus()
                        .ImpersonateSender(true)
                        .LoadMessageHandlers()
                    .Sagas();

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

            if (startBusAutomatically)
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

        private static readonly ILog Logger = LogManager.GetLogger(typeof(GenericHost));
    }
}