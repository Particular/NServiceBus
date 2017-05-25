using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Transport;

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
        //can't use bin dir since that will be too long on the build agents
        storageDir = Path.Combine(@"c:\temp", "att_tests");

        //we want the tests to be exposed to concurrency
        configuration.LimitMessageProcessingConcurrencyTo(PushRuntimeSettings.Default.MaxConcurrency);

        configuration.UseTransport<LearningTransport>()
            .StorageDirectory(storageDir);

        return Task.FromResult(0);
    }

    string storageDir;
}
