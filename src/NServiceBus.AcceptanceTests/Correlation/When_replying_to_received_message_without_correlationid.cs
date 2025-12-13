namespace NServiceBus.AcceptanceTests.Correlation;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using MessageMutator;
using NUnit.Framework;

public class When_replying_to_received_message_without_correlationid : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_use_the_incoming_message_id_as_the_correlation_id()
    {
        const string mycustomid = "mycustomid";

        var context = await Scenario.Define<Context>()
            .WithEndpoint<CorrelationEndpoint>(b => b.When(session =>
            {
                var sendOptions = new SendOptions();
                sendOptions.RouteToThisEndpoint();
                sendOptions.SetMessageId(mycustomid);
                return session.Send(new MyRequest(), sendOptions);
            }))
            .Run();

        Assert.That(context.CorrelationIdReceived, Is.EqualTo(mycustomid), "Correlation id should match MessageId");
    }

    public class Context : ScenarioContext
    {
        public string CorrelationIdReceived { get; set; }
    }

    public class CorrelationEndpoint : EndpointConfigurationBuilder
    {
        public CorrelationEndpoint() => EndpointSetup<DefaultServer>(c => c.RegisterMessageMutator(new RemoveCorrelationIdMutator()));

        public class MyRequestHandler : IHandleMessages<MyRequest>
        {
            public Task Handle(MyRequest message, IMessageHandlerContext context) => context.Reply(new MyResponse());
        }

        public class MyResponseHandler(Context context) : IHandleMessages<MyResponse>
        {
            public Task Handle(MyResponse message, IMessageHandlerContext c)
            {
                context.CorrelationIdReceived = c.MessageHeaders[Headers.CorrelationId];
                context.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }

        class RemoveCorrelationIdMutator : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                if (context.Headers[Headers.MessageIntent] != MessageIntent.Reply.ToString())
                {
                    context.Headers.Remove(Headers.CorrelationId);
                }

                return Task.CompletedTask;
            }
        }
    }

    public class MyRequest : IMessage;

    public class MyResponse : IMessage;
}