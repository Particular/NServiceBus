namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_TimeToBeReceived_set_and_dtc : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_throw_on_send()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<TransactionalEndpoint>(b => b.When(async (session, c) =>
                    {
                        try
                        {
                            using (new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
                            {
                                await session.SendLocal(new MyMessage());
                            }
                        }
                        catch (Exception ex)
                        {
                            c.Exception = ex;
                            c.GotTheException = true;
                        }
                    }))
                    .Done(c => c.GotTheException)
                    .Run();

            Assert.IsTrue(context.Exception.Message.EndsWith("Sending messages with a custom TimeToBeReceived is not supported on transactional MSMQ."));
        }

        public class Context : ScenarioContext
        {
            public bool GotTheException { get; set; }
            public Exception Exception { get; set; }
        }
        public class TransactionalEndpoint : EndpointConfigurationBuilder
        {
            public TransactionalEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.UseTransport<MsmqTransport>()
                            .Transactions(TransportTransactionMode.TransactionScope);
                });
            }
        }

        [TimeToBeReceived("00:00:10")]
        public class MyMessage : IMessage
        {
        }
    }
}