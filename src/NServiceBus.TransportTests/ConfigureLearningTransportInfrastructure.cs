using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Settings;
using NServiceBus.Transport;
using NServiceBus.TransportTests;

class ConfigureLearningTransportInfrastructure : IConfigureTransportInfrastructure
{
    public TransportConfigurationResult Configure(TransportSettings transportSettings)
    {
        storageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".transporttests");

        var transportDefinition = new LearningTransport
        {
            StorageDirectory = storageDir
        };

        return new TransportConfigurationResult
        {
            TransportInfrastructure = transportDefinition.Initialize(transportSettings),
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