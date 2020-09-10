namespace NServiceBus.AcceptanceTests.Correlation
{
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
                .Done(c => c.GotResponse)
                .Run();

            Assert.AreEqual(mycustomid, context.CorrelationIdReceived, "Correlation id should match MessageId");
        }

        public class Context : ScenarioContext
        {
            public bool GotResponse { get; set; }
            public string CorrelationIdReceived { get; set; }
        }

        public class CorrelationEndpoint : EndpointConfigurationBuilder
        {
            public CorrelationEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.RegisterMessageMutator(new RemoveCorrelationIdMutator()));
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Task Handle(MyRequest message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    return context.Reply(new MyResponse());
                }
            }

            public class MyResponseHandler : IHandleMessages<MyResponse>
            {
                public MyResponseHandler(Context context)
                {
                    this.context = context;
                }

                public Task Handle(MyResponse message, IMessageHandlerContext c, System.Threading.CancellationToken cancellationToken)
                {
                    context.CorrelationIdReceived = c.MessageHeaders[Headers.CorrelationId];
                    context.GotResponse = true;

                    return Task.FromResult(0);
                }

                readonly Context context;
            }

            class RemoveCorrelationIdMutator : IMutateIncomingTransportMessages
            {
                public Task MutateIncoming(MutateIncomingTransportMessageContext context)
                {
                    if (context.Headers[Headers.MessageIntent] != MessageIntentEnum.Reply.ToString())
                    {
                        context.Headers.Remove(Headers.CorrelationId);
                    }

                    return Task.FromResult(0);
                }
            }
        }

        public class MyRequest : IMessage
        {
        }

        public class MyResponse : IMessage
        {
        }
    }
}