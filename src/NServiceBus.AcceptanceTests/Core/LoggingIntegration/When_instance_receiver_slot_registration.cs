#nullable enable

namespace NServiceBus.AcceptanceTests.Core.LoggingIntegration;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class When_instance_receiver_slot_registration : NServiceBusAcceptanceTest
{
    static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(EndpointWithInstance));
    const string InstanceDiscriminator = "XYZ";

    [Test]
    public async Task Should_enrich_instance_receiver_logs_with_receiver_scope()
    {
        var context = await Scenario.Define<Context>(ctx => ctx.IncludeLoggingScopes = true)
            .WithEndpoint<EndpointWithInstance>(b => b
                .CustomConfig(c =>
                {
                    c.ConfigureRouting().RouteToEndpoint(typeof(InstanceMessage), ReceiverEndpoint);
                    c.MakeInstanceUniquelyAddressable(InstanceDiscriminator);
                })
                .When((session, _) =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToSpecificInstance(InstanceDiscriminator);
                    return session.Send(new InstanceMessage(), sendOptions);
                }))
            .Run();

        Assert.That(context.Logs, Has.One.Matches<ScenarioContext.LogItem>(l =>
            l.LoggerName.EndsWith("InstanceHandler") &&
            (l.Message ?? string.Empty).Contains("Instance processed") &&
            (l.Message ?? string.Empty).Contains("Endpoint = InstanceReceiverSlotRegistration.EndpointWithInstance, EndpointIdentifier = InstanceReceiverSlotRegistration.EndpointWithInstance0") &&
            (l.Message ?? string.Empty).Contains($"EndpointDiscriminator = {InstanceDiscriminator}")));
    }

    public class Context : ScenarioContext;

    public class EndpointWithInstance : EndpointConfigurationBuilder
    {
        public EndpointWithInstance() => EndpointSetup<DefaultServer>();

        [Handler]
        public class InstanceHandler(ILogger<InstanceHandler> logger, Context testContext) : IHandleMessages<InstanceMessage>
        {
            public Task Handle(InstanceMessage message, IMessageHandlerContext context)
            {
                logger.LogInformation("Instance processed");
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class InstanceMessage : IMessage;
}