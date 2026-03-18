#nullable enable

namespace NServiceBus.AcceptanceTests.Core.LoggingIntegration;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

[NonParallelizable]
public class When_external_logging_provider_configured : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_use_external_provider_instead_of_default()
    {
        var customProvider = new CollectingLoggerProvider();

        await Scenario.Define<Context>()
            .WithServices(services =>
            {
                services.RemoveAll<ILoggerProvider>();
                services.AddSingleton<ILoggerProvider>(customProvider);
            })
            .WithEndpoint<EndpointWithExternalLogging>()
            .Done(c => c.EndpointsStarted)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(customProvider.LogEntries, Is.Not.Empty, "External provider should receive logs");

            var logContent = string.Join("\n", customProvider.LogEntries);
            Assert.That(logContent, Does.Contain("EndpointWithExternalLogging"));
            Assert.That(logContent, Does.Contain("Debug"));
        }
    }

    class Context : ScenarioContext;

    class EndpointWithExternalLogging : EndpointConfigurationBuilder
    {
        public EndpointWithExternalLogging() => EndpointSetup<DefaultServer>();
    }
}