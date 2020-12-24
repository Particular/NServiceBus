using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Settings;
using NServiceBus.Transport;
using NServiceBus.TransportTests;

class ConfigureLearningTransportInfrastructure : IConfigureTransportInfrastructure
{
    public async Task<TransportConfigurationResult> Configure(HostSettings hostSettings, string inputQueueName, string errorQueueName, TransportTransactionMode transactionMode)
    {
        storageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".transporttests");
        var transportDefinition = new LearningTransport
        {
            StorageDirectory = storageDir
        };

        var mainReceiverSettings = new ReceiveSettings(
            "mainReceiver",
            inputQueueName,
            transportDefinition.SupportsPublishSubscribe,
            true, errorQueueName,
            transactionMode);

        return new TransportConfigurationResult
        {
            TransportDefinition = transportDefinition,
            TransportInfrastructure = await transportDefinition.Initialize(hostSettings, new[] {mainReceiverSettings}, new[] {errorQueueName}),
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