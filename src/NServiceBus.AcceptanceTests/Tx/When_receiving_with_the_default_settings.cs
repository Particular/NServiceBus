namespace NServiceBus.AcceptanceTests.Tx
{
    using System;
    using System.Transactions;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_receiving_with_the_default_settings : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_wrap_the_handler_pipeline_with_a_transactionscope()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<TransactionalEndpoint>(b => b.Given(bus => bus.SendLocal(new MyMessage())))
                    .Done(c => c.HandlerInvoked)
                    .Repeat(r => r.For(Transports.Default))
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

                public void Handle(MyMessage messageThatIsEnlisted)
                {
                    Context.AmbientTransactionExists = (Transaction.Current != null);
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