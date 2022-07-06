namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry
{
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_incoming_message_has_baggage_header : OpenTelemetryAcceptanceTest
    {
        [Test]
        public async Task Should_propagate_baggage_to_activity()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<TestEndpoint>(b =>
                    b.When(async session =>
                    {
                        var sendOptions = new SendOptions();
                        sendOptions.RouteToThisEndpoint();
                        sendOptions.SetHeader(Headers.DiagnosticsBaggage, "key1=value1,key2=value2");
                        await session.Send(new SomeMessage(), sendOptions);
                    })
                )
                .Done(ctx => ctx.MessageReceived)
                .Run();

            var incomingMessageTraces = NServicebusActivityListener.CompletedActivities.GetIncomingActivities();
            Assert.AreEqual(1, incomingMessageTraces.Count, "There should be 1 message received");
            var incomingMessageTrace = incomingMessageTraces.Single();

            VerifyBaggageItem("key1", "value1");
            VerifyBaggageItem("key2", "value2");

            void VerifyBaggageItem(string key, string expectedValue)
            {
                var baggageItemValue = incomingMessageTrace.GetBaggageItem(key);
                Assert.IsNotNull(baggageItemValue, $"Baggage item {key} should be populated");
                Assert.AreEqual(expectedValue, baggageItemValue, $"Baggage item {key} is not set correctly");
            }
        }

        class TestEndpoint : EndpointConfigurationBuilder
        {
            public TestEndpoint() => EndpointSetup<DefaultServer>();

            public class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                Context scenarioContext;

                public SomeMessageHandler(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    scenarioContext.MessageReceived = true;
                    return Task.CompletedTask;
                }
            }

        }

        public class SomeMessage : IMessage
        {
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; internal set; }
        }
    }
}