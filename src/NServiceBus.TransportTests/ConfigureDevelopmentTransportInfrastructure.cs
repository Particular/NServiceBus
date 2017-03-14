using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Settings;
using NServiceBus.TransportTests;

class ConfigureDevelopmentTransportInfrastructure : IConfigureTransportInfrastructure
{
    public TransportConfigurationResult Configure(SettingsHolder settings, TransportTransactionMode transactionMode)
    {
        storageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "transporttests");
        settings.Set("DevelopmentTransport.StoragePath", storageDir);

        var transportDefinition = new DevelopmentTransport();
        return new TransportConfigurationResult
        {
            TransportInfrastructure = transportDefinition.Initialize(settings, ""),
            PurgeInputQueueOnStartup = true
        };
    }

    public Task Cleanup()
    {
        if (Directory.Exists(storageDir))
        {
            Directory.Delete(storageDir, true);
        }
        return Task.FromResult(0);
    }

    string storageDir;
}