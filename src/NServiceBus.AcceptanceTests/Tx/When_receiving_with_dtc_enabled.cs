﻿namespace NServiceBus.AcceptanceTests.Tx
{
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_receiving_with_dtc_enabled : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_enlist_the_receive_in_the_dtc_tx()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<DTCEndpoint>(b => b.When(session => session.SendLocal(new MyMessage())))
                .Done(c => c.HandlerInvoked)
                .Repeat(r => r.For<AllDtcTransports>())
                .Should(c => Assert.False(c.CanEnlistPromotable, "There should exists a DTC tx"))
                .Run();
        }

        [Test]
        public void Basic_assumptions_promotable_should_fail_if_durable_already_exists()
        {
            using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                Transaction.Current.EnlistDurable(FakePromotableResourceManager.ResourceManagerId, new FakePromotableResourceManager(), EnlistmentOptions.None);
                Assert.False(Transaction.Current.EnlistPromotableSinglePhase(new FakePromotableResourceManager()));

                tx.Complete();
            }
        }

        [Test]
        public void Basic_assumptions_second_promotable_should_fail()
        {
            using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                Assert.True(Transaction.Current.EnlistPromotableSinglePhase(new FakePromotableResourceManager()));

                Assert.False(Transaction.Current.EnlistPromotableSinglePhase(new FakePromotableResourceManager()));

                tx.Complete();
            }
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
                EndpointSetup<DefaultServer>();
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

        
        public class MyMessage : ICommand
        {
        }
    }
}