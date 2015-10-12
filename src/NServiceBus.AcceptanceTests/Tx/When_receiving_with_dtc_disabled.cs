﻿namespace NServiceBus.AcceptanceTests.Tx
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.ConsistencyGuarantees;
    using ScenarioDescriptors;
    using NUnit.Framework;

    public class When_receiving_with_dtc_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_escalate_a_single_durable_rm_to_dtc_tx()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<NonDTCEndpoint>(b => b.When(bus => bus.SendLocalAsync(new MyMessage())))
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
                EndpointSetup<DefaultServer>(c => c.RequiredConsistency(ConsistencyGuarantee.AtLeastOnce));
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage messageThatIsEnlisted)
                {
                    using (var tx = new TransactionScope())
                    {
                        Context.DistributedIdentifierBefore = Transaction.Current.TransactionInformation.DistributedIdentifier;

                        Context.CanEnlistPromotable = Transaction.Current.EnlistPromotableSinglePhase(new FakePromotableResourceManager());

                        tx.Complete();
                    }

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