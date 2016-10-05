namespace NServiceBus.AcceptanceTests
{
    using System;
    using AcceptanceTesting;
    using Config;
    using NUnit.Framework;

    public class When_TimeToBeReceivedOnForwardedMessages_set_and_tx_scope_receives : NServiceBusAcceptanceTest
    {
        [Test]
        public void Endpoint_should_not_start_and_show_error()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>()
                    .Done(c => c.EndpointsStarted)
                    .Run();
            }, Throws.InnerException.InnerException.Message.Contains("Setting a custom TimeToBeReceivedOnForwardedMessages is not supported on transactional MSMQ."));
        }

        public class Context : ScenarioContext
        {
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.UseTransport<MsmqTransport>()
                        .Transactions(TransportTransactionMode.TransactionScope);
                })
                    .WithConfig<UnicastBusConfig>(c => c.TimeToBeReceivedOnForwardedMessages = TimeSpan.FromHours(1));
            }
        }
    }
}