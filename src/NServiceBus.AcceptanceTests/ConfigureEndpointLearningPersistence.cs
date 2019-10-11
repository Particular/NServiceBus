using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NUnit.Framework;

public class ConfigureEndpointLearningPersistence : IConfigureEndpointTestExecution
{
    bool useInMemoryPersistenceForSubscriptionAndTimeoutSupport;

    public ConfigureEndpointLearningPersistence() : this(true)
    {
    }

    public ConfigureEndpointLearningPersistence(bool useInMemoryPersistenceForSubscriptionAndTimeoutSupport)
    {
        this.useInMemoryPersistenceForSubscriptionAndTimeoutSupport = useInMemoryPersistenceForSubscriptionAndTimeoutSupport;
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

        if (useInMemoryPersistenceForSubscriptionAndTimeoutSupport)
        {
            configuration.UsePersistence<InMemoryPersistence, StorageType.Subscriptions>();
            configuration.UsePersistence<InMemoryPersistence, StorageType.Timeouts>();
        }

        configuration.UsePersistence<LearningPersistence, StorageType.Sagas>()
            .SagaStorageDirectory(storageDir);

        return Task.FromResult(0);
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