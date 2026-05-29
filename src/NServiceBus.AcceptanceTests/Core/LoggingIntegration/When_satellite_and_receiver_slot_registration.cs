#nullable enable

namespace NServiceBus.AcceptanceTests.Core.LoggingIntegration;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Transport;

public class When_satellite_and_receiver_slot_registration : NServiceBusAcceptanceTest
{
    const string SateliteName = "MySatellite";

    [Test]
    public async Task Should_enrich_satellite_logs_with_satellite_scope()
    {
        var context = await Scenario.Define<Context>(ctx => ctx.IncludeLoggingScopes = true)
            .WithEndpoint<EndpointWithSatellite>(b => b
                .When((session, _) => session.Send(EndpointWithSatellite.SatelliteAddress!, new SatelliteMessage())))
            .Run();

        Assert.That(context.Logs, Has.One.Matches<ScenarioContext.LogItem>(l =>
            l.LoggerName == "SatelliteHandler" &&
            (l.Message ?? string.Empty).Contains("Satellite processed") &&
            (l.Message ?? string.Empty).Contains("Endpoint = SatelliteAndReceiverSlotRegistration.EndpointWithSatellite, EndpointIdentifier = SatelliteAndReceiverSlotRegistration.EndpointWithSatellite0") &&
            (l.Message ?? string.Empty).Contains($"Satellite = {SateliteName}")));
    }

    public class Context : ScenarioContext;

    public class EndpointWithSatellite : EndpointConfigurationBuilder
    {
        public static string? SatelliteAddress { get; set; }

        public EndpointWithSatellite() => EndpointSetup<DefaultServer>(c => c.EnableFeature<MySatelliteFeature>());

        public class MySatelliteFeature : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                var endpointQueueName = context.Settings.EndpointQueueName();
                var queueAddress = new QueueAddress(endpointQueueName, qualifier: SateliteName);

                context.AddSatelliteReceiver(
                    SateliteName,
                    queueAddress,
                    PushRuntimeSettings.Default,
                    (c, ec) => RecoverabilityAction.MoveToError(c.Failed.ErrorQueue),
                    (builder, messageContext, cancellationToken) =>
                    {
                        var logger = builder.GetRequiredService<ILoggerFactory>().CreateLogger("SatelliteHandler");
                        logger.LogInformation("Satellite processed");
                        builder.GetRequiredService<Context>().MarkAsCompleted();
                        return Task.FromResult(true);
                    });

                context.RegisterStartupTask<SatelliteStartupTask>();
            }

            class SatelliteStartupTask(ReceiveAddresses receiveAddresses) : FeatureStartupTask
            {
                protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    SatelliteAddress = receiveAddresses.SatelliteReceiveAddresses.Single();
                    return Task.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }
        }
    }

    public class SatelliteMessage : IMessage;
}