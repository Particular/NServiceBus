using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NUnit.Framework;

public class ConfigureEndpointAcceptanceTestingTransport : IConfigureEndpointTestExecution
{
    public ConfigureEndpointAcceptanceTestingTransport(bool useNativePubSub, bool useNativeDelayedDelivery)
    {
        this.useNativePubSub = useNativePubSub;
        this.useNativeDelayedDelivery = useNativeDelayedDelivery;
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

        var transportConfig = configuration.UseTransport<AcceptanceTestingTransport>()
            .StorageDirectory(storageDir)
            .UseNativePubSub(useNativePubSub)
            .UseNativeDelayedDelivery(useNativeDelayedDelivery);

        if (!useNativePubSub)
        {
            //apply publisher registrations required for message driven pub/sub
            var routingConfig = transportConfig.Routing();
            foreach (var publisherMetadataPublisher in publisherMetadata.Publishers)
            {
                foreach (var @event in publisherMetadataPublisher.Events)
                {
                    routingConfig.RegisterPublisher(@event, publisherMetadataPublisher.PublisherName);
                }
            }
        }

        return Task.FromResult(0);
    }

    readonly bool useNativePubSub;
    readonly bool useNativeDelayedDelivery;

    string storageDir;
}