namespace NServiceBus.AcceptanceTests.Tx
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class Issue_2481 : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_enlist_the_receive_in_the_dtc_tx()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<DTCEndpoint>(b => b.When(bus => bus.SendLocal(new MyMessage())))
                    .Done(c => c.HandlerInvoked)
                    .Repeat(r => r.For<AllDtcTransports>())
                    .Should(c => Assert.False(c.CanEnlistPromotable, "There should exists a DTC tx"))
                    .Run();
        }


        public class Context : ScenarioContext
        {
            public bool HandlerInvoked { get; set; }

            public bool CanEnlistPromotable { get; set; }
        }

        public class DTCEndpoint : EndpointConfigurationBuilder
        {
            public DTCEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.UseTransport(context.GetTransportType())
                            .Transactions(TransportTransactionMode.TransactionScope);
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.CanEnlistPromotable = Transaction.Current.EnlistPromotableSinglePhase(new FakePromotableResourceManager());
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
