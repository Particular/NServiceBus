using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Transport;
using NUnit.Framework;

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
        var testRunId = TestContext.CurrentContext.Test.ID;

        storageDir = Path.Combine(Path.GetTempPath(), testRunId);

        //we want the tests to be exposed to concurrency
        configuration.LimitMessageProcessingConcurrencyTo(PushRuntimeSettings.Default.MaxConcurrency);

        var transportDefinition = new LearningTransport();
        transportDefinition.StorageDirectory = storageDir;
        configuration.UseTransport(transportDefinition);

        return Task.FromResult(0);
    }

    string storageDir;
}
