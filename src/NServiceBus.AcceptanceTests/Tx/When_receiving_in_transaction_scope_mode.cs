namespace NServiceBus.AcceptanceTests.Tx
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_receiving_in_transaction_scope_mode : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_enlist_in_dtc_transaction()
        {
            Requires.DtcSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<DTCEndpoint>(b => b.When(session => session.SendLocal(new MyMessage())))
                .Done(c => c.HandlerInvoked)
                .Run();

            Assert.True(context.DtcTransactionPresent, "There should exists a DTC tx");
        }


        public class Context : ScenarioContext
        {
            public bool HandlerInvoked { get; set; }

            public bool DtcTransactionPresent { get; set; }
        }

        public class DTCEndpoint : EndpointConfigurationBuilder
        {
            public DTCEndpoint()
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
                    Transaction.Current.EnlistDurable(FakePromotableResourceManager.ResourceManagerId, new FakePromotableResourceManager(), EnlistmentOptions.None);
                    testContext.DtcTransactionPresent = Transaction.Current.TransactionInformation.DistributedIdentifier != Guid.Empty;
                    testContext.HandlerInvoked = true;
                    return Task.CompletedTask;
                }

                Context testContext;
            }
        }

        public class MyMessage : ICommand
        {
        }
    }
}
