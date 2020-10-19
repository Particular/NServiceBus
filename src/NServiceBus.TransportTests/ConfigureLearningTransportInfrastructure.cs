using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;
using NServiceBus.TransportTests;

class ConfigureLearningTransportInfrastructure : IConfigureTransportInfrastructure
{
    public TransportConfigurationResult Configure(Settings settings)
    {
        storageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".transporttests");

        var transportDefinition = new LearningTransport
        {
            StorageDirectory = storageDir
        };

        return new TransportConfigurationResult
        {
            TransportDefinition = transportDefinition,
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