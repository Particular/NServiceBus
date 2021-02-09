using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;
using NServiceBus.TransportTests;

class ConfigureLearningTransportInfrastructure : IConfigureTransportInfrastructure
{
    public TransportDefinition CreateTransportDefinition()
    {
        storageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".transporttests");
        return new LearningTransport
        {
            StorageDirectory = storageDir,
        };
    }

    public async Task<TransportInfrastructure> Configure(TransportDefinition transportDefinition, HostSettings hostSettings, string inputQueueName, string errorQueueName)
    {
        var mainReceiverSettings = new ReceiveSettings(
            "mainReceiver",
            inputQueueName,
            transportDefinition.SupportsPublishSubscribe,
            true,
            errorQueueName);

        var transportInfrastructure = await transportDefinition.Initialize(
            hostSettings,
            new[] { mainReceiverSettings },
            new[] { errorQueueName });

        return transportInfrastructure;
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