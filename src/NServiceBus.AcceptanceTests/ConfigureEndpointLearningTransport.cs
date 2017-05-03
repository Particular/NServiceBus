using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureEndpointLearningTransport : IConfigureEndpointTestExecution
{
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
        storageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".att_tests");
     
        configuration.UseTransport<LearningTransport>()
            .StorageDirectory(storageDir);

        return Task.FromResult(0);
    }

    string storageDir;
}