#nullable enable

namespace NServiceBus.AcceptanceTests.Core.LoggingIntegration;

using System;
using System.IO;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NServiceBus.Logging;
using NUnit.Framework;

[NonParallelizable]
public class When_using_default_logging_internally_managed : NServiceBusAcceptanceTest
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
        // Reset LogManager to the default factory so the disposed externalLoggerFactory
        // is no longer referenced and subsequent tests start with a clean slate.
#pragma warning disable CS0618 // Test exercises deprecated DefaultFactory API intentionally
        LogManager.Use<DefaultFactory>();
#pragma warning restore CS0618

        if (Directory.Exists(logDirectory))
        {
            Directory.Delete(logDirectory, true);
        }
    }

    [Test]
    public async Task Should_write_to_rolling_file()
    {
#pragma warning disable CS0618 // Test exercises deprecated DefaultFactory API intentionally
        var defaultFactory = LogManager.Use<DefaultFactory>();
        defaultFactory.Directory(logDirectory);
        defaultFactory.Level(Logging.LogLevel.Debug);
#pragma warning restore CS0618

        await Scenario.Define<Context>()
            // clearing out the context appender to ensure that only the default logging is used and we can verify the output
            .WithServices(services => services.AddLogging(l => l.ClearProviders()))
            .WithEndpoint<EndpointWithDefaultLogging>(b =>
            {
                b.ToCreateInstance(
#pragma warning disable CS0618 // Type or member is obsolete
                    (_, configuration) => Endpoint.Create(configuration),
#pragma warning restore CS0618 // Type or member is obsolete
                    (startableEndpoint, _, ct) => startableEndpoint.Start(ct));
            })
            .Done(c => c.EndpointsStarted)
            .Run();

        var logFiles = Directory.GetFiles(logDirectory, "nsb_log_*.txt");
        Assert.That(logFiles, Is.Not.Empty, "Should have created at least one log file");

        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(logContent, Does.Contain("EndpointWithDefaultLogging"));
            Assert.That(logContent, Does.Contain("INFO"));
        }
    }

    public class Context : ScenarioContext;

    public class EndpointWithDefaultLogging : EndpointConfigurationBuilder
    {
        public EndpointWithDefaultLogging() => EndpointSetup<DefaultServer>();
    }
}