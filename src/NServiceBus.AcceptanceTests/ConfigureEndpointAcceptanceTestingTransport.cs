namespace NServiceBus.AcceptanceTests;

using System;
using System.IO;
using System.Threading.Tasks;
using AcceptanceTesting.Customization;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NUnit.Framework;

public class ConfigureEndpointAcceptanceTestingTransport(
    bool useNativePubSub,
    bool useNativeDelayedDelivery,
    TransportTransactionMode? transactionMode = null,
    bool? enforcePublisherMetadata = null)
    : IConfigureEndpointTestExecution
{
    public Task Cleanup()
    {
        try
        {
            if (Directory.Exists(storageDir))
            {
                Directory.Delete(storageDir, true);
            }
        }
        catch
        {
            // ignored
        }

        return Task.CompletedTask;
    }

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings,
        PublisherMetadata publisherMetadata)
    {
        var testRunId = TestContext.CurrentContext.Test.ID;

        string tempDir =
            //can't use bin dir since that will be too long on the build agents
            Environment.OSVersion.Platform == PlatformID.Win32NT ? @"c:\temp" : Path.GetTempPath();

        storageDir = Path.Combine(tempDir, "acc", testRunId);

        var acceptanceTestingTransport = new AcceptanceTestingTransport(
            enableNativeDelayedDelivery: useNativeDelayedDelivery,
            enableNativePublishSubscribe: useNativePubSub)
        {
            StorageLocation = storageDir,
        };

        if (transactionMode.HasValue)
        {
            acceptanceTestingTransport.TransportTransactionMode = transactionMode.Value;
        }

        if (enforcePublisherMetadata.GetValueOrDefault(false))
        {
            configuration.EnforcePublisherMetadataRegistration(endpointName, publisherMetadata);
        }

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

        return Task.CompletedTask;
    }

    string storageDir;
}