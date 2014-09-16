namespace NServiceBus.AcceptanceTests.Tx
{
    using System;
    using System.Transactions;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_receiving_with_dtc_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_escalate_a_single_durable_rm_to_dtc_tx()
        {

            Scenario.Define<Context>()
                    .WithEndpoint<NonDTCEndpoint>(b => b.Given(bus => bus.SendLocal(new MyMessage())))
                    .Done(c => c.HandlerInvoked)
                    .Repeat(r => r.For<AllDtcTransports>())
                    .Should(c =>
                        {
                            //this check mainly applies to MSMQ who creates a DTC tx right of the bat if DTC is on
                            Assert.AreEqual(Guid.Empty, c.DistributedIdentifierBefore, "No DTC tx should exist before enlistment");
                            Assert.True(c.CanEnlistPromotable, "A promotable RM should be able to enlist");
                        })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool HandlerInvoked { get; set; }

            public Guid DistributedIdentifierBefore { get; set; }

            public bool CanEnlistPromotable { get; set; }
        }

        public class NonDTCEndpoint : EndpointConfigurationBuilder
        {
            public NonDTCEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Transactions()
                    .DisableDistributedTransactions()
                    .WrapHandlersExecutionInATransactionScope());
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public void Handle(MyMessage messageThatIsEnlisted)
                {
                    Context.DistributedIdentifierBefore = Transaction.Current.TransactionInformation.DistributedIdentifier;

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