namespace NServiceBus.AcceptanceTests.NonTx
{
    using System;
    using System.Transactions;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_sending_inside_ambient_tx : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_roll_the_message_back_to_the_queue_in_case_of_failure()
        {

            Scenario.Define<Context>()
                    .WithEndpoint<NonTransactionalEndpoint>(b => b.Given(bus => bus.SendLocal(new MyMessage())))
                    .AllowExceptions()
                    .Done(c => c.TestComplete)
                    .Repeat(r => r.For<AllDtcTransports>()) 
                    .Should(c => Assert.False(c.MessageEnlistedInTheAmbientTxReceived, "The enlisted bus.Send should not commit"))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool TestComplete { get; set; }

            public bool MessageEnlistedInTheAmbientTxReceived { get; set; }
        }

        public class NonTransactionalEndpoint : EndpointConfigurationBuilder
        {
            public NonTransactionalEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Transactions().Disable().WrapHandlersExecutionInATransactionScope());
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }
                public void Handle(MyMessage message)
                {
                    Bus.SendLocal(new CompleteTest
                        {
                            EnlistedInTheAmbientTx = true
                        });

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
                    if (!Context.MessageEnlistedInTheAmbientTxReceived)
                        Context.MessageEnlistedInTheAmbientTxReceived = message.EnlistedInTheAmbientTx;

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
            public bool EnlistedInTheAmbientTx { get; set; }
        }


    }
}