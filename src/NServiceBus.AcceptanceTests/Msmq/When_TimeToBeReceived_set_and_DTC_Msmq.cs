namespace NServiceBus.AcceptanceTests.Msmq
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_TimeToBeReceived_set_and_DTC_Msmq : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_throw_on_send()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<TransactionalEndpoint>(b => b.When(async (bus, c) =>
                    {
                        try
                        {
                            using (new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
                            {
                                await bus.SendLocal(new MyMessage());
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
                    config.UseTransport(context.GetTransportType())
                            .Transactions(TransportTransactionMode.TransactionScope);
                });
            }
        }

        [Serializable]
        [TimeToBeReceived("00:00:10")]
        public class MyMessage : IMessage
        {
        }
    }
}