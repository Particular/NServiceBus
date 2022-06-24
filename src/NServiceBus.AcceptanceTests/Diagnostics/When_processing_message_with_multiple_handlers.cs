namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    [NonParallelizable] // Ensure only activities for the current test are captured
    public class When_processing_message_with_multiple_handlers : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_create_message_handler_spans()
        {
            using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>(b =>
                    b.When(session => session.SendLocal(new SomeMessage()))
                )
                .Done(c => c.FirstHandlerRun && c.SecondHandlerRun)
                .Run();

            Assert.AreEqual(activityListener.CompletedActivities.Count, activityListener.StartedActivities.Count, "all activities should be completed");

            var invokedHandlerActivities = activityListener.CompletedActivities.GetInvokedHandlerActivities();
            var receivePipelineActivities = activityListener.CompletedActivities.GetIncomingActivities();

            Assert.AreEqual(2, invokedHandlerActivities.Count, "a dedicated span for each handler should be created");
            Assert.AreEqual(1, receivePipelineActivities.Count, "the receive pipeline should be invoked once");

            var recordedHandlerTypes = new HashSet<string>();

            foreach (var invokedHandlerActivity in invokedHandlerActivities)
            {
                var handlerType = invokedHandlerActivity.GetTagItem("nservicebus.handler_type") as string;
                Assert.NotNull(handlerType, "Handler type tag should be set");
                recordedHandlerTypes.Add(handlerType);
                Assert.AreEqual(receivePipelineActivities[0].Id, invokedHandlerActivity.ParentId);
            }

            Assert.True(recordedHandlerTypes.Contains(typeof(ReceivingEndpoint.HandlerOne).FullName), "invocation of handler one should be traced");
            Assert.True(recordedHandlerTypes.Contains(typeof(ReceivingEndpoint.HandlerTwo).FullName), "invocation of handler two should be traced");
        }

        class Context : ScenarioContext
        {
            public bool FirstHandlerRun { get; set; }
            public bool SecondHandlerRun { get; set; }
        }

        class ReceivingEndpoint : EndpointConfigurationBuilder
        {
            public ReceivingEndpoint() => EndpointSetup<DefaultServer>();

            public class HandlerOne : IHandleMessages<SomeMessage>
            {
                Context testContext;

                public HandlerOne(Context context) => testContext = context;

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    testContext.FirstHandlerRun = true;
                    return Task.CompletedTask;
                }
            }

            public class HandlerTwo : IHandleMessages<SomeMessage>
            {
                Context testContext;

                public HandlerTwo(Context context) => testContext = context;

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    testContext.SecondHandlerRun = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class SomeMessage : IMessage
        {
        }
    }
}