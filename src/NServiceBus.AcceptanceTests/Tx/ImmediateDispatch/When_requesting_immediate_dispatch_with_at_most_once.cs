namespace NServiceBus.AcceptanceTests.Tx
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_requesting_immediate_dispatch_with_at_most_once : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_dispatch_immediately()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<NonTransactionalEndpoint>(b => b
                        .When(bus => bus.SendLocal(new InitiatingMessage()))
                        .DoNotFailOnErrorMessages())
                    .Done(c => c.MessageDispatched)
                    .Run();

            Assert.True(context.MessageDispatched, "Should dispatch the message immediately");

        }

        public class Context : ScenarioContext
        {
            public bool MessageDispatched { get; set; }
        }

        public class NonTransactionalEndpoint : EndpointConfigurationBuilder
        {
            public NonTransactionalEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.UseTransport(context.GetTransportType()).Transactions(TransportTransactionMode.None);
                });
            }

            public class InitiatingMessageHandler : IHandleMessages<InitiatingMessage>
            {
                public async Task Handle(InitiatingMessage message, IMessageHandlerContext context)
                {
                    var options = new SendOptions();

                    options.RequireImmediateDispatch();
                    options.RouteToLocalEndpointInstance();

                    await context.Send(new MessageToBeDispatchedImmediately(), options);

                    throw new SimulatedException();
                }
            }

            public class MessageToBeDispatchedImmediatelyHandler : IHandleMessages<MessageToBeDispatchedImmediately>
            {
                public Context Context { get; set; }

                public Task Handle(MessageToBeDispatchedImmediately message, IMessageHandlerContext context)
                {
                    Context.MessageDispatched = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class InitiatingMessage : ICommand { }
        public class MessageToBeDispatchedImmediately : ICommand { }
    }
}