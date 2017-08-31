using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Settings;
using NServiceBus.TransportTests;
using NUnit.Framework;

class ConfigureLearningTransportInfrastructure : IConfigureTransportInfrastructure
{
    public TransportConfigurationResult Configure(SettingsHolder settings, TransportTransactionMode transactionMode)
    {
        storageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".transporttests");
        settings.Set("LearningTransport.StoragePath", storageDir);

        TestContext.Out.WriteLine($"LearningTransport.StoragePath :: {storageDir}");

        var transportDefinition = new LearningTransport();

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