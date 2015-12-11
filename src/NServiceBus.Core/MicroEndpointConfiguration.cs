namespace NServiceBus
{
    using System;
    using System.Transactions;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.ObjectBuilder.Autofac;
    using NServiceBus.ObjectBuilder.Common;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    /// <summary>
    /// Allows to configure the node.
    /// </summary>
    public class MicroEndpointConfiguration : ExposeSettings
    {
        LogicalAddress logicalAddress;
        PipelineConfiguration pipelineCollection;
        IBuilder builder;

        /// <summary>
        /// Creates new instance.
        /// </summary>
        public MicroEndpointConfiguration(LogicalAddress logicalAddress) 
            : base(new SettingsHolder())
        {
            this.logicalAddress = logicalAddress;
            Settings.SetDefault("Transactions.IsolationLevel", IsolationLevel.ReadCommitted);
            Settings.SetDefault("Transactions.DefaultTimeout", TransactionManager.DefaultTimeout);

            pipelineCollection = new PipelineConfiguration();
            Settings.Set<PipelineConfiguration>(pipelineCollection);
            Pipeline = new PipelineSettings(pipelineCollection.MainPipeline);

            var objectBuilder = new AutofacObjectBuilder();
            var b = new CommonObjectBuilder
            {
                Container = objectBuilder,
            };
            Container = b;
            builder = b;
            Container.ConfigureComponent<CommonObjectBuilder>(DependencyLifecycle.SingleInstance).ConfigureProperty(c => c.Container, objectBuilder);
        }

        /// <summary>
        ///     Access to the pipeline configuration.
        /// </summary>
        public PipelineSettings Pipeline { get; }

        /// <summary>
        /// Allows to configure container.
        /// </summary>
        public IConfigureComponents Container { get; }

        /// <summary>
        /// Initializes a new node.
        /// </summary>
        public StartableMicroEndpoint Initialize()
        {
            var inboundTransport = Settings.Get<InboundTransport>();
            var defaultTransportAddress = inboundTransport.Definition.ToTransportAddress(logicalAddress);

            //Settings.SetDefault<EndpointInstance>(logicalAddress);
            Settings.SetDefault<LogicalAddress>(logicalAddress);
            Settings.SetDefault("NServiceBus.LocalAddress", defaultTransportAddress);

            var receiveConfigResult = inboundTransport.Configure(Settings);
            Container.ConfigureComponent(b => receiveConfigResult.MessagePumpFactory(b.Build<CriticalError>()), DependencyLifecycle.InstancePerCall);
            Container.ConfigureComponent(b => receiveConfigResult.QueueCreatorFactory(), DependencyLifecycle.SingleInstance);

            var outboundTransport = Settings.Get<OutboundTransport>();
            var sendConfigResult = outboundTransport.Configure(Settings);
            Container.ConfigureComponent(b => sendConfigResult.DispatcherFactory(), DependencyLifecycle.SingleInstance);

            Container.ConfigureComponent(() => new CriticalError(null), DependencyLifecycle.SingleInstance);

            return new StartableMicroEndpoint(Settings, builder, pipelineCollection, new PushRuntimeSettings());
        }
        
        /// <summary>
        /// Configures NServiceBus node to use the given transport.
        /// </summary>
        public TransportExtensions<T> UseTransport<T>() where T : TransportDefinition, new()
        {
            var type = typeof(TransportExtensions<>).MakeGenericType(typeof(T));
            var extension = (TransportExtensions<T>)Activator.CreateInstance(type, Settings);

            var transportDefinition = new T();
            Settings.Set<InboundTransport>(new InboundTransport(transportDefinition));
            Settings.Set<OutboundTransport>(new OutboundTransport(transportDefinition, true));
            return extension;
        }
    }
}