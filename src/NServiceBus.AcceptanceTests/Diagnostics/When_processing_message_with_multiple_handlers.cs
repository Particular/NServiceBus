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
            TestContext.WriteLine($"Created listener {activityListener.GetHashCode()}");
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>(b =>
                    b.When(session => session.SendLocal(new SomeMessage()))
                )
                .Done(c => c.FirstHandlerRun && c.SecondHandlerRun)
                .Run();

            Assert.AreEqual(activityListener.CompletedActivities.Count, activityListener.StartedActivities.Count, "all activities should be completed");

            var invokedHandlerActivities = activityListener.CompletedActivities.GetInvokedHandlerActivities();

            Assert.AreEqual(2, invokedHandlerActivities.Count, "Two handlers should be invoked");

            var recordedHandlerTypes = new HashSet<string>();

            foreach (var invokedHandlerActivity in invokedHandlerActivities)
            {
                var handlerType = invokedHandlerActivity.GetTagItem("nservicebus.handler_type") as string;
                Assert.NotNull(handlerType, "Handler type tag should be set");
                recordedHandlerTypes.Add(handlerType);
            }

            Assert.True(recordedHandlerTypes.Contains(typeof(ReceivingEndpoint.HandlerOne).FullName), "invocation of handler one should be traced");
            Assert.True(recordedHandlerTypes.Contains(typeof(ReceivingEndpoint.HandlerTwo).FullName), "invocation of handler two should be traced");
        }

        class ReceivingEndpoint : EndpointConfigurationBuilder
        {
            public ReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class HandlerOne : IHandleMessages<SomeMessage>
            {
                Context scenarioContext;

                public HandlerOne(Context context)
                {
                    scenarioContext = context;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    scenarioContext.FirstHandlerRun = true;
                    return Task.CompletedTask;
                }
            }

            public class HandlerTwo : IHandleMessages<SomeMessage>
            {
                Context scenarioContext;

                public HandlerTwo(Context context)
                {
                    scenarioContext = context;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    scenarioContext.SecondHandlerRun = true;
                    return Task.CompletedTask;
                }
            }
        }

        class SomeMessage : IMessage
        {

        }

        class Context : ScenarioContext
        {
            public bool FirstHandlerRun { get; set; }
            public bool SecondHandlerRun { get; set; }
        }
    }
}