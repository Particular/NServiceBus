namespace NServiceBus.AcceptanceTests.Tx
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_receiving_with_the_default_settings : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_wrap_the_handler_pipeline_with_a_transactionscope()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<TransactionalEndpoint>(b => b.When(bus => bus.SendLocal(new MyMessage())))
                    .Done(c => c.HandlerInvoked)
                    .Repeat(r => r.For<AllDtcTransports>())
                    .Should(c => Assert.True(c.AmbientTransactionExists, "There should exist an ambient transaction"))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool AmbientTransactionExists { get; set; }
            public bool HandlerInvoked { get; set; }
        }

        public class TransactionalEndpoint : EndpointConfigurationBuilder
        {
            public TransactionalEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.AmbientTransactionExists = Transaction.Current != null;
                    Context.HandlerInvoked = true;
                return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }
    }
}