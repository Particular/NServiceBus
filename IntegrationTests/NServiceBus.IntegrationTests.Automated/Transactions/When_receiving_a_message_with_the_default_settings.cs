namespace NServiceBus.IntegrationTests.Automated.Transactions
{
    using System;
    using System.Transactions;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;
    using Support;

    public class When_receiving_a_message_with_the_default_settings:NServiceBusIntegrationTest
    {
        [Test]
        public void Should_wrap_the_handler_pipeline_with_a_transactionscope()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<TransactionalEndpoint>()
                    .Done(c => c.HandlerInvoked)
                    .Repeat(r => r.For<AllTransports>())
                    .Should(c =>
                    {
                        Assert.True(c.AmbientTransactionExists, "There should exist an ambient transaction");
                    })

                    .Run();
        }

        public class Context : BehaviorContext
        {
            public bool AmbientTransactionExists { get; set; }
            public bool HandlerInvoked { get; set; }
        }

        public class TransactionalEndpoint : EndpointBuilder
        {
            public TransactionalEndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .When(bus => bus.SendLocal(new MyMessage()));
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