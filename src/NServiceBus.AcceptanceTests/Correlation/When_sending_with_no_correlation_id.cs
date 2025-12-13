namespace NServiceBus.AcceptanceTests.Correlation;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_sending_with_no_correlation_id : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_use_the_message_id_as_the_correlation_id()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<CorrelationEndpoint>(b => b.When(session => session.SendLocal(new MyRequest())))
            .Run();

        Assert.That(context.CorrelationIdReceived, Is.EqualTo(context.MessageIdReceived), "Correlation id should match MessageId");
    }

    public class Context : ScenarioContext
    {
        public string MessageIdReceived { get; set; }
        public string CorrelationIdReceived { get; set; }
    }

    public class CorrelationEndpoint : EndpointConfigurationBuilder
    {
        public CorrelationEndpoint() => EndpointSetup<DefaultServer>();

        public class MyResponseHandler(Context testContext) : IHandleMessages<MyRequest>
        {
            public Task Handle(MyRequest message, IMessageHandlerContext context)
            {
                testContext.CorrelationIdReceived = context.MessageHeaders[Headers.CorrelationId];
                testContext.MessageIdReceived = context.MessageId;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MyRequest : IMessage;
}