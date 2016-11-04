namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Routing;
    using Settings;
    using Transport;

    class InitializableRawEndpoint
    {
        public InitializableRawEndpoint(SettingsHolder settings, Func<MessageContext, IDispatchMessages, Task> onMessage)
        {
            this.settings = settings;
            this.onMessage = onMessage;
        }

        public Task<IStartableRawEndpoint> Initialize()
        {
            CreateCriticalErrorHandler();

            var transportDefinition = settings.Get<TransportDefinition>();
            var connectionString = settings.Get<TransportConnectionString>().GetConnectionStringOrRaiseError();
            var transportInfrastructure = transportDefinition.Initialize(settings, connectionString);
            settings.Set<TransportInfrastructure>(transportInfrastructure);

            var sendInfrastructure = transportInfrastructure.ConfigureSendInfrastructure();
            var dispatcher = sendInfrastructure.DispatcherFactory();

            TransportReceiveInfrastructure receiveInfrastructure = null;
            IPushMessages messagePump = null;
            if (!settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                receiveInfrastructure = transportInfrastructure.ConfigureReceiveInfrastructure();
                messagePump = receiveInfrastructure.MessagePumpFactory();

                var baseQueueName = settings.GetOrDefault<string>("BaseInputQueueName") ?? settings.EndpointName();

                var mainInstance = transportInfrastructure.BindToLocalEndpoint(new EndpointInstance(settings.EndpointName()));

                var mainLogicalAddress = LogicalAddress.CreateLocalAddress(baseQueueName, mainInstance.Properties);
                settings.SetDefault<LogicalAddress>(mainLogicalAddress);

                var mainAddress = transportInfrastructure.ToTransportAddress(mainLogicalAddress);
                settings.SetDefault("NServiceBus.SharedQueue", mainAddress);
            }

            var startableEndpoint = new StartableRawEndpoint(settings, transportInfrastructure, CreateCriticalErrorHandler(), messagePump, dispatcher, onMessage, () => ExecutePreStartupChecks(sendInfrastructure, receiveInfrastructure));
            return Task.FromResult<IStartableRawEndpoint>(startableEndpoint);
        }

        static async Task ExecutePreStartupChecks(TransportSendInfrastructure sendInfrastructure, TransportReceiveInfrastructure receiveInfrastructure)
        {
            var result = await sendInfrastructure.PreStartupCheck().ConfigureAwait(false);
            if (!result.Succeeded)
            {
                throw new Exception("Pre start-up check failed: " + result.ErrorMessage);
            }
            if (receiveInfrastructure != null)
            {
                result = await receiveInfrastructure.PreStartupCheck().ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    throw new Exception("Pre start-up check failed: " + result.ErrorMessage);
                }
            }
        }

        CriticalError CreateCriticalErrorHandler()
        {
            Func<ICriticalErrorContext, Task> errorAction;
            settings.TryGet("onCriticalErrorAction", out errorAction);
            return new CriticalError(errorAction);
        }

        SettingsHolder settings;
        Func<MessageContext, IDispatchMessages, Task> onMessage;
    }
}