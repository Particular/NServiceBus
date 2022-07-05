namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    [NonParallelizable] // Ensure only activities for the current test are captured
    public class When_processing_message_with_saga_handler : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_trace_saga_id()
        {
            using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
            var businessId = Guid.NewGuid();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<TestEndpoint>(b =>
                    b.When(session => session.SendLocal(new SomeMessage { BusinessId = businessId }))
                )
                .Done(ctx => ctx.MessageHandled)
                .Run();

            Assert.AreEqual(activityListener.CompletedActivities.Count, activityListener.StartedActivities.Count, "all activities should be completed");

            var invokedHandlerActivities = activityListener.CompletedActivities.GetInvokedHandlerActivities();

            Assert.AreEqual(1, invokedHandlerActivities.Count, "One handlers should be invoked");

            var handlerActivityTags = invokedHandlerActivities.Single().Tags.ToImmutableDictionary();
            handlerActivityTags.VerifyTag("nservicebus.handler_type", typeof(TestEndpoint.TracedSaga).FullName);
            handlerActivityTags.VerifyTag("nservicebus.saga_id", context.SagaId);
        }

        public class Context : ScenarioContext
        {
            public string SagaId { get; set; }
            public bool MessageHandled { get; set; }
        }

        public class TestEndpoint : EndpointConfigurationBuilder
        {
            public TestEndpoint() => EndpointSetup<DefaultServer>();

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
}