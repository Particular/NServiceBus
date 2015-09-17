namespace NServiceBus.AcceptanceTests.NonTx
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_sending_inside_ambient_tx : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_roll_the_message_back_to_the_queue_in_case_of_failure()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<NonTransactionalEndpoint>(b => b.Given(bus => bus.SendLocalAsync(new MyMessage())))
                    .AllowSimulatedExceptions()
                    .Done(c => c.TestComplete)
                    .Repeat(r => r.For<AllDtcTransports>())
                    .Should(c => Assert.False(c.MessageEnlistedInTheAmbientTxReceived, "The enlisted bus.SendAsync should not commit"))
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
                public async Task Handle(MyMessage message)
                {
                    await Bus.SendLocalAsync(new CompleteTest
                    {
                        EnlistedInTheAmbientTx = true
                    });

                    using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await Bus.SendLocalAsync(new CompleteTest());
                    }

                    throw new SimulatedException();
                }
            }

            public class CompleteTestHandler : IHandleMessages<CompleteTest>
            {
                public Context Context { get; set; }

                public Task Handle(CompleteTest message)
                {
                    if (!Context.MessageEnlistedInTheAmbientTxReceived)
                        Context.MessageEnlistedInTheAmbientTxReceived = message.EnlistedInTheAmbientTx;

                    Context.TestComplete = true;

                    return Task.FromResult(0);
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