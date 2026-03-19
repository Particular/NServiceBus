#nullable enable

namespace NServiceBus.AcceptanceTests.Core.LoggingIntegration;

using System;
using System.IO;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

[NonParallelizable]
public class When_using_rolling_logger_provider_options : NServiceBusAcceptanceTest
{
    string logDirectory;

    [SetUp]
    public void Setup()
    {
        logDirectory = Path.Combine(Path.GetTempPath(), "nsb-acceptance-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(logDirectory);
    }

    [TearDown]
    public void Teardown()
    {
        if (Directory.Exists(logDirectory))
        {
            Directory.Delete(logDirectory, true);
        }
    }

    [Test]
    public async Task Should_write_to_rolling_file_using_options()
    {
        var capturedDirectory = logDirectory;

        await Scenario.Define<Context>()
            .WithServices(services =>
            {
                services.AddLogging(l => l.ClearProviders());
                // Use PostConfigure to ensure our settings are applied after EndpointCreator's
                // legacy-config seeding, which also calls Configure<RollingLoggerProviderOptions>.
                services.PostConfigure<RollingLoggerProviderOptions>(o =>
                {
                    o.Directory = capturedDirectory;
                    o.LogLevel = LogLevel.Debug;
                });
            })
            .WithEndpoint<EndpointWithOptionsLogging>()
            .Done(c => c.EndpointsStarted)
            .Run();

        var logFiles = Directory.GetFiles(logDirectory, "nsb_log_*.txt");
        Assert.That(logFiles, Is.Not.Empty, "Should have created at least one log file");

        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(logContent, Does.Contain("EndpointWithOptionsLogging"));
            Assert.That(logContent, Does.Contain("DEBUG"));
        }
    }

    class Context : ScenarioContext;

    class EndpointWithOptionsLogging : EndpointConfigurationBuilder
    {
        public EndpointWithOptionsLogging() => EndpointSetup<DefaultServer>();
    }
}
