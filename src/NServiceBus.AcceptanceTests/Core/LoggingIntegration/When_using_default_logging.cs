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

public class When_using_default_logging : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_write_to_rolling_file()
    {
        var logDirectory = Path.Combine(Path.GetTempPath(), "nsb-acceptance-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(logDirectory);

        try
        {
            Logging.LogManager.Use<Logging.DefaultFactory>().Directory(logDirectory);

            await Scenario.Define<Context>()
                // clearing out the context appender to ensure that only the default logging is used and we can verify the output
                .WithServices(services => services.AddLogging(l => l.ClearProviders()))
                .WithEndpoint<EndpointWithDefaultLogging>()
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
        finally
        {
            if (Directory.Exists(logDirectory))
            {
                Directory.Delete(logDirectory, true);
            }
        }
    }

    class Context : ScenarioContext;

    class EndpointWithDefaultLogging : EndpointConfigurationBuilder
    {
        public EndpointWithDefaultLogging() =>
            EndpointSetup<DefaultServer>();
    }
}