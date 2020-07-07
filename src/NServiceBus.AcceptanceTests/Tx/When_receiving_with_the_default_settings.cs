namespace NServiceBus.AcceptanceTests.Tx
{
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_receiving_with_the_default_settings : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_wrap_the_handler_pipeline_with_a_transactionscope()
        {
            Requires.DtcSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<TransactionalEndpoint>(b => b.When(session => session.SendLocal(new MyMessage())))
                .Done(c => c.HandlerInvoked)
                .Run();

            Assert.True(context.AmbientTransactionExists, "There should exist an ambient transaction");
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
                public MyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyMessage messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    testContext.AmbientTransactionExists = Transaction.Current != null;
                    testContext.HandlerInvoked = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }


        public class MyMessage : ICommand
        {
        }
    }
}