using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NUnit.Framework;

public class ConfigureEndpointLearningPersistence : IConfigureEndpointTestExecution
{
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

        storageDir = Path.Combine(tempDir, "learn", testRunId);

        configuration.UsePersistence<LearningPersistence, StorageType.Sagas>()
            .SagaStorageDirectory(storageDir);

        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        try
        {
            if (Directory.Exists(storageDir))
            {
                Directory.Delete(storageDir, true);
            }
        }
        catch { }

        return Task.FromResult(0);
    }

    string storageDir;
}