namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_processing_message_with_convention_based_handler : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_trace_original_handler_type()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<ReceivingEndpoint>(b => b.When(session => session.SendLocal(new SomeMessage())))
            .Run();

        var invokedHandlerActivities = NServiceBusActivityListener.CompletedActivities.GetInvokedHandlerActivities();

        Assert.That(invokedHandlerActivities, Has.Count.EqualTo(1), "one handler should be invoked");

        var handlerActivityTags = invokedHandlerActivities.Single().Tags.ToImmutableDictionary();
        handlerActivityTags.VerifyTag("nservicebus.handler.handler_type", typeof(ReceivingEndpoint.ConventionBasedHandler).FullName);
    }

    public class Context : ScenarioContext
    {
        public bool MessageHandled { get; set; }
    }

    public class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() => EndpointSetup<NonScanningServer>(c =>
        {
            c.AddHandler<ConventionBasedHandler>();
        });

        [Handler]
        public class ConventionBasedHandler(Context testContext)
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.MessageHandled = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SomeMessage : IMessage;
}