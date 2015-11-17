namespace NServiceBus.AcceptanceTests.Msmq
{
    using System;
    using System.Transactions;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_TimeToBeReceived_set_and_DTC_Msmq : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_on_send()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<TransactionalEndpoint>(b => b.Given((bus, c) =>
                    {
                        var exception = Assert.Throws<Exception>(() =>
                        {
                            using (new TransactionScope(TransactionScopeOption.Required))
                            { 
                                bus.SendLocal(new MyMessage());
                            }
                        });
                        Assert.IsTrue(exception.Message.EndsWith("Sending messages with a custom TimeToBeReceived is not supported on transactional MSMQ."));
                        }))
                    .Repeat(r => r.For<MsmqOnly>())
                    .Run();
        }

        public class Context : ScenarioContext
        {
        }
        public class TransactionalEndpoint : EndpointConfigurationBuilder
        {
            public TransactionalEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Transactions().Enable().EnableDistributedTransactions());
            }
        }

        [Serializable]
        [TimeToBeReceived("00:00:10")]
        public class MyMessage : IMessage
        {
        }
    }
}
