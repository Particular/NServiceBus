namespace NServiceBus.AcceptanceTests;

using System.IO;
using System.Threading.Tasks;
using AcceptanceTesting.Customization;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Transport;
using NUnit.Framework;

public class ConfigureEndpointLearningTransport(bool enforcePublisherMetadata = true) : IConfigureEndpointTestExecution
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
        catch { }

        return Task.CompletedTask;
    }

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var testRunId = TestContext.CurrentContext.Test.ID;

        storageDir = Path.Combine(Path.GetTempPath(), "learn", testRunId);

        if (enforcePublisherMetadata)
        {
            configuration.EnforcePublisherMetadataRegistration(endpointName, publisherMetadata);
        }

        //we want the tests to be exposed to concurrency
        configuration.LimitMessageProcessingConcurrencyTo(PushRuntimeSettings.Default.MaxConcurrency);

        var learningTransport = new LearningTransport
        {
            StorageDirectory = storageDir
        };
        configuration.UseTransport(learningTransport);

        return Task.CompletedTask;
    }

    string storageDir;
}
