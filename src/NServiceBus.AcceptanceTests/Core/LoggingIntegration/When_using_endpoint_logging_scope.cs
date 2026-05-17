#nullable enable

namespace NServiceBus.AcceptanceTests.Core.LoggingIntegration;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using Conventions = AcceptanceTesting.Customization.Conventions;

[NonParallelizable]
public class When_using_endpoint_logging_scope : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_expose_endpoint_metadata()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithScope>(b => b
                .ServiceResolve(static (provider, ctx, _) =>
                {
                    var endpointScope = provider.GetRequiredService<EndpointLoggingScope>();
                    ctx.ResolvedEndpointName = endpointScope.EndpointName;
                    ctx.ResolvedEndpointIdentifier = endpointScope.EndpointIdentifier;

                    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("ScopeTest");
                    using (logger.BeginScope(endpointScope))
                    {
                        logger.LogInformation("Message inside logging scope");
                    }

                    ctx.MarkAsCompleted();
                    return Task.CompletedTask;
                }, afterStart: true))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ResolvedEndpointName, Is.EqualTo(Conventions.EndpointNamingConvention(typeof(EndpointWithScope))));
            Assert.That(context.ResolvedEndpointIdentifier, Is.EqualTo("UsingEndpointLoggingScope.EndpointWithScope0"));
            Assert.That(context.Logs, Has.One.Matches<ScenarioContext.LogItem>(l =>
                l.LoggerName == "ScopeTest" &&
                (l.Message ?? string.Empty).Contains("Message inside logging scope")));
        }
    }

    class Context : ScenarioContext
    {
        public string? ResolvedEndpointName { get; set; }
        public object? ResolvedEndpointIdentifier { get; set; }
    }

    class EndpointWithScope : EndpointConfigurationBuilder
    {
        public EndpointWithScope() => EndpointSetup<DefaultServer>();
    }
}