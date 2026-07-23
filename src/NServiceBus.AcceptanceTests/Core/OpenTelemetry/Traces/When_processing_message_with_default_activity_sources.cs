namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Linq;
using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_processing_message_with_default_activity_sources : OpenTelemetryAcceptanceTest
{
    // Until v11, handler spans are emitted from the "NServiceBus.Core" ActivitySource by default
    // for backwards compatibility. The dedicated "NServiceBus.Core.Handler" source is opt-in via
    // the NServiceBus.Core.OpenTelemetry.UseHandlerActivitySource AppContext switch (default in v11).
    [Test]
    public async Task Should_emit_handler_span_from_main_source()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<ReceivingEndpoint>(b =>
                b.When(session => session.SendLocal(new SomeMessage()))
            )
            .Run();

        var invokedHandlerActivities = NServiceBusActivityListener.CompletedActivities.GetInvokedHandlerActivities();

        Assert.That(invokedHandlerActivities, Has.Count.EqualTo(1));
        Assert.That(invokedHandlerActivities.Single().Source.Name, Is.EqualTo("NServiceBus.Core"),
            "without the opt-in switch, handler spans must keep coming from the main source so existing OpenTelemetry configurations keep seeing them");
    }

    public class Context : ScenarioContext;

    public class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class MessageHandler(Context testContext) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SomeMessage : IMessage;
}
