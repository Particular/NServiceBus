namespace NServiceBus.AcceptanceTests.Tx
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_requesting_immediate_dispatch_with_exactly_once : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_dispatch_immediately()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<ExactlyOnceEndpoint>(b => b.When(bus => bus.SendLocalAsync(new InitiatingMessage())))
                    .AllowSimulatedExceptions()
                    .Done(c => c.MessageDispatched)
                    .Run();

            Assert.True(context.MessageDispatched, "Should dispatch the message immediately");
        }

        public class Context : ScenarioContext
        {
            public bool MessageDispatched { get; set; }
        }

        public class ExactlyOnceEndpoint : EndpointConfigurationBuilder
        {
            public ExactlyOnceEndpoint()
            {
                //note: We don't have a explicit way to request "ExactlyOnce" yet so we have to rely on it beeing the default
                EndpointSetup<DefaultServer>();
            }

            public class InitiatingMessageHandler : IHandleMessages<InitiatingMessage>
            {
                public IBus Bus { get; set; }
                public async Task Handle(InitiatingMessage message)
                {
                    var options = new SendOptions();

                    options.RequireImmediateDispatch();
                    options.RouteToLocalEndpointInstance();

                    await Bus.SendAsync(new MessageToBeDispatchedImmediately(), options);

                    throw new SimulatedException();
                }
            }

            public class MessageToBeDispatchedImmediatelyHandler : IHandleMessages<MessageToBeDispatchedImmediately>
            {
                public Context Context { get; set; }

                public Task Handle(MessageToBeDispatchedImmediately message)
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