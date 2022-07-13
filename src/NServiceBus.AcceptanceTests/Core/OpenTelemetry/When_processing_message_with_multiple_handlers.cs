namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_processing_message_with_multiple_handlers : OpenTelemetryAcceptanceTest
    {
        [Test]
        public async Task Should_create_message_handler_spans()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>(b =>
                    b.When(session => session.SendLocal(new SomeMessage()))
                )
                .Done(c => c.FirstHandlerRun && c.SecondHandlerRun)
                .Run();

            var invokedHandlerActivities = NServicebusActivityListener.CompletedActivities.GetInvokedHandlerActivities();
            var receivePipelineActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities();

            Assert.AreEqual(2, invokedHandlerActivities.Count, "a dedicated span for each handler should be created");
            Assert.AreEqual(1, receivePipelineActivities.Count, "the receive pipeline should be invoked once");

            var recordedHandlerTypes = new HashSet<string>();

            foreach (var invokedHandlerActivity in invokedHandlerActivities)
            {
                var handlerTypeTag = invokedHandlerActivity.GetTagItem("nservicebus.handler.handler_type") as string;
                Assert.NotNull(handlerTypeTag, "Handler type tag should be set");
                recordedHandlerTypes.Add(handlerTypeTag);
                Assert.AreEqual(receivePipelineActivities[0].Id, invokedHandlerActivity.ParentId);
                Assert.AreEqual(ActivityStatusCode.Ok, invokedHandlerActivity.Status);
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
            public ReceivingEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

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