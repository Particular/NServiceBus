namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_processing_message_with_saga_handler : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_trace_saga_id()
    {
        var businessId = Guid.NewGuid();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b =>
                b.When(session => session.SendLocal(new SomeMessage { BusinessId = businessId }))
            )
            .Done(ctx => ctx.MessageHandled)
            .Run();

        var invokedHandlerActivities = NServicebusActivityListener.CompletedActivities.GetInvokedHandlerActivities();

        Assert.That(invokedHandlerActivities.Count, Is.EqualTo(1), "One handlers should be invoked");

        var handlerActivityTags = invokedHandlerActivities.Single().Tags.ToImmutableDictionary();
        handlerActivityTags.VerifyTag("nservicebus.handler.handler_type", typeof(TestEndpoint.TracedSaga).FullName);
        handlerActivityTags.VerifyTag("nservicebus.handler.saga_id", context.SagaId);
    }

    public class Context : ScenarioContext
    {
        public string SagaId { get; set; }
        public bool MessageHandled { get; set; }
    }

    public class TestEndpoint : EndpointConfigurationBuilder
    {
        public TestEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

        public class TracedSaga : Saga<TracedSaga.TracedSagaData>, IAmStartedByMessages<SomeMessage>
        {
            Context testContext;

            public TracedSaga(Context testContext) => this.testContext = testContext;

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TracedSagaData> mapper)
                => mapper.MapSaga(saga => saga.BusinessId)
                        .ToMessage<SomeMessage>(msg => msg.BusinessId);

            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.MessageHandled = true;
                testContext.SagaId = Data.Id.ToString();
                return Task.CompletedTask;
            }

            public class TracedSagaData : ContainSagaData
            {
                public virtual Guid BusinessId { get; set; }
            }
        }
    }

    public class SomeMessage : IMessage
    {
        public Guid BusinessId { get; set; }
    }
}