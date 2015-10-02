namespace NServiceBus.AcceptanceTests.Tx
{
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    //note: this test will no longer be relevant in v7
    public class When_requesting_immediate_dispatch_using_scope_suppress : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_dispatch_immediately()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<SuppressEndpoint>(b => b.When(bus => bus.SendLocalAsync(new InitiatingMessage())))
                    .AllowSimulatedExceptions()
                    .Done(c => c.MessageDispatched)
                    .Run();

            Assert.True(context.MessageDispatched, "Should dispatch the message immediately");
            Assert.True(context.Logs.Any(l=>l.Level == "warn" && l.Message.Contains("We detected that you suppressed the ambient transaction")));
        }

        public class Context : ScenarioContext
        {
            public bool MessageDispatched { get; set; }
        }

        public class SuppressEndpoint : EndpointConfigurationBuilder
        {
            public SuppressEndpoint()
            {
                 EndpointSetup<DefaultServer>();
            }

            public class InitiatingMessageHandler : IHandleMessages<InitiatingMessage>
            {
                public IBus Bus { get; set; }
                public async Task Handle(InitiatingMessage message)
                {
                    using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await Bus.SendLocalAsync(new MessageToBeDispatchedImmediately());
                    }

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