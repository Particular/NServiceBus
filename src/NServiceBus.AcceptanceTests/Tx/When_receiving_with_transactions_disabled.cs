namespace NServiceBus.AcceptanceTests.Tx
{
    using System;
    using System.Transactions;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_receiving_with_transactions_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_roll_the_message_back_to_the_queue_in_case_of_failure()
        {

            Scenario.Define<Context>()
                    .WithEndpoint<NonTransactionalEndpoint>(b => b.Given(bus => bus.SendLocal(new MyMessage())))
                    .AllowExceptions()
                    .Done(c => c.TestComplete)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.AreEqual(1, c.TimesCalled, "Should not retry the message"))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool TestComplete { get; set; }

            public int TimesCalled { get; set; }
        }

        public class NonTransactionalEndpoint : EndpointConfigurationBuilder
        {
            public NonTransactionalEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Transactions().Disable());
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }
                public void Handle(MyMessage message)
                {
                    Context.TimesCalled++;

                    using (new TransactionScope(TransactionScopeOption.Suppress))
                    {
                        Bus.SendLocal(new CompleteTest());
                    }

                    throw new Exception("Simulated exception");
                }
            }

            public class CompleteTestHandler : IHandleMessages<CompleteTest>
            {
                public Context Context { get; set; }

                public void Handle(CompleteTest message)
                {
                    Context.TestComplete = true;
                }
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }

        [Serializable]
        public class CompleteTest : ICommand
        {
        }


    }
}