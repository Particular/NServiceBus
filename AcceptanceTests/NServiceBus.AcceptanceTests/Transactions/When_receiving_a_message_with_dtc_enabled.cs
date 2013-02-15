namespace NServiceBus.AcceptanceTests.Transactions
{
    using System;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_receiving_a_message_with_dtc_enabled : NServiceBusIntegrationTest
    {
        [Test]
        public void Should_enlist_the_receive_in_the_dtc_tx()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<DTCEndpoint>(b => b.Given(bus => bus.SendLocal(new MyMessage())))
                    .Done(c => c.HandlerInvoked)
                    .Repeat(r => r.For<AllDtcTransports>())
                    .Should(c => Assert.AreNotEqual(Guid.Empty, c.DistributedIdentifier, "There should exists and DTC tx"))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool HandlerInvoked { get; set; }

            public Guid DistributedIdentifier { get; set; }
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

                public void Handle(MyMessage messageThatIsEnlisted)
                {
                    Transaction.Current.EnlistDurable(FakeDurableResourceManager.ResourceManagerId,
                                                      new FakeDurableResourceManager(), EnlistmentOptions.None);

                    Context.DistributedIdentifier = Transaction.Current.TransactionInformation.DistributedIdentifier;
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
