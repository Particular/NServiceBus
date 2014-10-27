namespace NServiceBus.AcceptanceTests.Tx
{
    using System;
    using System.Transactions;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class Issue_2481 : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_enlist_the_receive_in_the_dtc_tx()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<DTCEndpoint>(b => b.Given(bus => bus.SendLocal(new MyMessage())))
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
                EndpointSetup<DefaultServer>(c=>c.Transactions().Enable());
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public void Handle(MyMessage messageThatIsEnlisted)
                {
                    Context.CanEnlistPromotable = Transaction.Current.EnlistPromotableSinglePhase(new FakePromotableResourceManager());
                    Context.HandlerInvoked = true;
                }
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }



    }
}
