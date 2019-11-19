namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Routing;
    using Settings;
    using Transport;

    class TransportComponent
    {
        public static Configuration PrepareConfiguration(Settings settings)
        {
            var transportDefinition = settings.TransportDefinition;
            var connectionString = settings.TransportConnectionString.GetConnectionStringOrRaiseError(transportDefinition);

            var transportInfrastructure = transportDefinition.Initialize(settings.RawSettings, connectionString);

            settings.RegisterTransportInfrastructureForBackwardsCompatibility(transportInfrastructure);

            return new Configuration(transportInfrastructure, settings.QueueBindings, settings.ReceivingEnabled);
        }

        public static TransportComponent Initialize(Configuration configuration, HostingComponent hostingComponent)
        {
            var transportComponent = new TransportComponent(configuration.transportInfrastructure, configuration.QueueBindings);

            if (configuration.ReceivingEnabled)
            {
                transportComponent.ConfigureReceiveInfrastructure();
            }

            hostingComponent.Container.ConfigureComponent(() => transportComponent.GetOrCreateDispatcher(), DependencyLifecycle.SingleInstance);

            hostingComponent.AddStartupDiagnosticsSection("Transport", new
            {
                Type = configuration.TransportType.FullName,
                Version = FileVersionRetriever.GetFileVersion(configuration.TransportType)
            });

            return transportComponent;
        }

        [ObsoleteEx(
            Message = "Change transport infrastructure to configure the send infrastructure at component initialization time",
            RemoveInVersion = "8")]
        public void ConfigureSendInfrastructureForBackwardsCompatibility()
        {
            transportSendInfrastructure = transportInfrastructure.ConfigureSendInfrastructure();
        }

        void ConfigureReceiveInfrastructure()
        {
            transportReceiveInfrastructure = transportInfrastructure.ConfigureReceiveInfrastructure();
        }

        [ObsoleteEx(
            Message = "Change transport infrastructure to run send pre-startup checks on component.Start",
            RemoveInVersion = "8")]
        public async Task InvokeSendPreStartupChecksForBackwardsCompatibility()
        {
            var sendResult = await transportSendInfrastructure.PreStartupCheck().ConfigureAwait(false);
            if (!sendResult.Succeeded)
            {
                throw new Exception($"Pre start-up check failed: {sendResult.ErrorMessage}");
            }
        }

        public IDispatchMessages GetOrCreateDispatcher()
        {
            return dispatcher ?? (dispatcher = transportSendInfrastructure.DispatcherFactory());
        }

        public Func<IPushMessages> GetMessagePumpFactory()
        {
            return transportReceiveInfrastructure.MessagePumpFactory;
        }

        public Task CreateQueuesIfNecessary(string username)
        {
            if (transportReceiveInfrastructure == null)
            {
                return TaskEx.CompletedTask;
            }

            var queueCreator = transportReceiveInfrastructure.QueueCreatorFactory();

            return queueCreator.CreateQueueIfNecessary(queueBindings, username);
        }

        public async Task Start()
        {
            if (transportReceiveInfrastructure != null)
            {
                var result = await transportReceiveInfrastructure.PreStartupCheck().ConfigureAwait(false);

                if (!result.Succeeded)
                {
                    throw new Exception($"Pre start-up check failed: {result.ErrorMessage}");
                }
            }

            await transportInfrastructure.Start().ConfigureAwait(false);
        }

        public Task Stop()
        {
            return transportInfrastructure.Stop();
        }

        protected TransportComponent(TransportInfrastructure transportInfrastructure, QueueBindings queueBindings)
        {
            this.transportInfrastructure = transportInfrastructure;
            this.queueBindings = queueBindings;
        }

        TransportInfrastructure transportInfrastructure;
        TransportSendInfrastructure transportSendInfrastructure;
        TransportReceiveInfrastructure transportReceiveInfrastructure;
        QueueBindings queueBindings;
        IDispatchMessages dispatcher;

        public class Configuration
        {
            public Configuration(TransportInfrastructure transportInfrastructure, QueueBindings queueBindings, bool receivingEnabled)
            {
                this.transportInfrastructure = transportInfrastructure;
                QueueBindings = queueBindings;
                ReceivingEnabled = receivingEnabled;
                TransportType = transportInfrastructure.GetType();
            }

            public EndpointInstance BindToLocalEndpoint(EndpointInstance endpointInstance)
            {
                return transportInfrastructure.BindToLocalEndpoint(endpointInstance);
            }

            public string ToTransportAddress(LogicalAddress logicalAddress)
            {
                return transportInfrastructure.ToTransportAddress(logicalAddress);
            }

            public QueueBindings QueueBindings { get; }

            public Type TransportType { get; }

            public bool ReceivingEnabled { get; }

            public readonly TransportInfrastructure transportInfrastructure;
        }

        public class Settings
        {
            public Settings(SettingsHolder settings)
            {
                this.settings = settings;

                settings.SetDefault(TransportConnectionString.Default);
                settings.Set(new QueueBindings());
            }

            public TransportDefinition TransportDefinition
            {
                get
                {
                    if (!settings.HasExplicitValue<TransportDefinition>())
                    {
                        throw new Exception("A transport has not been configured. Use 'EndpointConfiguration.UseTransport()' to specify a transport.");
                    }

                    return settings.Get<TransportDefinition>();
                }
                set { settings.Set(value); }
            }

            public TransportConnectionString TransportConnectionString
            {
                get { return settings.Get<TransportConnectionString>(); }
                set { settings.Set(value); }
            }

            public QueueBindings QueueBindings
            {
                get { return settings.Get<QueueBindings>(); }
            }

            public void RegisterTransportInfrastructureForBackwardsCompatibility(TransportInfrastructure transportInfrastructure)
            {
                settings.Set(transportInfrastructure);
            }

            public SettingsHolder RawSettings { get { return settings; } }

            public bool ReceivingEnabled { get { return !settings.Get<bool>("Endpoint.SendOnly"); } }

            readonly SettingsHolder settings;
        }
    }
}