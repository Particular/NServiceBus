using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Configuration.AdvancedExtensibility;
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

        //ugly but it works
        if (!configuration.GetSettings().HasSetting("Diagnostics.RootPath"))
        {
            //no-op diagnostics
            configuration.CustomDiagnosticsWriter(d => Task.FromResult(0));
        }

        //we want the tests to be exposed to concurrency
        configuration.LimitMessageProcessingConcurrencyTo(PushRuntimeSettings.Default.MaxConcurrency);

        configuration.UseTransport<LearningTransport>()
            .StorageDirectory(storageDir);

        return Task.FromResult(0);
    }

    string storageDir;
}
