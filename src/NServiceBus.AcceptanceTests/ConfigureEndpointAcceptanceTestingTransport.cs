using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NUnit.Framework;

public class ConfigureEndpointAcceptanceTestingTransport : IConfigureEndpointTestExecution
{
    public ConfigureEndpointAcceptanceTestingTransport(bool useNativePubSub, bool useNativeDelayedDelivery, TransportTransactionMode transactionMode = TransportTransactionMode.ReceiveOnly)
    {
        this.useNativePubSub = useNativePubSub;
        this.useNativeDelayedDelivery = useNativeDelayedDelivery;
        this.transactionMode = transactionMode;
    }

    public Task Cleanup()
    {
        if (Directory.Exists(storageDir))
        {
            Directory.Delete(storageDir, true);
        }

        return Task.FromResult(0);
    }

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var testRunId = TestContext.CurrentContext.Test.ID;

        string tempDir;

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            //can't use bin dir since that will be too long on the build agents
            tempDir = @"c:\temp";
        }
        else
        {
            tempDir = Path.GetTempPath();
        }

        storageDir = Path.Combine(tempDir, testRunId);

        var acceptanceTestingTransport = new AcceptanceTestingTransport(
            enableNativeDelayedDelivery: useNativeDelayedDelivery,
            enableNativePublishSubscribe: useNativePubSub)
        {
            StorageLocation = storageDir,
            TransportTransactionMode = transactionMode
        };
        var routing = configuration.UseTransport(acceptanceTestingTransport);

        if (!useNativePubSub)
        {
            //apply publisher registrations required for message driven pub/sub
            foreach (var publisherMetadataPublisher in publisherMetadata.Publishers)
            {
                foreach (var @event in publisherMetadataPublisher.Events)
                {
                    routing.RegisterPublisher(@event, publisherMetadataPublisher.PublisherName);
                }
            }
        }

        return Task.FromResult(0);
    }

    readonly bool useNativePubSub;
    readonly bool useNativeDelayedDelivery;
    readonly TransportTransactionMode transactionMode;

    string storageDir;
}