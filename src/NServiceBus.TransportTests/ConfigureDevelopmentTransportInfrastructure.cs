using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Settings;
using NServiceBus.TransportTests;

class ConfigureDevelopmentTransportInfrastructure : IConfigureTransportInfrastructure
{
    public TransportConfigurationResult Configure(SettingsHolder settings, TransportTransactionMode transactionMode)
    {
        var msmqTransportDefinition = new DevelopmentTransport();
        settingsHolder = settings;
        return new TransportConfigurationResult
        {
            TransportInfrastructure = msmqTransportDefinition.Initialize(settingsHolder, ""),
            PurgeInputQueueOnStartup = true
        };
    }

    public Task Cleanup()
    {
        //todo
        return Task.FromResult(0);
    }

    SettingsHolder settingsHolder;
}