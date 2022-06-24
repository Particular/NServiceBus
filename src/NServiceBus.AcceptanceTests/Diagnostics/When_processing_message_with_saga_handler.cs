namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
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

            var invokedSagaActivity = invokedHandlerActivities.Single();

            var handlerType = invokedSagaActivity.GetTagItem("nservicebus.handler_type");
            Assert.NotNull(handlerType, "Handler type tag should be set");
            Assert.AreEqual(handlerType, typeof(TestEndpoint.MySaga).FullName, "invocation of saga should be recorded");

            var sagaId = invokedSagaActivity.GetTagItem("nservicebus.saga_id");
            Assert.NotNull(sagaId, "Saga Id tag should be set");
            Assert.AreEqual(context.SagaId, sagaId, "Saga Id does not match");
        }

        public class TestEndpoint : EndpointConfigurationBuilder
        {
            public TestEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MySaga : Saga<MySaga.SagaData>, IAmStartedByMessages<SomeMessage>
            {
                Context scenarioContext;

                public MySaga(Context scenarioContext) => this.scenarioContext = scenarioContext;

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
                    => mapper.MapSaga(saga => saga.BusinessId)
                            .ToMessage<SomeMessage>(msg => msg.BusinessId);

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    scenarioContext.MessageHandled = true;
                    scenarioContext.SagaId = Data.Id.ToString();
                    return Task.CompletedTask;
                }

                public class SagaData : ContainSagaData
                {
                    public Guid BusinessId { get; set; }
                }
            }
        }

        public class SomeMessage : IMessage
        {
            public Guid BusinessId { get; set; }
        }

        public class Context : ScenarioContext
        {
            public string SagaId { get; set; }
            public bool MessageHandled { get; set; }
        }
    }
}