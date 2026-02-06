namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Linq;
using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus.AcceptanceTesting;
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
                    sendOptions.SetHeader(Headers.DiagnosticsBaggage, "key1=value1,key2=value2,key3=");
                    await session.Send(new SomeMessage(), sendOptions);
                })
            )
            .Run();

        var incomingMessageTraces = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        Assert.That(incomingMessageTraces, Has.Count.EqualTo(1), "There should be 1 message received");
        var incomingMessageTrace = incomingMessageTraces.Single();

        VerifyBaggageItem("key1", "value1");
        VerifyBaggageItem("key2", "value2");
        VerifyBaggageItem("key3", "");
        return;

        void VerifyBaggageItem(string key, string expectedValue)
        {
            var baggageItemValue = incomingMessageTrace.GetBaggageItem(key);
            Assert.That(baggageItemValue, Is.Not.Null, $"Baggage item {key} should be populated");
            Assert.That(baggageItemValue, Is.EqualTo(expectedValue), $"Baggage item {key} is not set correctly");
        }
    }

    public class TestEndpoint : EndpointConfigurationBuilder
    {
        public TestEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class SomeMessageHandler(Context testContext) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }

    }

    public class SomeMessage : IMessage;

    public class Context : ScenarioContext;
}