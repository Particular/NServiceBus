#nullable enable

namespace NServiceBus.AcceptanceTests.Core.LoggingIntegration;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Logging;
using NUnit.Framework;

public class When_logging_outside_slot_scope : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_forward_logs_to_scenario_context_via_withservice_resolve()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithOutOfSlotLogging>()
            .WithServiceResolve(static (_, ctx, _) =>
            {
                var logger = LogManager.GetLogger("OutOfSlotLoggerViaWithServiceResolve");
                logger.Debug("Out-of-slot log via WithServiceResolve");
                ctx.MarkAsCompleted();
                return Task.CompletedTask;
            })
            .Run();

        Assert.That(context.Logs, Has.One.Matches<ScenarioContext.LogItem>(l =>
            l.LoggerName == "OutOfSlotLoggerViaWithServiceResolve" &&
            (l.Message ?? string.Empty).Contains("Out-of-slot log via WithServiceResolve")));
    }

    [Test]
    public async Task Should_forward_logs_to_scenario_context_via_endpoint_service_resolve()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithOutOfSlotLogging>(b => b
                .ServiceResolve(static (_, ctx, _) =>
                {
                    var logger = LogManager.GetLogger("OutOfSlotLoggerViaEndpointServiceResolve");
                    logger.Debug("Out-of-slot log via endpoint ServiceResolve");
                    ctx.MarkAsCompleted();
                    return Task.CompletedTask;
                }))
            .Run();

        Assert.That(context.Logs, Has.One.Matches<ScenarioContext.LogItem>(l =>
            l.LoggerName == "OutOfSlotLoggerViaEndpointServiceResolve" &&
            (l.Message ?? string.Empty).Contains("Out-of-slot log via endpoint ServiceResolve")));
    }

    class Context : ScenarioContext;

    class EndpointWithOutOfSlotLogging : EndpointConfigurationBuilder
    {
        public EndpointWithOutOfSlotLogging() => EndpointSetup<DefaultServer>();
    }
}